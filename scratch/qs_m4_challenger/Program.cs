using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using HPKata.Core.QuantitySurveying.Calculators;
using HPKata.Core.QuantitySurveying.Models;
using HPKata.Core.QuantitySurveying.Persistence;

namespace HPKata.QuantitySurveying.EmpiricalHarness
{
    public static class Program
    {
        private static int _passed;
        private static int _failed;
        private static readonly List<string> Failures = new();
        private static readonly List<string> CriticalFindings = new();

        public static int Main()
        {
            Console.WriteLine("======================================================================");
            Console.WriteLine("   HPKATA MILESTONE M4 QUANTITY SURVEYING EMPIRICAL STRESS HARNESS    ");
            Console.WriteLine("======================================================================");

            RunSuite1_PriorityDeductionHierarchy();
            RunSuite2_MultiMemberIntersections();
            RunSuite3_DegenerateAndAdversarialDimensions();
            RunSuite4_FormworkCalculationsAndContactDeductions();
            RunSuite5_ClosedXmlWorkbookGeneration();
            RunSuite6_LargeScaleAndPerformanceStress();
            RunSuite7_NegativeDimensionsDefectDemonstration();

            Console.WriteLine("\n======================================================================");
            Console.WriteLine($"   FINAL HARNESS SUMMARY: {_passed} PASSED, {_failed} FAILED");
            Console.WriteLine("======================================================================");

            if (CriticalFindings.Count > 0)
            {
                Console.WriteLine("\nCRITICAL OBSERVATIONS & FINDINGS:");
                foreach (var f in CriticalFindings)
                {
                    Console.WriteLine($" [!] {f}");
                }
            }

            if (Failures.Count > 0)
            {
                Console.WriteLine("\nFAILURES:");
                foreach (var fail in Failures)
                {
                    Console.WriteLine($" [FAIL] {fail}");
                }
            }

            return _failed;
        }

        private static void AssertTrue(bool condition, string testName, string details = "")
        {
            if (condition)
            {
                _passed++;
                Console.WriteLine($" [PASS] {testName}");
            }
            else
            {
                _failed++;
                string msg = string.IsNullOrEmpty(details) ? testName : $"{testName} - {details}";
                Failures.Add(msg);
                Console.WriteLine($" [FAIL] {msg}");
            }
        }

        private static void AssertEqual(double expected, double actual, double tolerance, string testName)
        {
            bool match = Math.Abs(expected - actual) <= tolerance;
            if (match)
            {
                _passed++;
                Console.WriteLine($" [PASS] {testName} (Expected: {expected}, Actual: {actual})");
            }
            else
            {
                _failed++;
                string msg = $"{testName}: Expected {expected} ± {tolerance}, got {actual}";
                Failures.Add(msg);
                Console.WriteLine($" [FAIL] {msg}");
            }
        }

        private static void AssertFormula(string expectedFormulaWithoutEq, string actualFormula, string testName)
        {
            string normalizedActual = actualFormula.TrimStart('=');
            string normalizedExpected = expectedFormulaWithoutEq.TrimStart('=');
            if (normalizedActual.Equals(normalizedExpected, StringComparison.OrdinalIgnoreCase))
            {
                _passed++;
                Console.WriteLine($" [PASS] {testName} ('{actualFormula}')");
            }
            else
            {
                _failed++;
                string msg = $"{testName}: Expected formula '{normalizedExpected}', got '{actualFormula}'";
                Failures.Add(msg);
                Console.WriteLine($" [FAIL] {msg}");
            }
        }

