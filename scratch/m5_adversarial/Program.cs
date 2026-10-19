using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HPBim.Client.Verification;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;

namespace M5AdversarialTests;

public static class Program
{
    private static int _passed = 0;
    private static int _failed = 0;
    private static readonly List<string> _findings = new();

    public static int Main()
    {
        Console.WriteLine("=== M5 ADVERSARIAL STRESS TEST SUITE ===");

        TestTargetUniqueIdPriorityAndFallbacks();
        TestPileCutoffElevationAdversarial();
        TestProfileSlabOutlineAndThicknessAdversarial();
        TestGenericCategoryRuleZeroEnforcement();
        TestConcurrentReentrancyOnEngine();
        TestNullAndCorruptedInputs();
        TestScriptTemplateIntegrity();

        Console.WriteLine("\n========================================");
        Console.WriteLine($"RESULTS: Passed: {_passed}, Failed: {_failed}");
        if (_findings.Count > 0)
        {
            Console.WriteLine("FINDINGS:");
            foreach (var f in _findings) Console.WriteLine($" - {f}");
        }
        Console.WriteLine("========================================");

        return _failed == 0 ? 0 : 1;
    }

    private static void Assert(bool condition, string name, string? details = null)
    {
        if (condition)
        {
            _passed++;
            Console.WriteLine($"[PASS] {name}");
        }
        else
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {name}: {details ?? "Failed"}");
            Console.ResetColor();
            _findings.Add($"{name}: {details}");
        }
    }

    private static void TestTargetUniqueIdPriorityAndFallbacks()
    {
        Console.WriteLine("\n--- TargetUniqueId Priority & Fallback Tests ---");
        var engine = new MeasuredGeometricEngine();

        // 1. TargetUniqueId matches different actual than SourceKey
        var item1 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            TargetUniqueId = "uid-correct",
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, -2500), OffsetZMm = -2500 }
        };
        var actuals1 = new Dictionary<string, ActualElementGeometry>
        {
            ["uid-correct"] = new() { Key = "uid-correct", UniqueId = "uid-correct", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(0, 0, -2500), TopElevationMm = -2500 },
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", UniqueId = "uid-wrong", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(500, 500, -2500), TopElevationMm = -2500 }
        };
        var audit1 = engine.AuditItem(item1, actuals1);
        Assert(audit1.Status == GeometricVerificationStatus.Verified && audit1.TargetUniqueId == "uid-correct",
            "1.1 TargetUniqueId takes strict priority over SourceKey");

        // 2. TargetUniqueId not in actuals, fallback to SourceKey
        var item2 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P2"),
            TargetUniqueId = "uid-nonexistent",
            Category = CoordinationCategory.Pile,
            Mark = "P2",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, -2500), OffsetZMm = -2500 }
        };
        var actuals2 = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P2"] = new() { Key = "Doc#Pile:P2", UniqueId = "uid-by-key", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(0, 0, -2500), TopElevationMm = -2500 }
        };
        var audit2 = engine.AuditItem(item2, actuals2);
        Assert(audit2.Status == GeometricVerificationStatus.Verified && audit2.TargetUniqueId == "uid-by-key",
            "1.2 TargetUniqueId absent falls back to SourceKey gracefully");

        // 3. TargetUniqueId is empty string, does not crash and falls back
        var item3 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P3"),
            TargetUniqueId = "",
            Category = CoordinationCategory.Pile,
            Mark = "P3",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, -2500), OffsetZMm = -2500 }
        };
        var audit3 = engine.AuditItem(item3, actuals2);
        Assert(audit3.Status == GeometricVerificationStatus.Missing,
            "1.3 Empty TargetUniqueId does not match empty string key in actuals");
    }

    private static void TestPileCutoffElevationAdversarial()
    {
        Console.WriteLine("\n--- Pile Cutoff Elevation Adversarial Tests ---");
        var tolerances = new CoordinationTolerances { ElevationMm = 1.0, PileCenterMm = 2.0 };
        var engine = new MeasuredGeometricEngine(tolerances);

        // 1. Missing TopElevationMm
        var item1 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = new PointGeometrySpec { Location = new Point3D(100, 200, -2500), OffsetZMm = -2500 }
        };
        var actMissingElev = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(100, 200, -2500), TopElevationMm = null }
        };
        var a1 = engine.AuditItem(item1, actMissingElev);
        Assert(a1.Status == GeometricVerificationStatus.Inconclusive && a1.DefectReason!.Contains("TopElevationMm"),
            "2.1 Missing TopElevationMm returns Inconclusive with TopElevationMm in reason");

        // 2. TopElevationMm exact tolerance boundary: delta = 1.0mm
        var actLimitElev = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(100, 200, -2500), TopElevationMm = -2499.0 }
        };
        var a2 = engine.AuditItem(item1, actLimitElev);
        Assert(a2.Status == GeometricVerificationStatus.Verified && !a2.IsElevationOutOfTolerance,
            "2.2 TopElevationMm error = 1.0mm exactly matches tolerance threshold");

        // 3. TopElevationMm exceeds tolerance: delta = 1.001mm
        var actOverElev = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = new Point3D(100, 200, -2500), TopElevationMm = -2498.999 }
        };
        var a3 = engine.AuditItem(item1, actOverElev);
        Assert(a3.Status == GeometricVerificationStatus.Defect && a3.IsElevationOutOfTolerance,
            "2.3 TopElevationMm error = 1.001mm marks Defect and IsElevationOutOfTolerance = true");

        // 4. Missing Centroid on pile
        var actMissingCentroid = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true, Centroid = null, TopElevationMm = -2500 }
        };
        var a4 = engine.AuditItem(item1, actMissingCentroid);
        Assert(a4.Status == GeometricVerificationStatus.Inconclusive && a4.DefectReason!.Contains("Centroid"),
            "2.4 Missing Centroid on pile returns Inconclusive");
    }

    private static void TestProfileSlabOutlineAndThicknessAdversarial()
    {
        Console.WriteLine("\n--- Profile / Slab Outline & Thickness Adversarial Tests ---");
        var tolerances = new CoordinationTolerances { ElevationMm = 1.0, PlanPositionMm = 1.0, SectionDimensionMm = 0.0 };
        var engine = new MeasuredGeometricEngine(tolerances);

        var squareLoop = new[]
        {
            new Point3D(0, 0, 0),
            new Point3D(2000, 0, 0),
            new Point3D(2000, 2000, 0),
            new Point3D(0, 2000, 0)
        };
        // Expected centroid: (1000, 1000)

        var slabItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, "S1"),
            Category = CoordinationCategory.Slab,
            Mark = "S1",
            Geometry = new ProfileGeometrySpec { OuterLoop = squareLoop, ThicknessMm = 150.0, ElevationMm = 0.0 }
        };

        // 1. Missing Centroid
        var actNoCentroid = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new() { Key = "Doc#Slab:S1", HasGeometry = true, Centroid = null, TopElevationMm = 0, ThicknessMm = 150 }
        };
        var a1 = engine.AuditItem(slabItem, actNoCentroid);
        Assert(a1.Status == GeometricVerificationStatus.Inconclusive && a1.DefectReason!.Contains("Centroid"),
            "3.1 Slab missing centroid returns Inconclusive");

        // 2. Missing Thickness
        var actNoThick = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new() { Key = "Doc#Slab:S1", HasGeometry = true, Centroid = new Point3D(1000, 1000, 0), TopElevationMm = 0, ThicknessMm = null }
        };
        var a2 = engine.AuditItem(slabItem, actNoThick);
        Assert(a2.Status == GeometricVerificationStatus.Inconclusive && a2.DefectReason!.Contains("Thickness"),
            "3.2 Slab missing thickness returns Inconclusive");

        // 3. Centroid discrepancy: offset = 1.5mm (> PlanPositionMm = 1.0mm)
        var actCentroidOffset = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new() { Key = "Doc#Slab:S1", HasGeometry = true, Centroid = new Point3D(1001.5, 1000, 0), TopElevationMm = 0, ThicknessMm = 150 }
        };
        var a3 = engine.AuditItem(slabItem, actCentroidOffset);
        Assert(a3.Status == GeometricVerificationStatus.Defect && a3.IsPositionOutOfTolerance,
            "3.3 Slab centroid offset 1.5mm (> 1.0mm) returns Defect");

        // 4. Centroid within tolerance: offset = 0.5mm (<= 1.0mm)
        var actCentroidPass = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S1"] = new() { Key = "Doc#Slab:S1", HasGeometry = true, Centroid = new Point3D(1000.5, 1000, 0), TopElevationMm = 0, ThicknessMm = 150 }
        };
        var a4 = engine.AuditItem(slabItem, actCentroidPass);
        Assert(a4.Status == GeometricVerificationStatus.Verified && !a4.IsPositionOutOfTolerance,
            "3.4 Slab centroid offset 0.5mm (<= 1.0mm) returns Verified");

        // 5. Polygon loop with empty list
        var emptyLoopItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Footing, "F1"),
            Category = CoordinationCategory.Footing,
            Mark = "F1",
            Geometry = new ProfileGeometrySpec { OuterLoop = Array.Empty<Point3D>(), ThicknessMm = 0, ElevationMm = -1600 }
        };
        var actFooting = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Footing:F1"] = new() { Key = "Doc#Footing:F1", HasGeometry = true, Centroid = new Point3D(0, 0, -1600), TopElevationMm = -1600 }
        };
        var a5 = engine.AuditItem(emptyLoopItem, actFooting);
        Assert(a5.Status == GeometricVerificationStatus.Verified,
            "3.5 Empty OuterLoop on Footing does not throw and passes if elevation matches");
    }

    private static void TestGenericCategoryRuleZeroEnforcement()
    {
        Console.WriteLine("\n--- Generic Category Rule 0 Enforcement Tests ---");
        var engine = new MeasuredGeometricEngine();

        var categories = new[]
        {
            CoordinationCategory.Opening,
            CoordinationCategory.Room,
            CoordinationCategory.MepFixture,
            CoordinationCategory.PipeRun,
            CoordinationCategory.Rebar,
            (CoordinationCategory)888,
            (CoordinationCategory)999
        };

        foreach (var cat in categories)
        {
            var item = new ElementPlanItem
            {
                SourceKey = new CompositeSourceKey("Doc", cat, "M1"),
                Category = cat,
                Mark = "M1",
                Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
            };
            var actuals = new Dictionary<string, ActualElementGeometry>
            {
                [$"Doc#{cat}:M1"] = new()
                {
                    Key = $"Doc#{cat}:M1",
                    HasGeometry = true,
                    Centroid = new Point3D(0, 0, 0),
                    WidthMm = 100,
                    HeightMm = 100,
                    ThicknessMm = 100,
                    TopElevationMm = 0
                }
            };

            var audit = engine.AuditItem(item, actuals);
            Assert(audit.Status == GeometricVerificationStatus.Inconclusive && audit.DefectReason!.Contains("Rule 0"),
                $"4.x Category {cat} strictly returns Inconclusive with Rule 0 explanation");
        }
    }

    private static void TestConcurrentReentrancyOnEngine()
    {
        Console.WriteLine("\n--- Concurrent Reentrancy on MeasuredGeometricEngine ---");
        var engine = new MeasuredGeometricEngine();

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-concurrent",
            Items = Enumerable.Range(1, 100).Select(i => new ElementPlanItem
            {
                SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, $"P{i}"),
                Category = CoordinationCategory.Pile,
                Mark = $"P{i}",
                Geometry = new PointGeometrySpec { Location = new Point3D(i * 100, i * 200, -2500), OffsetZMm = -2500 }
            }).ToList()
        };

        var actuals = plan.Items.ToDictionary(
            it => it.SourceKey!.ToString(),
            it => new ActualElementGeometry
            {
                Key = it.SourceKey!.ToString(),
                HasGeometry = true,
                Centroid = ((PointGeometrySpec)it.Geometry!).Location,
                TopElevationMm = -2500
            });

        bool allSucceeded = true;
        Parallel.For(0, 50, _ =>
        {
            var rep = engine.AuditPlan(plan, actuals);
            if (!rep.OverallPassed || rep.VerifiedCount != 100) allSucceeded = false;
        });

        Assert(allSucceeded, "5.1 50 parallel threads running AuditPlan simultaneously pass 100%");
    }

    private static void TestNullAndCorruptedInputs()
    {
        Console.WriteLine("\n--- Null and Corrupted Inputs Handling ---");
        var engine = new MeasuredGeometricEngine();

        bool threwNullPlan = false;
        try { engine.AuditPlan(null!, new Dictionary<string, ActualElementGeometry>()); }
        catch (ArgumentNullException) { threwNullPlan = true; }
        Assert(threwNullPlan, "6.1 Null plan throws ArgumentNullException");

        bool threwNullActuals = false;
        try { engine.AuditPlan(new BimExecutionPlan(), (IReadOnlyDictionary<string, ActualElementGeometry>)null!); }
        catch (ArgumentNullException) { threwNullActuals = true; }
        Assert(threwNullActuals, "6.2 Null actuals throws ArgumentNullException");

        bool threwNullItem = false;
        try { engine.AuditItem(null!, new Dictionary<string, ActualElementGeometry>()); }
        catch (ArgumentNullException) { threwNullItem = true; }
        Assert(threwNullItem, "6.3 Null item throws ArgumentNullException");

        // HasGeometry = false
        var planItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = new PointGeometrySpec()
        };
        var noGeomActual = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new() { Key = "Doc#Pile:P1", HasGeometry = false }
        };
        var audit = engine.AuditItem(planItem, noGeomActual);
        Assert(audit.Status == GeometricVerificationStatus.Inconclusive && audit.DefectReason!.Contains("Rule 0"),
            "6.4 Element with HasGeometry = false returns Inconclusive under Rule 0");
    }

    private static void TestScriptTemplateIntegrity()
    {
        Console.WriteLine("\n--- Script Template Integrity Tests ---");
        string code = RevitGeometryQueryScriptTemplate.GetScriptCode();

        Assert(code.Contains("doc.GetElement(targetUniqueId)"), "7.1 Query script contains doc.GetElement(targetUniqueId)");
        Assert(code.Contains("targetUniqueId"), "7.2 Query script contains targetUniqueId parameter read");
        Assert(code.Contains("bbPile.Max.Z"), "7.3 Query script extracts pile bounding box Max.Z");
        Assert(code.Contains("elem is Floor floorObj"), "7.4 Query script extracts floor object bounding box");
        Assert(code.Contains("record[\"thicknessMm\"] = Mm(pThk.AsDouble())"), "7.5 Query script extracts floor default thickness parameter");
        Assert(!code.Contains("//") && !code.Contains("/*"), "7.6 Query script contains zero comments");
    }
}
