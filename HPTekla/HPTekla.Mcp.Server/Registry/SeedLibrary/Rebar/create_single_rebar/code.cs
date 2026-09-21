int fatherId = args.Int("fatherId");
if (fatherId <= 0) throw new ArgumentException("Valid fatherId (> 0) is required.");
var pts = args.List("points");
if (pts.Count < 2) throw new ArgumentException("SingleRebar requires at least 2 centerline points.");

string size = args.Str("size", "20");
string grade = args.Str("grade", "B500B");
string name = args.Str("name", "MAIN_BAR");
int rClass = args.Int("rebarClass", 6);

var fatherPart = model.SelectModelObject(new Identifier(fatherId)) as Part;
if (fatherPart == null) throw new ArgumentException($"Host Part {fatherId} not found in model.");

var bar = new SingleRebar
{
    Father = fatherPart,
    Name = name,
    Class = rClass,
    Grade = grade,
    Size = size
};

var polygon = new Polygon();
for (int i = 0; i < pts.Count; i++)
{
    polygon.Points.Add(new Point(pts[i].Double("x"), pts[i].Double("y"), pts[i].Double("z")));
}
bar.Polygon = polygon;

bool ok = bar.Insert();
if (!ok) throw new InvalidOperationException($"Failed to insert SingleRebar '{name}' for part {fatherId}.");

log($"Inserted SingleRebar {bar.Identifier.ID} (size: {size}, {pts.Count} points)");

return new
{
    success = true,
    id = bar.Identifier.ID,
    fatherId = fatherId,
    name = name,
    size = size,
    grade = grade,
    pointCount = pts.Count
};
