"""Generates the 12 Civil 3D seed tools (tool.json + code.cs + examples.json) under HPCivil3d.Mcp.Server/Registry/SeedLibrary/.

Every API member a script names was verified on the installed Civil 3D 2026 (plans/260917-1633-civil3d-mcp-2026/research/
reflection-addendum-verified-signatures.md and the live runs recorded beside it). The scripts follow the seed contract: a plain
body ending in `return`, `args.X("literal", default)` for every schema key, mm at the tool boundary for plan geometry
(`units.ToMm`), stations / elevations / areas in the drawing unit with `drawingUnit` in every envelope, caller mistakes as
ArgumentException, per-item problems in `errors[]`, no Rebuild / data shortcut / file member. Page caps are sized from the
bytes one item costs on the wire (the bridge truncates a result over 64 KB to one string): alignment 325 B, profile 338 B,
parcel 232 B, COGO point 182 B, alignment entity 322 B, pipe 535 B, structure 396 B — every `maximum` below keeps
items x bytes under ~60 KB, and SeedLibraryTests pins those maximums.
Run it after editing a seed body here, then `dotnet test HPCivil3d.Mcp.Server.Tests` and the live smoke harness.
"""
import io, os, json

root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "HPCivil3d.Mcp.Server", "Registry", "SeedLibrary")


