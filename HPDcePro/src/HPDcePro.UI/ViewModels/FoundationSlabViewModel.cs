using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Calculations.Secondary;
using HPDcePro.Core.Calculations.Slab;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.UI.ViewModels
{
    public sealed partial class FoundationSlabViewModel : ObservableObject
    {
        // Pile Cap properties
        [ObservableProperty] private double _capWidthBMm = 1800.0;
        [ObservableProperty] private double _capLengthLMm = 1800.0;
        [ObservableProperty] private double _capThicknessHMm = 900.0;
        [ObservableProperty] private double _capColumnBMm = 400.0;
        [ObservableProperty] private double _capColumnHMm = 400.0;
        [ObservableProperty] private double _capAxialNKn = 1500.0;
        [ObservableProperty] private double _capMomentMxKnm = 50.0;
        [ObservableProperty] private double _capMomentMyKnm = 40.0;
        [ObservableProperty] private double _pileCapacityRcKn = 600.0;
        [ObservableProperty] private PileLayoutPattern _selectedLayoutPattern = PileLayoutPattern.FourPileRectangular;
        [ObservableProperty] private double _maxPileReactionKn;
        [ObservableProperty] private double _minPileReactionKn;
        [ObservableProperty] private double _pileCapacityRatio;
        [ObservableProperty] private double _capPunchingRatio;
        [ObservableProperty] private double _capRequiredAsXMm2;
        [ObservableProperty] private double _capRequiredAsYMm2;
        [ObservableProperty] private string _capStatusText = "PASS";

        // Pad Footing properties
        [ObservableProperty] private double _padWidthBMm = 2000.0;
        [ObservableProperty] private double _padLengthLMm = 2400.0;
        [ObservableProperty] private double _padThicknessHMm = 600.0;
        [ObservableProperty] private double _padColumnBMm = 400.0;
        [ObservableProperty] private double _padColumnHMm = 400.0;
        [ObservableProperty] private double _padDepthM = 1.5;
        [ObservableProperty] private double _padAxialNKn = 800.0;
        [ObservableProperty] private double _padMomentMxKnm = 60.0;
        [ObservableProperty] private double _padMomentMyKnm = 40.0;
        [ObservableProperty] private double _allowableSoilRtcKpa = 250.0;
        [ObservableProperty] private double _padBasePressureMaxKpa;
        [ObservableProperty] private double _padBasePressureMinKpa;
        [ObservableProperty] private double _padPunchingRatio;
        [ObservableProperty] private double _padRequiredAsXMm2;
        [ObservableProperty] private double _padRequiredAsYMm2;
        [ObservableProperty] private bool _padHasUplift;
        [ObservableProperty] private string _padStatusText = "PASS";

        // Slab properties
        [ObservableProperty] private double _slabSpanLxMm = 4000.0;
        [ObservableProperty] private double _slabSpanLyMm = 5000.0;
        [ObservableProperty] private double _slabThicknessHMm = 120.0;
        [ObservableProperty] private double _slabDeadGKnM2 = 3.5;
        [ObservableProperty] private double _slabLivePKnM2 = 2.4;
        [ObservableProperty] private SlabBoundaryCondition _selectedBoundary = SlabBoundaryCondition.FourEdgesFixed;
        [ObservableProperty] private double _slabAspectRatio;
        [ObservableProperty] private double _slabSpanMomentMxKnm;
        [ObservableProperty] private double _slabSpanMomentMyKnm;
        [ObservableProperty] private double _slabSupportMomentMxKnm;
        [ObservableProperty] private double _slabSupportMomentMyKnm;
        [ObservableProperty] private double _slabRequiredAsBotX;
        [ObservableProperty] private double _slabRequiredAsBotY;
        [ObservableProperty] private double _slabRequiredAsTopX;
        [ObservableProperty] private double _slabRequiredAsTopY;
        [ObservableProperty] private string _slabStatusText = "PASS";

        // Stair & Lintel properties
        [ObservableProperty] private double _stairSpanLMm = 3000.0;
        [ObservableProperty] private double _stairRiseHMm = 1800.0;
        [ObservableProperty] private double _stairWaistHMm = 120.0;
        [ObservableProperty] private int _stairStepsCount = 10;
        [ObservableProperty] private double _stairRiserHeightMm;
        [ObservableProperty] private double _stairTreadWidthMm;
        [ObservableProperty] private double _stairBlondelValue;
        [ObservableProperty] private double _stairMaxMomentKnm;
        [ObservableProperty] private double _stairRequiredAsMm2;
        [ObservableProperty] private string _stairStatusText = "PASS";

        [ObservableProperty] private double _lintelSpanL0Mm = 1200.0;
        [ObservableProperty] private double _lintelBearingMm = 250.0;
        [ObservableProperty] private double _lintelDepthHMm = 200.0;
        [ObservableProperty] private double _lintelWallBMm = 220.0;
        [ObservableProperty] private double _lintelMomentKnm;
        [ObservableProperty] private double _lintelShearKn;
        [ObservableProperty] private double _lintelRequiredAsMm2;
        [ObservableProperty] private int _lintelMainBarDia = 12;
        [ObservableProperty] private string _lintelStatusText = "PASS";

        public PileCapResult? LastPileCapResult { get; private set; }
        public PadFootingResult? LastPadFootingResult { get; private set; }
        public TwoWaySlabResult? LastSlabResult { get; private set; }
        public StaircaseResult? LastStairResult { get; private set; }
        public LintelResult? LastLintelResult { get; private set; }
        public CadDraftingBuffer? LastGeneratedBuffer { get; private set; }

        public FoundationSlabViewModel()
        {
            CalculatePileCap();
            CalculatePadFooting();
            CalculateSlab();
            CalculateStaircase();
            CalculateLintel();
        }

        [RelayCommand]
        public void CalculatePileCap()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var piles = PileCapCalculator.GenerateStandardLayout(SelectedLayoutPattern, 400.0);
            var input = new PileCapInput(
                "DC-1", CapWidthBMm, CapLengthLMm, CapThicknessHMm,
                CapColumnBMm, CapColumnHMm, concrete, rebar, piles,
                CapAxialNKn, CapMomentMxKnm, CapMomentMyKnm, PileCapacityRcKn);

            LastPileCapResult = PileCapCalculator.Calculate(input);
            MaxPileReactionKn = LastPileCapResult.MaxReactionKn;
            MinPileReactionKn = LastPileCapResult.MinReactionKn;
            PileCapacityRatio = LastPileCapResult.PileCapacityRatio;
            CapPunchingRatio = LastPileCapResult.ColumnPunchingRatio;
            CapRequiredAsXMm2 = LastPileCapResult.RequiredAsXMm2;
            CapRequiredAsYMm2 = LastPileCapResult.RequiredAsYMm2;
            CapStatusText = LastPileCapResult.IsSafe ? "PASS" : "FAIL";
        }

        [RelayCommand]
        public void CalculatePadFooting()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var input = new PadFootingInput(
                "MD-1", PadWidthBMm, PadLengthLMm, PadThicknessHMm,
                PadColumnBMm, PadColumnHMm, PadDepthM, concrete, rebar,
                PadAxialNKn, PadMomentMxKnm, PadMomentMyKnm, AllowableSoilRtcKpa);

            LastPadFootingResult = PadFootingCalculator.Calculate(input);
            PadBasePressureMaxKpa = LastPadFootingResult.BasePressureMaxKpa;
            PadBasePressureMinKpa = LastPadFootingResult.BasePressureMinKpa;
            PadPunchingRatio = LastPadFootingResult.PunchingRatio;
            PadRequiredAsXMm2 = LastPadFootingResult.RequiredAsXMm2;
            PadRequiredAsYMm2 = LastPadFootingResult.RequiredAsYMm2;
            PadHasUplift = LastPadFootingResult.HasUplift;
            PadStatusText = LastPadFootingResult.IsOverallSafe ? "PASS" : "FAIL";
        }

        [RelayCommand]
        public void CalculateSlab()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var input = new TwoWaySlabInput(
                "S1", SlabSpanLxMm, SlabSpanLyMm, SlabThicknessHMm,
                SlabDeadGKnM2, SlabLivePKnM2, SelectedBoundary, concrete, rebar);

            LastSlabResult = TwoWaySlabCalculator.Calculate(input);
            SlabAspectRatio = LastSlabResult.AspectRatioLambda;
            SlabSpanMomentMxKnm = LastSlabResult.SpanPositiveMomentMxKnm;
            SlabSpanMomentMyKnm = LastSlabResult.SpanPositiveMomentMyKnm;
            SlabSupportMomentMxKnm = LastSlabResult.SupportNegativeMomentMxKnm;
            SlabSupportMomentMyKnm = LastSlabResult.SupportNegativeMomentMyKnm;
            SlabRequiredAsBotX = LastSlabResult.RequiredAsBotXMm2;
            SlabRequiredAsBotY = LastSlabResult.RequiredAsBotYMm2;
            SlabRequiredAsTopX = LastSlabResult.RequiredAsTopXMm2;
            SlabRequiredAsTopY = LastSlabResult.RequiredAsTopYMm2;
            SlabStatusText = LastSlabResult.IsSafe ? "PASS" : "FAIL";
        }

        [RelayCommand]
        public void CalculateStaircase()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var input = new StaircaseInput(
                "THANG-1", StaircaseType.WaistSlab, StairSpanLMm, StairRiseHMm,
                StairWaistHMm, StairStepsCount, 1200.0, concrete, rebar);

            LastStairResult = StaircaseCalculator.Calculate(input);
            StairRiserHeightMm = LastStairResult.RiserHeightMm;
            StairTreadWidthMm = LastStairResult.TreadWidthMm;
            StairBlondelValue = LastStairResult.BlondelFormulaValue;
            StairMaxMomentKnm = LastStairResult.MaxDesignMomentKnm;
            StairRequiredAsMm2 = LastStairResult.RequiredMainAsMm2;
            StairStatusText = LastStairResult.IsSafe ? "PASS" : "FAIL";
        }

        [RelayCommand]
        public void CalculateLintel()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var input = new LintelInput(
                "LT-1", LintelSpanL0Mm, LintelBearingMm, LintelWallBMm,
                LintelDepthHMm, 1500.0, concrete, rebar);

            LastLintelResult = LintelCalculator.Calculate(input);
            LintelMomentKnm = LastLintelResult.DesignMomentKnm;
            LintelShearKn = LastLintelResult.DesignShearKn;
            LintelRequiredAsMm2 = LastLintelResult.RequiredBottomAsMm2;
            LintelMainBarDia = LastLintelResult.RecommendedMainBarDiameter;
            LintelStatusText = LastLintelResult.IsSafe ? "PASS" : "FAIL";
        }

        [RelayCommand]
        public void ExportCad()
        {
            var gen = new FoundationDetailingGenerator();
            if (LastPileCapResult != null)
            {
                var piles = PileCapCalculator.GenerateStandardLayout(SelectedLayoutPattern, 400.0);
                var input = new PileCapInput("DC-1", CapWidthBMm, CapLengthLMm, CapThicknessHMm,
                    CapColumnBMm, CapColumnHMm, MaterialLibrary.GetConcrete(ConcreteGrade.B25),
                    MaterialLibrary.GetRebar(RebarGrade.CB400V), piles, CapAxialNKn, CapMomentMxKnm, CapMomentMyKnm, PileCapacityRcKn);
                LastGeneratedBuffer = gen.GeneratePileCapDrawing(LastPileCapResult, input, new Point2D(0, 0));
            }
        }
    }
}
