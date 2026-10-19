using System;
using System.Collections.Generic;

namespace HPDcePro.Core.Calculations.Beam
{
    public sealed class FoundationBeamDesignInput
    {
        public string BeamMark { get; set; } = "GB1";
        public double SpanLengthMm { get; set; } = 6000.0;
        public double WidthMm { get; set; } = 400.0;
        public double HeightMm { get; set; } = 800.0;
        public double CoverMm { get; set; } = 50.0;
        public double UpwardMidspanMomentKnm { get; set; } = 180.0;
        public double SupportBottomMomentKnm { get; set; } = 150.0;
        public double ShearForceKn { get; set; } = 220.0;
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties Steel { get; set; }

        public FoundationBeamDesignInput(
            string beamMark,
            ConcreteProperties concrete,
            RebarProperties steel)
        {
            BeamMark = beamMark ?? throw new ArgumentNullException(nameof(beamMark));
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Steel = steel ?? throw new ArgumentNullException(nameof(steel));
        }
    }

    public sealed class FoundationBeamResult
    {
        public string BeamMark { get; set; } = string.Empty;
        public double MidSpanTopTensionAsMm2 { get; set; }
        public double SupportBottomTensionAsMm2 { get; set; }
        public bool IsDeepBeam { get; set; }
        public double DeepBeamShearCapacityKn { get; set; }
        public bool ShearCompliant { get; set; }
        public IReadOnlyList<RebarItem> MainTopBars { get; set; } = Array.Empty<RebarItem>();
        public IReadOnlyList<RebarItem> MainBottomBars { get; set; } = Array.Empty<RebarItem>();
        public IReadOnlyList<RebarItem> SideSkinBars { get; set; } = Array.Empty<RebarItem>();
        public double ConcreteCoverMm { get; set; }
        public string SummaryNotes { get; set; } = string.Empty;
    }

    public static class FoundationBeamCalculator
    {
        public static FoundationBeamResult Calculate(FoundationBeamDesignInput input)
        {
            Validate(input);

            double h0 = input.HeightMm - input.CoverMm;
            var flexTopMid = BeamFlexureCalculator.Calculate(new BeamFlexureInput(input.WidthMm, input.HeightMm, input.UpwardMidspanMomentKnm, input.Concrete, input.Steel)
            {
                CoverTensionMm = input.CoverMm,
                CoverCompressionMm = input.CoverMm
            });

            var flexBotSup = BeamFlexureCalculator.Calculate(new BeamFlexureInput(input.WidthMm, input.HeightMm, input.SupportBottomMomentKnm, input.Concrete, input.Steel)
            {
                CoverTensionMm = input.CoverMm,
                CoverCompressionMm = input.CoverMm
            });

            bool isDeep = (input.SpanLengthMm / input.HeightMm) <= 4.0;
            double vDeepCapacity = 0.25 * input.Concrete.Rb * input.WidthMm * h0 / 1000.0;
            bool shearOk = input.ShearForceKn <= vDeepCapacity;

            var topBars = BuildTopBars(input, flexTopMid.AsRequiredMm2);
            var botBars = BuildBottomBars(input, flexBotSup.AsRequiredMm2);
            var sideBars = BuildSideBars(input);

            return new FoundationBeamResult
            {
                BeamMark = input.BeamMark,
                MidSpanTopTensionAsMm2 = flexTopMid.AsRequiredMm2,
                SupportBottomTensionAsMm2 = flexBotSup.AsRequiredMm2,
                IsDeepBeam = isDeep,
                DeepBeamShearCapacityKn = vDeepCapacity,
                ShearCompliant = shearOk,
                MainTopBars = topBars,
                MainBottomBars = botBars,
                SideSkinBars = sideBars,
                ConcreteCoverMm = input.CoverMm,
                SummaryNotes = isDeep ? "Deep foundation beam design with inverted soil pressure profile." : "Foundation tie beam with inverted flexure."
            };
        }

