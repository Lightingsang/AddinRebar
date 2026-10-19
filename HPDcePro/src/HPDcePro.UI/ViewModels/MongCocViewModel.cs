using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Foundation;

namespace HPDcePro.UI.ViewModels
{
    public sealed partial class MongCocViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _capMark = "ĐC-1";

        [ObservableProperty]
        private double _capWidthBMm = 1800.0;

        [ObservableProperty]
        private double _capLengthLMm = 1800.0;

        [ObservableProperty]
        private double _capThicknessHMm = 900.0;

        [ObservableProperty]
        private double _columnWidthMm = 400.0;

        [ObservableProperty]
        private double _columnLengthMm = 400.0;

        [ObservableProperty]
        private double _axialForceNKn = 1500.0;

        [ObservableProperty]
        private double _momentMxKnm = 50.0;

        [ObservableProperty]
        private double _momentMyKnm = 40.0;

        [ObservableProperty]
        private double _pileCapacityRcKn = 600.0;

        [ObservableProperty]
        private double _pileDiameterMm = 400.0;

        [ObservableProperty]
        private double _pileEmbedmentMm = 100.0;

        [ObservableProperty]
        private PileLayoutPattern _selectedPattern = PileLayoutPattern.FourPileRectangular;

        [ObservableProperty]
        private ConcreteGrade _selectedConcrete = ConcreteGrade.B25;

        [ObservableProperty]
        private RebarGrade _selectedRebar = RebarGrade.CB400V;

        [ObservableProperty]
        private double _maxPileReactionKn;

        [ObservableProperty]
        private double _minPileReactionKn;

        [ObservableProperty]
        private double _pileCapacityRatio;

        [ObservableProperty]
        private double _columnPunchingRatio;

        [ObservableProperty]
        private double _cornerPilePunchingRatio;

        [ObservableProperty]
        private double _oneWayShearRatio;

        [ObservableProperty]
        private double _requiredAsXMm2;

        [ObservableProperty]
        private double _requiredAsYMm2;

        [ObservableProperty]
        private bool _isSafe;

        [ObservableProperty]
        private string _statusSummary = "Chưa tính toán";

        public MongCocViewModel()
        {
            Calculate();
        }

        [RelayCommand]
        public void Calculate()
        {
            var concrete = MaterialLibrary.GetConcrete(SelectedConcrete);
            var steel = MaterialLibrary.GetRebar(SelectedRebar);
            var piles = PileCapCalculator.GenerateStandardLayout(SelectedPattern, PileDiameterMm);

            var input = new PileCapInput(
                CapMark,
                CapWidthBMm > 0 ? CapWidthBMm : 1800.0,
                CapLengthLMm > 0 ? CapLengthLMm : 1800.0,
                CapThicknessHMm > 0 ? CapThicknessHMm : 900.0,
                ColumnWidthMm > 0 ? ColumnWidthMm : 400.0,
                ColumnLengthMm > 0 ? ColumnLengthMm : 400.0,
                concrete,
                steel,
                piles,
                AxialForceNKn,
                MomentMxKnm,
                MomentMyKnm,
                PileCapacityRcKn > 0 ? PileCapacityRcKn : 600.0);

            var res = PileCapCalculator.Calculate(input);
            MaxPileReactionKn = res.MaxReactionKn;
            MinPileReactionKn = res.MinReactionKn;
            PileCapacityRatio = res.PileCapacityRatio;
            ColumnPunchingRatio = res.ColumnPunchingRatio;
            CornerPilePunchingRatio = res.CornerPilePunchingRatio;
            OneWayShearRatio = res.OneWayShearRatio;
            RequiredAsXMm2 = res.RequiredAsXMm2;
            RequiredAsYMm2 = res.RequiredAsYMm2;
            IsSafe = res.IsSafe;
            StatusSummary = res.StatusSummary;
        }
    }
}
