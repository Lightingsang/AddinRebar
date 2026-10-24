using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using HPBimToolkit.Core.Protocol;

namespace HPBimToolkit.Core.Tests.TypeScript;

/// <summary>
///     Renders the frontend's contracts.generated.ts from the C# contracts of <see cref="ProtocolCatalog" />. Member names,
///     order and enum spellings come from <see cref="BridgeJson.Options" />, so the types describe exactly the JSON on the wire.
/// </summary>
internal sealed class TypeScriptContractWriter
{
    private const string Header =
        """
        // Generated from HPBimToolkit.Core/Contracts by TypeScriptContractsTests (HPBimToolkit.Core.Tests). Do not edit by hand:
        // change the C# contracts, then run `dotnet test` in HPBimToolkit.Core.Tests with HPBIM_UPDATE_CONTRACTS=1.

        """;

    private readonly NullabilityInfoContext _nullability = new();
    private readonly List<Type> _enums = [];
    private readonly List<Type> _interfaces = [];

    public static string Write() => new TypeScriptContractWriter().Render();

    /// <summary>Collects one type outside the catalog; tests use it to prove what the writer refuses.</summary>
    internal static void CollectOnly(Type type) => new TypeScriptContractWriter().Collect(type);

    private string Render()
    {
        foreach (var (_, request, response) in ProtocolCatalog.Commands)
        {
            if (request is not null)
            {
                Collect(request);
            }

            Collect(response);
        }

        foreach (var (_, payload) in ProtocolCatalog.Events)
        {
            Collect(payload);
        }

        var ts = new StringBuilder(Header).Append('\n');
        foreach (var (name, constantsOf) in ProtocolCatalog.ConstantUnions)
        {
            AppendUnion(ts, name, ConstantValues(constantsOf));
        }

        foreach (var type in _enums)
        {
            AppendUnion(ts, type.Name, Enum.GetNames(type).Select(JsonNamingPolicy.CamelCase.ConvertName).ToArray());
        }

        foreach (var type in _interfaces)
        {
            AppendInterface(ts, type);
        }

        AppendCommandMap(ts);
        AppendEventMap(ts);
        ts.Append("export type CommandName = keyof CommandMap;\n");
        ts.Append("export type EventName = keyof EventMap;\n");

        // The raw-string header carries the source file's line endings (CRLF on a Windows checkout).
        return ts.ToString().ReplaceLineEndings("\n");
    }

    private void Collect(Type type)
    {
        type = ElementOrSelf(Nullable.GetUnderlyingType(type) ?? type);
        if (type.IsEnum)
        {
            AddOnce(_enums, type);
            return;
        }

        if (Scalars.ContainsKey(type) || !AddOnce(_interfaces, type))
        {
            return;
        }

        if (type.IsPrimitive || type.IsValueType || type.IsGenericType || type.GetInterfaces().Any(IsDictionary))
        {
            throw new NotSupportedException($"{type} has no TypeScript mapping: use a record, an enum, a list or a scalar of {nameof(Scalars)}.");
        }

        foreach (var property in JsonProperties(type))
        {
            Collect(property.PropertyType);
        }
    }

    private void AppendInterface(StringBuilder ts, Type type)
    {
        ts.Append($"export interface {type.Name} {{\n");
        foreach (var property in JsonProperties(type))
        {
            var optional = IsOptional(type, property.Name) ? "?" : "";
            ts.Append($"  {property.Name}{optional}: {MemberType(type, property)};\n");
        }

        ts.Append("}\n\n");
    }

    private static void AppendCommandMap(StringBuilder ts)
    {
        ts.Append("/** Request and response payload per command, so `bridge.request` is typed end to end. */\n");
        ts.Append("export interface CommandMap {\n");
        foreach (var (name, request, response) in ProtocolCatalog.Commands)
        {
            ts.Append($"  '{name}': {{ request: {request?.Name ?? "undefined"}; response: {response.Name} }};\n");
        }

        ts.Append("}\n\n");
    }

    private static void AppendEventMap(StringBuilder ts)
    {
        ts.Append("export interface EventMap {\n");
        foreach (var (name, payload) in ProtocolCatalog.Events)
        {
            ts.Append($"  '{name}': {payload.Name};\n");
        }

        ts.Append("}\n\n");
    }

    private static void AppendUnion(StringBuilder ts, string name, IReadOnlyList<string> values)
    {
        var members = values.Select(value => $"'{value}'").ToArray();
        var line = $"export type {name} = {string.Join(" | ", members)};";
        ts.Append(line.Length <= 120 ? line : $"export type {name} =\n  | {string.Join("\n  | ", members)};");
        ts.Append("\n\n");
    }

    private string MemberType(Type owner, JsonPropertyInfo property)
    {
        var info = (PropertyInfo)property.AttributeProvider!;
        var tsType = ProtocolCatalog.StringUnions.TryGetValue((owner, info.Name), out var union)
            ? union
            : TypeName(property.PropertyType);
        var nullable = Nullable.GetUnderlyingType(property.PropertyType) is not null
                       || _nullability.Create(info).ReadState == NullabilityState.Nullable;
        return nullable ? $"{tsType} | null" : tsType;
    }

    private static string TypeName(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        var element = ElementOrSelf(type);
        if (element != type)
        {
            return $"{TypeName(element)}[]";
        }

        return type.IsEnum ? type.Name : Scalars.GetValueOrDefault(type, type.Name);
    }

    /// <summary>The only non-record member types the wire carries today; anything else is refused in <see cref="Collect" />.</summary>
    private static readonly Dictionary<Type, string> Scalars = new()
    {
        [typeof(string)] = "string",
        [typeof(bool)] = "boolean",
        [typeof(int)] = "number",
        [typeof(long)] = "number",
        [typeof(double)] = "number",
        [typeof(object)] = "unknown",
    };

    /// <summary>
    ///     A request parameter with a default value may be left out by the page: the host fills the default. Responses and
    ///     events always carry every member, so the rule applies to request payloads only.
    /// </summary>
    private static bool IsOptional(Type type, string jsonName) =>
        ProtocolCatalog.Commands.Any(command => command.Request == type) &&
        type.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter => parameter.HasDefaultValue
                              && string.Equals(parameter.Name, jsonName, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<JsonPropertyInfo> JsonProperties(Type type) => BridgeJson.Options.GetTypeInfo(type).Properties;

    private static string[] ConstantValues(Type constantsOf) =>
        constantsOf.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field is { IsLiteral: true } && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

    /// <summary>The element type of an array or list; dictionaries are objects on the wire, so they stay themselves (and are refused).</summary>
    private static Type ElementOrSelf(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType()!;
        }

        if (type == typeof(string) || type.GetInterfaces().Append(type).Any(IsDictionary))
        {
            return type;
        }

        var enumerable = type.GetInterfaces().Append(type)
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0] ?? type;
    }

    private static bool IsDictionary(Type type) =>
        type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) || type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

    private static bool AddOnce(List<Type> types, Type type)
    {
        if (types.Contains(type))
        {
            return false;
        }

        types.Add(type);
        return true;
    }
}
