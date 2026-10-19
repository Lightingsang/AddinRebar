using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;
using HPKata.Core.Models;

namespace HPKata.Core.Slabs.Calculators
{
    /// <summary>
    /// Computes 4-way reinforcement (orthogonal + 45-degree diagonal) and drop panel (drpn) stepped transitions.
    /// </summary>
    public static class SlabFourWayCalculator
    {
        public static List<RebarItem> CalculateFourWayRebar(
            string panelMark,
            double lengthMm,
            double widthMm,
            double thicknessMm,
            double coverMm = 15.0,
            int orthoDiaMm = 10,
            double orthoSpacingMm = 150.0,
            int diagDiaMm = 10,
            double diagSpacingMm = 200.0,
            int memberCount = 1)
        {
            if (lengthMm <= 0 || widthMm <= 0) throw new ArgumentOutOfRangeException("Dimensions must be positive.");
            var items = new List<RebarItem>();

            // 1. Layer 1 (0 deg, along length)
            double cut1 = Math.Max(200.0, lengthMm - 2.0 * coverMm);
            int count1 = (int)Math.Floor(Math.Max(1.0, widthMm - 2.0 * coverMm) / orthoSpacingMm) + 1;
            items.Add(CreateItem(panelMark, "4D-1", "01", orthoDiaMm, count1, memberCount, cut1, $"a{orthoSpacingMm:F0}", "Thép 4D phương 0°"));

            // 2. Layer 2 (90 deg, along width)
            double cut2 = Math.Max(200.0, widthMm - 2.0 * coverMm);
            int count2 = (int)Math.Floor(Math.Max(1.0, lengthMm - 2.0 * coverMm) / orthoSpacingMm) + 1;
            items.Add(CreateItem(panelMark, "4D-2", "01", orthoDiaMm, count2, memberCount, cut2, $"a{orthoSpacingMm:F0}", "Thép 4D phương 90°"));

            // 3. Layer 3 (+45 deg diagonal)
            double diagSpan = Math.Sqrt(lengthMm * lengthMm + widthMm * widthMm) - 2.0 * coverMm;
            int countDiag = (int)Math.Floor(Math.Min(lengthMm, widthMm) / diagSpacingMm) + 1;
            items.Add(CreateItem(panelMark, "4D-3", "01", diagDiaMm, countDiag, memberCount, diagSpan, $"a{diagSpacingMm:F0}", "Thép 4D chéo +45°"));

            // 4. Layer 4 (-45 deg diagonal)
            items.Add(CreateItem(panelMark, "4D-4", "01", diagDiaMm, countDiag, memberCount, diagSpan, $"a{diagSpacingMm:F0}", "Thép 4D chéo -45°"));

            return items;
        }

        public static List<RebarItem> CalculateDropPanelRebar(
            string panelMark,
            double panelSizeMm,
            double dropDepthMm,
            double slabThicknessMm,
            double coverMm = 15.0,
            int barDiaMm = 12,
            double barSpacingMm = 150.0,
            int memberCount = 1)
        {
            if (panelSizeMm <= 0 || dropDepthMm <= 0) throw new ArgumentOutOfRangeException("Drop panel dimensions must be positive.");
            var items = new List<RebarItem>();

            // Stepped transition bar (1:3 slope)
            double slopeLen = Math.Sqrt(dropDepthMm * dropDepthMm + Math.Pow(3.0 * dropDepthMm, 2));
            double flatLen = Math.Max(200.0, panelSizeMm - 2.0 * coverMm);
            double totalCut = flatLen + 2.0 * slopeLen + 300.0;
            int barCount = (int)Math.Floor(panelSizeMm / barSpacingMm) + 1;

            items.Add(CreateItem(panelMark, "DP-1", "50", barDiaMm, barCount, memberCount, totalCut, $"a{barSpacingMm:F0}", "Thép giật cấp drop panel X"));
            items.Add(CreateItem(panelMark, "DP-2", "50", barDiaMm, barCount, memberCount, totalCut, $"a{barSpacingMm:F0}", "Thép giật cấp drop panel Y"));

            // Closed perimeter shear tie
            double tiePerimeter = 4.0 * (panelSizeMm - 2.0 * coverMm) + 200.0;
            items.Add(CreateItem(panelMark, "DP-T", "51", 10, 3, memberCount, tiePerimeter, "a100", "Đai chu vi drop panel"));

            return items;
        }

        private static RebarItem CreateItem(
            string compMark, string barMark, string shape, int dia, int count, int members, double cutLen, string spacing, string notes)
        {
            return new RebarItem
            {
                ComponentMark = compMark,
                BarMark = barMark,
                ShapeCode = shape,
                DiameterMm = dia,
                CountPerMember = count,
                MemberCount = members,
                CuttingLengthMm = Math.Round(cutLen, 1),
                StructuralLengthMm = Math.Round(cutLen, 1),
                Spacing = spacing,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(dia),
                Notes = notes
            };
        }
    }
}
