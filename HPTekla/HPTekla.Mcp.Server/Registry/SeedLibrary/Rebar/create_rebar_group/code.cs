int fatherId = args.Int("fatherId");
if (fatherId <= 0) throw new ArgumentException("Valid fatherId (> 0) is required.");
var shape = args.List("shapePoints");
if (shape.Count < 2) throw new ArgumentException("Rebar shape requires at least 2 points.");

double sx = args.Double("startX"), sy = args.Double("startY"), sz = args.Double("startZ");
double ex = args.Double("endX"), ey = args.Double("endY"), ez = args.Double("endZ");
double spacing = args.Double("spacing", 150);
string size = args.Str("size", "12");
string grade = args.Str("grade", "B500B");
string name = args.Str("name", "STIRRUP");
int rClass = args.Int("rebarClass", 7);

var fatherPart = model.SelectModelObject(new Identifier(fatherId)) as Part;
if (fatherPart == null) throw new ArgumentException($"Host Part {fatherId} not found in model.");

var group = new RebarGroup
{
    Father = fatherPart,
    Name = name,
    Class = rClass,
    Grade = grade,
    Size = size,
    StartPoint = new Point(sx, sy, sz),
    EndPoint = new Point(ex, ey, ez)
};
group.SpacingType = BaseRebarGroup.RebarGroupSpacingTypeEnum.SPACING_TYPE_TARGET_SPACE;
group.Spacings.Add(spacing);

var polygon = new Polygon();
for (int i = 0; i < shape.Count; i++)
{
    polygon.Points.Add(new Point(shape[i].Double("x"), shape[i].Double("y"), shape[i].Double("z")));
}
group.Polygons.Add(polygon);

bool ok = group.Insert();
if (!ok) throw new InvalidOperationException($"Failed to insert RebarGroup '{name}' for part {fatherId}.");

log($"Inserted RebarGroup {group.Identifier.ID} (size: {size}, spacing: {spacing:F0} mm)");

return new
{
    success = true,
    id = group.Identifier.ID,
    fatherId = fatherId,
    name = name,
    size = size,
    grade = grade,
    spacing = spacing
};