        // ====================================================================
        // SUITE 1: PRIORITY DEDUCTION HIERARCHY
        // ====================================================================
        private static void RunSuite1_PriorityDeductionHierarchy()
        {
            Console.WriteLine("\n--- SUITE 1: PRIORITY DEDUCTION HIERARCHY ---");

            // 1.1 Footing > Column
            {
                var footing = new QsElementModel
                {
                    Id = "F1", Mark = "F1", MemberType = QsMemberType.Footing,
                    LengthMm = 2000, WidthMm = 2000, HeightMm = 800
                };
                var column = new QsElementModel
                {
                    Id = "C1", Mark = "C1", MemberType = QsMemberType.Column,
                    WidthMm = 400, HeightMm = 400, LengthMm = 3600,
                    EmbedmentDepthMm = 300, IntersectingElementIds = new List<string> { "F1" }
                };

                var res = QsIntersectionDeductionEngine.Evaluate(new[] { footing, column });
                AssertEqual(footing.LengthMm * footing.WidthMm * footing.HeightMm * 1e-9, res["F1"].NetVolumeM3, 1e-6, "Footing volume 100% undeducted by Column");
                AssertTrue(res["C1"].Deductions.Count == 1, "Column has exactly 1 deduction for Footing");
                double expectedColDeduction = 0.4 * 0.4 * 0.3; // 0.048 m3
                AssertEqual(expectedColDeduction, res["C1"].DeductedVolumeM3, 1e-5, "Column embedment deduction exactness (0.048 m3)");
                AssertEqual(res["C1"].GrossVolumeM3 - expectedColDeduction, res["C1"].NetVolumeM3, 1e-5, "Column net volume == gross - deduction");
            }

            // 1.2 Column > Beam
            {
                var col = new QsElementModel
                {
                    Id = "C1", Mark = "C1", MemberType = QsMemberType.Column,
                    WidthMm = 500, HeightMm = 500, LengthMm = 4000
                };
                var beam = new QsElementModel
                {
                    Id = "B1", Mark = "B1", MemberType = QsMemberType.Beam,
                    WidthMm = 300, HeightMm = 600, LengthMm = 6000,
                    IntersectingElementIds = new List<string> { "C1" }
                };

                var res = QsIntersectionDeductionEngine.Evaluate(new[] { col, beam });
                AssertEqual(res["C1"].GrossVolumeM3, res["C1"].NetVolumeM3, 1e-6, "Column volume 100% undeducted through Beam joint");
                double expectedBeamDeduction = 0.3 * 0.6 * (0.5 * 0.5); // 0.045 m3
                AssertEqual(expectedBeamDeduction, res["B1"].DeductedVolumeM3, 1e-5, "Beam deducts half-column joint overlap (0.045 m3)");
                AssertEqual(res["B1"].GrossVolumeM3 - expectedBeamDeduction, res["B1"].NetVolumeM3, 1e-5, "Beam net volume == gross - deduction");
            }

            // 1.3 Wall > Beam
            {
                var wall = new QsElementModel
                {
                    Id = "W1", Mark = "W1", MemberType = QsMemberType.Wall,
                    LengthMm = 6000, ThicknessMm = 300, HeightMm = 3600
                };
                var beam = new QsElementModel
                {
                    Id = "B1", Mark = "B1", MemberType = QsMemberType.Beam,
                    WidthMm = 250, HeightMm = 500, LengthMm = 4000,
                    IntersectingElementIds = new List<string> { "W1" }
                };

                var res = QsIntersectionDeductionEngine.Evaluate(new[] { wall, beam });
                AssertEqual(res["W1"].GrossVolumeM3, res["W1"].NetVolumeM3, 1e-6, "Wall volume 100% undeducted by Beam");
                AssertTrue(res["B1"].Deductions.Count == 1, "Beam has 1 deduction for Wall");
                AssertTrue(res["B1"].DeductedVolumeM3 > 0, "Beam deducts Wall intersection overlap");
            }

            // 1.4 Primary Beam > Secondary Beam
            {
                var pb = new QsElementModel
                {
                    Id = "PB1", Mark = "PB1", MemberType = QsMemberType.Beam, IsPrimary = true,
                    WidthMm = 350, HeightMm = 700, LengthMm = 8000
                };
                var sb = new QsElementModel
                {
                    Id = "SB1", Mark = "SB1", MemberType = QsMemberType.Beam, IsPrimary = false,
                    WidthMm = 200, HeightMm = 400, LengthMm = 5000,
                    IntersectingElementIds = new List<string> { "PB1" }
                };

                var res = QsIntersectionDeductionEngine.Evaluate(new[] { pb, sb });
                AssertEqual(res["PB1"].GrossVolumeM3, res["PB1"].NetVolumeM3, 1e-6, "Primary Beam volume 100% undeducted by Secondary Beam");
                double expectedSbDeduction = 0.2 * 0.4 * 0.35; // 0.028 m3
                AssertEqual(expectedSbDeduction, res["SB1"].DeductedVolumeM3, 1e-5, "Secondary Beam deducts Primary Beam width intersection (0.028 m3)");
                AssertEqual(res["SB1"].GrossVolumeM3 - expectedSbDeduction, res["SB1"].NetVolumeM3, 1e-5, "Secondary Beam net volume == gross - deduction");
            }

            // 1.5 Beam / Column / Wall > Slab
            {
                var col = new QsElementModel { Id = "C1", MemberType = QsMemberType.Column, WidthMm = 400, HeightMm = 400, LengthMm = 3500 };
                var beam = new QsElementModel { Id = "B1", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 5000 };
                var wall = new QsElementModel { Id = "W1", MemberType = QsMemberType.Wall, LengthMm = 4000, ThicknessMm = 200, HeightMm = 3500 };
                var slab = new QsElementModel
                {
                    Id = "S1", MemberType = QsMemberType.Slab,
                    LengthMm = 6000, WidthMm = 6000, ThicknessMm = 120,
                    IntersectingElementIds = new List<string> { "C1", "B1", "W1" }
                };

                var res = QsIntersectionDeductionEngine.Evaluate(new[] { col, beam, wall, slab });
                AssertEqual(res["C1"].GrossVolumeM3, res["C1"].NetVolumeM3, 1e-6, "Column undeducted by Slab");
                AssertEqual(res["B1"].GrossVolumeM3, res["B1"].NetVolumeM3, 1e-6, "Beam undeducted by Slab");
                AssertEqual(res["W1"].GrossVolumeM3, res["W1"].NetVolumeM3, 1e-6, "Wall undeducted by Slab");

                double expectedColInSlab = 0.4 * 0.4 * 0.12;   // 0.0192 m3
                double expectedBeamInSlab = 0.25 * 5.0 * 0.12;  // 0.1500 m3
                double expectedWallInSlab = 0.2 * 4.0 * 0.12;   // 0.0960 m3
                double totalExpectedSlabDeduction = expectedColInSlab + expectedBeamInSlab + expectedWallInSlab; // 0.2652 m3

                AssertEqual(totalExpectedSlabDeduction, res["S1"].DeductedVolumeM3, 1e-5, "Slab deducts Column, Beam stem, and Wall thickness (0.2652 m3)");
                AssertEqual(res["S1"].GrossVolumeM3 - totalExpectedSlabDeduction, res["S1"].NetVolumeM3, 1e-5, "Slab net volume == gross - deductions");
            }
        }

