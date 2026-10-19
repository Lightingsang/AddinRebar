using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Rebar;

namespace HPDcePro.UI.ViewModels
{
    public sealed class CuttingDemandRowItem : ObservableObject
    {
        public string DemandId { get; set; } = "D1-1";
        public int DiameterMm { get; set; } = 20;
        public double LengthMm { get; set; } = 5800.0;
        public int Quantity { get; set; } = 12;
    }

    public sealed class CuttingPatternRowItem : ObservableObject
    {
        public int PatternId { get; set; }
        public string DiameterDisplay { get; set; } = "Φ20";
        public string PiecesCutSummary { get; set; } = string.Empty;
        public double RemnantMm { get; set; }
        public bool IsReusable { get; set; }
    }

    public sealed partial class CuttingOptimizationAdvancedViewModel : ObservableObject
    {
        [ObservableProperty]
        private double _stockLengthMm = 11700.0;

        [ObservableProperty]
        private int _totalStockBarsUsed;

        [ObservableProperty]
        private double _totalScrapLengthMm;

        [ObservableProperty]
        private double _wastePercentage;

        [ObservableProperty]
        private double _totalTonnageKg;

        public ObservableCollection<CuttingDemandRowItem> Demands { get; } = new();
        public ObservableCollection<CuttingPatternRowItem> Patterns { get; } = new();

        public CuttingOptimizationAdvancedViewModel()
        {
            Demands.Add(new CuttingDemandRowItem { DemandId = "D1-Lớp trên", DiameterMm = 20, LengthMm = 5800.0, Quantity = 8 });
            Demands.Add(new CuttingDemandRowItem { DemandId = "D1-Lớp dưới", DiameterMm = 20, LengthMm = 5600.0, Quantity = 8 });
            Demands.Add(new CuttingDemandRowItem { DemandId = "D2-Gối gãy", DiameterMm = 20, LengthMm = 2800.0, Quantity = 12 });
            Optimize();
        }

        [RelayCommand]
        public void Optimize()
        {
            Patterns.Clear();
            var demandList = new List<CuttingDemandItem>();
            for (int i = 0; i < Demands.Count; i++)
            {
                var d = Demands[i];
                demandList.Add(new CuttingDemandItem(d.DemandId, d.DiameterMm, d.LengthMm, d.Quantity));
            }

            if (demandList.Count == 0) return;

            double stockLen = StockLengthMm > 0 ? StockLengthMm : 11700.0;
            var grouped = demandList.GroupBy(d => d.DiameterMm);

            int totalBars = 0;
            double totalScrap = 0.0;
            double totalUsed = 0.0;

            foreach (var grp in grouped)
            {
                var res = RebarCuttingOptimizer.Optimize(grp, grp.Key, CuttingStrategy.BestFitDecreasing, stockLen);
                totalBars += res.TotalStockBarsUsed;
                totalScrap += res.TotalRemnantLengthMm;
                totalUsed += res.TotalUsedLengthMm;

                for (int p = 0; p < res.Patterns.Count; p++)
                {
                    var pat = res.Patterns[p];
                    string pieces = string.Join(" + ", pat.Cuts.Select(c => $"{c.LengthMm:F0}"));
                    Patterns.Add(new CuttingPatternRowItem
                    {
                        PatternId = Patterns.Count + 1,
                        DiameterDisplay = $"Φ{pat.DiameterMm}",
                        PiecesCutSummary = pieces,
                        RemnantMm = pat.RemnantLengthMm,
                        IsReusable = pat.IsReusableRemnant
                    });
                }
            }

            TotalStockBarsUsed = totalBars;
            TotalScrapLengthMm = totalScrap;
            double totalMaterial = totalUsed + totalScrap;
            WastePercentage = totalMaterial > 0 ? (totalScrap / totalMaterial) * 100.0 : 0.0;
            TotalTonnageKg = totalBars * (StockLengthMm / 1000.0) * (20 * 20 * 0.006165);
        }

        [RelayCommand]
        public void AddDemand()
        {
            Demands.Add(new CuttingDemandRowItem
            {
                DemandId = $"Bar-{Demands.Count + 1}",
                DiameterMm = 20,
                LengthMm = 3500.0,
                Quantity = 6
            });
            Optimize();
        }
    }
}
