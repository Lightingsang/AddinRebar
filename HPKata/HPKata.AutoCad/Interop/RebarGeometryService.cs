using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Calculators;
using HPKata.Core.Configuration;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Generates and inspects 2D lightweight Polyline rebar entities with fillet arc corners,
    /// thickness, layer, and cutting length calculations.
    /// </summary>
    public static class RebarGeometryService
    {
        public static Polyline CreatePolyline(
            IReadOnlyList<Point2d> sharpPoints,
            double bendRadius,
            string layer = "kata_thep chu",
            double constantWidth = 0.0,
            double elevation = 0.0)
        {
            if (sharpPoints == null || sharpPoints.Count < 2)
            {
                throw new ArgumentException("At least 2 points are required for a polyline.", nameof(sharpPoints));
            }

            var polyline = new Polyline();
            List<(Point2d Point, double Bulge)> filleted = FilletArcCalculator.GenerateFilletedVertices(sharpPoints, bendRadius);

            for (int i = 0; i < filleted.Count; i++)
            {
                polyline.AddVertexAt(i, filleted[i].Point, filleted[i].Bulge, 0.0, 0.0);
            }

            polyline.Layer = layer;
            polyline.ConstantWidth = constantWidth;
            polyline.Elevation = elevation;
            return polyline;
        }

        public static List<double> MeasureSegmentLengths(Polyline polyline)
        {
            var lengths = new List<double>();
            if (polyline == null || polyline.NumberOfVertices < 2) return lengths;

            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                double dist = polyline.GetDistanceAtParameter(i + 1) - polyline.GetDistanceAtParameter(i);
                lengths.Add(Math.Round(dist, 1));
            }
            return lengths;
        }

        public static double CalculateCuttingLength(Polyline polyline, int diameterMm, bool applyBendDeduction)
        {
            if (polyline == null || polyline.NumberOfVertices < 2) return 0.0;

            double rawLength = polyline.Length;
            if (!applyBendDeduction || diameterMm <= 0)
            {
                return Math.Round(rawLength, 0);
            }

            // Bend deductions per corner turn
            double deductionTotal = 0.0;
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                double bulge = polyline.GetBulgeAt(i);
                if (Math.Abs(bulge) > 1e-4)
                {
                    double turnAngleRad = 4.0 * Math.Atan(Math.Abs(bulge));
                    double angleDeg = turnAngleRad * 180.0 / Math.PI;
                    deductionTotal += RebarCalculator.BendDeduction(diameterMm, angleDeg, true);
                }
            }

            double cutLength = Math.Max(0.0, rawLength - deductionTotal);
            return Math.Round(cutLength, 0);
        }

        public static List<Point2d> ExtractSharpVertices(Polyline polyline)
        {
            var points = new List<Point2d>();
            if (polyline == null) return points;

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(polyline.GetPoint2dAt(i));
            }
            return points;
        }

        public static string ResolveLayer(int diameterMm, bool isStirrup, KataConfig config)
        {
            if (isStirrup)
            {
                return config.LayerThepDai;
            }
            return config.LayerThepChu;
        }
    }
}