        // ====================================================================
        // SUITE 2: MULTI-MEMBER INTERSECTIONS
        // ====================================================================
        private static void RunSuite2_MultiMemberIntersections()
        {
            Console.WriteLine("\n--- SUITE 2: MULTI-MEMBER INTERSECTIONS ---");

            var col = new QsElementModel { Id = "C1", Mark = "C1", MemberType = QsMemberType.Column, WidthMm = 400, HeightMm = 400, LengthMm = 3500 };
            var bN = new QsElementModel { Id = "BN", Mark = "B_North", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 6000, IntersectingElementIds = new List<string> { "C1" } };
            var bS = new QsElementModel { Id = "BS", Mark = "B_South", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 6000, IntersectingElementIds = new List<string> { "C1" } };
            var bE = new QsElementModel { Id = "BE", Mark = "B_East", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 6000, IntersectingElementIds = new List<string> { "C1" } };
            var bW = new QsElementModel { Id = "BW", Mark = "B_West", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 6000, IntersectingElementIds = new List<string> { "C1" } };
            var slab = new QsElementModel { Id = "S1", Mark = "S1", MemberType = QsMemberType.Slab, LengthMm = 12000, WidthMm = 12000, ThicknessMm = 120, IntersectingElementIds = new List<string> { "C1", "BN", "BS", "BE", "BW" } };

            var elements = new[] { col, bN, bS, bE, bW, slab };
            var res = QsIntersectionDeductionEngine.Evaluate(elements);

            AssertEqual(res["C1"].GrossVolumeM3, res["C1"].NetVolumeM3, 1e-6, "4-beam joint: Column volume 100% undeducted (0.560 m3)");
            AssertEqual(0.0, res["C1"].DeductedVolumeM3, 1e-6, "4-beam joint: Column deducted volume is 0");

            double expectedBeamDeduction = 0.25 * 0.5 * 0.2; // 0.025 m3
            foreach (var bId in new[] { "BN", "BS", "BE", "BW" })
            {
                AssertEqual(expectedBeamDeduction, res[bId].DeductedVolumeM3, 1e-5, $"{bId} deducts column overlap (0.025 m3)");
                AssertEqual(0.75 - expectedBeamDeduction, res[bId].NetVolumeM3, 1e-5, $"{bId} net volume == 0.725 m3");
                AssertTrue(res[bId].NetVolumeM3 > 0, $"{bId} net volume is strictly positive");
            }

            double expectedSlabDeduction = 0.0192 + 0.72; // 0.7392 m3
            AssertEqual(expectedSlabDeduction, res["S1"].DeductedVolumeM3, 1e-5, "Slab deducts column footprint and 4 beam stems (0.7392 m3)");
            AssertEqual(17.28 - expectedSlabDeduction, res["S1"].NetVolumeM3, 1e-5, "Slab net volume == 16.5408 m3");
            AssertTrue(res["S1"].NetVolumeM3 > 0, "Slab net volume is strictly positive");

            foreach (var kvp in res)
            {
                AssertTrue(kvp.Value.NetVolumeM3 >= 0.0, $"Member {kvp.Key} has zero negative net volume ({kvp.Value.NetVolumeM3})");
            }

            var summary = QsScheduleAggregator.Aggregate(elements);
            double sumDeductedNet = res.Values.Sum(v => v.NetVolumeM3);
            AssertEqual(sumDeductedNet, summary.TotalVolumeM3, 1e-5, "Summary TotalVolumeM3 == sum(deducted member net volumes) (20.0008 m3)");

            var tinySlab = new QsElementModel
            {
                Id = "TinySlab", Mark = "TS", MemberType = QsMemberType.Slab,
                LengthMm = 1000, WidthMm = 1000, ThicknessMm = 100,
                IntersectingElementIds = new List<string> { "BN", "BS", "BE", "BW" }
            };
            var resTiny = QsIntersectionDeductionEngine.Evaluate(new[] { bN, bS, bE, bW, tinySlab });
            AssertTrue(resTiny["TinySlab"].NetVolumeM3 == 0.0, "Oversized deduction on tiny slab clamps NetVolumeM3 cleanly to 0.0 (no negative volume)");
            AssertEqual(0.1, resTiny["TinySlab"].DeductedVolumeM3, 1e-5, "Deducted volume cannot exceed gross volume");
        }

