using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Calculations.Column
{
    public enum ColumnSectionStyle
    {
        REC1,
        REC2,
        Ci
    }

    public sealed class ColumnStoryDetail
    {
        public string StoryName { get; }
        public double FloorElevationMm { get; }
        public double StoryHeightMm { get; }
        public double BeamDepthAboveMm { get; }
        public double WidthBMm { get; }
        public double HeightHMm { get; }
        public int MainBarDiameterMm { get; }
        public int MainBarCount { get; }
        public int CornerBarDiameterMm { get; }
        public int CornerBarCount { get; }
        public int TieDiameterMm { get; }
        public double TieSpacingConfinementMm { get; }
        public double TieSpacingMidMm { get; }

        public ColumnStoryDetail(
            string storyName,
            double floorElevationMm,
            double storyHeightMm,
            double beamDepthAboveMm,
            double widthBMm,
            double heightHMm,
            int mainBarDiameterMm = 20,
            int mainBarCount = 8,
            int cornerBarDiameterMm = 20,
            int cornerBarCount = 4,
            int tieDiameterMm = 8,
            double tieSpacingConfinementMm = 100.0,
            double tieSpacingMidMm = 200.0)
        {
            StoryName = string.IsNullOrWhiteSpace(storyName) ? "TẦNG" : storyName;
            FloorElevationMm = floorElevationMm;
            StoryHeightMm = storyHeightMm > 0 ? storyHeightMm : 3600.0;
            BeamDepthAboveMm = beamDepthAboveMm > 0 ? beamDepthAboveMm : 400.0;
            WidthBMm = widthBMm > 0 ? widthBMm : 400.0;
            HeightHMm = heightHMm > 0 ? heightHMm : 400.0;
            MainBarDiameterMm = mainBarDiameterMm;
            MainBarCount = mainBarCount >= 4 ? mainBarCount : 4;
            CornerBarDiameterMm = cornerBarDiameterMm;
            CornerBarCount = cornerBarCount >= 4 ? cornerBarCount : 4;
            TieDiameterMm = tieDiameterMm;
            TieSpacingConfinementMm = tieSpacingConfinementMm > 0 ? tieSpacingConfinementMm : 100.0;
            TieSpacingMidMm = tieSpacingMidMm > 0 ? tieSpacingMidMm : 200.0;
        }
    }

    public sealed class ColumnDetailingOptions
    {
        public string ColumnMark { get; set; } = "C1";
        public ColumnSectionStyle Style { get; set; } = ColumnSectionStyle.REC1;
        public IReadOnlyList<ColumnStoryDetail> Stories { get; set; } = Array.Empty<ColumnStoryDetail>();
        public Point2D Origin { get; set; } = new Point2D(0, 0);
        public double ConcreteCoverMm { get; set; } = 30.0;
        public bool IncludeCrossSections { get; set; } = true;
        public string ConcreteLayer { get; set; } = "DCE_Cot_Bao";
        public string RebarLayer { get; set; } = "DCE_Cot_Thep";
        public string AxisLayer { get; set; } = "DCE_Cot_Truc";
        public string DimensionLayer { get; set; } = "DCE_Cot_Dim";
        public string TextLayer { get; set; } = "DCE_Cot_Text";
    }
}
