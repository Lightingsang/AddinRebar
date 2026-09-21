using System.ComponentModel;
using System.Text.Json;
using HPExcel.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPExcel.Mcp.Server.Tools;

/// <summary>
///     The tool that makes Excel a runtime for the AI: any C# the model writes runs against
///     the Microsoft Excel instance or workbook attached to the HPExcel MCP Bridge.
/// </summary>
[McpServerToolType]
public sealed class ExecuteExcelCodeTool(ExecuteCodeService service)
{
    public const string ToolDescription =
        "Runs a C# script against the Microsoft Excel session attached to the HPExcel MCP Bridge. " +
        "Globals: excel (Excel.Application), workbook (active Workbook), sheet (active Worksheet), ct, log(string), progress(cur,total,msg), args (args.Str/Int/Double/Bool(key, fallback)). " +
        "ClosedXML.Excel is available for headless .xlsx manipulation. End with `return <value>;`. " +
        "Three tiers: R read-only (read_range, read_worksheet_info, find_cells, read_table, evaluate_formula; transaction=none). " +
        "W write (write_range, format_range, create_table, create_chart, export_worksheet; transaction=auto; automatic .xlsx snapshot saved before writing). " +
        "D destructive (delete worksheet, clear contents, run_macro; requires 'Allow destructive operations' toggle in bridge UI; automatic snapshot saved before execution; timeout up to 600s). " +
        "dryRun on a writing script provides a static preview without execution. Never call Application.Quit, Process.Start, or modal dialogs (blocked by guard). " +
        "Requires 'Allow AI code execution' enabled in the HPExcel MCP Bridge desktop window.";

    [McpServerTool(
        Name = ExcelHostProfile.ExecuteToolName,
        Title = "Execute C# in Excel",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> ExecuteAsync(
        [Description("C# script body, max 32 KB. No `await`, no System.IO / System.Net / reflection / interop, no Quit/dialog calls (blocked by guard).")]
        string code,
        [Description("auto (default): writing script — takes an automatic .xlsx backup snapshot before running. none: read-only script. manual: accepted for compatibility, behaves like auto.")]
        string transaction = TransactionModes.Auto,
        [Description("Static preview for a writing script; a read-only script runs normally. Refused for destructive members.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–120 (up to 600 while destructive operations are allowed).")]
        int timeoutSeconds = 30,
        [Description("Short name for the audit log and snapshot file name. Max 64 characters; letters, digits, _ and - survive, the rest becomes _.")]
        string? label = null,
        [Description("Optional JSON object handed to the script as `args` (e.g. {\"range\": \"A1:D10\", \"sheet\": \"Sheet1\"}). Keys are matched case-insensitively.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return service.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, cancellationToken);
    }
}