        // ====================================================================
        // SUITE 3: DEGENERATE AND ADVERSARIAL DIMENSIONS
        // ====================================================================
        private static void RunSuite3_DegenerateAndAdversarialDimensions()
        {
            Console.WriteLine("\n--- SUITE 3: DEGENERATE AND ADVERSARIAL DIMENSIONS ---");

            bool nullElementThrows = false;
            try { QsTakeoffCalculator.CalculateGrossVolume(null!); }
            catch (ArgumentNullException) { nullElementThrows = true; }
            AssertTrue(nullElementThrows, "CalculateGrossVolume(null) throws ArgumentNullException");

            bool nullCollectionThrows = false;
            try { QsIntersectionDeductionEngine.Evaluate(null!); }
            catch (ArgumentNullException) { nullCollectionThrows = true; }
            AssertTrue(nullCollectionThrows, "QsIntersectionDeductionEngine.Evaluate(null) throws ArgumentNullException");

            foreach (var mType in new[] { QsMemberType.Footing, QsMemberType.Column, QsMemberType.Wall, QsMemberType.Beam, QsMemberType.Slab, QsMemberType.Stair })
            {
                var zeroElem = new QsElementModel { MemberType = mType, WidthMm = 0, HeightMm = 0, LengthMm = 0, ThicknessMm = 0, PlanAreaMm2 = 0 };
                double vol = QsTakeoffCalculator.CalculateGrossVolume(zeroElem);
                AssertEqual(0.0, vol, 1e-9, $"Zero dimensions {mType} yields 0.0 volume");
            }

            var zeroThickSlab = new QsElementModel { MemberType = QsMemberType.Slab, PlanAreaMm2 = 50e6, ThicknessMm = 0, HeightMm = 0 };
            double zeroThickVol = QsTakeoffCalculator.CalculateGrossVolume(zeroThickSlab);
            AssertEqual(0.0, zeroThickVol, 1e-9, "Zero thickness slab yields 0.0 volume");

            var negCountCol = new QsElementModel { MemberType = QsMemberType.Column, WidthMm = 400, HeightMm = 400, LengthMm = 3000, MemberCount = -5 };
            double negCountVol = QsTakeoffCalculator.CalculateGrossVolume(negCountCol);
            AssertEqual(0.4 * 0.4 * 3.0 * 1, negCountVol, 1e-6, "MemberCount <= 0 is safely clamped to 1 via Math.Max(1, count)");

            var zeroCountCol = new QsElementModel { MemberType = QsMemberType.Column, WidthMm = 400, HeightMm = 400, LengthMm = 3000, MemberCount = 0 };
            double zeroCountVol = QsTakeoffCalculator.CalculateGrossVolume(zeroCountCol);
            AssertEqual(0.4 * 0.4 * 3.0 * 1, zeroCountVol, 1e-6, "MemberCount == 0 is safely clamped to 1 via Math.Max(1, count)");

            var elemA = new QsElementModel { Id = "A", MemberType = QsMemberType.Column, WidthMm = 400, HeightMm = 400, LengthMm = 3000, IntersectingElementIds = new List<string> { "B" } };
            var elemB = new QsElementModel { Id = "B", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 5000, IntersectingElementIds = new List<string> { "A" } };
            var resCycle = QsIntersectionDeductionEngine.Evaluate(new[] { elemA, elemB });
            AssertTrue(resCycle["A"].Deductions.Count == 0, "Mutual cycle: Column A does not falsely deduct from Beam B");
            AssertTrue(resCycle["B"].Deductions.Count == 1, "Mutual cycle: Beam B deducts exactly once from Column A (no duplicate deduction)");

            var elemSelf = new QsElementModel { Id = "Self", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 5000, IntersectingElementIds = new List<string> { "Self" } };
            var resSelf = QsIntersectionDeductionEngine.Evaluate(new[] { elemSelf });
            AssertEqual(0.0, resSelf["Self"].DeductedVolumeM3, 1e-9, "Self-intersecting element (i == j) is skipped and yields 0 deduction");
        }

