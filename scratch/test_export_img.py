import json, subprocess

exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

csharp_code = """
var view = doc.GetElement(new Autodesk.Revit.DB.ElementId(9922873)) as Autodesk.Revit.DB.View;
var filePath = args.Str("filePath");

view.CropBoxActive = true;
view.CropBoxVisible = false;

// Set CropBox bounds to tightly wrap the building (in feet)
// X: -2m to 52m (-6.5ft to 170ft)
// Y: -2m to 30m (-6.5ft to 98ft)
var cropBox = view.CropBox;
cropBox.Min = new Autodesk.Revit.DB.XYZ(-2000.0 / 304.8, -2000.0 / 304.8, cropBox.Min.Z);
cropBox.Max = new Autodesk.Revit.DB.XYZ(52000.0 / 304.8, 30000.0 / 304.8, cropBox.Max.Z);
view.CropBox = cropBox;

var opts = new Autodesk.Revit.DB.ImageExportOptions
{
    ExportRange = Autodesk.Revit.DB.ExportRange.SetOfViews,
    FilePath = filePath,
    FitDirection = Autodesk.Revit.DB.FitDirectionType.Horizontal,
    HLRandWFViewsFileType = Autodesk.Revit.DB.ImageFileType.PNG,
    ImageResolution = Autodesk.Revit.DB.ImageResolution.DPI_300,
    ZoomType = Autodesk.Revit.DB.ZoomFitType.FitToPage,
    PixelSize = 3840
};
opts.SetViewsAndSheets(new List<Autodesk.Revit.DB.ElementId> { view.Id });

doc.ExportImage(opts);
log($"Exported tight 4K plan view '{view.Name}' successfully to {filePath}");
return $"Exported tight plan view '{view.Name}'";
"""

payload = {
    "code": csharp_code,
    "args": {
        "filePath": r"C:\Users\STR-HP03\.gemini\antigravity\brain\4cb249bf-78d3-4e3f-aa18-e0869d1403cc\mat_bang_dam_tang1_updated_4k"
    },
    "transaction": "auto",
    "dryRun": False,
    "timeoutSeconds": 60
}

with open("scratch/export_args.json", "w", encoding="utf-8") as f:
    json.dump(payload, f)

cmd = ["python", "McpShared/tools/mcp-call.py", exe, "tools/call", "execute_dynamo_code", "@scratch/export_args.json", "--out", "scratch/export_out.json"]
res = subprocess.run(cmd, capture_output=True, text=True, cwd=r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
print("Export run completed. Returncode:", res.returncode)