def seed(category, name, tool, code, examples):
    d = os.path.join(root, category, name)
    os.makedirs(d, exist_ok=True)
    base = {"name": name, "version": 1, "status": "published", "author": "hprebar", "category": category,
            "host": "civil3d", "hostVersions": ["2026"]}
    base.update(tool)
    io.open(os.path.join(d, "tool.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(base, indent=2, ensure_ascii=False) + "\n")
    io.open(os.path.join(d, "code.cs"), "w", encoding="utf-8", newline="\n").write(code.strip("\n") + "\n")
    io.open(os.path.join(d, "examples.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(examples, indent=2, ensure_ascii=False) + "\n")
    print("ok", category, name, len(code.strip("\n").split("\n")), "lines")


def limit(default, maximum=500):
    return {"type": "integer", "default": default, "minimum": 1, "maximum": maximum, "description": f"Maximum number of items to return (1–{maximum})"}


OFFSET = {"type": "integer", "default": 0, "minimum": 0, "description": "Skip this many items first (paging, in the listed order)"}
NAME_PATTERN = {"type": "string", "default": "*", "description": "Name filter with * and ? wildcards, case-insensitive; * = all"}

# Shared script fragments. Civil and AutoCAD both define Entity / DBObject / Surface, so the Civil ones are fully qualified.
LIKE = r'''
bool Like(string text, string pattern) => string.IsNullOrEmpty(pattern) || pattern == "*" || System.Text.RegularExpressions.Regex.IsMatch(text ?? "",
    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);'''

ERR = r'''
var errors = new List<object>();
void Fail(string code, string message, string handle) => errors.Add(new { code, message, handle });
string Classify(Exception ex) => ex is Autodesk.AutoCAD.Runtime.Exception acad ? acad.ErrorStatus.ToString() : ex.GetType().Name;'''

RESOLVE = r'''
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
}'''

# ---------------------------------------------------------------------------------------------------------------------- R1
seed("Document", "get_civil_document_info", {
    "transaction": "none", "timeoutSeconds": 30, "destructive": False,
    "tags": ["seed", "civil3d", "document", "units", "query"],
    "title": "Get Civil document info",
    "description": "Overview of the active Civil 3D drawing: product, whether it is a Civil document, the Civil drawing unit (Meters or Feet — every station, elevation and area the other tools return is in this unit; plan geometry crosses the tool boundary in mm), the coordinate system (code, description, datum, unit; absent without a zone), INSUNITS and whether it disagrees with the Civil unit (insunitsMismatch — a drawing without Civil settings reports Feet), one count per object family (alignments, siteless alignments, sites, parcels, surfaces, corridors, pipe networks, pressure networks, COGO points, point groups) and up to 20 style names per family. Read-only. Call it first.",
    "inputSchema": {"type": "object", "properties": {
        "includeStyles": {"type": "boolean", "default": True, "description": "List up to 20 style names per family (alignment, profile, surface, point, parcel, pipe, structure, corridor)"}},
        "additionalProperties": False}},
r'''
bool includeStyles = args.Bool("includeStyles", true);
var warnings = new List<string>();
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document; open it in Civil 3D.");
var unitZone = civil.Settings.DrawingSettings.UnitZoneSettings;
string drawingUnit = unitZone.DrawingUnits.ToString();
string code = unitZone.CoordinateSystemCode;
object coordinateSystem = null;
if (!string.IsNullOrWhiteSpace(code) && code != ".")
{
    try
    {
        var cs = Autodesk.Civil.Settings.SettingsUnitZone.GetCoordinateSystemByCode(code);
        coordinateSystem = new { code = cs.Code, description = cs.Description, datum = cs.Datum, unit = cs.Unit, projection = cs.Projection, category = cs.Category };
    }
    catch (Exception ex) { coordinateSystem = new { code }; warnings.Add("coordinate system '" + code + "' could not be described: " + ex.Message); }
}
bool insunitsMismatch = units.Note != null;
if (insunitsMismatch) warnings.Add(units.Note);

int parcels = 0;
foreach (ObjectId siteId in civil.GetSiteIds()) parcels += ((Site)tr.GetObject(siteId, OpenMode.ForRead)).GetParcelIds().Count;
var counts = new
{
    alignments = civil.GetAlignmentIds().Count, sitelessAlignments = civil.GetSitelessAlignmentIds().Count, sites = civil.GetSiteIds().Count, parcels,
    surfaces = civil.GetSurfaceIds().Count, corridors = civil.CorridorCollection.Count, pipeNetworks = civil.GetPipeNetworkIds().Count,
    pressureNetworks = Autodesk.Civil.ApplicationServices.CivilDocumentPressurePipesExtension.GetPressurePipeNetworkIds(civil).Count,
    cogoPoints = (int)civil.CogoPoints.Count, pointGroups = civil.PointGroups.Count,
};

List<string> Names(TreeNodeCollectionBase styles)
{
    var names = new List<string>();
    foreach (ObjectId id in styles) { if (names.Count >= 20) break; ct.ThrowIfCancellationRequested(); names.Add(((StyleBase)tr.GetObject(id, OpenMode.ForRead)).Name); }
    return names;
}
object styles = null;
if (includeStyles)
{
    var s = civil.Styles;
    styles = new { alignment = Names(s.AlignmentStyles), profile = Names(s.ProfileStyles), surface = Names(s.SurfaceStyles), point = Names(s.PointStyles),
                   parcel = Names(s.ParcelStyles), pipe = Names(s.PipeStyles), structure = Names(s.StructureStyles), corridor = Names(s.CorridorStyles) };
}
log($"{drawingUnit}; {counts.alignments} alignments, {counts.surfaces} surfaces, {counts.corridors} corridors, {counts.pipeNetworks} pipe networks, {counts.cogoPoints} COGO points");
return new
{
    success = true, summary = $"Civil 3D drawing in {drawingUnit}: {counts.alignments} alignments, {counts.surfaces} surfaces, {counts.corridors} corridors, {counts.pipeNetworks} pipe networks, {parcels} parcels, {counts.cogoPoints} COGO points",
    product = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveProduct.ToString(), isCivilDocument = true, drawingUnit, lengthUnit = "mm", coordinateSystem,
    insunits = db.Insunits.ToString(), insunitsMismatch, imperialToMetric = unitZone.ImperialToMetricConversion.ToString(), drawingScale = unitZone.DrawingScale,
    counts, styles, warnings,
};
''', [
    {"title": "Document overview with style names", "args": {}},
    {"title": "Counts and units only", "args": {"includeStyles": False}},
])

# ---------------------------------------------------------------------------------------------------------------------- R2
seed("Alignment", "list_alignments", {
    "transaction": "none", "timeoutSeconds": 30, "destructive": False,
    "tags": ["seed", "civil3d", "alignment", "query"],
    "title": "List alignments",
    "description": "Lists the alignments of the active Civil 3D drawing in handle order: handle, name, type, length in mm, start/end station (drawing units, raw and as formatted labels with equations), site (empty = siteless), style, profile count, entity count, description. Filter by name wildcard and site; page with limit/offset. Read-only.",
    "inputSchema": {"type": "object", "properties": {
        "namePattern": NAME_PATTERN,
        "site": {"type": "string", "description": "Site name to restrict to; \"\" = siteless alignments only; omit = every alignment"},
        "limit": limit(100, 180), "offset": OFFSET},
        "additionalProperties": False}},
r'''
string namePattern = args.Str("namePattern", "*");
string site = args.Str("site");
bool filterSite = args.Has("site");
int limit = args.Int("limit", 100);
int offset = args.Int("offset", 0);
if (limit <= 0 || limit > 180) throw new ArgumentException("limit must be 1–180.");
if (offset < 0) throw new ArgumentException("offset must be >= 0.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");''' + LIKE + ERR + r'''

var items = new List<object>();
int matched = 0;
foreach (ObjectId id in civil.GetAlignmentIds())
{
    ct.ThrowIfCancellationRequested();
    try
    {
        var a = (Alignment)tr.GetObject(id, OpenMode.ForRead);
        if (!Like(a.Name, namePattern)) continue;
        if (filterSite && !string.Equals(a.IsSiteless ? "" : a.SiteName, site, StringComparison.OrdinalIgnoreCase)) continue;
        matched++;
        if (matched <= offset) continue;
        if (items.Count >= limit) break;
        items.Add(new
        {
            handle = id.Handle.ToString(), name = a.Name, type = a.AlignmentType.ToString(), lengthMm = Math.Round(units.ToMm(a.Length), 1),
            startStation = Math.Round(a.StartingStation, 4), endStation = Math.Round(a.EndingStation, 4),
            startStationLabel = a.GetStationStringWithEquations(a.StartingStation), endStationLabel = a.GetStationStringWithEquations(a.EndingStation),
            site = a.IsSiteless ? "" : a.SiteName, isSiteless = a.IsSiteless, style = a.StyleName, profileCount = a.GetProfileIds().Count, entityCount = a.Entities.Count, description = a.Description,
        });
    }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
}
bool truncated = matched > offset + items.Count;
log($"{items.Count} alignments (matched {matched}, total {civil.GetAlignmentIds().Count})");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {matched} matching alignments", count = items.Count, total = civil.GetAlignmentIds().Count, offset, truncated, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors };
''', [
    {"title": "All alignments", "args": {}},
    {"title": "Road centerlines only", "args": {"namePattern": "*Center*", "limit": 50}},
    {"title": "Siteless alignments", "args": {"site": ""}},
])

# ---------------------------------------------------------------------------------------------------------------------- R3
seed("Alignment", "get_alignment_geometry", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["seed", "civil3d", "alignment", "geometry", "query"],
    "title": "Get alignment geometry",
    "description": "Horizontal geometry of one alignment (by name or handle): its entities in order (paged by entityLimit/entityOffset) with type, start/end station (drawing units), length in mm, start/end point in mm, and for arcs radius (mm), centre (mm), direction and delta (radians), PI station; optional samples along the alignment every sampleStepMm (station in drawing units, x/y in mm, at most maxSamples). Read-only.",
    "inputSchema": {"type": "object", "properties": {
        "alignment": {"type": "string", "description": "Alignment name or handle (required)"},
        "entityLimit": {"type": "integer", "default": 100, "minimum": 1, "maximum": 150, "description": "Entities listed per call (1–150); page with entityOffset"},
        "entityOffset": {"type": "integer", "default": 0, "minimum": 0, "description": "Skip this many entities first (in alignment order)"},
        "sampleStepMm": {"type": "number", "default": 0, "minimum": 0, "description": "Sample points along the alignment every this many mm (0 = no samples; at least 1000 mm)"},
        "maxSamples": {"type": "integer", "default": 100, "minimum": 1, "maximum": 200, "description": "Cap on the number of samples (1–200)"}},
        "required": ["alignment"], "additionalProperties": False}},
r'''
string key = args.Require("alignment");
int entityLimit = args.Int("entityLimit", 100);
int entityOffset = args.Int("entityOffset", 0);
double sampleStepMm = args.Double("sampleStepMm", 0);
int maxSamples = args.Int("maxSamples", 100);
if (entityLimit <= 0 || entityLimit > 150) throw new ArgumentException("entityLimit must be 1–150.");
if (entityOffset < 0) throw new ArgumentException("entityOffset must be >= 0.");
if (sampleStepMm < 0 || (sampleStepMm > 0 && sampleStepMm < 1000)) throw new ArgumentException("sampleStepMm must be 0 (no samples) or at least 1000 mm.");
if (maxSamples <= 0 || maxSamples > 200) throw new ArgumentException("maxSamples must be 1–200.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");''' + RESOLVE + ERR + r'''

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
''', [
    {"title": "Geometry of one alignment", "args": {"alignment": "Alignment - (1)"}},
    {"title": "With samples every 50 m", "args": {"alignment": "Alignment - (1)", "sampleStepMm": 50000, "maxSamples": 100}},
    {"title": "Second page of a long alignment", "args": {"alignment": "Alignment - (1)", "entityLimit": 100, "entityOffset": 100}},
])

# ---------------------------------------------------------------------------------------------------------------------- R4
seed("Profile", "list_profiles", {
    "transaction": "none", "timeoutSeconds": 30, "destructive": False,
    "tags": ["seed", "civil3d", "profile", "query"],
    "title": "List profiles",
    "description": "Lists the vertical profiles of one alignment (name or handle) or of every alignment: handle, parent alignment, name, profile type (surface / layout), start/end station, min/max elevation (drawing units), length in mm, PVI count, entity count, style. Read-only.",
    "inputSchema": {"type": "object", "properties": {
        "alignment": {"type": "string", "description": "Alignment name or handle; omit for every alignment"},
        "limit": limit(100, 180)},
        "additionalProperties": False}},
r'''
string key = args.Str("alignment");
int limit = args.Int("limit", 100);
if (limit <= 0 || limit > 180) throw new ArgumentException("limit must be 1–180.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");''' + RESOLVE + ERR + r'''

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
''', [
    {"title": "Profiles of every alignment", "args": {}},
    {"title": "Profiles of one alignment", "args": {"alignment": "Alignment - (1)", "limit": 20}},
])

# ---------------------------------------------------------------------------------------------------------------------- R5
seed("Surface", "list_surfaces", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["seed", "civil3d", "surface", "query"],
    "title": "List surfaces",
    "description": "Lists the surfaces of the active Civil 3D drawing: handle, name, kind (TinSurface, GridSurface, TinVolumeSurface, …), description, out-of-date and auto-rebuild flags, style, point count, min/mean/max elevation (drawing units), bounds in mm, and for TIN surfaces 2D/3D area (drawing units squared) and max slope. Read-only; never rebuilds.",
    "inputSchema": {"type": "object", "properties": {"namePattern": NAME_PATTERN, "limit": limit(100)}, "additionalProperties": False}},
r'''
string namePattern = args.Str("namePattern", "*");
int limit = args.Int("limit", 100);
if (limit <= 0 || limit > 500) throw new ArgumentException("limit must be 1–500.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");''' + LIKE + ERR + r'''

var items = new List<object>();
int matched = 0;
foreach (ObjectId id in civil.GetSurfaceIds())
{
    ct.ThrowIfCancellationRequested();
    try
    {
        var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(id, OpenMode.ForRead);
        if (!Like(s.Name, namePattern)) continue;
        matched++;
        if (items.Count >= limit) continue;
        var g = s.GetGeneralProperties();
        object area2D = null, area3D = null, slopeMax = null;
        if (s is TinSurface tin)
        {
            try { var t = tin.GetTerrainProperties(); area2D = t.SurfaceArea2D; area3D = t.SurfaceArea3D; slopeMax = t.MaximumGradeOrSlope; }
            catch (Exception ex) { Fail(Classify(ex), "terrain properties: " + ex.Message, id.Handle.ToString()); }
        }
        items.Add(new
        {
            handle = id.Handle.ToString(), name = s.Name, surfaceType = s.GetType().Name, description = s.Description, isOutOfDate = s.IsOutOfDate, autoRebuild = s.AutoRebuild, style = s.StyleName,
            pointCount = g.NumberOfPoints, elevationMin = g.MinimumElevation, elevationMax = g.MaximumElevation, elevationMean = g.MeanElevation,
            boundsMm = new { minX = Math.Round(units.ToMm(g.MinimumCoordinateX), 1), minY = Math.Round(units.ToMm(g.MinimumCoordinateY), 1), maxX = Math.Round(units.ToMm(g.MaximumCoordinateX), 1), maxY = Math.Round(units.ToMm(g.MaximumCoordinateY), 1) },
            area2D, area3D, slopeMax,
        });
    }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
}
log($"{items.Count} surfaces");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {matched} matching surfaces (areas in {units.Label}²)", count = items.Count, truncated = matched > items.Count, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors };
''', [
    {"title": "All surfaces", "args": {}},
    {"title": "Existing ground surfaces", "args": {"namePattern": "EG*", "limit": 10}},
])

# ---------------------------------------------------------------------------------------------------------------------- R6
seed("Surface", "get_surface_elevation", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["seed", "civil3d", "surface", "elevation", "query"],
    "title": "Get surface elevation at points",
    "description": "Elevation of one surface (name or handle) at up to 500 plan points given in mm ({x, y}); each item reports ok with the elevation in drawing units, or ok=false with the reason (OUTSIDE_SURFACE when the point is off the surface — the other points still answer). Read-only.",
    "inputSchema": {"type": "object", "properties": {
        "surface": {"type": "string", "description": "Surface name or handle (required)"},
        "points": {"type": "array", "minItems": 1, "maxItems": 500, "description": "Plan points in millimetres",
                   "items": {"type": "object", "properties": {"x": {"type": "number"}, "y": {"type": "number"}}, "required": ["x", "y"]}}},
        "required": ["surface", "points"], "additionalProperties": False}},
r'''
string key = args.Require("surface");
var points = args.List("points");
if (points.Count == 0 || points.Count > 500) throw new ArgumentException("points needs 1–500 entries of {x, y} in millimetres.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");''' + RESOLVE + r'''

var id = Resolve(key, civil.GetSurfaceIds(), "surface");
var surface = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(id, OpenMode.ForRead);
var items = new List<object>();
int okCount = 0, outsideCount = 0, failedCount = 0;
for (int i = 0; i < points.Count; i++)
{
    ct.ThrowIfCancellationRequested();
    if (!points[i].Has("x") || !points[i].Has("y")) throw new ArgumentException($"points[{i}] must be an object {{x, y}} in millimetres.");
    double x = points[i].Double("x"), y = points[i].Double("y");
    try
    {
        double elevation = surface.FindElevationAtXY(units.ToDrawing(x), units.ToDrawing(y));
        okCount++;
        items.Add(new { index = i, x, y, ok = true, elevation = Math.Round(elevation, 4) });
    }
    catch (PointNotOnEntityException ex)
    {
        outsideCount++;
        items.Add(new { index = i, x, y, ok = false, error = "OUTSIDE_SURFACE", message = ex.Message });
    }
    catch (Exception ex)
    {
        failedCount++;
        items.Add(new { index = i, x, y, ok = false, error = ex.GetType().Name, message = ex.Message });
    }
}
log($"{surface.Name}: {okCount} on the surface, {outsideCount} outside, {failedCount} failed");
return new { success = failedCount == 0, summary = $"{surface.Name}: {okCount}/{points.Count} points on the surface, {outsideCount} outside", surface = new { handle = id.Handle.ToString(), name = surface.Name }, count = items.Count, okCount, outsideCount, failedCount, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors = new List<object>() };
''', [
    {"title": "One point", "args": {"surface": "EG", "points": [{"x": 312450000, "y": 23872000}]}},
    {"title": "Grid of three points", "args": {"surface": "Existing Ground", "points": [{"x": 5300000, "y": 4166000}, {"x": 5350000, "y": 4166000}, {"x": 5400000, "y": 4166000}]}},
])

# ---------------------------------------------------------------------------------------------------------------------- R7
seed("Corridor", "list_corridors", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["seed", "civil3d", "corridor", "query"],
    "title": "List corridors",
    "description": "Lists the corridors of the active Civil 3D drawing: handle, name, out-of-date and rebuild-automatic flags, code set style, baselines (name, alignment, profile, region count) and corridor surfaces (name, linked surface). Read-only; a corridor is never rebuilt by this tool.",
    "inputSchema": {"type": "object", "properties": {"namePattern": NAME_PATTERN, "limit": limit(50)}, "additionalProperties": False}},
r'''
string namePattern = args.Str("namePattern", "*");
int limit = args.Int("limit", 50);
if (limit <= 0 || limit > 500) throw new ArgumentException("limit must be 1–500.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");''' + LIKE + ERR + r'''

string NameOf(ObjectId id) { if (id.IsNull) return null; try { return (tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Entity)?.Name; } catch { return null; } }
var items = new List<object>();
int matched = 0;
foreach (ObjectId id in civil.CorridorCollection)
{
    ct.ThrowIfCancellationRequested();
    try
    {
        var c = (Corridor)tr.GetObject(id, OpenMode.ForRead);
        if (!Like(c.Name, namePattern)) continue;
        matched++;
        if (items.Count >= limit) continue;
        var baselines = new List<object>();
        foreach (Baseline b in c.Baselines) baselines.Add(new { name = b.Name, alignment = NameOf(b.AlignmentId), profile = NameOf(b.ProfileId), regionCount = b.BaselineRegions.Count });
        var surfaces = new List<object>();
        foreach (CorridorSurface s in c.CorridorSurfaces) surfaces.Add(new { name = s.Name, surface = NameOf(s.SurfaceId) });
        items.Add(new { handle = id.Handle.ToString(), name = c.Name, isOutOfDate = c.IsOutOfDate, rebuildAutomatic = c.RebuildAutomatic, codeSetStyle = c.CodeSetStyleName, baselines, surfaces });
    }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
}
log($"{items.Count} corridors");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {matched} matching corridors", count = items.Count, truncated = matched > items.Count, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors };
''', [
    {"title": "All corridors", "args": {}},
    {"title": "Corridors by name", "args": {"namePattern": "Corridor*", "limit": 10}},
])

