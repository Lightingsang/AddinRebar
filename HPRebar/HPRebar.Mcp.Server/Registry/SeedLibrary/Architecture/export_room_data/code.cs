double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
double M2(double sqft) => Math.Round(UnitUtils.ConvertFromInternalUnits(sqft, UnitTypeId.SquareMeters), 3);
double M3(double cuft) => Math.Round(UnitUtils.ConvertFromInternalUnits(cuft, UnitTypeId.CubicMeters), 3);
bool includeUnplaced = args.Bool("includeUnplacedRooms");
bool includeNotEnclosed = args.Bool("includeNotEnclosedRooms");

var rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType()
    .Cast<Autodesk.Revit.DB.Architecture.Room>().ToList();
var list = new List<object>();
int skippedUnplaced = 0, skippedNotEnclosed = 0;
foreach (var r in rooms)
{
    bool unplaced = r.Location == null;
    bool notEnclosed = !unplaced && r.Area <= 0;
    if (unplaced && !includeUnplaced) { skippedUnplaced++; continue; }
    if (notEnclosed && !includeNotEnclosed) { skippedNotEnclosed++; continue; }
    string S(BuiltInParameter p) => r.get_Parameter(p)?.AsString();
    list.Add(new
    {
        id = r.Id.Value,
        uniqueId = r.UniqueId,
        name = r.Name,
        number = r.Number,
        level = r.Level?.Name,
        area = M2(r.Area),
        volume = M3(r.Volume),
        perimeter = Mm(r.Perimeter),
        unboundedHeight = Mm(r.UnboundedHeight),
        department = S(BuiltInParameter.ROOM_DEPARTMENT),
        comments = S(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS),
        occupancy = S(BuiltInParameter.ROOM_OCCUPANCY),
        phase = (doc.GetElement(r.get_Parameter(BuiltInParameter.ROOM_PHASE)?.AsElementId() ?? ElementId.InvalidElementId) as Phase)?.Name,
        status = unplaced ? "unplaced" : notEnclosed ? "notEnclosed" : "placed",
    });
}
return new
{
    totalRooms = list.Count,
    totalArea = Math.Round(rooms.Where(r => r.Location != null).Sum(r => M2(r.Area)), 3),
    skippedUnplaced,
    skippedNotEnclosed,
    rooms = list,
};
