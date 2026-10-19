using System.Collections.Generic;

namespace HPKata.Core.Excel.Data
{
    /// <summary>
    /// Data transfer object representing the contents of the 'Cot' (Column) sheet in Kata.xlsm.
    /// </summary>
    public sealed class KataColumnSheetData
    {
        public string ColumnMark { get; set; } = "C1";
        public string ConcreteGrade { get; set; } = "B25";
        public string MainSteelGrade { get; set; } = "CB400-V";
        public string TieSteelGrade { get; set; } = "CB240-T";
        public double ConcreteCoverMm { get; set; } = 30.0;
        public bool IsShopMode { get; set; }
        public string SpliceMethod { get; set; } = "LapSplice";
        public double LapLengthFactor { get; set; } = 35.0;
        public double StaggerOffsetMm { get; set; } = 600.0;
        public double MaxCrankStepDownMm { get; set; } = 50.0;

        public int TieDiameter { get; set; } = 8;
        public double DenseSpacing { get; set; } = 100.0;
        public double SparseSpacing { get; set; } = 200.0;
        public string InnerHoopType { get; set; } = "CrossTie";

        public int MemberCount { get; set; } = 1;
        public double TransverseBeamWidthMm { get; set; } = 800.0;
        public double SlabThicknessMm { get; set; } = 200.0;
        public string DrawingScale { get; set; } = "1:30";
        public string SectionScale { get; set; } = "1:20";
        public string CoverSpec { get; set; } = "30/100";
        public string DefaultOuterTie { get; set; } = "d10a100/200";
        public double FoundationBottomElevationM { get; set; } = -5.5;
        public int DowelHookFactor { get; set; } = 15;
        public string DefaultExtraTie { get; set; } = "d8a400";

        public List<KataColumnStoryData> Stories { get; } = new();
    }

    public sealed class KataColumnStoryData
    {
        public int StoryIndex { get; set; }
        public string StoryName { get; set; } = string.Empty;
        public double StoryHeightMm { get; set; } = 3300.0;
        public double StoryHeightM => StoryHeightMm / 1000.0;
        public double WidthMm { get; set; } = 400.0;
        public double DepthMm { get; set; } = 400.0;
        public int CornerDiameter { get; set; } = 20;
        public int SideCountB { get; set; } = 1;
        public int SideDiameterB { get; set; } = 18;
        public int SideCountH { get; set; } = 1;
        public int SideDiameterH { get; set; } = 18;
        public double DenseSpacing { get; set; } = 100.0;
        public double SparseSpacing { get; set; } = 200.0;

        public string RebarDiameterSpec { get; set; } = "20";
        public double SplicePositionMm { get; set; }
        public double BeamDepthMm { get; set; } = 600.0;
        public string OuterTieSpec { get; set; } = "d10a100/200";
        public double RectTieVerRatio { get; set; }
        public string RectTieVerSpec { get; set; } = string.Empty;
        public int RectTieVerCount { get; set; }
        public double RectTieHorRatio { get; set; }
        public string RectTieHorSpec { get; set; } = string.Empty;
        public int RectTieHorCount { get; set; }
        public string CrossTieVerSpec { get; set; } = string.Empty;
        public int CrossTieVerCount { get; set; }
        public string CrossTieHorSpec { get; set; } = string.Empty;
        public int CrossTieHorCount { get; set; }
    }
}
