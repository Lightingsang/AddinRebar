using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Columns.Calculators;
using HPKata.Core.Columns.Models;
using HPKata.Core.Excel;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for multi-story column detailing, crank/splice rules, and tie distribution.
    /// </summary>
    public sealed partial class ColumnDetailingViewModel : ObservableObject
    {
        public ObservableCollection<ColumnStoryRowViewModel> Stories { get; } = new();

        [ObservableProperty] private string _columnMark = "C1";
        [ObservableProperty] private string _concreteGrade = "B25";
        [ObservableProperty] private string _mainSteelGrade = "CB400-V";
        [ObservableProperty] private string _tieSteelGrade = "CB240-T";
        [ObservableProperty] private double _concreteCoverMm = 30.0;

        [ObservableProperty] private bool _isShopMode;
        [ObservableProperty] private string _spliceMethod = "LapSplice";
        [ObservableProperty] private double _lapLengthFactor = 35.0;
        [ObservableProperty] private double _staggerOffsetMm = 600.0;
        [ObservableProperty] private double _maxCrankStepDownMm = 50.0;

        [ObservableProperty] private int _tieDiameter = 8;
        [ObservableProperty] private double _denseSpacing = 100.0;
        [ObservableProperty] private double _sparseSpacing = 200.0;
        [ObservableProperty] private string _innerHoopType = "CrossTie";

        public event Action? RequestClose;
        public ColumnRebarLayout? ResultLayout { get; private set; }

        public ColumnDetailingViewModel() : this(null) { }

        public ColumnDetailingViewModel(IKataExcelAdapter? excelAdapter)
        {
            _excelAdapter = excelAdapter ?? KataExcelAdapterProvider.Current;
            Stories.Add(new ColumnStoryRowViewModel
            {
                StoryIndex = 0,
                StoryName = "Tầng 1",
                StoryHeightMm = 3600.0,
                BeamDepthMm = 600.0,
                WidthMm = 400.0,
                DepthMm = 400.0,
                CornerDiameter = 20,
                SideCountB = 1,
                SideDiameterB = 18,
                SideCountH = 1,
                SideDiameterH = 18,
                DenseSpacing = 100.0,
                SparseSpacing = 200.0
            });
        }

        [RelayCommand]
        private void AddStory()
        {
            int nextIdx = Stories.Count;
            var last = Stories[^1];
            Stories.Add(new ColumnStoryRowViewModel
            {
                StoryIndex = nextIdx,
                StoryName = $"Tầng {nextIdx + 1}",
                StoryHeightMm = 3300.0,
                BeamDepthMm = 500.0,
                WidthMm = last.WidthMm,
                DepthMm = last.DepthMm,
                CornerDiameter = last.CornerDiameter,
                SideCountB = last.SideCountB,
                SideDiameterB = last.SideDiameterB,
                SideCountH = last.SideCountH,
                SideDiameterH = last.SideDiameterH,
                DenseSpacing = last.DenseSpacing,
                SparseSpacing = last.SparseSpacing
            });
        }

        [RelayCommand]
        private void RemoveStory()
        {
            if (Stories.Count > 1)
            {
                Stories.RemoveAt(Stories.Count - 1);
            }
        }

        [RelayCommand]
        private void AutoSizeBars()
        {
            foreach (var story in Stories)
            {
                var opt = TcvnColumnRebarOptimizer.Optimize(
                    story.WidthMm, story.DepthMm, requiredAstMm2: 0.0, minRho: 0.8);
                story.CornerDiameter = opt.CornerDiameterMm;
                story.SideCountB = opt.SideCountB;
                story.SideDiameterB = opt.SideDiameterB;
                story.SideCountH = opt.SideCountH;
                story.SideDiameterH = opt.SideDiameterH;
            }
        }

        [RelayCommand]
        private void Apply()
        {
            if (AutoSyncOnApply)
            {
                WriteExcel();
            }

            var layout = new ColumnRebarLayout { ColumnMark = ColumnMark };
            double currentElevation = 0.0;

            for (int i = 0; i < Stories.Count; i++)
            {
                var s = Stories[i];
                var seg = CreateStorySegment(s, currentElevation);
                layout.Stories.Add(seg);
                layout.StoryTies[i] = seg.TieDistribution;
                currentElevation += seg.StoryHeightMm;
            }

            BuildLongitudinalRuns(layout);
            ResultLayout = layout;
            RequestClose?.Invoke();
        }

        private ColumnStorySegment CreateStorySegment(ColumnStoryRowViewModel s, double currentElevation)
        {
            return new ColumnStorySegment
            {
                StoryIndex = s.StoryIndex,
                StoryName = s.StoryName,
                BottomElevationMm = currentElevation,
                StoryHeightMm = s.StoryHeightMm,
                BeamDepthMm = s.BeamDepthMm,
                WidthMm = s.WidthMm,
                DepthMm = s.DepthMm,
                ConcreteGrade = ConcreteGrade,
                ConcreteCoverMm = ConcreteCoverMm,
                Section = new ColumnSectionModel
                {
                    WidthMm = s.WidthMm,
                    DepthMm = s.DepthMm,
                    CornerDiameterMm = s.CornerDiameter,
                    SideCountB = s.SideCountB,
                    SideDiameterB = s.SideDiameterB,
                    SideCountH = s.SideCountH,
                    SideDiameterH = s.SideDiameterH
                },
                TieDistribution = new ColumnTieDistributionModel
                {
                    TieDiameterMm = TieDiameter,
                    DenseSpacingMm = s.DenseSpacing,
                    SparseSpacingMm = s.SparseSpacing,
                    ConfinementHeightMm = ColumnConfinementCalculator.CalculateConfinementHeight(
                        s.WidthMm, s.DepthMm, s.ClearHeightMm)
                }
            };
        }

        private void BuildLongitudinalRuns(ColumnRebarLayout layout)
        {
            if (layout.Stories.Count == 0) return;
            int cornerDia = layout.Stories[0].Section.CornerDiameterMm;
            double totalH = layout.TotalHeightMm;

            var segments = ColumnSpliceCalculator.SegmentCommercialColumnBars(
                0.0, totalH, cornerDia, layout.Stories[0].StoryHeightMm, IsShopMode);

            int barCounter = 1;
            foreach (var seg in segments)
            {
                layout.LongitudinalBars.Add(new ColumnBarRunModel
                {
                    BarMark = $"C{ColumnMark}-{barCounter++}",
                    DiameterMm = cornerDia,
                    LengthMm = seg.LengthMm,
                    StartZMm = seg.StartZMm,
                    EndZMm = seg.EndZMm,
                    SpliceType = seg.IsLapSplice ? ColumnSpliceType.LapSplice : ColumnSpliceType.ContinuousStraight,
                    LapLengthMm = seg.LapLengthMm,
                    HasBottomHook = (seg.StartZMm <= 0.0),
                    BottomHookLengthMm = 250.0
                });
            }
        }
    }
}