        private static void Validate(FoundationBeamDesignInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.CoverMm < 30.0)
            {
                throw new InvalidOperationException($"Foundation beam requires minimum 30mm concrete cover; received {input.CoverMm}mm.");
            }
            if (input.WidthMm < 200.0 || input.HeightMm < 400.0)
            {
                throw new InvalidOperationException($"Foundation beam section {input.WidthMm}x{input.HeightMm} is undersized.");
            }
            if (input.Concrete == null) throw new ArgumentNullException(nameof(input.Concrete));
            if (input.Steel == null) throw new ArgumentNullException(nameof(input.Steel));
        }

        private static List<RebarItem> BuildTopBars(FoundationBeamDesignInput input, double asReq)
        {
            double barDia = (asReq > 1000.0) ? 22.0 : 20.0;
            int count = Math.Max(2, (int)Math.Ceiling(asReq / (0.25 * Math.PI * barDia * barDia)));
            return new List<RebarItem>
            {
                new RebarItem
                {
                    BarMark = "F1",
                    DiameterMm = barDia,
                    BarCount = count,
                    StartX = input.CoverMm,
                    EndX = input.SpanLengthMm - input.CoverMm,
                    ElevationY = input.HeightMm - input.CoverMm,
                    LengthMm = input.SpanLengthMm - 2.0 * input.CoverMm + 500.0,
                    SegmentA = 250.0,
                    SegmentB = input.SpanLengthMm - 2.0 * input.CoverMm,
                    SegmentC = 250.0,
                    ShapeCode = 2,
                    PlacementType = RebarPlacementType.MainTop,
                    LoaiThep = "CTopNen"
                }
            };
        }

        private static List<RebarItem> BuildBottomBars(FoundationBeamDesignInput input, double asReq)
        {
            double barDia = (asReq > 1000.0) ? 22.0 : 20.0;
            int count = Math.Max(2, (int)Math.Ceiling(asReq / (0.25 * Math.PI * barDia * barDia)));
            return new List<RebarItem>
            {
                new RebarItem
                {
                    BarMark = "F2",
                    DiameterMm = barDia,
                    BarCount = count,
                    StartX = input.CoverMm,
                    EndX = input.SpanLengthMm - input.CoverMm,
                    ElevationY = input.CoverMm,
                    LengthMm = input.SpanLengthMm - 2.0 * input.CoverMm + 500.0,
                    SegmentA = 250.0,
                    SegmentB = input.SpanLengthMm - 2.0 * input.CoverMm,
                    SegmentC = 250.0,
                    ShapeCode = 2,
                    PlacementType = RebarPlacementType.MainBottom,
                    LoaiThep = "CBotNen"
                }
            };
        }

        private static List<RebarItem> BuildSideBars(FoundationBeamDesignInput input)
        {
            var bars = new List<RebarItem>();
            if (input.HeightMm < 700.0) return bars;

            int sidePairs = (int)Math.Floor((input.HeightMm - 2.0 * input.CoverMm) / 300.0);
            double step = (input.HeightMm - 2.0 * input.CoverMm) / (sidePairs + 1);
            for (int p = 1; p <= sidePairs; p++)
            {
                double eleY = input.CoverMm + p * step;
                bars.Add(new RebarItem
                {
                    BarMark = $"FS{p}",
                    DiameterMm = 14.0,
                    BarCount = 2,
                    StartX = input.CoverMm,
                    EndX = input.SpanLengthMm - input.CoverMm,
                    ElevationY = eleY,
                    LengthMm = input.SpanLengthMm - 2.0 * input.CoverMm,
                    SegmentB = input.SpanLengthMm - 2.0 * input.CoverMm,
                    ShapeCode = 1,
                    PlacementType = RebarPlacementType.SideSkin,
                    LoaiThep = "SuonNen"
                });
            }
            return bars;
        }
    }
}
