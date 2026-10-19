using System;
using HPKata.Core.Walls.Models;

namespace HPKata.Core.Walls.Calculators
{
    public sealed class OpeningTrimmerSummary
    {
        public int DiagonalBarCount { get; set; }
        public double DiagonalBarLengthMm { get; set; }
        public int DiagonalBarDiameterMm { get; set; }

        public int JambExtraBarCount { get; set; }
        public double JambExtraBarLengthMm { get; set; }
        public int JambExtraBarDiameterMm { get; set; }

        public int HeaderSillExtraBarCount { get; set; }
        public double HeaderSillExtraBarLengthMm { get; set; }
        public int HeaderSillExtraBarDiameterMm { get; set; }
    }

    /// <summary>
    /// Calculates diagonal crack-control trimmer bars, jamb vertical extra bars,
    /// and header/sill horizontal extra bars around shear wall openings.
    /// </summary>
    public static class WallOpeningTrimmerCalculator
    {
        public const double DefaultAnchorageLengthMm = 500.0;

        public static OpeningTrimmerSummary CalculateTrimmers(
            WallOpeningModel opening,
            double anchorageLengthMm = DefaultAnchorageLengthMm)
        {
            if (opening == null) throw new ArgumentNullException(nameof(opening));

            int diagCount = opening.HasDiagonalBars ? (4 * opening.DiagonalBarsPerCorner) : 0;
            double diagLen = opening.DiagonalBarLengthMm > 0.0
                ? opening.DiagonalBarLengthMm
                : Math.Round(2.0 * anchorageLengthMm, 0);

            int jambCount = 2 * opening.JambExtraBarCountPerSide;
            double jambLen = Math.Round(opening.HeightMm + 2.0 * anchorageLengthMm, 0);

            int headerCount = 2 * opening.HeaderExtraBarCountPerEdge;
            double headerLen = Math.Round(opening.WidthMm + 2.0 * anchorageLengthMm, 0);

            return new OpeningTrimmerSummary
            {
                DiagonalBarCount = diagCount,
                DiagonalBarLengthMm = diagLen,
                DiagonalBarDiameterMm = opening.DiagonalBarDiameterMm,

                JambExtraBarCount = jambCount,
                JambExtraBarLengthMm = jambLen,
                JambExtraBarDiameterMm = opening.JambExtraBarDiameterMm,

                HeaderSillExtraBarCount = headerCount,
                HeaderSillExtraBarLengthMm = headerLen,
                HeaderSillExtraBarDiameterMm = opening.HeaderExtraBarDiameterMm
            };
        }
    }
}
