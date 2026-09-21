namespace HPExcel.McpBridge.Safety;

/// <summary>
///     3-Tier safety classification for Excel operations:
///     - ReadOnly: safe inspection (read_range, find_cells, get_excel_context)
///     - Write: mutating cells, styles, tables, charts (write_range, format_range, create_table)
///     - Destructive: deleting sheets, bulk clear, executing macros (manage_worksheet delete, run_macro)
/// </summary>
public enum ExcelTier
{
    ReadOnly = 0,
    Write = 1,
    Destructive = 2
}
