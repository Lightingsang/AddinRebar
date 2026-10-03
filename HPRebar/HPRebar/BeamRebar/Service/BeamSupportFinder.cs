using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Serilog;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Detects bearing supports (columns, walls, girders) and intersecting secondary framing beams.
/// </summary>
public static class BeamSupportFinder
{
    /// <summary>
    /// Discovers all bearing support nodes along the continuous beam run.
    /// </summary>
    public static IReadOnlyList<BeamSupportNode> FindSupports(
        Document doc,
        IReadOnlyList<Element> sortedBeams,
        XYZ beamAxis,
        XYZ transverseAxis,
        XYZ originPoint)
    {
        var measured = new List<MeasuredSupport>();
        var beamIds = new HashSet<ElementId>(sortedBeams.Select(b => b.Id));

        foreach (var beam in sortedBeams)
        {
            var box = beam.get_BoundingBox(null);
            if (box is null) continue;

            var beamBottom = BeamSolidFaceReader.GetBottom(beam);
            double beamSoffitZ = beamBottom?.Origin.Z ?? box.Min.Z;

            // Expand box downwards to capture columns, walls, and girders below soffit
            var outline = new Outline(
                new XYZ(box.Min.X - 1.0, box.Min.Y - 1.0, box.Min.Z - 3.0),
                new XYZ(box.Max.X + 1.0, box.Max.Y + 1.0, box.Min.Z + 0.5));

            var filter = new LogicalOrFilter(new ElementFilter[]
            {
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns),
                new ElementCategoryFilter(BuiltInCategory.OST_Walls),
                new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming)
            });

            var candidates = new FilteredElementCollector(doc)
                .WherePasses(new BoundingBoxIntersectsFilter(outline))
                .WherePasses(filter)
                .WhereElementIsNotElementType()
                .Where(e => !beamIds.Contains(e.Id))
                .ToList();

