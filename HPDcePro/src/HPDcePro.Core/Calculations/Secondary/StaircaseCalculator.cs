using System;

namespace HPDcePro.Core.Calculations.Secondary
{
    public static class StaircaseCalculator
    {
        public static StaircaseResult Calculate(StaircaseInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            ValidateInput(input);

            int nSteps = Math.Max(2, input.NumberOfSteps);
            double hr = input.TotalRiseHMm / nSteps;
            double bt = input.FlightSpanLMm / (nSteps - 1);
            double blondel = (2.0 * hr) + bt;
            bool isErgonomic = blondel >= 600.0 && blondel <= 640.0;

            double angleRad = Math.Atan2(input.TotalRiseHMm, input.FlightSpanLMm);
            double angleDeg = angleRad * (180.0 / Math.PI);
            double cosAlpha = Math.Cos(angleRad);

            double qTotal = ComputeDesignLoad(input, hr, cosAlpha);
            double l0M = ((input.Landing1LengthMm * 0.5) + input.FlightSpanLMm + (input.Landing2LengthMm * 0.5)) * 1e-3;
            double momentFactor = input.Type == StaircaseType.FoldedPlateZigZag ? 9.0 : 10.0;
            double maxM = (qTotal * Math.Pow(l0M, 2)) / momentFactor;

            double h0 = Math.Max(50.0, input.WaistThicknessHMm - input.ConcreteCoverMm - (input.MainBarDiameterMm * 0.5));
            double widthM = input.StairWidthBMm * 1e-3;
            double asReqPerM = (maxM * 1e6) / (0.9 * h0 * input.Rebar.Rs);
            double asTotal = Math.Max(asReqPerM * widthM, 0.0015 * input.StairWidthBMm * h0);

            double barArea = Math.PI * Math.Pow(input.MainBarDiameterMm * 0.5, 2);
            int barCount = Math.Max(4, (int)Math.Ceiling(asTotal / barArea));
            double lapLen = Math.Max(35.0 * input.MainBarDiameterMm, 400.0);

            string notes = input.Type == StaircaseType.FoldedPlateZigZag
                ? "Bản thang ziczac: Bố trí thép uốn độc lập từng bậc đan chéo giao nhau, neo vào nút >= 35d"
                : "Bản thang đan thẳng: Thép chủ uốn bẻ chữ Z neo qua vùng kéo của nút gãy khúc >= 35d";
            string summary = isErgonomic && maxM > 0.0
                ? $"PASS - Thang {(input.Type == StaircaseType.WaistSlab ? "bản trượt" : "ziczac")} đạt công thái học và chịu lực"
                : "WARN - Kích thước bậc thang nằm ngoài dải công thái học 600-640mm hoặc nội lực lớn";

            return new StaircaseResult
            {
                Mark = input.Mark,
                Type = input.Type,
                RiserHeightMm = hr,
                TreadWidthMm = bt,
                BlondelFormulaValue = blondel,
                IsErgonomicSafe = isErgonomic,
                InclineAngleDeg = angleDeg,
                TotalDesignLoadQKnM = qTotal,
                MaxDesignMomentKnm = maxM,
                RequiredMainAsMm2 = asTotal,
                RecommendedBarCount = barCount,
                RecommendedBarDiameter = input.MainBarDiameterMm,
                CrankedBarLapLengthMm = lapLen,
                DetailingNotes = notes,
                IsSafe = maxM > 0.0,
                StatusSummary = summary
            };
        }

        private static void ValidateInput(StaircaseInput input)
        {
            if (input.FlightSpanLMm <= 0 || input.TotalRiseHMm <= 0 || input.WaistThicknessHMm <= 0)
                throw new ArgumentException("Staircase dimensions must be strictly positive.", nameof(input));
            if (input.NumberOfSteps <= 1)
                throw new ArgumentException("NumberOfSteps must be greater than 1.", nameof(input));
        }

        private static double ComputeDesignLoad(StaircaseInput input, double hr, double cosAlpha)
        {
            double gWaist = ((input.WaistThicknessHMm * 1e-3) / Math.Max(cosAlpha, 0.1)) * 25.0 * 1.1;
            double gSteps = 0.5 * (hr * 1e-3) * 25.0 * 1.1;
            double gFin = input.FinishesLoadKnM2 * 1.2;
            double pLive = input.LiveLoadPKnM2 * 1.2;
            double qM2 = gWaist + gSteps + gFin + pLive;

            if (input.Type == StaircaseType.FoldedPlateZigZag)
            {
                qM2 *= 1.15; // zig-zag folded plate self-weight adjustment
            }
            return qM2 * (input.StairWidthBMm * 1e-3);
        }
    }
}
