using System;
using System.Collections.Generic;

namespace HPKata.Core.Columns.Calculators
{
    public sealed class ColumnSpliceSegment
    {
        public int TierIndex { get; set; }
        public double StartZMm { get; set; }
        public double EndZMm { get; set; }
        public double LengthMm => Math.Max(0.0, EndZMm - StartZMm);
        public double LapLengthMm { get; set; }
        public bool IsLapSplice { get; set; }
    }

    /// <summary>
    /// Calculates compression/tension lap splices, 50% staggered splice offsets,
    /// and commercial 11.7m bar division for multi-story columns.
    /// </summary>
    public static class ColumnSpliceCalculator
    {
        public const double CommercialStockLengthMm = 11700.0;
        public const double MinLapLengthMm = 400.0;
        public const double MinStaggerOffsetMm = 600.0;

        public static double CalculateLapLength(int diameterMm, bool isTension = false)
        {
            if (diameterMm <= 0) return 0.0;
            double factor = isTension ? 45.0 : 35.0;
            double calculated = factor * diameterMm;
            return Math.Round(Math.Max(calculated, Math.Max(30.0 * diameterMm, MinLapLengthMm)), 1);
        }

        public static double CalculateStaggerOffset(double lapLengthMm)
        {
            if (lapLengthMm <= 0.0) return MinStaggerOffsetMm;
            return Math.Round(Math.Max(1.3 * lapLengthMm, MinStaggerOffsetMm), 1);
        }

        public static double CalculateSpliceElevation(double floorZMm, double beamDepthMm, double confinementL0Mm)
        {
            double aboveJoint = floorZMm + beamDepthMm;
            return Math.Round(aboveJoint + Math.Max(confinementL0Mm, 500.0), 1);
        }

        public static List<ColumnSpliceSegment> SegmentCommercialColumnBars(
            double baseZMm,
            double topZMm,
            int diameterMm,
            double storyHeightMm,
            bool isStaggeredTier = false)
        {
            var segments = new List<ColumnSpliceSegment>();
            double totalHeight = topZMm - baseZMm;
            if (totalHeight <= 0.0) return segments;

            double lap = CalculateLapLength(diameterMm, isTension: false);
            double stagger = isStaggeredTier ? CalculateStaggerOffset(lap) : 0.0;

            if (totalHeight <= CommercialStockLengthMm && !isStaggeredTier)
            {
                segments.Add(new ColumnSpliceSegment
                {
                    TierIndex = 0,
                    StartZMm = baseZMm,
                    EndZMm = topZMm,
                    LapLengthMm = 0.0,
                    IsLapSplice = false
                });
                return segments;
            }

            BuildSegments(segments, baseZMm, topZMm, storyHeightMm, lap, stagger);
            return segments;
        }

        private static void BuildSegments(
            List<ColumnSpliceSegment> segments,
            double baseZ,
            double topZ,
            double storyHeight,
            double lap,
            double stagger)
        {
            double currentStart = baseZ;
            int tier = 0;
            double minAdvancement = Math.Max(storyHeight > 0.0 ? storyHeight : 500.0, 500.0);

            while (currentStart < topZ)
            {
                double targetSpan = (storyHeight > 0.0) ? Math.Max(storyHeight * 2.0, lap + minAdvancement) : CommercialStockLengthMm;
                double maxAllowed = Math.Min(CommercialStockLengthMm, targetSpan);

                if (tier == 0 && stagger > 0.0)
                {
                    double staggerSpan = Math.Max(storyHeight + stagger, lap + minAdvancement);
                    maxAllowed = Math.Min(maxAllowed, staggerSpan);
                }

                double currentEnd = currentStart + maxAllowed;
                if (currentEnd >= topZ)
                {
                    currentEnd = topZ;
                }

                bool isLap = currentStart > baseZ;
                segments.Add(new ColumnSpliceSegment
                {
                    TierIndex = tier,
                    StartZMm = currentStart,
                    EndZMm = currentEnd,
                    LapLengthMm = isLap ? lap : 0.0,
                    IsLapSplice = isLap
                });

                if (currentEnd >= topZ) break;
                double nextStart = currentEnd - lap;
                if (nextStart <= currentStart)
                {
                    nextStart = currentStart + minAdvancement;
                }
                currentStart = nextStart;
                tier++;
            }
        }
    }
}
