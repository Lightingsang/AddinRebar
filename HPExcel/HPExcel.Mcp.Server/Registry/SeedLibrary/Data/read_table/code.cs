string tableName = args.Require("tableName");
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
int maxRows = args.Int("maxRows", 1000);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
ListObject targetTable = null;
Worksheet targetSheet = null;

if (!string.IsNullOrEmpty(sheetName))
{
    targetSheet = wb.Worksheets[sheetName];
    try { targetTable = targetSheet.ListObjects[tableName]; } catch { }
}
else
{
    foreach (var ws in wb.Worksheets)
    {
        try
        {
            targetTable = ws.ListObjects[tableName];
            targetSheet = ws;
            break;
        }
        catch { }
    }
}

if (targetTable == null)
{
    throw new ArgumentException($"Table '{tableName}' not found in workbook.");
}

var columns = new List<string>();
foreach (ListColumn col in targetTable.ListColumns)
{
    columns.Add((string)col.Name);
}

var body = targetTable.DataBodyRange;
var rows = new List<Dictionary<string, object>>();
int rowCount = 0;

if (body != null)
{
    rowCount = Math.Min((int)body.Rows.Count, maxRows);
    int colCount = columns.Count;
    object[,] vals = rowCount == 1 && colCount == 1
        ? new object[,] { { null, null }, { null, body.Value2 } }
        : (object[,])body.Resize[rowCount, colCount].Value2;

    for (int r = 1; r <= rowCount; r++)
    {
        var row = new Dictionary<string, object>();
        for (int c = 1; c <= colCount; c++)
        {
            row[columns[c - 1]] = vals[r, c];
        }
        rows.Add(row);
    }
}

var totals = new Dictionary<string, object>();
bool hasTotals = (bool)targetTable.ShowTotals;
if (hasTotals && targetTable.TotalsRowRange != null)
{
    var tRange = targetTable.TotalsRowRange;
    for (int c = 1; c <= columns.Count; c++)
    {
        totals[columns[c - 1]] = ((Microsoft.Office.Interop.Excel.Range)tRange.Cells[1, c]).Value2;
    }
}

log($"Table '{tableName}' read: {rows.Count} row(s), {columns.Count} column(s)");
return new
{
    tableName = (string)targetTable.Name,
    sheet = (string)targetSheet.Name,
    range = (string)targetTable.Range.Address,
    columns,
    rowCount = rows.Count,
    hasTotalsRow = hasTotals,
    totals = hasTotals ? totals : null,
    rows,
    summary = $"Table '{tableName}' on sheet '{targetSheet.Name}': {rows.Count} rows x {columns.Count} columns"
};
