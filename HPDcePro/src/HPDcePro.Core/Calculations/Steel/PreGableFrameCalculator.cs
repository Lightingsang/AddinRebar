using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Calculations.Steel
{
    /// <summary>
    /// Portal gable frame geometry, tapered built-up sections, and purlin layout calculator.
    /// </summary>
    public static class PreGableFrameCalculator
    {
        public static GableFrameResult Calculate(GableFrameInput input)
        {
            ValidateInput(input);

            double halfSpan = input.SpanMm * 0.5;
            double slopeRatio = input.RoofSlopePercent / 100.0;
            double apexRise = halfSpan * slopeRatio;
            double apexHeight = input.EaveHeightMm + apexRise;
            double slopeLen = Math.Sqrt((halfSpan * halfSpan) + (apexRise * apexRise));
            double slopeAngleRad = Math.Atan(slopeRatio);
            double slopeAngleDeg = slopeAngleRad * (180.0 / Math.PI);

            var (purlins, actualSpacing, countPerRafter) = LayoutPurlins(input, halfSpan, apexHeight, slopeLen, slopeAngleRad);
            var (factoredLoad, kneeMoment, apexMoment, vBase, hBase) = EstimateInternalForces(input);

            var secKnee = ComputeTaperedSection(0.0, slopeLen, input.RafterStartDepthMm, input.RafterEndDepthMm, input.RafterFlangeWidthMm, input.RafterFlangeThicknessMm, input.RafterWebThicknessMm);
            var secApex = ComputeTaperedSection(slopeLen, slopeLen, input.RafterStartDepthMm, input.RafterEndDepthMm, input.RafterFlangeWidthMm, input.RafterFlangeThicknessMm, input.RafterWebThicknessMm);

            double kneeStressRatio = (kneeMoment * 1e6) / (secKnee.WxMm3 * input.YieldStrengthMpa);
            double apexStressRatio = (apexMoment * 1e6) / (secApex.WxMm3 * input.YieldStrengthMpa);

            double allowableDeflection = input.SpanMm / 180.0;
            double avgIx = (secKnee.IxMm4 + secApex.IxMm4) * 0.5;
            double estimatedDeflection = EstimateDeflection(input, avgIx);

            return new GableFrameResult
            {
                ApexHeightMm = apexHeight,
                RafterSlopeAngleDeg = slopeAngleDeg,
                RafterSlopeLengthMm = slopeLen,
                BaseLeft = new Point2D(0, 0),
                EaveLeft = new Point2D(0, input.EaveHeightMm),
                Apex = new Point2D(halfSpan, apexHeight),
                EaveRight = new Point2D(input.SpanMm, input.EaveHeightMm),
                BaseRight = new Point2D(input.SpanMm, 0),
                Purlins = purlins,
                ActualPurlinSpacingMm = actualSpacing,
                PurlinCountPerRafter = countPerRafter,
                FactoredLoadKnM = factoredLoad,
                MaxKneeMomentKnm = kneeMoment,
                MaxApexMomentKnm = apexMoment,
                BaseVerticalReactionKn = vBase,
                BaseHorizontalThrustKn = hBase,
                KneeStressRatio = kneeStressRatio,
                ApexStressRatio = apexStressRatio,
                EstimatedRidgeDeflectionMm = estimatedDeflection,
                AllowableRidgeDeflectionMm = allowableDeflection
            };
        }

        public static TaperedSectionAtX ComputeTaperedSection(
            double xFromStartMm, double totalLengthMm,
            double startDepthMm, double endDepthMm,
            double flangeWidthMm, double flangeThicknessMm, double webThicknessMm)
        {
            double ratio = totalLengthMm > 0 ? Math.Max(0.0, Math.Min(1.0, xFromStartMm / totalLengthMm)) : 0.0;
            double depth = startDepthMm + ((endDepthMm - startDepthMm) * ratio);

            var props = SteelSectionProperties.CreateISection(depth, flangeWidthMm, webThicknessMm, flangeThicknessMm);
            return new TaperedSectionAtX
            {
                XFromStartMm = xFromStartMm,
                DepthMm = depth,
                FlangeWidthMm = flangeWidthMm,
                FlangeThicknessMm = flangeThicknessMm,
                WebThicknessMm = webThicknessMm,
                AreaMm2 = props.AreaMm2,
                IxMm4 = props.IxMm4,
                WxMm3 = props.WxMm3
            };
        }

        private static (IReadOnlyList<PurlinRun>, double, int) LayoutPurlins(
            GableFrameInput input, double halfSpan, double apexHeight, double slopeLen, double slopeAngleRad)
        {
            double usable = slopeLen - (input.EavePurlinOffsetMm + input.RidgePurlinOffsetMm);
            int spaces = Math.Max(1, (int)Math.Round(usable / input.TargetPurlinSpacingMm));
            double actualSpacing = usable / spaces;
            int countPerRafter = spaces + 1;

            var list = new List<PurlinRun>();
            double cos = Math.Cos(slopeAngleRad);
            double sin = Math.Sin(slopeAngleRad);

            for (int i = 0; i < countPerRafter; i++)
            {
                double dist = input.EavePurlinOffsetMm + (i * actualSpacing);
                double xLeft = dist * cos;
                double yLeft = input.EaveHeightMm + (dist * sin);
                list.Add(new PurlinRun { Index = i, Side = FrameRafterSide.Left, SlopeDistanceMm = dist, Coordinate = new Point2D(xLeft, yLeft) });

                double xRight = input.SpanMm - xLeft;
                list.Add(new PurlinRun { Index = i, Side = FrameRafterSide.Right, SlopeDistanceMm = dist, Coordinate = new Point2D(xRight, yLeft) });
            }

            return (list, actualSpacing, countPerRafter);
        }

        private static (double, double, double, double, double) EstimateInternalForces(GableFrameInput input)
        {
            double bayM = input.BaySpacingMm / 1000.0;
            double spanM = input.SpanMm / 1000.0;
            double eaveM = input.EaveHeightMm / 1000.0;

            double qFactored = ((1.2 * input.DeadLoadKnM2) + (1.5 * input.LiveLoadKnM2)) * bayM + 0.35;
            double kneeMoment = (qFactored * spanM * spanM) / 16.0;
            double apexMoment = (qFactored * spanM * spanM) / 24.0;
            double vBase = (qFactored * spanM) * 0.5;
            double hBase = eaveM > 0 ? kneeMoment / eaveM : 0.0;

            return (qFactored, kneeMoment, apexMoment, vBase, hBase);
        }

        private static double EstimateDeflection(GableFrameInput input, double avgIxMm4)
        {
            double bayM = input.BaySpacingMm / 1000.0;
            double spanM = input.SpanMm / 1000.0;
            double qServiceNPerMm = ((input.DeadLoadKnM2 + input.LiveLoadKnM2) * bayM);
            double numerator = 5.0 * qServiceNPerMm * Math.Pow(input.SpanMm, 4);
            double denominator = 384.0 * input.ElasticModulusMpa * (avgIxMm4 > 0 ? avgIxMm4 : 1.0);
            return (numerator / denominator) * 0.45; // two-hinged portal frame reduction factor
        }

        private static void ValidateInput(GableFrameInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.SpanMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.SpanMm), "Span must be positive.");
            if (input.EaveHeightMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.EaveHeightMm), "Eave height must be positive.");
            if (input.RoofSlopePercent <= 0) throw new ArgumentOutOfRangeException(nameof(input.RoofSlopePercent), "Roof slope must be positive.");
            if (input.BaySpacingMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.BaySpacingMm), "Bay spacing must be positive.");
            if (input.TargetPurlinSpacingMm <= 0) throw new ArgumentOutOfRangeException(nameof(input.TargetPurlinSpacingMm), "Purlin spacing must be positive.");
        }
    }
}
