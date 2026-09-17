"""Generates the 12 ETABS seed tools (tool.json + code.cs + examples.json) under HPEtabs.Mcp.Server/Registry/SeedLibrary/.

The OAPI signatures the scripts call were read by reflection from the installed ETABSv1.dll (see tool.json.notes); the
scripts follow the seed contract in plans/260916-2152-etabs-mcp-2026/phase-03-*.md. Run it after editing a seed body here,
then `dotnet test HPEtabs.Mcp.Server.Tests` (structure + compile/tier checks) and the live harness `-Phase seeds`.
"""
import io, os, json
root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "HPEtabs.Mcp.Server", "Registry", "SeedLibrary")

def seed(category, name, tool, code, examples):
    d = os.path.join(root, category, name)
    os.makedirs(d, exist_ok=True)
    base = {"name": name, "version": 1, "status": "published", "author": "hprebar", "category": category,
            "host": "etabs", "hostVersions": ["22"]}
    base.update(tool)
    io.open(os.path.join(d, "tool.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(base, indent=2, ensure_ascii=False) + "\n")
    io.open(os.path.join(d, "code.cs"), "w", encoding="utf-8", newline="\n").write(code.strip("\n") + "\n")
    io.open(os.path.join(d, "examples.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(examples, indent=2, ensure_ascii=False) + "\n")
    print("ok", category, name, len(code.strip("\n").split("\n")), "lines")

LIMIT = {"type": "integer", "default": 200, "minimum": 1, "maximum": 500, "description": "Maximum number of items to return (1–500)"}
OFFSET = {"type": "integer", "default": 0, "minimum": 0, "description": "Skip this many items first (paging)"}

# ---------------------------------------------------------------------------------------------------------------------- R1
seed("Model", "get_model_info", {
    "transaction": "none", "timeoutSeconds": 30, "destructive": False,
    "tags": ["model", "query", "units"],
    "title": "Get model info",
    "description": "Overview of the ETABS model the bridge is attached to: file name and folder, lock state, present and database units, ETABS version, object counts (points, frames, areas) and optionally the stories. Call it first: every script works in kN and mm whatever the model's units. Read-only.",
    "notes": "OAPI: cSapModel.GetModelFilename/GetModelFilepath/GetModelIsLocked/GetPresentUnits/GetDatabaseUnits/GetVersion, cPointObj/cFrameObj/cAreaObj.GetNameList, cStory.GetStories_2 (documentation topics of the same names). After a save GetModelFilename reports the .$et working copy; the seed reports the .EDB.",
    "inputSchema": {"type": "object", "properties": {
        "includeStories": {"type": "boolean", "default": False, "description": "Also list the stories with elevation and height in mm"}},
        "additionalProperties": False}},
r'''
bool includeStories = args.Bool("includeStories", false);
string workingFile = sapModel.GetModelFilename(true);
bool hasModel = !string.IsNullOrWhiteSpace(workingFile) && workingFile.Contains("\\");
// After any save ETABS names its working copy `.$et` beside the `.EDB`; the model the user (and the snapshots) know is the `.EDB`.
string file = hasModel && workingFile.EndsWith(".$et", StringComparison.OrdinalIgnoreCase) ? workingFile.Substring(0, workingFile.Length - 4) + ".EDB" : workingFile;
string version = ""; double versionNumber = 0;
int retV = sapModel.GetVersion(ref version, ref versionNumber);
if (retV != 0) throw new InvalidOperationException($"ETABS returned {retV} from GetVersion");

int np = 0, nf = 0, na = 0; string[] ln = null;
int rp = sapModel.PointObj.GetNameList(ref np, ref ln);
if (rp != 0) throw new InvalidOperationException($"ETABS returned {rp} from PointObj.GetNameList");
int rf = sapModel.FrameObj.GetNameList(ref nf, ref ln);
if (rf != 0) throw new InvalidOperationException($"ETABS returned {rf} from FrameObj.GetNameList");
int ra = sapModel.AreaObj.GetNameList(ref na, ref ln);
if (ra != 0) throw new InvalidOperationException($"ETABS returned {ra} from AreaObj.GetNameList");
int points = np, frames = nf, areas = na;

object stories = null;
if (includeStories)
{
    double baseElevation = 0; int ns = 0; string[] names = null; double[] elevations = null, heights = null, spliceHeights = null;
    bool[] isMaster = null, splice = null; string[] similar = null; int[] color = null;
    int ret = sapModel.Story.GetStories_2(ref baseElevation, ref ns, ref names, ref elevations, ref heights, ref isMaster, ref similar, ref splice, ref spliceHeights, ref color);
    if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Story.GetStories_2");
    stories = Enumerable.Range(0, ns).Select(i => new { name = names[i], elevationMm = elevations[i], heightMm = heights[i], isMaster = isMaster[i], similarTo = similar[i] }).ToList();
}

log($"{(hasModel ? file : "(no model file)")}: {points} points, {frames} frames, {areas} areas");
return new
{
    success = true,
    hasModelFile = hasModel,
    modelName = hasModel ? file.Substring(file.LastIndexOf('\\') + 1) : null,
    modelFolder = hasModel ? sapModel.GetModelFilepath() : null,
    workingFile = hasModel && !string.Equals(workingFile, file, StringComparison.OrdinalIgnoreCase) ? workingFile.Substring(workingFile.LastIndexOf('\\') + 1) : null,
    isLocked = sapModel.GetModelIsLocked(),
    presentUnits = sapModel.GetPresentUnits().ToString(),
    databaseUnits = sapModel.GetDatabaseUnits().ToString(),
    scriptUnits = units.Label,
    etabsVersion = version,
    counts = new { points, frames, areas },
    stories,
    summary = $"{points} points, {frames} frames, {areas} areas; units {units.Label} in scripts",
};
''',
[{"title": "Overview", "args": {}}, {"title": "With stories", "args": {"includeStories": True}}])

# ---------------------------------------------------------------------------------------------------------------------- R2
seed("Geometry", "get_stories_and_grids", {
    "transaction": "none", "timeoutSeconds": 30, "destructive": False,
    "tags": ["story", "grid", "query"],
    "title": "Get stories and grid systems",
    "description": "Lists the stories (name, elevation and height in mm, master/similar flags, splice) from the base up and the names of the grid systems. Read-only.",
    "notes": "OAPI: cStory.GetStories_2 (10 ref parameters, BaseElevation first), cGridSys.GetNameList.",
    "inputSchema": {"type": "object", "properties": {"limit": LIMIT}, "additionalProperties": False}},
r'''
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);

double baseElevation = 0; int ns = 0; string[] names = null; double[] elevations = null, heights = null, spliceHeights = null;
bool[] isMaster = null, splice = null; string[] similar = null; int[] color = null;
int ret = sapModel.Story.GetStories_2(ref baseElevation, ref ns, ref names, ref elevations, ref heights, ref isMaster, ref similar, ref splice, ref spliceHeights, ref color);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Story.GetStories_2");

var stories = Enumerable.Range(0, Math.Min(ns, limit))
    .Select(i => new { name = names[i], elevationMm = elevations[i], heightMm = heights[i], isMaster = isMaster[i], similarTo = similar[i], hasSplice = splice[i], spliceHeightMm = spliceHeights[i] })
    .ToList();

int ng = 0; string[] gridNames = null;
int retG = sapModel.GridSys.GetNameList(ref ng, ref gridNames);
if (retG != 0) throw new InvalidOperationException($"ETABS returned {retG} from GridSys.GetNameList");

log($"{ns} stories (base elevation {baseElevation} mm), {ng} grid systems");
return new
{
    success = true,
    baseElevationMm = baseElevation,
    stories,
    storyCount = ns,
    truncated = ns > limit,
    gridSystems = (gridNames ?? new string[0]).ToList(),
    summary = $"{ns} stories, {ng} grid systems",
};
''',
[{"title": "All stories", "args": {}}, {"title": "First 5 stories", "args": {"limit": 5}}])

# ---------------------------------------------------------------------------------------------------------------------- R3
seed("Geometry", "get_structural_objects", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["frame", "area", "point", "geometry", "query"],
    "title": "Get structural objects",
    "description": "Pages through the frame, area or point objects: unique name, label and story, the frame section (areas: none — the API's section member is blocked), end points or vertices in mm and the frame length in mm. Filter by story or by a substring of the name. Read-only.",
    "notes": "OAPI: cFrameObj.GetNameList/GetLabelFromName/GetSection/GetPoints, cAreaObj.GetNameList/GetLabelFromName/GetPoints, cPointObj.GetNameList/GetLabelFromName/GetCoordCartesian. cAreaObj.GetProperty is a base-guard collision (reflection name) and is not used.",
    "inputSchema": {"type": "object", "properties": {
        "kind": {"type": "string", "enum": ["frame", "area", "point"], "default": "frame", "description": "Which objects to list"},
        "story": {"type": "string", "description": "Only objects on this story (exact story name)"},
        "nameLike": {"type": "string", "description": "Only objects whose unique name or label contains this text (case-insensitive)"},
        "limit": LIMIT, "offset": OFFSET},
        "additionalProperties": False}},
r'''
string kind = (args.Str("kind", "frame") ?? "frame").Trim().ToLowerInvariant();
string story = args.Str("story", null);
string nameLike = args.Str("nameLike", null);
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);
int offset = Math.Max(0, args.Int("offset", 0));
if (kind != "frame" && kind != "area" && kind != "point") throw new ArgumentException("kind must be frame, area or point");

int n = 0; string[] names = null;
int ret = kind == "frame" ? sapModel.FrameObj.GetNameList(ref n, ref names) : kind == "area" ? sapModel.AreaObj.GetNameList(ref n, ref names) : sapModel.PointObj.GetNameList(ref n, ref names);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from {kind}.GetNameList");
names = names ?? new string[0];

bool Matches(string name, string label, string objStory) =>
    (story == null || string.Equals(objStory, story, StringComparison.OrdinalIgnoreCase)) &&
    (nameLike == null || name.IndexOf(nameLike, StringComparison.OrdinalIgnoreCase) >= 0 || (label ?? "").IndexOf(nameLike, StringComparison.OrdinalIgnoreCase) >= 0);

(string point, double xMm, double yMm, double zMm) Coords(string point)
{
    double x = 0, y = 0, z = 0;
    int r = sapModel.PointObj.GetCoordCartesian(point, ref x, ref y, ref z);
    if (r != 0) throw new InvalidOperationException($"ETABS returned {r} from PointObj.GetCoordCartesian({point})");
    return (point, x, y, z);
}

var items = new List<object>();
int matched = 0;
var warnings = new List<string>();
foreach (var name in names)
{
    ct.ThrowIfCancellationRequested();
    string label = "", objStory = "";
    int rl = kind == "frame" ? sapModel.FrameObj.GetLabelFromName(name, ref label, ref objStory) : kind == "area" ? sapModel.AreaObj.GetLabelFromName(name, ref label, ref objStory) : sapModel.PointObj.GetLabelFromName(name, ref label, ref objStory);
    if (rl != 0) { label = ""; objStory = ""; }
    if (!Matches(name, label, objStory)) continue;
    matched++;
    if (matched <= offset) continue;
    if (items.Count >= limit) break; // one past the page is enough to know there is more; `matched` is then a lower bound

    if (kind == "frame")
    {
        string p1 = "", p2 = "", section = "", auto = "";
        int rp = sapModel.FrameObj.GetPoints(name, ref p1, ref p2);
        if (rp != 0) { warnings.Add($"{name}: GetPoints returned {rp}"); continue; }
        int rs = sapModel.FrameObj.GetSection(name, ref section, ref auto);
        var a = Coords(p1); var b = Coords(p2);
        double length = Math.Sqrt((b.xMm - a.xMm) * (b.xMm - a.xMm) + (b.yMm - a.yMm) * (b.yMm - a.yMm) + (b.zMm - a.zMm) * (b.zMm - a.zMm));
        items.Add(new { name, label, story = objStory, kind, section = rs == 0 ? section : null, endpointsMm = new[] { a, b }, lengthMm = Math.Round(length, 1) });
    }
    else if (kind == "area")
    {
        int np = 0; string[] pts = null;
        int rp = sapModel.AreaObj.GetPoints(name, ref np, ref pts);
        if (rp != 0) { warnings.Add($"{name}: GetPoints returned {rp}"); continue; }
        items.Add(new { name, label, story = objStory, kind, vertexCount = np, verticesMm = (pts ?? new string[0]).Take(32).Select(pt => Coords(pt)).ToList() });
    }
    else
    {
        items.Add(new { name, label, story = objStory, kind, coordinatesMm = Coords(name) });
    }
}

bool truncated = matched > offset + items.Count;
log($"{kind}: {(truncated ? "≥ " : "")}{matched} matched of {n}, returned {items.Count} from offset {offset}");
return new
{
    success = true,
    kind,
    items,
    count = items.Count,
    offset,
    matched = truncated ? (int?)null : matched,
    matchedAtLeast = matched,
    total = n,
    truncated,
    warnings,
    summary = truncated ? $"{items.Count} {kind}(s) of at least {matched} matched ({n} in the model); more available from offset {offset + items.Count}" : $"{items.Count} {kind}(s) of {matched} matched ({n} in the model)",
};
''',
[{"title": "Frames on story 1", "args": {"kind": "frame", "story": "Story1"}},
 {"title": "Second page of all points", "args": {"kind": "point", "limit": 100, "offset": 100}},
 {"title": "Areas whose label contains F", "args": {"kind": "area", "nameLike": "F"}}])

# ---------------------------------------------------------------------------------------------------------------------- R4
seed("Property", "get_materials_and_sections", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["material", "section", "property", "query"],
    "title": "Get materials and frame sections",
    "description": "Lists the material properties (type, E in MPa, Poisson ratio, thermal coefficient, G in MPa) and the frame section properties (shape type, area in mm², I22/I33 in mm⁴, S/Z in mm³, radii of gyration in mm) defined in the model. Read-only.",
    "notes": "OAPI: cPropMaterial.GetNameList/GetMaterial/GetMPIsotropic, cPropFrame.GetNameList/GetTypeOAPI/GetSectProps. Stresses come back in kN/mm² under the forced units and are reported ×1000 as MPa.",
    "inputSchema": {"type": "object", "properties": {"limit": LIMIT}, "additionalProperties": False}},
r'''
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);
var warnings = new List<string>();

int nm = 0; string[] matNames = null;
int ret = sapModel.PropMaterial.GetNameList(ref nm, ref matNames);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from PropMaterial.GetNameList");
var materials = new List<object>();
foreach (var name in (matNames ?? new string[0]).Take(limit))
{
    ct.ThrowIfCancellationRequested();
    eMatType type = eMatType.Steel; int color = 0; string notes = "", guid = "";
    int rm = sapModel.PropMaterial.GetMaterial(name, ref type, ref color, ref notes, ref guid);
    if (rm != 0) { warnings.Add($"material {name}: GetMaterial returned {rm}"); continue; }
    double e = 0, u = 0, a = 0, g = 0;
    int ri = sapModel.PropMaterial.GetMPIsotropic(name, ref e, ref u, ref a, ref g);
    materials.Add(new { name, type = type.ToString(), eMPa = ri == 0 ? Math.Round(e * 1000, 1) : (double?)null, poisson = ri == 0 ? u : (double?)null, thermalCoefficient = ri == 0 ? a : (double?)null, gMPa = ri == 0 ? Math.Round(g * 1000, 1) : (double?)null, isotropic = ri == 0 });
}

int ns = 0; string[] secNames = null;
int retS = sapModel.PropFrame.GetNameList(ref ns, ref secNames);
if (retS != 0) throw new InvalidOperationException($"ETABS returned {retS} from PropFrame.GetNameList");
var frameSections = new List<object>();
foreach (var name in (secNames ?? new string[0]).Take(limit))
{
    ct.ThrowIfCancellationRequested();
    eFramePropType type = eFramePropType.General;
    int rt = sapModel.PropFrame.GetTypeOAPI(name, ref type);
    double area = 0, as2 = 0, as3 = 0, torsion = 0, i22 = 0, i33 = 0, s22 = 0, s33 = 0, z22 = 0, z33 = 0, r22 = 0, r33 = 0;
    int rp = sapModel.PropFrame.GetSectProps(name, ref area, ref as2, ref as3, ref torsion, ref i22, ref i33, ref s22, ref s33, ref z22, ref z33, ref r22, ref r33);
    if (rp != 0) warnings.Add($"section {name}: GetSectProps returned {rp}");
    frameSections.Add(new { name, type = rt == 0 ? type.ToString() : null, areaMm2 = rp == 0 ? area : (double?)null, i22Mm4 = rp == 0 ? i22 : (double?)null, i33Mm4 = rp == 0 ? i33 : (double?)null, s22Mm3 = rp == 0 ? s22 : (double?)null, s33Mm3 = rp == 0 ? s33 : (double?)null, z22Mm3 = rp == 0 ? z22 : (double?)null, z33Mm3 = rp == 0 ? z33 : (double?)null, r22Mm = rp == 0 ? r22 : (double?)null, r33Mm = rp == 0 ? r33 : (double?)null, torsionMm4 = rp == 0 ? torsion : (double?)null });
}

log($"{nm} materials, {ns} frame sections");
return new
{
    success = true,
    materials,
    materialCount = nm,
    frameSections,
    frameSectionCount = ns,
    truncated = nm > limit || ns > limit,
    warnings,
    summary = $"{materials.Count} of {nm} materials, {frameSections.Count} of {ns} frame sections",
};
''',
[{"title": "Everything (first 200 each)", "args": {}}, {"title": "First 20 of each", "args": {"limit": 20}}])

# ---------------------------------------------------------------------------------------------------------------------- R5
seed("Load", "get_load_definitions", {
    "transaction": "none", "timeoutSeconds": 30, "destructive": False,
    "tags": ["load", "pattern", "case", "combo", "query"],
    "title": "Get load definitions",
    "description": "Lists the load patterns (type, self-weight multiplier), the load cases (type, sub-type) and the load combinations (type and, on request, the cases with their scale factors). Read-only.",
    "notes": "OAPI: cLoadPatterns.GetNameList/GetLoadType/GetSelfWTMultiplier, cLoadCases.GetNameList/GetTypeOAPI, cCombo.GetNameList/GetTypeOAPI/GetCaseList (sapModel.RespCombo is cCombo in the wrapper).",
    "inputSchema": {"type": "object", "properties": {
        "includeComboCases": {"type": "boolean", "default": False, "description": "Also list each combination's cases and scale factors"},
        "limit": LIMIT},
        "additionalProperties": False}},
r'''
bool includeComboCases = args.Bool("includeComboCases", false);
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);
var warnings = new List<string>();
string ComboTypeName(int t) => t == 0 ? "linear-additive" : t == 1 ? "envelope" : t == 2 ? "absolute-additive" : t == 3 ? "srss" : t == 4 ? "range-additive" : t.ToString();

int np = 0; string[] patNames = null;
int ret = sapModel.LoadPatterns.GetNameList(ref np, ref patNames);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from LoadPatterns.GetNameList");
var patterns = new List<object>();
foreach (var name in (patNames ?? new string[0]).Take(limit))
{
    ct.ThrowIfCancellationRequested();
    eLoadPatternType type = eLoadPatternType.Dead; double swm = 0;
    int rt = sapModel.LoadPatterns.GetLoadType(name, ref type);
    int rs = sapModel.LoadPatterns.GetSelfWTMultiplier(name, ref swm);
    patterns.Add(new { name, type = rt == 0 ? type.ToString() : null, selfWeightMultiplier = rs == 0 ? swm : (double?)null });
}

int nc = 0; string[] caseNames = null;
int retC = sapModel.LoadCases.GetNameList(ref nc, ref caseNames);
if (retC != 0) throw new InvalidOperationException($"ETABS returned {retC} from LoadCases.GetNameList");
var cases = new List<object>();
foreach (var name in (caseNames ?? new string[0]).Take(limit))
{
    ct.ThrowIfCancellationRequested();
    eLoadCaseType type = eLoadCaseType.LinearStatic; int subType = 0;
    int rt = sapModel.LoadCases.GetTypeOAPI(name, ref type, ref subType);
    cases.Add(new { name, type = rt == 0 ? type.ToString() : null, subType = rt == 0 ? subType : (int?)null });
}

int nk = 0; string[] comboNames = null;
int retK = sapModel.RespCombo.GetNameList(ref nk, ref comboNames);
if (retK != 0) throw new InvalidOperationException($"ETABS returned {retK} from RespCombo.GetNameList");
var combos = new List<object>();
foreach (var name in (comboNames ?? new string[0]).Take(limit))
{
    ct.ThrowIfCancellationRequested();
    int comboType = 0;
    int rt = sapModel.RespCombo.GetTypeOAPI(name, ref comboType);
    object members = null;
    if (includeComboCases)
    {
        int ni = 0; eCNameType[] kinds = null; string[] cnames = null; double[] factors = null;
        int rl = sapModel.RespCombo.GetCaseList(name, ref ni, ref kinds, ref cnames, ref factors);
        if (rl != 0) warnings.Add($"combo {name}: GetCaseList returned {rl}");
        else members = Enumerable.Range(0, ni).Select(i => new { name = cnames[i], kind = kinds[i].ToString(), scaleFactor = factors[i] }).ToList();
    }
    combos.Add(new { name, comboType = rt == 0 ? ComboTypeName(comboType) : null, cases = members });
}

log($"{np} patterns, {nc} cases, {nk} combos");
return new { success = true, patterns, cases, combos, patternCount = np, caseCount = nc, comboCount = nk, truncated = np > limit || nc > limit || nk > limit, warnings, summary = $"{np} load patterns, {nc} load cases, {nk} combinations" };
''',
[{"title": "Names and types", "args": {}}, {"title": "With combination members, first 50 of each", "args": {"includeComboCases": True, "limit": 50}}])

# ---------------------------------------------------------------------------------------------------------------------- R6
RESULT_HEAD = r'''
string caseOrCombo = args.Require("caseOrCombo");
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);
'''

RESULT_STATUS_AND_SELECT = r'''
int nc = 0; string[] caseNames = null; int[] caseStatus = null;
int rc = sapModel.Analyze.GetCaseStatus(ref nc, ref caseNames, ref caseStatus);
if (rc != 0) throw new InvalidOperationException($"ETABS returned {rc} from Analyze.GetCaseStatus");
int caseIndex = Array.FindIndex(caseNames ?? new string[0], c => string.Equals(c, caseOrCombo, StringComparison.OrdinalIgnoreCase));
// A load case that exists but was never run has no results: say so instead of reporting an empty table (status 4 = finished).
if (caseIndex >= 0 && caseStatus[caseIndex] != 4) throw new InvalidOperationException($"case '{caseOrCombo}' has no results (status {(caseStatus[caseIndex] == 1 ? "not run" : caseStatus[caseIndex] == 2 ? "could not start" : "not finished")}) — run_analysis first");
int r0 = sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
if (r0 != 0) throw new InvalidOperationException($"ETABS returned {r0} from Results.Setup.DeselectAllCasesAndCombosForOutput");
bool selected = sapModel.Results.Setup.SetCaseSelectedForOutput(caseOrCombo) == 0 || sapModel.Results.Setup.SetComboSelectedForOutput(caseOrCombo) == 0;
if (!selected) throw new ArgumentException($"caseOrCombo '{caseOrCombo}' is neither a load case nor a load combination in this model");
'''

seed("Results", "get_joint_reactions", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["results", "reaction", "joint", "query"],
    "title": "Get joint reactions",
    "description": "Reactions at the restrained joints for one load case or combination: forces in kN and moments in kN·m, per joint and step. Needs analysis results (run_analysis first, or run it in ETABS). Filter by point names or by story. Read-only — selecting the output case is a results setting, not a model change.",
    "notes": "OAPI: cAnalyze.GetCaseStatus (a load case must be finished = 4), cAnalysisResultsSetup.DeselectAllCasesAndCombosForOutput/SetCaseSelectedForOutput/SetComboSelectedForOutput, cPointObj.GetNameList (names validated), cAnalysisResults.JointReact (group 'All' or per object), cPointObj.GetLabelFromName. Moments arrive in kN·mm under the forced units and are divided by 1000.",
    "inputSchema": {"type": "object", "required": ["caseOrCombo"], "properties": {
        "caseOrCombo": {"type": "string", "description": "Load case or combination name whose results to read"},
        "pointNames": {"type": "array", "items": {"type": "string"}, "description": "Only these joints — unique names from get_structural_objects.name, not labels; default every joint with a reaction"},
        "story": {"type": "string", "description": "Only joints on this story"},
        "limit": LIMIT},
        "additionalProperties": False}},
RESULT_HEAD + r'''
var pointNames = args.Strings("pointNames").Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
string story = args.Str("story", null);
if (pointNames.Count > 0)
{
    int npn = 0; string[] all = null;
    int rn = sapModel.PointObj.GetNameList(ref npn, ref all);
    if (rn != 0) throw new InvalidOperationException($"ETABS returned {rn} from PointObj.GetNameList");
    var known = new HashSet<string>(all ?? new string[0], StringComparer.OrdinalIgnoreCase);
    var unknown = pointNames.Where(p => !known.Contains(p)).ToList();
    if (unknown.Count > 0) throw new ArgumentException($"not unique point names (labels are not names — use get_structural_objects.name): {string.Join(", ", unknown)}");
}
''' + RESULT_STATUS_AND_SELECT + r'''

int n = 0; string[] obj = null, elm = null, loadCase = null, stepType = null; double[] stepNum = null, f1 = null, f2 = null, f3 = null, m1 = null, m2 = null, m3 = null;
int ret = pointNames.Count == 0
    ? sapModel.Results.JointReact("All", eItemTypeElm.GroupElm, ref n, ref obj, ref elm, ref loadCase, ref stepType, ref stepNum, ref f1, ref f2, ref f3, ref m1, ref m2, ref m3)
    : 0;
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Results.JointReact — no results for '{caseOrCombo}'? run_analysis first");

var items = new List<object>();
int kept = 0;
var storyOf = new Dictionary<string, string>();
string StoryOf(string point)
{
    if (storyOf.TryGetValue(point, out var s)) return s;
    string label = "", st = "";
    s = sapModel.PointObj.GetLabelFromName(point, ref label, ref st) == 0 ? st : "";
    storyOf[point] = s;
    return s;
}
void Add(int i)
{
    if (story != null && !string.Equals(StoryOf(obj[i]), story, StringComparison.OrdinalIgnoreCase)) return;
    kept++;
    if (items.Count >= limit) return;
    items.Add(new { point = obj[i], story = StoryOf(obj[i]), loadCase = loadCase[i], stepType = stepType[i], stepNum = stepNum[i], fxKN = f1[i], fyKN = f2[i], fzKN = f3[i], mxKNm = m1[i] / 1000, myKNm = m2[i] / 1000, mzKNm = m3[i] / 1000 });
}

int total = 0;
if (pointNames.Count == 0)
{
    total = n;
    for (int i = 0; i < n; i++) { ct.ThrowIfCancellationRequested(); Add(i); }
}
else
{
    foreach (var point in pointNames)
    {
        ct.ThrowIfCancellationRequested();
        int rp = sapModel.Results.JointReact(point, eItemTypeElm.ObjectElm, ref n, ref obj, ref elm, ref loadCase, ref stepType, ref stepNum, ref f1, ref f2, ref f3, ref m1, ref m2, ref m3);
        if (rp != 0) throw new InvalidOperationException($"ETABS returned {rp} from Results.JointReact({point}) — no results for '{caseOrCombo}'? run_analysis first");
        total += n;
        for (int i = 0; i < n; i++) Add(i);
    }
}

log($"{items.Count} reaction rows for {caseOrCombo}");
return new { success = true, caseOrCombo, items, count = items.Count, matched = kept, total, truncated = kept > items.Count, summary = $"{items.Count} of {kept} joint reaction rows for {caseOrCombo} (kN, kN·m)" };
''',
[{"title": "All reactions for Dead", "args": {"caseOrCombo": "Dead"}},
 {"title": "Two joints under a combination", "args": {"caseOrCombo": "UDStlS1", "pointNames": ["1", "2"]}},
 {"title": "Base story only", "args": {"caseOrCombo": "Dead", "story": "Base", "limit": 50}}])

# ---------------------------------------------------------------------------------------------------------------------- R7
seed("Results", "get_frame_forces", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["results", "frame", "force", "moment", "query"],
    "title": "Get frame forces",
    "description": "Internal forces along frame objects for one load case or combination: axial P, shears V2/V3 in kN, torsion T and moments M2/M3 in kN·m at each output station (mm from the I end). Needs analysis results (run_analysis first). Filter by frame names. Read-only.",
    "notes": "OAPI: cAnalyze.GetCaseStatus (a load case must be finished = 4), cAnalysisResultsSetup.* (output selection), cFrameObj.GetNameList (names validated), cAnalysisResults.FrameForce (group 'All' or per object). Moments arrive in kN·mm and are divided by 1000.",
    "inputSchema": {"type": "object", "required": ["caseOrCombo"], "properties": {
        "caseOrCombo": {"type": "string", "description": "Load case or combination name whose results to read"},
        "frameNames": {"type": "array", "items": {"type": "string"}, "description": "Only these frames — unique names from get_structural_objects.name, not labels; default every frame"},
        "limit": LIMIT},
        "additionalProperties": False}},
RESULT_HEAD + r'''
var frameNames = args.Strings("frameNames").Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().ToList();
if (frameNames.Count > 0)
{
    int nfn = 0; string[] all = null;
    int rn = sapModel.FrameObj.GetNameList(ref nfn, ref all);
    if (rn != 0) throw new InvalidOperationException($"ETABS returned {rn} from FrameObj.GetNameList");
    var known = new HashSet<string>(all ?? new string[0], StringComparer.OrdinalIgnoreCase);
    var unknown = frameNames.Where(f => !known.Contains(f)).ToList();
    if (unknown.Count > 0) throw new ArgumentException($"not unique frame names (labels are not names — use get_structural_objects.name): {string.Join(", ", unknown)}");
}
''' + RESULT_STATUS_AND_SELECT + r'''

int n = 0; string[] obj = null, elm = null, loadCase = null, stepType = null; double[] objSta = null, elmSta = null, stepNum = null, p = null, v2 = null, v3 = null, t = null, m2 = null, m3 = null;
var items = new List<object>();
int total = 0;
void Take(int rows)
{
    total += rows;
    for (int i = 0; i < rows && items.Count < limit; i++)
        items.Add(new { frame = obj[i], stationMm = objSta[i], loadCase = loadCase[i], stepType = stepType[i], stepNum = stepNum[i], pKN = p[i], v2KN = v2[i], v3KN = v3[i], tKNm = t[i] / 1000, m2KNm = m2[i] / 1000, m3KNm = m3[i] / 1000 });
}

if (frameNames.Count == 0)
{
    int ret = sapModel.Results.FrameForce("All", eItemTypeElm.GroupElm, ref n, ref obj, ref objSta, ref elm, ref elmSta, ref loadCase, ref stepType, ref stepNum, ref p, ref v2, ref v3, ref t, ref m2, ref m3);
    if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Results.FrameForce — no results for '{caseOrCombo}'? run_analysis first");
    Take(n);
}
else
{
    foreach (var frame in frameNames)
    {
        ct.ThrowIfCancellationRequested();
        int ret = sapModel.Results.FrameForce(frame, eItemTypeElm.ObjectElm, ref n, ref obj, ref objSta, ref elm, ref elmSta, ref loadCase, ref stepType, ref stepNum, ref p, ref v2, ref v3, ref t, ref m2, ref m3);
        if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Results.FrameForce({frame}) — no results for '{caseOrCombo}'? run_analysis first");
        Take(n);
    }
}

log($"{items.Count} force rows for {caseOrCombo}");
return new { success = true, caseOrCombo, items, count = items.Count, total, truncated = total > items.Count, summary = $"{items.Count} frame force rows for {caseOrCombo} (kN, kN·m, stations in mm)" };
''',
[{"title": "Every frame under Dead", "args": {"caseOrCombo": "Dead", "limit": 500}},
 {"title": "Two beams under a combination", "args": {"caseOrCombo": "UDStlS1", "frameNames": ["12", "13"]}}])