            foreach (var candidate in candidates)
            {
                var cat = candidate.Category?.BuiltInCategory;
                if (cat == BuiltInCategory.OST_StructuralColumns)
                {
                    var support = MeasureColumnSupport(candidate, beamAxis, transverseAxis, originPoint);
                    if (support is not null)
                    {
                        measured.Add(support);
                    }
                }
                else if (cat == BuiltInCategory.OST_Walls)
                {
                    var support = MeasureWallSupport(candidate, beamAxis, transverseAxis, originPoint);
                    if (support is not null)
                    {
                        measured.Add(support);
                    }
                }
                else if (cat == BuiltInCategory.OST_StructuralFraming)
                {
                    var support = MeasureGirderSupport(candidate, beamAxis, transverseAxis, originPoint, beamSoffitZ);
                    if (support is not null)
                    {
                        measured.Add(support);
                    }
                }
            }
        }

        if (measured.Count == 0)
        {
            Log.Warning("BeamSupportFinder detected 0 physical supports; synthesizing boundary nodes.");
            return BeamSupportLayout.Synthesize(sortedBeams.Select(LengthMm).ToList());
        }

        var pieces = sortedBeams.Select(beam => Extent(beam, beamAxis, originPoint)).ToList();
        return BeamSupportLayout.Arrange(measured, pieces);
    }

    /// <summary>
    /// Identifies secondary framing beams intersecting the continuous beam web.
    /// </summary>
    public static IReadOnlyList<SecondaryBeamIntersection> FindSecondaryBeams(
        Document doc,
        IReadOnlyList<Element> sortedBeams,
        XYZ beamAxis,
        XYZ transverseAxis,
        XYZ originPoint)
    {
        var intersections = new List<SecondaryBeamIntersection>();
        var beamIds = new HashSet<ElementId>(sortedBeams.Select(b => b.Id));
        int index = 0;

        for (int spanIdx = 0; spanIdx < sortedBeams.Count; spanIdx++)
        {
            var beam = sortedBeams[spanIdx];
            var box = beam.get_BoundingBox(null);
            if (box is null) continue;

            var outline = new Outline(
                new XYZ(box.Min.X - 0.5, box.Min.Y - 0.5, box.Min.Z - 0.2),
                new XYZ(box.Max.X + 0.5, box.Max.Y + 0.5, box.Max.Z + 0.2));

            var candidates = new FilteredElementCollector(doc)
                .WherePasses(new BoundingBoxIntersectsFilter(outline))
                .WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming))
                .WhereElementIsNotElementType()
                .Where(e => !beamIds.Contains(e.Id))
                .ToList();

            foreach (var candidate in candidates)
            {
                var curve = (candidate.Location as LocationCurve)?.Curve as Line;
                if (curve is null) continue;

                XYZ candDir = (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();
                double dot = Math.Abs(candDir.DotProduct(beamAxis));

                // Angle must be near 90 degrees (dot product < 0.25 -> angle between 75 and 105 deg)
                if (dot > 0.25) continue;

                // Intersection point between candidate line and primary beam centerline
                var primaryLine = (beam.Location as LocationCurve)?.Curve as Line;
                if (primaryLine is null) continue;

                XYZ ptIntersect = FirstIntersection(primaryLine, curve) ?? curve.GetEndPoint(0);

                double centerXMm = RevitUnits.FtToMm((ptIntersect - originPoint).DotProduct(beamAxis));
                double widthMm = BeamSolidFaceReader.GetWidthMm(candidate, candDir.CrossProduct(XYZ.BasisZ).Normalize());
                double heightMm = BeamSolidFaceReader.GetHeightMm(candidate);

                if (widthMm <= 0.0) widthMm = 200.0; // Fallback default
                if (heightMm <= 0.0) heightMm = 400.0;

                // Determine framing side relative to primary beam axis
                XYZ toCandidate = (curve.GetEndPoint(1) - ptIntersect).Normalize();
                double sideDot = toCandidate.DotProduct(transverseAxis);
                var side = Math.Abs(sideDot) < 0.2 ? IntersectionSide.Both : (sideDot > 0 ? IntersectionSide.Left : IntersectionSide.Right);

                intersections.Add(new SecondaryBeamIntersection(
                    index: index++,
                    hostSpanIndex: spanIdx,
                    centerX: centerXMm,
                    width: widthMm,
                    height: heightMm,
                    topElevation: RevitUnits.FtToMm(ptIntersect.Z),
                    framingSide: side,
                    elementUniqueId: candidate.UniqueId));
            }
        }

        return intersections;
    }

    private static MeasuredSupport? MeasureColumnSupport(Element column, XYZ beamAxis, XYZ transverseAxis, XYZ originPoint)
    {
        var solid = BeamSolidFaceReader.GetSingleSolid(column);
        if (solid is null) return null;

        var top = BeamSolidFaceReader.GetTop(column);
        var corners = new List<XYZ>();

        if (top.EdgeLoops != null && top.EdgeLoops.Size > 0)
        {
            var loop = top.EdgeLoops.get_Item(0);
            foreach (Edge edge in loop)
            {
                var curve = edge.AsCurve();
                if (curve is Arc or Ellipse)
                {
                    corners.Add(curve.Evaluate(0.0, true));
                    corners.Add(curve.Evaluate(0.25, true));
                    corners.Add(curve.Evaluate(0.5, true));
                    corners.Add(curve.Evaluate(0.75, true));
                }
                else
                {
                    corners.Add(curve.GetEndPoint(0));
                }
            }
        }

        if (corners.Count == 0)
        {
            var box = column.get_BoundingBox(null);
            if (box is null) return null;
            corners.Add(new XYZ(box.Min.X, box.Min.Y, box.Max.Z));
            corners.Add(new XYZ(box.Max.X, box.Min.Y, box.Max.Z));
            corners.Add(new XYZ(box.Max.X, box.Max.Y, box.Max.Z));
            corners.Add(new XYZ(box.Min.X, box.Max.Y, box.Max.Z));
        }

        double minS = corners.Min(c => (c - originPoint).DotProduct(beamAxis));
        double maxS = corners.Max(c => (c - originPoint).DotProduct(beamAxis));
        double minY = corners.Min(c => (c - originPoint).DotProduct(transverseAxis));
        double maxY = corners.Max(c => (c - originPoint).DotProduct(transverseAxis));

        double centerS = (minS + maxS) / 2.0;
        double widthS = maxS - minS;
        double depthY = maxY - minY;

        // Circular column fallback if top edge loop was a single periodic curve or collapsed to ~0 width
        if (widthS <= 0.001)
        {
            var uvBox = top.GetBoundingBox();
            if (uvBox != null)
            {
                double diam = uvBox.Max.U - uvBox.Min.U;
                if (diam > 0.1) widthS = diam;
            }
            if (widthS <= 0.001)
            {
                var box = column.get_BoundingBox(null);
                if (box != null)
                {
                    widthS = box.Max.X - box.Min.X;
                    if (depthY <= 0.001) depthY = box.Max.Y - box.Min.Y;
                }
            }
        }
        if (depthY <= 0.001) depthY = widthS;

        return new MeasuredSupport(
            RevitUnits.FtToMm(centerS),
            RevitUnits.FtToMm(widthS),
            RevitUnits.FtToMm(depthY),
            SupportType.Column,
            column.UniqueId);
    }

    private static MeasuredSupport? MeasureWallSupport(Element wall, XYZ beamAxis, XYZ transverseAxis, XYZ originPoint)
    {
        var solid = BeamSolidFaceReader.GetSingleSolid(wall);
        if (solid is null) return null;

        var box = wall.get_BoundingBox(null);
        if (box is null) return null;

        XYZ center = (box.Min + box.Max) * 0.5;
        double centerS = (center - originPoint).DotProduct(beamAxis);

        // Project bounding box extents along beam axis
        double widthS = Math.Abs((box.Max - box.Min).DotProduct(beamAxis));
        double depthY = Math.Abs((box.Max - box.Min).DotProduct(transverseAxis));

        return new MeasuredSupport(
            RevitUnits.FtToMm(centerS),
            RevitUnits.FtToMm(widthS > 0.3 ? widthS : 0.8), // ~250 mm fallback
            RevitUnits.FtToMm(depthY),
            SupportType.Wall,
            wall.UniqueId);
    }

    private static MeasuredSupport? MeasureGirderSupport(
        Element girder, XYZ beamAxis, XYZ transverseAxis, XYZ originPoint, double beamSoffitZ)
    {
        var curve = (girder.Location as LocationCurve)?.Curve as Line;
        if (curve is null) return null;

        XYZ candDir = (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();
        double dot = Math.Abs(candDir.DotProduct(beamAxis));

        // Angle must be between 60° and 120°
        if (dot > 0.5) return null;

        // Verify girder top elevation is strictly below the beam soffit datum
        // Flush framing members are secondary beams, not supporting girders
        var girderTop = BeamSolidFaceReader.GetTop(girder);
        double girderTopZ = girderTop?.Origin.Z ?? (girder.get_BoundingBox(null)?.Max.Z ?? double.MaxValue);
        const double elevToleranceFt = 0.05; // ~15 mm
        if (girderTopZ > beamSoffitZ + elevToleranceFt)
            return null;

        XYZ mid = (curve.GetEndPoint(0) + curve.GetEndPoint(1)) * 0.5;
        double centerS = (mid - originPoint).DotProduct(beamAxis);
        double width = BeamSolidFaceReader.GetWidthMm(girder, candDir.CrossProduct(XYZ.BasisZ).Normalize());
        double height = BeamSolidFaceReader.GetHeightMm(girder);

        return new MeasuredSupport(
            RevitUnits.FtToMm(centerS),
            width > 0 ? width : 300.0,
            height > 0 ? height : 600.0,
            SupportType.Girder,
            girder.UniqueId);
    }

    /// <summary>Where a straight framing element starts and ends along the run axis (mm).</summary>
    private static BeamPieceExtent Extent(Element beam, XYZ beamAxis, XYZ originPoint)
    {
        var line = (beam.Location as LocationCurve)!.Curve as Line;
        double s0 = RevitUnits.FtToMm((line!.GetEndPoint(0) - originPoint).DotProduct(beamAxis));
        double s1 = RevitUnits.FtToMm((line.GetEndPoint(1) - originPoint).DotProduct(beamAxis));
        return new BeamPieceExtent(Math.Min(s0, s1), Math.Max(s0, s1));
    }

    private static double LengthMm(Element beam) =>
        (beam.Location as LocationCurve)?.Curve is Line line
            ? RevitUnits.FtToMm(line.Length)
            : BeamSupportLayout.UnknownBeamLengthMm;

    /// <summary>The first point where two curves meet, or null when they do not.</summary>
    private static XYZ? FirstIntersection(Curve first, Curve second)
    {
        // Multi-version: curve intersection — Revit 2026 adds Intersect(Curve, CurveIntersectResultOption),
        // the only overload left in 2027; earlier versions return the points through an out array.
#if REVIT2026_OR_GREATER
        var result = first.Intersect(second, CurveIntersectResultOption.Detailed);
        if (result.Result != SetComparisonResult.Overlap)
        {
            return null;
        }
        var overlaps = result.GetOverlaps();
        return overlaps.Count > 0 ? overlaps[0].Point : null;
#else
        var comparison = first.Intersect(second, out IntersectionResultArray? points);
        return comparison == SetComparisonResult.Overlap && points != null && points.Size > 0
            ? points.get_Item(0).XYZPoint
            : null;
#endif
    }
}
