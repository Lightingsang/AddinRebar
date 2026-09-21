double x1 = args.Double("startX"), y1 = args.Double("startY"), z1 = args.Double("startZ");
double x2 = args.Double("endX"), y2 = args.Double("endY"), z2 = args.Double("endZ");
string profile = args.Str("profile", "HEA300");
string material = args.Str("material", "S235JR");
string name = args.Str("name", "BEAM");
string partClass = args.Str("partClass", "1");

double dx = x2 - x1, dy = y2 - y1, dz = z2 - z1;
double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
if (length < 1.0) throw new ArgumentException($"Start and end points are coincident (length {length:F2} mm < 1 mm).");

var beam = new Beam
{
    StartPoint = new Point(x1, y1, z1),
    EndPoint = new Point(x2, y2, z2),
    Name = name,
    Class = partClass
};
beam.Profile.ProfileString = profile;
beam.Material.MaterialString = material;
beam.Position.Plane = Position.PlaneEnum.MIDDLE;
beam.Position.Depth = Position.DepthEnum.MIDDLE;

bool ok = beam.Insert();
if (!ok) throw new InvalidOperationException($"Failed to insert Beam '{name}' ({profile}) into Tekla model.");

log($"Inserted Beam {beam.Identifier.ID} ({profile}, length: {length:F0} mm)");

return new
{
    success = true,
    id = beam.Identifier.ID,
    guid = beam.Identifier.GUID.ToString(),
    name = name,
    profile = profile,
    material = material,
    lengthMm = length
};
