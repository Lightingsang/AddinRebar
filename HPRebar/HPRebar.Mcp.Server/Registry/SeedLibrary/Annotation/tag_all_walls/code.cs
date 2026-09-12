var view = doc.ActiveView;
var wanted = args.Longs("wallIds").ToHashSet();
var walls = new FilteredElementCollector(doc, view.Id).OfClass(typeof(Wall)).Cast<Wall>().Where(w => w.Location is LocationCurve && (wanted.Count == 0 || wanted.Contains(w.Id.Value))).ToList();
var tagged = new FilteredElementCollector(doc, view.Id).OfClass(typeof(IndependentTag)).Cast<IndependentTag>()
    .SelectMany(t => t.GetTaggedLocalElementIds()).Select(id => id.Value).ToHashSet();

long tagTypeId = args.Long("tagTypeId", -1);
var tagType = tagTypeId > 0 ? doc.GetElement(new ElementId(tagTypeId)) as FamilySymbol : null;
tagType ??= new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_WallTags).Cast<FamilySymbol>().FirstOrDefault()
    ?? throw new InvalidOperationException("No wall tag family loaded. Load a wall tag (Annotations → Wall Tag) first.");
if (!tagType.IsActive) tagType.Activate();
bool leader = args.Bool("useLeader", false);

var created = new List<object>(); var skipped = new List<object>();
foreach (var wall in walls)
{
    ct.ThrowIfCancellationRequested();
    if (tagged.Contains(wall.Id.Value)) { skipped.Add(new { wallId = wall.Id.Value, reason = "already tagged in this view" }); continue; }
    var mid = (wall.Location as LocationCurve).Curve.Evaluate(0.5, true);
    var tag = IndependentTag.Create(doc, tagType.Id, view.Id, new Reference(wall), leader, TagOrientation.Horizontal, mid);
    created.Add(new { tagId = tag.Id.Value, wallId = wall.Id.Value, wallType = wall.WallType.Name });
}

return new { view = view.Name, tagType = tagType.FamilyName + ": " + tagType.Name, created = created.Count, tags = created, skipped };