# ---------------------------------------------------------------------------------------------------------------------- R8
seed("Pipe", "list_pipe_networks", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["seed", "civil3d", "pipe", "network", "query"],
    "title": "List pipe networks",
    "description": "Lists the gravity pipe networks (handle, name, reference alignment and surface, parts list, pipe and structure counts) and, with includeParts, the pipes (family, size, start/end in mm with elevation in drawing units, slope, 2D/3D length in mm, inner diameter in mm, shape, flow direction, start/end structure, minimum cover in mm) and structures (family, size, position, rim/sump elevation in drawing units, sump depth, height and inner diameter in mm, connected pipes) — partLimit is the total number of parts listed across the answer (default 60, max 100, so a page stays under 64 KB); a network cut short reports partsTruncated, and networks past the budget list none; pressure networks are summarised by counts. Read-only.",
    "inputSchema": {"type": "object", "properties": {
        "namePattern": NAME_PATTERN,
        "includeParts": {"type": "boolean", "default": False, "description": "Also list the pipes and structures of each network (up to partLimit of each per network)"},
        "limit": limit(50),
        "partLimit": {"type": "integer", "default": 60, "minimum": 1, "maximum": 100, "description": "Total budget of parts (pipes + structures) listed across the whole answer (1–100); networks past the budget report partsTruncated"}},
        "additionalProperties": False}},
