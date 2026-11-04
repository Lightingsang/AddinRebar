using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataRebar.ColumnStack;
using HPRebar.KataRebar.Service;

namespace HPRebar.KataExport.Service;

/// <summary>
/// The column stack's frame: mm along b (X) and h (Y) of the lowest rectangular column's plan axes, absolute elevation
/// (Z), mapped to model coordinates (ft). One mapping for the bars and the views that draw them, plus the plan
/// geometry of a bar's points.
/// </summary>
internal static class KataColumnFrame
{
    /// <summary>Plan offset (0.5 mm) below which the points of a bar count as one vertical plane.</summary>
    private static readonly double PlaneToleranceFt = RevitUnits.MmToFt(0.5);

    public static XYZ ToModel(Transform frame, double x, double y, double z)
    {
        var o = frame.Origin;
        var plan = new XYZ(o.X, o.Y, 0) + frame.BasisX * RevitUnits.MmToFt(x) + frame.BasisY * RevitUnits.MmToFt(y);
        return new XYZ(plan.X, plan.Y, RevitUnits.MmToFt(z));
    }

    /// <summary>Plan position (frame mm) of a bar's longest plumb segment: where it sits in its storey's section.</summary>
    public static (double X, double Y) PlumbPosition(IReadOnlyList<KataPoint3> points) =>
        points.Zip(points.Skip(1), (a, b) => (A: a, B: b))
            .Where(s => Math.Abs(s.B.X - s.A.X) < 1e-6 && Math.Abs(s.B.Y - s.A.Y) < 1e-6)
            .OrderByDescending(s => Math.Abs(s.B.Z - s.A.Z))
            .Select(s => (s.A.X, s.A.Y))
            .DefaultIfEmpty((points[0].X, points[0].Y))
            .First();

    /// <summary>
    /// The normal of the vertical plane a bar lies in (all its plan offsets on one line; a straight bar lies in the
    /// plane of <paramref name="straightAlong"/>), or null for a bar that bends in two plan directions (built free-form).
    /// </summary>
    public static XYZ? PlaneNormal(IReadOnlyList<XYZ> points, XYZ straightAlong)
    {
        XYZ? direction = null;
        foreach (var p in points.Skip(1))
        {
            var d = new XYZ(p.X - points[0].X, p.Y - points[0].Y, 0);
            if (d.GetLength() < 1e-6)
            {
                continue;
            }

            if (direction is null)
            {
                direction = d.Normalize();
            }
            else if (Math.Abs(direction.X * d.Y - direction.Y * d.X) > PlaneToleranceFt)
            {
                return null;
            }
        }

        return (direction ?? straightAlong).CrossProduct(XYZ.BasisZ).Normalize();
    }
}
