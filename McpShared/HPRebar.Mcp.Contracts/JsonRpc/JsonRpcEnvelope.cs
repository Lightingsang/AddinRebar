using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPRebar.Mcp.Contracts.JsonRpc;

/// <summary>
///     One JSON-RPC 2.0 message as it travels over the pipe, one object per line. A single shape covers
///     requests, notifications and responses so a reader can deserialize first and classify afterwards
///     via <see cref="Kind"/>; the factories below produce each kind with only its relevant fields set.
/// </summary>
public sealed class JsonRpcEnvelope
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }

    [JsonPropertyName("result")]
    public JsonElement? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }

    [JsonIgnore]
    public JsonRpcKind Kind =>
        Method is null
            ? JsonRpcKind.Response
            : Id is null
                ? JsonRpcKind.Notification
                : JsonRpcKind.Request;

    public static JsonRpcEnvelope Request(long id, string method, object? parameters) => new JsonRpcEnvelope
    {
        Id = id,
        Method = method,
        Params = BridgeJson.ToElement(parameters),
    };

    public static JsonRpcEnvelope Notification(string method, object? parameters) => new JsonRpcEnvelope
    {
        Method = method,
        Params = BridgeJson.ToElement(parameters),
    };

    public static JsonRpcEnvelope Success(long id, object? result) => new JsonRpcEnvelope
    {
        Id = id,
        Result = BridgeJson.ToElement(result),
    };

    public static JsonRpcEnvelope Failure(long id, int code, string message) => new JsonRpcEnvelope
    {
        Id = id,
        Error = new JsonRpcError { Code = code, Message = message },
    };

    /// <summary>Deserializes <see cref="Params"/> (request) or <see cref="Result"/> (response) into a DTO.</summary>
    public T? ParamsAs<T>() => Params is null ? default : Params.Value.Deserialize<T>(BridgeJson.Options);

    public T? ResultAs<T>() => Result is null ? default : Result.Value.Deserialize<T>(BridgeJson.Options);
}

public enum JsonRpcKind
{
    Request,
    Notification,
    Response,
}
