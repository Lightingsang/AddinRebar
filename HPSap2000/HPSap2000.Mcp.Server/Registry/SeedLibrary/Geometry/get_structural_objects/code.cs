string kind = (args.Str("kind", "frame") ?? "frame").Trim().ToLowerInvariant();
string group = args.Str("group", null);
string nameLike = args.Str("nameLike", null);
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);
int offset = Math.Max(0, args.Int("offset", 0));
if (kind != "frame" && kind != "area" && kind != "point") throw new ArgumentException("kind must be frame, area or point");

HashSet<string> groupMembers = null;
if (!string.IsNullOrWhiteSpace(group))
{
    int numItems = 0; int[] objTypes = null; string[] objNames = null;
    int rg = sapModel.GroupDef.GetAssignments(group, ref numItems, ref objTypes, ref objNames);
    if (rg != 0) throw new InvalidOperationException($"SAP2000 returned {rg} from GroupDef.GetAssignments for group '{group}'");
    int targetType = kind == "point" ? 1 : kind == "frame" ? 2 : 5; // SAP2000 object types: 1=Point, 2=Frame, 5=Area
    groupMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < numItems; i++)
    {
        if (objTypes[i] == targetType) groupMembers.Add(objNames[i]);
    }
}

int n = 0; string[] names = null;
int ret = kind == "frame" ? sapModel.FrameObj.GetNameList(ref n, ref names) : kind == "area" ? sapModel.AreaObj.GetNameList(ref n, ref names) : sapModel.PointObj.GetNameList(ref n, ref names);
if (ret != 0) throw new InvalidOperationException($"SAP2000 returned {ret} from {kind}.GetNameList");
names = names ?? new string[0];

bool Matches(string name) =>
    (groupMembers == null || groupMembers.Contains(name)) &&
    (nameLike == null || name.IndexOf(nameLike, StringComparison.OrdinalIgnoreCase) >= 0);

(string point, double xM, double yM, double zM) Coords(string point)
{
    double x = 0, y = 0, z = 0;
    int r = sapModel.PointObj.GetCoordCartesian(point, ref x, ref y, ref z, "Global");
    if (r != 0) throw new InvalidOperationException($"SAP2000 returned {r} from PointObj.GetCoordCartesian({point})");
    return (point, x, y, z);
}

var items = new List<object>();
int matched = 0;
var warnings = new List<string>();
foreach (var name in names)
{
    ct.ThrowIfCancellationRequested();
    if (!Matches(name)) continue;
    matched++;
    if (matched <= offset) continue;
    if (items.Count >= limit) break;

    if (kind == "frame")
    {
        string p1 = "", p2 = "", section = "", auto = "";
        int rp = sapModel.FrameObj.GetPoints(name, ref p1, ref p2);
        if (rp != 0) { warnings.Add($"{name}: GetPoints returned {rp}"); continue; }
        int rs = sapModel.FrameObj.GetSection(name, ref section, ref auto);
        var a = Coords(p1); var b = Coords(p2);
        double length = Math.Sqrt((b.xM - a.xM) * (b.xM - a.xM) + (b.yM - a.yM) * (b.yM - a.yM) + (b.zM - a.zM) * (b.zM - a.zM));
        items.Add(new { name, kind, section = rs == 0 ? section : null, endpointsM = new[] { a, b }, lengthM = Math.Round(length, 3) });
    }
    else if (kind == "area")
    {
        int np = 0; string[] pts = null;
        int rp = sapModel.AreaObj.GetPoints(name, ref np, ref pts);
        if (rp != 0) { warnings.Add($"{name}: GetPoints returned {rp}"); continue; }
        items.Add(new { name, kind, vertexCount = np, verticesM = (pts ?? new string[0]).Take(32).Select(pt => Coords(pt)).ToList() });
    }
    else
    {
        items.Add(new { name, kind, coordinatesM = Coords(name) });
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