r'''
string namePattern = args.Str("namePattern", "*");
bool includeParts = args.Bool("includeParts", false);
int limit = args.Int("limit", 50);
int partLimit = args.Int("partLimit", 200);
if (limit <= 0 || limit > 500) throw new ArgumentException("limit must be 1–500.");
if (partLimit <= 0 || partLimit > 100) throw new ArgumentException("partLimit must be 1–100.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");''' + LIKE + ERR + r'''

string NameOf(ObjectId id) { if (id.IsNull) return null; try { return (tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Entity)?.Name; } catch { return null; } }
object Pt(Point3d p) => new { x = Math.Round(units.ToMm(p.X), 1), y = Math.Round(units.ToMm(p.Y), 1), z = Math.Round(p.Z, 4) };
double Mm(double du) => Math.Round(units.ToMm(du), 1);
bool truncated = false;
int partBudget = partLimit;
var items = new List<object>();
int matched = 0;
foreach (ObjectId id in civil.GetPipeNetworkIds())
{
    ct.ThrowIfCancellationRequested();
    try
    {
        var n = (Network)tr.GetObject(id, OpenMode.ForRead);
        if (!Like(n.Name, namePattern)) continue;
        matched++;
        if (items.Count >= limit) { truncated = true; continue; }
        var pipeIds = n.GetPipeIds(); var structureIds = n.GetStructureIds();
        List<object> pipes = null, structures = null;
        bool partsTruncated = false;
        if (includeParts)
        {
            pipes = new List<object>(); structures = new List<object>();
            foreach (ObjectId pid in pipeIds)
            {
                ct.ThrowIfCancellationRequested();
                if (partBudget <= 0) { partsTruncated = true; break; }
                partBudget--;
                try
                {
                    var p = (Pipe)tr.GetObject(pid, OpenMode.ForRead);
                    pipes.Add(new { handle = pid.Handle.ToString(), name = p.Name, family = p.PartFamilyName, size = p.PartSizeName, start = Pt(p.StartPoint), end = Pt(p.EndPoint), slope = Math.Round(p.Slope, 6), slopePercent = Math.Round(p.Slope * 100, 3),
                                    length2DMm = Mm(p.Length2DCenterToCenter), length3DMm = Mm(p.Length3D), innerDiameterMm = Mm(p.InnerDiameterOrWidth), innerHeightMm = Mm(p.InnerHeight), shape = p.CrossSectionalShape.ToString(), flowDirection = p.FlowDirection.ToString(),
                                    startStructure = NameOf(p.StartStructureId), endStructure = NameOf(p.EndStructureId), minCoverMm = Mm(p.MinimumCover) });
                }
                catch (Exception ex) { Fail(Classify(ex), ex.Message, pid.Handle.ToString()); }
            }
            foreach (ObjectId sid in structureIds)
            {
                ct.ThrowIfCancellationRequested();
                if (partBudget <= 0) { partsTruncated = true; break; }
                partBudget--;
                try
                {
                    var s = (Structure)tr.GetObject(sid, OpenMode.ForRead);
                    structures.Add(new { handle = sid.Handle.ToString(), name = s.Name, family = s.PartFamilyName, size = s.PartSizeName, position = Pt(s.Position), rimElevation = Math.Round(s.RimElevation, 4), sumpElevation = Math.Round(s.SumpElevation, 4),
                                         sumpDepthMm = Mm(s.SumpDepth), heightMm = Mm(s.Height), innerDiameterMm = Mm(s.InnerDiameterOrWidth), connectedPipes = s.ConnectedPipesCount });
                }
                catch (Exception ex) { Fail(Classify(ex), ex.Message, sid.Handle.ToString()); }
            }
        }
        if (partsTruncated) truncated = true;
        items.Add(new { handle = id.Handle.ToString(), name = n.Name, referenceAlignment = n.ReferenceAlignmentName, referenceSurface = n.ReferenceSurfaceName, partsList = n.PartsListName, pipeCount = pipeIds.Count, structureCount = structureIds.Count, partsTruncated, pipes, structures });
    }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
}
var pressureNetworks = new List<object>();
foreach (ObjectId id in Autodesk.Civil.ApplicationServices.CivilDocumentPressurePipesExtension.GetPressurePipeNetworkIds(civil))
{
    try { var p = (PressurePipeNetwork)tr.GetObject(id, OpenMode.ForRead); pressureNetworks.Add(new { handle = id.Handle.ToString(), name = p.Name, pipeCount = p.GetPipeIds().Count, fittingCount = p.GetFittingIds().Count, appurtenanceCount = p.GetAppurtenanceIds().Count }); }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
}
log($"{items.Count} pipe networks, {pressureNetworks.Count} pressure networks");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {matched} matching pipe networks, {pressureNetworks.Count} pressure networks (elevations in {units.Label})", count = items.Count, truncated, drawingUnit = units.Label, lengthUnit = "mm", items, pressureNetworks, warnings = new List<string>(), errors };
''', [
    {"title": "Networks only", "args": {}},
    {"title": "One network with its parts", "args": {"namePattern": "Storm*", "includeParts": True, "partLimit": 100}},
    {"title": "Every network, first 20 parts overall", "args": {"includeParts": True, "partLimit": 20}},
])

