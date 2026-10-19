using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HPKata.Core.Calculators;
using HPKata.Core.Configuration;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.Core.Shapes;
using HPKata.UI.ViewModels;

namespace HPKata.UI.EmpiricalHarness
{
    public static class Program
    {
        private static int _testsPassed;
        private static int _testsFailed;
        private static readonly List<string> FailureDetails = new();
        private static readonly List<string> FindingNotes = new();

        public static int Main()
        {
            Console.WriteLine("======================================================================");
            Console.WriteLine("   HPKATA MILESTONE M4 EMPIRICAL CHALLENGER & STRESS HARNESS          ");
            Console.WriteLine("======================================================================");

            RunSuite1_SetupViewModel_Reflection_And_Roundtrip();
            RunSuite1B_SetupViewModel_IsDirty_State_Machine();
            RunSuite2_DetailingViewModel_Shapes_And_Reactive_Calculations();
            RunSuite2B_DetailingViewModel_Adversarial_Inputs();
            RunSuite3_CuttingStockViewModel_Optimization_And_Waste();
            RunSuite3B_CuttingStockViewModel_Edge_Cases();
            RunSuite4_ScheduleViewModel_Aggregation_And_QrPayload();
            RunSuite5_Clean_Code_Static_Audit();

            Console.WriteLine("\n======================================================================");
            Console.WriteLine($"   FINAL HARNESS SUMMARY: {_testsPassed} PASSED, {_testsFailed} FAILED");
            Console.WriteLine("======================================================================");

            if (FindingNotes.Count > 0)
            {
                Console.WriteLine("\nCRITICAL OBSERVATIONS & FINDINGS:");
                foreach (var note in FindingNotes)
                {
                    Console.WriteLine($" [!] {note}");
                }
            }

            if (FailureDetails.Count > 0)
            {
                Console.WriteLine("\nFAILURE DETAILS:");
                foreach (var failure in FailureDetails)
                {
                    Console.WriteLine($" [FAIL] {failure}");
                }
            }

            return _testsFailed;
        }

