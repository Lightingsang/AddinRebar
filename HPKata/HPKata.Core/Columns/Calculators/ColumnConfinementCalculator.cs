using System;

namespace HPKata.Core.Columns.Calculators
{
    public sealed class ColumnConfinementResult
    {
        public double ConfinementHeightL0Mm { get; set; }
        public double DenseSpacingA1Mm { get; set; }
        public double SparseSpacingA2Mm { get; set; }
        public double JointSpacingAJointMm { get; set; }

        public int BottomDenseSets { get; set; }
        public int TopDenseSets { get; set; }
        public int MidSparseSets { get; set; }
        public int JointSets { get; set; }
        public int TotalSets => BottomDenseSets + TopDenseSets + MidSparseSets + JointSets;
    }

    /// <summary>
    /// Calculates seismic confinement zones (l0), dense/sparse tie spacing (a1, a2),
    /// and beam-column joint tie distribution per TCVN 5574:2018 and TCVN 9386:2012.
    /// </summary>
    public static class ColumnConfinementCalculator
    {
        public const double MinConfinementHeightMm = 500.0;
        public const double MaxDenseSpacingMm = 100.0;
        public const double MaxSparseSpacingMm = 200.0;
        public const double MaxJointSpacingMm = 150.0;

        public static double CalculateConfinementHeight(double widthMm, double depthMm, double clearHeightMm)
        {
            double maxDim = Math.Max(widthMm, depthMm);
            double hSixth = clearHeightMm / 6.0;
            return Math.Round(Math.Max(maxDim, Math.Max(hSixth, MinConfinementHeightMm)), 1);
        }

        public static double CalculateDenseSpacing(double minSectionDimMm, int minLongitudinalDiaMm)
        {
            double quarterDim = minSectionDimMm / 4.0;
            double sixDb = 6.0 * minLongitudinalDiaMm;
            double spacing = Math.Min(Math.Min(quarterDim, sixDb), MaxDenseSpacingMm);
            return Math.Max(50.0, Math.Round(spacing, 0));
        }

        public static double CalculateSparseSpacing(double minSectionDimMm, int minLongitudinalDiaMm)
        {
            double fifteenDb = 15.0 * minLongitudinalDiaMm;
            double spacing = Math.Min(Math.Min(fifteenDb, minSectionDimMm), MaxSparseSpacingMm);
            return Math.Max(100.0, Math.Round(spacing, 0));
        }

        public static ColumnConfinementResult CalculateConfinement(
            double widthMm, double depthMm, double clearHeightMm, double beamDepthMm, int minLongitudinalDiaMm)
        {
            double l0 = CalculateConfinementHeight(widthMm, depthMm, clearHeightMm);
            double minDim = Math.Min(widthMm, depthMm);
            double a1 = CalculateDenseSpacing(minDim, minLongitudinalDiaMm);
            double a2 = CalculateSparseSpacing(minDim, minLongitudinalDiaMm);
            double aJoint = MaxJointSpacingMm;

            double effectiveL0 = Math.Min(l0, clearHeightMm / 2.0);
            double midHeight = Math.Max(0.0, clearHeightMm - 2.0 * effectiveL0);

            int bottomSets = (int)Math.Ceiling(effectiveL0 / a1);
            int topSets = (int)Math.Ceiling(effectiveL0 / a1);
            int midSets = midHeight > 0.0 ? (int)Math.Ceiling(midHeight / a2) : 0;
            int jointSets = beamDepthMm > 0.0 ? Math.Max(2, (int)Math.Ceiling(beamDepthMm / aJoint)) : 0;

            return new ColumnConfinementResult
            {
                ConfinementHeightL0Mm = effectiveL0,
                DenseSpacingA1Mm = a1,
                SparseSpacingA2Mm = a2,
                JointSpacingAJointMm = aJoint,
                BottomDenseSets = bottomSets,
                TopDenseSets = topSets,
                MidSparseSets = midSets,
                JointSets = jointSets
            };
        }
    }
}
