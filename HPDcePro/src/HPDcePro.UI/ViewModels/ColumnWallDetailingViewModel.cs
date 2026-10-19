using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Column;
using HPDcePro.Core.Calculations.Wall;
using HPDcePro.Core.Drafting;

namespace HPDcePro.UI.ViewModels
{
    public sealed partial class ColumnWallDetailingViewModel : ObservableObject
    {
        [ObservableProperty]
        private double _widthBMm = 400.0;

        [ObservableProperty]
        private double _heightHMm = 400.0;

        [ObservableProperty]
        private double _lengthL0Mm = 3600.0;

        [ObservableProperty]
        private ConcreteGrade _selectedConcrete = ConcreteGrade.B25;

        [ObservableProperty]
        private RebarGrade _selectedRebar = RebarGrade.CB400V;

        [ObservableProperty]
        private double _designAxialNKn = 1200.0;

        [ObservableProperty]
        private double _designMomentMxKnm = 80.0;

        [ObservableProperty]
        private double _designMomentMyKnm = 60.0;

        [ObservableProperty]
        private int _cornerBarDiameter = 20;

        [ObservableProperty]
        private int _cornerBarCount = 4;

        [ObservableProperty]
        private int _sideBarDiameter = 18;

        [ObservableProperty]
        private int _sideBarCountB = 2;

        [ObservableProperty]
        private int _sideBarCountH = 2;

        [ObservableProperty]
        private double _slendernessEtaX = 1.0;

        [ObservableProperty]
        private double _slendernessEtaY = 1.0;

        [ObservableProperty]
        private double _magnifiedMxd = 80.0;

        [ObservableProperty]
        private double _magnifiedMyd = 60.0;

        [ObservableProperty]
        private double _axialCapacityN0 = 0.0;

        [ObservableProperty]
        private double _momentCapacityMx0 = 0.0;

        [ObservableProperty]
        private double _momentCapacityMy0 = 0.0;

        [ObservableProperty]
        private double _utilizationRatio = 0.0;

        [ObservableProperty]
        private bool _isSafe = true;

        [ObservableProperty]
        private string _statusText = "PASS";

        // Wall Inputs & Outputs
        [ObservableProperty]
        private double _wallLengthLwMm = 3000.0;

        [ObservableProperty]
        private double _wallThicknessTwMm = 250.0;

        [ObservableProperty]
        private double _wallHeightHwMm = 3600.0;

        [ObservableProperty]
        private double _wallDesignAxialNKn = 2500.0;

        [ObservableProperty]
        private double _wallDesignShearVuKn = 450.0;

        [ObservableProperty]
        private double _wallDesignMomentMuKnm = 900.0;

        [ObservableProperty]
        private int _webVerticalDiameter = 12;

        [ObservableProperty]
        private double _webVerticalSpacing = 200.0;

        [ObservableProperty]
        private int _webHorizontalDiameter = 10;

        [ObservableProperty]
        private double _webHorizontalSpacing = 150.0;

        [ObservableProperty]
        private int _crossTiesPerM2 = 4;

        [ObservableProperty]
        private double _boundaryLengthLc = 450.0;

        [ObservableProperty]
        private double _boundaryRebarRatio = 0.8;

        [ObservableProperty]
        private double _webShearCapacity = 800.0;

        [ObservableProperty]
        private double _shearUtilizationRatio = 0.56;

        [ObservableProperty]
        private bool _isWallSafe = true;

        [ObservableProperty]
        private string _wallStatusText = "PASS";

        public ColumnPmmResult? PmmResult { get; private set; }
        public ShearWallResult? WallResult { get; private set; }
        public CadDraftingBuffer? LastGeneratedBuffer { get; private set; }

        public ColumnWallDetailingViewModel()
        {
            CalculatePmm();
            CalculateWall();
        }

