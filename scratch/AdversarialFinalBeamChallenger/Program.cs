using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Tcvn5574.Calculators;
using HPRebar.Core.Tcvn5574.Materials;
using HPRebar.Core.Tcvn5574.Models;

public static class Program
{
    private static int totalTests = 0;
    private static int passedTests = 0;
    private static int failedTests = 0;
    private static readonly List<string> failures = new();

    public static int Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  EMPIRICAL ADVERSARIAL STRESS CHALLENGER: BEAM REBAR & TCVN 5574 ENGINE");
        Console.WriteLine("================================================================================");
        Console.WriteLine();

        Challenge1_MaterialAndFlexureBoundaries();
        Challenge2_ShearAndWebCrushBoundaries();
        Challenge3_OptimizerCombinatoricsAndCongestion();
        Challenge4_GeometricInvariantsAndBoundingBoxContainment();
        Challenge5_PathologicalArgumentsAndPreconditions();
        Challenge6_StressAndThroughputBenchmark();

        Console.WriteLine();
        Console.WriteLine("================================================================================");
        Console.WriteLine($"FINAL CHALLENGE RESULTS: {passedTests}/{totalTests} PASSED ({(failedTests == 0 ? "100% GREEN" : $"{failedTests} FAILED")})");
        Console.WriteLine("================================================================================");

        if (failures.Count > 0)
        {
            Console.WriteLine("DETECTED EMPIRICAL FAILURES:");
            foreach (var f in failures)
            {
                Console.WriteLine($"  [FAIL] {f}");
            }
            return 1;
        }

