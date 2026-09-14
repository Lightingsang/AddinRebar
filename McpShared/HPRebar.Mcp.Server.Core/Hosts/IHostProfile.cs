using System.Reflection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;

namespace HPRebar.Mcp.Server.Hosts;

/// <summary>
///     Everything that differs between the CAD hosts an MCP server exe can serve. The engine in this
///     assembly reads the host through this seam only; each exe supplies one profile at startup and
///     never switches at runtime — one exe, one host, one pipe, one registry root.
/// </summary>
public interface IHostProfile
{
    /// <summary>`revit`, `autocad`. Stored in tool.json as `host` and used in pipe names.</summary>
    string HostId { get; }

    /// <summary>Display name in messages: "Revit", "AutoCAD".</summary>
    string DisplayName { get; }

    /// <summary>MCP `serverInfo.name`.</summary>
    string ServerName { get; }

    /// <summary>`%AppData%\{ProductFolder}\McpServer\` holds the tools library and the registry database.</summary>
    string ProductFolder { get; }

    /// <summary>Environment-variable prefix for configuration overrides, e.g. `HPREBAR_MCP_`.</summary>
    string EnvPrefix { get; }

    int DefaultVersion { get; }

    IReadOnlyCollection<int> ValidVersions { get; }

    /// <summary>Wire prefix of every pipe method, e.g. `revit.` → `revit.execute`.</summary>
    string MethodPrefix { get; }

    string ExecuteToolName { get; }

    string ContextToolName { get; }

    /// <summary>Scheme of the document resources: `revit` → `revit://document/info`.</summary>
    string ResourceScheme { get; }

    /// <summary>Tool categories the registry accepts for this host.</summary>
    IReadOnlyCollection<string> Categories { get; }

    /// <summary>Names of the host's own core tools; a registry tool may not shadow them.</summary>
    IReadOnlyCollection<string> CoreToolNames { get; }

    /// <summary>Default `using`s of a script in this host, from <see cref="HostScriptContracts"/>.</summary>
    IReadOnlyCollection<string> ScriptImports { get; }

    /// <summary>One paragraph for prompts: globals, units, transaction rules of this host's scripts.</summary>
    string ScriptContractSummary { get; }

    /// <summary>The exe assembly: where the host's tool classes and embedded seed library live.</summary>
    Assembly HostAssembly { get; }

    string PipeName(int version);

    /// <summary>`Method("execute")` → `revit.execute` / `autocad.execute`.</summary>
    string Method(string suffix);
}

/// <summary>
///     Data-only implementation. <see cref="Revit"/> carries the values the Revit server shipped with,
///     so every engine constructor can default to it and the 2026-09 tests keep passing unchanged;
///     an exe copies it and sets <see cref="HostAssembly"/>.
/// </summary>
public sealed class HostProfile : IHostProfile
{
    public static readonly HostProfile Revit = new HostProfile
    {
        HostId = PipeNaming.RevitHost,
        DisplayName = "Revit",
        ServerName = "HPRebar Revit MCP",
        ProductFolder = "HPRebar",
        EnvPrefix = "HPREBAR_MCP_",
        DefaultVersion = 2026,
        ValidVersions = new[] { 2025, 2026 },
        MethodPrefix = JsonRpcMethods.RevitPrefix,
        ExecuteToolName = "execute_revit_code",
        ContextToolName = "get_revit_context",
        ResourceScheme = "revit",
        Categories = new[] { "Architecture", "Structure", "MEP", "Annotation", "View", "Data", "Generic" },
        CoreToolNames = new[] { "execute_revit_code", "get_revit_context", "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.RevitImports,
        ScriptContractSummary =
            "Globals: doc (Document), uidoc (UIDocument), app (Application), uiapp (UIApplication), ct, log(string), progress(cur,total,msg), args. " +
            "Revit API lengths are in feet; accept millimetres in args and convert with UnitUtils. " +
            "transaction: auto when the code modifies the model and opens no Transaction itself; none when it only reads; manual only if it opens its own Transaction.",
        HostAssembly = typeof(HostProfile).Assembly,
    };

    public required string HostId { get; init; }

    public required string DisplayName { get; init; }

    public required string ServerName { get; init; }

    public required string ProductFolder { get; init; }

    public required string EnvPrefix { get; init; }

    public required int DefaultVersion { get; init; }

    public required IReadOnlyCollection<int> ValidVersions { get; init; }

    public required string MethodPrefix { get; init; }

    public required string ExecuteToolName { get; init; }

    public required string ContextToolName { get; init; }

    public required string ResourceScheme { get; init; }

    public required IReadOnlyCollection<string> Categories { get; init; }

    public required IReadOnlyCollection<string> CoreToolNames { get; init; }

    public required IReadOnlyCollection<string> ScriptImports { get; init; }

    public required string ScriptContractSummary { get; init; }

    public required Assembly HostAssembly { get; init; }

    public string PipeName(int version) => PipeNaming.For(HostId, version);

    public string Method(string suffix) => JsonRpcMethods.For(MethodPrefix, suffix);

    /// <summary>Same data, different exe assembly — what a host exe registers in DI.</summary>
    public HostProfile WithHostAssembly(Assembly assembly) => new HostProfile
    {
        HostId = HostId, DisplayName = DisplayName, ServerName = ServerName, ProductFolder = ProductFolder, EnvPrefix = EnvPrefix,
        DefaultVersion = DefaultVersion, ValidVersions = ValidVersions, MethodPrefix = MethodPrefix, ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName, ResourceScheme = ResourceScheme, Categories = Categories, CoreToolNames = CoreToolNames,
        ScriptImports = ScriptImports, ScriptContractSummary = ScriptContractSummary, HostAssembly = assembly,
    };
}
