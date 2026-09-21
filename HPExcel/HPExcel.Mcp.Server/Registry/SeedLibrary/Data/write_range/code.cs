string startCell = args.Require("startCell");
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
bool autoFit = args.Bool("autoFitColumns", false);
bool isFormula = args.Bool("isFormula", false);

var rowsList = args.List("values");
if (rowsList.Count == 0)
{
    throw new ArgumentException("Parameter 'values' must be a non-empty 2D array.");
}

int numRows = rowsList.Count;
int numCols = 0;
foreach (var r in rowsList)
{
    if (r.IsArray && r.Raw.HasValue)
    {
        int c = r.Raw.Value.GetArrayLength();
        if (c > numCols) numCols = c;
    }
}
if (numCols == 0) throw new ArgumentException("Parameter 'values' must contain at least one row with columns.");

object[,] data = new object[numRows, numCols];
for (int r = 0; r < numRows; r++)
{
    if (rowsList[r].IsArray && rowsList[r].Raw.HasValue)
    {
        var cols = rowsList[r].Raw.Value.EnumerateArray().ToList();
        for (int c = 0; c < numCols; c++)
        {
            if (c < cols.Count)
            {
                var val = cols[c];
                data[r, c] = val.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.Number => val.TryGetInt64(out var l) ? (object)l : val.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => val.GetString()
                };
            }
            else data[r, c] = null;
        }
    }
}

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var ws = string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName];
var origin = ws.Range[startCell];
var target = origin.Resize[numRows, numCols];

if (isFormula) target.Formula = data;
else target.Value2 = data;

if (autoFit) target.Columns.AutoFit();

string targetAddr = (string)target.Address;
log($"Wrote {numRows} rows x {numCols} cols to {ws.Name}!{targetAddr}");
return new
{
    success = true,
    sheet = (string)ws.Name,
    targetRange = targetAddr,
    rowsWritten = numRows,
    colsWritten = numCols,
    summary = $"Wrote {numRows} rows x {numCols} cols to {ws.Name}!{targetAddr}; snapshot saved"
};