# ---------------------------------------------------------------------------------------------------------------------- R8
seed("Results", "get_modal_results", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["results", "modal", "period", "mass", "query"],
    "title": "Get modal results",
    "description": "Periods, frequencies and participating mass ratios (UX, UY, UZ, RX, RY, RZ with their sums) of the modal load case. Needs analysis results with a modal case (run_analysis first). Read-only.",
    "notes": "OAPI: cLoadCases.GetTypeOAPI (must be Modal), cAnalyze.GetCaseStatus (4 = finished), cAnalysisResultsSetup.* (output selection), cAnalysisResults.ModalPeriod, cAnalysisResults.ModalParticipatingMassRatios.",
    "inputSchema": {"type": "object", "properties": {
        "caseName": {"type": "string", "default": "Modal", "description": "The modal load case"},
        "limit": LIMIT},
        "additionalProperties": False}},
r'''
string caseName = args.Str("caseName", "Modal") ?? "Modal";
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);
eLoadCaseType caseType = eLoadCaseType.Modal; int subType = 0;
if (sapModel.LoadCases.GetTypeOAPI(caseName, ref caseType, ref subType) != 0) throw new ArgumentException($"caseName '{caseName}' is not a load case in this model");
if (caseType != eLoadCaseType.Modal) throw new ArgumentException($"caseName '{caseName}' is a {caseType} case, not a modal case");
int nc = 0; string[] caseNames = null; int[] caseStatus = null;
int rc = sapModel.Analyze.GetCaseStatus(ref nc, ref caseNames, ref caseStatus);
if (rc != 0) throw new InvalidOperationException($"ETABS returned {rc} from Analyze.GetCaseStatus");
int caseIndex = Array.FindIndex(caseNames ?? new string[0], c => string.Equals(c, caseName, StringComparison.OrdinalIgnoreCase));
if (caseIndex >= 0 && caseStatus[caseIndex] != 4) throw new InvalidOperationException($"case '{caseName}' has no results (status {(caseStatus[caseIndex] == 1 ? "not run" : caseStatus[caseIndex] == 2 ? "could not start" : "not finished")}) — run_analysis first");
int r0 = sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
if (r0 != 0) throw new InvalidOperationException($"ETABS returned {r0} from Results.Setup.DeselectAllCasesAndCombosForOutput");
if (sapModel.Results.Setup.SetCaseSelectedForOutput(caseName) != 0) throw new InvalidOperationException($"ETABS returned non-zero from Results.Setup.SetCaseSelectedForOutput({caseName})");

int n = 0; string[] loadCase = null, stepType = null; double[] stepNum = null, period = null, frequency = null, circFreq = null, eigen = null;
int ret = sapModel.Results.ModalPeriod(ref n, ref loadCase, ref stepType, ref stepNum, ref period, ref frequency, ref circFreq, ref eigen);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Results.ModalPeriod — no modal results for '{caseName}'? run_analysis first");

int nm = 0; string[] mCase = null, mStep = null; double[] mNum = null, mPeriod = null, ux = null, uy = null, uz = null, sux = null, suy = null, suz = null, rx = null, ry = null, rz = null, srx = null, sry = null, srz = null;
int rm = sapModel.Results.ModalParticipatingMassRatios(ref nm, ref mCase, ref mStep, ref mNum, ref mPeriod, ref ux, ref uy, ref uz, ref sux, ref suy, ref suz, ref rx, ref ry, ref rz, ref srx, ref sry, ref srz);
var warnings = new List<string>();
if (rm != 0) warnings.Add($"ModalParticipatingMassRatios returned {rm}; mass ratios omitted");
else if (nm != n) warnings.Add($"ModalParticipatingMassRatios returned {nm} rows for {n} modes");

var modes = new List<object>();
for (int i = 0; i < n && i < limit; i++)
{
    ct.ThrowIfCancellationRequested();
    // Both calls list the selected case's modes in the same order; when the row counts differ, fall back to the mode number.
    int j = rm == 0 && nm == n ? i : -1;
    if (rm == 0 && j < 0) for (int k = 0; k < nm; k++) if (Math.Abs(mNum[k] - stepNum[i]) < 0.5) { j = k; break; }
    modes.Add(new
    {
        mode = (int)stepNum[i], loadCase = loadCase[i], periodS = period[i], frequencyHz = frequency[i], circularFrequencyRadS = circFreq[i], eigenvalue = eigen[i],
        ux = j >= 0 ? ux[j] : (double?)null, uy = j >= 0 ? uy[j] : (double?)null, uz = j >= 0 ? uz[j] : (double?)null,
        rx = j >= 0 ? rx[j] : (double?)null, ry = j >= 0 ? ry[j] : (double?)null, rz = j >= 0 ? rz[j] : (double?)null,
        sumUx = j >= 0 ? sux[j] : (double?)null, sumUy = j >= 0 ? suy[j] : (double?)null, sumRz = j >= 0 ? srz[j] : (double?)null,
    });
}

log($"{n} modes for {caseName}");
return new { success = true, caseName, modes, count = modes.Count, total = n, truncated = n > modes.Count, warnings, summary = $"{modes.Count} modes; T1 = {(n > 0 ? period[0].ToString("0.000") : "n/a")} s" };
''',
[{"title": "Default modal case", "args": {}}, {"title": "First 12 modes of a named case", "args": {"caseName": "Modal", "limit": 12}}])

