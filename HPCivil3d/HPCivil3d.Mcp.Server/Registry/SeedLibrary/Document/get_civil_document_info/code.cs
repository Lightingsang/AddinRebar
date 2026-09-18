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
