using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Calculators;
using HPKata.Core.Models;

namespace HPKata.Core.Slabs.Calculators
{
    /// <summary>
    /// Computes parallel rebar chords, length quantization (Delta L = 50mm),
    /// and variable mark clustering (1a, 1b...) for irregular polygon slab panels.
    /// </summary>
    public static class SlabPolylineSteppingCalculator
    {
        public static List<RebarItem> CalculateSteppingRebar(
            string panelMark,
            List<(double X, double Y)> polygonVertices,
            double spacingMm = 150.0,
            double coverMm = 15.0,
            int diameterMm = 10,
            double deltaLMm = 50.0,
            int memberCount = 1)
        {
            if (polygonVertices == null || polygonVertices.Count < 3)
                throw new ArgumentException("Polygon must have at least 3 vertices.", nameof(polygonVertices));
            if (spacingMm <= 0) throw new ArgumentOutOfRangeException(nameof(spacingMm));
            double step = deltaLMm > 0 ? deltaLMm : 50.0;

            double minY = polygonVertices.Min(p => p.Y) + coverMm;
            double maxY = polygonVertices.Max(p => p.Y) - coverMm;
            if (maxY <= minY) return new List<RebarItem>();

            var chords = CollectChords(polygonVertices, minY, maxY, spacingMm, coverMm, step);
            return BuildSteppedItems(panelMark, chords, diameterMm, spacingMm, step, memberCount);
        }

        private static List<double> CollectChords(
            List<(double X, double Y)> polygonVertices, double minY, double maxY, double spacingMm, double coverMm, double step)
        {
            var chords = new List<double>();
            for (double y = minY + spacingMm / 2.0; y < maxY; y += spacingMm)
            {
                var xIntersects = FindXIntersections(polygonVertices, y);
                if (xIntersects.Count >= 2)
                {
                    xIntersects.Sort();
                    for (int i = 0; i < xIntersects.Count - 1; i += 2)
                    {
                        double rawLen = (xIntersects[i + 1] - xIntersects[i]) - 2.0 * coverMm;
                        if (rawLen >= 200.0)
                        {
                            double quantized = Math.Round(rawLen / step) * step;
                            chords.Add(quantized);
                        }
                    }
                }
            }
            return chords;
        }

        private static List<RebarItem> BuildSteppedItems(
            string panelMark, List<double> chords, int diameterMm, double spacingMm, double step, int memberCount)
        {
            var grouped = chords.GroupBy(len => len).OrderBy(g => g.Key).ToList();
            var items = new List<RebarItem>();
            char suffix = 'a';
            foreach (var g in grouped)
            {
                string mark = $"1{suffix++}";
                double cutLen = g.Key;
                items.Add(new RebarItem
                {
                    ComponentMark = panelMark,
                    BarMark = mark,
                    ShapeCode = "01",
                    DiameterMm = diameterMm,
                    CountPerMember = g.Count(),
                    MemberCount = memberCount,
                    CuttingLengthMm = cutLen,
                    StructuralLengthMm = cutLen,
                    Spacing = $"a{spacingMm:F0}",
                    UnitMassKgPerM = RebarCalculator.GetUnitMass(diameterMm),
                    Notes = $"Thép sàn biến thiên (bước {step:F0}mm)"
                });
            }
            return items;
        }

        private static List<double> FindXIntersections(List<(double X, double Y)> poly, double y)
        {
            var results = new List<double>();
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                var p1 = poly[i];
                var p2 = poly[(i + 1) % n];

                if ((p1.Y <= y && p2.Y > y) || (p2.Y <= y && p1.Y > y))
                {
                    double t = (y - p1.Y) / (p2.Y - p1.Y);
                    double x = p1.X + t * (p2.X - p1.X);
                    results.Add(x);
                }
            }
            return results;
        }
    }
}
