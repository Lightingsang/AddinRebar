string format = args.Require("format").ToLowerInvariant();
string outputPath = args.Str("outputPath", null);
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
bool landscape = args.Bool("landscape", false);
bool fitToPage = args.Bool("fitToPage", true);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
string wbPath = (string)wb.Path;
string defaultWbName = (string)wb.Name;
if (string.IsNullOrEmpty(outputPath))
{
    string folder = string.IsNullOrEmpty(wbPath) ? System.IO.Path.GetTempPath() : wbPath;
    string baseName = System.IO.Path.GetFileNameWithoutExtension(defaultWbName);
    string ext = format == "pdf" ? ".pdf" : ".csv";
    string tag = !string.IsNullOrEmpty(sheetName) ? $"_{sheetName}" : "";
    outputPath = System.IO.Path.Combine(folder, $"{baseName}{tag}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
}

var targetObj = !string.IsNullOrEmpty(sheetName) ? wb.Worksheets[sheetName] : wb;

if (format == "pdf")
{
    if (!string.IsNullOrEmpty(sheetName))
    {
        var ws = wb.Worksheets[sheetName];
        if (landscape) ws.PageSetup.Orientation = 2; // xlLandscape
        if (fitToPage)
        {
            ws.PageSetup.Zoom = false;
            ws.PageSetup.FitToPagesWide = 1;
            ws.PageSetup.FitToPagesTall = false;
        }
    }
    targetObj.ExportAsFixedFormat(0, outputPath, 0, true, false, Type.Missing, Type.Missing, false, Type.Missing); // xlTypePDF (0), xlQualityStandard (0)
}
else if (format == "csv")
{
    if (string.IsNullOrEmpty(sheetName)) sheetName = (string)wb.ActiveSheet.Name;
    var ws = wb.Worksheets[sheetName];
    ws.Copy();
    var tempWb = excel.ActiveWorkbook;
    excel.DisplayAlerts = false;
    try
    {
        tempWb.SaveAs(outputPath, 6); // xlCSV (6)
    }
    finally
    {
        tempWb.Close(false);
        excel.DisplayAlerts = true;
    }
}

log($"Exported to {format.ToUpperInvariant()} at {outputPath}");
return new
{
    success = true,
    format,
    filePath = outputPath,
    sheet = sheetName,
    summary = $"Exported to {format.ToUpperInvariant()} at {outputPath}; snapshot saved"
};
