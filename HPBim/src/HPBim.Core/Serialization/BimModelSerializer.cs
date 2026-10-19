namespace HPBim.Core.Serialization;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Models;
using HPBim.Core.Validation;

/// <summary>
/// Clean JSON serialization and deserialization for BimModel using System.Text.Json.
/// Universal netstandard2.0 compatible with zero external dependencies.
/// </summary>
public static class BimModelSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = CreateOptions(true);
    private static readonly JsonSerializerOptions CompactOptions = CreateOptions(false);

    public static JsonSerializerOptions CreateOptions(bool indented)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static string Serialize(BimModel model, bool indented = true)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        return JsonSerializer.Serialize(model, indented ? DefaultOptions : CompactOptions);
    }

    public static BimModel Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON content cannot be null or whitespace.", nameof(json));
        }

        var model = JsonSerializer.Deserialize<BimModel>(json, DefaultOptions);
        return model ?? throw new JsonException("Deserialization returned null BimModel.");
    }

    public static void SerializeToFile(BimModel model, string filePath, bool indented = true)
    {
        string json = Serialize(model, indented);
        File.WriteAllText(filePath, json);
    }

    public static BimModel DeserializeFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"BIM model JSON file not found at: {filePath}", filePath);
        }

        string json = File.ReadAllText(filePath);
        return Deserialize(json);
    }

    public static async Task SerializeToFileAsync(
        BimModel model, string filePath, bool indented = true, CancellationToken cancellationToken = default)
    {
        string json = Serialize(model, indented);
        using (var writer = new StreamWriter(filePath, false))
        {
            await writer.WriteAsync(json).ConfigureAwait(false);
        }
    }

    public static async Task<BimModel> DeserializeFromFileAsync(
        string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"BIM model JSON file not found at: {filePath}", filePath);
        }

        using (var reader = new StreamReader(filePath))
        {
            string json = await reader.ReadToEndAsync().ConfigureAwait(false);
            return Deserialize(json);
        }
    }

    /// <summary>
    /// Deserializes JSON after strictly validating against BimModelSchemaValidator.
    /// Throws SchemaValidationException if the JSON is raw CAD, empty, or violates schema rules.
    /// </summary>
    public static BimModel DeserializeValidated(string json, bool requireStrictDiscriminant = false)
    {
        var validationResult = BimModelSchemaValidator.Validate(json, requireStrictDiscriminant);
        if (!validationResult.IsValid)
        {
            throw new SchemaValidationException(validationResult.Errors);
        }

        return Deserialize(json);
    }

    public static BimModel DeserializeValidatedFromFile(string filePath, bool requireStrictDiscriminant = false)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"BIM model JSON file not found at: {filePath}", filePath);
        }

        string json = File.ReadAllText(filePath);
        return DeserializeValidated(json, requireStrictDiscriminant);
    }

    /// <summary>
    /// Asynchronously reads file and deserializes after strict schema validation.
    /// </summary>
    public static async Task<BimModel> DeserializeValidatedFromFileAsync(
        string filePath, bool requireStrictDiscriminant = false, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"BIM model JSON file not found at: {filePath}", filePath);
        }

        using (var reader = new StreamReader(filePath))
        {
            string json = await reader.ReadToEndAsync().ConfigureAwait(false);
            return DeserializeValidated(json, requireStrictDiscriminant);
        }
    }
}