# ---------------------------------------------------------------------------------------------------------------------- W1
seed("Geometry", "draw_frame_by_coords", {
    "transaction": "auto", "timeoutSeconds": 60, "destructive": True,
    "tags": ["frame", "create", "geometry"],
    "title": "Draw a frame by coordinates",
    "description": "Adds one frame object between two points given in mm (global coordinates), optionally with a frame section and a user name. Writes the model: the bridge saves it and copies a .EDB snapshot first (see the result's snapshot); ETABS has no undo. Returns the new frame's unique name.",
    "notes": "OAPI: cFrameObj.AddByCoord (creates the frame plus its two end points, so the result's changed.added is 3), cPropFrame.GetNameList to validate the section (the defined casing is passed on).",
    "inputSchema": {"type": "object", "required": ["x1", "y1", "z1", "x2", "y2", "z2"], "properties": {
        "x1": {"type": "number", "description": "Start X in mm"}, "y1": {"type": "number", "description": "Start Y in mm"}, "z1": {"type": "number", "description": "Start Z in mm"},
        "x2": {"type": "number", "description": "End X in mm"}, "y2": {"type": "number", "description": "End Y in mm"}, "z2": {"type": "number", "description": "End Z in mm"},
        "section": {"type": "string", "description": "Frame section property name; default the model's default section"},
        "name": {"type": "string", "description": "User name for the new frame; default ETABS numbers it"}},
        "additionalProperties": False}},
r'''
double x1 = args.RequireDouble("x1"), y1 = args.RequireDouble("y1"), z1 = args.RequireDouble("z1");
double x2 = args.RequireDouble("x2"), y2 = args.RequireDouble("y2"), z2 = args.RequireDouble("z2");
string section = args.Str("section", null);
string userName = args.Str("name", "") ?? "";
double length = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1) + (z2 - z1) * (z2 - z1));
if (length < 1) throw new ArgumentException("the two points coincide (length < 1 mm)");

if (section != null)
{
    int ns = 0; string[] sections = null;
    int rs = sapModel.PropFrame.GetNameList(ref ns, ref sections);
    if (rs != 0) throw new InvalidOperationException($"ETABS returned {rs} from PropFrame.GetNameList");
    section = (sections ?? new string[0]).FirstOrDefault(x => string.Equals(x, section, StringComparison.OrdinalIgnoreCase))
              ?? throw new ArgumentException($"section '{section}' is not defined; see get_materials_and_sections");
}

string name = "";
int ret = sapModel.FrameObj.AddByCoord(x1, y1, z1, x2, y2, z2, ref name, section ?? "Default", userName);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from FrameObj.AddByCoord");

log($"frame {name} added, length {length:0} mm, section {section ?? "Default"}");
return new { success = true, createdCount = 1, affectedNames = new[] { name }, lengthMm = Math.Round(length, 1), section = section ?? "Default", summary = $"frame {name} ({length:0} mm) added; snapshot in the result" };
''',
[{"title": "Column 3 m tall", "args": {"x1": 0, "y1": 0, "z1": 0, "x2": 0, "y2": 0, "z2": 3000}},
 {"title": "Beam with a section and a name", "args": {"x1": 0, "y1": 0, "z1": 3000, "x2": 6000, "y2": 0, "z2": 3000, "section": "B300X500", "name": "B1"}}])

