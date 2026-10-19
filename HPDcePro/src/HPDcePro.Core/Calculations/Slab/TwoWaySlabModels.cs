using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Slab
{
    public enum SlabBoundaryCondition
    {
        FourEdgesSimplySupported,
        FourEdgesFixed,
        OneEdgeFixedThreeSupported,
        TwoAdjacentEdgesFixed,
        TwoOppositeEdgesFixed,
        ThreeEdgesFixedOneSupported
    }

    public sealed class SlabRebarLayer
    {
        public string LayerName { get; set; } = string.Empty;
        public int BarDiameterMm { get; set; } = 10;
        public double SpacingMm { get; set; } = 150.0;
        public double ProvidedAsMm2PerM => (Math.PI * Math.Pow(BarDiameterMm * 0.5, 2)) * (1000.0 / SpacingMm);
        public double RequiredAsMm2PerM { get; set; }
        public string CutoffDescription { get; set; } = string.Empty;

        public SlabRebarLayer() { }

        public SlabRebarLayer(string name, int dia, double spacing, double reqAs, string cutoff)
        {
            LayerName = name;
            BarDiameterMm = dia;
            SpacingMm = spacing;
            RequiredAsMm2PerM = reqAs;
            CutoffDescription = cutoff;
        }
    }

    public sealed class TwoWaySlabInput
    {
        public string PanelName { get; set; } = "S1";
        public double ShortSpanLxMm { get; set; } = 4000.0;
        public double LongSpanLyMm { get; set; } = 5000.0;
        public double SlabThicknessHMm { get; set; } = 120.0;
        public double DeadLoadGKnM2 { get; set; } = 3.5;
        public double LiveLoadPKnM2 { get; set; } = 2.4;
        public SlabBoundaryCondition Boundary { get; set; } = SlabBoundaryCondition.FourEdgesFixed;
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties Rebar { get; set; }
        public double ConcreteCoverMm { get; set; } = 15.0;
        public int BottomBarDiameterMm { get; set; } = 10;
        public int TopBarDiameterMm { get; set; } = 10;

        public TwoWaySlabInput(
            string name, double lxMm, double lyMm, double thicknessH,
            double deadG, double liveP, SlabBoundaryCondition boundary,
            ConcreteProperties concrete, RebarProperties rebar)
        {
            PanelName = name;
            ShortSpanLxMm = lxMm;
            LongSpanLyMm = lyMm;
            SlabThicknessHMm = thicknessH;
            DeadLoadGKnM2 = deadG;
            LiveLoadPKnM2 = liveP;
            Boundary = boundary;
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Rebar = rebar ?? throw new ArgumentNullException(nameof(rebar));
        }
    }

    public sealed class TwoWaySlabResult
    {
        public string PanelName { get; set; } = string.Empty;
        public double AspectRatioLambda { get; set; }
        public bool IsTwoWay { get; set; }
        public double TotalDesignLoadQKnM2 { get; set; }
        public double SpanPositiveMomentMxKnm { get; set; }
        public double SpanPositiveMomentMyKnm { get; set; }
        public double SupportNegativeMomentMxKnm { get; set; }
        public double SupportNegativeMomentMyKnm { get; set; }
        public double RequiredAsBotXMm2 { get; set; }
        public double RequiredAsBotYMm2 { get; set; }
        public double RequiredAsTopXMm2 { get; set; }
        public double RequiredAsTopYMm2 { get; set; }
        public IReadOnlyList<SlabRebarLayer> Layers { get; set; } = Array.Empty<SlabRebarLayer>();
        public double SpacerChairCountPerM2 => 1.0;
        public double SpacerChairHeightMm { get; set; }
        public double TopBarCutoffLengthXMm { get; set; }
        public double TopBarCutoffLengthYMm { get; set; }
        public double EstimatedDeflectionMm { get; set; }
        public bool IsDeflectionSafe { get; set; }
        public bool IsSafe { get; set; }
        public string StatusSummary { get; set; } = string.Empty;
    }
}
