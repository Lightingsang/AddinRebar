using System;

namespace HPKata.Core.Columns.Calculators
{
    public enum ColumnTransitionStrategy
    {
        ContinuousStraight,
        CrankBend,
        CutoffAndStarterDowel
    }

    /// <summary>
    /// Calculates column size transition strategies, crank (offset) bend geometry (1:6 slope),
    /// and starter dowel triggers per TCVN 5574:2018 and ACI 318.
    /// </summary>
    public static class ColumnCrankBendCalculator
    {
        public const double MaxCrankStepDownMm = 50.0;
        public const double CrankSlopeRatio = 6.0;

        public static ColumnTransitionStrategy DetermineStrategy(double stepDownMm)
        {
            if (stepDownMm < 0.0)
                throw new ArgumentOutOfRangeException(nameof(stepDownMm), "Step down cannot be negative.");
            if (stepDownMm <= 0.001)
                return ColumnTransitionStrategy.ContinuousStraight;
            if (stepDownMm <= MaxCrankStepDownMm)
                return ColumnTransitionStrategy.CrankBend;

            return ColumnTransitionStrategy.CutoffAndStarterDowel;
        }

        public static double CalculateCrankLength(double stepDownMm)
        {
            if (stepDownMm <= 0.0) return 0.0;
            return Math.Round(stepDownMm * CrankSlopeRatio, MidpointRounding.AwayFromZero);
        }

        public static double CalculateStepDown(double lowerDimMm, double upperDimMm, bool isConcentric = true)
        {
            double diff = lowerDimMm - upperDimMm;
            if (diff <= 0.0) return 0.0;
            return isConcentric ? (diff / 2.0) : diff;
        }

        public static (double LowerEndZMm, double CrankTopZMm, double UpperStartZMm) CalculateCrankElevation(
            double floorZMm, double beamDepthMm, double stepDownMm)
        {
            double crankLen = CalculateCrankLength(stepDownMm);
            double effectiveCrankLen = beamDepthMm > 0.0 ? Math.Min(crankLen, beamDepthMm) : crankLen;
            double crankTopZ = floorZMm + effectiveCrankLen;
            double upperStartZ = floorZMm + Math.Max(beamDepthMm, effectiveCrankLen);

            return (floorZMm, crankTopZ, upperStartZ);
        }
    }
}
