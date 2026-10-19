using System;
using System.Collections.Generic;
using HPKata.Core.Beams.Models;

namespace HPKata.Core.Beams.Calculators
{
    /// <summary>
    /// Calculates stirrup zone distributions, spacing, leg configurations, and station coordinates.
    /// </summary>
    public static class BeamStirrupZoneCalculator
    {
        public static StirrupZoneDistributionModel CalculateZones(
            int spanIndex,
            double clearSpanMm,
            double beamWidthMm,
            double beamHeightMm,
            int stirrupDiaMm = 8,
            double denseSpacingMm = 100.0,
            double sparseSpacingMm = 200.0,
            bool isCantileverLeft = false,
            bool isCantileverRight = false)
        {
            double denseZone = Math.Max(clearSpanMm / 4.0, 2.0 * beamHeightMm);
            denseZone = Math.Min(denseZone, clearSpanMm / 2.0);
            denseZone = Math.Round(denseZone / 50.0) * 50.0;
            if (denseZone * 2.0 > clearSpanMm)
            {
                denseZone = Math.Floor((clearSpanMm / 2.0) / 50.0) * 50.0;
            }
            denseZone = Math.Max(0.0, denseZone);

            double leftDense = isCantileverLeft ? 0.0 : denseZone;
            double rightDense = isCantileverRight ? 0.0 : denseZone;
            double midspanZone = Math.Max(0.0, clearSpanMm - (leftDense + rightDense));

            int legCount = ResolveLegCount(beamWidthMm);
            int closedHoops = legCount >= 4 ? 1 : 1;
            int cTies = legCount >= 4 ? legCount - 2 : 0;

            return new StirrupZoneDistributionModel
            {
                SpanIndex = spanIndex,
                StirrupDiameterMm = stirrupDiaMm,
                LeftDenseZoneLengthMm = leftDense,
                LeftDenseSpacingMm = denseSpacingMm,
                MidspanSparseZoneLengthMm = midspanZone,
                MidspanSparseSpacingMm = sparseSpacingMm,
                RightDenseZoneLengthMm = rightDense,
                RightDenseSpacingMm = denseSpacingMm,
                TotalLegCount = legCount,
                ClosedHoopCount = closedHoops,
                CTieCount = cTies
            };
        }

        public static StirrupZoneDistributionModel CalculateZones(
            BeamSpanModel span,
            int stirrupDiaMm = 8,
            double denseSpacingMm = 100.0,
            double sparseSpacingMm = 200.0)
        {
            if (span == null) throw new ArgumentNullException(nameof(span));
            return CalculateZones(
                span.SpanIndex,
                span.ClearLengthMm,
                span.WidthMm,
                span.HeightMm,
                stirrupDiaMm,
                denseSpacingMm,
                sparseSpacingMm,
                span.IsCantileverLeft,
                span.IsCantileverRight);
        }

        public static int ResolveLegCount(double beamWidthMm)
        {
            if (beamWidthMm <= 350.0) return 2;
            if (beamWidthMm <= 600.0) return 4;
            return 6;
        }

        public static List<double> GenerateStirrupStations(
            double clearSpanMm,
            StirrupZoneDistributionModel model,
            double firstOffsetMm = 50.0)
        {
            var stations = new List<double>();
            if (clearSpanMm <= 0.0) return stations;

            double maxStation = clearSpanMm - firstOffsetMm;

            // 1. Left Dense Zone
            double currentX = firstOffsetMm;
            double leftDenseEnd = model.LeftDenseZoneLengthMm;
            while (currentX <= leftDenseEnd && currentX <= maxStation)
            {
                stations.Add(currentX);
                currentX += model.LeftDenseSpacingMm;
            }

            // 2. Midspan Sparse Zone
            double rightDenseStart = model.RightDenseZoneLengthMm > 0.0
                ? clearSpanMm - model.RightDenseZoneLengthMm
                : maxStation + 1.0;
            while (currentX < rightDenseStart && currentX <= maxStation)
            {
                stations.Add(currentX);
                currentX += model.MidspanSparseSpacingMm;
            }

            // 3. Right Dense Zone
            if (model.RightDenseZoneLengthMm > 0.0)
            {
                while (currentX <= maxStation)
                {
                    stations.Add(currentX);
                    currentX += model.RightDenseSpacingMm;
                }
            }

            return stations;
        }
    }
}
