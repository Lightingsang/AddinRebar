var pts = args.List("points");
if (pts.Count < 3) throw new ArgumentException("At least 3 points are required to define a contour plate.");

double thickness = args.Double("thickness", 20);
string material = args.Str("material", "S235JR");
string name = args.Str("name", "PLATE");
string partClass = args.Str("partClass", "3");

var plate = new ContourPlate
{
    Name = name,
    Class = partClass
};
plate.Profile.ProfileString = $"PL{thickness:F0}";
plate.Material.MaterialString = material;

for (int i = 0; i < pts.Count; i++)
{
    double px = pts[i].Double("x");
    double py = pts[i].Double("y");
    double pz = pts[i].Double("z");
    plate.AddContourPoint(new ContourPoint(new Point(px, py, pz), null));
}

bool ok = plate.Insert();
if (!ok) throw new InvalidOperationException("Failed to insert ContourPlate into Tekla model.");

log($"Inserted ContourPlate {plate.Identifier.ID} (PL{thickness:F0}, {pts.Count} vertices)");

return new
{
    success = true,
    id = plate.Identifier.ID,
    name = name,
    profile = plate.Profile.ProfileString,
    vertexCount = pts.Count
};
