using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Drafting
{
    public sealed class BeamDetailingOptions
    {
        public double ElevationScale { get; set; } = 25.0; // 1:25
        public double SectionScale { get; set; } = 20.0;   // 1:20
        public string BeamLayer { get; set; } = "DCE_Beam";
        public string RebarLayer { get; set; } = "DCE_Thep";
        public string DimLayer { get; set; } = "DCE_Dim";
        public string TextLayer { get; set; } = "DCE_Text";
        public string HatchLayer { get; set; } = "DCE_Hatch";
        public string AxisLayer { get; set; } = "DCE_Truc";
        public double ClearCoverMm { get; set; } = 25.0;
        public double FloorElevationM { get; set; } = 3.60;
        public bool IncludeCrossSections { get; set; } = true;
        public Point2D Origin { get; set; } = new Point2D(0, 0);
    }
}
