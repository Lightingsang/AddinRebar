using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Rebar;

namespace HPDcePro.UI.ViewModels
{
    public sealed partial class CuttingDemandItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private int _diameterMm = 18;

        [ObservableProperty]
        private double _lengthMm = 3500.0;

        [ObservableProperty]
        private int _quantity = 10;

        public CuttingDemandItemViewModel() { }

        public CuttingDemandItemViewModel(string id, int diameterMm, double lengthMm, int quantity)
        {
            Id = id;
            DiameterMm = diameterMm;
            LengthMm = lengthMm;
            Quantity = quantity;
        }

        public CuttingDemandItem ToDomain() => new(Id, DiameterMm, LengthMm, Quantity);
    }

    public sealed partial class CuttingOptimizationViewModel : ObservableObject
    {
        [ObservableProperty]
        private double _stockLengthMm = RebarCuttingOptimizer.CommercialStockLengthMm;

        [ObservableProperty]
        private int _selectedDiameter = 18;

        [ObservableProperty]
        private int _totalStockBarsUsed;

        [ObservableProperty]
        private double _scrapPercentage;

        [ObservableProperty]
        private double _totalWastePercentage;

        [ObservableProperty]
        private CuttingStrategy _selectedStrategy = CuttingStrategy.Auto;

        public ObservableCollection<CuttingDemandItemViewModel> Demands { get; } = new();
        public ObservableCollection<CuttingPattern> ResultPatterns { get; } = new();

        public IReadOnlyList<int> AvailableDiameters { get; } = new[] { 10, 12, 14, 16, 18, 20, 22, 25, 28, 32 };
        public IReadOnlyList<CuttingStrategy> AvailableStrategies { get; } = new[]
        {
            CuttingStrategy.Auto,
            CuttingStrategy.BestFitDecreasing,
            CuttingStrategy.FirstFitDecreasing
        };

        public CuttingOptimizationViewModel()
        {
            LoadSampleDemands();
            Optimize();
        }

        [RelayCommand]
        public void AddDemand()
        {
            int nextIdx = Demands.Count + 1;
            Demands.Add(new CuttingDemandItemViewModel($"Bar_{nextIdx}", SelectedDiameter, 2500.0, 5));
        }

        [RelayCommand]
        public void RemoveDemand(CuttingDemandItemViewModel? item)
        {
            if (item != null)
            {
                Demands.Remove(item);
            }
        }

        [RelayCommand]
        public void LoadSampleDemands()
        {
            Demands.Clear();
            Demands.Add(new CuttingDemandItemViewModel("D18_Beam1_Top", 18, 5800.0, 4));
            Demands.Add(new CuttingDemandItemViewModel("D18_Beam1_Bot", 18, 4200.0, 6));
            Demands.Add(new CuttingDemandItemViewModel("D18_Beam2_Ext", 18, 2600.0, 8));
            Demands.Add(new CuttingDemandItemViewModel("D18_Column_Splice", 18, 3900.0, 4));
        }

        [RelayCommand]
        public void Optimize()
        {
            ResultPatterns.Clear();
            var domainDemands = Demands.Select(d => d.ToDomain()).ToList();
            var result = RebarCuttingOptimizer.Optimize(
                domainDemands, SelectedDiameter, SelectedStrategy, StockLengthMm);

            TotalStockBarsUsed = result.TotalStockBarsUsed;
            ScrapPercentage = Math.Round(result.ScrapPercentage, 2);
            TotalWastePercentage = Math.Round(result.TotalWastePercentage, 2);

            foreach (var pat in result.Patterns)
            {
                ResultPatterns.Add(pat);
            }
        }
    }
}
