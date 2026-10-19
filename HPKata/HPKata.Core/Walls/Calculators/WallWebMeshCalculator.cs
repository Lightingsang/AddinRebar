using System;

namespace HPKata.Core.Walls.Calculators
{
    public sealed class WallMeshSummary
    {
        public int VerticalBarCount { get; set; }
        public double VerticalBarLengthMm { get; set; }
        public int HorizontalBarCount { get; set; }
        public double HorizontalBarLengthMm { get; set; }
        public int CrossTieCount { get; set; }
        public int UBarCount { get; set; }
        public double UBarLengthMm { get; set; }
    }

    /// <summary>
    /// Calculates double-curtain web reinforcement, cross-ties, and U-bar edge closures for shear walls.
    /// </summary>
    public static class WallWebMeshCalculator
    {
        public static int CalculateVerticalBarCount(
            double wallLengthMm,
            double boundaryLeftMm,
            double boundaryRightMm,
            double spacingMm,
            bool isDoubleCurtain = true)
        {
            if (spacingMm <= 0.0) return 0;
            double webLength = Math.Max(0.0, wallLengthMm - boundaryLeftMm - boundaryRightMm);
            if (webLength <= 0.0) return 0;

            int perCurtain = (int)Math.Floor(webLength / spacingMm) + 1;
            return isDoubleCurtain ? (perCurtain * 2) : perCurtain;
        }

        public static int CalculateHorizontalBarCount(
            double wallHeightMm,
            double spacingMm,
            bool isDoubleCurtain = true)
        {
            if (spacingMm <= 0.0 || wallHeightMm <= 0.0) return 0;
            int perCurtain = (int)Math.Floor(wallHeightMm / spacingMm) + 1;
            return isDoubleCurtain ? (perCurtain * 2) : perCurtain;
        }

        public static int CalculateCrossTieCount(
            double webLengthMm,
            double webHeightMm,
            double tiesPerSquareMeter = 4.0)
        {
            if (webLengthMm <= 0.0 || webHeightMm <= 0.0) return 0;
            double areaM2 = (webLengthMm / 1000.0) * (webHeightMm / 1000.0);
            return (int)Math.Ceiling(areaM2 * tiesPerSquareMeter);
        }

        public static (double WidthMm, double LegLengthMm, double TotalLengthMm) CalculateUBarDimensions(
            double wallThicknessMm, double coverMm, int barDiaMm)
        {
            double width = Math.Max(50.0, wallThicknessMm - 2.0 * coverMm);
            double leg = Math.Max(30.0 * barDiaMm, 300.0);
            double total = width + 2.0 * leg;
            return (Math.Round(width, 1), Math.Round(leg, 1), Math.Round(total, 1));
        }

        public static WallMeshSummary CalculateMesh(
            double wallLengthMm,
            double wallHeightMm,
            double wallThicknessMm,
            double boundaryLeftMm,
            double boundaryRightMm,
            double vSpacingMm = 200.0,
            double hSpacingMm = 200.0,
            int barDiaMm = 12,
            double coverMm = 25.0)
        {
            int vCount = CalculateVerticalBarCount(wallLengthMm, boundaryLeftMm, boundaryRightMm, vSpacingMm);
            int hCount = CalculateHorizontalBarCount(wallHeightMm, hSpacingMm);

            double webLen = Math.Max(0.0, wallLengthMm - boundaryLeftMm - boundaryRightMm);
            int ties = CalculateCrossTieCount(webLen, wallHeightMm);

            var ubar = CalculateUBarDimensions(wallThicknessMm, coverMm, barDiaMm);
            int ubarCount = hCount; // 1 U-bar at each horizontal bar termination per side

            return new WallMeshSummary
            {
                VerticalBarCount = vCount,
                VerticalBarLengthMm = wallHeightMm,
                HorizontalBarCount = hCount,
                HorizontalBarLengthMm = wallLengthMm,
                CrossTieCount = ties,
                UBarCount = ubarCount,
                UBarLengthMm = ubar.TotalLengthMm
            };
        }
    }
}
