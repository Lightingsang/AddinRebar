using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPDcePro.Core.Rebar;

namespace HPDcePro.UI.ViewModels
{
    public sealed class RebarScheduleRowItem : ObservableObject
    {
        public string ElementMark { get; set; } = "D1";
        public string BarNumber { get; set; } = "1";
        public int ShapeCode { get; set; } = 1;
        public int DiameterMm { get; set; } = 18;
        public int CountPerElement { get; set; } = 4;
        public int ElementCount { get; set; } = 6;
        public int TotalCount => CountPerElement * ElementCount;
        public double TotalLengthMm { get; set; } = 5800.0;
        public double UnitWeightKgM => DiameterMm * DiameterMm * 0.006165;
        public double TotalWeightKg => (TotalLengthMm / 1000.0) * UnitWeightKgM * TotalCount;
    }

    public sealed partial class MasterRebarScheduleDashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _batchQuantity = 8;

        [ObservableProperty]
        private int _batchDiameter = 20;

        [ObservableProperty]
        private double _totalWeightSmallDiaKg;

        [ObservableProperty]
        private double _totalWeightMediumDiaKg;

        [ObservableProperty]
        private double _totalWeightLargeDiaKg;

        [ObservableProperty]
        private double _grandTotalTonnageTon;

        public ObservableCollection<RebarScheduleRowItem> ScheduleItems { get; } = new();

        public MasterRebarScheduleDashboardViewModel()
        {
            ScheduleItems.Add(new RebarScheduleRowItem { ElementMark = "D1", BarNumber = "1", DiameterMm = 18, CountPerElement = 4, ElementCount = 6, TotalLengthMm = 5800 });
            ScheduleItems.Add(new RebarScheduleRowItem { ElementMark = "D1", BarNumber = "2", DiameterMm = 8, CountPerElement = 28, ElementCount = 6, TotalLengthMm = 1450 });
            ScheduleItems.Add(new RebarScheduleRowItem { ElementMark = "D2", BarNumber = "1", DiameterMm = 20, CountPerElement = 4, ElementCount = 4, TotalLengthMm = 6200 });
            ScheduleItems.Add(new RebarScheduleRowItem { ElementMark = "C1", BarNumber = "1", DiameterMm = 22, CountPerElement = 8, ElementCount = 8, TotalLengthMm = 4200 });
            RecalculateWeights();
        }

        [RelayCommand]
        public void RecalculateWeights()
        {
            double small = 0.0;
            double medium = 0.0;
            double large = 0.0;

            for (int i = 0; i < ScheduleItems.Count; i++)
            {
                var item = ScheduleItems[i];
                double w = item.TotalWeightKg;
                if (item.DiameterMm <= 10) small += w;
                else if (item.DiameterMm <= 18) medium += w;
                else large += w;
            }

            TotalWeightSmallDiaKg = small;
            TotalWeightMediumDiaKg = medium;
            TotalWeightLargeDiaKg = large;
            GrandTotalTonnageTon = (small + medium + large) / 1000.0;
        }

        [RelayCommand]
        public void BatchUpdateQuantity()
        {
            for (int i = 0; i < ScheduleItems.Count; i++)
            {
                ScheduleItems[i].ElementCount = BatchQuantity;
            }
            RecalculateWeights();
        }

        [RelayCommand]
        public void BatchUpdateDiameter()
        {
            for (int i = 0; i < ScheduleItems.Count; i++)
            {
                if (ScheduleItems[i].DiameterMm >= 16)
                {
                    ScheduleItems[i].DiameterMm = BatchDiameter;
                }
            }
            RecalculateWeights();
        }
    }
}
