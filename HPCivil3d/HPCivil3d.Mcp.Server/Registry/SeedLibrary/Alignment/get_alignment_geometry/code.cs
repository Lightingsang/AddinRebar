string key = args.Require("alignment");
int entityLimit = args.Int("entityLimit", 100);
int entityOffset = args.Int("entityOffset", 0);
double sampleStepMm = args.Double("sampleStepMm", 0);
int maxSamples = args.Int("maxSamples", 100);
if (entityLimit <= 0 || entityLimit > 150) throw new ArgumentException("entityLimit must be 1–150.");
if (entityOffset < 0) throw new ArgumentException("entityOffset must be >= 0.");
if (sampleStepMm < 0 || (sampleStepMm > 0 && sampleStepMm < 1000)) throw new ArgumentException("sampleStepMm must be 0 (no samples) or at least 1000 mm.");
if (maxSamples <= 0 || maxSamples > 200) throw new ArgumentException("maxSamples must be 1–200.");
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

var id = Resolve(key, civil.GetAlignmentIds(), "alignment");
var a = (Alignment)tr.GetObject(id, OpenMode.ForRead);
object Pt(Point2d p) => new { x = Math.Round(units.ToMm(p.X), 1), y = Math.Round(units.ToMm(p.Y), 1) };
var entities = new List<object>();
int entityCount = a.Entities.Count;
bool truncated = entityOffset + entityLimit < entityCount;
for (int order = entityOffset; order < entityCount && entities.Count < entityLimit; order++)
{
    ct.ThrowIfCancellationRequested();
    try
    {
        var e = a.Entities.GetEntityByOrder(order);
        var curve = e as AlignmentCurve;
        var arc = e as AlignmentArc;
        var line = e as AlignmentLine;
        entities.Add(new
        {
            order, type = e.EntityType.ToString(), entityId = e.EntityId, subEntityCount = e.SubEntityCount,
            startStation = curve == null ? (double?)null : Math.Round(curve.StartStation, 4), endStation = curve == null ? (double?)null : Math.Round(curve.EndStation, 4), lengthMm = curve == null ? (double?)null : Math.Round(units.ToMm(curve.Length), 1),
            start = curve == null ? null : Pt(curve.StartPoint), end = curve == null ? null : Pt(curve.EndPoint),
            radiusMm = arc == null ? (double?)null : Math.Round(units.ToMm(arc.Radius), 1), center = arc == null ? null : Pt(arc.CenterPoint),
            clockwise = arc?.Clockwise, piStation = arc?.PIStation, deltaRad = arc?.Delta, directionRad = line?.Direction,
        });
    }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString() + "#" + order); }
}

var samples = new List<object>();
if (sampleStepMm > 0)
{
    double step = units.ToDrawing(sampleStepMm);
    double station = a.StartingStation;
    for (; station <= a.EndingStation && samples.Count < maxSamples; station += step)
    {
        ct.ThrowIfCancellationRequested();
        double easting = 0, northing = 0;
        a.PointLocation(station, 0, ref easting, ref northing);
        samples.Add(new { station = Math.Round(station, 4), x = Math.Round(units.ToMm(easting), 1), y = Math.Round(units.ToMm(northing), 1) });
    }
    if (station <= a.EndingStation) truncated = true;   // the cap stopped the walk before the end
}
log($"{a.Name}: {entities.Count} entities, {samples.Count} samples");
return new
{
    success = errors.Count == 0, summary = $"{a.Name}: {entities.Count} entities over {Math.Round(units.ToMm(a.Length) / 1000, 2)} m" + (samples.Count > 0 ? $", {samples.Count} samples" : ""),
    alignment = new { handle = id.Handle.ToString(), name = a.Name, startStation = a.StartingStation, endStation = a.EndingStation, lengthMm = Math.Round(units.ToMm(a.Length), 1) },
    count = entities.Count, entityCount, entityOffset, truncated, drawingUnit = units.Label, lengthUnit = "mm", entities, samples, warnings = new List<string>(), errors,
};
