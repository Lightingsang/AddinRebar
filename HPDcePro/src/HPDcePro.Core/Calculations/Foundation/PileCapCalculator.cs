using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDcePro.Core.Calculations.Foundation
{
    public static class PileCapCalculator
    {
        public static PileCapResult Calculate(PileCapInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Piles == null || input.Piles.Count == 0)
                throw new ArgumentException("Pile cap must contain at least 1 pile.", nameof(input));

            double selfWeight = ComputeSelfWeightAndSoil(input);
            double totalN = input.AxialForceNKn + selfWeight;

            var reactions = CalculatePileReactions(input, totalN);
            double maxR = reactions.Max(p => p.ReactionKn);
            double minR = reactions.Min(p => p.ReactionKn);
            double avgR = reactions.Average(p => p.ReactionKn);
            double capRatio = input.PileCapacityRcKn > 0 ? maxR / input.PileCapacityRcKn : 0.0;

            var (colPunchF, colPunchCap, colPunchRatio) = CheckColumnPunching(input, reactions);
            var (cornerF, cornerCap, cornerRatio) = CheckCornerPilePunching(input, reactions);
            var (shearF, shearCap, shearRatio) = CheckOneWayShear(input, reactions);
            var (mColX, asY, mColY, asX) = CalculateFlexuralRebar(input, reactions);
            var schedule = BuildCoordinateSchedule(reactions, input.PileCapacityRcKn);

            bool isSafe = capRatio <= 1.0 && colPunchRatio <= 1.0 &&
                          cornerRatio <= 1.0 && shearRatio <= 1.0;
            string summary = isSafe ? "PASS - Đủ khả năng chịu lực" : "FAIL - Vượt quá khả năng chịu tải";

            return new PileCapResult
            {
                Mark = input.Mark,
                PileCount = reactions.Count,
                Piles = reactions,
                MaxReactionKn = maxR,
                MinReactionKn = minR,
                AvgReactionKn = avgR,
                PileCapacityRatio = capRatio,
                SelfWeightAndSoilKn = selfWeight,
                ColumnPunchingForceKn = colPunchF,
                ColumnPunchingCapacityKn = colPunchCap,
                ColumnPunchingRatio = colPunchRatio,
                CornerPilePunchingForceKn = cornerF,
                CornerPilePunchingCapacityKn = cornerCap,
                CornerPilePunchingRatio = cornerRatio,
                OneWayShearForceKn = shearF,
                OneWayShearCapacityKn = shearCap,
                OneWayShearRatio = shearRatio,
                DesignMomentMxKnm = mColX,
                RequiredAsYMm2 = asY,
                DesignMomentMyKnm = mColY,
                RequiredAsXMm2 = asX,
                IsSafe = isSafe,
                StatusSummary = summary,
                CoordinateSchedule = schedule
            };
        }

        private static double ComputeSelfWeightAndSoil(PileCapInput input)
        {
            double volCapM3 = (input.WidthBMm * input.LengthLMm * input.ThicknessHMm) * 1e-9;
            double volSoilM3 = (input.WidthBMm * input.LengthLMm * input.SoilDepthM * 1000.0) * 1e-9;
            return (volCapM3 * input.PileCapDensityKnM3) + (volSoilM3 * input.SoilDensityKnM3);
        }

        public static IReadOnlyList<PileReactionResult> CalculatePileReactions(
            PileCapInput input, double totalN)
        {
            int n = input.Piles.Count;
            double avgX = input.Piles.Average(p => p.XMm);
            double avgY = input.Piles.Average(p => p.YMm);

            double sumX2 = input.Piles.Sum(p => Math.Pow(p.XMm - avgX, 2));
            double sumY2 = input.Piles.Sum(p => Math.Pow(p.YMm - avgY, 2));

            var list = new List<PileReactionResult>(n);
            for (int i = 0; i < n; i++)
            {
                var p = input.Piles[i];
                double dx = p.XMm - avgX;
                double dy = p.YMm - avgY;

                double r = totalN / n;
                if (sumX2 > 1e-3) r += (input.MomentMyKnm * 1e3 * dx) / sumX2;
                if (sumY2 > 1e-3) r += (input.MomentMxKnm * 1e3 * dy) / sumY2;

                list.Add(new PileReactionResult(p.PileId, p.XMm, p.YMm, r, input.PileCapacityRcKn));
            }
            return list;
        }

        private static (double Force, double Cap, double Ratio) CheckColumnPunching(
            PileCapInput input, IReadOnlyList<PileReactionResult> reactions)
        {
            double h0 = Math.Max(100.0, input.ThicknessHMm - input.ConcreteCoverMm);
            double cX = input.ColumnWidthBMm;
            double cY = input.ColumnHeightHMm;

            double uPm = 2.0 * (cX + h0) + 2.0 * (cY + h0);
            double cap = 0.75 * input.Concrete.Rbt * uPm * h0 * 1e-3;

            double halfX = (cX * 0.5) + h0;
            double halfY = (cY * 0.5) + h0;
            double reactionInside = reactions
                .Where(p => Math.Abs(p.XMm) <= halfX && Math.Abs(p.YMm) <= halfY)
                .Sum(p => p.ReactionKn);

            double force = Math.Max(0.0, input.AxialForceNKn - reactionInside);
            double ratio = cap > 0.0 ? force / cap : 0.0;
            return (force, cap, ratio);
        }

        private static (double Force, double Cap, double Ratio) CheckCornerPilePunching(
            PileCapInput input, IReadOnlyList<PileReactionResult> reactions)
        {
            double h0 = Math.Max(100.0, input.ThicknessHMm - input.ConcreteCoverMm);
            var maxPile = reactions.OrderByDescending(p => p.ReactionKn).First();
            double force = Math.Max(0.0, maxPile.ReactionKn);

            var origPile = input.Piles.FirstOrDefault(p => p.PileId == maxPile.PileId);
            double dPile = origPile?.DiameterMm ?? 400.0;
            double uPile = Math.PI * (dPile + h0);
            double cap = 0.75 * input.Concrete.Rbt * uPile * h0 * 1e-3;
            double ratio = cap > 0.0 ? force / cap : 0.0;
            return (force, cap, ratio);
        }

        private static (double Force, double Cap, double Ratio) CheckOneWayShear(
            PileCapInput input, IReadOnlyList<PileReactionResult> reactions)
        {
            double h0 = Math.Max(100.0, input.ThicknessHMm - input.ConcreteCoverMm);
            double cX = input.ColumnWidthBMm;
            double critFaceX = (cX * 0.5) + (h0 * 0.5);

            double force = reactions.Where(p => p.XMm >= critFaceX).Sum(p => p.ReactionKn);
            double cap = input.Concrete.Rbt * input.WidthBMm * h0 * 1e-3;
            double ratio = cap > 0.0 ? force / cap : 0.0;
            return (force, cap, ratio);
        }

        private static (double Mx, double AsY, double My, double AsX) CalculateFlexuralRebar(
            PileCapInput input, IReadOnlyList<PileReactionResult> reactions)
        {
            double h0 = Math.Max(100.0, input.ThicknessHMm - input.ConcreteCoverMm);
            double halfColX = input.ColumnWidthBMm * 0.5;
            double halfColY = input.ColumnHeightHMm * 0.5;

            double mColX = reactions.Where(p => p.YMm > halfColY)
                .Sum(p => p.ReactionKn * (p.YMm - halfColY) * 1e-3);
            double mColY = reactions.Where(p => p.XMm > halfColX)
                .Sum(p => p.ReactionKn * (p.XMm - halfColX) * 1e-3);

            double arm = 0.9 * h0;
            double asY = (mColX * 1e6) / (arm * input.Rebar.Rs);
            double asX = (mColY * 1e6) / (arm * input.Rebar.Rs);

            double minAsY = 0.001 * input.LengthLMm * h0;
            double minAsX = 0.001 * input.WidthBMm * h0;

            return (mColX, Math.Max(asY, minAsY), mColY, Math.Max(asX, minAsX));
        }

        public static IReadOnlyList<PileCoordinateItem> GenerateStandardLayout(
            PileLayoutPattern pattern, double pileDiameterMm, double spacingMultiplier = 3.0)
        {
            double s = pileDiameterMm * spacingMultiplier;
            var list = new List<PileCoordinateItem>();

            switch (pattern)
            {
                case PileLayoutPattern.SinglePile:
                    list.Add(new PileCoordinateItem("C1", 0.0, 0.0, pileDiameterMm));
                    break;
                case PileLayoutPattern.TwoPile:
                    list.Add(new PileCoordinateItem("C1", -s * 0.5, 0.0, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C2", s * 0.5, 0.0, pileDiameterMm));
                    break;
                case PileLayoutPattern.ThreePileTriangular:
                    double hTri = s * Math.Sqrt(3.0) * 0.5;
                    list.Add(new PileCoordinateItem("C1", 0.0, 2.0 * hTri / 3.0, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C2", -s * 0.5, -hTri / 3.0, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C3", s * 0.5, -hTri / 3.0, pileDiameterMm));
                    break;
                case PileLayoutPattern.FourPileRectangular:
                    list.Add(new PileCoordinateItem("C1", -s * 0.5, -s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C2", s * 0.5, -s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C3", s * 0.5, s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C4", -s * 0.5, s * 0.5, pileDiameterMm));
                    break;
                case PileLayoutPattern.FivePileDice:
                    list.Add(new PileCoordinateItem("C1", -s * 0.5, -s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C2", s * 0.5, -s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C3", s * 0.5, s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C4", -s * 0.5, s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C5", 0.0, 0.0, pileDiameterMm));
                    break;
                case PileLayoutPattern.SixPileRectangular:
                    list.Add(new PileCoordinateItem("C1", -s, -s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C2", 0.0, -s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C3", s, -s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C4", s, s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C5", 0.0, s * 0.5, pileDiameterMm));
                    list.Add(new PileCoordinateItem("C6", -s, s * 0.5, pileDiameterMm));
                    break;
            }
            return list;
        }

        private static IReadOnlyList<PileCoordinateScheduleItem> BuildCoordinateSchedule(
            IReadOnlyList<PileReactionResult> reactions, double rcKn)
        {
            var res = new List<PileCoordinateScheduleItem>(reactions.Count);
            for (int i = 0; i < reactions.Count; i++)
            {
                var r = reactions[i];
                bool safe = rcKn <= 0.0 || (r.ReactionKn <= rcKn && r.ReactionKn >= 0.0);
                res.Add(new PileCoordinateScheduleItem
                {
                    Index = i + 1,
                    PileId = r.PileId,
                    XLocalM = r.XMm * 1e-3,
                    YLocalM = r.YMm * 1e-3,
                    DiameterM = 0.4,
                    DesignCapacityKn = rcKn,
                    ReactionKn = r.ReactionKn,
                    Status = safe ? "PASS" : "FAIL"
                });
            }
            return res;
        }
    }
}
