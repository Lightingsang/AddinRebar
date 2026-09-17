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
