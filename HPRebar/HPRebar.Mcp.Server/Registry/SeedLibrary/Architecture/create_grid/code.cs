double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);

int xCount = Math.Max(1, args.Int("xCount", 1)), yCount = Math.Max(1, args.Int("yCount", 1));
double xSpacing = args.RequireDouble("xSpacing"), ySpacing = args.RequireDouble("ySpacing");
double x0 = args.Double("xStartPosition", 0), y0 = args.Double("yStartPosition", 0);
double xLast = x0 + (xCount - 1) * xSpacing, yLast = y0 + (yCount - 1) * ySpacing;
double xMin = args.Double("xExtentMin", x0 - 1000), xMax = args.Double("xExtentMax", xLast + 1000);
double yMin = args.Double("yExtentMin", y0 - 1000), yMax = args.Double("yExtentMax", yLast + 1000);
double z = Ft(args.Double("elevation", 0));

var used = new HashSet<string>(new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>().Select(g => g.Name), StringComparer.OrdinalIgnoreCase);

string Alpha(int index)
{
    var s = "";
    index++;
    while (index > 0) { index--; s = (char)('A' + index % 26) + s; index /= 26; }
    return s;
}
int AlphaIndex(string label)
{
    int v = 0;
    foreach (var ch in label.ToUpperInvariant()) { if (ch < 'A' || ch > 'Z') return 0; v = v * 26 + (ch - 'A' + 1); }
    return Math.Max(0, v - 1);
}
List<string> Labels(int count, string start, string style)
{
    var list = new List<string>();
    if (string.Equals(style, "numeric", StringComparison.OrdinalIgnoreCase))
    {
        int n = int.TryParse(start, out var parsed) ? parsed : 1;
        for (int i = 0; i < count; i++) list.Add((n + i).ToString());
    }
    else
    {
        int n = AlphaIndex(string.IsNullOrEmpty(start) ? "A" : start);
        for (int i = 0; i < count; i++) list.Add(Alpha(n + i));
    }
    return list;
}
string Unique(string label)
{
    var candidate = label; int k = 1;
    while (used.Contains(candidate)) candidate = label + "." + (k++);
    used.Add(candidate);
    return candidate;
}

var created = new List<object>();
var xLabels = Labels(xCount, args.Str("xStartLabel", "A"), args.Str("xNamingStyle", "alphabetic"));
for (int i = 0; i < xCount; i++)
{
    ct.ThrowIfCancellationRequested();
    double x = Ft(x0 + i * xSpacing);
    var grid = Grid.Create(doc, Line.CreateBound(new XYZ(x, Ft(yMin), z), new XYZ(x, Ft(yMax), z)));
    grid.Name = Unique(xLabels[i]);
    created.Add(new { id = grid.Id.Value, name = grid.Name, axis = "x", position = x0 + i * xSpacing });
}
var yLabels = Labels(yCount, args.Str("yStartLabel", "1"), args.Str("yNamingStyle", "numeric"));
for (int j = 0; j < yCount; j++)
{
    ct.ThrowIfCancellationRequested();
    double y = Ft(y0 + j * ySpacing);
    var grid = Grid.Create(doc, Line.CreateBound(new XYZ(Ft(xMin), y, z), new XYZ(Ft(xMax), y, z)));
    grid.Name = Unique(yLabels[j]);
    created.Add(new { id = grid.Id.Value, name = grid.Name, axis = "y", position = y0 + j * ySpacing });
}

return new { created = created.Count, grids = created };