# ---------------------------------------------------------------------------------------------------------------------- R9
seed("Parcel", "list_parcels", {
    "transaction": "none", "timeoutSeconds": 30, "destructive": False,
    "tags": ["seed", "civil3d", "parcel", "site", "query"],
    "title": "List parcels",
    "description": "Lists the parcels of every site (or of one site) in the active Civil 3D drawing: handle, site, name, number, tax id, address, style, description, centroid in mm and area in drawing units squared (m² for Meters, ft² for Feet). Filter by name wildcard; page with limit. Read-only.",
    "inputSchema": {"type": "object", "properties": {
        "site": {"type": "string", "description": "Site name to restrict to; omit for every site"},
        "namePattern": NAME_PATTERN, "limit": limit(100, 250)},
        "additionalProperties": False}},
r'''
string site = args.Str("site");
string namePattern = args.Str("namePattern", "*");
int limit = args.Int("limit", 100);
if (limit <= 0 || limit > 250) throw new ArgumentException("limit must be 1–250.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");''' + LIKE + ERR + r'''

var items = new List<object>();
int matched = 0, sites = 0;
foreach (ObjectId siteId in civil.GetSiteIds())
{
    ct.ThrowIfCancellationRequested();
    var s = (Site)tr.GetObject(siteId, OpenMode.ForRead);
    if (!string.IsNullOrWhiteSpace(site) && !string.Equals(s.Name, site, StringComparison.OrdinalIgnoreCase)) continue;
    sites++;
    foreach (ObjectId id in s.GetParcelIds())
    {
        try
        {
            var p = (Parcel)tr.GetObject(id, OpenMode.ForRead);
            if (!Like(p.Name, namePattern)) continue;
            matched++;
            if (items.Count >= limit) continue;
            items.Add(new { handle = id.Handle.ToString(), site = s.Name, name = p.Name, number = p.Number, taxId = p.TaxId, address = p.Address, style = p.StyleName, description = p.Description,
                            centroid = new { x = Math.Round(units.ToMm(p.Centroid.X), 1), y = Math.Round(units.ToMm(p.Centroid.Y), 1) }, area = Math.Round(p.Area, 3) });
        }
        catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
    }
}
if (!string.IsNullOrWhiteSpace(site) && sites == 0) throw new ArgumentException($"No site named '{site}' in this drawing.");
log($"{items.Count} parcels in {sites} site(s)");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {matched} matching parcels in {sites} site(s) (area in {units.Label}²)", count = items.Count, truncated = matched > items.Count, drawingUnit = units.Label, areaUnit = units.Label + "²", lengthUnit = "mm", items, warnings = new List<string>(), errors };
''', [
    {"title": "All parcels", "args": {}},
    {"title": "Single-family lots of one site", "args": {"site": "Site 1", "namePattern": "Single-Family*", "limit": 50}},
])

