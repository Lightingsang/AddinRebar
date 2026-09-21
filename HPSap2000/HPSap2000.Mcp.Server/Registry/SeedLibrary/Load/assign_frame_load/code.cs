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
if (rp != 0) throw new InvalidOperationException($"SAP2000 returned {rp} from LoadPatterns.GetNameList");
if (!(patterns ?? new string[0]).Contains(pattern, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException($"pattern '{pattern}' is not defined; see get_load_definitions");

string csys = direction <= 3 ? "Local" : "Global";
var affected = new List<string>();
var errors = new List<object>();
foreach (var name in frameNames)
{
    ct.ThrowIfCancellationRequested();
    int ret = loadType == "distributed"
        ? sapModel.FrameObj.SetLoadDistributed(name, pattern, 1, direction, 0, 1, valueKNperM.Value, valueKNperM.Value, csys, true, replace, eItemType.Objects)
        : sapModel.FrameObj.SetLoadPoint(name, pattern, 1, direction, relativePosition, valueKN.Value, csys, true, replace, eItemType.Objects);
    if (ret == 0) affected.Add(name);
    else errors.Add(new { code = "SAP2000_RET", message = $"SAP2000 returned {ret} from FrameObj.{(loadType == "distributed" ? "SetLoadDistributed" : "SetLoadPoint")}({name}) — unknown frame?", name });
}

string described = loadType == "distributed" ? $"{valueKNperM} kN/m" : $"{valueKN} kN at {relativePosition:0.00}";
log($"{described} ({pattern}, dir {direction}) on {affected.Count} of {frameNames.Count} frames");
return new { success = errors.Count == 0, modifiedCount = affected.Count, affectedNames = affected, pattern, loadType, direction, coordinateSystem = csys, errors, summary = $"{described} assigned under {pattern} to {affected.Count} frame(s); snapshot in the result" };