# ---------------------------------------------------------------------------------------------------------------------- W2
seed("Property", "assign_frame_section", {
    "transaction": "auto", "timeoutSeconds": 60, "destructive": True,
    "tags": ["frame", "section", "assign"],
    "title": "Assign a frame section",
    "description": "Assigns one frame section property to the given frame objects (unique names). Writes the model: the bridge saves it and copies a .EDB snapshot first; ETABS has no undo. Frames ETABS refuses are listed in errors.",
    "notes": "OAPI: cFrameObj.SetSection (a Set* on an existing object leaves its name in place, so the result's changed stays 0), cPropFrame.GetNameList to validate the section (the defined casing is passed on).",
    "inputSchema": {"type": "object", "required": ["frameNames", "section"], "properties": {
        "frameNames": {"type": "array", "items": {"type": "string"}, "minItems": 1, "description": "Unique names of the frames"},
        "section": {"type": "string", "description": "Frame section property name (must exist)"}},
        "additionalProperties": False}},
r'''
var frameNames = args.Strings("frameNames").Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
string section = args.Require("section");
if (frameNames.Count == 0) throw new ArgumentException("frameNames must name at least one frame");

int ns = 0; string[] sections = null;
int rs = sapModel.PropFrame.GetNameList(ref ns, ref sections);
if (rs != 0) throw new InvalidOperationException($"ETABS returned {rs} from PropFrame.GetNameList");
section = (sections ?? new string[0]).FirstOrDefault(x => string.Equals(x, section, StringComparison.OrdinalIgnoreCase))
          ?? throw new ArgumentException($"section '{section}' is not defined; see get_materials_and_sections");

var affected = new List<string>();
var errors = new List<object>();
foreach (var name in frameNames)
{
    ct.ThrowIfCancellationRequested();
    int ret = sapModel.FrameObj.SetSection(name, section);
    if (ret == 0) affected.Add(name);
    else errors.Add(new { code = "ETABS_RET", message = $"ETABS returned {ret} from FrameObj.SetSection({name}) — unknown frame?", name });
}

log($"section {section} assigned to {affected.Count} of {frameNames.Count} frames");
return new { success = errors.Count == 0, modifiedCount = affected.Count, affectedNames = affected, section, errors, summary = $"{affected.Count} frame(s) now use {section}; snapshot in the result" };
''',
[{"title": "One frame", "args": {"frameNames": ["12"], "section": "B300X500"}},
 {"title": "Three columns", "args": {"frameNames": ["C1", "C2", "C3"], "section": "C400X400"}}])

