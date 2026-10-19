using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Drafting
{
    /// <summary>
    /// Drafting configuration and dimension parameters for steel portal frames, purlin plans, and anchor bolts.
    /// </summary>
    public sealed class SteelFrameDraftingOptions
    {
        public Point2D Origin { get; set; } = new Point2D(0, 0);
        public double Scale { get; set; } = 1.0;

        // Portal Frame Dimensions (in millimeters)
        public double SpanMm { get; set; } = 24000.0;
        public double EaveHeightMm { get; set; } = 7000.0;
        public double RoofSlopePercent { get; set; } = 10.0;
        public IReadOnlyList<double>? SpansMm { get; set; }

        // Column & Rafter Profiles
        public double ColumnStartDepthMm { get; set; } = 400.0;
        public double ColumnEndDepthMm { get; set; } = 750.0;
        public double ColumnFlangeWidthMm { get; set; } = 250.0;
        public double RafterStartDepthMm { get; set; } = 750.0;
        public double RafterEndDepthMm { get; set; } = 450.0;
        public double RafterFlangeWidthMm { get; set; } = 220.0;

        // Concrete Columns (for MiddleFrameBT, MiddleFrame3BT)
        public double ConcreteColumnBMm { get; set; } = 400.0;
        public double ConcreteColumnHMm { get; set; } = 400.0;

        // Purlin Plan Parameters
        public double BuildingLengthMm { get; set; } = 60000.0;
        public double BaySpacingMm { get; set; } = 6000.0;
        public double PurlinSpacingMm { get; set; } = 1350.0;
        public string PurlinProfile { get; set; } = "Z200x65x20x2.0";

        // Monitor Roof Parameters
        public double MonitorWidthMm { get; set; } = 4000.0;
        public double MonitorHeightMm { get; set; } = 1500.0;
        public double MonitorPurlinSpacingMm { get; set; } = 1000.0;

        // Anchor Bolt Detail Parameters
        public string ColumnMark { get; set; } = "C1";
        public double PedestalLengthMm { get; set; } = 800.0;
        public double PedestalWidthMm { get; set; } = 600.0;
        public double BasePlateLengthMm { get; set; } = 600.0;
        public double BasePlateWidthMm { get; set; } = 400.0;
        public double BasePlateThicknessMm { get; set; } = 25.0;
        public int AnchorBoltCount { get; set; } = 4;
        public double AnchorBoltDiameterMm { get; set; } = 24.0;
        public double AnchorEmbedmentLengthMm { get; set; } = 600.0;
        public double AnchorHookLengthMm { get; set; } = 100.0;
        public double GroutThicknessMm { get; set; } = 30.0;

        // CAD Layers
        public string FrameLayer { get; set; } = "DCE_Thep_Khung";
        public string RafterLayer { get; set; } = "DCE_Thep_Keo";
        public string PurlinLayer { get; set; } = "DCE_Thep_XaGo";
        public string BracingLayer { get; set; } = "DCE_Thep_Giang";
        public string AnchorLayer { get; set; } = "DCE_Thep_BuLong";
        public string TextLayer { get; set; } = "DCE_Thep_Text";
        public string DimLayer { get; set; } = "DCE_Thep_Dim";
        public string AxisLayer { get; set; } = "DCE_Thep_Truc";
    }
}
