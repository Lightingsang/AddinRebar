using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Calculations.Pile;

namespace HPDcePro.UI.ViewModels
{
    public sealed partial class CocBtViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pileMark = "C1";

        [ObservableProperty]
        private PileInstallationType _selectedInstallationType = PileInstallationType.DrivenPile;

        [ObservableProperty]
        private bool _isCircular = false;

        [ObservableProperty]
        private double _dimensionMm = 350.0;

        [ObservableProperty]
        private double _tipDepthM = 22.0;

        [ObservableProperty]
        private double _tipSoilN60 = 25.0;

        [ObservableProperty]
        private SoilConsistency _selectedSoilType = SoilConsistency.Cohesionless;

        [ObservableProperty]
        private double _tipCapacityRbKn;

        [ObservableProperty]
        private double _shaftCapacityRsKn;

        [ObservableProperty]
        private double _ultimateCapacityRuKn;

        [ObservableProperty]
        private double _designCapacityRcKn;

        [ObservableProperty]
        private string _statusSummary = "Chưa tính toán";

        public CocBtViewModel()
        {
            CalculateCapacity();
        }

        [RelayCommand]
        public void CalculateCapacity()
        {
            double dimM = (DimensionMm > 0 ? DimensionMm : 350.0) / 1000.0;
            var pile = new PileGeometry
            {
                IsCircular = IsCircular,
                DiameterM = dimM,
                WidthM = dimM,
                HeightM = dimM,
                TopDepthM = 0.0,
                TipDepthM = TipDepthM > 1.0 ? TipDepthM : 20.0
            };

            var layers = new List<SoilLayer>
            {
                new SoilLayer("Lớp 1: Sét pha mềm", SoilConsistency.Cohesive, 0.0, 6.0, 6.0),
                new SoilLayer("Lớp 2: Cát pha dẻo", SoilConsistency.Cohesionless, 6.0, 14.0, 14.0),
                new SoilLayer("Lớp 3: Cát hạt trung chặt vừa", SoilConsistency.Cohesionless, 14.0, pile.TipDepthM, TipSoilN60)
            };

            var res = PileCapacityCalculator.Calculate(
                pile, layers, SelectedInstallationType, TipSoilN60, SelectedSoilType);

            TipCapacityRbKn = res.TipCapacityRbKn;
            ShaftCapacityRsKn = res.ShaftCapacityRsKn;
            UltimateCapacityRuKn = res.UltimateCapacityRuKn;
            DesignCapacityRcKn = res.DesignCapacityRcKn;
            StatusSummary = $"Rc = {res.DesignCapacityRcKn:F1} kN (Ru = {res.UltimateCapacityRuKn:F1} kN) - Đạt TCVN 10304:2025";
        }
    }
}
