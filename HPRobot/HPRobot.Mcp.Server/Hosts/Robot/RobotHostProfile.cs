using System.Reflection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;

namespace HPRobot.Mcp.Server.Hosts.Robot;

/// <summary>
///     The Autodesk Robot Structural Analysis Professional 2026 profile: pipe <c>hprobot-mcp-{version}</c>,
///     wire prefix <c>robot.</c>, registry root <c>%AppData%\HPRobot\McpServer\</c>, 300 s timeout ceiling,
///     and this assembly hosting the Robot tool classes and the embedded seed library.
/// </summary>
public sealed class RobotHostProfile : IHostProfile
{
    public const string ExecuteToolName = "execute_robot_code";
    public const string ContextToolName = "get_robot_context";

    public const int Version = 2026;

    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.RobotHeavyMaxTimeoutSeconds;

    public const string BridgeExecutable = "HPRobot.McpBridge.exe";

    public static readonly RobotHostProfile Instance = new();

    public string HostId => PipeNaming.RobotHost;

    public string DisplayName => "Robot Structural Analysis";

    public string ServerName => "HPRobot MCP";

    public string ProductFolder => "HPRobot";

    public string EnvPrefix => "HPROBOT_MCP_";

    public int DefaultVersion => Version;

    public IReadOnlyCollection<int> ValidVersions => new[] { 2024, 2025, 2026 };

    public string MethodPrefix => JsonRpcMethods.RobotPrefix;

    string IHostProfile.ExecuteToolName => ExecuteToolName;

    string IHostProfile.ContextToolName => ContextToolName;

    public string ResourceScheme => PipeNaming.RobotHost;

    public IReadOnlyCollection<string> Categories => new[]
    {
        "Model", "Geometry", "Property", "Load", "Analysis", "Results", "Generic"
    };

    public IReadOnlyCollection<string> CoreToolNames => new[]
    {
        ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution"
    };

    public IReadOnlyCollection<string> ScriptImports => HostScriptContracts.RobotImports;

    public string ScriptContractSummary =>
        "Globals: robot (IRobotApplication of the attached Robot), structure (IRobotStructure), units (IRobotUnitMngr; lengths in m, forces in kN, moments in kN·m, stresses in MPa; user unit preferences restored afterwards), ct, log(string), progress(cur,total,msg), args. " +
        "Robot Structural Analysis has no transaction or undo API. Tiers decided statically: " +
        "R read-only (Get*/Is*/Has*/Count/Find*/Exist/Query; transaction=none), " +
        "W write (Create/Add*/SetLabel*/SetValue*/Store*/Update; transaction=auto; the bridge takes an automatic .rtd snapshot first; unsaved models are refused), " +
        "D destructive/heavy (Calculate, Delete*, project.New/Open/Close/SaveAs — user must tick 'Allow heavy/destructive operations' in HPRobot MCP Bridge, else -32001; up to 300 s). " +
        "dryRun or transaction: none on a writing script is a static preview (PREVIEW diagnostic, nothing runs). " +
        "Never use Quit, ApplicationExit, Interactive, MessageBox, Process, reflection, threading, #r or #load — the guard rejects them. Always end with return <value>;.";

    public Assembly HostAssembly => typeof(RobotHostProfile).Assembly;

    public string CliExecutable => "HPRobot.Mcp.Server.exe";

    public int MaxTimeoutSeconds => HeavyMaxTimeoutSeconds;

    public string? BridgeNotConnectedHint =>
        $"Start {BridgeExecutable} beside Robot Structural Analysis Professional {Version}, click Attach and tick 'Allow AI code execution' (pipe {PipeNaming.For(PipeNaming.RobotHost, Version)}).";

    public string? TimeoutSemanticsHint =>
        "Robot Structural Analysis may still be running the call; changes made before the timeout persisted (no rollback) — check the snapshot named in the bridge window before retrying.";

    public string PipeName(int version) => PipeNaming.For(HostId, version);

    public string Method(string suffix) => JsonRpcMethods.For(MethodPrefix, suffix);
}
