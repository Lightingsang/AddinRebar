string rangeAddr = args.Str("range", null);
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
string outputFormat = args.Str("outputFormat", "values");
bool hasHeaders = args.Bool("hasHeaders", false);
int maxRows = args.Int("maxRows", 1000);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var ws = string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName];
var targetRange = string.IsNullOrEmpty(rangeAddr) ? ws.UsedRange : ws.Range[rangeAddr];

int rowCount = Math.Min((int)targetRange.Rows.Count, maxRows);
int colCount = (int)targetRange.Columns.Count;
string actualAddr = (string)targetRange.Address;

object[,] rawValues = null;
if (rowCount == 1 && colCount == 1)
{
    rawValues = new object[2, 2];
    rawValues[1, 1] = outputFormat == "formulas" ? targetRange.Formula : (outputFormat == "formatted" ? targetRange.Text : targetRange.Value2);
}
else
{
    var slice = targetRange.Resize[rowCount, colCount];
    rawValues = outputFormat == "formulas" ? (object[,])slice.Formula : (outputFormat == "formatted" ? (object[,])slice.Text : (object[,])slice.Value2);
}

var headers = new List<string>();
var rows = new List<object>();

if (hasHeaders && rowCount > 1)
{
    for (int c = 1; c <= colCount; c++)
    {
        headers.Add(rawValues[1, c]?.ToString() ?? $"Col_{c}");
    }
    for (int r = 2; r <= rowCount; r++)
    {
        var rowObj = new Dictionary<string, object>();
        for (int c = 1; c <= colCount; c++)
        {
            rowObj[headers[c - 1]] = rawValues[r, c];
        }
        rows.Add(rowObj);
    }
}
else
{
    for (int r = 1; r <= rowCount; r++)
    {
        var rowList = new List<object>();
        for (int c = 1; c <= colCount; c++)
        {
            rowList.Add(rawValues[r, c]);
        }
        rows.Add(rowList);
    }
}

log($"Read {rowCount}x{colCount} from {ws.Name}!{actualAddr}");
return new
{
    sheet = (string)ws.Name,
    address = actualAddr,
    rowCount,
    colCount,
    hasHeaders,
    headers = hasHeaders ? headers : null,
    data = rows,
    summary = $"Read {rowCount} rows x {colCount} cols from {ws.Name}!{actualAddr}"
};
