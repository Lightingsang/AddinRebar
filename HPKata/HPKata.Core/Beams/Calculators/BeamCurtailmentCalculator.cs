using System;
using System.Collections.Generic;
using HPKata.Core.Beams.Models;

namespace HPKata.Core.Beams.Calculators
{
    /// <summary>
    /// Curtailment segment resulting from 11.7m commercial bar division.
    /// </summary>
    public sealed class CommercialBarSegment
    {
        public double StartXMm { get; set; }
        public double LengthMm { get; set; }
        public int DiameterMm { get; set; }
        public bool IsLapSplice { get; set; }
        public double LapLengthMm { get; set; }
    }

    /// <summary>
    /// Calculates bar cutoffs, anchorage hooks, and 11.7m commercial stock bar division with staggered splices.
    /// </summary>
    public static class BeamCurtailmentCalculator
    {
        public const double CommercialStockLengthMm = 11700.0;

        public static double CalculateTopCutoff(double clearSpanMm, double ratio = 0.25, bool isCantilever = false)
        {
            if (clearSpanMm <= 0.0) return 0.0;
            if (isCantilever) return clearSpanMm;
            return Math.Round(clearSpanMm * ratio, MidpointRounding.AwayFromZero);
        }

        public static double CalculateTopCutoff(BeamSpanModel span, double ratio = 0.25)
        {
            if (span == null) throw new ArgumentNullException(nameof(span));
            bool isCantilever = span.IsCantileverLeft || span.IsCantileverRight;
            return CalculateTopCutoff(span.ClearLengthMm, ratio, isCantilever);
        }

        public static (double StartOffsetMm, double EndOffsetMm) CalculateBottomCutoffs(
            double clearSpanMm, double startRatio = 0.10, double endRatio = 0.10)
        {
            if (clearSpanMm <= 0.0) return (0.0, 0.0);
            double start = Math.Round(clearSpanMm * startRatio, MidpointRounding.AwayFromZero);
            double end = Math.Round(clearSpanMm * (1.0 - endRatio), MidpointRounding.AwayFromZero);
            return (start, end);
        }

        public static double CalculateAnchorCog(int diameterMm, double beamHeightMm, double coverMm = 30.0)
        {
            double standardCog = 12.0 * diameterMm;
            double availableRoom = Math.Max(150.0, beamHeightMm - 2.0 * coverMm);
            return Math.Round(Math.Min(standardCog, availableRoom), MidpointRounding.AwayFromZero);
        }

        public static List<CommercialBarSegment> SegmentCommercialBars(
            double totalRunLengthMm,
            int diameterMm,
            bool isTopBar,
            double staggerOffsetMm = 0.0)
        {
            var segments = new List<CommercialBarSegment>();
            if (totalRunLengthMm <= 0.0) return segments;

            double lapLength = 30.0 * diameterMm;
            if (totalRunLengthMm <= CommercialStockLengthMm)
            {
                segments.Add(CreateSegment(0.0, totalRunLengthMm, diameterMm, 0.0, false));
                return segments;
            }

            BuildMultiSegments(segments, totalRunLengthMm, diameterMm, lapLength, staggerOffsetMm);
            return segments;
        }

        private static void BuildMultiSegments(
            List<CommercialBarSegment> segments,
            double totalLength,
            int diameter,
            double lap,
            double staggerOffset)
        {
            double currentStart = 0.0;
            bool isFirst = true;
            while (currentStart < totalLength)
            {
                double maxSegment = isFirst && staggerOffset > 0.0
                    ? Math.Max(lap * 2.0, CommercialStockLengthMm - staggerOffset)
                    : CommercialStockLengthMm;
                isFirst = false;

                double remaining = totalLength - currentStart;
                bool isLast = remaining <= maxSegment;
                double segLength = isLast ? remaining : maxSegment;
                bool isLap = currentStart > 0.0;

                segments.Add(CreateSegment(currentStart, segLength, diameter, isLap ? lap : 0.0, isLap));
                if (isLast) break;
                currentStart += (maxSegment - lap);
            }
        }

        private static CommercialBarSegment CreateSegment(
            double startX, double length, int dia, double lap, bool isLap) =>
            new()
            {
                StartXMm = startX,
                LengthMm = length,
                DiameterMm = dia,
                IsLapSplice = isLap,
                LapLengthMm = lap
            };
    }
}