        [RelayCommand]
        public void CalculatePmm()
        {
            var concrete = MaterialLibrary.GetConcrete(SelectedConcrete);
            var rebar = MaterialLibrary.GetRebar(SelectedRebar);
            var bars = GenerateBarLayout();

            var input = new ColumnPmmInput(
                "C1", ColumnSectionShape.Rectangular, WidthBMm, HeightHMm, LengthL0Mm,
                concrete, rebar, bars, DesignAxialNKn, DesignMomentMxKnm, DesignMomentMyKnm);

            PmmResult = ColumnPmmInteractionCalculator.Calculate(input);

            SlendernessEtaX = PmmResult.SlendernessEtaX;
            SlendernessEtaY = PmmResult.SlendernessEtaY;
            MagnifiedMxd = PmmResult.MagnifiedMomentMxdKnm;
            MagnifiedMyd = PmmResult.MagnifiedMomentMydKnm;
            AxialCapacityN0 = PmmResult.AxialCapacityN0Kn;
            MomentCapacityMx0 = PmmResult.MomentCapacityMx0Knm;
            MomentCapacityMy0 = PmmResult.MomentCapacityMy0Knm;
            UtilizationRatio = PmmResult.UtilizationRatio;
            IsSafe = PmmResult.IsSafe;
            StatusText = IsSafe ? "PASS" : "FAIL";
        }

        [RelayCommand]
        public void CalculateWall()
        {
            var concrete = MaterialLibrary.GetConcrete(SelectedConcrete);
            var rebar = MaterialLibrary.GetRebar(SelectedRebar);

            var input = new ShearWallInput(
                "W1", WallLengthLwMm, WallThicknessTwMm, WallHeightHwMm, concrete, rebar,
                WallDesignAxialNKn, WallDesignShearVuKn, WallDesignMomentMuKnm,
                WebVerticalDiameter, WebVerticalSpacing, WebHorizontalDiameter, WebHorizontalSpacing,
                CrossTiesPerM2);

            WallResult = ShearWallCalculator.Calculate(input);

            BoundaryLengthLc = WallResult.BoundaryElement.LengthLcMm;
            BoundaryRebarRatio = WallResult.BoundaryElement.RebarRatioPercent;
            WebShearCapacity = WallResult.TotalShearCapacityVuMaxKn;
            ShearUtilizationRatio = WallResult.ShearUtilizationRatio;
            IsWallSafe = WallResult.IsOverallSafe;
            WallStatusText = IsWallSafe ? "PASS" : "FAIL";
        }

        [RelayCommand]
        public void ExportCad()
        {
            var gen = new ColumnDetailingGenerator();
            var stories = new List<ColumnStoryDetail>
            {
                new ColumnStoryDetail("TẦNG 1", 0.0, LengthL0Mm, 400.0, WidthBMm, HeightHMm, CornerBarDiameter, 8),
                new ColumnStoryDetail("TẦNG 2", LengthL0Mm, LengthL0Mm, 400.0, WidthBMm, HeightHMm, CornerBarDiameter, 8)
            };
            var opt = new ColumnDetailingOptions
            {
                ColumnMark = "C1",
                Style = ColumnSectionStyle.REC2,
                Stories = stories,
                IncludeCrossSections = true
            };
            LastGeneratedBuffer = gen.GenerateElevationAndSlices(opt);
        }

        private List<ColumnBarPlacement> GenerateBarLayout()
        {
            var list = new List<ColumnBarPlacement>();
            double cover = 30.0;
            double halfB = WidthBMm * 0.5 - cover;
            double halfH = HeightHMm * 0.5 - cover;

            list.Add(new ColumnBarPlacement(-halfB, -halfH, CornerBarDiameter));
            list.Add(new ColumnBarPlacement(halfB, -halfH, CornerBarDiameter));
            list.Add(new ColumnBarPlacement(halfB, halfH, CornerBarDiameter));
            list.Add(new ColumnBarPlacement(-halfB, halfH, CornerBarDiameter));

            for (int i = 1; i <= SideBarCountB; i++)
            {
                double x = -halfB + (2.0 * halfB * i / (SideBarCountB + 1));
                list.Add(new ColumnBarPlacement(x, -halfH, SideBarDiameter));
                list.Add(new ColumnBarPlacement(x, halfH, SideBarDiameter));
            }

            for (int j = 1; j <= SideBarCountH; j++)
            {
                double y = -halfH + (2.0 * halfH * j / (SideBarCountH + 1));
                list.Add(new ColumnBarPlacement(-halfB, y, SideBarDiameter));
                list.Add(new ColumnBarPlacement(halfB, y, SideBarDiameter));
            }

            return list;
        }
    }
}