# ---------------------------------------------------------------------------------------------------------------------- R10
seed("Point", "list_cogo_points", {
    "transaction": "none", "timeoutSeconds": 60, "destructive": False,
    "tags": ["seed", "civil3d", "cogo", "point", "query"],
    "title": "List COGO points",
    "description": "Lists the COGO points of the active Civil 3D drawing: handle, point number, name, x/y (easting/northing) in mm, elevation in drawing units, raw and full description, primary point group. Filter by primary point group (must exist), description wildcard and number range (a range of at most 5 000 numbers is looked up directly instead of scanning every point); page with limit/offset in point-number order. Read-only.",
    "inputSchema": {"type": "object", "properties": {
        "pointGroup": {"type": "string", "description": "Primary point group name to restrict to (must exist in the drawing)"},
        "descriptionPattern": {"type": "string", "default": "*", "description": "Wildcard on the full description (* and ?), case-insensitive"},
        "numberFrom": {"type": "integer", "minimum": 0, "description": "Lowest point number to include"},
        "numberTo": {"type": "integer", "minimum": 0, "description": "Highest point number to include"},
        "limit": limit(200, 300), "offset": OFFSET},
        "additionalProperties": False}},
r'''
string pointGroup = args.Str("pointGroup");
string descriptionPattern = args.Str("descriptionPattern", "*");
long numberFrom = args.Long("numberFrom", -1);
long numberTo = args.Long("numberTo", -1);
int limit = args.Int("limit", 200);
int offset = args.Int("offset", 0);
if (limit <= 0 || limit > 300) throw new ArgumentException("limit must be 1–300.");
if (offset < 0) throw new ArgumentException("offset must be >= 0.");
if (numberFrom >= 0 && numberTo >= 0 && numberTo < numberFrom) throw new ArgumentException("numberTo must be >= numberFrom.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
if (!string.IsNullOrWhiteSpace(pointGroup) && !civil.PointGroups.Contains(pointGroup)) throw new ArgumentException($"No point group named '{pointGroup}' in this drawing.");''' + LIKE + ERR + r'''

var groupNames = new Dictionary<ObjectId, string>();
string GroupName(ObjectId id)
{
    if (id.IsNull) return null;
    if (!groupNames.TryGetValue(id, out var name)) { try { name = ((PointGroup)tr.GetObject(id, OpenMode.ForRead)).Name; } catch { name = null; } groupNames[id] = name; }
    return name;
}
// a narrow number range is looked up by number; otherwise every point is scanned (the collection has no index by anything else)
IEnumerable<ObjectId> Candidates()
{
    if (numberFrom >= 0 && numberTo >= 0 && numberTo - numberFrom <= 5000)
    {
        for (long n = numberFrom; n <= numberTo; n++) if (civil.CogoPoints.Contains((uint)n)) yield return civil.CogoPoints.GetPointByPointNumber((uint)n);
    }
    else foreach (ObjectId id in civil.GetAllPointIds()) yield return id;
}
var rows = new List<CogoPoint>();
foreach (ObjectId id in Candidates())
{
    ct.ThrowIfCancellationRequested();
    try
    {
        var p = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
        if (numberFrom >= 0 && p.PointNumber < numberFrom) continue;
        if (numberTo >= 0 && p.PointNumber > numberTo) continue;
        if (!Like(p.FullDescription, descriptionPattern)) continue;
        if (!string.IsNullOrWhiteSpace(pointGroup) && !string.Equals(GroupName(p.PrimaryPointGroupId), pointGroup, StringComparison.OrdinalIgnoreCase)) continue;
        rows.Add(p);
    }
    catch (Exception ex) { Fail(Classify(ex), ex.Message, id.Handle.ToString()); }
}
var items = rows.OrderBy(p => p.PointNumber).Skip(offset).Take(limit).Select(p => new
{
    handle = p.Handle.ToString(), number = p.PointNumber, name = p.PointName, x = Math.Round(units.ToMm(p.Easting), 1), y = Math.Round(units.ToMm(p.Northing), 1), elevation = Math.Round(p.Elevation, 4),
    rawDescription = p.RawDescription, fullDescription = p.FullDescription, pointGroup = GroupName(p.PrimaryPointGroupId),
}).ToList();
log($"{items.Count} of {rows.Count} matching COGO points (total {civil.CogoPoints.Count})");
return new { success = errors.Count == 0 || items.Count > 0, summary = $"{items.Count} of {rows.Count} matching COGO points, {civil.CogoPoints.Count} in the drawing", count = items.Count, total = (int)civil.CogoPoints.Count, offset, truncated = rows.Count > offset + items.Count, drawingUnit = units.Label, lengthUnit = "mm", items, warnings = new List<string>(), errors };
''', [
    {"title": "First 200 points", "args": {}},
    {"title": "Points 100–199 described as trees", "args": {"numberFrom": 100, "numberTo": 199, "descriptionPattern": "*TREE*", "limit": 100}},
])

