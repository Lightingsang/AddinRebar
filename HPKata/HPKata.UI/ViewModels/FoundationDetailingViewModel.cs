using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Foundations.Calculators;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Foundations.Persistence;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for foundation detailing dialog supporting pad footings, pile caps, and stepped pile caps.
    /// </summary>
    public sealed partial class FoundationDetailingViewModel : ObservableObject
    {
        [ObservableProperty] private string _foundationMark = "F1";
        [ObservableProperty] private bool _isPileCap;
        [ObservableProperty] private double _lengthMm = 1800.0;
        [ObservableProperty] private double _widthMm = 1800.0;
        [ObservableProperty] private double _totalHeightMm = 900.0;
        [ObservableProperty] private double _baseSlabHeightMm = 350.0;

        // Pedestal (pad footing)
        [ObservableProperty] private double _pedestalLengthMm = 500.0;
        [ObservableProperty] private double _pedestalWidthMm = 500.0;
        [ObservableProperty] private PedestalType _pedestalStyle = PedestalType.Sloped;

        // Piles (pile cap)
        [ObservableProperty] private int _pileCount = 4;
        [ObservableProperty] private double _pileDiameterMm = 400.0;
        [ObservableProperty] private bool _isPileSquare;
        [ObservableProperty] private bool _isStepped;
        [ObservableProperty] private double _tier2LengthMm = 1200.0;
        [ObservableProperty] private double _tier2WidthMm = 1200.0;
        [ObservableProperty] private double _tier2HeightMm = 400.0;

        // Rebar
        [ObservableProperty] private double _coverBottomMm = 50.0;
        [ObservableProperty] private double _coverSideMm = 50.0;
        [ObservableProperty] private int _bottomMeshXDia = 16;
        [ObservableProperty] private double _bottomMeshXSpacing = 150.0;
        [ObservableProperty] private int _bottomMeshYDia = 16;
        [ObservableProperty] private double _bottomMeshYSpacing = 150.0;

        [ObservableProperty] private bool _hasTopMesh;
        [ObservableProperty] private int _topMeshDia = 14;
        [ObservableProperty] private double _topMeshSpacing = 200.0;

        [ObservableProperty] private bool _hasSkinTies = true;
        [ObservableProperty] private int _skinTieDia = 12;
        [ObservableProperty] private double _skinTieSpacing = 200.0;

        [ObservableProperty] private int _starterDowelCount = 6;
        [ObservableProperty] private int _starterDowelDia = 20;
        [ObservableProperty] private double _starterDowelLap = 600.0;

        [ObservableProperty] private double _estimatedVolumeM3;
        [ObservableProperty] private double _estimatedWeightKg;

        public event Action? RequestClose;
        public FoundationRebarLayout? ResultLayout { get; private set; }

        public FoundationDetailingViewModel()
        {
            Recalculate();
        }

        [RelayCommand]
        public void Recalculate()
        {
            if (IsPileCap)
            {
                var cap = BuildPileCapModel();
                EstimatedVolumeM3 = Math.Round(FootingGeometryCalculator.CalculatePileCapVolume(cap), 2);
                var items = PileCapRebarCalculator.CalculatePileCapRebar(cap);
                double w = 0.0;
                foreach (var it in items) w += it.TotalWeightKg;
                EstimatedWeightKg = Math.Round(w, 1);
            }
            else
            {
                var footing = BuildFootingModel();
                EstimatedVolumeM3 = Math.Round(FootingGeometryCalculator.CalculateFootingVolume(footing), 2);
                var items = FoundationScheduleAggregator.AggregateFooting(footing);
                double w = 0.0;
                foreach (var it in items) w += it.TotalWeightKg;
                EstimatedWeightKg = Math.Round(w, 1);
            }
        }

        [RelayCommand]
        public void Apply()
        {
            Recalculate();
            if (IsPileCap)
            {
                var cap = BuildPileCapModel();
                ResultLayout = FoundationRebarLayout.FromPileCap(cap);
            }
            else
            {
                var footing = BuildFootingModel();
                ResultLayout = FoundationRebarLayout.FromFooting(footing);
            }
            RequestClose?.Invoke();
        }

        public IsolatedFootingModel BuildFootingModel()
        {
            return new IsolatedFootingModel
            {
                FootingMark = FoundationMark,
                LengthMm = LengthMm,
                WidthMm = WidthMm,
                TotalHeightMm = TotalHeightMm,
                BaseSlabHeightMm = BaseSlabHeightMm,
                PedestalLengthMm = PedestalLengthMm,
                PedestalWidthMm = PedestalWidthMm,
                PedestalStyle = PedestalStyle,
                CoverBottomMm = CoverBottomMm,
                CoverSideMm = CoverSideMm,
                BottomMeshXDiaMm = BottomMeshXDia,
                BottomMeshXSpacingMm = BottomMeshXSpacing,
                BottomMeshYDiaMm = BottomMeshYDia,
                BottomMeshYSpacingMm = BottomMeshYSpacing,
                StarterDowelCount = StarterDowelCount,
                StarterDowelDiaMm = StarterDowelDia,
                StarterDowelLapMm = StarterDowelLap
            };
        }

        public PileCapModel BuildPileCapModel()
        {
            var (piles, _, _) = PileCoordinateGenerator.GenerateLayout(
                PileCount, PileDiameterMm, isSquare: IsPileSquare);

            return new PileCapModel
            {
                CapMark = FoundationMark,
                LengthMm = LengthMm,
                WidthMm = WidthMm,
                TotalHeightMm = TotalHeightMm,
                IsStepped = IsStepped,
                Tier2LengthMm = Tier2LengthMm,
                Tier2WidthMm = Tier2WidthMm,
                Tier2HeightMm = Tier2HeightMm,
                Piles = piles,
                PileDiameterMm = PileDiameterMm,
                IsPileSquare = IsPileSquare,
                CoverBottomMm = CoverBottomMm,
                CoverSideMm = CoverSideMm,
                BottomMeshXDiaMm = BottomMeshXDia,
                BottomMeshXSpacingMm = BottomMeshXSpacing,
                BottomMeshYDiaMm = BottomMeshYDia,
                BottomMeshYSpacingMm = BottomMeshYSpacing,
                HasTopMesh = HasTopMesh,
                TopMeshXDiaMm = TopMeshDia,
                TopMeshXSpacingMm = TopMeshSpacing,
                TopMeshYDiaMm = TopMeshDia,
                TopMeshYSpacingMm = TopMeshSpacing,
                HasSkinTies = HasSkinTies,
                SkinTieDiaMm = SkinTieDia,
                SkinTieSpacingMm = SkinTieSpacing,
                StarterDowelCount = StarterDowelCount,
                StarterDowelDiaMm = StarterDowelDia,
                StarterDowelLapMm = StarterDowelLap
            };
        }
    }
}
