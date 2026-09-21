using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;

namespace HPSap2000.Mcp.Server.Hosts;

/// <summary>
///     The SAP2000 profile as this exe registers it: pipe <c>hpsap2000-mcp-{version}</c> where the version is CSI's
///     own major number (27 for SAP2000 27 — CSI numbers releases, it does not name them after years), wire prefix
///     <c>sap2000.</c>, registry root <c>%AppData%\HPSap2000\McpServer\</c>, a 600 s timeout ceiling (an analysis run
///     cannot be interrupted and may take minutes), and this assembly as the home of the SAP2000 tool classes and the
///     embedded seed library. The bridge is a separate desktop program, not an add-in, so the two message hints
///     tell the user to start it rather than to look for it inside SAP2000. One exe, one host, one pipe.
/// </summary>
public static class Sap2000HostProfile
{
    public const string ExecuteToolName = "execute_sap2000_code";
    public const string ContextToolName = "get_sap2000_context";

    /// <summary>The primary verified SAP2000 release on the dev machine (v27).</summary>
    public const int Version = 27;

    /// <summary>Longest run the bridge accepts once the user allowed destructive operations (shared constant); the bridge clamps to 120 otherwise.</summary>
    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.Sap2000HeavyMaxTimeoutSeconds;

    public const string BridgeExecutable = "HPSap2000.McpBridge.exe";

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.Sap2000Host,
        DisplayName = "SAP2000",
        ServerName = "HPSap2000 MCP",
        ProductFolder = "HPSap2000",
        EnvPrefix = "HPSAP2000_MCP_",
        DefaultVersion = Version,
        ValidVersions = new[] { 24, 25, 26, 27 },
        MethodPrefix = JsonRpcMethods.Sap2000Prefix,
        ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = PipeNaming.Sap2000Host,
        Categories = new[] { "Model", "Geometry", "Property", "Load", "Analysis", "Results", "Table", "Data", "Generic" },
        CoreToolNames = new[] { ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.Sap2000Imports,
        ScriptContractSummary =
            "Globals: sapModel (cSapModel of the attached SAP2000), sap (cOAPI), units (present units are forced to kN_m_C for the run: lengths m, forces kN, moments kN·m, stresses kN/m²; restored afterwards), " +
            "ct, log(string), progress(cur,total,msg), args. Every OAPI call returns an int: check `ret` and throw InvalidOperationException($\"SAP2000 returned {ret} from X\"); caller-input problems throw ArgumentException. " +
            "SAP2000 has no transaction and no undo. Three tiers decided statically before the run: R read-only (Get*/Is*/Has*/Count/RefreshView/AnalysisResults*/GetTableForDisplayArray — transaction: none), " +
            "W write (any other member — transaction: auto; the bridge saves the model and copies a .SDB snapshot first; unsaved or UNC models are refused; rolledBack:false after an exception means the changes persisted), " +
            "D destructive (SetModelIsLocked, RunAnalysis, DeleteResults, File.OpenFile/New*/Save(path), ApplyEditedTables, Start*/Modify*/Merge*/Reset*/Clear*/Rename*/Show*/Export*/Import*, any path-taking member — the user must tick 'Allow destructive operations' in the HPSap2000 MCP Bridge window, else -32001; up to 600 s). " +
            "dryRun or transaction: none on a writing script is a static preview (PREVIEW diagnostic, nothing runs); manual runs like auto. Cancel and timeout cannot interrupt a running SAP2000 call. " +
            "Never use Helper, ApplicationExit, CreateObject or dialogs — the guard rejects them.",
        MaxTimeoutSeconds = HeavyMaxTimeoutSeconds,
        HostAssembly = typeof(Sap2000HostProfile).Assembly,
        CliExecutable = "HPSap2000.Mcp.Server.exe",
        BridgeNotConnectedHint =
            $"Start {BridgeExecutable} beside SAP2000 {Version}, click Attach and tick 'Allow AI code execution' (pipe {PipeNaming.For(PipeNaming.Sap2000Host, Version)}).",
        TimeoutSemanticsHint =
            "SAP2000 may still be running the call; changes made before the timeout persisted (SAP2000 has no rollback) — check the snapshot named in the bridge window before retrying.",
    };
}
