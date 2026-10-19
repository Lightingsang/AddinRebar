using System;
using System.Collections.Generic;

namespace HPKata.Core.Excel.Data
{
    /// <summary>
    /// Data transfer object representing the complete contents of the 'Dam' (Beam) sheet in Kata.xlsm.
    /// </summary>
    public sealed class KataBeamSheetData
    {
        public string BeamMark { get; set; } = "B01";
        public string StoryName { get; set; } = "Tầng 2";
        public string ConcreteGrade { get; set; } = "B25";
        public string MainSteelGrade { get; set; } = "CB400-V";
        public string StirrupSteelGrade { get; set; } = "CB240-T";
        public double ConcreteCoverMm { get; set; } = 25.0;
        public string CoverSpec { get; set; } = "50/25";
        public double MainBarCentroidCoverMm { get; set; } = 50.0;
        public bool IsShopMode { get; set; }

        public int MemberCount { get; set; } = 1;
        public double DefaultWidthMm { get; set; } = 300.0;
        public double DefaultHeightMm { get; set; } = 600.0;
        public double SlabThicknessMm { get; set; } = 150.0;
        public string DrawingScale { get; set; } = "1:50";
        public string SectionScale { get; set; } = "1:25";
        public int SameScaleFlag { get; set; } = 1;
        public double TopCurtailmentExtensionMm { get; set; } = 500.0;
        public double TensionAnchorageFactor { get; set; } = 40.0;
        public double CompressionAnchorageFactor { get; set; } = 30.0;
        public double TopCurtailmentRatio { get; set; } = 0.2;
        public double BotCurtailmentRatio { get; set; } = 0.25;

        public double ElevationDeltaMm { get; set; }
        public string ElevationString { get; set; } = "+3.300";
        public string AxisName { get; set; } = string.Empty;
        public double AxisOffsetMm { get; set; }

        public string TopContinuousRebar { get; set; } = "2f20";
        public string BotContinuousRebar { get; set; } = "2f20";
        public int TopContinuousCount { get; set; } = 2;
        public int TopContinuousDiameter { get; set; } = 20;
        public int BotContinuousCount { get; set; } = 2;
        public int BotContinuousDiameter { get; set; } = 20;

        public bool HasSideBars { get; set; }
        public int SideBarCount { get; set; } = 2;
        public int SideBarDiameter { get; set; } = 12;
        public int SkinBarDiameterMm { get; set; } = 12;
        public int SkinBarLayers { get; set; } = 1;

        public int StirrupDiameter { get; set; } = 8;
        public int StirrupDiameterMm { get => StirrupDiameter; set => StirrupDiameter = value; }
        public double DenseSpacing { get; set; } = 100.0;
        public double SparseSpacing { get; set; } = 200.0;
        public int LegCount { get; set; } = 2;
        public string StirrupNearSupport { get; set; } = "a100";
        public string StirrupMidSpan { get; set; } = "a200";
        public string StirrupCantilever { get; set; } = "150";
        public string TieSpacingSpec { get; set; } = "a500";

        public List<KataBeamSupportData> Supports { get; } = new();
        public List<KataBeamSpanData> Spans { get; } = new();
    }

    public sealed class KataBeamSupportData
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public double WidthMm { get; set; } = 400.0;
        public string TopCurtailmentLayer1 { get; set; } = string.Empty;
        public string TopCurtailmentLayer2 { get; set; } = string.Empty;
        public string TopCurtailmentLayer3 { get; set; } = string.Empty;
        public string TransverseBeamSize { get; set; } = string.Empty;
        public double TransverseBeamOffsetMm { get; set; }
        public string ColumnGridAxis { get; set; } = string.Empty;
        public double ColumnGridOffsetMm { get; set; }
    }

    public sealed class KataBeamSpanData
    {
        public int SpanIndex { get; set; }
        public string SpanName { get; set; } = string.Empty;
        public double ClearLengthMm { get; set; } = 5000.0;
        public double WidthMm { get; set; } = 300.0;
        public double HeightMm { get; set; } = 600.0;
        public double LeftSupportWidthMm { get; set; } = 400.0;
        public double RightSupportWidthMm { get; set; } = 400.0;
        public double SlabThicknessOverrideMm { get; set; }
        public double TopElevationDeltaMm { get; set; }
        public double BottomStepUpMm { get; set; }
        public bool IsCantileverLeft { get; set; }
        public bool IsCantileverRight { get; set; }
        public string BotCurtailmentLayer1 { get; set; } = string.Empty;
        public string BotCurtailmentLayer2 { get; set; } = string.Empty;
        public string BotCurtailmentLayer3 { get; set; } = string.Empty;
        public string TopStepDownDelta { get; set; } = string.Empty;
        public string BottomStepDownDelta { get; set; } = string.Empty;
        public string TopMainRebarOverride { get; set; } = string.Empty;
        public string BotMainRebarOverride { get; set; } = string.Empty;
        public string SkinBarSpec { get; set; } = string.Empty;
        public string StirrupOverride { get; set; } = string.Empty;
    }
}
