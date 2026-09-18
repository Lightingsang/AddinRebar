string key = args.Str("alignment");
int limit = args.Int("limit", 100);
if (limit <= 0 || limit > 180) throw new ArgumentException("limit must be 1–180.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
ObjectId Resolve(string key, ObjectIdCollection ids, string what)
{
    if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException($"{what} is required: give its name or handle.");
    foreach (ObjectId id in ids)
    {
        if (string.Equals(id.Handle.ToString(), key, StringComparison.OrdinalIgnoreCase)) return id;
        var entity = tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Entity;
        if (entity != null && string.Equals(entity.Name, key, StringComparison.OrdinalIgnoreCase)) return id;
    }
    throw new ArgumentException($"No {what} named '{key}' (name or handle) in this drawing.");
}
var errors = new List<object>();
void Fail(string code, string message, string handle) => errors.Add(new { code, message, handle });
string Classify(Exception ex) => ex is Autodesk.AutoCAD.Runtime.Exception acad ? acad.ErrorStatus.ToString() : ex.GetType().Name;

var alignmentIds = new List<ObjectId>();
if (string.IsNullOrWhiteSpace(key)) foreach (ObjectId id in civil.GetAlignmentIds()) alignmentIds.Add(id);
else alignmentIds.Add(Resolve(key, civil.GetAlignmentIds(), "alignment"));

var items = new List<object>();
int matched = 0;
foreach (var alignmentId in alignmentIds)
{
    ct.ThrowIfCancellationRequested();
    var a = (Alignment)tr.GetObject(alignmentId, OpenMode.ForRead);
    foreach (ObjectId profileId in a.GetProfileIds())
    {
        matched++;
        if (items.Count >= limit) continue;
        try
        {
            var p = (Profile)tr.GetObject(profileId, OpenMode.ForRead);
            items.Add(new
            {
                handle = profileId.Handle.ToString(), alignment = a.Name, name = p.Name, profileType = p.ProfileType.ToString(),
                startStation = Math.Round(p.StartingStation, 4), endStation = Math.Round(p.EndingStation, 4), lengthMm = Math.Round(units.ToMm(p.Length), 1),
                elevationMin = Math.Round(p.ElevationMin, 4), elevationMax = Math.Round(p.ElevationMax, 4), pviCount = p.PVIs.Count, entityCount = p.Entities.Count, style = p.StyleName,
            });
        }
        catch (Exception ex) { Fail(Classify(ex), ex.Message, profileId.Handle.ToString()); }
    }
}
log($"{items.Count} profiles on {alignmentIds.Count} alignment(s)");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {matched} profiles on {alignmentIds.Count} alignment(s)", count = items.Count, truncated = matched > items.Count, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors };
