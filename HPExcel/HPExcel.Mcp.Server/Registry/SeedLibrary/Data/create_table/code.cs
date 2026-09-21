string rangeAddr = args.Require("range");
string tableName = args.Require("tableName");
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
bool hasHeaders = args.Bool("hasHeaders", true);
string tableStyle = args.Str("tableStyle", "TableStyleMedium2");
bool showTotals = args.Bool("showTotalsRow", false);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var ws = string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName];
var targetRange = ws.Range[rangeAddr];

int xlSrcRange = 1;
int xlHeaders = hasHeaders ? 1 : 2; // xlYes (1) vs xlNo (2)

var tbl = ws.ListObjects.Add(xlSrcRange, targetRange, Type.Missing, xlHeaders, Type.Missing);
tbl.Name = tableName;
tbl.TableStyle = tableStyle;
tbl.ShowTotals = showTotals;

string finalAddr = (string)tbl.Range.Address;
log($"Created table '{tableName}' at {ws.Name}!{finalAddr}");
return new
{
    success = true,
    tableName,
    sheet = (string)ws.Name,
    range = finalAddr,
    tableStyle,
    showTotalsRow = showTotals,
    summary = $"Created table '{tableName}' on {ws.Name}!{finalAddr}; snapshot saved"
};
