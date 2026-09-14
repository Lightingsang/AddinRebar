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
