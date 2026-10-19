namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Geometry;
using HPBim.Core.Qc;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
/// Service to query live foundation elements from Revit 2026 via Named Pipe.
/// Adheres to Clean Code C1/FM4/M7: Boundary queries isolated from domain logic and CLI commands.
/// </summary>
public static class RevitFoundationQueryService
{
    public static async Task<(IReadOnlyList<GridAuditItem>, IReadOnlyList<LevelAuditItem>)> QueryGridsAndLevelsAsync(
        IRevitPipeClient client, CancellationToken ct)
    {
        string script = """
            var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>()
                .Select(g => {
                    var crv = g.Curve;
                    var p0 = crv.GetEndPoint(0);
                    var p1 = crv.GetEndPoint(1);
                    return new {
                        name = g.Name,
                        pinned = g.Pinned,
                        p0x = Math.Round(p0.X * 304.8, 1),
                        p0y = Math.Round(p0.Y * 304.8, 1),
                        p1x = Math.Round(p1.X * 304.8, 1),
                        p1y = Math.Round(p1.Y * 304.8, 1)
                    };
                }).ToList();

            var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                .Select(l => new {
                    name = l.Name,
                    pinned = l.Pinned,
                    elev = Math.Round(l.Elevation * 304.8, 1)
                }).ToList();

            return new { grids, levels };
        """;

        var res = await client.ExecuteAsync(new ExecuteRequest(script, TransactionModes.None, false, 30, "Query Grids/Levels"), ct).ConfigureAwait(false);
        if (res.IsError || !res.Value.HasValue) return (Array.Empty<GridAuditItem>(), Array.Empty<LevelAuditItem>());

        var root = res.Value.Value;
        var gList = new List<GridAuditItem>();
        if (root.TryGetProperty("grids", out var gp))
        {
            foreach (var g in gp.EnumerateArray())
            {
                gList.Add(new GridAuditItem(
                    g.GetProperty("name").GetString() ?? "",
                    g.GetProperty("p0x").GetDouble(), g.GetProperty("p0y").GetDouble(),
                    g.GetProperty("p1x").GetDouble(), g.GetProperty("p1y").GetDouble(),
                    g.GetProperty("pinned").GetBoolean()));
            }
        }

        var lList = new List<LevelAuditItem>();
        if (root.TryGetProperty("levels", out var lp))
        {
            foreach (var l in lp.EnumerateArray())
            {
                lList.Add(new LevelAuditItem(
                    l.GetProperty("name").GetString() ?? "",
                    l.GetProperty("elev").GetDouble(),
                    l.GetProperty("pinned").GetBoolean()));
            }
        }

        return (gList, lList);
    }

    public static async Task<IReadOnlyList<PileAuditItem>> QueryPilesAsync(IRevitPipeClient client, CancellationToken ct)
    {
        string script = """
            var opt = new Options();
            var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
                .Where(p => p.Symbol.Family.Name.Contains("Coc") || p.Symbol.Name.Contains("P400") || p.Symbol.Name.Contains("250"))
                .Select(p => {
                    var loc = (LocationPoint)p.Location;
                    var pt = loc != null ? loc.Point : XYZ.Zero;
                    double solidTopZMm = double.MinValue;
                    void Extract(GeometryElement ge)
                    {
                        if (ge == null) return;
                        foreach (var obj in ge)
                        {
                            if (obj is Solid s && s.Volume > 0.0001)
                            {
                                foreach (Face f in s.Faces)
                                {
                                    if (f is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ))
                                    {
                                        double fz = pf.Origin.Z * 304.8;
                                        if (fz > solidTopZMm) solidTopZMm = fz;
                                    }
                                }
                            }
                            else if (obj is GeometryInstance gi)
                            {
                                Extract(gi.GetInstanceGeometry());
                            }
                        }
                    }
                    Extract(p.get_Geometry(opt));
                    if (solidTopZMm < -90000.0)
                    {
                        double offsetFt = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsDouble() ?? 0.0;
                        solidTopZMm = -1600.0 + (offsetFt * 304.8);
                    }
                    return new {
                        id = p.Id.Value,
                        mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                        sym = p.Symbol.Name,
                        x = Math.Round(pt.X * 304.8, 1),
                        y = Math.Round(pt.Y * 304.8, 1),
                        z = Math.Round(solidTopZMm, 1)
                    };
                }).ToList();
            return piles;
        """;

        var res = await client.ExecuteAsync(new ExecuteRequest(script, TransactionModes.None, false, 30, "Query Piles"), ct).ConfigureAwait(false);
        if (res.IsError || !res.Value.HasValue) return Array.Empty<PileAuditItem>();

        var list = new List<PileAuditItem>();
        foreach (var p in res.Value.Value.EnumerateArray())
        {
            list.Add(new PileAuditItem(
                p.GetProperty("id").GetInt64(),
                p.GetProperty("mark").GetString() ?? "",
                p.GetProperty("x").GetDouble(),
                p.GetProperty("y").GetDouble(),
                p.GetProperty("z").GetDouble(),
                p.GetProperty("sym").GetString() ?? ""));
        }
        return list;
    }

