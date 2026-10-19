using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Shapes;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for Feature 13 (Rebar Detailing & Shape Dialog).
    /// Provides live cutting length calculation for all 85 shapes, hook selection, and weight takeoff.
    /// </summary>
    public sealed partial class DetailingViewModel : ObservableObject
    {
        public ObservableCollection<RebarShapeDefinition> Shapes { get; } = new();
        public IReadOnlyList<int> Diameters { get; } = new[] { 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32, 36, 40, 50 };
        public IReadOnlyList<string> SteelGrades { get; } = new[] { "CB240-T", "CB300-V", "CB400-V", "CB500-V" };
        public IReadOnlyList<string> HookTypes { get; } = new[] { "Không móc (None)", "Móc 90°", "Móc 135°", "Móc 180°" };

        [ObservableProperty] private RebarShapeDefinition _selectedShape;
        [ObservableProperty] private int _diameter = 16;
        [ObservableProperty] private string _steelGrade = "CB400-V";
        [ObservableProperty] private string _hookType = "Không móc (None)";
        [ObservableProperty] private string _barMark = "1";
        [ObservableProperty] private string _componentMark = "D1";
        [ObservableProperty] private int _countPerMember = 2;
        [ObservableProperty] private int _memberCount = 1;
        [ObservableProperty] private string _spacing = "a200";
        [ObservableProperty] private string _notes = string.Empty;

        [ObservableProperty] private double _l1 = 1000.0;
        [ObservableProperty] private double _l2 = 300.0;
        [ObservableProperty] private double _l3 = 300.0;
        [ObservableProperty] private double _l4;
        [ObservableProperty] private double _l5;
        [ObservableProperty] private double _l6;
        [ObservableProperty] private double _angleDeg = 90.0;
        [ObservableProperty] private bool _applyBendDeduction;

        [ObservableProperty] private double _cuttingLengthMm;
        [ObservableProperty] private double _unitMassKgPerM;
        [ObservableProperty] private int _totalCount = 2;
        [ObservableProperty] private double _totalLengthM;
        [ObservableProperty] private double _totalWeightKg;

        [ObservableProperty] private bool _isL1Visible = true;
        [ObservableProperty] private bool _isL2Visible = true;
        [ObservableProperty] private bool _isL3Visible = true;
        [ObservableProperty] private bool _isL4Visible;
        [ObservableProperty] private bool _isL5Visible;
        [ObservableProperty] private bool _isL6Visible;
        [ObservableProperty] private bool _isAngleVisible;

        public event Action? RequestClose;
        public RebarItem? ResultItem { get; private set; }

        public DetailingViewModel()
        {
            PopulateShapes();
            _selectedShape = Shapes.Count > 1 ? Shapes[1] : Shapes[0]; // default to dang01
            UpdateShapeVisibility();
            Recalculate();
        }

        private void PopulateShapes()
        {
            foreach (RebarShapeType shapeType in Enum.GetValues(typeof(RebarShapeType)))
            {
                var def = RebarShapeCatalog.GetDefinition(shapeType);
                Shapes.Add(def);
            }
        }

        partial void OnSelectedShapeChanged(RebarShapeDefinition value)
        {
            UpdateShapeVisibility();
            Recalculate();
        }

        partial void OnDiameterChanged(int value) => Recalculate();
        partial void OnL1Changed(double value) => Recalculate();
        partial void OnL2Changed(double value) => Recalculate();
        partial void OnL3Changed(double value) => Recalculate();
        partial void OnL4Changed(double value) => Recalculate();
        partial void OnL5Changed(double value) => Recalculate();
        partial void OnL6Changed(double value) => Recalculate();
        partial void OnAngleDegChanged(double value) => Recalculate();
        partial void OnApplyBendDeductionChanged(bool value) => Recalculate();
        partial void OnCountPerMemberChanged(int value) => Recalculate();
        partial void OnMemberCountChanged(int value) => Recalculate();

        private void UpdateShapeVisibility()
        {
            if (SelectedShape is null) return;
            int count = SelectedShape.ParameterCount;
            IsL1Visible = count >= 1;
            IsL2Visible = count >= 2;
            IsL3Visible = count >= 3;
            IsL4Visible = count >= 4;
            IsL5Visible = count >= 5;
            IsL6Visible = count >= 6;
            IsAngleVisible = SelectedShape.HasAngleParameter;
        }

        public void Recalculate()
        {
            if (SelectedShape is null) return;

            double[] parameters = { L1, L2, L3, L4, L5, L6 };
            CuttingLengthMm = RebarShapeCatalog.CalculateCuttingLength(
                SelectedShape.ShapeType,
                parameters,
                AngleDeg,
                Diameter,
                ApplyBendDeduction);

            UnitMassKgPerM = RebarCalculator.GetUnitMass(Diameter);
            TotalCount = Math.Max(1, CountPerMember) * Math.Max(1, MemberCount);
            TotalLengthM = (CuttingLengthMm * TotalCount) / 1000.0;
            TotalWeightKg = TotalLengthM * UnitMassKgPerM;
        }

        public RebarItem ToRebarItem()
        {
            Recalculate();
            return new RebarItem
            {
                ComponentMark = ComponentMark,
                BarMark = BarMark,
                ShapeCode = SelectedShape?.ShapeCode ?? "01",
                DiameterMm = Diameter,
                Spacing = Spacing,
                CountPerMember = CountPerMember,
                MemberCount = MemberCount,
                ParametersMm = new[] { L1, L2, L3, L4, L5, L6 },
                SpecialAngleDeg = AngleDeg,
                Notes = Notes,
                CuttingLengthMm = CuttingLengthMm,
                UnitMassKgPerM = UnitMassKgPerM
            };
        }

        [RelayCommand]
        public void Apply()
        {
            ResultItem = ToRebarItem();
            RequestClose?.Invoke();
        }

        [RelayCommand]
        public void Cancel()
        {
            ResultItem = null;
            RequestClose?.Invoke();
        }
    }
}
