using System.Reflection;
using HPRebar.Mcp.Contracts.Messages;

namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     Answers "what does this type look like?" by reflection over the assemblies it is given (the Revit
///     API assemblies, in the add-in). Read-only, needs no Revit thread, and cheap enough for the AI to
///     call before every unfamiliar API — cheaper than a compile error round trip.
/// </summary>
public sealed class TypeInspector
{
    private static readonly HashSet<string> ObjectMembers = ["ToString", "GetHashCode", "Equals", "GetType", "ReferenceEquals", "Finalize", "MemberwiseClone"];

    private readonly Lazy<Type[]> _types;

    public TypeInspector(IReadOnlyCollection<Assembly> assemblies)
    {
        _types = new Lazy<Type[]>(() => assemblies.SelectMany(SafeExportedTypes).ToArray());
    }

    public InspectResult Inspect(InspectRequest request)
    {
        var wanted = request.TypeName.Trim();
        var matches = _types.Value.Where(t => string.Equals(t.FullName, wanted, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length == 0) matches = _types.Value.Where(t => string.Equals(t.Name, wanted, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (matches.Length == 0)
        {
            var close = _types.Value
                .Where(t => t.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.FullName)
                .OrderBy(n => n!.Length)
                .Take(10)
                .ToArray();

            return new InspectResult
            {
                TypeName = wanted,
                Message = close.Length == 0
                    ? $"No public type named '{wanted}' in the Revit API assemblies."
                    : $"No type named '{wanted}'. Close matches: {string.Join(", ", close)}",
            };
        }

        if (matches.Length > 1)
        {
            return new InspectResult
            {
                TypeName = wanted,
                Message = $"'{wanted}' is ambiguous. Use one of: {string.Join(", ", matches.Select(t => t.FullName))}",
            };
        }

        var type = matches[0];
        var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => !ObjectMembers.Contains(m.Name) && !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_")
                        && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_") && !m.Name.StartsWith("op_"))
            .Select(Describe)
            .Where(d => d is not null)
            .Select(d => d!)
            .Where(d => request.MemberFilter is null || d.Signature.Contains(request.MemberFilter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.Kind).ThenBy(d => d.Signature, StringComparer.Ordinal)
            .ToArray();

        var max = Math.Clamp(request.MaxMembers, 1, 500);

        return new InspectResult
        {
            TypeName = type.Name,
            FullName = type.FullName,
            BaseType = type.BaseType?.FullName,
            Members = members.Take(max).ToArray(),
            Truncated = members.Length > max,
        };
    }

    private static MemberSignature? Describe(MemberInfo member) => member switch
    {
        PropertyInfo p => new MemberSignature("property",
            $"{Name(p.PropertyType)} {p.Name} {{ {(p.CanRead ? "get; " : string.Empty)}{(p.CanWrite ? "set; " : string.Empty)}}}"),
        MethodInfo m => new MemberSignature("method",
            $"{(m.IsStatic ? "static " : string.Empty)}{Name(m.ReturnType)} {m.Name}({string.Join(", ", m.GetParameters().Select(x => $"{Name(x.ParameterType)} {x.Name}"))})"),
        FieldInfo f => new MemberSignature("field", $"{(f.IsStatic ? "static " : string.Empty)}{Name(f.FieldType)} {f.Name}"),
        EventInfo e => new MemberSignature("event", $"event {Name(e.EventHandlerType!)} {e.Name}"),
        ConstructorInfo c => new MemberSignature("constructor",
            $"new {c.DeclaringType!.Name}({string.Join(", ", c.GetParameters().Select(x => $"{Name(x.ParameterType)} {x.Name}"))})"),
        _ => null,
    };

    private static string Name(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Name))}>";
        }

        return type.IsByRef ? "ref " + Name(type.GetElementType()!) : type.Name;
    }

    private static IEnumerable<Type> SafeExportedTypes(Assembly assembly)
    {
        try { return assembly.GetExportedTypes(); }
        catch (ReflectionTypeLoadException exception) { return exception.Types.Where(t => t is not null)!; }
    }
}
