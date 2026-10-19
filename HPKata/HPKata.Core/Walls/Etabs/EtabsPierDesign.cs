namespace HPKata.Core.Walls.Etabs
{
    /// <summary>
    /// Represents design forces, boundary reinforcement, and shear mesh requirements
    /// for a shear wall pier from ETABS.
    /// </summary>
    public sealed class EtabsPierDesign
    {
        public string StoryName { get; set; } = string.Empty;
        public string PierMark { get; set; } = string.Empty;

        public double ThicknessMm { get; set; } = 300.0;
        public double LengthMm { get; set; } = 3000.0;
        public double HeightMm { get; set; } = 3600.0;

        public double AxialForceP_kN { get; set; }
        public double ShearV2_kN { get; set; }
        public double MomentM3_kNm { get; set; }

        public double RequiredBoundaryAsbMm2 { get; set; }
        public double RequiredShearAvhPerSMm2PerM { get; set; }
    }
}
