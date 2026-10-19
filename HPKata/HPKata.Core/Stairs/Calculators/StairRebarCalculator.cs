using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Stairs.Models;

namespace HPKata.Core.Stairs.Calculators
{
    /// <summary>
    /// Computes staircase reinforcement cutting lengths and bar schedules,
    /// enforcing the structural Scissor-Crank crossing rule (L_ext >= 35d) at re-entrant corners.
    /// </summary>
    public static class StairRebarCalculator
    {
        public static List<RebarItem> CalculateStairRebar(StairFlightModel flight)
        {
            if (flight == null) throw new ArgumentNullException(nameof(flight));
            flight.Validate();

            var items = new List<RebarItem>();
            string mark = flight.FlightMark;
            int members = flight.MemberCount;
            double lExt = flight.ScissorCrankExtensionMm;

            // 1. Bottom Flight Tension Bar with Upper Scissor Extension (Shape 02)
            double ancLower = Math.Max(400.0, 30.0 * flight.BottomTensionDiaMm);
            double cutFlightBot = flight.SlopeLengthMm + ancLower + lExt;
            int countBot = (int)Math.Floor(Math.Max(1.0, flight.FlightWidthMm - 2.0 * flight.ConcreteCoverMm) / flight.BottomTensionSpacingMm) + 1;
            items.Add(CreateItem(mark, "1", "02", flight.BottomTensionDiaMm, countBot, members, cutFlightBot, $"a{flight.BottomTensionSpacingMm:F0}", "Thép nhịp bản thang (scissor chéo góc lõm)"));

            // 2. Upper Landing Bottom Bar crossing into flight (Scissor crank pair)
            double cutLandBot = flight.TopLandingLengthMm + lExt;
            items.Add(CreateItem(mark, "2", "02", flight.BottomTensionDiaMm, countBot, members, cutLandBot, $"a{flight.BottomTensionSpacingMm:F0}", "Thép đáy chiếu tới (scissor chéo góc lõm)"));

            // 3. Top Negative Support Bars (Thép mũ gối trên & dưới)
            double hookTop = 12.0 * flight.TopNegativeDiaMm;
            double lCutTop = Math.Max(flight.SlopeLengthMm / 4.0, 40.0 * flight.TopNegativeDiaMm);
            double cutNegTop = hookTop + flight.TopLandingLengthMm + lCutTop;
            int countNeg = (int)Math.Floor(Math.Max(1.0, flight.FlightWidthMm - 2.0 * flight.ConcreteCoverMm) / flight.TopNegativeSpacingMm) + 1;
            items.Add(CreateItem(mark, "3", "02", flight.TopNegativeDiaMm, countNeg, members, cutNegTop, $"a{flight.TopNegativeSpacingMm:F0}", "Thép gối trên bản thang"));

            double cutNegBot = hookTop + flight.BottomLandingLengthMm + lCutTop;
            items.Add(CreateItem(mark, "4", "02", flight.TopNegativeDiaMm, countNeg, members, cutNegBot, $"a{flight.TopNegativeSpacingMm:F0}", "Thép gối dưới bản thang"));

            // 4. Step Nosing Transverse Bars (Thép cấu tạo mũi bậc)
            double cutNose = Math.Max(200.0, flight.FlightWidthMm - 2.0 * flight.ConcreteCoverMm);
            int countNose = flight.RiserCount;
            items.Add(CreateItem(mark, "5", "01", flight.StepNoseDiaMm, countNose, members, cutNose, "1 thanh/mũi", "Thép phân bố mũi bậc thang"));

            // 5. Soffit Distribution Bars along waist
            int countSoffitDist = (int)Math.Floor(flight.SlopeLengthMm / flight.StepNoseSpacingMm) + 1;
            items.Add(CreateItem(mark, "6", "01", flight.StepNoseDiaMm, countSoffitDist, members, cutNose, $"a{flight.StepNoseSpacingMm:F0}", "Thép phân bố đáy bản thang"));

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