        // ====================================================================
        // SUITE 4: FORMWORK CALCULATIONS AND CONTACT DEDUCTIONS
        // ====================================================================
        private static void RunSuite4_FormworkCalculationsAndContactDeductions()
        {
            Console.WriteLine("\n--- SUITE 4: FORMWORK CALCULATIONS AND CONTACT DEDUCTIONS ---");

            var col = new QsElementModel { Id = "C1", MemberType = QsMemberType.Column, WidthMm = 400, HeightMm = 400, LengthMm = 3500 };
            var b1 = new QsElementModel { Id = "B1", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 6000, IntersectingElementIds = new List<string> { "C1" } };
            var fw1 = QsFormworkCalculator.Evaluate(new[] { col, b1 });

            AssertEqual(5.6, fw1["C1"].GrossFormworkM2, 1e-5, "Column gross formwork perimeter * height (5.600 m2)");
            AssertEqual(0.125, fw1["C1"].DeductedAreaM2, 1e-5, "Column formwork deducts beam end contact face (0.125 m2)");
            AssertEqual(5.6 - 0.125, fw1["C1"].NetFormworkM2, 1e-5, "Column net formwork == 5.475 m2");

            var b2 = new QsElementModel { Id = "B2", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 6000, IntersectingElementIds = new List<string> { "C1" } };
            var b3 = new QsElementModel { Id = "B3", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 6000, IntersectingElementIds = new List<string> { "C1" } };
            var b4 = new QsElementModel { Id = "B4", MemberType = QsMemberType.Beam, WidthMm = 250, HeightMm = 500, LengthMm = 6000, IntersectingElementIds = new List<string> { "C1" } };
            var fw4 = QsFormworkCalculator.Evaluate(new[] { col, b1, b2, b3, b4 });
            AssertEqual(4 * 0.125, fw4["C1"].DeductedAreaM2, 1e-5, "Column formwork deducts 4 beam ends (0.500 m2)");
            AssertEqual(5.6 - 0.5, fw4["C1"].NetFormworkM2, 1e-5, "Column net formwork with 4 beams == 5.100 m2");

            var beam = new QsElementModel { Id = "B_Main", MemberType = QsMemberType.Beam, WidthMm = 300, HeightMm = 600, LengthMm = 7000 };
            var slab = new QsElementModel { Id = "S_Main", MemberType = QsMemberType.Slab, LengthMm = 7000, WidthMm = 5000, ThicknessMm = 150, IntersectingElementIds = new List<string> { "B_Main" } };
            var fwSlab = QsFormworkCalculator.Evaluate(new[] { beam, slab });

            AssertEqual(1.05, fwSlab["B_Main"].DeductedAreaM2, 1e-5, "Beam side formwork deducts slab thickness contact area (1.05 m2)");
            AssertEqual(10.5, fwSlab["B_Main"].GrossFormworkM2, 1e-5, "Beam gross formwork (10.500 m2)");
            AssertEqual(10.5 - 1.05, fwSlab["B_Main"].NetFormworkM2, 1e-5, "Beam net formwork == 9.450 m2");

            var pb = new QsElementModel { Id = "PB_Fw", MemberType = QsMemberType.Beam, IsPrimary = true, WidthMm = 350, HeightMm = 700, LengthMm = 8000 };
            var sb = new QsElementModel { Id = "SB_Fw", MemberType = QsMemberType.Beam, IsPrimary = false, WidthMm = 200, HeightMm = 400, LengthMm = 4000, IntersectingElementIds = new List<string> { "PB_Fw" } };
            var fwSec = QsFormworkCalculator.Evaluate(new[] { pb, sb });
            AssertEqual(0.08, fwSec["PB_Fw"].DeductedAreaM2, 1e-5, "Primary beam formwork deducts secondary beam end contact (0.08 m2)");

            var footing = new QsElementModel { Id = "F_Main", MemberType = QsMemberType.Footing, LengthMm = 2000, WidthMm = 2000, HeightMm = 800 };
            var gradeBeam = new QsElementModel { Id = "GB_Main", MemberType = QsMemberType.Beam, WidthMm = 300, HeightMm = 600, LengthMm = 5000, IntersectingElementIds = new List<string> { "F_Main" } };
            var fwFooting = QsFormworkCalculator.Evaluate(new[] { footing, gradeBeam });
            AssertEqual(0.18, fwFooting["F_Main"].DeductedAreaM2, 1e-5, "Footing formwork deducts grade beam end contact face (0.18 m2)");

            var shortCol = new QsElementModel { Id = "ShortCol", MemberType = QsMemberType.Column, WidthMm = 200, HeightMm = 200, LengthMm = 300 };
            var hugeBeam = new QsElementModel { Id = "HugeBeam", MemberType = QsMemberType.Beam, WidthMm = 500, HeightMm = 1000, LengthMm = 3000, IntersectingElementIds = new List<string> { "ShortCol" } };
            var fwExcess = QsFormworkCalculator.Evaluate(new[] { shortCol, hugeBeam });
            AssertTrue(fwExcess["ShortCol"].NetFormworkM2 == 0.0, "Excessive contact deduction clamps NetFormworkM2 to 0.0 (no negative formwork)");
            AssertEqual(0.24, fwExcess["ShortCol"].DeductedAreaM2, 1e-5, "Deducted area cannot exceed gross formwork");
        }

