using System.ComponentModel;
using HPExcel.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPExcel.Mcp.Server.Tools;

/// <summary>Read-only snapshot of the attached Excel session so the AI can inspect workbooks and sheets before writing code.</summary>
[McpServerToolType]
public sealed class ExcelContextTool(ContextService service)
{
    [McpServerTool(
        Name = ExcelHostProfile.ContextToolName,
        Title = "Get Excel context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Returns the Microsoft Excel session the bridge app is attached to: hostVersion (2026), docTitle and docPath of active workbook, " +
        "isModifiable (workbook is open, Excel is idle — no modal dialog active), executionEnabled, destructiveOperationsEnabled, " +
        "active sheet name, used range, selected cells/range, worksheet count, and open workbook count. " +
        "Call this before execute_excel_code to understand the workbook layout and active sheet.")]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include the currently selected range or cells in Excel.")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
