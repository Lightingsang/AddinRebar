using System;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Secondary
{
    public enum StaircaseType
    {
        WaistSlab,        // EngineBanThang
        FoldedPlateZigZag // EngineZigZac
    }

    public sealed class StaircaseInput
    {
        public string Mark { get; set; } = "THANG-1";
        public StaircaseType Type { get; set; } = StaircaseType.WaistSlab;
        public double FlightSpanLMm { get; set; } = 3000.0;
        public double TotalRiseHMm { get; set; } = 1800.0;
        public double WaistThicknessHMm { get; set; } = 120.0;
        public int NumberOfSteps { get; set; } = 10;
        public double StairWidthBMm { get; set; } = 1200.0;
        public double Landing1LengthMm { get; set; } = 1000.0;
        public double Landing2LengthMm { get; set; } = 1000.0;
        public double FinishesLoadKnM2 { get; set; } = 1.2;
        public double LiveLoadPKnM2 { get; set; } = 3.0;
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties Rebar { get; set; }
        public double ConcreteCoverMm { get; set; } = 20.0;
        public int MainBarDiameterMm { get; set; } = 12;

        public StaircaseInput(
            string mark, StaircaseType type, double flightL, double totalH, double waistH,
            int steps, double widthB, ConcreteProperties concrete, RebarProperties rebar)
        {
            Mark = mark;
            Type = type;
            FlightSpanLMm = flightL;
            TotalRiseHMm = totalH;
            WaistThicknessHMm = waistH;
            NumberOfSteps = steps;
            StairWidthBMm = widthB;
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Rebar = rebar ?? throw new ArgumentNullException(nameof(rebar));
        }
    }

    public sealed class StaircaseResult
    {
        public string Mark { get; set; } = string.Empty;
        public StaircaseType Type { get; set; }
        public double RiserHeightMm { get; set; }
        public double TreadWidthMm { get; set; }
        public double BlondelFormulaValue { get; set; }
        public bool IsErgonomicSafe { get; set; }
        public double InclineAngleDeg { get; set; }
        public double TotalDesignLoadQKnM { get; set; }
        public double MaxDesignMomentKnm { get; set; }
        public double RequiredMainAsMm2 { get; set; }
        public int RecommendedBarCount { get; set; }
        public int RecommendedBarDiameter { get; set; }
        public double CrankedBarLapLengthMm { get; set; }
        public string DetailingNotes { get; set; } = string.Empty;
        public bool IsSafe { get; set; }
        public string StatusSummary { get; set; } = string.Empty;
    }
}
