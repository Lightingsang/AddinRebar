using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Calculators;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.Core.Shapes;
using HPKata.Core.Tagging;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for Feature 14 (Rebar Scheduling Dialog).
    /// Manages the detailing rebar schedule table, summaries by TCVN bins, QR tags, and Excel export.
    /// </summary>
    public sealed partial class ScheduleViewModel : ObservableObject
    {
        public ObservableCollection<RebarItem> Items { get; } = new();

        [ObservableProperty] private RebarItem? _selectedItem;
        [ObservableProperty] private string _selectedQrPayload = string.Empty;
        [ObservableProperty] private string _statusMessage = "Ready";

        // Summary Metrics
        [ObservableProperty] private int _totalItemCount;
        [ObservableProperty] private int _totalBarCount;
        [ObservableProperty] private double _totalLengthM;
        [ObservableProperty] private double _totalWeightKg;
        [ObservableProperty] private double _smallBarWeightKg;   // d <= 10mm
        [ObservableProperty] private double _mediumBarWeightKg;  // 10 < d <= 18mm
        [ObservableProperty] private double _largeBarWeightKg;   // d > 18mm

        public event Action<RebarItem>? RequestEditItem;
        public event Action<IEnumerable<RebarItem>>? RequestOpenCuttingStock;

        public ScheduleViewModel()
        {
            LoadSampleData();
        }

        partial void OnSelectedItemChanged(RebarItem? value)
        {
            UpdateQrPayload(value);
        }

        private void UpdateQrPayload(RebarItem? item)
        {
            if (item is null)
            {
                SelectedQrPayload = string.Empty;
                return;
            }

            var payload = new QrTagPayload
            {
                Project = "HPKata Project",
                Component = item.ComponentMark,
                Mark = item.BarMark,
                ShapeCode = item.ShapeCode,
                DiameterMm = item.DiameterMm,
                CutLengthMm = item.CuttingLengthMm,
                Quantity = item.TotalCount,
                WeightKg = item.TotalWeightKg,
                SegmentsMm = item.ParametersMm
            };
            SelectedQrPayload = QrTagGenerator.FormatPayload(payload);
        }

        [RelayCommand]
        public void LoadSampleData()
        {
            Items.Clear();
            var sampleBars = CreateSampleRebarItems();
            foreach (var b in sampleBars)
            {
                Items.Add(b);
            }
            RecalculateTotals();
            StatusMessage = $"Loaded {Items.Count} sample detailing bars.";
        }

        [RelayCommand]
        public void AddItem(RebarItem? item)
        {
            if (item is null) return;
            Items.Add(item);
            RecalculateTotals();
            SelectedItem = item;
            StatusMessage = $"Added bar {item.ComponentMark} #{item.BarMark}.";
        }

        [RelayCommand]
        public void EditItem(RebarItem? item)
        {
            var target = item ?? SelectedItem;
            if (target is not null)
            {
                RequestEditItem?.Invoke(target);
            }
        }

        [RelayCommand]
        public void RemoveItem(RebarItem? item)
        {
            var target = item ?? SelectedItem;
            if (target is null) return;
            Items.Remove(target);
            RecalculateTotals();
            SelectedItem = Items.FirstOrDefault();
            StatusMessage = "Item removed.";
        }

        [RelayCommand]
        public void ClearAll()
        {
            Items.Clear();
            RecalculateTotals();
            SelectedItem = null;
            StatusMessage = "Schedule cleared.";
        }

        [RelayCommand]
        public void RecalculateTotals()
        {
            TotalItemCount = Items.Count;
            TotalBarCount = Items.Sum(i => i.TotalCount);
            TotalLengthM = Items.Sum(i => i.TotalLengthM);
            TotalWeightKg = Items.Sum(i => i.TotalWeightKg);

            SmallBarWeightKg = Items.Where(i => i.DiameterMm <= 10).Sum(i => i.TotalWeightKg);
            MediumBarWeightKg = Items.Where(i => i.DiameterMm > 10 && i.DiameterMm <= 18).Sum(i => i.TotalWeightKg);
            LargeBarWeightKg = Items.Where(i => i.DiameterMm > 18).Sum(i => i.TotalWeightKg);
        }

        [RelayCommand]
        public void ExportToExcel(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                StatusMessage = "Invalid export file path.";
                return;
            }

            try
            {
                var report = RebarScheduleAggregator.GenerateReport(Items);
                ClosedXmlExporter.ExportScheduleToFile(report, filePath);
                StatusMessage = $"Exported schedule to {System.IO.Path.GetFileName(filePath)}.";
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: Excel export error logging
                StatusMessage = $"Excel export error: {ex.Message}";
            }
        }

        [RelayCommand]
        public void OpenCuttingStock()
        {
            RequestOpenCuttingStock?.Invoke(Items);
        }

        private static List<RebarItem> CreateSampleRebarItems()
        {
            return new List<RebarItem>
            {
                new() { ComponentMark = "D1", BarMark = "1", ShapeCode = "01", DiameterMm = 20, CountPerMember = 3, MemberCount = 2, ParametersMm = new double[] { 5850 }, CuttingLengthMm = 5850, UnitMassKgPerM = 2.47 },
                new() { ComponentMark = "D1", BarMark = "2", ShapeCode = "01", DiameterMm = 20, CountPerMember = 2, MemberCount = 2, ParametersMm = new double[] { 3900 }, CuttingLengthMm = 3900, UnitMassKgPerM = 2.47 },
                new() { ComponentMark = "D1", BarMark = "3", ShapeCode = "22", DiameterMm = 8, CountPerMember = 24, MemberCount = 2, ParametersMm = new double[] { 250, 450, 75 }, CuttingLengthMm = 1550, Spacing = "a150", UnitMassKgPerM = 0.395 },
                new() { ComponentMark = "D1", BarMark = "4", ShapeCode = "01", DiameterMm = 16, CountPerMember = 2, MemberCount = 2, ParametersMm = new double[] { 4200 }, CuttingLengthMm = 4200, UnitMassKgPerM = 1.58 },
                new() { ComponentMark = "C1", BarMark = "1", ShapeCode = "01", DiameterMm = 22, CountPerMember = 8, MemberCount = 4, ParametersMm = new double[] { 3800 }, CuttingLengthMm = 3800, UnitMassKgPerM = 2.98 },
                new() { ComponentMark = "C1", BarMark = "2", ShapeCode = "22", DiameterMm = 10, CountPerMember = 28, MemberCount = 4, ParametersMm = new double[] { 350, 350, 80 }, CuttingLengthMm = 1560, Spacing = "a100/a200", UnitMassKgPerM = 0.617 }
            };
        }
    }
}
