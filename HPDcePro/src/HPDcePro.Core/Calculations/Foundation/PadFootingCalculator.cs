using System;

namespace HPDcePro.Core.Calculations.Foundation
{
    public static class PadFootingCalculator
    {
        public static PadFootingResult Calculate(PadFootingInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            ValidateDimensions(input);

            double areaM2 = (input.LengthLMm * input.WidthBMm) * 1e-6;
            double nSw = areaM2 * input.FoundationDepthM * input.AverageSoilDensityKnM3;
            double totalN = input.DesignAxialNKn + nSw;

            var (pMax, pMin, pAvg, ex, ey, hasUplift) = CalculateSoilPressure(input, totalN, areaM2);
            double soilRatio = input.AllowableSoilPressureRtcKpa > 0.0
                ? pMax / (1.2 * input.AllowableSoilPressureRtcKpa)
                : 0.0;

            var (punchF, punchCap, punchRatio) = CheckPunchingShear(input, pAvg);
            var (mFaceX, asX, mFaceY, asY) = CalculateFlexuralRebars(input, areaM2);

            bool isSafe = soilRatio <= 1.0 && punchRatio <= 1.0 && pAvg <= input.AllowableSoilPressureRtcKpa;
            string summary = isSafe
                ? "PASS - Móng đơn thỏa mãn điều kiện áp lực đất và chống chọc thủng"
                : "FAIL - Vượt quá khả năng chịu lực của nền hoặc móng";

            return new PadFootingResult
            {
                Mark = input.Mark,
                TotalVerticalLoadKn = totalN,
                BasePressureMaxKpa = pMax,
                BasePressureMinKpa = pMin,
                BasePressureAvgKpa = pAvg,
                EccentricityXMm = ex,
                EccentricityYMm = ey,
                HasUplift = hasUplift,
                SoilCapacityRatio = soilRatio,
                PunchingForceKn = punchF,
                PunchingCapacityKn = punchCap,
                PunchingRatio = punchRatio,
                MomentAtColumnFaceXkNm = mFaceX,
                RequiredAsXMm2 = asX,
                MomentAtColumnFaceYkNm = mFaceY,
                RequiredAsYMm2 = asY,
                IsOverallSafe = isSafe,
                StatusSummary = summary
            };
        }

        private static void ValidateDimensions(PadFootingInput input)
        {
            if (input.LengthLMm <= 0 || input.WidthBMm <= 0 || input.ThicknessHMm <= 0)
                throw new ArgumentException("Footing dimensions must be positive.", nameof(input));
            if (input.ColumnWidthBMm <= 0 || input.ColumnHeightHMm <= 0)
                throw new ArgumentException("Column dimensions must be positive.", nameof(input));
        }

        private static (double PMax, double PMin, double PAvg, double Ex, double Ey, bool HasUplift)
            CalculateSoilPressure(PadFootingInput input, double totalN, double areaM2)
        {
            double wx = (input.WidthBMm * Math.Pow(input.LengthLMm, 2)) * (1.0 / 6.0) * 1e-9;
            double wy = (input.LengthLMm * Math.Pow(input.WidthBMm, 2)) * (1.0 / 6.0) * 1e-9;

            double pAvg = totalN / areaM2;
            double sigX = wy > 1e-6 ? input.DesignMomentMxKnm / wy : 0.0;
            double sigY = wx > 1e-6 ? input.DesignMomentMyKnm / wx : 0.0;

            double ex = totalN > 0.0 ? (Math.Abs(input.DesignMomentMyKnm) / totalN) * 1e3 : 0.0;
            double ey = totalN > 0.0 ? (Math.Abs(input.DesignMomentMxKnm) / totalN) * 1e3 : 0.0;

            double pMax = pAvg + sigX + sigY;
            double pMin = pAvg - sigX - sigY;
            bool hasUplift = pMin < 0.0 || ex > (input.LengthLMm / 6.0) || ey > (input.WidthBMm / 6.0);

            if (pMin < 0.0)
            {
                pMin = 0.0;
                double bM = input.WidthBMm * 1e-3;
                double lM = input.LengthLMm * 1e-3;
                double exM = ex * 1e-3;
                double cZone = (lM * 0.5) - exM;
                if (cZone > 0.05)
                {
                    pMax = (2.0 * totalN) / (3.0 * bM * cZone);
                }
            }

            return (pMax, pMin, pAvg, ex, ey, hasUplift);
        }

        private static (double Force, double Cap, double Ratio) CheckPunchingShear(
            PadFootingInput input, double pAvg)
        {
            double h0 = Math.Max(100.0, input.ThicknessHMm - input.ConcreteCoverMm);
            double basePunchX = input.ColumnWidthBMm + (2.0 * h0);
            double basePunchY = input.ColumnHeightHMm + (2.0 * h0);
            double areaPunchM2 = (basePunchX * basePunchY) * 1e-6;

            double force = Math.Max(0.0, input.DesignAxialNKn - (pAvg * areaPunchM2));
            double uM = 2.0 * (input.ColumnWidthBMm + h0) + 2.0 * (input.ColumnHeightHMm + h0);
            double cap = 0.75 * input.Concrete.Rbt * uM * h0 * 1e-3;
            double ratio = cap > 0.0 ? force / cap : 0.0;

            return (force, cap, ratio);
        }

        private static (double Mx, double AsX, double My, double AsY) CalculateFlexuralRebars(
            PadFootingInput input, double areaM2)
        {
            double h0 = Math.Max(100.0, input.ThicknessHMm - input.ConcreteCoverMm);
            double pNet = input.DesignAxialNKn / areaM2;

            double c1 = Math.Max(0.0, (input.LengthLMm - input.ColumnWidthBMm) * 0.5);
            double c2 = Math.Max(0.0, (input.WidthBMm - input.ColumnHeightHMm) * 0.5);

            double mFaceX = 0.5 * pNet * Math.Pow(c1 * 1e-3, 2) * (input.WidthBMm * 1e-3);
            double mFaceY = 0.5 * pNet * Math.Pow(c2 * 1e-3, 2) * (input.LengthLMm * 1e-3);

            double arm = 0.9 * h0;
            double asX = (mFaceX * 1e6) / (arm * input.Rebar.Rs);
            double asY = (mFaceY * 1e6) / (arm * input.Rebar.Rs);

            double minAsX = 0.001 * input.WidthBMm * h0;
            double minAsY = 0.001 * input.LengthLMm * h0;

            return (mFaceX, Math.Max(asX, minAsX), mFaceY, Math.Max(asY, minAsY));
        }
    }
}
