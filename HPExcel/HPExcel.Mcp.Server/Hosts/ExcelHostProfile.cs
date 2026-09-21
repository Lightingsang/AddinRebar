using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;

namespace HPExcel.Mcp.Server.Hosts;

/// <summary>
///     The Microsoft Excel profile: pipe <c>hpexcel-mcp-{version}</c> (e.g. 2026), wire prefix <c>excel.</c>,
///     registry root <c>%AppData%\HPExcel\McpServer\</c>, timeout ceiling 600s, and this assembly
///     hosting the Excel tool classes and embedded seed library. The bridge is a standalone desktop application
///     connecting out-of-process to live running Microsoft Excel instances via COM Interop, with hybrid support
///     for direct headless workbook operations via ClosedXML.
/// </summary>
public static class ExcelHostProfile
{
    public const string ExecuteToolName = "execute_excel_code";
    public const string ContextToolName = "get_excel_context";

    public const int Version = 2026;

    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.ExcelHeavyMaxTimeoutSeconds;

    public const string BridgeExecutable = "HPExcel.McpBridge.exe";

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.ExcelHost,
        DisplayName = "Excel",
        ServerName = "HPExcel MCP",
        ProductFolder = "HPExcel",
        EnvPrefix = "HPEXCEL_MCP_",
        DefaultVersion = Version,
        ValidVersions = new[] { Version },
        MethodPrefix = JsonRpcMethods.ExcelPrefix,
        ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = PipeNaming.ExcelHost,
        Categories = new[] { "Data", "Workbook", "Format", "Chart", "Calculation", "Export", "Automation", "Generic" },
        CoreToolNames = new[] { ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.ExcelImports,
        ScriptContractSummary =
            "Globals: excel (Microsoft.Office.Interop.Excel.Application), workbook (Workbook), sheet (Worksheet), ct, log(string), progress(cur,total,msg), args. " +
            "ClosedXML is available for headless .xlsx manipulation (ClosedXML.Excel.XLWorkbook). " +
            "Three operation tiers: R read-only (read_range, read_worksheet_info, find_cells, read_table, evaluate_formula; transaction: none), " +
            "W write (write_range, format_range, create_table, create_chart, export_worksheet, sheet add/rename; transaction: auto; automatic .xlsx snapshot saved before writing), " +
            "D destructive (delete worksheet, clear contents, run_macro; requires 'Allow destructive operations' toggle in bridge UI; automatic snapshot saved before execution; timeout up to 600s). " +
            "dryRun on writing script gives a static preview without execution. Never call Application.Quit, Process.Start, or modal blocking dialogs; the guard denies them.",
        MaxTimeoutSeconds = HeavyMaxTimeoutSeconds,
        HostAssembly = typeof(ExcelHostProfile).Assembly,
        CliExecutable = "HPExcel.Mcp.Server.exe",
        BridgeNotConnectedHint =
            $"Start {BridgeExecutable} beside Microsoft Excel, click Attach or open workbook and tick 'Allow AI code execution' (pipe {PipeNaming.For(PipeNaming.ExcelHost, Version)}).",
        TimeoutSemanticsHint =
            "Excel may still be executing long-running operations or waiting on modal dialogs; changes made before timeout may have persisted — check the snapshot named in the bridge window before retrying.",
    };
}
