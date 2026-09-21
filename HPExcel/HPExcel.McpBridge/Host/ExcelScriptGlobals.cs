using System;
using System.Threading;
using HPExcel.McpBridge.Headless;
using HPRebar.McpBridge.Core.Scripting;

namespace HPExcel.McpBridge.Host;

/// <summary>
///     Globals exposed to C# Roslyn scripts executed against Microsoft Excel.
///     Contracts: excel, workbook, sheet, ct, log, progress, args (and closedXml helper).
/// </summary>
public sealed class ExcelScriptGlobals
{
    /// <summary>Active Microsoft.Office.Interop.Excel.Application COM root or null if disconnected.</summary>
    public dynamic? excel { get; set; }

    /// <summary>Active Microsoft.Office.Interop.Excel.Workbook or null.</summary>
    public dynamic? workbook { get; set; }

    /// <summary>Active Microsoft.Office.Interop.Excel.Worksheet or null.</summary>
    public dynamic? sheet { get; set; }

    /// <summary>Headless ClosedXML workbook service for direct .xlsx operations.</summary>
    public ClosedXmlWorkbookService? closedXml { get; set; }

    /// <summary>Cancellation token for the execution.</summary>
    public CancellationToken ct { get; set; }

    /// <summary>Script log function.</summary>
    public Action<string> log { get; set; } = _ => { };

    /// <summary>Script progress reporter (current, total, message).</summary>
    public Action<int, int?, string?> progress { get; set; } = (_, _, _) => { };

    /// <summary>Parameters passed to the script.</summary>
    public ScriptArgs args { get; set; } = ScriptArgs.Empty;
}
