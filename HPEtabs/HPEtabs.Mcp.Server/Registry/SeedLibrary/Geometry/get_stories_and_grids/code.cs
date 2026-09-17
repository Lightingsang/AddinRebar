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