# ---------------------------------------------------------------------------------------------------------------------- W3
seed("Load", "assign_frame_load", {
    "transaction": "auto", "timeoutSeconds": 60, "destructive": True,
    "tags": ["frame", "load", "assign"],
    "title": "Assign a frame load",
    "description": "Assigns a uniformly distributed load (kN/m over the whole length) or a point load (kN at a relative position 0–1) to frame objects under a load pattern. direction 10 = gravity by default (a positive value acts downward); 1–3 are the frame's local axes, 4–6 global X/Y/Z, 7–9 projected. Writes the model: the bridge saves it and copies a .EDB snapshot first; ETABS has no undo.",
    "notes": "OAPI: cFrameObj.SetLoadDistributed / SetLoadPoint (MyType 1 = force; Dir 1–3 need CSys = Local, 4–11 CSys = Global: 4–6 global X/Y/Z, 7–9 projected, 10 gravity, 11 projected gravity), cLoadPatterns.GetNameList to validate the pattern. Values are entered per mm under the forced units: kN/m ÷ 1000.",
    "inputSchema": {"type": "object", "required": ["frameNames", "pattern"], "properties": {
        "frameNames": {"type": "array", "items": {"type": "string"}, "minItems": 1, "description": "Unique names of the frames"},
        "pattern": {"type": "string", "description": "Load pattern name (must exist)"},
        "loadType": {"type": "string", "enum": ["distributed", "point"], "default": "distributed", "description": "Uniform distributed load or a single point load"},
        "valueKNperM": {"type": "number", "description": "Distributed load in kN/m (positive = along the direction; downward for gravity directions 10/11)"},
        "valueKN": {"type": "number", "description": "Point load in kN"},
        "relativePosition": {"type": "number", "default": 0.5, "minimum": 0, "maximum": 1, "description": "Point load position as a fraction of the length from the I end"},
        "direction": {"type": "integer", "default": 10, "minimum": 1, "maximum": 11, "description": "1–3 frame local axes, 4–6 global X/Y/Z, 7–9 projected global, 10 gravity, 11 projected gravity"},
        "replace": {"type": "boolean", "default": True, "description": "Replace existing loads of this pattern on the frame (false = add)"}},
        "additionalProperties": False}},
r'''
var frameNames = args.Strings("frameNames").Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
string pattern = args.Require("pattern");
string loadType = (args.Str("loadType", "distributed") ?? "distributed").Trim().ToLowerInvariant();
double? valueKNperM = args.DoubleOrNull("valueKNperM");
double? valueKN = args.DoubleOrNull("valueKN");
double relativePosition = args.Double("relativePosition", 0.5);
int direction = args.Int("direction", 10);
bool replace = args.Bool("replace", true);

if (frameNames.Count == 0) throw new ArgumentException("frameNames must name at least one frame");
if (loadType != "distributed" && loadType != "point") throw new ArgumentException("loadType must be distributed or point");
if (loadType == "distributed" && valueKNperM == null) throw new ArgumentException("valueKNperM is required for a distributed load");
if (loadType == "point" && valueKN == null) throw new ArgumentException("valueKN is required for a point load");
if (relativePosition < 0 || relativePosition > 1) throw new ArgumentException("relativePosition must be between 0 and 1");
if (direction < 1 || direction > 11) throw new ArgumentException("direction must be 1–11");

int np = 0; string[] patterns = null;
int rp = sapModel.LoadPatterns.GetNameList(ref np, ref patterns);
if (rp != 0) throw new InvalidOperationException($"ETABS returned {rp} from LoadPatterns.GetNameList");
if (!(patterns ?? new string[0]).Contains(pattern, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException($"pattern '{pattern}' is not defined; see get_load_definitions");

// Directions 1–3 are the frame's local axes and only exist in the Local coordinate system; 4–11 are global.
string csys = direction <= 3 ? "Local" : "Global";
var affected = new List<string>();
var errors = new List<object>();
foreach (var name in frameNames)
{
    ct.ThrowIfCancellationRequested();
    int ret = loadType == "distributed"
        ? sapModel.FrameObj.SetLoadDistributed(name, pattern, 1, direction, 0, 1, valueKNperM.Value / 1000, valueKNperM.Value / 1000, csys, true, replace)
        : sapModel.FrameObj.SetLoadPoint(name, pattern, 1, direction, relativePosition, valueKN.Value, csys, true, replace);
    if (ret == 0) affected.Add(name);
    else errors.Add(new { code = "ETABS_RET", message = $"ETABS returned {ret} from FrameObj.{(loadType == "distributed" ? "SetLoadDistributed" : "SetLoadPoint")}({name}) — unknown frame?", name });
}

string described = loadType == "distributed" ? $"{valueKNperM} kN/m" : $"{valueKN} kN at {relativePosition:0.00}";
log($"{described} ({pattern}, dir {direction}) on {affected.Count} of {frameNames.Count} frames");
return new { success = errors.Count == 0, modifiedCount = affected.Count, affectedNames = affected, pattern, loadType, direction, coordinateSystem = csys, errors, summary = $"{described} assigned under {pattern} to {affected.Count} frame(s); snapshot in the result" };
''',
[{"title": "Uniform gravity load on two beams", "args": {"frameNames": ["12", "13"], "pattern": "Live", "valueKNperM": 15}},
 {"title": "Mid-span point load, added to existing", "args": {"frameNames": ["12"], "pattern": "Dead", "loadType": "point", "valueKN": 50, "relativePosition": 0.5, "replace": False}}])

