using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.UI.ViewModels
{
    public sealed partial class BeamDetailingViewModel : ObservableObject
    {
        [ObservableProperty]
        private double _widthMm = 220.0;

        [ObservableProperty]
        private double _heightMm = 400.0;

        [ObservableProperty]
        private double _lengthMm = 5000.0;

        [ObservableProperty]
        private ConcreteGrade _selectedConcrete = ConcreteGrade.B25;

        [ObservableProperty]
        private RebarGrade _selectedRebar = RebarGrade.CB400V;

        [ObservableProperty]
        private int _topContinuousCount = 2;

        [ObservableProperty]
        private int _topContinuousDiameter = 18;

        [ObservableProperty]
        private int _bottomContinuousCount = 3;

        [ObservableProperty]
        private int _bottomContinuousDiameter = 18;

        [ObservableProperty]
        private int _topExtraSupportCount = 2;

        [ObservableProperty]
        private int _topExtraSupportDiameter = 18;

        [ObservableProperty]
        private int _bottomExtraSpanCount = 1;

        [ObservableProperty]
        private int _bottomExtraSpanDiameter = 18;

        [ObservableProperty]
        private int _stirrupDiameter = 8;

        [ObservableProperty]
        private double _stirrupSpacingSupport = 100.0;

        [ObservableProperty]
        private double _stirrupSpacingSpan = 200.0;

        [ObservableProperty]
        private double _designMomentKnM = 80.0;

        [ObservableProperty]
        private double _designShearKn = 60.0;

        [ObservableProperty]
        private double _topAsMm2;

        [ObservableProperty]
        private double _bottomAsMm2;

        [ObservableProperty]
        private double _momentCapacityKnM;

        [ObservableProperty]
        private double _shearCapacityKn;

        [ObservableProperty]
        private double _reinforcementRatioPercent;

        [ObservableProperty]
        private bool _isFlexurePass;

        [ObservableProperty]
        private bool _isShearPass;

        [ObservableProperty]
        private string _overallStatus = "READY";

        public IReadOnlyList<ConcreteGrade> AvailableConcretes { get; } = new[]
        {
            ConcreteGrade.B15, ConcreteGrade.B20, ConcreteGrade.B25,
            ConcreteGrade.B30, ConcreteGrade.B35, ConcreteGrade.B40
        };

        public IReadOnlyList<RebarGrade> AvailableRebars { get; } = new[]
        {
            RebarGrade.CB240T, RebarGrade.CB300V, RebarGrade.CB400V, RebarGrade.CB500V
        };

        public IReadOnlyList<int> AvailableDiameters { get; } = new[] { 10, 12, 14, 16, 18, 20, 22, 25, 28, 32 };
        public IReadOnlyList<int> StirrupDiameters { get; } = new[] { 6, 8, 10, 12 };

        public BeamDetailingViewModel()
        {
            UpdateCalculations();
        }

        partial void OnWidthMmChanged(double value) => UpdateCalculations();
        partial void OnHeightMmChanged(double value) => UpdateCalculations();
        partial void OnSelectedConcreteChanged(ConcreteGrade value) => UpdateCalculations();
        partial void OnSelectedRebarChanged(RebarGrade value) => UpdateCalculations();
        partial void OnTopContinuousCountChanged(int value) => UpdateCalculations();
        partial void OnTopContinuousDiameterChanged(int value) => UpdateCalculations();
        partial void OnBottomContinuousCountChanged(int value) => UpdateCalculations();
        partial void OnBottomContinuousDiameterChanged(int value) => UpdateCalculations();
        partial void OnTopExtraSupportCountChanged(int value) => UpdateCalculations();
        partial void OnTopExtraSupportDiameterChanged(int value) => UpdateCalculations();
        partial void OnBottomExtraSpanCountChanged(int value) => UpdateCalculations();
        partial void OnBottomExtraSpanDiameterChanged(int value) => UpdateCalculations();
        partial void OnStirrupDiameterChanged(int value) => UpdateCalculations();
        partial void OnStirrupSpacingSupportChanged(double value) => UpdateCalculations();
        partial void OnDesignMomentKnMChanged(double value) => UpdateCalculations();
        partial void OnDesignShearKnChanged(double value) => UpdateCalculations();

        [RelayCommand]
        public void UpdateCalculations()
        {
            var concrete = MaterialLibrary.GetConcrete(SelectedConcrete);
            var rebar = MaterialLibrary.GetRebar(SelectedRebar);

            BottomAsMm2 = BarArea(BottomContinuousCount, BottomContinuousDiameter) +
                          BarArea(BottomExtraSpanCount, BottomExtraSpanDiameter);
            TopAsMm2 = BarArea(TopContinuousCount, TopContinuousDiameter) +
                       BarArea(TopExtraSupportCount, TopExtraSupportDiameter);

            double h0 = Math.Max(50.0, HeightMm - 40.0);
            ReinforcementRatioPercent = (BottomAsMm2 / (WidthMm * h0)) * 100.0;

            var flexResult = BeamFlexureCalculator.Calculate(new BeamFlexureInput(
                WidthMm, HeightMm, DesignMomentKnM, concrete, rebar));

            IsFlexurePass = flexResult.IsCompliant && BottomAsMm2 >= flexResult.AsRequiredMm2;
            MomentCapacityKnM = ComputeMomentCapacity(BottomAsMm2, WidthMm, h0, concrete, rebar);

            var shearResult = BeamShearCalculator.Calculate(new BeamShearInput(
                WidthMm, HeightMm, DesignShearKn, concrete, rebar)
            {
                StirrupDiameterMm = StirrupDiameter,
                IsNearSupport = true
            });

            ShearCapacityKn = shearResult.TotalShearCapacityVuKn;
            IsShearPass = shearResult.IsCompliant && ShearCapacityKn >= DesignShearKn;

            OverallStatus = (IsFlexurePass && IsShearPass) ? "PASS" : "FAIL";
        }

        private static double BarArea(int count, int diameterMm)
        {
            if (count <= 0 || diameterMm <= 0) return 0.0;
            return count * (Math.PI * diameterMm * diameterMm / 4.0);
        }

        private static double ComputeMomentCapacity(
            double asMm2, double b, double h0, ConcreteProperties c, RebarProperties s)
        {
            if (asMm2 <= 0 || b <= 0 || h0 <= 0) return 0.0;
            double x = (s.Rs * asMm2) / (c.Rb * b);
            double limitX = s.XiR * h0;
            double effectiveX = Math.Min(x, limitX);
            double mNmm = c.Rb * b * effectiveX * (h0 - 0.5 * effectiveX);
            return mNmm / 1e6;
        }
    }
}
