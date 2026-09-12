using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Model;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Validates a selection of structural framing elements against continuous beam geometric rules.
/// </summary>
public static class BeamStackValidator
{
    public const double MaxCollinearAngleDeg = 1.0;
    public const double MaxOffsetMm = 10.0;
    public const double MaxElevationDiffMm = 5.0;

    public static ValidationResult Validate(Document doc, IReadOnlyList<Element> beams)
    {
        if (beams is null || beams.Count == 0)
            return ValidationResult.Fail(1);

        var rules = new (int Code, Func<bool> Holds)[]
        {
            (2, () => beams.All(b => b.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralFraming && b is FamilyInstance)),
            (3, () => beams.All(b => (b.Location as LocationCurve)?.Curve is Line)),
            (4, () => beams.All(b => BeamSolidFaceReader.GetSolids(b).Count == 1)),
            (5, () => AreAllRectangular(beams)),
            (6, () => IsCollinear(beams)),
            (7, () => IsWithinLateralOffset(beams)),
            (8, () => HasConsistentTopElevation(beams)),
            (9, () => AreSpansContiguous(beams)),
            (10, () => HasPositiveDimensions(beams))
        };

        foreach (var (code, holds) in rules)
        {
            if (!holds()) return ValidationResult.Fail(code);
        }

        if (!HasUniformWidth(beams))
            return ValidationResult.Fail("Continuous beams with stepped widths are not currently supported.");

        return ValidationResult.Ok;
    }

    private static bool AreAllRectangular(IReadOnlyList<Element> beams)
    {
        foreach (var beam in beams)
        {
            var line = (beam.Location as LocationCurve)?.Curve as Line;
            if (line is null) return false;

            XYZ axis = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
            XYZ trans = XYZ.BasisZ.CrossProduct(axis).Normalize();

            if (BeamSolidFaceReader.GetSectionStyle(beam, axis, trans) != BeamSectionStyle.Rectangle)
                return false;
        }
        return true;
    }

    private static bool IsCollinear(IReadOnlyList<Element> beams)
    {
        if (beams.Count <= 1) return true;

        var line0 = (beams[0].Location as LocationCurve)?.Curve as Line;
        if (line0 is null) return false;

        XYZ primaryAxis = (line0.GetEndPoint(1) - line0.GetEndPoint(0)).Normalize();

        for (int i = 1; i < beams.Count; i++)
        {
            var line = (beams[i].Location as LocationCurve)?.Curve as Line;
            if (line is null) return false;

            XYZ axis = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
            double dot = Math.Abs(axis.DotProduct(primaryAxis));
            double angleDeg = Math.Acos(Math.Min(1.0, dot)) * 180.0 / Math.PI;

            if (angleDeg > MaxCollinearAngleDeg) return false;
        }

        return true;
    }

    private static bool IsWithinLateralOffset(IReadOnlyList<Element> beams)
    {
        if (beams.Count <= 1) return true;

        var line0 = (beams[0].Location as LocationCurve)?.Curve as Line;
        if (line0 is null) return false;

        XYZ origin = line0.GetEndPoint(0);
        XYZ primaryAxis = (line0.GetEndPoint(1) - origin).Normalize();

        for (int i = 1; i < beams.Count; i++)
        {
            var line = (beams[i].Location as LocationCurve)?.Curve as Line;
            if (line is null) return false;

            XYZ mid = (line.GetEndPoint(0) + line.GetEndPoint(1)) * 0.5;
            XYZ diff = mid - origin;
            XYZ perp = diff - diff.DotProduct(primaryAxis) * primaryAxis;
            double offsetMm = RevitUnits.FtToMm(perp.GetLength());

            if (offsetMm > MaxOffsetMm) return false;
        }

        return true;
    }

    private static bool HasConsistentTopElevation(IReadOnlyList<Element> beams)
    {
        if (beams.Count <= 1) return true;

        double z0 = BeamSolidFaceReader.GetTop(beams[0]).Origin.Z;

        for (int i = 1; i < beams.Count; i++)
        {
            double z = BeamSolidFaceReader.GetTop(beams[i]).Origin.Z;
            double diffMm = RevitUnits.FtToMm(Math.Abs(z - z0));

            if (diffMm > MaxElevationDiffMm) return false;
        }

        return true;
    }

    private static bool AreSpansContiguous(IReadOnlyList<Element> beams)
    {
        if (beams.Count <= 1) return true;

        var line0 = (beams[0].Location as LocationCurve)?.Curve as Line;
        if (line0 is null) return false;

        XYZ origin = line0.GetEndPoint(0);
        XYZ axis = (line0.GetEndPoint(1) - origin).Normalize();

        // Project and sort endpoints
        var spans = beams.Select(b =>
        {
            var l = (b.Location as LocationCurve)!.Curve as Line;
            double s0 = (l!.GetEndPoint(0) - origin).DotProduct(axis);
            double s1 = (l.GetEndPoint(1) - origin).DotProduct(axis);
            return (Start: Math.Min(s0, s1), End: Math.Max(s0, s1));
        }).OrderBy(s => s.Start).ToList();

        for (int i = 0; i < spans.Count - 1; i++)
        {
            double gapFt = spans[i + 1].Start - spans[i].End;
            double gapMm = RevitUnits.FtToMm(gapFt);

            // Gaps up to 2000 mm are acceptable if spanning across a wide column/wall
            if (gapMm > 2000.0) return false;
        }

        return true;
    }

    private static bool HasPositiveDimensions(IReadOnlyList<Element> beams)
    {
        foreach (var beam in beams)
        {
            var line = (beam.Location as LocationCurve)?.Curve as Line;
            if (line is null || line.Length <= 0) return false;

            XYZ axis = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
            XYZ trans = XYZ.BasisZ.CrossProduct(axis).Normalize();

            double b = BeamSolidFaceReader.GetWidthMm(beam, trans);
            double h = BeamSolidFaceReader.GetHeightMm(beam);

            if (b < 100.0 || h < 150.0) return false;
        }
        return true;
    }

    private static bool HasUniformWidth(IReadOnlyList<Element> beams)
    {
        if (beams.Count <= 1) return true;

        var line0 = (beams[0].Location as LocationCurve)?.Curve as Line;
        if (line0 is null) return false;
        XYZ axis0 = (line0.GetEndPoint(1) - line0.GetEndPoint(0)).Normalize();
        XYZ trans0 = XYZ.BasisZ.CrossProduct(axis0).Normalize();
        double b0 = BeamSolidFaceReader.GetWidthMm(beams[0], trans0);

        for (int i = 1; i < beams.Count; i++)
        {
            var line = (beams[i].Location as LocationCurve)?.Curve as Line;
            if (line is null) return false;
            XYZ axis = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
            XYZ trans = XYZ.BasisZ.CrossProduct(axis).Normalize();
            double bi = BeamSolidFaceReader.GetWidthMm(beams[i], trans);

            if (Math.Abs(bi - b0) > 1.0)
                return false;
        }

        return true;
    }
}
