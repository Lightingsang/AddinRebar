string action = args.Require("action").ToLowerInvariant();
string sheetName = args.Require("sheet");
string newName = args.Str("newSheetName", null);
string wbName = args.Str("workbook", null);
string vis = args.Str("visibility", "visible");

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];

Worksheet targetSheet = null;
if (action != "add")
{
    targetSheet = wb.Worksheets[sheetName];
}

string resultMsg = "";
if (action == "add")
{
    var ws = wb.Worksheets.Add(Type.Missing, wb.Worksheets[wb.Worksheets.Count]);
    if (!string.IsNullOrEmpty(newName)) ws.Name = newName;
    resultMsg = $"Added new worksheet '{ws.Name}'";
}
else if (action == "rename")
{
    if (string.IsNullOrEmpty(newName)) throw new ArgumentException("newSheetName is required for rename action.");
    string old = (string)targetSheet.Name;
    targetSheet.Name = newName;
    resultMsg = $"Renamed worksheet '{old}' to '{newName}'";
}
else if (action == "duplicate")
{
    targetSheet.Copy(Type.Missing, targetSheet);
    var copy = wb.ActiveSheet;
    if (!string.IsNullOrEmpty(newName)) copy.Name = newName;
    resultMsg = $"Duplicated worksheet '{sheetName}' as '{copy.Name}'";
}
else if (action == "delete")
{
    excel.DisplayAlerts = false;
    try { targetSheet.Delete(); }
    finally { excel.DisplayAlerts = true; }
    resultMsg = $"Deleted worksheet '{sheetName}'";
}
else if (action == "hide")
{
    targetSheet.Visible = vis == "very_hidden" ? XlSheetVisibility.xlSheetVeryHidden : XlSheetVisibility.xlSheetHidden;
    resultMsg = $"Set worksheet '{sheetName}' visibility to {vis}";
}
else if (action == "unhide")
{
    targetSheet.Visible = XlSheetVisibility.xlSheetVisible;
    resultMsg = $"Unhid worksheet '{sheetName}'";
}

log(resultMsg);
return new
{
    success = true,
    action,
    sheet = sheetName,
    newSheetName = newName,
    summary = $"{resultMsg}; snapshot saved"
};