# ---------------------------------------------------------------------------------------------------------------------- W1
seed("Point", "create_cogo_points", {
    "transaction": "auto", "timeoutSeconds": 60, "destructive": True,
    "tags": ["seed", "civil3d", "cogo", "point", "create"],
    "title": "Create COGO points",
    "description": "Creates COGO points from a list of {x, y, elevation?, description?, name?} — x/y in mm, elevation in drawing units (0 when omitted) — numbered by the drawing's next-point-number setting; each item may carry its own description (default: the tool's description argument) and point name. Side effects: adds one CogoPoint per item to the drawing's point collection. dryRun creates them and rolls back, reporting the same envelope: createdCount, items[{index, handle, number}], affectedHandles.",
    "inputSchema": {"type": "object", "properties": {
        "points": {"type": "array", "minItems": 1, "maxItems": 500, "description": "Points to create: x/y in millimetres, elevation in drawing units",
                   "items": {"type": "object", "properties": {"x": {"type": "number"}, "y": {"type": "number"}, "elevation": {"type": "number", "description": "Elevation in drawing units; omitted = 0"},
                                                              "description": {"type": "string", "description": "Raw description for this point (default: the tool's description)"},
                                                              "name": {"type": "string", "description": "Point name (optional)"}},
                             "required": ["x", "y"]}},
        "description": {"type": "string", "default": "MCP", "description": "Raw description for points without their own"}},
        "required": ["points"], "additionalProperties": False}},
r'''
var points = args.List("points");
string defaultDescription = args.Str("description", "MCP");
if (points.Count == 0 || points.Count > 500) throw new ArgumentException("points needs 1–500 entries of {x, y, elevation?} (x/y in millimetres, elevation in drawing units).");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
for (int i = 0; i < points.Count; i++)
{
    if (!points[i].Has("x") || !points[i].Has("y")) throw new ArgumentException($"points[{i}] must be an object {{x, y}} in millimetres (elevation optional, drawing units).");
    string n = points[i].Str("name");
    if (!string.IsNullOrWhiteSpace(n) && !names.Add(n)) throw new ArgumentException($"points[{i}]: the name '{n}' is used twice in this batch.");
}

var items = new List<object>();
var affectedHandles = new List<string>();
uint firstNumber = 0;
for (int i = 0; i < points.Count; i++)
{
    ct.ThrowIfCancellationRequested();
    var p = points[i];
    var location = new Point3d(units.ToDrawing(p.Double("x")), units.ToDrawing(p.Double("y")), p.Double("elevation", 0));
    string description = p.Str("description", defaultDescription);
    ObjectId id = civil.CogoPoints.Add(location, description, true);
    string name = p.Str("name");
    if (!string.IsNullOrWhiteSpace(name)) civil.CogoPoints.SetPointName(id, name);
    var created = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
    if (firstNumber == 0) firstNumber = created.PointNumber;
    items.Add(new { index = i, handle = id.Handle.ToString(), number = created.PointNumber, name = created.PointName });
    affectedHandles.Add(id.Handle.ToString());
}
log($"{items.Count} COGO points created (numbers from {firstNumber})");
return new { success = true, summary = $"{items.Count} COGO points created, elevations in {units.Label}", createdCount = items.Count, modifiedCount = 0, deletedCount = 0, drawingUnit = units.Label, lengthUnit = "mm", items, affectedHandles, warnings = new List<string>(), errors = new List<object>() };
''', [
    {"title": "One point with elevation", "args": {"points": [{"x": 1000, "y": 2000, "elevation": 12.5, "description": "MCP TEST"}]}},
    {"title": "Three named points", "args": {"points": [{"x": 0, "y": 0, "elevation": 10, "name": "A"}, {"x": 5000, "y": 0, "elevation": 10.5, "name": "B"}, {"x": 5000, "y": 5000, "elevation": 11, "name": "C"}], "description": "SURVEY"}},
])

