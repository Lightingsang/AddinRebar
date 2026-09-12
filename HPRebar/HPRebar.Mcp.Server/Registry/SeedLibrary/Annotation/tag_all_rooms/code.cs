var view = doc.ActiveView;
if (view.ViewType is not (ViewType.FloorPlan or ViewType.CeilingPlan or ViewType.AreaPlan or ViewType.EngineeringPlan))
    throw new InvalidOperationException($"Active view '{view.Name}' is a {view.ViewType}; open a plan view to tag rooms.");

var wanted = args.Longs("roomIds").ToHashSet();
var rooms = new FilteredElementCollector(doc, view.Id).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType()
    .Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Location != null && r.Area > 0 && (wanted.Count == 0 || wanted.Contains(r.Id.Value))).ToList();
var tagged = new FilteredElementCollector(doc, view.Id).OfCategory(BuiltInCategory.OST_RoomTags).WhereElementIsNotElementType()
    .Cast<Autodesk.Revit.DB.Architecture.RoomTag>().Where(t => t.Room != null).Select(t => t.Room.Id.Value).ToHashSet();

long tagTypeId = args.Long("tagTypeId", -1);
var tagType = tagTypeId > 0 ? doc.GetElement(new ElementId(tagTypeId)) as Autodesk.Revit.DB.Architecture.RoomTagType : null;
// RoomTagType is not a native Revit class filter: collect FamilySymbols of OST_RoomTags and pick the RoomTagType ones
tagType ??= new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_RoomTags).Cast<FamilySymbol>().OfType<Autodesk.Revit.DB.Architecture.RoomTagType>().FirstOrDefault()
    ?? throw new InvalidOperationException("No room tag family loaded. Load a room tag (Annotations → Room Tag) first.");
bool leader = args.Bool("useLeader", false);

var created = new List<object>(); var skipped = new List<object>();
foreach (var room in rooms)
{
    ct.ThrowIfCancellationRequested();
    if (tagged.Contains(room.Id.Value)) { skipped.Add(new { roomId = room.Id.Value, name = room.Name, reason = "already tagged in this view" }); continue; }
    var p = (room.Location as LocationPoint).Point;
    var tag = doc.Create.NewRoomTag(new LinkElementId(room.Id), new UV(p.X, p.Y), view.Id);
    if (tag.RoomTagType.Id != tagType.Id) tag.RoomTagType = tagType;
    tag.HasLeader = leader;
    created.Add(new { tagId = tag.Id.Value, roomId = room.Id.Value, name = room.Name, number = room.Number });
}

return new { view = view.Name, tagType = tagType.Name, created = created.Count, tags = created, skipped };
