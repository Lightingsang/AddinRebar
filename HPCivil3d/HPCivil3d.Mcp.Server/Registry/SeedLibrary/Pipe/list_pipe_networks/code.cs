string namePattern = args.Str("namePattern", "*");
bool includeParts = args.Bool("includeParts", false);
int limit = args.Int("limit", 50);
int partLimit = args.Int("partLimit", 200);
if (limit <= 0 || limit > 500) throw new ArgumentException("limit must be 1–500.");
if (partLimit <= 0 || partLimit > 100) throw new ArgumentException("partLimit must be 1–100.");
if (civil == null) throw new InvalidOperationException("The active drawing has no Civil document.");
bool Like(string text, string pattern) => string.IsNullOrEmpty(pattern) || pattern == "*" || System.Text.RegularExpressions.Regex.IsMatch(text ?? "",
    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
var errors = new List<object>();
void Fail(string code, string message, string handle) => errors.Add(new { code, message, handle });
string Classify(Exception ex) => ex is Autodesk.AutoCAD.Runtime.Exception acad ? acad.ErrorStatus.ToString() : ex.GetType().Name;

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
