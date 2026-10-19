using System;

namespace HPKata.Core.Beams.Models
{
    /// <summary>
    /// Represents cross-sectional geometry, cover, and continuous rebar dimensions of a concrete beam.
    /// </summary>
    public sealed class BeamSectionModel
    {
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double ConcreteCoverMainMm { get; set; } = 30.0;
        public double ConcreteCoverStirrupMm { get; set; } = 25.0;
        public int TopMainCount { get; set; } = 2;
        public int TopMainDiameterMm { get; set; } = 20;
        public int BottomMainCount { get; set; } = 2;
        public int BottomMainDiameterMm { get; set; } = 20;
        public int SideBarCountPerFace { get; set; }
        public int SideBarDiameterMm { get; set; } = 12;

        public double EffectiveDepthH0Mm => Math.Max(0.0, HeightMm - ConcreteCoverMainMm);

        public void Validate()
        {
            if (WidthMm <= 0.0)
            {
                throw new ArgumentException("Section width must be greater than zero.", nameof(WidthMm));
            }

            if (HeightMm <= 0.0)
            {
                throw new ArgumentException("Section height must be greater than zero.", nameof(HeightMm));
            }

            if (ConcreteCoverMainMm <= 0.0 || ConcreteCoverStirrupMm <= 0.0)
            {
                throw new ArgumentException("Concrete covers must be greater than zero.");
            }
        }
    }
}
