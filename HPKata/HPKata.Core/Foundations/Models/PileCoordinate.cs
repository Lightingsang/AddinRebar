using System;

namespace HPKata.Core.Foundations.Models
{
    /// <summary>
    /// Represents the coordinate, dimensions, and embedment of an individual pile within a pile cap.
    /// Centered relative to the pile cap geometric centroid (0, 0).
    /// </summary>
    public class PileCoordinate
    {
        public int PileIndex { get; set; } = 1;
        public double X { get; set; }
        public double Y { get; set; }
        public double DiameterMm { get; set; } = 400.0;
        public bool IsSquare { get; set; }
        public double EmbedmentDepthMm { get; set; } = 100.0;
        public double DowelLengthMm { get; set; } = 500.0;

        public PileCoordinate() { }

        public PileCoordinate(int index, double x, double y, double diameterMm = 400.0, bool isSquare = false)
        {
            PileIndex = index;
            X = x;
            Y = y;
            DiameterMm = diameterMm > 0 ? diameterMm : 400.0;
            IsSquare = isSquare;
        }

        public double AreaMm2 => IsSquare
            ? DiameterMm * DiameterMm
            : Math.PI * (DiameterMm / 2.0) * (DiameterMm / 2.0);

        public override string ToString() =>
            $"Pile #{PileIndex}: ({X:F1}, {Y:F1}), D={DiameterMm:F0}{(IsSquare ? " (Square)" : " (Circular)")}";
    }
}
