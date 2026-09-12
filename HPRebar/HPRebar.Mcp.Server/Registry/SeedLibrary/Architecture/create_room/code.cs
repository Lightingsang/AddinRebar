double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
if (levels.Count == 0) throw new InvalidOperationException("The document has no levels.");
Level Nearest(double zFt) => levels.OrderBy(l => Math.Abs(l.Elevation - zFt)).First();

var created = new List<object>();
var warnings = new List<string>();
int index = 0;
foreach (var item in args.List("data"))
{
    ct.ThrowIfCancellationRequested();
    var loc = item.Obj("location");
    long levelId = item.Long("levelId", -1);
    var level = levelId > 0 ? doc.GetElement(new ElementId(levelId)) as Level : null;
    if (levelId > 0 && level == null) warnings.Add($"data[{index}]: levelId {levelId} is not a level — using the nearest one.");
    level ??= Nearest(Ft(loc.Double("z")));

    var room = doc.Create.NewRoom(level, new UV(Ft(loc.Double("x")), Ft(loc.Double("y"))));
    room.get_Parameter(BuiltInParameter.ROOM_NAME)?.Set(item.Require("name"));
    if (item.Has("number")) room.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.Set(item.Str("number"));
    long upper = item.Long("upperLimitId", -1);
    if (upper > 0 && doc.GetElement(new ElementId(upper)) is Level) room.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL)?.Set(new ElementId(upper));
    if (item.Has("limitOffset")) room.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET)?.Set(Ft(item.Double("limitOffset")));
    if (item.Has("baseOffset")) room.get_Parameter(BuiltInParameter.ROOM_LOWER_OFFSET)?.Set(Ft(item.Double("baseOffset")));
    if (item.Has("department")) room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT)?.Set(item.Str("department"));
    if (item.Has("comments")) room.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(item.Str("comments"));
    doc.Regenerate();

    bool enclosed = room.Area > 0;
    if (!enclosed) warnings.Add($"Room '{room.Name}' ({room.Id.Value}) is not enclosed — check the walls around ({loc.Double("x")}, {loc.Double("y")}).");
    created.Add(new { id = room.Id.Value, name = room.Name, number = room.Number, level = level.Name, enclosed, area = Math.Round(UnitUtils.ConvertFromInternalUnits(room.Area, UnitTypeId.SquareMeters), 3) });
    index++;
}

return new { created = created.Count, rooms = created, warnings };
