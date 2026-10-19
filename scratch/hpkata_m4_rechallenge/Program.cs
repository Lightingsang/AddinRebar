using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HPKata.Core.Configuration;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.UI.ViewModels;

namespace HPKata.UI.EmpiricalRechallenge
{
    public static class Program
    {
        private static int _passed;
        private static int _failed;
        private static readonly List<string> Failures = new();

        public static int Main()
        {
            Console.WriteLine("======================================================================");
            Console.WriteLine("   HPKATA MILESTONE M4 ITERATION 2 EMPIRICAL RE-CHALLENGE HARNESS     ");
            Console.WriteLine("======================================================================");

            Test_Defect_M4_02_SetupViewModel_IsDirty();
            Test_Defect_M4_02_Exhaustive_86_Properties();
            Test_Defect_M4_03_PatternCat_Kerf_Guard();
            Test_Defect_M4_03_PatternCat_Kerf_Matrix();
            Test_All_Solvers_Consistency();

            Console.WriteLine("\n======================================================================");
            Console.WriteLine($"   FINAL RESULTS: {_passed} PASSED, {_failed} FAILED");
            Console.WriteLine("======================================================================");

            if (Failures.Count > 0)
            {
                Console.WriteLine("\nFAILURES:");
                foreach (var f in Failures)
                {
                    Console.WriteLine($" [FAIL] {f}");
                }
            }

            return _failed;
        }

        private static void Assert(bool condition, string testName, string detail = "")
        {
            if (condition)
            {
                _passed++;
                Console.WriteLine($"  [PASS] {testName}");
            }
            else
            {
                _failed++;
                string msg = string.IsNullOrEmpty(detail) ? testName : $"{testName} -> {detail}";
                Failures.Add(msg);
                Console.WriteLine($"  [FAIL] {msg}");
            }
        }

