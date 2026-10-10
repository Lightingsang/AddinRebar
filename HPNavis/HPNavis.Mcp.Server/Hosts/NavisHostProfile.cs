using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;

namespace HPNavis.Mcp.Server.Hosts;

/// <summary>
///     The Navisworks profile as this exe registers it: pipe <c>hpnavis-mcp-{version}</c>, wire prefix
///     <c>navis.</c>, registry root <c>%AppData%\HPNavis\McpServer\</c>, a 600 s timeout ceiling (a clash run
///     or a file append cannot be interrupted and may take minutes), and this assembly as the home of the
///     Navisworks tool classes and the embedded seed library. One exe, one host, one pipe.
/// </summary>
public static class NavisHostProfile
{
    public const string ExecuteToolName = "execute_navis_code";
    public const string ContextToolName = "get_navis_context";

    /// <summary>Longest run the bridge accepts once the user allowed heavy operations (shared constant); the bridge clamps to 120 otherwise.</summary>
    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.NavisHeavyMaxTimeoutSeconds;

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.NavisHost,
        DisplayName = "Navisworks",
        ServerName = "HPNavis MCP",
        ProductFolder = "HPNavis",
        EnvPrefix = "HPNAVIS_MCP_",
        DefaultVersion = 2026,
        // Navisworks Manage 2026 (runtime 23.0 on .NET Framework 4.8) is the only verified host.
        ValidVersions = new[] { 2026 },
        MethodPrefix = JsonRpcMethods.NavisPrefix,
        ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = PipeNaming.NavisHost,
        Categories = new[] { "Model", "Search", "Selection", "Viewpoint", "Clash", "Timeliner", "Report", "Coordination", "Data", "Generic" },
        CoreToolNames = new[] { ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.NavisImports,
        ScriptContractSummary =
            "Globals: doc (Document), app (Navisworks version/year, HasClashModule, IsModified), units (units.ToMm(du), units.ToDrawing(mm), units.Label — the API works in the document's units), " +
            "ct, log(string), progress(cur,total,msg), args. Navisworks is a review tool: geometry is read-only. " +
            "Undoable edits (selection sets, saved viewpoints, comments, permanent appearance overrides, hidden/required, clash tests and result status, TimeLiner tasks) go in one Undo entry `MCP: <label>`; " +
            "AppendFile/MergeFile/SaveFile/Export/TestsRunTest are heavy: the user must allow them in the bridge window, they are never undone and cannot be interrupted. " +
            "transaction: auto when the code changes anything, none when it only reads (best-effort fingerprint check); manual runs like auto. " +
            "Never open a Transaction yourself, never call Undo/Redo/Rollback, no dialogs, no Document.Database SQL.",
        MaxTimeoutSeconds = HeavyMaxTimeoutSeconds,
        HostAssembly = typeof(NavisHostProfile).Assembly,
        CliExecutable = "HPNavis.Mcp.Server.exe",
    };
}
