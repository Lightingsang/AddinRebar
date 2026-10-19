using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Slabs.Calculators;
using HPKata.Core.Slabs.Models;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for two-way slab detailing, chair stool calculations, and mesh layout.
    /// </summary>
    public sealed partial class SlabDetailingViewModel : ObservableObject
    {
        [ObservableProperty] private string _panelMark = "S1";
        [ObservableProperty] private double _spanXMm = 4000.0;
        [ObservableProperty] private double _spanYMm = 5000.0;
        [ObservableProperty] private double _thicknessMm = 120.0;
        [ObservableProperty] private double _coverMm = 15.0;

        [ObservableProperty] private double _supportWidthMm = 220.0;

        [ObservableProperty] private int _bottomMeshXDia = 10;
        [ObservableProperty] private double _bottomMeshXSpacing = 150.0;
        [ObservableProperty] private int _bottomMeshYDia = 10;
        [ObservableProperty] private double _bottomMeshYSpacing = 150.0;

        [ObservableProperty] private double _topSupportRatio = 0.25;
        [ObservableProperty] private int _topSupportDia = 10;
        [ObservableProperty] private double _topSupportSpacing = 150.0;

        [ObservableProperty] private int _chairDia = 10;
        [ObservableProperty] private double _chairDensity = 1.2;
        [ObservableProperty] private double _calculatedStoolHeightMm;
        [ObservableProperty] private int _totalChairs;

        [ObservableProperty] private bool _isCantilever;
        [ObservableProperty] private double _cantileverLengthMm = 1200.0;

        [ObservableProperty] private double _estimatedVolumeM3;
        [ObservableProperty] private double _estimatedWeightKg;

        public event Action? RequestClose;
        public SlabRebarLayout? ResultLayout { get; private set; }

        public SlabDetailingViewModel()
        {
            Recalculate();
        }

        [RelayCommand]
        public void Recalculate()
        {
            var panel = BuildPanelModel();
            EstimatedVolumeM3 = Math.Round((panel.SpanXMm * panel.SpanYMm * panel.ThicknessMm) / 1e9, 2);

            double areaM2 = (panel.SpanXMm / 1000.0) * (panel.SpanYMm / 1000.0);
            var stool = SlabChairCalculator.CalculateStool(
                panel.ThicknessMm, panel.CoverMm, panel.CoverMm,
                TopSupportDia, BottomMeshXDia, ChairDia, ChairDensity, areaM2);
            CalculatedStoolHeightMm = stool.StoolHeightMm;
            TotalChairs = stool.TotalCount;

            var items = SlabRebarCalculator.CalculateSlabRebar(panel);
            items.Add(SlabChairCalculator.CreateChairRebarItem(panel.PanelMark, stool));

            double w = 0.0;
            foreach (var it in items) w += it.TotalWeightKg;
            EstimatedWeightKg = Math.Round(w, 1);
        }

        [RelayCommand]
        public void Apply()
        {
            Recalculate();
            var panel = BuildPanelModel();
            ResultLayout = SlabRebarLayout.FromPanel(panel);
            RequestClose?.Invoke();
        }

        public SlabPanelModel BuildPanelModel()
        {
            return new SlabPanelModel
            {
                PanelMark = PanelMark,
                SpanXMm = SpanXMm,
                SpanYMm = SpanYMm,
                ThicknessMm = ThicknessMm,
                CoverMm = CoverMm,
                SupportWidthLeftMm = SupportWidthMm,
                SupportWidthRightMm = SupportWidthMm,
                SupportWidthBottomMm = SupportWidthMm,
                SupportWidthTopMm = SupportWidthMm,
                BottomMeshXDiaMm = BottomMeshXDia,
                BottomMeshXSpacingMm = BottomMeshXSpacing,
                BottomMeshYDiaMm = BottomMeshYDia,
                BottomMeshYSpacingMm = BottomMeshYSpacing,
                TopSupportRatio = TopSupportRatio,
                TopSupportDiaMm = TopSupportDia,
                TopSupportSpacingMm = TopSupportSpacing,
                IsCantilever = IsCantilever,
                CantileverLengthMm = CantileverLengthMm
            };
        }
    }
}
