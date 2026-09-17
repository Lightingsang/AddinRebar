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
