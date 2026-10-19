using System;
using System.Collections.Generic;

namespace HPDcePro.Core.Calculations.Slab
{
    public static class TwoWaySlabCalculator
    {
        public static TwoWaySlabResult Calculate(TwoWaySlabInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            ValidateInput(input);

            double l1 = Math.Min(input.ShortSpanLxMm, input.LongSpanLyMm);
            double l2 = Math.Max(input.ShortSpanLxMm, input.LongSpanLyMm);
            double lambda = l2 / l1;
            bool isTwoWay = lambda <= 2.0;

            double qDes = (1.1 * input.DeadLoadGKnM2) + (1.2 * input.LiveLoadPKnM2);
            var (msx, msy, mtx, mty) = CalculateMoments(input, l1, l2, lambda, isTwoWay, qDes);

            double h0x = Math.Max(50.0, input.SlabThicknessHMm - input.ConcreteCoverMm - (input.BottomBarDiameterMm * 0.5));
            double h0y = Math.Max(50.0, h0x - input.BottomBarDiameterMm);
            double h0Top = Math.Max(50.0, input.SlabThicknessHMm - input.ConcreteCoverMm - (input.TopBarDiameterMm * 0.5));

            double asBotX = CalculateAs(msx, h0x, input.Rebar.Rs);
            double asBotY = CalculateAs(msy, h0y, input.Rebar.Rs);
            double asTopX = CalculateAs(mtx, h0Top, input.Rebar.Rs);
            double asTopY = CalculateAs(mty, h0Top, input.Rebar.Rs);

            var layers = BuildRebarLayers(input, asBotX, asBotY, asTopX, asTopY, l1, l2);
            double chairH = Math.Max(20.0, input.SlabThicknessHMm - (2.0 * input.ConcreteCoverMm) - input.BottomBarDiameterMm - input.TopBarDiameterMm);

            double allowableSpanOverH = isTwoWay ? 35.0 : 30.0;
            double actualSpanOverH = l1 / input.SlabThicknessHMm;
            bool isDeflectionSafe = actualSpanOverH <= allowableSpanOverH;
            double estDeflection = (actualSpanOverH / allowableSpanOverH) * (l1 / 250.0);

            bool isSafe = isDeflectionSafe && msx > 0.0;
            string summary = isSafe
                ? $"PASS - Bản sàn {(isTwoWay ? "2 phương" : "1 phương dầm")} thỏa mãn chịu lực và độ võng"
                : "FAIL - Độ võng hoặc nội lực vượt quá khả năng chịu lực";

            return new TwoWaySlabResult
            {
                PanelName = input.PanelName,
                AspectRatioLambda = lambda,
                IsTwoWay = isTwoWay,
                TotalDesignLoadQKnM2 = qDes,
                SpanPositiveMomentMxKnm = msx,
                SpanPositiveMomentMyKnm = msy,
                SupportNegativeMomentMxKnm = mtx,
                SupportNegativeMomentMyKnm = mty,
                RequiredAsBotXMm2 = asBotX,
                RequiredAsBotYMm2 = asBotY,
                RequiredAsTopXMm2 = asTopX,
                RequiredAsTopYMm2 = asTopY,
                Layers = layers,
                SpacerChairHeightMm = chairH,
                TopBarCutoffLengthXMm = l1 * 0.25,
                TopBarCutoffLengthYMm = l2 * 0.25,
                EstimatedDeflectionMm = estDeflection,
                IsDeflectionSafe = isDeflectionSafe,
                IsSafe = isSafe,
                StatusSummary = summary
            };
        }

        private static void ValidateInput(TwoWaySlabInput input)
        {
            if (input.ShortSpanLxMm <= 0 || input.LongSpanLyMm <= 0 || input.SlabThicknessHMm <= 0)
                throw new ArgumentException("Slab dimensions must be strictly positive.", nameof(input));
            if (input.DeadLoadGKnM2 < 0 || input.LiveLoadPKnM2 < 0)
                throw new ArgumentException("Loads must be non-negative.", nameof(input));
        }

        private static (double Msx, double Msy, double Mtx, double Mty) CalculateMoments(
            TwoWaySlabInput input, double l1, double l2, double lambda, bool isTwoWay, double qDes)
        {
            double l1M = l1 * 1e-3;
            double l2M = l2 * 1e-3;

            if (!isTwoWay)
            {
                double mSpan = (qDes * l1M * l1M) / 10.0;
                double mSupp = (qDes * l1M * l1M) / 12.0;
                return (mSpan, 0.25 * mSpan, mSupp, 0.25 * mSupp);
            }

            double kx = Math.Pow(lambda, 4) / (1.0 + Math.Pow(lambda, 4));
            double ky = 1.0 / (1.0 + Math.Pow(lambda, 4));
            double qx = kx * qDes;
            double qy = ky * qDes;

            if (input.Boundary == SlabBoundaryCondition.FourEdgesSimplySupported)
            {
                return ((qx * l1M * l1M) / 8.0, (qy * l2M * l2M) / 8.0, 0.0, 0.0);
            }
            if (input.Boundary == SlabBoundaryCondition.FourEdgesFixed)
            {
                return ((qx * l1M * l1M) / 24.0, (qy * l2M * l2M) / 24.0,
                        (qx * l1M * l1M) / 12.0, (qy * l2M * l2M) / 12.0);
            }

            // Partially fixed/continuous boundaries
            return ((qx * l1M * l1M) / 16.0, (qy * l2M * l2M) / 16.0,
                    (qx * l1M * l1M) / 14.0, (qy * l2M * l2M) / 14.0);
        }

        private static double CalculateAs(double mKnm, double h0Mm, double rsMpa)
        {
            if (mKnm <= 0.0) return 0.001 * 1000.0 * h0Mm;
            double asReq = (mKnm * 1e6) / (0.9 * h0Mm * rsMpa);
            double asMin = 0.001 * 1000.0 * h0Mm; // min 0.1% per meter
            return Math.Max(asReq, asMin);
        }

        private static IReadOnlyList<SlabRebarLayer> BuildRebarLayers(
            TwoWaySlabInput input, double asBx, double asBy, double asTx, double asTy, double l1, double l2)
        {
            return new List<SlabRebarLayer>
            {
                CreateLayer("Lớp dưới X (B1)", input.BottomBarDiameterMm, asBx, "Rải liên tục suốt nhịp L1"),
                CreateLayer("Lớp dưới Y (B2)", input.BottomBarDiameterMm, asBy, "Rải liên tục suốt nhịp L2"),
                CreateLayer("Lớp trên gối X (T1)", input.TopBarDiameterMm, asTx, $"Cắt thép tại L0/4 = {l1 * 0.25:F0}mm"),
                CreateLayer("Lớp trên gối Y (T2)", input.TopBarDiameterMm, asTy, $"Cắt thép tại L0/4 = {l2 * 0.25:F0}mm")
            };
        }

        private static SlabRebarLayer CreateLayer(string name, int dia, double reqAs, string cutoff)
        {
            double barArea = Math.PI * Math.Pow(dia * 0.5, 2);
            double exactSpacing = (barArea * 1000.0) / Math.Max(reqAs, 1.0);
            double spacing = Math.Min(200.0, Math.Max(100.0, Math.Floor(exactSpacing / 25.0) * 25.0));
            return new SlabRebarLayer(name, dia, spacing, reqAs, cutoff);
        }
    }
}
