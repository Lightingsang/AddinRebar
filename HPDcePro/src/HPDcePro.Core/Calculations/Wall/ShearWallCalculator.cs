using System;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Wall
{
    public static class ShearWallCalculator
    {
        public static ShearWallResult Calculate(ShearWallInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            double lw = input.LengthLwMm;
            double tw = input.ThicknessTwMm;
            double aw = lw * tw;
            double ww = tw * lw * lw / 6.0;

            double axialN = input.DesignAxialNKn * 1e3;
            double momentM = Math.Abs(input.DesignMomentMuKnm) * 1e6;
            double sigmaC = (axialN / aw) + (momentM / ww);
            double threshold = 0.20 * input.Concrete.Rb;
            bool boundaryRequired = sigmaC > threshold;

            var boundary = CalculateBoundaryElement(input, boundaryRequired);
            var web = CalculateWebReinforcement(input);

            double h0 = 0.85 * lw;
            double vb = (0.30 * input.Concrete.Rbt * tw * h0) * 1e-3;
            double aswOnePitch = 2.0 * (Math.PI * Math.Pow(input.WebHorizontalBarDiameter, 2) / 4.0);
            double vsw = (0.80 * input.Rebar.Rsw * (aswOnePitch / input.WebHorizontalBarSpacingMm) * h0) * 1e-3;
            double vCrush = (0.30 * input.Concrete.Rb * tw * h0) * 1e-3;

            double vuMax = Math.Min(vb + vsw, vCrush);
            double shearUr = vuMax > 1e-3 ? Math.Abs(input.DesignShearVuKn) / vuMax : 999.0;
            bool isShearSafe = shearUr <= 1.0;
            bool isOverallSafe = isShearSafe && web.IsSufficient;

            string summary = isOverallSafe
                ? $"PASS (Vu={input.DesignShearVuKn:F1}kN, VuMax={vuMax:F1}kN, UR={shearUr:F2}, Boundary={boundaryRequired})"
                : $"FAIL (Vu={input.DesignShearVuKn:F1}kN, VuMax={vuMax:F1}kN, UR={shearUr:F2}, Boundary={boundaryRequired})";

            return new ShearWallResult(
                input.WallMark, sigmaC, threshold, boundaryRequired, boundary, web,
                vb, vsw, vuMax, shearUr, isShearSafe, isOverallSafe, summary);
        }

        private static BoundaryElementResult CalculateBoundaryElement(ShearWallInput input, bool isRequired)
        {
            double lw = input.LengthLwMm;
            double tw = input.ThicknessTwMm;

            double lc = Math.Max(0.15 * lw, Math.Max(1.5 * tw, 300.0));
            double bc = tw;
            double areaBc = lc * bc;
            double requiredAs = 0.005 * areaBc;

            int bestDia = 16;
            int bestCount = 6;
            double bestAs = bestCount * (Math.PI * bestDia * bestDia / 4.0);

            int[] diameters = { 16, 18, 20, 22, 25 };
            for (int dIdx = 0; dIdx < diameters.Length; dIdx++)
            {
                int d = diameters[dIdx];
                double barArea = Math.PI * d * d / 4.0;
                int count = (int)Math.Ceiling(requiredAs / barArea);
                if (count % 2 != 0) count++;
                if (count < 6) count = 6;

                double provided = count * barArea;
                if (provided >= requiredAs)
                {
                    bestDia = d;
                    bestCount = count;
                    bestAs = provided;
                    break;
                }
            }

            double ratio = (bestAs / areaBc) * 100.0;
            int tieDia = Math.Max(8, (int)Math.Ceiling(bestDia / 3.0));
            double tieSpacing = Math.Min(100.0, 10.0 * bestDia);

            return new BoundaryElementResult(
                isRequired, lc, bc, bestCount, bestDia, bestAs, ratio, tieDia, tieSpacing);
        }

        private static WebReinforcementResult CalculateWebReinforcement(ShearWallInput input)
        {
            double tw = input.ThicknessTwMm;
            double asv = 2.0 * (Math.PI * Math.Pow(input.WebVerticalBarDiameter, 2) / 4.0);
            double muV = (asv / (tw * input.WebVerticalBarSpacingMm)) * 100.0;

            double ash = 2.0 * (Math.PI * Math.Pow(input.WebHorizontalBarDiameter, 2) / 4.0);
            double muH = (ash / (tw * input.WebHorizontalBarSpacingMm)) * 100.0;

            bool isSufficient = muV >= 0.25 && muH >= 0.25 && input.CrossTiesPerM2 >= 4;

            return new WebReinforcementResult(muV, muH, 2, input.CrossTiesPerM2, isSufficient);
        }
    }
}
