using System;

namespace HPKata.Core.Beams.Models
{
    /// <summary>
    /// Geometric definition for inclined beams (ve_dam_xien) and horizontally curved beams (ve_dam_cong).
    /// </summary>
    public sealed class InclinedCurvedBeamModel
    {
        public string BeamName { get; set; } = string.Empty;
        public double WidthMm { get; set; } = 250.0;
        public double HeightMm { get; set; } = 400.0;

        // Inclined Beam parameters
        public bool IsInclined { get; set; }
        public double HorizontalRunMm { get; set; }
        public double ElevationDeltaMm { get; set; }
        public double SlopeLengthMm =>
            Math.Sqrt((HorizontalRunMm * HorizontalRunMm) + (ElevationDeltaMm * ElevationDeltaMm));
        public double SlopeAngleDeg =>
            HorizontalRunMm > 0.0 ? Math.Atan2(Math.Abs(ElevationDeltaMm), HorizontalRunMm) * 180.0 / Math.PI : 0.0;

        // Curved Beam parameters
        public bool IsCurved { get; set; }
        public double RadiusMm { get; set; }
        public double SweepAngleDeg { get; set; }
        public double ArcLengthMm =>
            (Math.PI * RadiusMm * SweepAngleDeg) / 180.0;

        public void Validate()
        {
            if (WidthMm <= 0.0 || HeightMm <= 0.0)
            {
                throw new ArgumentException("Beam dimensions must be greater than zero.");
            }

            if (IsInclined && SlopeAngleDeg > 60.0)
            {
                throw new ArgumentException("Inclined beam slope angle cannot exceed 60 degrees.");
            }

            if (IsCurved && RadiusMm < 500.0)
            {
                throw new ArgumentException("Curved beam radius is below minimum fabrication threshold.");
            }
        }
    }
}
