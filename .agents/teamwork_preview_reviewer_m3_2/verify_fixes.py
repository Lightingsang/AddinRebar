import os
import subprocess
import tempfile

csc = r"C:\Program Files\dotnet\sdk\10.0.400\Roslyn\bincore\csc.dll"
net48_dir = r"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
tekla_dir = r"C:\Program Files\Tekla Structures\2025.0\bin"
mcp_core = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core\bin\Release\net48\HPRebar.McpBridge.Core.dll"
mcp_contracts = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Contracts\bin\Release\netstandard2.0\HPRebar.Mcp.Contracts.dll"

ref_files = [
    os.path.join(net48_dir, "mscorlib.dll"),
    os.path.join(net48_dir, "System.dll"),
    os.path.join(net48_dir, "System.Core.dll"),
    os.path.join(net48_dir, "Microsoft.CSharp.dll"),
    os.path.join(tekla_dir, "Tekla.Structures.dll"),
    os.path.join(tekla_dir, "Tekla.Structures.Model.dll"),
    os.path.join(tekla_dir, "Tekla.Structures.Drawing.dll"),
    os.path.join(tekla_dir, "Tekla.Structures.Catalogs.dll"),
    os.path.join(tekla_dir, "Tekla.Structures.Datatype.dll"),
    mcp_core,
    mcp_contracts
]

test_cases = {
    "get_model_info_fixed": """
bool includeProj = args.Bool("includeProjectInfo", true);
bool includePhase = args.Bool("includePhaseInfo", true);

var info = model.GetInfo();
string modelName = info.ModelName ?? "";
string modelPath = info.ModelPath ?? "";
bool isConnected = model.GetConnectionStatus();

object projData = null;
if (includeProj)
{
    var proj = model.GetProjectInfo();
    projData = new
    {
        projectName = proj.Name,
        projectNumber = proj.ProjectNumber,
        designer = proj.Designer,
        builder = proj.Builder
    };
}

int currentPhase = 0;
if (includePhase)
{
    currentPhase = info.CurrentPhase;
}

log($"Tekla Model: {modelName} | Connected: {isConnected} | Phase: {currentPhase}");

return new
{
    success = true,
    isConnected = isConnected,
    modelName = modelName,
    modelPath = modelPath,
    currentPhase = currentPhase,
    project = projData,
    summary = $"Model '{modelName}' connected, current phase {currentPhase}."
};
""",
    "select_objects_fixed": """
string filter = args.Str("typeFilter", "ALL").ToUpperInvariant();
int limit = Math.Max(1, Math.Min(200, args.Int("limit", 50)));
bool setSel = args.Bool("setSelection", false);

var modelSelector = model.GetModelObjectSelector();
ModelObjectEnumerator enumerator;
if (filter == "BEAM" || filter == "COLUMN")
    enumerator = modelSelector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.BEAM);
else if (filter == "CONTOURPLATE")
    enumerator = modelSelector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.CONTOURPLATE);
else if (filter == "REBAR")
    enumerator = modelSelector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.REBARGROUP);
else
    enumerator = modelSelector.GetAllObjects();

var list = new List<object>();
var selectedObjects = new System.Collections.ArrayList();

while (enumerator.MoveNext() && list.Count < limit)
{
    if (enumerator.Current is ModelObject mo)
    {
        string typeStr = mo.GetType().Name;
        string name = "";
        string profile = "";
        if (mo is Part part)
        {
            name = part.Name ?? "";
            profile = part.Profile?.ProfileString ?? "";
        }

        list.Add(new
        {
            id = mo.Identifier.ID,
            guid = mo.Identifier.GUID.ToString(),
            type = typeStr,
            name = name,
            profile = profile
        });

        if (setSel) selectedObjects.Add(mo);
    }
}

if (setSel && selectedObjects.Count > 0)
{
    selector.Select(selectedObjects);
}

log($"Found {list.Count} objects matching filter '{filter}' (limit {limit})");

return new
{
    success = true,
    count = list.Count,
    filter = filter,
    objects = list
};
""",
    "get_reinforcement_info_fixed": """
int fatherId = args.Int("fatherId", 0);
int limit = Math.Max(1, Math.Min(200, args.Int("limit", 50)));

var modelSelector = model.GetModelObjectSelector();
ModelObjectEnumerator enumerator = modelSelector.GetAllObjects();
var list = new List<object>();

while (enumerator.MoveNext() && list.Count < limit)
{
    if (enumerator.Current is Reinforcement rebar)
    {
        if (fatherId > 0 && rebar.Father?.Identifier.ID != fatherId) continue;

        double length = 0.0, weight = 0.0;
        rebar.GetReportProperty("LENGTH", ref length);
        rebar.GetReportProperty("WEIGHT", ref weight);

        string size = "";
        int quantity = 1;
        if (rebar is SingleRebar sr)
        {
            size = sr.Size;
            quantity = 1;
        }
        else if (rebar is BaseRebarGroup group)
        {
            size = group.Size;
            if (group is RebarGroup rg)
            {
                quantity = rg.Polygons.Count > 0 ? (int)Math.Max(1, rg.GetNumberOfRebars()) : 1;
            }
        }

        list.Add(new
        {
            id = rebar.Identifier.ID,
            fatherId = rebar.Father?.Identifier.ID,
            name = rebar.Name,
            size = size,
            grade = rebar.Grade,
            quantity = quantity,
            lengthMm = length,
            weightKg = weight
        });
    }
}

log($"Found {list.Count} reinforcement items (fatherId: {fatherId}, limit: {limit})");

return new
{
    success = true,
    count = list.Count,
    rebars = list
};
""",
    "list_drawings_fixed": """
string filter = args.Str("drawingType", "ALL").ToUpperInvariant();
int limit = Math.Max(1, Math.Min(200, args.Int("limit", 50)));

var dh = new Tekla.Structures.Drawing.DrawingHandler();
var drawings = dh.GetDrawings();
var list = new List<object>();

if (drawings != null)
{
    while (drawings.MoveNext() && list.Count < limit)
    {
        var dwg = drawings.Current;
        if (dwg == null) continue;
        string dwgTypeName = dwg.GetType().Name.Replace("Drawing", "").ToUpperInvariant();

        if (filter != "ALL" && !dwgTypeName.Contains(filter)) continue;

        list.Add(new
        {
            name = dwg.Name,
            title1 = dwg.Title1,
            title2 = dwg.Title2,
            type = dwgTypeName,
            mark = dwg.Mark,
            upToDate = dwg.UpToDateStatus.ToString()
        });
    }
}

log($"Found {list.Count} drawings (type filter: {filter})");

return new
{
    success = true,
    count = list.Count,
    filter = filter,
    drawings = list
};
""",
    "export_ifc_fixed": """
string path = args.Str("outputFilePath");
if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("outputFilePath is required.");
string format = args.Str("format", "IFC4").ToUpperInvariant();
bool selectedOnly = args.Bool("exportSelectedOnly", false);

if (!path.EndsWith(".ifc", StringComparison.OrdinalIgnoreCase))
{
    path += ".ifc";
}

log($"Exporting Tekla model to {path} (Format: {format}, SelectedOnly: {selectedOnly})...");

var exportView = format == "IFC2X3"
    ? Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.REFERENCE_VIEW
    : Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.DESIGN_TRANSFER_VIEW;

bool ok = Tekla.Structures.Model.Operations.Operation.CreateIFC4ExportFromSelected(
    path,
    exportView,
    new List<string>(),
    Tekla.Structures.Model.Operations.Operation.ExportBasePoint.WORK_PLANE,
    "",
    "",
    new Tekla.Structures.Model.Operations.Operation.IFCExportFlags(),
    ""
);

if (!ok) throw new InvalidOperationException($"Tekla IFC export failed for output path: {path}");

log($"IFC Export successfully created: {path}");

return new
{
    success = true,
    filePath = path,
    format = format,
    selectedOnly = selectedOnly,
    summary = $"Exported IFC file to {path}"
};
"""
}

