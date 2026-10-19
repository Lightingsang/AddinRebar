using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Walls.Calculators;
using HPKata.Core.Walls.Models;
using HPKata.Core.Excel;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for shear wall and core wall detailing, boundary cages, web mesh, and openings.
    /// </summary>
    public sealed partial class WallDetailingViewModel : ObservableObject
    {
        public ObservableCollection<WallOpeningViewModel> Openings { get; } = new();

        [ObservableProperty] private string _wallMark = "V1";
        [ObservableProperty] private WallShapeType _shapeType = WallShapeType.Planar_I;
        [ObservableProperty] private double _thicknessMm = 300.0;
        [ObservableProperty] private double _lengthMm = 3000.0;
        [ObservableProperty] private double _storyHeightMm = 3600.0;
        [ObservableProperty] private int _storyCount = 1;
        [ObservableProperty] private string _concreteGrade = "B25";
        [ObservableProperty] private double _concreteCoverMm = 25.0;

        // Boundary Element Settings
        [ObservableProperty] private bool _hasBoundaryElements = true;
        [ObservableProperty] private double _boundaryLengthMm = 500.0;
        [ObservableProperty] private int _boundaryBarCount = 8;
        [ObservableProperty] private int _boundaryBarDiameter = 20;
        [ObservableProperty] private int _boundaryTieDiameter = 8;
        [ObservableProperty] private double _boundaryTieSpacing = 100.0;

        // Web Mesh Settings
        [ObservableProperty] private bool _isDoubleCurtain = true;
        [ObservableProperty] private int _webVerticalDiameter = 12;
        [ObservableProperty] private double _webVerticalSpacing = 200.0;
        [ObservableProperty] private int _webHorizontalDiameter = 12;
        [ObservableProperty] private double _webHorizontalSpacing = 200.0;
        [ObservableProperty] private int _crossTieDiameter = 8;
        [ObservableProperty] private bool _hasUBarEdgeCaps = true;

        public event Action? RequestClose;
        public ShearWallModel? ResultModel { get; private set; }

        public WallDetailingViewModel() : this(null) { }

        public WallDetailingViewModel(IKataExcelAdapter? excelAdapter)
        {
            _excelAdapter = excelAdapter ?? KataExcelAdapterProvider.Current;
        }

        [RelayCommand]
        private void AddOpening()
        {
            int nextIdx = Openings.Count + 1;
            Openings.Add(new WallOpeningViewModel
            {
                OpeningMark = $"OP{nextIdx}",
                XOffsetMm = Math.Max(500.0, LengthMm / 3.0),
                YOffsetMm = 0.0,
                WidthMm = 900.0,
                HeightMm = 2100.0
            });
        }

        [RelayCommand]
        private void RemoveOpening()
        {
            if (Openings.Count > 0)
            {
                Openings.RemoveAt(Openings.Count - 1);
            }
        }

        [RelayCommand]
        private void AutoCalculateBoundary()
        {
            BoundaryLengthMm = WallBoundaryCalculator.CalculateBoundaryLength(LengthMm, ThicknessMm);
            BoundaryTieSpacing = WallBoundaryCalculator.CalculateBoundaryTieSpacing(ThicknessMm, BoundaryBarDiameter);
        }

        [RelayCommand]
        private void Apply()
        {
            if (AutoSyncOnApply)
            {
                WriteExcel();
            }

            var wall = new ShearWallModel
            {
                WallMark = WallMark,
                ShapeType = ShapeType,
                ThicknessMm = ThicknessMm,
                LengthMm = LengthMm,
                ConcreteGrade = ConcreteGrade,
                ConcreteCoverMm = ConcreteCoverMm
            };

            double elevation = 0.0;
            for (int i = 0; i < Math.Max(1, StoryCount); i++)
            {
                wall.Stories.Add(new WallStorySegment
                {
                    StoryIndex = i,
                    StoryName = $"Tầng {i + 1}",
                    BottomElevationMm = elevation,
                    StoryHeightMm = StoryHeightMm,
                    ThicknessMm = ThicknessMm,
                    LengthMm = LengthMm
                });
                elevation += StoryHeightMm;
            }

            ConfigureBoundaryAndWeb(wall);
            AddOpeningsToWall(wall);

            ResultModel = wall;
            RequestClose?.Invoke();
        }

        private void ConfigureBoundaryAndWeb(ShearWallModel wall)
        {
            if (HasBoundaryElements)
            {
                wall.BoundaryElements.Add(new WallBoundaryElementModel
                {
                    Mark = "BE-L",
                    Position = WallBoundaryPosition.Left,
                    LengthBbMm = BoundaryLengthMm,
                    ThicknessTbMm = ThicknessMm,
                    VerticalBarCount = BoundaryBarCount,
                    VerticalBarDiameterMm = BoundaryBarDiameter,
                    TieDiameterMm = BoundaryTieDiameter,
                    TieSpacingMm = BoundaryTieSpacing
                });
                wall.BoundaryElements.Add(new WallBoundaryElementModel
                {
                    Mark = "BE-R",
                    Position = WallBoundaryPosition.Right,
                    LengthBbMm = BoundaryLengthMm,
                    ThicknessTbMm = ThicknessMm,
                    VerticalBarCount = BoundaryBarCount,
                    VerticalBarDiameterMm = BoundaryBarDiameter,
                    TieDiameterMm = BoundaryTieDiameter,
                    TieSpacingMm = BoundaryTieSpacing
                });
            }

            wall.WebReinforcement = new WallWebReinforcementModel
            {
                IsDoubleCurtain = IsDoubleCurtain,
                VerticalBarDiameterMm = WebVerticalDiameter,
                VerticalBarSpacingMm = WebVerticalSpacing,
                HorizontalBarDiameterMm = WebHorizontalDiameter,
                HorizontalBarSpacingMm = WebHorizontalSpacing,
                CrossTieDiameterMm = CrossTieDiameter,
                HasUBarEdgeCaps = HasUBarEdgeCaps,
                UBarDiameterMm = WebHorizontalDiameter
            };
        }

        private void AddOpeningsToWall(ShearWallModel wall)
        {
            foreach (var op in Openings)
            {
                wall.Openings.Add(new WallOpeningModel
                {
                    OpeningMark = op.OpeningMark,
                    XOffsetMm = op.XOffsetMm,
                    YOffsetMm = op.YOffsetMm,
                    WidthMm = op.WidthMm,
                    HeightMm = op.HeightMm,
                    HasDiagonalBars = op.HasDiagonalBars,
                    DiagonalBarDiameterMm = op.DiagonalDiameter,
                    JambExtraBarDiameterMm = op.JambExtraDiameter,
                    HeaderExtraBarDiameterMm = op.HeaderExtraDiameter
                });
            }
        }
    }
}
