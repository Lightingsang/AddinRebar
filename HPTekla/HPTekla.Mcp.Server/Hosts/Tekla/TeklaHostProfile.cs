using System.Reflection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;

namespace HPTekla.Mcp.Server.Hosts.Tekla;

/// <summary>
///     The Trimble Tekla Structures 2025.0 profile: pipe <c>hptekla-mcp-{version}</c>,
///     wire prefix <c>tekla.</c>, registry root <c>%AppData%\HPTekla\McpServer\</c>, 600 s timeout ceiling,
///     and this assembly hosting the Tekla tool classes and the embedded seed library.
/// </summary>
public sealed class TeklaHostProfile : IHostProfile
{
    public const string ExecuteToolName = "execute_tekla_code";
    public const string ContextToolName = "get_tekla_context";
    public const int Version = 2025;
    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.TeklaHeavyMaxTimeoutSeconds;
    public const string BridgePluginName = "HPTekla MCP Bridge";

    public static readonly TeklaHostProfile Instance = new();

    public string HostId => PipeNaming.TeklaHost;
    public string DisplayName => "Tekla Structures";
    public string ServerName => "HPTekla MCP";
    public string ProductFolder => "HPTekla";
    public string EnvPrefix => "HPTEKLA_MCP_";
    public int DefaultVersion => Version;
    public IReadOnlyCollection<int> ValidVersions => new[] { 2025 };
    public string MethodPrefix => JsonRpcMethods.TeklaPrefix;

    string IHostProfile.ExecuteToolName => ExecuteToolName;
    string IHostProfile.ContextToolName => ContextToolName;
    public string ResourceScheme => PipeNaming.TeklaHost;

    public IReadOnlyCollection<string> Categories => new[]
    {
        "Model", "Geometry", "Property", "Rebar", "Drawing", "Export", "Generic"
    };

    public IReadOnlyCollection<string> CoreToolNames => new[]
    {
        ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution"
    };

    public IReadOnlyCollection<string> ScriptImports => HostScriptContracts.TeklaImports;

    public string ScriptContractSummary =>
        "Globals: model (Tekla.Structures.Model.Model), selector (ModelObjectSelector), ct (CancellationToken), " +
        "log(string), progress(cur,total,msg), args (ScriptArgs). " +
        "Coordinates and lengths are in millimetres (mm). " +
        "Transaction control: transaction=auto (default for write operations) commits model changes via model.CommitChanges() upon successful completion; " +
        "dryRun=true runs the script logic in memory without calling CommitChanges(), ensuring zero persistence. " +
        "Heavy operations (e.g. IFC export, drawing numbering) require the 'Allow heavy operations' toggle in HPTekla MCP Bridge. " +
        "No MessageBox/Process/#r/#load. Scripts must end with return <value>;.";

    public Assembly HostAssembly => typeof(TeklaHostProfile).Assembly;
    public string CliExecutable => "HPTekla.Mcp.Server.exe";
    public int MaxTimeoutSeconds => HeavyMaxTimeoutSeconds;

    public string? BridgeNotConnectedHint =>
        $"Open Tekla Structures {Version} and ensure the HPTekla MCP Bridge plugin is loaded (Named Pipe: {PipeNaming.For(PipeNaming.TeklaHost, Version)}).";

    public string? TimeoutSemanticsHint =>
        "Tekla Structures script execution timed out; in dryRun=true mode no changes were committed, while in auto mode changes may have been discarded.";

    public string PipeName(int version) => PipeNaming.For(HostId, version);
    public string Method(string suffix) => JsonRpcMethods.For(MethodPrefix, suffix);
}