        // ====================================================================
        // SUITE 5: CLOSEDXML WORKBOOK GENERATION
        // ====================================================================
        private static void RunSuite5_ClosedXmlWorkbookGeneration()
        {
            Console.WriteLine("\n--- SUITE 5: CLOSEDXML WORKBOOK GENERATION ---");

            var summary = new QsTakeoffSummary
            {
                TotalVolumeM3 = 150.75, TotalFormworkM2 = 620.40,
                FootingVolumeM3 = 30.0, FootingFormworkM2 = 80.0,
                ColumnVolumeM3 = 25.0, ColumnFormworkM2 = 140.0,
                WallVolumeM3 = 15.75, WallFormworkM2 = 90.40,
                BeamVolumeM3 = 35.0, BeamFormworkM2 = 150.0,
                SlabVolumeM3 = 40.0, SlabFormworkM2 = 150.0,
                StairVolumeM3 = 5.0, StairFormworkM2 = 10.0
            };

            summary.Items.Add(new QsTakeoffItem { DrawingName = "Tầng 1", Story = "Story 1", ItemMark = "F1", MemberType = QsMemberType.Footing, Count = 4, AreaM2 = 4.0, LengthM = 2.0, HeightM = 0.8, TotalAreaM2 = 16.0, TotalLengthM = 8.0, TotalVolumeM3 = 12.8, FormworkAreaM2 = 25.6 });
            summary.Items.Add(new QsTakeoffItem { DrawingName = "Tầng 1", Story = "Story 1", ItemMark = "C1", MemberType = QsMemberType.Column, Count = 8, AreaM2 = 0.16, LengthM = 3.6, HeightM = 3.6, TotalAreaM2 = 1.28, TotalLengthM = 28.8, TotalVolumeM3 = 4.608, FormworkAreaM2 = 46.08 });
            summary.Items.Add(new QsTakeoffItem { DrawingName = "Tầng 1", Story = "Story 1", ItemMark = "D1", MemberType = QsMemberType.Beam, Count = 12, AreaM2 = 0.15, LengthM = 6.0, HeightM = 0.5, TotalAreaM2 = 1.8, TotalLengthM = 72.0, TotalVolumeM3 = 10.8, FormworkAreaM2 = 90.0 });
            summary.Items.Add(new QsTakeoffItem { DrawingName = "Tầng 1", Story = "Story 1", ItemMark = "S1", MemberType = QsMemberType.Slab, Count = 1, AreaM2 = 200.0, LengthM = 20.0, HeightM = 0.12, TotalAreaM2 = 200.0, TotalLengthM = 20.0, TotalVolumeM3 = 24.0, FormworkAreaM2 = 200.0 });

            using var ms = new MemoryStream();
            QsWorkbookGenerator.ExportToStream(summary, ms);
            AssertTrue(ms.Length > 0, "ExportToStream produces non-empty byte stream");

            ms.Position = 0;
            bool validOpenXmlPackage = false;
            try
            {
                using var doc = SpreadsheetDocument.Open(ms, false);
                validOpenXmlPackage = (doc.WorkbookPart != null && doc.WorkbookPart.WorksheetParts.Count() >= 2);
            }
            catch (Exception ex)
            {
                Failures.Add($"SpreadsheetDocument Open error: {ex.Message}");
            }
            AssertTrue(validOpenXmlPackage, "Low-level SpreadsheetDocument parses package cleanly with WorkbookPart and WorksheetParts");

            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            var wsQs = wb.Worksheet("Kata_QS");
            var wsSum = wb.Worksheet("Thong_ke_QS");
            AssertTrue(wsQs != null, "Worksheet Kata_QS exists");
            AssertTrue(wsSum != null, "Worksheet Thong_ke_QS exists");

            string formG3 = wsQs!.Cell("G3").FormulaA1;
            string formH3 = wsQs!.Cell("H3").FormulaA1;
            AssertFormula("C3*D3", formG3, "Kata_QS row 3 Total Area formula");
            AssertFormula("C3*E3", formH3, "Kata_QS row 3 Total Length formula");

            int totalRow = 3 + summary.Items.Count; // row 7
            string sumI = wsQs!.Cell(totalRow, 9).FormulaA1;
            string sumJ = wsQs!.Cell(totalRow, 10).FormulaA1;
            AssertFormula("SUM(I3:I6)", sumI, "Kata_QS total row volume formula");
            AssertFormula("SUM(J3:J6)", sumJ, "Kata_QS total row formwork formula");

            string sumVol = wsSum!.Cell("B9").FormulaA1;
            string sumFw = wsSum!.Cell("C9").FormulaA1;
            AssertFormula("SUM(B3:B8)", sumVol, "Thong_ke_QS Grand Total volume formula");
            AssertFormula("SUM(C3:C8)", sumFw, "Thong_ke_QS Grand Total formwork formula");

            double evalVolSum = wsSum!.Cell("B9").GetDouble();
            double expectedCatVolSum = summary.FootingVolumeM3 + summary.ColumnVolumeM3 + summary.WallVolumeM3 + summary.BeamVolumeM3 + summary.SlabVolumeM3 + summary.StairVolumeM3;
            AssertEqual(expectedCatVolSum, evalVolSum, 1e-3, "Thong_ke_QS formula B9 evaluated sum matches expected category sum (150.75 m3)");

            double evalFwSum = wsSum!.Cell("C9").GetDouble();
            double expectedCatFwSum = summary.FootingFormworkM2 + summary.ColumnFormworkM2 + summary.WallFormworkM2 + summary.BeamFormworkM2 + summary.SlabFormworkM2 + summary.StairFormworkM2;
            AssertEqual(expectedCatFwSum, evalFwSum, 1e-3, "Thong_ke_QS formula C9 evaluated sum matches expected category sum (620.40 m2)");

            var emptySummary = new QsTakeoffSummary();
            using var msEmpty = new MemoryStream();
            QsWorkbookGenerator.ExportToStream(emptySummary, msEmpty);
            AssertTrue(msEmpty.Length > 0, "Empty summary exports valid package without throwing");
            msEmpty.Position = 0;
            using var wbEmpty = new XLWorkbook(msEmpty);
            AssertTrue(wbEmpty.Worksheet("Kata_QS") != null, "Empty summary has Kata_QS sheet");
            AssertTrue(wbEmpty.Worksheet("Thong_ke_QS") != null, "Empty summary has Thong_ke_QS sheet");

            string tempFile = Path.Combine(Path.GetTempPath(), $"QS_Challenge_{Guid.NewGuid():N}.xlsx");
            try
            {
                QsWorkbookGenerator.ExportToFile(summary, tempFile);
                AssertTrue(File.Exists(tempFile), "ExportToFile creates actual .xlsx on disk");
                var fi = new FileInfo(tempFile);
                AssertTrue(fi.Length > 2000, $"Exported file has valid size ({fi.Length} bytes)");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        // ====================================================================
        // SUITE 6: LARGE-SCALE & PERFORMANCE STRESS
        // ====================================================================
        private static void RunSuite6_LargeScaleAndPerformanceStress()
        {
            Console.WriteLine("\n--- SUITE 6: LARGE-SCALE & PERFORMANCE STRESS (1,000 ELEMENTS) ---");

            var sw = Stopwatch.StartNew();
            const int count = 1000;
            var elements = new List<QsElementModel>(count);

            for (int i = 0; i < count; i++)
            {
                var type = (QsMemberType)(i % 6);
                var elem = new QsElementModel
                {
                    Id = $"Elem_{i}",
                    Mark = $"{type}_{i}",
                    Story = $"Story {1 + (i / 100)}",
                    MemberType = type,
                    MemberCount = 1 + (i % 4),
                    LengthMm = 3000 + (i % 5000),
                    WidthMm = 200 + (i % 600),
                    HeightMm = 300 + (i % 700),
                    ThicknessMm = type == QsMemberType.Slab ? 120 : (type == QsMemberType.Wall ? 200 : 0)
                };

                // Link to adjacent elements to form a complex graph
                if (i > 0 && i % 2 == 0)
                {
                    elem.IntersectingElementIds.Add($"Elem_{i - 1}");
                }
                if (i > 2 && i % 3 == 0)
                {
                    elem.IntersectingElementIds.Add($"Elem_{i - 2}");
                }
                elements.Add(elem);
            }

            sw.Stop();
            Console.WriteLine($"  [Scale] Generated {count} elements in {sw.ElapsedMilliseconds} ms");

            // Evaluate Volume Deductions
            sw.Restart();
            var volResults = QsIntersectionDeductionEngine.Evaluate(elements);
            sw.Stop();
            AssertTrue(volResults.Count == count, $"Volume deduction evaluated {count} elements in {sw.ElapsedMilliseconds} ms");
            AssertTrue(sw.ElapsedMilliseconds < 3000, "1,000 element volume deduction completes in < 3000 ms");

            // Evaluate Formwork Contact Deductions
            sw.Restart();
            var fwResults = QsFormworkCalculator.Evaluate(elements);
            sw.Stop();
            AssertTrue(fwResults.Count == count, $"Formwork contact evaluated {count} elements in {sw.ElapsedMilliseconds} ms");
            AssertTrue(sw.ElapsedMilliseconds < 3000, "1,000 element formwork contact completes in < 3000 ms");

            // Full Aggregation
            sw.Restart();
            var summary = QsScheduleAggregator.Aggregate(elements);
            sw.Stop();
            AssertTrue(summary.Items.Count == count, $"Schedule aggregation produced {count} items in {sw.ElapsedMilliseconds} ms");
            AssertTrue(summary.TotalVolumeM3 > 0, $"TotalVolumeM3 is positive: {summary.TotalVolumeM3:F2} m3");
            AssertTrue(summary.TotalFormworkM2 > 0, $"TotalFormworkM2 is positive: {summary.TotalFormworkM2:F2} m2");

            // Full Workbook Generation (1,000 rows in Kata_QS)
            sw.Restart();
            using var ms = new MemoryStream();
            QsWorkbookGenerator.ExportToStream(summary, ms);
            sw.Stop();
            AssertTrue(ms.Length > 20000, $"1,000 row workbook exported ({ms.Length / 1024} KB) in {sw.ElapsedMilliseconds} ms");
            AssertTrue(sw.ElapsedMilliseconds < 5000, "1,000 row Excel generation completes in < 5000 ms");
        }

        // ====================================================================
        // SUITE 7: NEGATIVE DIMENSIONS DEFECT DEMONSTRATION
        // ====================================================================
        private static void RunSuite7_NegativeDimensionsDefectDemonstration()
        {
            Console.WriteLine("\n--- SUITE 7: NEGATIVE DIMENSIONS DEFECT DEMONSTRATION ---");

            // Test 7.1 Single negative dimension (negative width)
            var bNegWidth = new QsElementModel { Id = "NegW", MemberType = QsMemberType.Beam, WidthMm = -250, HeightMm = 500, LengthMm = 6000 };
            double volNegW = QsTakeoffCalculator.CalculateGrossVolume(bNegWidth);
            bool producesNegativeGross = volNegW < 0;
            AssertTrue(producesNegativeGross, "PROOF OF DEFECT: Negative WidthMm produces negative GrossVolume (-0.75 m3)");

            // Test 7.2 Two negative dimensions (-250 x -500 x 6000) produce false positive volume
            var bDoubleNeg = new QsElementModel { Id = "DoubleNeg", MemberType = QsMemberType.Beam, WidthMm = -250, HeightMm = -500, LengthMm = 6000 };
            double volDoubleNeg = QsTakeoffCalculator.CalculateGrossVolume(bDoubleNeg);
            bool falsePositiveVol = volDoubleNeg > 0;
            AssertTrue(falsePositiveVol, "PROOF OF DEFECT: Two negative dimensions (-250 x -500 x 6000) multiply to false positive volume (+0.75 m3)");

            // Test 7.3 Negative gross volume passes into QsVolumeResult and QsScheduleAggregator
            var summaryNeg = QsScheduleAggregator.Aggregate(new[] { bNegWidth });
            bool summaryContainsNegativeVol = summaryNeg.TotalVolumeM3 < 0;
            AssertTrue(summaryContainsNegativeVol, $"PROOF OF DEFECT: QsScheduleAggregator computes negative TotalVolumeM3 ({summaryNeg.TotalVolumeM3} m3) for negative dimension input");

            // Test 7.4 Negative formwork surface area
            var (soffitNeg, sideNeg) = QsTakeoffCalculator.CalculateGrossFormwork(bNegWidth);
            bool negativeFormwork = soffitNeg < 0 || sideNeg < 0;
            AssertTrue(negativeFormwork, $"PROOF OF DEFECT: QsTakeoffCalculator produces negative formwork (Soffit={soffitNeg} m2, Sides={sideNeg} m2)");

            CriticalFindings.Add($"DEFECT B-QS-01: QsTakeoffCalculator lacks guard/clamping for negative dimensions (WidthMm, HeightMm, LengthMm, ThicknessMm), allowing negative or corrupted volumes (-0.75 m3) and negative formwork surfaces to propagate into takeoff schedules and ClosedXML workbooks.");
        }
    }
}
