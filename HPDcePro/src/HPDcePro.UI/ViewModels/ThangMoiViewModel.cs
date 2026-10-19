using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Secondary;

namespace HPDcePro.UI.ViewModels
{
    public sealed partial class ThangMoiViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _stairMark = "TH1";

        [ObservableProperty]
        private double _flightSpanLMm = 3000.0;

        [ObservableProperty]
        private double _totalRiseHMm = 1800.0;

        [ObservableProperty]
        private double _waistThicknessHMm = 120.0;

        [ObservableProperty]
        private int _numberOfSteps = 10;

        [ObservableProperty]
        private double _stairWidthBMm = 1200.0;

        [ObservableProperty]
        private double _landing1LengthMm = 1000.0;

        [ObservableProperty]
        private double _landing2LengthMm = 1000.0;

        [ObservableProperty]
        private StaircaseType _selectedStairType = StaircaseType.WaistSlab;

        [ObservableProperty]
        private ConcreteGrade _selectedConcrete = ConcreteGrade.B25;

        [ObservableProperty]
        private RebarGrade _selectedRebar = RebarGrade.CB400V;

        [ObservableProperty]
        private double _riserHeightMm;

        [ObservableProperty]
        private double _treadWidthMm;

        [ObservableProperty]
        private double _blondelValue;

        [ObservableProperty]
        private bool _isErgonomicSafe;

        [ObservableProperty]
        private double _inclineAngleDeg;

        [ObservableProperty]
        private double _maxDesignMomentKnm;

        [ObservableProperty]
        private double _requiredMainAsMm2;

        [ObservableProperty]
        private int _recommendedBarCount;

        [ObservableProperty]
        private double _crankedBarLapLengthMm;

        [ObservableProperty]
        private string _statusSummary = "Chưa tính toán";

        public ThangMoiViewModel()
        {
            Calculate();
        }

        [RelayCommand]
        public void Calculate()
        {
            var concrete = MaterialLibrary.GetConcrete(SelectedConcrete);
            var rebar = MaterialLibrary.GetRebar(SelectedRebar);

            var input = new StaircaseInput(
                StairMark,
                SelectedStairType,
                FlightSpanLMm > 0 ? FlightSpanLMm : 3000.0,
                TotalRiseHMm > 0 ? TotalRiseHMm : 1800.0,
                WaistThicknessHMm > 0 ? WaistThicknessHMm : 120.0,
                NumberOfSteps > 0 ? NumberOfSteps : 10,
                StairWidthBMm > 0 ? StairWidthBMm : 1200.0,
                concrete,
                rebar)
            {
                Landing1LengthMm = Landing1LengthMm,
                Landing2LengthMm = Landing2LengthMm
            };

            var res = StaircaseCalculator.Calculate(input);
            RiserHeightMm = res.RiserHeightMm;
            TreadWidthMm = res.TreadWidthMm;
            BlondelValue = res.BlondelFormulaValue;
            IsErgonomicSafe = res.IsErgonomicSafe;
            InclineAngleDeg = res.InclineAngleDeg;
            MaxDesignMomentKnm = res.MaxDesignMomentKnm;
            RequiredMainAsMm2 = res.RequiredMainAsMm2;
            RecommendedBarCount = res.RecommendedBarCount;
            CrankedBarLapLengthMm = res.CrankedBarLapLengthMm;
            StatusSummary = res.StatusSummary;
        }
    }
}
