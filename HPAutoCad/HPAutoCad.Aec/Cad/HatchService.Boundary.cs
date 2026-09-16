using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The boundary half of <see cref="HatchService"/>: <c>detectBoundary</c> (closed entities containing a point, innermost
///     first — geometric, so it needs no command context), the pattern-type / style parsers, and the area of a hatch or of the
///     boundary it was just built on (AutoCAD does not report a hatch's area inside the transaction that created it).
/// </summary>
public static partial class HatchService
{
    public static AnalysisResult<Dictionary<string, object?>> DetectBoundary(EditContext cx, Editor ed, CancellationToken ct, Point3d seed, string? space, int maxCandidates, int limit)
    {
        var result = new AnalysisResult<Dictionary<string, object?>>();
        var filter = new EntityFilter { Types = ClosedTypes, Space = string.IsNullOrWhiteSpace(space) ? "current" : space! };
        var query = EntityQueryService.Query(cx.Db, ed, cx.Tr, cx.Units, ct, filter, GeometryTolerance.Default, maxCandidates, 0, true, maxCandidates);
        result.Warnings.AddRange(query.Warnings);
        var point = new Pt(cx.ToMm(seed.X), cx.ToMm(seed.Y));
        var hits = query.Records
            .Where(r => r.Shape is { Closed: true } shape && shape.ContainsPointXY(point, GeometryTolerance.Default.PointEquality))
            .Select(r => new { Record = r, Area = r.AreaMm2 ?? r.Shape!.AreaMm2 })
            .OrderBy(x => x.Area)
            .ToArray();
        result.Items = hits.Take(limit).Select(x => new Dictionary<string, object?>
        {
            ["handle"] = x.Record.Handle, ["type"] = x.Record.Type, ["layer"] = x.Record.Layer, ["areaMm2"] = Math.Round(x.Area, 1), ["vertexCount"] = x.Record.Shape!.Vertices.Count, ["boundsMm"] = x.Record.BoundsMm,
        }).ToArray();
        result.Count = hits.Length;
        result.Truncated = query.Truncated || hits.Length > limit;
        result.Summary = new { seedPointMm = point, candidates = query.Records.Count, containing = hits.Length, innermost = hits.FirstOrDefault()?.Record.Handle };
        return result;
    }

    private static HatchPatternType ParsePatternType(string? text) => (text ?? "predefined").Trim().ToLowerInvariant() switch
    {
        "predefined" or "" => HatchPatternType.PreDefined,
        "userdefined" or "user" => HatchPatternType.UserDefined,
        "custom" => HatchPatternType.CustomDefined,
        _ => throw new ArgumentException("patternType must be predefined (default), userDefined or custom."),
    };

    private static HatchStyle ParseStyle(string? text) => (text ?? "normal").Trim().ToLowerInvariant() switch
    {
        "normal" or "" => HatchStyle.Normal,
        "outer" => HatchStyle.Outer,
        "ignore" => HatchStyle.Ignore,
        _ => throw new ArgumentException($"hatchStyle must be one of {string.Join(", ", Styles)}."),
    };

    /// <summary>AutoCAD's own hatch area; null until the hatch has been regenerated (a freshly evaluated hatch may not report one yet).</summary>
    private static double? Area(EditContext cx, Hatch hatch)
    {
        try { return Math.Round(cx.ToMm(cx.ToMm(hatch.Area)), 1); }
        catch (AcadException) { return null; }
    }

    /// <summary>The outer loop's area from the boundary itself (curve area, or the polygon) — what a hatch just created encloses.</summary>
    private static double? BoundaryArea(EditContext cx, IReadOnlyList<Curve> loops, Point2dCollection? polygon)
    {
        try
        {
            if (polygon is not null)
            {
                double sum = 0;
                for (var i = 0; i < polygon.Count; i++)
                {
                    var a = polygon[i];
                    var b = polygon[(i + 1) % polygon.Count];
                    sum += a.X * b.Y - b.X * a.Y;
                }

                return Math.Round(cx.ToMm(cx.ToMm(Math.Abs(sum) / 2)), 1);
            }

            if (loops.Count > 0) return Math.Round(cx.ToMm(cx.ToMm(loops[0].Area)), 1);
        }
        catch (AcadException) { /* no area to report */ }

        return null;
    }
}
