using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for staircase flight detailing, re-entrant scissor crank bars, and lintel beam parameters.
    /// </summary>
    public sealed partial class StairLintelDetailingViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isStair = true;

        // Stair Parameters
        [ObservableProperty] private string _stairMark = "ST1";
        [ObservableProperty] private int _riserCount = 11;
        [ObservableProperty] private double _riserHeightMm = 160.0;
        [ObservableProperty] private double _treadWidthMm = 280.0;
        [ObservableProperty] private double _flightWidthMm = 1100.0;
        [ObservableProperty] private double _throatThicknessMm = 120.0;
        [ObservableProperty] private double _bottomLandingLengthMm = 1200.0;
        [ObservableProperty] private double _topLandingLengthMm = 1200.0;
        [ObservableProperty] private int _bottomTensionDia = 12;
        [ObservableProperty] private double _bottomTensionSpacing = 150.0;
        [ObservableProperty] private int _topNegativeDia = 10;
        [ObservableProperty] private double _topNegativeSpacing = 150.0;
        [ObservableProperty] private int _stepNoseDia = 6;
        [ObservableProperty] private double _scissorExtensionMm = 500.0;

        // Ergonomics
        [ObservableProperty] private bool _isErgonomicOk = true;
        [ObservableProperty] private double _blondelValue = 600.0;

        // Lintel Parameters
        [ObservableProperty] private string _lintelMark = "LT1";
        [ObservableProperty] private double _clearSpanMm = 1200.0;
        [ObservableProperty] private double _bearingLengthMm = 250.0;
        [ObservableProperty] private double _lintelWidthMm = 220.0;
        [ObservableProperty] private double _lintelHeightMm = 150.0;
        [ObservableProperty] private int _topHangerCount = 2;
        [ObservableProperty] private int _topHangerDia = 10;
        [ObservableProperty] private int _bottomTensionCount = 2;
        [ObservableProperty] private int _lintelBottomDia = 12;
        [ObservableProperty] private int _lintelStirrupDia = 6;
        [ObservableProperty] private double _stirrupEndSpacing = 100.0;
        [ObservableProperty] private double _stirrupMidSpacing = 150.0;

        [ObservableProperty] private double _estimatedVolumeM3;
        [ObservableProperty] private double _estimatedWeightKg;

        public event Action? RequestClose;
        public StairFlightModel? ResultFlight { get; private set; }
        public LintelModel? ResultLintel { get; private set; }

        public StairLintelDetailingViewModel()
        {
            Recalculate();
        }

        [RelayCommand]
        public void Recalculate()
        {
            if (IsStair)
            {
                var flight = BuildFlightModel();
                IsErgonomicOk = StairGeometryCalculator.CheckErgonomics(RiserHeightMm, TreadWidthMm, out double bv);
                BlondelValue = bv;
                ScissorExtensionMm = flight.ScissorCrankExtensionMm;
                EstimatedVolumeM3 = Math.Round(StairGeometryCalculator.CalculateConcreteVolume(flight), 2);
                var items = StairRebarCalculator.CalculateStairRebar(flight);
                double w = 0.0;
                foreach (var it in items) w += it.TotalWeightKg;
                EstimatedWeightKg = Math.Round(w, 1);
            }
            else
            {
                var lintel = BuildLintelModel();
                EstimatedVolumeM3 = Math.Round((lintel.TotalLengthMm * lintel.WidthMm * lintel.HeightMm) / 1e9, 3);
                var items = LintelRebarCalculator.CalculateLintelRebar(lintel);
                double w = 0.0;
                foreach (var it in items) w += it.TotalWeightKg;
                EstimatedWeightKg = Math.Round(w, 1);
            }
        }

        [RelayCommand]
        public void Apply()
        {
            Recalculate();
            if (IsStair)
            {
                ResultFlight = BuildFlightModel();
            }
            else
            {
                ResultLintel = BuildLintelModel();
            }
            RequestClose?.Invoke();
        }

        public StairFlightModel BuildFlightModel()
        {
            return new StairFlightModel
            {
                FlightMark = StairMark,
                RiserCount = RiserCount,
                RiserHeightMm = RiserHeightMm,
                TreadWidthMm = TreadWidthMm,
                FlightWidthMm = FlightWidthMm,
                ThroatThicknessMm = ThroatThicknessMm,
                BottomLandingLengthMm = BottomLandingLengthMm,
                TopLandingLengthMm = TopLandingLengthMm,
                BottomTensionDiaMm = BottomTensionDia,
                BottomTensionSpacingMm = BottomTensionSpacing,
                TopNegativeDiaMm = TopNegativeDia,
                TopNegativeSpacingMm = TopNegativeSpacing,
                StepNoseDiaMm = StepNoseDia
            };
        }

        public LintelModel BuildLintelModel()
        {
            return new LintelModel
            {
                LintelMark = LintelMark,
                ClearSpanMm = ClearSpanMm,
                BearingLengthMm = BearingLengthMm,
                WidthMm = LintelWidthMm,
                HeightMm = LintelHeightMm,
                TopHangerCount = TopHangerCount,
                TopHangerDiaMm = TopHangerDia,
                BottomTensionCount = BottomTensionCount,
                BottomTensionDiaMm = LintelBottomDia,
                StirrupDiaMm = LintelStirrupDia,
                StirrupSpacingEndMm = StirrupEndSpacing,
                StirrupSpacingMidMm = StirrupMidSpacing
            };
        }
    }
}
