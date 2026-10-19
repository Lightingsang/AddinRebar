namespace HPKata.Core.Columns.Etabs
{
    /// <summary>
    /// Represents design forces and required reinforcement areas for a single column story segment from ETABS.
    /// </summary>
    public sealed class EtabsColumnDesign
    {
        public string StoryName { get; set; } = string.Empty;
        public string ColumnMark { get; set; } = string.Empty;
        public double WidthMm { get; set; } = 400.0;
        public double DepthMm { get; set; } = 400.0;
        public double HeightMm { get; set; } = 3600.0;

        public double AxialForceP_kN { get; set; }
        public double MomentM2_kNm { get; set; }
        public double MomentM3_kNm { get; set; }

        public double RequiredAstMm2 { get; set; }
        public double RequiredAvPerSMm2PerM { get; set; }
    }
}
