using System;
using System.Collections.Generic;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     Turns a bar's plan position and splice settings into its centre-line, bottom end first.
///     All millimetres in the column's local frame.
/// </summary>
public static class BarPolylineBuilder
{
    /// <summary>
    ///     Centre-line of one bar. <paramref name="upperPosition"/> is where the bar has to arrive on the
    ///     section above, and only matters when the bar bends across (top dowels type 0).
    /// </summary>
    public static BarPolyline Build(
        ColumnSection section,
        BarLayoutSpec spec,
        BarPosition bar,
        SpliceSpec splice,
        PlanPoint upperPosition,
        string barTypeName = "")
    {
        if (section is null) throw new ArgumentNullException(nameof(section));
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (bar is null) throw new ArgumentNullException(nameof(bar));
        if (splice is null) throw new ArgumentNullException(nameof(splice));

        var points = new List<Point3>();

        AppendBottom(points, section, spec, bar, splice);
        AppendTop(points, section, spec, bar, splice, upperPosition);

        return new BarPolyline
        {
            BarNumber = bar.BarNumber,
            Diameter = spec.BarDiameter,
            Points = points,
            Splice = splice,
            BarTypeName = barTypeName
        };
    }

    private static void AppendBottom(
        ICollection<Point3> points,
        ColumnSection section,
        BarLayoutSpec spec,
        BarPosition bar,
        SpliceSpec splice)
    {
        if (!splice.IsBottomDowels)
        {
            points.Add(new Point3(bar.X0, bar.Y0, section.BottomPosition));
            return;
        }

        if (splice.BottomStyle == BottomDowelStyle.StartAboveBase)
        {
            // Bar starts clear of the base — no dowel running down into the segment below.
            points.Add(new Point3(bar.X0, bar.Y0, section.BottomPosition + splice.LcBottom));
            return;
        }

        var z = section.BottomPosition - splice.LbBottom;

        if (Tolerance.AreEqual(splice.LaBottom, 0d))
        {
            points.Add(new Point3(bar.X0, bar.Y0, z));
            return;
        }

        points.Add(HookPoint(section, spec, bar, splice.LaBottom, z));
        points.Add(new Point3(bar.X0, bar.Y0, z));
    }

    private static void AppendTop(
        ICollection<Point3> points,
        ColumnSection section,
        BarLayoutSpec spec,
        BarPosition bar,
        SpliceSpec splice,
        PlanPoint upperPosition)
    {
        if (!splice.IsTopDowels)
        {
            points.Add(new Point3(bar.X0, bar.Y0, section.TopPosition - spec.Cover));
            return;
        }

        if (splice.TopStyle == TopDowelStyle.BendIntoColumnAbove)
        {
            // Start the sideways bend below the beam soffit, cross over at the segment top, then anchor up
            // into the column above.
            points.Add(new Point3(bar.X0, bar.Y0, section.TopPosition - section.BendDepth));
            points.Add(new Point3(upperPosition.X, upperPosition.Y, section.TopPosition));
            points.Add(new Point3(upperPosition.X, upperPosition.Y, section.TopPosition + splice.LbTop));
            return;
        }

        var z = section.TopPosition - spec.Cover - section.Zb;
        points.Add(new Point3(bar.X0, bar.Y0, z));

        if (Tolerance.AreEqual(splice.LaTop, 0d))
        {
            return;
        }

        points.Add(HookPoint(section, spec, bar, splice.LaTop, z));
    }

