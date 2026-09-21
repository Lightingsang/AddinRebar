double x1 = args.Double("startX");
double y1 = args.Double("startY");
double z1 = args.Double("startZ");
double x2 = args.Double("endX");
double y2 = args.Double("endY");
double z2 = args.Double("endZ");
string section = args.Str("sectionName");
int barNum = args.Int("barNumber", 0);

double dx = x2 - x1, dy = y2 - y1, dz = z2 - z1;
double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
if (len < 0.001) throw new ArgumentException($"Start and end coordinates are coincident (distance {len:F4} m < 1 mm).");

int n1 = structure.Nodes.FindXYZ(x1, y1, z1);
if (n1 <= 0)
{
    n1 = structure.Nodes.FreeNumber;
    structure.Nodes.Create(n1, x1, y1, z1);
}

int n2 = structure.Nodes.FindXYZ(x2, y2, z2);
if (n2 <= 0)
{
    n2 = structure.Nodes.FreeNumber;
    structure.Nodes.Create(n2, x2, y2, z2);
}

int bId = barNum > 0 ? barNum : structure.Bars.FreeNumber;
structure.Bars.Create(bId, n1, n2);

if (!string.IsNullOrWhiteSpace(section))
{
    var sel = structure.Selections.Create(IRobotObjectType.I_OT_BAR);
    sel.AddOne(bId);
    structure.Bars.SetLabel(sel, IRobotLabelType.I_LT_BAR_SECTION, section);
}

log($"Created bar {bId} between nodes {n1} ({x1},{y1},{z1}) and {n2} ({x2},{y2},{z2}), length: {len:F3} m");

return new
{
    success = true,
    barNumber = bId,
    startNode = n1,
    endNode = n2,
    length = len,
    section = section
};
