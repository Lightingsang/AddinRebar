using System.Collections.Generic;
using HPKata.Core.Walls.Models;

namespace HPKata.Core.Excel.Data
{
    /// <summary>
    /// Data transfer object representing the contents of the 'Vach' (Wall) sheet in Kata.xlsm.
    /// </summary>
    public sealed class KataWallSheetData
    {
        public string WallMark { get; set; } = "V1";
        public WallShapeType ShapeType { get; set; } = WallShapeType.Planar_I;
        public double ThicknessMm { get; set; } = 300.0;
        public double LengthMm { get; set; } = 3000.0;
        public double StoryHeightMm { get; set; } = 3600.0;
        public int StoryCount { get; set; } = 1;
        public string ConcreteGrade { get; set; } = "B25";
        public double ConcreteCoverMm { get; set; } = 25.0;

        public bool HasBoundaryElements { get; set; } = true;
        public double BoundaryLengthMm { get; set; } = 500.0;
        public int BoundaryBarCount { get; set; } = 8;
        public int BoundaryBarDiameter { get; set; } = 20;
        public int BoundaryTieDiameter { get; set; } = 8;
        public double BoundaryTieSpacing { get; set; } = 100.0;

        public bool IsDoubleCurtain { get; set; } = true;
        public int WebVerticalDiameter { get; set; } = 12;
        public double WebVerticalSpacing { get; set; } = 200.0;
        public int WebHorizontalDiameter { get; set; } = 12;
        public double WebHorizontalSpacing { get; set; } = 200.0;
        public int CrossTieDiameter { get; set; } = 8;
        public bool HasUBarEdgeCaps { get; set; } = true;

        public int MemberCount { get; set; } = 1;
        public double TransverseBeamWidthMm { get; set; } = 800.0;
        public double BoundaryZoneWidthMm { get; set; } = 600.0;
        public double SlabThicknessMm { get; set; } = 200.0;
        public string DrawingScale { get; set; } = "1:30";
        public string SectionScale { get; set; } = "1:20";
        public string CoverSpec { get; set; } = "30/100";
        public string DefaultOuterTie { get; set; } = "d10a100/200";
        public double FoundationBottomElevationM { get; set; } = -5.5;
        public int DowelHookFactor { get; set; } = 15;
        public string DefaultExtraTie { get; set; } = "d8a400";

        public List<KataWallStoryData> Stories { get; } = new();
        public List<KataWallOpeningData> Openings { get; } = new();
    }

    public sealed class KataWallStoryData
    {
        public int StoryIndex { get; set; }
        public string StoryName { get; set; } = string.Empty;
        public double StoryHeightM { get; set; } = 3.3;
        public double LengthLwMm { get; set; } = 3000.0;
        public double ThicknessTwMm { get; set; } = 300.0;
        public int BoundaryBarsCount { get; set; } = 6;
        public int WebBarsCount { get; set; } = 10;
        public int TransverseBarsLayer1 { get; set; } = 2;
        public int TransverseBarsLayer2 { get; set; } = 2;
        public string VerticalBarDiameterSpec { get; set; } = "20";
        public double SplicePositionMm { get; set; }
        public double CouplingBeamDepthMm { get; set; } = 600.0;
        public string OuterTieSpec { get; set; } = "d10a100/200";
        public string BoundaryTieSpec { get; set; } = "d10a100/200";
        public string CrossTieVerSpec { get; set; } = "d10a400";
        public string CrossTieVerCount { get; set; } = "2";
        public string CrossTieHorSpec { get; set; } = "d10a400";
        public int CrossTieHorCount { get; set; } = 2;
    }

    public sealed class KataWallOpeningData
    {
        public string OpeningMark { get; set; } = string.Empty;
        public double XOffsetMm { get; set; }
        public double YOffsetMm { get; set; }
        public double WidthMm { get; set; } = 900.0;
        public double HeightMm { get; set; } = 2100.0;
        public bool HasDiagonalBars { get; set; }
        public int DiagonalDiameter { get; set; } = 16;
        public int JambExtraDiameter { get; set; } = 18;
        public int HeaderExtraDiameter { get; set; } = 18;
    }
}
