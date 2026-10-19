using System;
using System.Collections.Generic;

namespace HPDcePro.Core.Calculations.Beam
{
    public sealed class BeamSpanInput
    {
        public int SpanIndex { get; set; }
        public double ClearSpanMm { get; set; }
        public double SectionWidthMm { get; set; }
        public double SectionHeightMm { get; set; }
        public double LeftSupportWidthMm { get; set; } = 300.0;
        public double RightSupportWidthMm { get; set; } = 300.0;
        public double TopMomentLeftKnm { get; set; }
        public double TopMomentRightKnm { get; set; }
        public double BottomMomentMidKnm { get; set; }
        public double ShearLeftKn { get; set; }
        public double ShearRightKn { get; set; }
        public double SlabThicknessMm { get; set; } = 120.0;
    }

    public sealed class ContinuousBeamInput
    {
        public string BeamMark { get; set; } = "D1";
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties Steel { get; set; }
        public IReadOnlyList<BeamSpanInput> Spans { get; set; }
        public double MomentRedistributionRatio { get; set; } = 0.15;
        public double CommercialBarLengthMm { get; set; } = 11700.0;
        public double DefaultCoverMm { get; set; } = 30.0;

        public ContinuousBeamInput(
            string beamMark,
            ConcreteProperties concrete,
            RebarProperties steel,
            IReadOnlyList<BeamSpanInput> spans)
        {
            BeamMark = beamMark ?? throw new ArgumentNullException(nameof(beamMark));
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Steel = steel ?? throw new ArgumentNullException(nameof(steel));
            Spans = spans ?? throw new ArgumentNullException(nameof(spans));
        }
    }

    public enum RebarPlacementType
    {
        MainTop = 1,
        MainBottom = 2,
        ExtraTopSupport = 3,
        ExtraBottomSpan = 4,
        SideSkin = 5,
        Stirrup = 6
    }

    public sealed class RebarItem
    {
        public string BarMark { get; set; } = "1";
        public double DiameterMm { get; set; } = 20.0;
        public int BarCount { get; set; } = 2;
        public double StartX { get; set; }
        public double EndX { get; set; }
        public double ElevationY { get; set; }
        public double LengthMm { get; set; }
        public double SegmentA { get; set; }
        public double SegmentB { get; set; }
        public double SegmentC { get; set; }
        public double SegmentD { get; set; }
        public short ShapeCode { get; set; } = 1;
        public RebarPlacementType PlacementType { get; set; } = RebarPlacementType.MainTop;
        public string LoaiThep { get; set; } = "CTop";
    }

    public sealed class StirrupZone
    {
        public int SpanIndex { get; set; }
        public double StartX { get; set; }
        public double EndX { get; set; }
        public double DiameterMm { get; set; } = 8.0;
        public double SpacingMm { get; set; } = 150.0;
        public int LegCount { get; set; } = 2;
    }

    public sealed class LapSplice
    {
        public string BarMark { get; set; } = string.Empty;
        public RebarPlacementType Layer { get; set; }
        public double SpliceX { get; set; }
        public double LapLengthMm { get; set; }
        public double StaggerOffsetMm { get; set; }
    }

    public sealed class BeamSpanDesign
    {
        public int SpanIndex { get; set; }
        public double SectionWidthMm { get; set; }
        public double SectionHeightMm { get; set; }
        public double ClearSpanMm { get; set; }
        public double LeftSupportWidthMm { get; set; } = 300.0;
        public double RightSupportWidthMm { get; set; } = 300.0;
        public double RedistributedTopMomentLeftKnm { get; set; }
        public double RedistributedTopMomentRightKnm { get; set; }
        public double RedistributedBottomMomentMidKnm { get; set; }
        public double SupportTopAsLeft { get; set; }
        public double SupportTopAsRight { get; set; }
        public double MidSpanBottomAs { get; set; }
        public List<RebarItem> TopBars { get; } = new List<RebarItem>();
        public List<RebarItem> BottomBars { get; } = new List<RebarItem>();
        public List<StirrupZone> StirrupZones { get; } = new List<StirrupZone>();
    }

    public sealed class ContinuousBeamResult
    {
        public string BeamMark { get; set; } = string.Empty;
        public double TotalLengthMm { get; set; }
        public IReadOnlyList<BeamSpanDesign> Spans { get; set; } = Array.Empty<BeamSpanDesign>();
        public IReadOnlyList<RebarItem> ContinuousTopBars { get; set; } = Array.Empty<RebarItem>();
        public IReadOnlyList<RebarItem> ContinuousBottomBars { get; set; } = Array.Empty<RebarItem>();
        public IReadOnlyList<RebarItem> SupportExtraTopBars { get; set; } = Array.Empty<RebarItem>();
        public IReadOnlyList<RebarItem> SpanExtraBottomBars { get; set; } = Array.Empty<RebarItem>();
        public IReadOnlyList<RebarItem> SideBars { get; set; } = Array.Empty<RebarItem>();
        public IReadOnlyList<StirrupZone> StirrupZones { get; set; } = Array.Empty<StirrupZone>();
        public IReadOnlyList<LapSplice> LapSplices { get; set; } = Array.Empty<LapSplice>();
    }
}
