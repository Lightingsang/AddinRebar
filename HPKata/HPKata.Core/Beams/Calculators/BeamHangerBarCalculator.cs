using System;
using System.Collections.Generic;
using HPKata.Core.Beams.Models;

namespace HPKata.Core.Beams.Calculators
{
    /// <summary>
    /// Calculates hanger stirrups and anti-crack bars for main-secondary beam intersections.
    /// Also handles beam splitting operations (cat_dam).
    /// </summary>
    public static class BeamHangerBarCalculator
    {
        public static BeamIntersectionModel CalculateHangers(
            int spanIndex,
            double distanceAlongSpanMm,
            string secondaryBeamName,
            double secondaryBeamWidthMm,
            double secondaryBeamHeightMm,
            int hangerCount = 5,
            int hangerDiaMm = 10,
            double hangerSpacingMm = 50.0)
        {
            var model = new BeamIntersectionModel
            {
                SpanIndex = spanIndex,
                DistanceAlongSpanMm = distanceAlongSpanMm,
                SecondaryBeamName = secondaryBeamName,
                SecondaryBeamWidthMm = secondaryBeamWidthMm,
                SecondaryBeamHeightMm = secondaryBeamHeightMm,
                HangerStirrupCount = hangerCount,
                HangerStirrupDiameterMm = hangerDiaMm,
                HangerStirrupSpacingMm = hangerSpacingMm,
                RequiresAntiCrackBars = secondaryBeamHeightMm >= 400.0,
                AntiCrackBarDiameterMm = 14,
                AntiCrackBarCount = 2,
                AntiCrackBarLengthMm = 1200.0
            };

            double cageWidth = (hangerCount > 1 ? (hangerCount - 1) * hangerSpacingMm : 0.0);
            double halfSecWidth = secondaryBeamWidthMm / 2.0;

            // Generate hanger offsets relative to intersection centerline
            var offsets = new List<double>();
            // Left group outside secondary beam face
            for (int i = 0; i < hangerCount; i++)
            {
                double offset = -halfSecWidth - (i + 1) * hangerSpacingMm;
                offsets.Add(Math.Round(offset, 1));
            }

            // Right group outside secondary beam face
            for (int i = 0; i < hangerCount; i++)
            {
                double offset = halfSecWidth + (i + 1) * hangerSpacingMm;
                offsets.Add(Math.Round(offset, 1));
            }

            offsets.Sort();
            model.HangerOffsetsMm = offsets;
            return model;
        }

        public static (BeamSpanModel LeftSpan, BeamSpanModel RightSpan) SplitBeamSpan(
            BeamSpanModel originalSpan,
            double splitDistanceMm,
            double supportWidthMm = 300.0)
        {
            if (splitDistanceMm <= 0.0 || splitDistanceMm >= originalSpan.ClearLengthMm)
            {
                throw new ArgumentException("Split distance must be strictly inside the clear span.");
            }

            var left = new BeamSpanModel
            {
                SpanIndex = originalSpan.SpanIndex,
                SpanName = $"{originalSpan.SpanName}_A",
                ClearLengthMm = splitDistanceMm - (supportWidthMm / 2.0),
                LeftSupportWidthMm = originalSpan.LeftSupportWidthMm,
                RightSupportWidthMm = supportWidthMm,
                WidthMm = originalSpan.WidthMm,
                HeightMm = originalSpan.HeightMm,
                TopElevationDeltaMm = originalSpan.TopElevationDeltaMm,
                SoffitDropMm = originalSpan.SoffitDropMm,
                LeftSlabThicknessMm = originalSpan.LeftSlabThicknessMm,
                RightSlabThicknessMm = originalSpan.RightSlabThicknessMm
            };

            var right = new BeamSpanModel
            {
                SpanIndex = originalSpan.SpanIndex + 1,
                SpanName = $"{originalSpan.SpanName}_B",
                ClearLengthMm = originalSpan.ClearLengthMm - splitDistanceMm - (supportWidthMm / 2.0),
                LeftSupportWidthMm = supportWidthMm,
                RightSupportWidthMm = originalSpan.RightSupportWidthMm,
                WidthMm = originalSpan.WidthMm,
                HeightMm = originalSpan.HeightMm,
                TopElevationDeltaMm = originalSpan.TopElevationDeltaMm,
                SoffitDropMm = originalSpan.SoffitDropMm,
                LeftSlabThicknessMm = originalSpan.LeftSlabThicknessMm,
                RightSlabThicknessMm = originalSpan.RightSlabThicknessMm
            };

            return (left, right);
        }
    }
}
