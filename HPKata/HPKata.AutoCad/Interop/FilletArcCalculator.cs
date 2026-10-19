using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Pure mathematical helper computing corner fillet tangents and vertex bulges (b = +/- tan(theta/4))
    /// for lightweight 2D polyline rebar geometry.
    /// </summary>
    public static class FilletArcCalculator
    {
        public const double Bulge45Deg = 0.198912367379658;
        public const double Bulge90Deg = 0.414213562373095; // sqrt(2) - 1
        public const double Bulge135Deg = 0.668178637919299;
        public const double Bulge180Deg = 1.0;

        public static void ComputeTurn(Point2d prev, Point2d corner, Point2d next, out double turnRad, out double crossZ)
        {
            Vector2d v1 = corner - prev;
            Vector2d v2 = next - corner;
            double len1 = v1.Length;
            double len2 = v2.Length;

            if (len1 < 1e-6 || len2 < 1e-6)
            {
                turnRad = 0.0;
                crossZ = 0.0;
                return;
            }

            Vector2d u1 = v1 / len1;
            Vector2d u2 = v2 / len2;

            double dot = (u1.X * u2.X) + (u1.Y * u2.Y);
            crossZ = (u1.X * u2.Y) - (u1.Y * u2.X);

            // Clamp dot for numerical stability
            dot = Math.Clamp(dot, -1.0, 1.0);
            turnRad = Math.Atan2(crossZ, dot);
        }

        public static double ComputeTangentDistance(double bendRadius, double turnRad)
        {
            double absTurn = Math.Abs(turnRad);
            if (absTurn < 1e-6 || Math.Abs(absTurn - Math.PI) < 1e-6)
            {
                return 0.0;
            }
            return bendRadius * Math.Tan(absTurn / 2.0);
        }

        public static double ClipTangent(double tangentDist, double len1, double len2, double factor = 0.45)
        {
            double maxAllowed = Math.Min(len1, len2) * factor;
            return Math.Min(tangentDist, maxAllowed);
        }

        public static double ComputeBulge(double turnRad, double crossZ)
        {
            double absTurn = Math.Abs(turnRad);
            if (absTurn < 1e-6)
            {
                return 0.0;
            }
            double sign = crossZ >= 0.0 ? 1.0 : -1.0;
            return sign * Math.Tan(absTurn / 4.0);
        }

        public static List<(Point2d Point, double Bulge)> GenerateFilletedVertices(
            IReadOnlyList<Point2d> points,
            double bendRadius,
            double clipFactor = 0.45)
        {
            var result = new List<(Point2d Point, double Bulge)>();
            if (points == null || points.Count == 0) return result;
            if (points.Count <= 2 || bendRadius <= 1e-3)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    result.Add((points[i], 0.0));
                }
                return result;
            }

            result.Add((points[0], 0.0));
            for (int i = 1; i < points.Count - 1; i++)
            {
                ProcessCorner(points[i - 1], points[i], points[i + 1], bendRadius, clipFactor, result);
            }
            result.Add((points[^1], 0.0));
            return result;
        }

        private static void ProcessCorner(
            Point2d prev,
            Point2d corner,
            Point2d next,
            double bendRadius,
            double clipFactor,
            List<(Point2d Point, double Bulge)> result)
        {
            ComputeTurn(prev, corner, next, out double turnRad, out double crossZ);
            double absTurn = Math.Abs(turnRad);

            if (absTurn < 1e-4)
            {
                result.Add((corner, 0.0));
                return;
            }

            Vector2d v1 = corner - prev;
            Vector2d v2 = next - corner;
            double tangentDist = ComputeTangentDistance(bendRadius, turnRad);
            double actualTangent = ClipTangent(tangentDist, v1.Length, v2.Length, clipFactor);

            Vector2d u1 = v1 / v1.Length;
            Vector2d u2 = v2 / v2.Length;

            Point2d t1 = corner - (u1 * actualTangent);
            Point2d t2 = corner + (u2 * actualTangent);
            double bulge = ComputeBulge(turnRad, crossZ);

            result.Add((t1, bulge));
            result.Add((t2, 0.0));
        }
    }
}