    public static async Task<IReadOnlyList<FootingAuditItem>> QueryFootingsAsync(IRevitPipeClient client, CancellationToken ct)
    {
        string script = """
            var footings = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>()
                .Where(f => f.FloorType.Name.StartsWith("HP-ES-CAP") || f.FloorType.Name.StartsWith("ĐC-") || f.FloorType.Name.StartsWith("DC-"))
                .Select(f => {
                    var bb = f.get_BoundingBox(null);
                    double topZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
                    double botZ = bb != null ? bb.Min.Z * 304.8 : 0.0;
                    double thick = f.FloorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM)?.AsDouble() * 304.8 ?? 0.0;
                    
                    var opt = new Options();
                    var geom = f.get_Geometry(opt);
                    var poly = new List<object>();
                    if (geom != null) {
                        PlanarFace bestPf = null;
                        double bestArea = -1;
                        foreach (var obj in geom) {
                            if (obj is Solid solid && solid.Volume > 0) {
                                foreach (Face face in solid.Faces) {
                                    if (face is PlanarFace pf && pf.FaceNormal.Z < -0.9 && pf.Area > bestArea) {
                                        bestArea = pf.Area;
                                        bestPf = pf;
                                    }
                                }
                            }
                        }
                        if (bestPf == null) {
                            foreach (var obj in geom) {
                                if (obj is Solid solid && solid.Volume > 0) {
                                    foreach (Face face in solid.Faces) {
                                        if (face is PlanarFace pf && pf.FaceNormal.Z > 0.9 && pf.Area > bestArea) {
                                            bestArea = pf.Area;
                                            bestPf = pf;
                                        }
                                    }
                                }
                            }
                        }
                        if (bestPf != null && bestPf.EdgeLoops.Size > 0) {
                            foreach (Edge edge in bestPf.EdgeLoops.get_Item(0)) {
                                var p = edge.AsCurve().GetEndPoint(0);
                                poly.Add(new { x = Math.Round(p.X * 304.8, 1), y = Math.Round(p.Y * 304.8, 1) });
                            }
                        }
                    }

                    return new {
                        id = f.Id.Value,
                        mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                        typeName = f.FloorType.Name,
                        topZ = Math.Round(topZ, 1),
                        botZ = Math.Round(botZ, 1),
                        thickMm = Math.Round(thick, 1),
                        poly = poly
                    };
                }).ToList();
            return footings;
        """;

        var res = await client.ExecuteAsync(new ExecuteRequest(script, TransactionModes.None, false, 45, "Query Footings"), ct).ConfigureAwait(false);
        if (res.IsError || !res.Value.HasValue)
        {
            throw new InvalidOperationException($"Query Footings script failed: {res.Message} (Diags: {string.Join("; ", res.Diagnostics?.Select(d => d.Message) ?? Array.Empty<string>())})");
        }

        var list = new List<FootingAuditItem>();
        foreach (var f in res.Value.Value.EnumerateArray())
        {
            var pts = new List<Point2D>();
            if (f.TryGetProperty("poly", out var pp))
            {
                foreach (var pt in pp.EnumerateArray())
                {
                    pts.Add(new Point2D(pt.GetProperty("x").GetDouble(), pt.GetProperty("y").GetDouble()));
                }
            }
            var poly = pts.Count >= 3 ? new Polygon2D(pts) : Polygon2D.Empty;
            list.Add(new FootingAuditItem(
                f.GetProperty("id").GetInt64(),
                f.GetProperty("mark").GetString() ?? "",
                f.GetProperty("typeName").GetString() ?? "",
                f.GetProperty("topZ").GetDouble(),
                f.GetProperty("botZ").GetDouble(),
                f.GetProperty("thickMm").GetDouble(),
                poly.Centroid,
                poly));
        }
        return list;
    }

    public static async Task<IReadOnlyList<BeamAuditItem>> QueryBeamsAsync(IRevitPipeClient client, CancellationToken ct)
    {
        string script = """
            var beams = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType().Cast<FamilyInstance>()
                .Select(b => {
                    var sym = b.Symbol;
                    var pB = sym.LookupParameter("b") ?? sym.LookupParameter("Width") ?? sym.LookupParameter("B");
                    var pH = sym.LookupParameter("h") ?? sym.LookupParameter("Height") ?? sym.LookupParameter("H");
                    double bMm = pB != null ? Math.Round(pB.AsDouble() * 304.8, 1) : 0.0;
                    double hMm = pH != null ? Math.Round(pH.AsDouble() * 304.8, 1) : 0.0;
                    var bb = b.get_BoundingBox(null);
                    double topZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
                    var locCrv = b.Location as LocationCurve;
                    var p0 = locCrv != null ? locCrv.Curve.GetEndPoint(0) : XYZ.Zero;
                    var p1 = locCrv != null ? locCrv.Curve.GetEndPoint(1) : XYZ.Zero;

                    return new {
                        id = b.Id.Value,
                        mark = b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                        sym = sym.Name,
                        bMm = bMm,
                        hMm = hMm,
                        topZ = Math.Round(topZ, 1),
                        p0x = Math.Round(p0.X * 304.8, 1),
                        p0y = Math.Round(p0.Y * 304.8, 1),
                        p1x = Math.Round(p1.X * 304.8, 1),
                        p1y = Math.Round(p1.Y * 304.8, 1)
                    };
                }).ToList();
            return beams;
        """;

        var res = await client.ExecuteAsync(new ExecuteRequest(script, TransactionModes.None, false, 45, "Query Beams"), ct).ConfigureAwait(false);
        if (res.IsError || !res.Value.HasValue) return Array.Empty<BeamAuditItem>();

        var list = new List<BeamAuditItem>();
        foreach (var b in res.Value.Value.EnumerateArray())
        {
            list.Add(new BeamAuditItem(
                b.GetProperty("id").GetInt64(),
                b.GetProperty("mark").GetString() ?? "",
                b.GetProperty("sym").GetString() ?? "",
                b.GetProperty("bMm").GetDouble(),
                b.GetProperty("hMm").GetDouble(),
                b.GetProperty("topZ").GetDouble(),
                new Point2D(b.GetProperty("p0x").GetDouble(), b.GetProperty("p0y").GetDouble()),
                new Point2D(b.GetProperty("p1x").GetDouble(), b.GetProperty("p1y").GetDouble())));
        }
        return list;
    }
}