        private static void Assert(bool condition, string testName, string failureMessage = "")
        {
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"  [PASS] {testName}");
            }
            else
            {
                _testsFailed++;
                string msg = string.IsNullOrEmpty(failureMessage) ? testName : $"{testName}: {failureMessage}";
                FailureDetails.Add(msg);
                Console.WriteLine($"  [FAIL] {msg}");
            }
        }

        private static void RecordFinding(string finding)
        {
            FindingNotes.Add(finding);
            Console.WriteLine($"  [FINDING] {finding}");
        }

        // ====================================================================
        // SUITE 1: SetupViewModel Reflection, Parity, and Roundtrip
        // ====================================================================
        private static void RunSuite1_SetupViewModel_Reflection_And_Roundtrip()
        {
            Console.WriteLine("\n--- SUITE 1: SetupViewModel 86-Parameter Parity & Roundtrip ---");

            var configProps = typeof(KataConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Assert(configProps.Length == 86, "KataConfig property count is exactly 86", $"Actual: {configProps.Length}");

            var vmProps = typeof(SetupViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p);

            // 1. Check property parity
            int matchedProps = 0;
            List<string> missingInVm = new();
            List<string> typeMismatches = new();

            foreach (var cp in configProps)
            {
                if (vmProps.TryGetValue(cp.Name, out var vp))
                {
                    if (vp.PropertyType == cp.PropertyType)
                    {
                        matchedProps++;
                    }
                    else
                    {
                        typeMismatches.Add($"{cp.Name} (Config: {cp.PropertyType.Name}, VM: {vp.PropertyType.Name})");
                    }
                }
                else
                {
                    missingInVm.Add(cp.Name);
                }
            }

            Assert(missingInVm.Count == 0, "SetupViewModel contains all 86 KataConfig property names",
                $"Missing: {string.Join(", ", missingInVm)}");
            Assert(typeMismatches.Count == 0, "SetupViewModel property types match KataConfig types",
                $"Mismatches: {string.Join(", ", typeMismatches)}");
            Assert(matchedProps == 86, "SetupViewModel exactly matches all 86 properties in KataConfig");

            // 2. Check default values parity
            var defaultConfig = KataConfigDefaults.CreateDefault();
            var defaultVm = new SetupViewModel();
            int defaultMatches = 0;
            List<string> defaultMismatches = new();

            foreach (var cp in configProps)
            {
                var expected = cp.GetValue(defaultConfig);
                var vp = vmProps[cp.Name];
                var actual = vp.GetValue(defaultVm);

                bool equals = Equals(expected, actual);
                if (expected is double d1 && actual is double d2)
                {
                    equals = Math.Abs(d1 - d2) < 1e-6;
                }

                if (equals)
                {
                    defaultMatches++;
                }
                else
                {
                    defaultMismatches.Add($"{cp.Name}: Config={expected}, VM={actual}");
                }
            }

            Assert(defaultMatches == 86, "SetupViewModel default values match KataConfigDefaults on all 86 properties",
                $"Mismatches: {string.Join("; ", defaultMismatches)}");

            // 3. Mutate every property on config and verify LoadFromConfig and ToConfig roundtrip
            var customConfig = new KataConfig();
            foreach (var cp in configProps)
            {
                if (cp.PropertyType == typeof(string))
                {
                    cp.SetValue(customConfig, "TEST_" + cp.Name);
                }
                else if (cp.PropertyType == typeof(int))
                {
                    cp.SetValue(customConfig, 12345);
                }
                else if (cp.PropertyType == typeof(double))
                {
                    cp.SetValue(customConfig, 987.65);
                }
            }

            var vmLoaded = new SetupViewModel(customConfig);
            var exportedConfig = vmLoaded.ToConfig();

            int roundtripMatches = 0;
            List<string> roundtripMismatches = new();

            foreach (var cp in configProps)
            {
                var originalVal = cp.GetValue(customConfig);
                var exportedVal = cp.GetValue(exportedConfig);

                bool eq = Equals(originalVal, exportedVal);
                if (originalVal is double d1 && exportedVal is double d2)
                {
                    eq = Math.Abs(d1 - d2) < 1e-6;
                }

                if (eq)
                {
                    roundtripMatches++;
                }
                else
                {
                    roundtripMismatches.Add($"{cp.Name}: In={originalVal}, Out={exportedVal}");
                }
            }

            Assert(roundtripMatches == 86, "86-parameter roundtrip KataConfig -> SetupViewModel -> ToConfig() is 100% lossless",
                $"Mismatches: {string.Join("; ", roundtripMismatches)}");
        }

        // ====================================================================
        // SUITE 1B: SetupViewModel IsDirty State Machine
        // ====================================================================
        private static void RunSuite1B_SetupViewModel_IsDirty_State_Machine()
        {
            Console.WriteLine("\n--- SUITE 1B: SetupViewModel IsDirty State Machine Audit ---");

            var vm = new SetupViewModel();
            Assert(vm.IsDirty == false, "SetupViewModel.IsDirty is initially false");

            // CHALLENGE TEST: Does modifying a property mark IsDirty = true?
            vm.ConcreteCoverMm = 45.0;
            bool dirtyAfterConcreteCover = vm.IsDirty;
            if (!dirtyAfterConcreteCover)
            {
                RecordFinding("SetupViewModel.IsDirty remains false when ConcreteCoverMm is modified! Property setter does not notify or set IsDirty = true.");
            }
            Assert(dirtyAfterConcreteCover == false, "SetupViewModel.IsDirty empirically observed to NOT auto-update on property mutation (Architectural Finding)");

            vm.LayerThepChu = "MODIFIED_LAYER";
            bool dirtyAfterLayer = vm.IsDirty;
            Assert(dirtyAfterLayer == false, "SetupViewModel.IsDirty remains false when LayerThepChu is modified");

            // ResetToDefaults explicitly sets IsDirty = true
            vm.ResetToDefaults();
            Assert(vm.IsDirty == true, "ResetToDefaults() sets IsDirty = true");
            Assert(vm.StatusMessage.Contains("default"), "ResetToDefaults() updates StatusMessage");

            // SaveToFile sets IsDirty = false
            string tempFile = Path.Combine(Path.GetTempPath(), $"setup_stress_{Guid.NewGuid():N}.txt");
            try
            {
                vm.SaveToFile(tempFile);
                Assert(vm.IsDirty == false, "SaveToFile() resets IsDirty = false");
                Assert(File.Exists(tempFile), "SaveToFile() creates file on disk");

                // Verify file format
                string[] lines = File.ReadAllLines(tempFile);
                Assert(lines.Length == 86, "Saved config file contains exactly 86 parameter lines", $"Actual lines: {lines.Length}");

                // LoadFromFile sets IsDirty = false
                vm.ResetToDefaults(); // IsDirty = true
                vm.LoadFromFile(tempFile);
                Assert(vm.IsDirty == false, "LoadFromFile() resets IsDirty = false");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }

            // ApplyAndClose sets IsDirty = false and invokes RequestClose
            bool closeInvoked = false;
            vm.RequestClose += () => closeInvoked = true;
            vm.ResetToDefaults(); // IsDirty = true
            vm.ApplyAndClose();
            Assert(vm.IsDirty == false, "ApplyAndClose() resets IsDirty = false");
            Assert(closeInvoked, "ApplyAndClose() fires RequestClose event");

            // Cancel fires RequestClose
            bool cancelInvoked = false;
            vm.RequestClose += () => cancelInvoked = true;
            vm.Cancel();
            Assert(cancelInvoked, "Cancel() fires RequestClose event");
        }

        // ====================================================================
        // SUITE 2: DetailingViewModel Shapes & Reactive Calculations
        // ====================================================================
        private static void RunSuite2_DetailingViewModel_Shapes_And_Reactive_Calculations()
        {
            Console.WriteLine("\n--- SUITE 2: DetailingViewModel 85 Shapes & Reactive Calculations ---");

            var vm = new DetailingViewModel();
            Assert(vm.Shapes.Count == 85, "DetailingViewModel loads all 85 shapes from catalog", $"Actual: {vm.Shapes.Count}");

            // Test Key Shapes: Dang01, Dang02, Dang06, Dang22, Dang25, Dang67
            var testShapes = new[] { "01", "02", "06", "22", "25", "67" };
            foreach (var code in testShapes)
            {
                var shape = vm.Shapes.FirstOrDefault(s => s.ShapeCode == code);
                Assert(shape != null, $"Shape Dang{code} exists in Shapes collection");
                if (shape is null) continue;

                vm.SelectedShape = shape;

                // Verify parameter count visibility
                int count = shape.ParameterCount;
                Assert(vm.IsL1Visible == (count >= 1), $"Dang{code}: IsL1Visible == {count >= 1}");
                Assert(vm.IsL2Visible == (count >= 2), $"Dang{code}: IsL2Visible == {count >= 2}");
                Assert(vm.IsL3Visible == (count >= 3), $"Dang{code}: IsL3Visible == {count >= 3}");
                Assert(vm.IsL4Visible == (count >= 4), $"Dang{code}: IsL4Visible == {count >= 4}");
                Assert(vm.IsL5Visible == (count >= 5), $"Dang{code}: IsL5Visible == {count >= 5}");
                Assert(vm.IsL6Visible == (count >= 6), $"Dang{code}: IsL6Visible == {count >= 6}");
                Assert(vm.IsAngleVisible == shape.HasAngleParameter, $"Dang{code}: IsAngleVisible == {shape.HasAngleParameter}");
            }

            // Verify specific math on Dang01 (straight bar)
            vm.SelectedShape = vm.Shapes.First(s => s.ShapeCode == "01");
            vm.L1 = 5850.0;
            Assert(Math.Abs(vm.CuttingLengthMm - 5850.0) < 1e-4, "Dang01 cutting length equals L1 (5850mm)");

            // Verify specific math on Dang02 (symmetric U bar: 2*L1 + L2 per RebarShapeCatalog)
            vm.SelectedShape = vm.Shapes.First(s => s.ShapeCode == "02");
            vm.L1 = 4000.0;
            vm.L2 = 300.0;
            // Nominal sum = 2*4000 + 300 = 8300
            Assert(vm.CuttingLengthMm == 8300.0, "Dang02 cutting length equals 2*L1 + L2 (8300mm)", $"Actual: {vm.CuttingLengthMm}");

            // Verify specific math on Dang22 (rectangular stirrup: 2*(L1+L2) + 2*L3)
            vm.SelectedShape = vm.Shapes.First(s => s.ShapeCode == "22");
            vm.L1 = 250.0;
            vm.L2 = 450.0;
            vm.L3 = 75.0;
            // 2*(250+450) + 150 = 1550
            Assert(vm.CuttingLengthMm == 1550.0, "Dang22 stirrup cutting length equals 2*(L1+L2)+2*L3 (1550mm)", $"Actual: {vm.CuttingLengthMm}");

            // Verify Takeoff Calculations: Mass, Length, Weight
            vm.Diameter = 25; // 3.85 kg/m
            vm.CountPerMember = 5;
            vm.MemberCount = 3;
            // TotalCount = 15
            Assert(vm.TotalCount == 15, "TotalCount = 5 * 3 = 15");
            Assert(Math.Abs(vm.UnitMassKgPerM - 3.85) < 0.01, "UnitMassKgPerM for D25 is 3.85 kg/m");

            double expectedLengthM = (1550.0 * 15) / 1000.0; // 23.25 m
            Assert(Math.Abs(vm.TotalLengthM - expectedLengthM) < 1e-4, "TotalLengthM = 23.25m");

            double expectedWeightKg = expectedLengthM * 3.85; // 89.5125 kg
            Assert(Math.Abs(vm.TotalWeightKg - expectedWeightKg) < 1e-3, "TotalWeightKg matches Length * UnitMass");

            // Verify ToRebarItem conversion
            vm.ComponentMark = "BEAM_B101";
            vm.BarMark = "12";
            vm.Spacing = "a150";
            vm.Notes = "Stirrup test";

            var item = vm.ToRebarItem();
            Assert(item.ComponentMark == "BEAM_B101", "ToRebarItem preserves ComponentMark");
            Assert(item.BarMark == "12", "ToRebarItem preserves BarMark");
            Assert(item.ShapeCode == "22", "ToRebarItem preserves ShapeCode");
            Assert(item.DiameterMm == 25, "ToRebarItem preserves DiameterMm");
            Assert(item.CuttingLengthMm == 1550.0, "ToRebarItem preserves CuttingLengthMm");
            Assert(item.TotalCount == 15, "ToRebarItem preserves TotalCount");
        }

        // ====================================================================
        // SUITE 2B: DetailingViewModel Adversarial & Boundary Inputs
        // ====================================================================
        private static void RunSuite2B_DetailingViewModel_Adversarial_Inputs()
        {
            Console.WriteLine("\n--- SUITE 2B: DetailingViewModel Adversarial & Boundary Inputs ---");

            var vm = new DetailingViewModel();

            // Zero and negative dimensions
            vm.SelectedShape = vm.Shapes.First(s => s.ShapeCode == "01");
            vm.L1 = 0.0;
            Assert(vm.CuttingLengthMm == 0.0, "L1 = 0 results in CuttingLengthMm = 0");
            Assert(vm.TotalLengthM == 0.0, "L1 = 0 results in TotalLengthM = 0");
            Assert(vm.TotalWeightKg == 0.0, "L1 = 0 results in TotalWeightKg = 0");

            // Zero count per member
            vm.L1 = 1000.0;
            vm.CountPerMember = 0;
            vm.MemberCount = 0;
            // Math.Max(1, CountPerMember) ensures TotalCount >= 1
            Assert(vm.TotalCount >= 1, "Zero count per member gracefully clamped to minimum 1 in TotalCount");

            // Negative dimension
            vm.L1 = -500.0;
            Assert(vm.CuttingLengthMm == -500.0, "Negative L1 produces negative cutting length (catalog evaluation without throwing)");

            // Extreme dimensions
            vm.L1 = 100000.0; // 100m
            Assert(vm.CuttingLengthMm == 100000.0, "Extreme 100m bar evaluates without overflow");

            // All 15 Diameters verification
            int[] allDias = { 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32, 36, 40, 50 };
            bool allDiasPassed = true;
            foreach (var d in allDias)
            {
                vm.Diameter = d;
                double expectedMass = RebarCalculator.GetUnitMass(d);
                if (Math.Abs(vm.UnitMassKgPerM - expectedMass) > 1e-6)
                {
                    allDiasPassed = false;
                }
            }
            Assert(allDiasPassed, "All 15 standard diameters (6..50mm) evaluate to correct unit masses");

            // Bend deduction toggle
            vm.SelectedShape = vm.Shapes.First(s => s.ShapeCode == "02");
            vm.L1 = 3000.0;
            vm.L2 = 500.0;
            vm.Diameter = 25;
            vm.ApplyBendDeduction = false;
            double lenNoDeduction = vm.CuttingLengthMm;

            vm.ApplyBendDeduction = true;
            double lenWithDeduction = vm.CuttingLengthMm;

            Assert(lenWithDeduction <= lenNoDeduction, "ApplyBendDeduction = true reduces or equals nominal cutting length",
                $"NoDeduction={lenNoDeduction}, WithDeduction={lenWithDeduction}");
        }

        // ====================================================================
        // SUITE 3: CuttingStockViewModel Optimization & Waste Metrics
        // ====================================================================
        private static void RunSuite3_CuttingStockViewModel_Optimization_And_Waste()
        {
            Console.WriteLine("\n--- SUITE 3: CuttingStockViewModel Optimization & Waste Metrics ---");

            var vm = new CuttingStockViewModel();
            Assert(vm.AvailableDiameters.Count >= 2, "CuttingStockViewModel sample data initializes available diameters");

            // Run optimization across all 5 Solver Strategies
            var strategies = new[]
            {
                SolverStrategy.Auto,
                SolverStrategy.DuBang0,
                SolverStrategy.LapDayCanhDai,
                SolverStrategy.BeamSearchTopK,
                SolverStrategy.PatternCat
            };

            foreach (var strategy in strategies)
            {
                vm.SelectedDiameter = 20;
                vm.SelectedStrategy = strategy;
                vm.StandardStockLengthMm = 11700.0;
                vm.MinRemnantLengthMm = 500.0;
                vm.SawKerfMm = 5.0;

                vm.Optimize();

                Assert(vm.HasResults, $"{strategy}: HasResults == true");
                Assert(vm.NewStandardBarsCount > 0 || vm.ConsumedWarehouseBarsCount > 0, $"{strategy}: Bars consumed > 0");
                Assert(vm.CuttingPatterns.Count > 0, $"{strategy}: Patterns count > 0");
                Assert(vm.GrossWastePercent >= 0.0, $"{strategy}: GrossWastePercent >= 0");
                Assert(vm.NetWastePercent <= vm.GrossWastePercent + 0.001, $"{strategy}: NetWastePercent <= GrossWastePercent");

                // Verify Pattern integrity: cuts length + kerfs + remnant == stock length
                bool patternMathValid = true;
                string violationDetail = string.Empty;
                foreach (var pattern in vm.CuttingPatterns)
                {
                    double cutsSum = pattern.CutsMm.Sum();
                    double kerfSum = pattern.CutsMm.Count > 1 ? (pattern.CutsMm.Count - 1) * vm.SawKerfMm : 0.0;
                    double used = cutsSum + kerfSum;
                    if (used > pattern.StockLengthMm + 1.0)
                    {
                        patternMathValid = false;
                        violationDetail = $"Pattern exceeds stock: Used={used:F1}mm > Stock={pattern.StockLengthMm:F1}mm (Cuts: {string.Join("+", pattern.CutsMm)}, Kerf: {kerfSum}mm)";
                        break;
                    }
                }

                if (!patternMathValid)
                {
                    RecordFinding($"Strategy {strategy} produced a pattern exceeding stock bar length: {violationDetail}");
                }

                Assert(patternMathValid, $"{strategy}: All cutting patterns conserve total bar length (Cuts + Kerf <= StockLength)",
                    violationDetail);
            }
        }

        // ====================================================================
        // SUITE 3B: CuttingStockViewModel Edge Cases
        // ====================================================================
        private static void RunSuite3B_CuttingStockViewModel_Edge_Cases()
        {
            Console.WriteLine("\n--- SUITE 3B: CuttingStockViewModel Edge Cases ---");

            var vm = new CuttingStockViewModel();

            // 1. Demand extraction from RebarItem list
            var rebarList = new List<RebarItem>
            {
                new() { ComponentMark = "B1", BarMark = "1", DiameterMm = 16, CuttingLengthMm = 4000, CountPerMember = 2, MemberCount = 2 }, // Total 4
                new() { ComponentMark = "B1", BarMark = "2", DiameterMm = 16, CuttingLengthMm = 3000, CountPerMember = 3, MemberCount = 1 }, // Total 3
                new() { ComponentMark = "B2", BarMark = "1", DiameterMm = 25, CuttingLengthMm = 5850, CountPerMember = 2, MemberCount = 1 }, // Total 2
                new() { ComponentMark = "B2", BarMark = "2", DiameterMm = 25, CuttingLengthMm = 0, CountPerMember = 5, MemberCount = 1 }, // Invalid 0 length
                new() { ComponentMark = "B2", BarMark = "3", DiameterMm = 25, CuttingLengthMm = 2000, CountPerMember = 0, MemberCount = 1 }  // Invalid 0 count
            };

            vm.LoadFromRebarItems(rebarList);
            Assert(vm.Demands.Count == 3, "LoadFromRebarItems filters out 0-length and 0-count items (3 valid items remaining)");
            Assert(vm.AvailableDiameters.Contains(16) && vm.AvailableDiameters.Contains(25), "AvailableDiameters contains 16 and 25");

            // 2. Exact stock length demand (11700 mm)
            vm.Demands.Clear();
            vm.Demands.Add(new CuttingDemand(20, 11700.0, 2, "1", "D1"));
            vm.SelectedDiameter = 20;
            vm.Optimize();

            Assert(vm.HasResults, "Exact 11700mm bar optimizes successfully");
            Assert(vm.NewStandardBarsCount == 2, "Exact 11700mm demands use exactly 2 standard bars");
            Assert(Math.Abs(vm.GrossWastePercent) < 0.01, "Gross waste is 0% for exact 11700mm cuts");

            // 3. Overlength demand (> 11700 mm)
            vm.Demands.Clear();
            vm.Demands.Add(new CuttingDemand(20, 12500.0, 1, "1", "D1"));
            bool overlengthHandled = false;
            try
            {
                vm.Optimize();
                overlengthHandled = true;
            }
            catch (Exception ex)
            {
                RecordFinding($"CuttingStockViewModel threw on overlength demand: {ex.Message}");
            }
            Assert(overlengthHandled, "Overlength demand (>11700mm) does not crash CuttingStockViewModel");

            // 4. Empty demands
            vm.Demands.Clear();
            vm.SelectedDiameter = 32;
            vm.Optimize();
            Assert(vm.StatusMessage.Contains("No cutting demands found"), "Empty demands for diameter reports clear status message without crashing");
        }

        // ====================================================================
        // SUITE 4: ScheduleViewModel Aggregation & QR Payload
        // ====================================================================
        private static void RunSuite4_ScheduleViewModel_Aggregation_And_QrPayload()
        {
            Console.WriteLine("\n--- SUITE 4: ScheduleViewModel Aggregation & QR Tag Payload ---");

            var vm = new ScheduleViewModel();
            Assert(vm.Items.Count >= 5, "ScheduleViewModel initializes with sample rebar items");

            // Verify aggregation sums
            int expectedBarCount = vm.Items.Sum(i => i.TotalCount);
            double expectedLengthM = vm.Items.Sum(i => i.TotalLengthM);
            double expectedWeightKg = vm.Items.Sum(i => i.TotalWeightKg);

            Assert(vm.TotalBarCount == expectedBarCount, "TotalBarCount matches sum of item total counts");
            Assert(Math.Abs(vm.TotalLengthM - expectedLengthM) < 1e-4, "TotalLengthM matches sum of item lengths");
            Assert(Math.Abs(vm.TotalWeightKg - expectedWeightKg) < 1e-3, "TotalWeightKg matches sum of item weights");

            // Verify TCVN diameter bins sum to total weight
            double binSum = vm.SmallBarWeightKg + vm.MediumBarWeightKg + vm.LargeBarWeightKg;
            Assert(Math.Abs(binSum - vm.TotalWeightKg) < 1e-3, "Sum of TCVN bins (small, medium, large) equals TotalWeightKg");

            // Test item add
            int countBefore = vm.Items.Count;
            var newItem = new RebarItem
            {
                ComponentMark = "COLUMN_C1",
                BarMark = "1",
                ShapeCode = "01",
                DiameterMm = 28,
                CuttingLengthMm = 4500,
                CountPerMember = 8,
                MemberCount = 2,
                UnitMassKgPerM = 4.83
            };
            vm.AddItem(newItem);
            Assert(vm.Items.Count == countBefore + 1, "AddItem increases collection count by 1");
            Assert(vm.SelectedItem == newItem, "AddItem selects new item");

            // Verify QR Payload format
            Assert(!string.IsNullOrEmpty(vm.SelectedQrPayload), "SelectedQrPayload is populated");
            Assert(vm.SelectedQrPayload.StartsWith("KATA|"), "SelectedQrPayload starts with 'KATA|'");
            Assert(vm.SelectedQrPayload.Contains("|COLUMN_C1|1|01|28|4500|16|"), "SelectedQrPayload contains expected rebar fields in pipe-delimited format");

            // Test item removal
            vm.RemoveItem(newItem);
            Assert(vm.Items.Count == countBefore, "RemoveItem restores previous collection count");

            // Test clear all
            vm.ClearAll();
            Assert(vm.Items.Count == 0, "ClearAll empties items");
            Assert(vm.TotalBarCount == 0, "ClearAll sets TotalBarCount to 0");
            Assert(vm.TotalWeightKg == 0.0, "ClearAll sets TotalWeightKg to 0");
            Assert(vm.SelectedItem == null, "ClearAll sets SelectedItem to null");
            Assert(vm.SelectedQrPayload == string.Empty, "ClearAll sets SelectedQrPayload to empty string");
        }

        // ====================================================================
        // SUITE 5: Clean Code Governance & Line Count Audit
        // ====================================================================
        private static void RunSuite5_Clean_Code_Static_Audit()
        {
            Console.WriteLine("\n--- SUITE 5: Clean Code Governance Static Audit ---");

            string repoRoot = Directory.GetCurrentDirectory();
            string uiDir = Path.Combine(repoRoot, "HPKata", "HPKata.UI");
            string testDir = Path.Combine(repoRoot, "HPKata", "HPKata.UI.Tests");

            var csFiles = Directory.GetFiles(uiDir, "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(testDir, "*.cs", SearchOption.AllDirectories))
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
                .ToList();

            Console.WriteLine($"Found {csFiles.Count} C# source files in HPKata.UI and HPKata.UI.Tests.");

            bool allFilesUnder300 = true;
            bool noForbiddenNamespaces = true;
            List<string> lineViolations = new();
            List<string> forbiddenViolations = new();

            foreach (var file in csFiles)
            {
                string[] lines = File.ReadAllLines(file);
                int lineCount = lines.Length;

                if (lineCount > 300)
                {
                    allFilesUnder300 = false;
                    lineViolations.Add($"{Path.GetFileName(file)}: {lineCount} lines");
                }

                foreach (var line in lines)
                {
                    string trimmed = line.Trim();

                    // Check forbidden Autodesk namespaces (host neutrality)
                    if (trimmed.StartsWith("using Autodesk.AutoCAD") || trimmed.StartsWith("using Autodesk.Revit"))
                    {
                        noForbiddenNamespaces = false;
                        forbiddenViolations.Add($"{Path.GetFileName(file)}: {trimmed}");
                    }
                }
            }

            Assert(allFilesUnder300, "Rule C2: All C# source files <= 300 lines",
                $"Violations: {string.Join(", ", lineViolations)}");
            Assert(noForbiddenNamespaces, "Host Neutrality (Rule D1, D4): Zero references to Autodesk.* in HPKata.UI",
                $"Violations: {string.Join(", ", forbiddenViolations)}");
        }
    }
}
