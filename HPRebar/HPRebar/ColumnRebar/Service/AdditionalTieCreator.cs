using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Places the intermediate cross-ties that restrain the bars between the corners.
///
///     Two placement styles, matching the original tool:
///     type 0 puts one narrower closed tie inside the perimeter tie, sized by the leg length the user gave;
///     any other type spreads a given number of single cross-ties evenly across the section.
///
///     Cross-ties follow the same vertical groups as the perimeter ties, nudged up by one bar diameter so
///     they sit just above the perimeter tie rather than clashing with it.
/// </summary>
public static class AdditionalTieCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        ColumnFaces faces,
        ColumnSection section,
        RebarShapeResolver shapes,
        RebarBarType barType,
        double tieDiameterMm,
        double perimeterBarDiameterMm,
        double coverMm,
        AdditionalTieSpec spec,
        IReadOnlyList<StirrupRun> runs,
        string partitionName)
    {
        var created = new List<Rebar>();

        if (!spec.AddH && !spec.AddV) return created;

        // Offset every group so the cross-ties clear the perimeter tie they sit beside.
        var offsetRuns = new List<StirrupRun>(runs.Count);

        foreach (var run in runs)
        {
            offsetRuns.Add(run with { StartOffset = run.StartOffset + perimeterBarDiameterMm });
        }

        if (section.Shape == SectionShape.Rectangle)
        {
            if (spec.AddH) created.AddRange(RectangleHorizontal(document, faces, section, shapes, barType, coverMm, spec, offsetRuns, partitionName));
            if (spec.AddV) created.AddRange(RectangleVertical(document, faces, section, shapes, barType, coverMm, spec, offsetRuns, partitionName));
        }
        else
        {
            if (spec.AddH) created.AddRange(CircleClosed(document, faces, section, shapes, barType, tieDiameterMm, coverMm, offsetRuns, partitionName));
            if (spec.AddV) created.AddRange(CircleCross(document, faces, section, shapes, barType, tieDiameterMm, coverMm, spec, offsetRuns, partitionName));
        }

        return created;
    }

    private static IEnumerable<Rebar> RectangleHorizontal(
        Document document,
        ColumnFaces faces,
        ColumnSection section,
        RebarShapeResolver shapes,
        RebarBarType barType,
        double coverMm,
        AdditionalTieSpec spec,
        IReadOnlyList<StirrupRun> runs,
        string partitionName)
    {
        var created = new List<Rebar>();

        if (spec.KindH == CrossTieKind.ClosedTie)
        {
            // A closed tie of leg length AH, centred in the section. Zero leg means nothing to place.
            if (spec.AH == 0) return created;

            var shape = shapes.MainTie(SectionShape.Rectangle)!;
            var inset = (section.B - 2 * coverMm - spec.AH) / 2;

            foreach (var run in runs)
            {
                var placement = StirrupGeometry.Rectangle(
                    faces, coverMm, section.B - inset, section.H, inset, 0, run.StartOffset);

                created.Add(StirrupCreator.Place(document, faces.Element, shape, barType, placement, run, partitionName));
            }

            return created;
        }

        var crossShape = shapes.CrossTie(spec.KindH)!;
        var spacing = (section.B - 2 * coverMm) / (spec.NH + 1);

        for (var i = 0; i < spec.NH; i++)
        {
            var offsetX = coverMm + (i + 1) * spacing;

            foreach (var run in runs)
            {
                var placement = StirrupGeometry.CrossTieAcrossDepth(faces, coverMm, section.H, offsetX, run.StartOffset);

                created.Add(PlaceCrossTie(document, faces.Element, crossShape, barType, placement, run, partitionName));
            }
        }

        return created;
    }

    private static IEnumerable<Rebar> RectangleVertical(
        Document document,
        ColumnFaces faces,
        ColumnSection section,
        RebarShapeResolver shapes,
        RebarBarType barType,
        double coverMm,
        AdditionalTieSpec spec,
        IReadOnlyList<StirrupRun> runs,
        string partitionName)
    {
        var created = new List<Rebar>();

        if (spec.KindV == CrossTieKind.ClosedTie)
        {
            if (spec.AV == 0) return created;

            var shape = shapes.MainTie(SectionShape.Rectangle)!;
            var inset = (section.H - 2 * coverMm - spec.AV) / 2;

            foreach (var run in runs)
            {
                var placement = StirrupGeometry.Rectangle(
                    faces, coverMm, section.B, section.H - inset, 0, inset, run.StartOffset);

                created.Add(StirrupCreator.Place(document, faces.Element, shape, barType, placement, run, partitionName));
            }

            return created;
        }

        var crossShape = shapes.CrossTie(spec.KindV)!;
        var spacing = (section.H - 2 * coverMm) / (spec.NV + 1);

        for (var i = 0; i < spec.NV; i++)
        {
            var offsetY = coverMm + (i + 1) * spacing;

            foreach (var run in runs)
            {
                var placement = StirrupGeometry.CrossTieAcrossWidth(faces, coverMm, section.B, offsetY, run.StartOffset);

                created.Add(PlaceCrossTie(document, faces.Element, crossShape, barType, placement, run, partitionName));
            }
        }

        return created;
    }

    /// <summary>The square tie laid diagonally over a circular section.</summary>
    private static IEnumerable<Rebar> CircleClosed(
        Document document,
        ColumnFaces faces,
        ColumnSection section,
        RebarShapeResolver shapes,
        RebarBarType barType,
        double tieDiameterMm,
        double coverMm,
        IReadOnlyList<StirrupRun> runs,
        string partitionName)
    {
        var shape = shapes.MainTie(SectionShape.Rectangle)!;
        var created = new List<Rebar>();

        foreach (var run in runs)
        {
            var placement = StirrupGeometry.DiagonalTieOnCircle(faces, section.D, coverMm, tieDiameterMm, run.StartOffset);

            created.Add(StirrupCreator.Place(document, faces.Element, shape, barType, placement, run, partitionName));
        }

        return created;
    }

    /// <summary>A crossing pair of single ties on a circular section, one on each axis.</summary>
    private static IEnumerable<Rebar> CircleCross(
        Document document,
        ColumnFaces faces,
        ColumnSection section,
        RebarShapeResolver shapes,
        RebarBarType barType,
        double tieDiameterMm,
        double coverMm,
        AdditionalTieSpec spec,
        IReadOnlyList<StirrupRun> runs,
        string partitionName)
    {
        // The window offers one list for both section shapes, so the choice means the same hooks here.
        var shape = shapes.CrossTie(spec.KindV)!;
        var created = new List<Rebar>();

        foreach (var run in runs)
        {
            var acrossX = StirrupGeometry.CrossTieOnCircleAcrossX(faces, section.D, coverMm, tieDiameterMm, run.StartOffset);
            created.Add(PlaceCrossTie(document, faces.Element, shape, barType, acrossX, run, partitionName));

            var acrossY = StirrupGeometry.CrossTieOnCircleAcrossY(faces, section.D, coverMm, tieDiameterMm, run.StartOffset);
            created.Add(PlaceCrossTie(document, faces.Element, shape, barType, acrossY, run, partitionName));
        }

        return created;
    }

    /// <summary>
    ///     A single cross-tie spans the section along one axis and has nothing to stretch across the other,
    ///     so its two box vectors go in the opposite order from a closed tie: the span first, then the
    ///     direction it has no length in.
    /// </summary>
    private static Rebar PlaceCrossTie(
        Document document,
        Element host,
        RebarShape shape,
        RebarBarType barType,
        StirrupPlacement placement,
        StirrupRun run,
        string partitionName)
    {
        var rebar = Rebar.CreateFromRebarShape(
            document, shape, barType, host, placement.Origin, placement.XVector, placement.YVector);

        var accessor = rebar.GetShapeDrivenAccessor();

        // ScaleToBox scales the shape parameters to match BOTH edges of the box, and when the shape cannot
        // satisfy both it falls back to scaling the whole thing until one of them fits — without saying
        // which. A cross-tie has only its span to give, so the second edge is set to the height the shape
        // already has at its default parameters, leaving the span as the only thing left to scale. Passing
        // a bare direction vector here instead would ask for a 1 ft height on every section and invite that
        // fallback to resize the tie as a whole.
        var across = placement.Width.Normalize();

        accessor.ScaleToBox(placement.Origin, placement.Height, HeightAcross(rebar, across, barType) * across);
        accessor.SetLayoutAsNumberWithSpacing(run.Count, RevitUnits.MmToFt(run.Spacing), true, true, true);

        MainBarCreator.SetPartition(rebar, partitionName);

        return rebar;
    }

    /// <summary>
    ///     How far the bar reaches along <paramref name="across"/> while it still carries the shape's own
    ///     default parameters — for a hooked cross-tie, the reach of its hooks. Never less than one bar
    ///     diameter: ScaleToBox measures a box including the bar's thickness, and a zero-height box has no
    ///     shape to scale to.
    /// </summary>
    private static double HeightAcross(Rebar rebar, XYZ across, RebarBarType barType)
    {
        var lowest = double.MaxValue;
        var highest = double.MinValue;

        foreach (var curve in rebar.GetCenterlineCurves(
                     false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0))
        {
            foreach (var point in curve.Tessellate())
            {
                var along = point.DotProduct(across);

                if (along < lowest) lowest = along;
                if (along > highest) highest = along;
            }
        }

        var diameter = barType.BarNominalDiameter;

        return highest < lowest ? diameter : Math.Max(highest - lowest, diameter);
    }
}
