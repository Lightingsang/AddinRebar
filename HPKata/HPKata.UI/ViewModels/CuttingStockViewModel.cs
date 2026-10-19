using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Optimization;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for Feature 14 (1D Cutting Stock Optimization Dialog).
    /// Solves 1D-CSP for 11,700 mm commercial stock bars with 5 heuristics and warehouse offcut inventory.
    /// </summary>
    public sealed partial class CuttingStockViewModel : ObservableObject
    {
        public ObservableCollection<CuttingDemand> Demands { get; } = new();
        public ObservableCollection<WarehouseOffcut> WarehouseOffcuts { get; } = new();
        public ObservableCollection<CuttingPattern> CuttingPatterns { get; } = new();
        public ObservableCollection<WarehouseOffcut> GeneratedOffcuts { get; } = new();
        public ObservableCollection<int> AvailableDiameters { get; } = new();

        public IReadOnlyList<SolverStrategy> Strategies { get; } = new[]
        {
            SolverStrategy.Auto,
            SolverStrategy.DuBang0,
            SolverStrategy.LapDayCanhDai,
            SolverStrategy.BeamSearchTopK,
            SolverStrategy.PatternCat
        };

        [ObservableProperty] private int _selectedDiameter = 20;
        [ObservableProperty] private double _standardStockLengthMm = 11700.0;
        [ObservableProperty] private double _minRemnantLengthMm = 500.0;
        [ObservableProperty] private double _sawKerfMm = 5.0;
        [ObservableProperty] private SolverStrategy _selectedStrategy = SolverStrategy.Auto;

        [ObservableProperty] private int _newStandardBarsCount;
        [ObservableProperty] private int _consumedWarehouseBarsCount;
        [ObservableProperty] private int _totalCutsCount;
        [ObservableProperty] private double _grossWastePercent;
        [ObservableProperty] private double _netWastePercent;
        [ObservableProperty] private double _totalScrapWeightKg;
        [ObservableProperty] private double _executionTimeMs;
        [ObservableProperty] private string _statusMessage = "Ready to optimize.";
        [ObservableProperty] private bool _hasResults;

        public CuttingStockViewModel()
        {
            LoadSampleDemands();
        }

        public void LoadFromRebarItems(IEnumerable<RebarItem> items)
        {
            Demands.Clear();
            foreach (var item in items)
            {
                if (item.CuttingLengthMm > 0.0 && item.TotalCount > 0)
                {
                    Demands.Add(new CuttingDemand(
                        item.DiameterMm,
                        item.CuttingLengthMm,
                        item.TotalCount,
                        item.BarMark,
                        item.ComponentMark));
                }
            }
            RefreshAvailableDiameters();
        }

        [RelayCommand]
        public void LoadSampleDemands()
        {
            Demands.Clear();
            Demands.Add(new CuttingDemand(20, 5850, 6, "1", "D1"));
            Demands.Add(new CuttingDemand(20, 3900, 4, "2", "D1"));
            Demands.Add(new CuttingDemand(20, 2340, 8, "3", "D1"));
            Demands.Add(new CuttingDemand(20, 1950, 10, "4", "D1"));
            Demands.Add(new CuttingDemand(25, 4200, 8, "1", "C1"));
            Demands.Add(new CuttingDemand(25, 3300, 6, "2", "C1"));

            WarehouseOffcuts.Clear();
            WarehouseOffcuts.Add(new WarehouseOffcut(20, 4000, 2));
            WarehouseOffcuts.Add(new WarehouseOffcut(20, 2500, 3));

            RefreshAvailableDiameters();
            StatusMessage = "Loaded sample cutting demands and offcuts.";
        }

        private void RefreshAvailableDiameters()
        {
            var dias = Demands.Select(d => d.DiameterMm).Distinct().OrderBy(d => d).ToList();
            AvailableDiameters.Clear();
            foreach (var d in dias)
            {
                AvailableDiameters.Add(d);
            }
            if (AvailableDiameters.Count > 0 && !AvailableDiameters.Contains(SelectedDiameter))
            {
                SelectedDiameter = AvailableDiameters[0];
            }
        }

        [RelayCommand]
        public void Optimize()
        {
            var targetDemands = Demands.Where(d => d.DiameterMm == SelectedDiameter).ToList();
            if (targetDemands.Count == 0)
            {
                StatusMessage = $"No cutting demands found for diameter Φ{SelectedDiameter}.";
                return;
            }

            var sw = Stopwatch.StartNew();
            var engine = new CuttingStockEngine
            {
                StandardStockLengthMm = StandardStockLengthMm,
                MinRemnantLengthMm = MinRemnantLengthMm,
                SawKerfMm = SawKerfMm
            };

            var targetWarehouse = WarehouseOffcuts.Where(w => w.DiameterMm == SelectedDiameter).ToList();
            var result = engine.Solve(SelectedDiameter, targetDemands, targetWarehouse, SelectedStrategy);
            sw.Stop();

            ExecutionTimeMs = sw.Elapsed.TotalMilliseconds;
            DisplayResult(result);
        }

        private void DisplayResult(CuttingStockResult result)
        {
            CuttingPatterns.Clear();
            foreach (var p in result.Patterns)
            {
                CuttingPatterns.Add(p);
            }

            GeneratedOffcuts.Clear();
            foreach (var off in result.NewWarehouseOffcuts)
            {
                GeneratedOffcuts.Add(off);
            }

            NewStandardBarsCount = result.NewStandardBarsCount;
            ConsumedWarehouseBarsCount = result.ConsumedWarehouseBarsCount;
            TotalCutsCount = result.Patterns.Sum(p => p.CutsMm.Count);
            GrossWastePercent = result.GrossWastePercent;
            NetWastePercent = result.NetWastePercent;

            double unitMass = RebarCalculator.GetUnitMass(SelectedDiameter);
            double scrapLengthM = result.Patterns
                .Where(p => !p.IsReusableRemnant)
                .Sum(p => p.RemnantLengthMm) / 1000.0;
            TotalScrapWeightKg = scrapLengthM * unitMass;

            HasResults = true;
            StatusMessage = $"Optimized {TotalCutsCount} cuts using {NewStandardBarsCount} stock bars (Waste: {NetWastePercent:F2}%) in {ExecutionTimeMs:F1}ms.";
        }
    }
}
