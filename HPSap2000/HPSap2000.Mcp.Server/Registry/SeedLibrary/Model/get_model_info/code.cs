bool includeGroups = args.Bool("includeGroups", false);
string workingFile = sapModel.GetModelFilename(true);
bool hasModel = !string.IsNullOrWhiteSpace(workingFile) && workingFile.Contains("\\");
// After any save SAP2000 may name its working copy `.$sb` or `.$2k` beside the `.SDB`; the model the user (and snapshots) know is the `.SDB`.
string file = hasModel && (workingFile.EndsWith(".$sb", StringComparison.OrdinalIgnoreCase) || workingFile.EndsWith(".$2k", StringComparison.OrdinalIgnoreCase))
    ? workingFile.Substring(0, workingFile.Length - 4) + ".SDB"
    : workingFile;
string version = ""; double versionNumber = 0;
int retV = sapModel.GetVersion(ref version, ref versionNumber);
if (retV != 0) throw new InvalidOperationException($"SAP2000 returned {retV} from GetVersion");

int np = 0, nf = 0, na = 0; string[] ln = null;
int rp = sapModel.PointObj.GetNameList(ref np, ref ln);
if (rp != 0) throw new InvalidOperationException($"SAP2000 returned {rp} from PointObj.GetNameList");
int rf = sapModel.FrameObj.GetNameList(ref nf, ref ln);
if (rf != 0) throw new InvalidOperationException($"SAP2000 returned {rf} from FrameObj.GetNameList");
int ra = sapModel.AreaObj.GetNameList(ref na, ref ln);
if (ra != 0) throw new InvalidOperationException($"SAP2000 returned {ra} from AreaObj.GetNameList");
int points = np, frames = nf, areas = na;

object groups = null;
if (includeGroups)
{
    int ng = 0; string[] gnames = null;
    int rg = sapModel.GroupDef.GetNameList(ref ng, ref gnames);
    if (rg != 0) throw new InvalidOperationException($"SAP2000 returned {rg} from GroupDef.GetNameList");
    groups = (gnames ?? new string[0]).ToList();
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
    sap2000Version = version,
    counts = new { points, frames, areas },
    groups,
    summary = $"{points} points, {frames} frames, {areas} areas; units {units.Label} in scripts",
};