# ---------------------------------------------------------------------------------------------------------------------- D1
seed("Analysis", "run_analysis", {
    "transaction": "auto", "timeoutSeconds": 600, "destructive": True,
    "tags": ["destructive", "analysis", "run"],
    "title": "Run the analysis",
    "description": "DESTRUCTIVE: runs the ETABS analysis (all load cases, or only the ones named) after optionally deleting existing results, and reports each case's status. Needs the user's 'Allow destructive operations' checkbox in the bridge window on every call. Choose timeoutSeconds from the last analysis time in the ETABS GUI (up to 600): a timeout does not abort the analysis — ETABS keeps running it and later calls answer busy until it finishes. The model is saved and a .EDB snapshot copied first; results of a run cannot be undone.",
    "notes": "OAPI: cAnalyze.SetRunCaseFlag (All=true clears/sets every case), cAnalyze.DeleteResults(All=true), cAnalyze.RunAnalysis (synchronous, no cancel), cAnalyze.GetCaseStatus (1 not run, 2 could not start, 3 not finished, 4 finished).",
    "inputSchema": {"type": "object", "properties": {
        "cases": {"type": "array", "items": {"type": "string"}, "description": "Only run these load cases — the run flags of the others are switched off and stay off in the model until changed; default every case"},
        "deleteResultsFirst": {"type": "boolean", "default": False, "description": "Delete all existing results before running"}},
        "additionalProperties": False}},
r'''
var cases = args.Strings("cases").Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
bool deleteResultsFirst = args.Bool("deleteResultsFirst", false);

string StatusName(int status) => status == 1 ? "not-run" : status == 2 ? "could-not-start" : status == 3 ? "not-finished" : status == 4 ? "finished" : status.ToString();

if (cases.Count > 0)
{
    // Validate before touching any run flag: the flags persist in the model, so a misspelt name must change nothing.
    int nk = 0; string[] known = null;
    int rk = sapModel.LoadCases.GetNameList(ref nk, ref known);
    if (rk != 0) throw new InvalidOperationException($"ETABS returned {rk} from LoadCases.GetNameList");
    var unknown = cases.Where(c => !(known ?? new string[0]).Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
    if (unknown.Count > 0) throw new ArgumentException($"not load cases in this model: {string.Join(", ", unknown)} (see get_load_definitions)");

    int rAll = sapModel.Analyze.SetRunCaseFlag("", false, true);
    if (rAll != 0) throw new InvalidOperationException($"ETABS returned {rAll} from Analyze.SetRunCaseFlag(all=false)");
    foreach (var c in cases)
    {
        int rc = sapModel.Analyze.SetRunCaseFlag(c, true);
        if (rc != 0) throw new InvalidOperationException($"ETABS returned {rc} from Analyze.SetRunCaseFlag({c})");
    }
}
else
{
    int rAll = sapModel.Analyze.SetRunCaseFlag("", true, true);
    if (rAll != 0) throw new InvalidOperationException($"ETABS returned {rAll} from Analyze.SetRunCaseFlag(all=true)");
}

if (deleteResultsFirst)
{
    int rd = sapModel.Analyze.DeleteResults("", true);
    if (rd != 0) throw new InvalidOperationException($"ETABS returned {rd} from Analyze.DeleteResults(all)");
}

ct.ThrowIfCancellationRequested();
var started = DateTime.UtcNow;
log($"RunAnalysis starting ({(cases.Count > 0 ? string.Join(", ", cases) : "all cases")})…");
int ret = sapModel.Analyze.RunAnalysis();
double seconds = (DateTime.UtcNow - started).TotalSeconds;
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Analyze.RunAnalysis after {seconds:0.0} s");

int nc = 0; string[] caseNames = null; int[] caseStatus = null;
int rs = sapModel.Analyze.GetCaseStatus(ref nc, ref caseNames, ref caseStatus);
if (rs != 0) throw new InvalidOperationException($"ETABS returned {rs} from Analyze.GetCaseStatus");
var ranCases = Enumerable.Range(0, nc).Select(i => new { name = caseNames[i], status = StatusName(caseStatus[i]) }).ToList();
int finished = ranCases.Count(c => c.status == "finished");
var errors = ranCases.Where(c => c.status == "could-not-start" || c.status == "not-finished").Select(c => new { code = "CASE_FAILED", message = $"case {c.name}: {c.status}", name = c.name }).ToList();
log($"analysis finished in {seconds:0.0} s; {finished} of {ranCases.Count} cases finished, {errors.Count} failed");
return new { success = errors.Count == 0, ranCases, finishedCount = finished, errors, durationSeconds = Math.Round(seconds, 1), isLocked = sapModel.GetModelIsLocked(), summary = $"analysis ran in {seconds:0.0} s; {finished}/{ranCases.Count} cases finished{(errors.Count > 0 ? $", {errors.Count} failed" : "")}; snapshot in the result" };
''',
[{"title": "Everything", "args": {}}, {"title": "Dead and Live only, fresh results", "args": {"cases": ["Dead", "Live"], "deleteResultsFirst": True}}])