wrapper_template = """using System;
using System.Linq;
using System.Collections.Generic;
using Tekla.Structures;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Catalogs;
using HPRebar.McpBridge.Core.Scripting;

public class SeedHost
{{
    public Tekla.Structures.Model.Model model;
    public Tekla.Structures.Model.UI.ModelObjectSelector selector;
    public System.Threading.CancellationToken ct;
    public Action<string> log;
    public Action<int, int?, string?> progress;
    public ScriptArgs args;

    public object Run()
    {{
#line 1 "code.cs"
{code}
    }}
}}
"""

with tempfile.TemporaryDirectory() as tmpdir:
    for name, code in test_cases.items():
        src_path = os.path.join(tmpdir, f"{name}.cs")
        out_dll = os.path.join(tmpdir, f"{name}.dll")
        with open(src_path, "w", encoding="utf-8") as f:
            f.write(wrapper_template.format(code=code))
        args = ["dotnet", csc, "-target:library", f"-out:{out_dll}", "-nologo", "-langversion:latest", "-nowarn:CS0168,CS0219"]
        for r in ref_files:
            args.append(f"-r:{r}")
        args.append(src_path)
        res = subprocess.run(args, capture_output=True, text=True)
        if res.returncode == 0:
            print(f"[FIX VERIFIED OK] {name}")
        else:
            print(f"[FIX FAILED] {name}")
            print(res.stdout)
            print(res.stderr)
