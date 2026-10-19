using System;

namespace HPDcePro.Core.Calculations.Secondary
{
    public static class LintelCalculator
    {
        public static LintelResult Calculate(LintelInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            ValidateInput(input);

            double totalSpanMm = input.ClearSpanL0Mm + (2.0 * input.BearingLengthMm);
            double calcSpanM = (input.ClearSpanL0Mm + input.BearingLengthMm) * 1e-3;

            double archH = Math.Min(input.WallHeightAboveMm, input.ClearSpanL0Mm / 3.0);
            double qTotal = ComputeDesignLoad(input, archH);

            double desM = (qTotal * Math.Pow(calcSpanM, 2)) / 8.0;
            double desV = (qTotal * calcSpanM) * 0.5;

            double h0 = Math.Max(50.0, input.LintelDepthHMm - 25.0);
            double asReq = (desM * 1e6) / (0.9 * h0 * input.Rebar.Rs);
            double asMin = 0.001 * input.WallThicknessBMm * h0;
            double asFinal = Math.Max(asReq, asMin);

            int barDia = SelectMainBarDiameter(asFinal);
            double shearCap = CalculateShearCapacity(input, h0);

            bool isBearingOk = input.BearingLengthMm >= 200.0;
            bool isShearOk = desV <= shearCap;
            bool isSpanOk = input.ClearSpanL0Mm >= 800.0 && input.ClearSpanL0Mm <= 3200.0;
            bool isSafe = isBearingOk && isShearOk && isSpanOk;

            string summary = isSafe
                ? "PASS - Lanh tô đủ khả năng chịu uốn, cắt và chiều dài gối tựa >= 200mm"
                : "FAIL - Chiều dài gối tựa hoặc khả năng chịu cắt lanh tô không đạt";

            return new LintelResult
            {
                Mark = input.Mark,
                TotalSpanLMm = totalSpanMm,
                BearingLengthMm = input.BearingLengthMm,
                EquivalentArchingHeightMm = archH,
                TotalDesignLoadQKnM = qTotal,
                DesignMomentKnm = desM,
                DesignShearKn = desV,
                RequiredBottomAsMm2 = asFinal,
                RecommendedMainBarCount = 2,
                RecommendedMainBarDiameter = barDia,
                ShearCapacityKn = shearCap,
                IsSafe = isSafe,
                StatusSummary = summary
            };
        }

        private static void ValidateInput(LintelInput input)
        {
            if (input.ClearSpanL0Mm <= 0 || input.BearingLengthMm <= 0 || input.LintelDepthHMm <= 0)
                throw new ArgumentException("Lintel dimensions must be strictly positive.", nameof(input));
            if (input.WallThicknessBMm <= 0)
                throw new ArgumentException("Wall thickness must be strictly positive.", nameof(input));
        }

        private static double ComputeDesignLoad(LintelInput input, double archH)
        {
            double bM = input.WallThicknessBMm * 1e-3;
            double hM = input.LintelDepthHMm * 1e-3;
            double gBeam = (bM * hM * 25.0) * 1.1;

            double archHM = archH * 1e-3;
            double gMasonry = (archHM * bM * input.MasonryDensityKnM3) * (2.0 / 3.0) * 1.2;
            return gBeam + gMasonry;
        }

        private static int SelectMainBarDiameter(double asReq)
        {
            int[] candidates = { 10, 12, 14, 16 };
            foreach (int dia in candidates)
            {
                double area2Bars = 2.0 * Math.PI * Math.Pow(dia * 0.5, 2);
                if (area2Bars >= asReq) return dia;
            }
            return 16;
        }

        private static double CalculateShearCapacity(LintelInput input, double h0)
        {
            double bM = input.WallThicknessBMm;
            double vb = 0.5 * input.Concrete.Rbt * bM * h0 * 1e-3;

            double asw = 2.0 * Math.PI * Math.Pow(input.StirrupDiameterMm * 0.5, 2);
            double rsw = 170.0; // stirrup design strength
            double vsw = (0.75 * rsw * asw * (h0 / Math.Max(input.StirrupSpacingMm, 50.0))) * 1e-3;

            return vb + vsw;
        }
    }
}
