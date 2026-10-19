using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HPDcePro.UI.ViewModels
{
    public sealed class RebarArrangementCandidate
    {
        public int DiameterMm { get; set; }
        public double SpacingMm { get; set; }
        public int BarCount { get; set; }
        public double ProvidedAsMm2 { get; set; }
        public double Ratio { get; set; }
        public bool IsSafe { get; set; }
        public double SteelWeightKg { get; set; }
    }

    public sealed partial class MongCocChonThepTuDongViewModel : ObservableObject
    {
        [ObservableProperty]
        private double _requiredAsXMm2 = 1850.0;

        [ObservableProperty]
        private double _requiredAsYMm2 = 1650.0;

        [ObservableProperty]
        private double _capWidthBMm = 1800.0;

        [ObservableProperty]
        private double _capLengthLMm = 1800.0;

        [ObservableProperty]
        private string _selectedDirection = "X";

        public ObservableCollection<RebarArrangementCandidate> Candidates { get; } = new();

        public MongCocChonThepTuDongViewModel()
        {
            SearchOptimalConfigurations();
        }

        [RelayCommand]
        public void SearchOptimalConfigurations()
        {
            Candidates.Clear();
            double targetAs = SelectedDirection == "X" ? RequiredAsXMm2 : RequiredAsYMm2;
            double span = SelectedDirection == "X" ? CapWidthBMm : CapLengthLMm;
            int[] testDiameters = { 12, 14, 16, 18, 20, 22 };
            double[] testSpacings = { 100.0, 120.0, 150.0, 180.0, 200.0 };

            for (int d = 0; d < testDiameters.Length; d++)
            {
                int dia = testDiameters[d];
                double area1 = Math.PI * dia * dia * 0.25;

                for (int s = 0; s < testSpacings.Length; s++)
                {
                    double sp = testSpacings[s];
                    int count = Math.Max(2, (int)Math.Floor(span / sp) + 1);
                    double providedAs = count * area1;
                    double ratio = targetAs > 0 ? providedAs / targetAs : 1.0;

                    if (ratio >= 0.95 && ratio <= 1.40)
                    {
                        double weightPerM = dia * dia * 0.006165;
                        double totalWeight = count * (span / 1000.0) * weightPerM;

                        Candidates.Add(new RebarArrangementCandidate
                        {
                            DiameterMm = dia,
                            SpacingMm = sp,
                            BarCount = count,
                            ProvidedAsMm2 = providedAs,
                            Ratio = ratio,
                            IsSafe = ratio >= 1.0,
                            SteelWeightKg = totalWeight
                        });
                    }
                }
            }
        }
    }
}
