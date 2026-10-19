using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Stairs.Models;

namespace HPKata.Core.Stairs.Calculators
{
    /// <summary>
    /// Computes masonry opening lintel reinforcement cutting lengths, bar counts, and stirrup distribution.
    /// </summary>
    public static class LintelRebarCalculator
    {
        public static List<RebarItem> CalculateLintelRebar(LintelModel lintel)
        {
            if (lintel == null) throw new ArgumentNullException(nameof(lintel));
            lintel.Validate();

            var items = new List<RebarItem>();
            string mark = lintel.LintelMark;
            int members = lintel.MemberCount;

            // 1. Top Hanger Bars (Shape 02 with 90° downward end hooks)
            double hookH = Math.Max(50.0, lintel.HeightMm - 2.0 * lintel.ConcreteCoverMm);
            double lNet = lintel.TotalLengthMm - 2.0 * lintel.ConcreteCoverMm;
            double cutTop = lNet + 2.0 * hookH - 4.0 * lintel.TopHangerDiaMm;
            items.Add(CreateItem(mark, "1", "02", lintel.TopHangerDiaMm, lintel.TopHangerCount, members, cutTop, "-", "Thép giá lanh tô"));

            // 2. Bottom Tension Bars (Shape 02 with 90° upward end hooks)
            double cutBot = lNet + 2.0 * hookH - 4.0 * lintel.BottomTensionDiaMm;
            items.Add(CreateItem(mark, "2", "02", lintel.BottomTensionDiaMm, lintel.BottomTensionCount, members, cutBot, "-", "Thép chủ nhịp lanh tô"));

            // 3. Closed Stirrups (Shape 51)
            double bSt = Math.Max(50.0, lintel.WidthMm - 2.0 * lintel.ConcreteCoverMm);
            double hSt = Math.Max(50.0, lintel.HeightMm - 2.0 * lintel.ConcreteCoverMm);
            double cutTie = 2.0 * (bSt + hSt) + 2.0 * RebarCalculator.Hook135Length(lintel.StirrupDiaMm) - 8.0 * lintel.StirrupDiaMm;

            double endZone = lintel.BearingLengthMm + lintel.ClearSpanMm / 4.0;
            int countEnd = Math.Max(2, (int)Math.Floor(endZone / lintel.StirrupSpacingEndMm) + 1);
            int countMid = Math.Max(1, (int)Math.Floor((lintel.ClearSpanMm / 2.0) / lintel.StirrupSpacingMidMm) + 1);
            int totalStirrups = 2 * countEnd + countMid;

            items.Add(CreateItem(mark, "3", "51", lintel.StirrupDiaMm, totalStirrups, members, cutTie, $"a{lintel.StirrupSpacingEndMm:F0}/a{lintel.StirrupSpacingMidMm:F0}", "Đai lanh tô"));

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
