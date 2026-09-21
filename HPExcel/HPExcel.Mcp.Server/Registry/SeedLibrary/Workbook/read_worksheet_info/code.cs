string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var sheets = new List<object>();

var targetSheets = new List<Worksheet>();
if (!string.IsNullOrEmpty(sheetName))
{
    targetSheets.Add((Worksheet)wb.Worksheets[sheetName]);
}
else
{
    foreach (Worksheet s in wb.Worksheets) targetSheets.Add(s);
}

foreach (Worksheet ws in targetSheets)
{
    var tables = new List<string>();
    foreach (ListObject tbl in ws.ListObjects) tables.Add((string)tbl.Name);

    var charts = new List<string>();
    foreach (ChartObject ch in (ChartObjects)ws.ChartObjects()) charts.Add((string)ch.Name);

    var used = ws.UsedRange;
    string usedAddr = used != null ? (string)used.Address : "$A$1";
    int rows = used != null ? (int)used.Rows.Count : 0;
    int cols = used != null ? (int)used.Columns.Count : 0;
    int vis = (int)(XlSheetVisibility)ws.Visible; // -1: xlSheetVisible, 0: xlSheetHidden, 2: xlSheetVeryHidden
    string visStr = vis == -1 ? "Visible" : (vis == 0 ? "Hidden" : "VeryHidden");

    sheets.Add(new
    {
        name = (string)ws.Name,
        index = (int)ws.Index,
        visibility = visStr,
        usedRange = usedAddr,
        rowCount = rows,
        colCount = cols,
        tables,
        charts,
        isProtected = (bool)ws.ProtectContents
    });
}

string activeName = (string)wb.ActiveSheet.Name;
log($"Workbook '{wb.Name}' has {sheets.Count} worksheet(s); active: {activeName}");
return new
{
    workbook = (string)wb.Name,
    activeSheet = activeName,
    sheetCount = sheets.Count,
    sheets,
    summary = $"Workbook contains {sheets.Count} worksheet(s); active sheet: '{activeName}'"
};
