using System;
using HPKata.Core.Walls.Models;

namespace HPKata.Core.Walls.Calculators
{
    /// <summary>
    /// Calculates shear wall boundary element dimensions (b_bz), vertical reinforcement,
    /// and confinement tie spacing per TCVN 5574:2018.
    /// </summary>
    public static class WallBoundaryCalculator
    {
        public const double MinBoundaryLengthMm = 400.0;
        public const double MinReinforcementRatioPercent = 1.0;
        public const double MaxConfinementTieSpacingMm = 100.0;

        public static double CalculateBoundaryLength(double wallLengthMm, double wallThicknessMm)
        {
            double ratioLw = 0.15 * wallLengthMm;
            double ratioTw = 1.5 * wallThicknessMm;
            double maxVal = Math.Max(ratioLw, Math.Max(ratioTw, MinBoundaryLengthMm));
            return Math.Round(maxVal, 0);
        }

        public static double CalculateBoundaryTieSpacing(double wallThicknessMm, int verticalBarDiaMm)
        {
            double halfTw = wallThicknessMm / 2.0;
            double sixDb = 6.0 * verticalBarDiaMm;
            double spacing = Math.Min(Math.Min(halfTw, sixDb), MaxConfinementTieSpacingMm);
            return Math.Max(50.0, Math.Round(spacing, 0));
        }

        public static WallBoundaryElementModel CreateBoundaryElement(
            string mark,
            WallBoundaryPosition position,
            double wallLengthMm,
            double wallThicknessMm,
            double requiredAsbMm2 = 0.0)
        {
            double bbz = CalculateBoundaryLength(wallLengthMm, wallThicknessMm);
            double minArea = (bbz * wallThicknessMm) * (MinReinforcementRatioPercent / 100.0);
            double targetArea = Math.Max(requiredAsbMm2, minArea);

            int dia = 20;
            double barArea = Math.PI * dia * dia / 4.0;
            int count = (int)Math.Ceiling(targetArea / barArea);
            if (count % 2 != 0) count++;
            if (count < 6) count = 6;

            double tieSpacing = CalculateBoundaryTieSpacing(wallThicknessMm, dia);

            return new WallBoundaryElementModel
            {
                Mark = mark,
                Position = position,
                LengthBbMm = bbz,
                ThicknessTbMm = wallThicknessMm,
                VerticalBarCount = count,
                VerticalBarDiameterMm = dia,
                TieDiameterMm = 8,
                TieSpacingMm = tieSpacing,
                CrossTieDiameterMm = 8,
                CrossTieCountPerSet = 2
            };
        }
    }
}
