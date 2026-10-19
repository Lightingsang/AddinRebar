using System;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Secondary
{
    public sealed class LintelInput
    {
        public string Mark { get; set; } = "LT-1";
        public double ClearSpanL0Mm { get; set; } = 1200.0;
        public double BearingLengthMm { get; set; } = 250.0;
        public double WallThicknessBMm { get; set; } = 220.0;
        public double LintelDepthHMm { get; set; } = 200.0;
        public double WallHeightAboveMm { get; set; } = 1500.0;
        public double MasonryDensityKnM3 { get; set; } = 18.0;
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties Rebar { get; set; }
        public int MainBarDiameterMm { get; set; } = 12;
        public int StirrupDiameterMm { get; set; } = 6;
        public double StirrupSpacingMm { get; set; } = 150.0;

        public LintelInput(
            string mark, double clearSpanL0, double bearingLen, double wallB, double depthH,
            double wallHeightAbove, ConcreteProperties concrete, RebarProperties rebar)
        {
            Mark = mark;
            ClearSpanL0Mm = clearSpanL0;
            BearingLengthMm = bearingLen;
            WallThicknessBMm = wallB;
            LintelDepthHMm = depthH;
            WallHeightAboveMm = wallHeightAbove;
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Rebar = rebar ?? throw new ArgumentNullException(nameof(rebar));
        }
    }

    public sealed class LintelResult
    {
        public string Mark { get; set; } = string.Empty;
        public double TotalSpanLMm { get; set; }
        public bool IsBearingAdequate => BearingLengthMm >= 200.0;
        public double BearingLengthMm { get; set; }
        public double EquivalentArchingHeightMm { get; set; }
        public double TotalDesignLoadQKnM { get; set; }
        public double DesignMomentKnm { get; set; }
        public double DesignShearKn { get; set; }
        public double RequiredBottomAsMm2 { get; set; }
        public int RecommendedMainBarCount { get; set; }
        public int RecommendedMainBarDiameter { get; set; }
        public double ShearCapacityKn { get; set; }
        public bool IsShearSafe => DesignShearKn <= ShearCapacityKn;
        public bool IsSafe { get; set; }
        public string StatusSummary { get; set; } = string.Empty;
    }
}
