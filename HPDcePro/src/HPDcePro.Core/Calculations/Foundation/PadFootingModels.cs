using System;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Foundation
{
    public sealed class PadFootingInput
    {
        public string Mark { get; set; } = "MD-1";
        public double WidthBMm { get; set; } = 2000.0;
        public double LengthLMm { get; set; } = 2400.0;
        public double ThicknessHMm { get; set; } = 600.0;
        public double ColumnWidthBMm { get; set; } = 400.0;
        public double ColumnHeightHMm { get; set; } = 400.0;
        public double FoundationDepthM { get; set; } = 1.5;
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties Rebar { get; set; }
        public double ConcreteCoverMm { get; set; } = 50.0;
        public double DesignAxialNKn { get; set; } = 800.0;
        public double DesignMomentMxKnm { get; set; } = 60.0;
        public double DesignMomentMyKnm { get; set; } = 40.0;
        public double AllowableSoilPressureRtcKpa { get; set; } = 250.0;
        public double AverageSoilDensityKnM3 { get; set; } = 20.0;

        public PadFootingInput(
            string mark, double widthB, double lengthL, double thicknessH,
            double colB, double colH, double depthM, ConcreteProperties concrete, RebarProperties rebar,
            double nKn, double mxKnm, double myKnm, double rtcKpa)
        {
            Mark = mark;
            WidthBMm = widthB;
            LengthLMm = lengthL;
            ThicknessHMm = thicknessH;
            ColumnWidthBMm = colB;
            ColumnHeightHMm = colH;
            FoundationDepthM = depthM;
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Rebar = rebar ?? throw new ArgumentNullException(nameof(rebar));
            DesignAxialNKn = nKn;
            DesignMomentMxKnm = mxKnm;
            DesignMomentMyKnm = myKnm;
            AllowableSoilPressureRtcKpa = rtcKpa;
        }
    }

    public sealed class PadFootingResult
    {
        public string Mark { get; set; } = string.Empty;
        public double TotalVerticalLoadKn { get; set; }
        public double BasePressureMaxKpa { get; set; }
        public double BasePressureMinKpa { get; set; }
        public double BasePressureAvgKpa { get; set; }
        public double EccentricityXMm { get; set; }
        public double EccentricityYMm { get; set; }
        public bool HasUplift { get; set; }
        public double SoilCapacityRatio { get; set; }
        public double PunchingForceKn { get; set; }
        public double PunchingCapacityKn { get; set; }
        public double PunchingRatio { get; set; }
        public bool IsPunchingSafe => PunchingRatio <= 1.0;
        public double MomentAtColumnFaceXkNm { get; set; }
        public double RequiredAsXMm2 { get; set; }
        public double MomentAtColumnFaceYkNm { get; set; }
        public double RequiredAsYMm2 { get; set; }
        public bool IsOverallSafe { get; set; }
        public string StatusSummary { get; set; } = string.Empty;
    }
}
