using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Geometry;

namespace HPDcePro.UI.ViewModels
{
    public sealed partial class DamCongViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _beamMark = "CB1";

        [ObservableProperty]
        private double _centerX = 0.0;

        [ObservableProperty]
        private double _centerY = 0.0;

        [ObservableProperty]
        private double _radiusMm = 6000.0;

        [ObservableProperty]
        private double _startAngleDeg = 0.0;

        [ObservableProperty]
        private double _endAngleDeg = 90.0;

        [ObservableProperty]
        private double _widthMm = 300.0;

        [ObservableProperty]
        private double _heightMm = 600.0;

        [ObservableProperty]
        private double _coverMm = 35.0;

        [ObservableProperty]
        private double _designMomentKnM = 150.0;

        [ObservableProperty]
        private double _designShearKn = 120.0;

        [ObservableProperty]
        private double _designTorsionKnM = 40.0;

        [ObservableProperty]
        private double _maxChordLengthMm = 1000.0;

        [ObservableProperty]
        private ConcreteGrade _selectedConcrete = ConcreteGrade.B25;

        [ObservableProperty]
        private RebarGrade _selectedRebar = RebarGrade.CB400V;

        [ObservableProperty]
        private double _arcLengthMm;

        [ObservableProperty]
        private double _sweepAngleDeg;

        [ObservableProperty]
        private double _torsionCapacityMaxKnm;

        [ObservableProperty]
        private bool _isTorsionCompliant;

        [ObservableProperty]
        private double _requiredStirrupAswPerS;

        [ObservableProperty]
        private double _requiredLongitudinalAslMm2;

        [ObservableProperty]
        private double _flexuralAsRequiredMm2;

        [ObservableProperty]
        private string _statusSummary = "Chưa tính toán";

        public DamCongViewModel()
        {
            Calculate();
        }

        [RelayCommand]
        public void Calculate()
        {
            var concrete = MaterialLibrary.GetConcrete(SelectedConcrete);
            var steel = MaterialLibrary.GetRebar(SelectedRebar);

            var input = new CurvedBeamDesignInput(BeamMark, concrete, steel)
            {
                Center = new Point2D(CenterX, CenterY),
                RadiusMm = RadiusMm > 0 ? RadiusMm : 6000.0,
                StartAngleRad = StartAngleDeg * Math.PI / 180.0,
                EndAngleRad = EndAngleDeg * Math.PI / 180.0,
                WidthMm = WidthMm > 0 ? WidthMm : 300.0,
                HeightMm = HeightMm > 0 ? HeightMm : 600.0,
                CoverMm = CoverMm > 0 ? CoverMm : 35.0,
                MomentKnm = DesignMomentKnM,
                ShearKn = DesignShearKn,
                TorsionKnm = DesignTorsionKnM,
                MaxChordLengthMm = MaxChordLengthMm > 0 ? MaxChordLengthMm : 1000.0
            };

            var res = CurvedBeamCalculator.Calculate(input);
            ArcLengthMm = res.ArcLengthMm;
            SweepAngleDeg = res.SweepAngleDeg;
            TorsionCapacityMaxKnm = res.TorsionCapacityMaxKnm;
            IsTorsionCompliant = res.TorsionCapacityCompliant;
            RequiredStirrupAswPerS = res.TotalCombinedStirrupAswPerS;
            RequiredLongitudinalAslMm2 = res.RequiredLongitudinalTorsionAslMm2;
            FlexuralAsRequiredMm2 = res.FlexuralAsRequiredMm2;
            StatusSummary = res.SummaryNotes;
        }
    }
}
