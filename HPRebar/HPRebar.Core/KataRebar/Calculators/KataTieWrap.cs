using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Lays out a tie whose hooks wrap two bars (<see cref="KataBarSet.WrapEnds"/>): the straight part runs one
/// bend radius beside the bars' centres and reaches past each by the radius plus half the tie, the outer face
/// of a hook turned round that bar. The radius is the model's (bar type and hook style), so it is a parameter.
/// </summary>
public static class KataTieWrap
{
    /// <param name="bendRadius">Centre-line radius of the hook bend (mm); 0 draws the tie through the bars' centres.</param>
    /// <returns>The tie's centre line at the set's first station, and the bar centres the start and end hooks turn round.</returns>
    public static (Polyline3 Shape, Point3 StartBar, Point3 EndBar) Lay(KataBarSet set, double bendRadius)
    {
        if (set is null) throw new ArgumentNullException(nameof(set));
        if (set.Shape.Points.Count != 2) throw new ArgumentException("A wrapping tie joins exactly two bars.", nameof(set));

        var c0 = set.Shape.Points[0];
        var c1 = set.Shape.Points[1];
        double ly = c1.Y - c0.Y, lz = c1.Z - c0.Z;
        double length = Math.Sqrt(ly * ly + lz * lz);
        if (length < 1e-6) throw new ArgumentException("The wrapped bars coincide.", nameof(set));

        double uy = ly / length, uz = lz / length;
        double ol = Math.Sqrt(set.WrapOffset.Y * set.WrapOffset.Y + set.WrapOffset.Z * set.WrapOffset.Z);
        double vy = ol > 0 ? set.WrapOffset.Y / ol : 0.0, vz = ol > 0 ? set.WrapOffset.Z / ol : 0.0;
        double reach = bendRadius > 0.0 ? bendRadius + set.Diameter / 2.0 : 0.0;

        var start = new Point3(c0.X, c0.Y + vy * bendRadius - uy * reach, c0.Z + vz * bendRadius - uz * reach);
        var end = new Point3(c1.X, c1.Y + vy * bendRadius + uy * reach, c1.Z + vz * bendRadius + uz * reach);
        return (new Polyline3(new List<Point3> { start, end }), c0, c1);
    }
}