        private static void Test_Defect_M4_02_SetupViewModel_IsDirty()
        {
            Console.WriteLine("\n--- TEST: DEFECT-M4-02 SetupViewModel.IsDirty State Machine ---");

            var vm = new SetupViewModel();
            Assert(!vm.IsDirty, "SetupViewModel.IsDirty is initially false");

            var config = KataConfigDefaults.CreateDefault();
            config.ConcreteCoverMm = 40.0;
            var vmConfig = new SetupViewModel(config);
            Assert(!vmConfig.IsDirty, "Constructor with KataConfig leaves IsDirty == false");

            vm.StatusMessage = "Updated status";
            Assert(!vm.IsDirty, "Updating StatusMessage does NOT trigger IsDirty = true");

            vm.ConcreteCoverMm = 45.0;
            Assert(vm.IsDirty, "Setting ConcreteCoverMm = 45.0 updates IsDirty to true");

            string tempFile = Path.Combine(Path.GetTempPath(), $"rechallenge_{Guid.NewGuid():N}.txt");
            try
            {
                vm.SaveToFile(tempFile);
                Assert(!vm.IsDirty, "SaveToFile resets IsDirty to false");

                vm.BaseBarDiameter = 25;
                Assert(vm.IsDirty, "Setting BaseBarDiameter = 25 updates IsDirty to true");

                vm.ApplyAndClose();
                Assert(!vm.IsDirty, "ApplyAndClose resets IsDirty to false");

                vm.LayerThepChu = "CUSTOM_LAYER";
                Assert(vm.IsDirty, "Setting string property LayerThepChu updates IsDirty to true");

                vm.LoadFromFile(tempFile);
                Assert(!vm.IsDirty, "LoadFromFile resets IsDirty to false");

                vm.ResetToDefaults();
                Assert(vm.IsDirty, "ResetToDefaults sets IsDirty to true");
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        private static void Test_Defect_M4_02_Exhaustive_86_Properties()
        {
            Console.WriteLine("\n--- TEST: DEFECT-M4-02 Exhaustive Property Mutation on SetupViewModel ---");

            var configProps = typeof(KataConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            int reactiveCount = 0;
            var nonReactive = new List<string>();

            foreach (var prop in configProps)
            {
                var vm = new SetupViewModel();
                var vmProp = typeof(SetupViewModel).GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance);
                if (vmProp == null || !vmProp.CanWrite)
                {
                    continue;
                }

                object? originalVal = vmProp.GetValue(vm);
                object? newVal = null;

                if (vmProp.PropertyType == typeof(string))
                {
                    newVal = (string)originalVal! + "_MOD";
                }
                else if (vmProp.PropertyType == typeof(int))
                {
                    newVal = (int)originalVal! + 1;
                }
                else if (vmProp.PropertyType == typeof(double))
                {
                    newVal = (double)originalVal! + 5.0;
                }

                if (newVal != null)
                {
                    vmProp.SetValue(vm, newVal);
                    if (vm.IsDirty)
                    {
                        reactiveCount++;
                    }
                    else
                    {
                        nonReactive.Add(prop.Name);
                    }
                }
            }

            Assert(reactiveCount == 86, $"All 86 KataConfig properties on SetupViewModel set IsDirty = true when modified (Verified: {reactiveCount}/86)",
                $"Non-reactive: {string.Join(", ", nonReactive)}");
        }

        private static void Test_Defect_M4_03_PatternCat_Kerf_Guard()
        {
            Console.WriteLine("\n--- TEST: DEFECT-M4-03 PatternCat Kerf Guard Behavior ---");

            var engine = new CuttingStockEngine
            {
                StandardStockLengthMm = 11700.0,
                SawKerfMm = 5.0
            };

            // 1. Modular 5850mm cuts with kerf 5mm: (2 * 5850 + 5 = 11705 > 11700)
            var demands5850 = new[] { new CuttingDemand(20, 5850.0, 4) };
            var res5850 = engine.Solve(20, demands5850, strategy: SolverStrategy.PatternCat);

            Assert(res5850.Patterns.Count > 0, "PatternCat solves 4x5850mm demands with kerf=5mm");
            bool all5850Valid = res5850.Patterns.All(p => p.UsedLengthMm <= p.StockLengthMm + 1e-4);
            Assert(all5850Valid, "Zero patterns exceed StockLength for 5850mm modular demands with kerf=5mm");
            bool noDouble5850 = res5850.Patterns.All(p => p.CutsMm.Count <= 1);
            Assert(noDouble5850, "PatternCat correctly refused to pair 2x5850mm on 11,700mm bar when kerf=5mm");

            // 2. Modular 5850mm cuts with kerf 0mm: (2 * 5850 = 11700 <= 11700)
            var engineNoKerf = new CuttingStockEngine
            {
                StandardStockLengthMm = 11700.0,
                SawKerfMm = 0.0
            };
            var res5850NoKerf = engineNoKerf.Solve(20, demands5850, strategy: SolverStrategy.PatternCat);
            bool hasDouble5850 = res5850NoKerf.Patterns.Any(p => p.CutsMm.Count == 2);
            Assert(hasDouble5850, "PatternCat safely pairs 2x5850mm on 11,700mm bar when kerf=0mm");

            // 3. Pair cuts: 6000mm + 5700mm with kerf 5mm: (6000 + 5700 + 5 = 11705 > 11700)
            var demandsPair = new[]
            {
                new CuttingDemand(20, 6000.0, 2),
                new CuttingDemand(20, 5700.0, 2)
            };
            var resPair = engine.Solve(20, demandsPair, strategy: SolverStrategy.PatternCat);
            bool allPairValid = resPair.Patterns.All(p => p.UsedLengthMm <= p.StockLengthMm + 1e-4);
            Assert(allPairValid, "Zero patterns exceed StockLength for 6000mm+5700mm pair demands with kerf=5mm");

            // 4. Larger stock bar (12,000mm) with kerf 5mm: (2 * 5850 + 5 = 11705 <= 12000)
            var engine12m = new CuttingStockEngine
            {
                StandardStockLengthMm = 12000.0,
                SawKerfMm = 5.0
            };
            var res12m = engine12m.Solve(20, demands5850, strategy: SolverStrategy.PatternCat);
            bool hasPairOn12m = res12m.Patterns.Any(p => p.CutsMm.Count == 2);
            Assert(hasPairOn12m, "PatternCat pairs 2x5850mm when stock bar is 12,000mm with kerf=5mm (11,705 <= 12,000)");
        }

        private static void Test_Defect_M4_03_PatternCat_Kerf_Matrix()
        {
            Console.WriteLine("\n--- TEST: DEFECT-M4-03 Stress Matrix across Kerf Values (0, 1, 3, 5, 10, 20 mm) ---");

            double[] kerfs = { 0.0, 1.0, 3.0, 5.0, 10.0, 20.0 };
            var demands = new[]
            {
                new CuttingDemand(20, 5850.0, 6),
                new CuttingDemand(20, 3900.0, 6),
                new CuttingDemand(20, 2925.0, 8),
                new CuttingDemand(20, 6000.0, 4),
                new CuttingDemand(20, 5700.0, 4),
                new CuttingDemand(20, 2500.0, 5),
                new CuttingDemand(20, 1200.0, 8)
            };

            foreach (double kerf in kerfs)
            {
                var engine = new CuttingStockEngine
                {
                    StandardStockLengthMm = 11700.0,
                    SawKerfMm = kerf
                };

                var res = engine.Solve(20, demands, strategy: SolverStrategy.PatternCat);
                bool valid = true;
                string failMsg = "";

                foreach (var pat in res.Patterns)
                {
                    double cuts = pat.CutsMm.Sum();
                    double kerfSum = pat.CutsMm.Count > 1 ? (pat.CutsMm.Count - 1) * kerf : 0.0;
                    double used = cuts + kerfSum;
                    if (used > pat.StockLengthMm + 1e-4)
                    {
                        valid = false;
                        failMsg = $"Used={used:F1}mm > Stock={pat.StockLengthMm:F1}mm (Cuts: {string.Join("+", pat.CutsMm)})";
                        break;
                    }
                }

                Assert(valid, $"PatternCat with kerf={kerf}mm: All {res.Patterns.Count} patterns conserve length (Used <= Stock)", failMsg);
            }
        }

        private static void Test_All_Solvers_Consistency()
        {
            Console.WriteLine("\n--- TEST: Consistency Across All 5 Solver Strategies ---");

            var strategies = new[]
            {
                SolverStrategy.Auto,
                SolverStrategy.DuBang0,
                SolverStrategy.LapDayCanhDai,
                SolverStrategy.BeamSearchTopK,
                SolverStrategy.PatternCat
            };

            var vm = new CuttingStockViewModel();
            foreach (var strat in strategies)
            {
                vm.SelectedDiameter = 20;
                vm.SelectedStrategy = strat;
                vm.StandardStockLengthMm = 11700.0;
                vm.SawKerfMm = 5.0;
                vm.MinRemnantLengthMm = 500.0;

                vm.Optimize();

                Assert(vm.HasResults, $"{strat}: Optimize() produces results");
                Assert(vm.CuttingPatterns.Count > 0, $"{strat}: Produced patterns > 0");
                bool valid = vm.CuttingPatterns.All(p => p.UsedLengthMm <= p.StockLengthMm + 1e-4);
                Assert(valid, $"{strat}: All patterns satisfy UsedLengthMm <= StockLengthMm");
            }
        }
    }
}