# ---------------------------------------------------------------------------------------------------------------------- W2
seed("Alignment", "create_alignment_from_polyline", {
    "transaction": "auto", "timeoutSeconds": 60, "destructive": True,
    "tags": ["seed", "civil3d", "alignment", "create"],
    "title": "Create alignment from polyline",
    "description": "Creates a Civil 3D alignment from an existing lightweight polyline (handle) with a unique name: site (empty = siteless), layer (default: the current layer), alignment style and label set (defaults: the drawing's first ones — the label set is mandatory in the API), optional curves between tangents, optional erase of the polyline. Side effects: adds an Alignment (and its labels) to the drawing; with erasePolyline the source polyline is deleted. dryRun creates and rolls back. Returns the alignment's handle, name, length in mm, start/end station in drawing units, entity count, site and style.",
    "inputSchema": {"type": "object", "properties": {
        "polyline": {"type": "string", "description": "Handle of an existing LWPOLYLINE (required)"},
        "name": {"type": "string", "description": "Alignment name, unique in the drawing (required)"},
        "site": {"type": "string", "default": "", "description": "Site name; \"\" = siteless"},
        "layer": {"type": "string", "default": "", "description": "Existing layer name; \"\" = the current layer"},
        "style": {"type": "string", "default": "", "description": "Alignment style name; \"\" = the drawing's first alignment style (whatever the template lists first — pass a name to choose)"},
        "labelSet": {"type": "string", "default": "", "description": "Alignment label set style name; \"\" = the drawing's first label set (the API refuses an empty one)"},
        "addCurvesBetweenTangents": {"type": "boolean", "default": False, "description": "Insert curves between tangents"},
        "erasePolyline": {"type": "boolean", "default": False, "description": "Erase the source polyline"}},
        "required": ["polyline", "name"], "additionalProperties": False}},
r'''
string polylineHandle = args.Require("polyline");
string name = args.Require("name");
string site = args.Str("site", "");
string layer = args.Str("layer", "");
string style = args.Str("style", "");
string labelSet = args.Str("labelSet", "");
bool addCurves = args.Bool("addCurvesBetweenTangents", false);
bool erasePolyline = args.Bool("erasePolyline", false);
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is required.");

ObjectId plineId;
try { plineId = db.GetObjectId(false, new Handle(Convert.ToInt64(polylineHandle, 16)), 0); }
catch (Exception) { throw new ArgumentException($"'{polylineHandle}' is not a valid handle in this drawing."); }
if (plineId.IsNull || plineId.IsErased || !(tr.GetObject(plineId, OpenMode.ForRead) is Polyline)) throw new ArgumentException($"'{polylineHandle}' is not a lightweight polyline (LWPOLYLINE).");

foreach (ObjectId id in civil.GetAlignmentIds())
    if (string.Equals(((Alignment)tr.GetObject(id, OpenMode.ForRead)).Name, name, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"An alignment named '{name}' already exists.");

string FirstStyle(TreeNodeCollectionBase styles, string given, string what)
{
    if (!string.IsNullOrWhiteSpace(given)) { if (!styles.Contains(given)) throw new ArgumentException($"{what} '{given}' does not exist in this drawing."); return given; }
    foreach (ObjectId id in styles) return ((StyleBase)tr.GetObject(id, OpenMode.ForRead)).Name;
    throw new ArgumentException($"The drawing has no {what}; create one in Civil 3D first.");
}
string styleName = FirstStyle(civil.Styles.AlignmentStyles, style, "alignment style");
string labelSetName = FirstStyle(civil.Styles.LabelSetStyles.AlignmentLabelSetStyles, labelSet, "alignment label set");
if (string.IsNullOrWhiteSpace(layer)) layer = ((LayerTableRecord)tr.GetObject(db.Clayer, OpenMode.ForRead)).Name;
else if (!((LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead)).Has(layer)) throw new ArgumentException($"Layer '{layer}' does not exist in this drawing.");
if (!string.IsNullOrWhiteSpace(site))
{
    bool found = false;
    foreach (ObjectId sid in civil.GetSiteIds()) if (string.Equals(((Site)tr.GetObject(sid, OpenMode.ForRead)).Name, site, StringComparison.OrdinalIgnoreCase)) found = true;
    if (!found) throw new ArgumentException($"Site '{site}' does not exist; use \"\" for a siteless alignment.");
}

var options = new PolylineOptions { PlineId = plineId, AddCurvesBetweenTangents = addCurves, EraseExistingEntities = erasePolyline };
ObjectId alignmentId = Alignment.Create(civil, options, name, site, layer, styleName, labelSetName);
var a = (Alignment)tr.GetObject(alignmentId, OpenMode.ForRead);
log($"alignment {a.Name} ({alignmentId.Handle}) from polyline {polylineHandle}: {a.Entities.Count} entities, {Math.Round(units.ToMm(a.Length), 1)} mm");
return new
{
    success = true, summary = $"Alignment '{a.Name}' created: {a.Entities.Count} entities, {Math.Round(units.ToMm(a.Length) / 1000, 2)} m, stations {a.StartingStation}–{a.EndingStation} {units.Label}",
    createdCount = 1, modifiedCount = 0, deletedCount = erasePolyline ? 1 : 0, drawingUnit = units.Label, lengthUnit = "mm",
    alignment = new { handle = alignmentId.Handle.ToString(), name = a.Name, lengthMm = Math.Round(units.ToMm(a.Length), 1), startStation = a.StartingStation, endStation = a.EndingStation, entityCount = a.Entities.Count, site = a.IsSiteless ? "" : a.SiteName, style = a.StyleName, labelSet = labelSetName, layer },
    affectedHandles = erasePolyline ? new List<string> { alignmentId.Handle.ToString(), polylineHandle } : new List<string> { alignmentId.Handle.ToString() }, warnings = new List<string>(), errors = new List<object>(),
};
''', [
    {"title": "Siteless alignment with defaults", "args": {"polyline": "5F3E", "name": "MCP Road A"}},
    {"title": "In a site, with curves, erasing the polyline", "args": {"polyline": "5F3E", "name": "MCP Road B", "site": "Site 1", "style": "Local Road", "addCurvesBetweenTangents": True, "erasePolyline": True}},
])