    /// <summary>
    ///     End of a horizontal hook of length <paramref name="la"/>. On a rectangle the hook turns inward
    ///     from the face the bar sits on; on a circle it runs radially outward.
    /// </summary>
    private static Point3 HookPoint(ColumnSection section, BarLayoutSpec spec, BarPosition bar, double la, double z)
    {
        if (section.Shape == SectionShape.Circular)
        {
            var radius = BarLayoutCalculator.BarRadius(section, spec) + la;
            var angle = (bar.BarNumber - 1) * Math.PI * 2 / spec.Nd;

            return new Point3(
                section.CenterX + radius * Math.Round(Math.Cos(angle), 9),
                section.CenterY + radius * Math.Round(Math.Sin(angle), 9),
                z);
        }

        switch (BarSideClassifier.SideOf(bar.BarNumber, spec.Nx, spec.Ny))
        {
            case BarSide.South: return new Point3(bar.X0, bar.Y0 - la, z);
            case BarSide.East: return new Point3(bar.X0 + la, bar.Y0, z);
            case BarSide.North: return new Point3(bar.X0, bar.Y0 + la, z);
            default: return new Point3(bar.X0 - la, bar.Y0, z);
        }
    }

    /// <summary>True when every point shares the same plan position, i.e. the bar is a straight vertical.</summary>
    public static bool IsStraight(IReadOnlyList<Point3> points)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));

        for (var i = 1; i < points.Count; i++)
        {
            if (!Tolerance.AreEqual(points[i - 1].X, points[i].X) ||
                !Tolerance.AreEqual(points[i - 1].Y, points[i].Y))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     How far a point may sit off the line between its neighbours and still count as lying on it.
    ///     Millimetres, and far below anything the bar geometry produces: the points of a straight run are
    ///     built from the same plan numbers, so a real corner is never this close to straight.
    /// </summary>
    private const double CollinearToleranceMm = 1.0e-6;

    /// <summary>
    ///     The points where the bar actually turns. An interior point lying on the straight run between the
    ///     point before it and the point after it carries no bend, so it is dropped and the run becomes one
    ///     segment; the two ends are always kept. A point that doubles back is a corner however straight the
    ///     line through it is, and is kept.
    /// </summary>
    public static IReadOnlyList<Point3> Corners(IReadOnlyList<Point3> points)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));

        if (points.Count < 2)
        {
            throw new ArgumentException("A centre-line needs at least two points.", nameof(points));
        }

        var kept = new List<Point3> { points[0] };

        for (var i = 1; i < points.Count - 1; i++)
        {
            if (!LiesBetween(kept[kept.Count - 1], points[i], points[i + 1]))
            {
                kept.Add(points[i]);
            }
        }

        kept.Add(points[points.Count - 1]);

        return kept;
    }

    /// <summary>
    ///     True when <paramref name="middle"/> sits on the straight run from <paramref name="first"/> to
    ///     <paramref name="last"/> — on the line, and between the two rather than past either end.
    /// </summary>
    private static bool LiesBetween(Point3 first, Point3 middle, Point3 last)
    {
        var ux = last.X - first.X;
        var uy = last.Y - first.Y;
        var uz = last.Z - first.Z;

        var lengthSquared = ux * ux + uy * uy + uz * uz;

        // The two ends coincide, so the middle point is the only thing giving the run a shape.
        if (lengthSquared <= 0d) return false;

        var vx = middle.X - first.X;
        var vy = middle.Y - first.Y;
        var vz = middle.Z - first.Z;

        var along = (vx * ux + vy * uy + vz * uz) / lengthSquared;

        if (along < 0d || along > 1d) return false;

        var crossX = vy * uz - vz * uy;
        var crossY = vz * ux - vx * uz;
        var crossZ = vx * uy - vy * ux;

        var offLine = Math.Sqrt(crossX * crossX + crossY * crossY + crossZ * crossZ) / Math.Sqrt(lengthSquared);

        return offLine <= CollinearToleranceMm;
    }

    /// <summary>Summed segment length of a centre-line.</summary>
    public static double Length(IReadOnlyList<Point3> points)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));

        var total = 0d;

        for (var i = 1; i < points.Count; i++)
        {
            total += Distance(points[i - 1], points[i]);
        }

        return total;
    }

    /// <summary>Straight-line distance between two points.</summary>
    public static double Distance(Point3 a, Point3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;

        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
