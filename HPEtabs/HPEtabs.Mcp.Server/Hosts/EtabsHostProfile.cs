using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;

namespace HPEtabs.Mcp.Server.Hosts;

/// <summary>
///     The ETABS profile as this exe registers it: pipe <c>hpetabs-mcp-{version}</c> where the version is CSI's
///     own major number (22 for ETABS 22 — CSI numbers releases, it does not name them after years), wire prefix
///     <c>etabs.</c>, registry root <c>%AppData%\HPEtabs\McpServer\</c>, a 600 s timeout ceiling (an analysis run
///     cannot be interrupted and may take minutes), and this assembly as the home of the ETABS tool classes and the
///     embedded seed library. The bridge is a separate desktop program, not an add-in, so the two message hints
///     tell the user to start it rather than to look for it inside ETABS. One exe, one host, one pipe.
/// </summary>
public static class EtabsHostProfile
{
    public const string ExecuteToolName = "execute_etabs_code";
    public const string ContextToolName = "get_etabs_context";
    public const string ConnectToolName = "connect_etabs";

    /// <summary>The only ETABS release verified on the dev machine (v22.7.0.4095, OAPI wrapper 2.10).</summary>
    public const int Version = 22;

    /// <summary>Longest run the bridge accepts once the user allowed destructive operations (shared constant); the bridge clamps to 120 otherwise.</summary>
    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.EtabsHeavyMaxTimeoutSeconds;

    public const string BridgeExecutable = "HPEtabs.McpBridge.exe";

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.EtabsHost,
        DisplayName = "ETABS",
        ServerName = "HPEtabs MCP",
        ProductFolder = "HPEtabs",
        EnvPrefix = "HPETABS_MCP_",
        DefaultVersion = Version,
        ValidVersions = new[] { Version },
        MethodPrefix = JsonRpcMethods.EtabsPrefix,
        ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = PipeNaming.EtabsHost,
        Categories = new[] { "Model", "Geometry", "Property", "Load", "Analysis", "Results", "Table", "Data", "Generic" },
        CoreToolNames = new[] { ExecuteToolName, ContextToolName, ConnectToolName, "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.EtabsImports,
        ScriptContractSummary =
            "Globals: sapModel (cSapModel of the attached ETABS), etabs (cOAPI), units (present units are forced to kN_mm_C for the run: lengths mm, forces kN, moments kN·mm, stresses kN/mm²; restored afterwards), " +
            "ct, log(string), progress(cur,total,msg), args. Every OAPI call returns an int: check `ret` and throw InvalidOperationException($\"ETABS returned {ret} from X\"); caller-input problems throw ArgumentException. " +
            "ETABS has no transaction and no undo. Three tiers decided statically before the run: R read-only (Get*/Is*/Has*/Count/RefreshView/AnalysisResults*/GetTableForDisplayArray — transaction: none), " +
            "W write (any other member — transaction: auto; the bridge saves the model and copies a .EDB snapshot first; unsaved or UNC models are refused; rolledBack:false after an exception means the changes persisted), " +
            "D destructive (SetModelIsLocked, RunAnalysis, DeleteResults, File.OpenFile/New*/Save(path), ApplyEditedTables, Start*/Modify*/Merge*/Reset*/Clear*/Rename*/Show*/Export*/Import*, any path-taking member — the user must tick 'Allow destructive operations' in the HPEtabs MCP Bridge window, else -32001; up to 600 s). " +
            "dryRun or transaction: none on a writing script is a static preview (PREVIEW diagnostic, nothing runs); manual runs like auto. Cancel and timeout cannot interrupt a running ETABS call. " +
            "Never use Helper, ApplicationExit, CreateObject or dialogs — the guard rejects them.",
        MaxTimeoutSeconds = HeavyMaxTimeoutSeconds,
        HostAssembly = typeof(EtabsHostProfile).Assembly,
        CliExecutable = "HPEtabs.Mcp.Server.exe",
        BridgeNotConnectedHint =
            $"Start {BridgeExecutable} beside ETABS {Version}, click Attach and tick 'Allow AI code execution' (pipe {PipeNaming.For(PipeNaming.EtabsHost, Version)}).",
        TimeoutSemanticsHint =
            "ETABS may still be running the call; changes made before the timeout persisted (ETABS has no rollback) — check the snapshot named in the bridge window before retrying.",
    };
}