        return 0;
    }

    private static void AssertTest(string testName, bool condition, string detail = "")
    {
        totalTests++;
        if (condition)
        {
            passedTests++;
            Console.WriteLine($"  [PASS] {testName}");
        }
        else
        {
            failedTests++;
            string msg = $"{testName}: {detail}";
            Console.WriteLine($"  [FAIL] {msg}");
            failures.Add(msg);
        }
    }

    private static void Challenge1_MaterialAndFlexureBoundaries()
    {
        Console.WriteLine("--- CHALLENGE 1: MATERIAL & FLEXURE EQUILIBRIUM BOUNDARIES ---");
        var concreteGrades = new[] { ConcreteGrade.B15, ConcreteGrade.B20, ConcreteGrade.B25, ConcreteGrade.B30, ConcreteGrade.B35, ConcreteGrade.B40 };
        foreach (var cg in concreteGrades)
        {
            var mat = ConcreteMaterial.Create(cg);
            AssertTest($"Concrete {cg} Rb > 0 and Eb > 0", mat.Rb > 0 && mat.Rbt > 0 && mat.Eb > 0);
        }

        var steelGrades = new[] { SteelGrade.CB240T, SteelGrade.CB300V, SteelGrade.CB400V, SteelGrade.CB500V };
        foreach (var sg in steelGrades)
        {
            var mat = SteelMaterial.Create(sg);
            AssertTest($"Steel {sg} Rs > 0 and XiR in (0, 1)", mat.Rs > 0 && mat.XiR > 0 && mat.XiR < 1.0 && mat.AlphaR > 0 && mat.AlphaR < 1.0);
        }

        var b25 = ConcreteMaterial.Create(ConcreteGrade.B25);
        var cb400 = SteelMaterial.Create(SteelGrade.CB400V);

        // Zero moment produces minimum reinforcement mu = 0.1%
        var resZero = TcvnBeamFlexureCalculator.Calculate(300, 600, 0.0, b25, cb400, a: 40);
        double expectedAsMin = 0.001 * 300 * 560;
        AssertTest("Zero moment enforces 0.1% minimum reinforcement", Math.Abs(resZero.AsRequired - expectedAsMin) < 1e-4 && resZero.IsCompliant);

        // Infinitesimal moment 1e-12 kNm does not divide by zero or NaN
        var resInf = TcvnBeamFlexureCalculator.Calculate(300, 600, 1e-12, b25, cb400, a: 40);
        AssertTest("Infinitesimal moment yields valid finite As", !double.IsNaN(resInf.AsRequired) && !double.IsInfinity(resInf.AsRequired) && resInf.AsRequired >= expectedAsMin);

        // Negative moment produces identical positive steel requirement
        var resNeg = TcvnBeamFlexureCalculator.Calculate(300, 600, -150.0, b25, cb400, a: 40);
        var resPos = TcvnBeamFlexureCalculator.Calculate(300, 600, 150.0, b25, cb400, a: 40);
        AssertTest("Negative moment magnitude produces identical AsRequired", Math.Abs(resNeg.AsRequired - resPos.AsRequired) < 1e-4);

        // Enormous moment alpha_m >> alpha_R triggers doubly reinforced or overload
        var resHuge = TcvnBeamFlexureCalculator.Calculate(200, 400, 250.0, b25, cb400, a: 35);
        AssertTest("Massive moment switches to doubly reinforced", resHuge.IsDoublyReinforced && resHuge.AsCompressionRequired > 0);
    }

    private static void Challenge2_ShearAndWebCrushBoundaries()
    {
        Console.WriteLine("\n--- CHALLENGE 2: SHEAR & WEB CRUSH EQUILIBRIUM BOUNDARIES ---");
        var b25 = ConcreteMaterial.Create(ConcreteGrade.B25);
        var cb240 = SteelMaterial.Create(SteelGrade.CB240T);

        double b = 300, h = 600, a = 40, dStirrup = 8.0;
        int legs = 2;
        double h0 = h - a;
        double qbMaxKn = 0.3 * b25.Rb * b * h0 / 1e3;

        // Exactly at crush limit vs slightly above
        var resBelowCrush = TcvnBeamShearCalculator.Calculate(b, h, qbMaxKn - 1.0, b25, cb240, stirrupDiameter: dStirrup, legs: legs, a: a);
        AssertTest("Shear just below 0.3*Rb*b*h0 does not trigger crush", !resBelowCrush.IsWebCrushExceeded);

        var resAboveCrush = TcvnBeamShearCalculator.Calculate(b, h, qbMaxKn + 5.0, b25, cb240, stirrupDiameter: dStirrup, legs: legs, a: a);
        AssertTest("Shear above 0.3*Rb*b*h0 triggers IsWebCrushExceeded", resAboveCrush.IsWebCrushExceeded && !resAboveCrush.IsCompliant);

        // Low shear resists by concrete and uses constructive spacing
        var resLow = TcvnBeamShearCalculator.Calculate(b, h, 20.0, b25, cb240, stirrupDiameter: dStirrup, legs: legs, a: a);
        AssertTest("Low shear requires constructive spacing without crush", !resLow.RequiresStirrupByCalculation && resLow.IsCompliant && resLow.RecommendedSpacing > 0);
    }

    private static void Challenge3_OptimizerCombinatoricsAndCongestion()
    {
        Console.WriteLine("\n--- CHALLENGE 3: OPTIMIZER COMBINATORICS & CONGESTION BOUNDARIES ---");
        // Narrow beam b = 150mm with high steel demand
        var opt = TcvnBeamRebarOptimizer.Optimize(
            asRequired: 900.0,
            b: 150.0,
            h: 350.0,
            cover: 25.0,
            stirrupDiameter: 6.0,
            isTop: false);

        AssertTest("Narrow beam congestion returns rebar option", opt != null);
        AssertTest("Optimizer enforces valid area or spacing flag", opt!.ProvidedArea >= 900.0 || !opt.IsSpacingCompliant);

        // Candidates generation
        var candidates = TcvnBeamRebarOptimizer.GenerateCandidates(
            asRequired: 600.0,
            b: 250.0,
            cover: 25.0,
            stirrupDiameter: 8.0,
            isTop: false);

        AssertTest("Candidate generation produces compliant candidates", candidates.Count > 0 && candidates.All(c => c.ProvidedArea >= 600.0));
    }

    private static void Challenge4_GeometricInvariantsAndBoundingBoxContainment()
    {
        Console.WriteLine("\n--- CHALLENGE 4: 3D GEOMETRIC INVARIANTS & BOUNDING BOX CONTAINMENT ---");
        double clearSpan = 6000.0;
        double b = 300.0;
        double h = 800.0;

        var deepSpan = new BeamSpan(0, "DeepSpan", clearSpan, b, h, 0.0, 25.0, clearSpan);
        var stack = new BeamContinuousStack(new[] { deepSpan }, Array.Empty<BeamSupportNode>());
        var sideSpec = new BeamSideBarSpec(autoSkinBars: true, depthThreshold: 700.0, diameter: 12.0);
        var skinBars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, sideSpec, 8.0, 20.0);

        AssertTest("Deep beam generates skin bars", skinBars.Count > 0);

        bool allInside = true;
        foreach (var bar in skinBars)
        {
            foreach (var pt in bar.Polyline.Points)
            {
                if (pt.X < -10.0 || pt.X > clearSpan + 10.0 ||
                    Math.Abs(pt.Y) > b / 2.0 + 1.0 ||
                    Math.Abs(pt.Z) > h + 10.0)
                {
                    allInside = false;
                    break;
                }
            }
        }
        AssertTest("All skin bar polyline vertices reside inside physical beam envelope", allInside);
    }

    private static void Challenge5_PathologicalArgumentsAndPreconditions()
    {
        Console.WriteLine("\n--- CHALLENGE 5: PATHOLOGICAL ARGUMENTS & DEFENSIVE GUARDS ---");
        var b25 = ConcreteMaterial.Create(ConcreteGrade.B25);
        var cb400 = SteelMaterial.Create(SteelGrade.CB400V);

        bool threwNegativeWidth = false;
        try { TcvnBeamFlexureCalculator.Calculate(-100, 500, 100, b25, cb400); }
        catch (ArgumentOutOfRangeException) { threwNegativeWidth = true; }
        AssertTest("Flexure calculator rejects negative width", threwNegativeWidth);

        bool threwNegativeHeight = false;
        try { TcvnBeamFlexureCalculator.Calculate(300, -500, 100, b25, cb400); }
        catch (ArgumentOutOfRangeException) { threwNegativeHeight = true; }
        AssertTest("Flexure calculator rejects negative height", threwNegativeHeight);

        bool threwZeroStirrup = false;
        try { TcvnBeamShearCalculator.Calculate(300, 500, 50, b25, cb400, stirrupDiameter: 0.0, legs: 2); }
        catch (ArgumentOutOfRangeException) { threwZeroStirrup = true; }
        AssertTest("Shear calculator rejects zero stirrup diameter", threwZeroStirrup);
    }

    private static void Challenge6_StressAndThroughputBenchmark()
    {
        Console.WriteLine("\n--- CHALLENGE 6: HIGH-THROUGHPUT STRESS BENCHMARK ---");
        var b25 = ConcreteMaterial.Create(ConcreteGrade.B25);
        var cb400 = SteelMaterial.Create(SteelGrade.CB400V);
        var cb240 = SteelMaterial.Create(SteelGrade.CB240T);

        const int iterations = 10000;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            double m = 10.0 + (i % 200);
            var flex = TcvnBeamFlexureCalculator.Calculate(300, 600, m, b25, cb400, a: 40);
            double q = 10.0 + (i % 150);
            var shear = TcvnBeamShearCalculator.Calculate(300, 600, q, b25, cb240, stirrupDiameter: 8.0, legs: 2);
        }
        sw.Stop();
        double msPerCalc = (double)sw.ElapsedMilliseconds / iterations;
        Console.WriteLine($"  Benchmark: {iterations} full flexure+shear cycles in {sw.ElapsedMilliseconds} ms ({msPerCalc:F4} ms/op)");
        AssertTest("Calculation throughput < 0.1 ms per calculation cycle", msPerCalc < 0.1);
    }
}
