using System;
using HPKata.Core.CadUtilities.Models;

namespace HPKata.Core.CadUtilities.Calculators
{
    /// <summary>
    /// Vector projection algorithms for dimension line witness trimming and boundary intersections.
    /// </summary>
    public static class VectorProjectionHelper
    {
        public static Point2D ProjectPointOnLine(Point2D pt, Point2D linePt, Vector2D lineDir)
        {
            var u = lineDir.Normalize();
            var v = pt.Subtract(linePt);
            double t = v.Dot(u);
            return linePt.Add(u.Scale(t));
        }

        public static (Point2D newExt1, Point2D newExt2) TrimWitnessLines(
            Point2D ext1,
            Point2D ext2,
            Point2D dimLinePt,
            double offsetFromDimLineMm)
        {
            var uDim = ext2.Subtract(ext1).Normalize();
            var n = uDim.Normal(); // Direction of witness lines

            // Project dimLinePt onto witness line 1 and 2
            var pDim1 = ProjectPointOnLine(dimLinePt, ext1, n);
            var pDim2 = ProjectPointOnLine(dimLinePt, ext2, n);

            // Determine direction vector from dimension line toward the original witness points
            var dir1 = ext1.Subtract(pDim1).Normalize();
            var dir2 = ext2.Subtract(pDim2).Normalize();

            // If points are coincident with dimline, use normal
            if (dir1.Length < 1e-6) dir1 = n;
            if (dir2.Length < 1e-6) dir2 = n;

            var newExt1 = pDim1.Add(dir1.Scale(offsetFromDimLineMm));
            var newExt2 = pDim2.Add(dir2.Scale(offsetFromDimLineMm));

            return (newExt1, newExt2);
        }

        public static double DistancePointToLine(Point2D pt, Point2D lineStart, Point2D lineEnd)
        {
            var lineVec = lineEnd.Subtract(lineStart);
            double len = lineVec.Length;
            if (len < 1e-9) return pt.DistanceTo(lineStart);

            var u = lineVec.Normalize();
            var v = pt.Subtract(lineStart);
            double proj = v.Dot(u);
            var projPt = lineStart.Add(u.Scale(proj));
            return pt.DistanceTo(projPt);
        }
    }
}
