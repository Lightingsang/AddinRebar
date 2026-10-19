using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Models;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Mappers;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for continuous beam detailing, section cut generation, and shop drawing configuration.
    /// </summary>
    public sealed partial class BeamDetailingViewModel : ObservableObject
    {
        public ObservableCollection<BeamSpanViewModel> Spans { get; } = new();

        [ObservableProperty] private string _beamMark = "D1";
        [ObservableProperty] private string _storyName = "Tầng 2";
        [ObservableProperty] private string _concreteGrade = "B25";
        [ObservableProperty] private string _mainSteelGrade = "CB400-V";
        [ObservableProperty] private string _stirrupSteelGrade = "CB240-T";
        [ObservableProperty] private double _concreteCoverMm = 25.0;
        [ObservableProperty] private bool _isShopMode;

        // Longitudinal Continuous Rebar
        [ObservableProperty] private int _topContinuousCount = 2;
        [ObservableProperty] private int _topContinuousDiameter = 20;
        [ObservableProperty] private int _botContinuousCount = 2;
        [ObservableProperty] private int _botContinuousDiameter = 20;

        // Side / Skin Bars
        [ObservableProperty] private bool _hasSideBars;
        [ObservableProperty] private int _sideBarCount = 2;
        [ObservableProperty] private int _sideBarDiameter = 12;

        // Stirrup Configuration
        [ObservableProperty] private int _stirrupDiameter = 8;
        [ObservableProperty] private double _denseSpacing = 100.0;
        [ObservableProperty] private double _sparseSpacing = 200.0;
        [ObservableProperty] private int _legCount = 2;

        public event Action? RequestClose;
        public BeamRebarLayout? ResultLayout { get; private set; }
        public bool IsConfirmed { get; private set; }

        public BeamDetailingViewModel() : this(null) { }

        public BeamDetailingViewModel(IKataExcelAdapter? excelAdapter)
        {
            _excelAdapter = excelAdapter ?? KataExcelAdapterProvider.Current;
            Spans.Add(new BeamSpanViewModel
            {
                SpanIndex = 0,
                SpanName = "Nhịp 1",
                ClearLengthMm = 5400.0,
                WidthMm = 300.0,
                HeightMm = 600.0
            });
        }

        [RelayCommand]
        private void AddSpan()
        {
            int nextIdx = Spans.Count;
            Spans.Add(new BeamSpanViewModel
            {
                SpanIndex = nextIdx,
                SpanName = $"Nhịp {nextIdx + 1}",
                ClearLengthMm = 5000.0,
                WidthMm = 300.0,
                HeightMm = 600.0
            });
            CheckSideBarRequirement();
        }

        [RelayCommand]
        private void RemoveSpan()
        {
            if (Spans.Count > 1)
            {
                Spans.RemoveAt(Spans.Count - 1);
                CheckSideBarRequirement();
            }
        }

        private void CheckSideBarRequirement()
        {
            foreach (var span in Spans)
            {
                if (span.HeightMm >= 700.0)
                {
                    HasSideBars = true;
                    return;
                }
            }
        }

        [RelayCommand]
        public void Apply()
        {
            if (AutoSyncOnApply)
            {
                WriteExcel();
            }

            var sheetData = BuildSheetData();
            var layout = KataBeamDataMapper.ToRebarLayout(sheetData);
            layout.BeamName = BeamMark;

            if (HasSideBars && layout.SkinBars.Count == 0)
            {
                var skinRuns = BeamSkinBarCalculator.GenerateSkinBarRuns(
                    layout.TotalLengthMm, 750.0, ConcreteCoverMm, SideBarDiameter);
                layout.SkinBars.AddRange(skinRuns);
            }

            ResultLayout = layout;
            IsConfirmed = true;
            RequestClose?.Invoke();
        }

        [RelayCommand]
        public void Cancel()
        {
            IsConfirmed = false;
            ResultLayout = null;
            RequestClose?.Invoke();
        }
    }
}
