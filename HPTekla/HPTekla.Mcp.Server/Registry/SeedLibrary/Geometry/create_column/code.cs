double x = args.Double("x"), y = args.Double("y");
double baseZ = args.Double("baseZ", 0);
double topZ = args.Double("topZ", 3500);
string profile = args.Str("profile", "HEB300");
string material = args.Str("material", "S235JR");
string name = args.Str("name", "COLUMN");
string partClass = args.Str("partClass", "2");

double height = topZ - baseZ;
if (Math.Abs(height) < 1.0) throw new ArgumentException($"Column height cannot be 0 (baseZ={baseZ}, topZ={topZ}).");

var col = new Beam
{
    StartPoint = new Point(x, y, baseZ),
    EndPoint = new Point(x, y, topZ),
    Name = name,
    Class = partClass
};
col.Profile.ProfileString = profile;
col.Material.MaterialString = material;
col.Position.Plane = Position.PlaneEnum.MIDDLE;
col.Position.Depth = Position.DepthEnum.MIDDLE;
col.Position.Rotation = Position.RotationEnum.FRONT;

bool ok = col.Insert();
if (!ok) throw new InvalidOperationException($"Failed to insert Column '{name}' ({profile}) into Tekla model.");

log($"Inserted Column {col.Identifier.ID} at ({x}, {y}), height: {height:F0} mm");

return new
{
    success = true,
    id = col.Identifier.ID,
    guid = col.Identifier.GUID.ToString(),
    name = name,
    profile = profile,
    material = material,
    heightMm = height
};
