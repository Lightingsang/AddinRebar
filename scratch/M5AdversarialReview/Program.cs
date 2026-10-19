namespace M5AdversarialReview;

using System;
using System.Collections.Generic;
using HPBim.Client.Verification;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;

public static class Program
{
    private static int _total = 0;
    private static int _passed = 0;
    private static int _failed = 0;
    private static readonly List<string> _findings = new();

    public static int Main(string[] args)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  M5 INDEPENDENT ADVERSARIAL STRESS TEST & BOUNDARY VERIFICATION");
        Console.WriteLine("================================================================================");

        TestDegenerateGeometries();
        TestToleranceBoundaries();
        TestAmbiguousAndDuplicateMarks();
        TestExtremeAndPathologicalInputs();
        TestAuditReportEdgeCases();

        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"  RESULTS: Total={_total}, Passed={_passed}, Failed={_failed}");
        Console.WriteLine("================================================================================");

        if (_findings.Count > 0)
        {
            Console.WriteLine("\nADVERSARIAL FINDINGS:");
            foreach (var f in _findings)
            {
                Console.WriteLine($"  [!] {f}");
            }
        }

        return _failed == 0 ? 0 : 1;
    }

    private static void Assert(bool condition, string testName, string? detail = null)
    {
        _total++;
        if (condition)
        {
            _passed++;
            Console.WriteLine($"[PASS] {testName}");
        }
        else
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {testName} - {detail ?? "Assertion failed"}");
            Console.ResetColor();
            _findings.Add($"FAIL: {testName} ({detail})");
        }
    }

    private static void TestDegenerateGeometries()
    {
        Console.WriteLine("\n--- Group 1: Degenerate Geometries ---");
        var engine = new MeasuredGeometricEngine();

        // 1.1 Empty outer loop profile
        var itemEmptyLoop = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, "S_EMPTY"),
            Category = CoordinationCategory.Slab,
            Mark = "S_EMPTY",
            Geometry = new ProfileGeometrySpec
            {
                OuterLoop = Array.Empty<Point3D>(),
                ThicknessMm = 150.0,
                ElevationMm = 0.0
            }
        };
        var actualSlab = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S_EMPTY"] = new()
            {
                Key = "Doc#Slab:S_EMPTY", CategoryName = "Slab", HasGeometry = true,
                Centroid = new Point3D(0, 0, 0), TopElevationMm = 0.0, ThicknessMm = 150.0
            }
        };
        var auditEmptyLoop = engine.AuditItem(itemEmptyLoop, actualSlab);
        Assert(auditEmptyLoop.Status == GeometricVerificationStatus.Verified,
            "1.1 Empty outer loop with matching thickness/elevation does not crash and audits thickness/elevation");

        // 1.2 Degenerate outer loop with 1 point
        var item1PtLoop = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, "S_1PT"),
            Category = CoordinationCategory.Slab,
            Mark = "S_1PT",
            Geometry = new ProfileGeometrySpec
            {
                OuterLoop = new[] { new Point3D(100.0, 200.0, 0.0) },
                ThicknessMm = 150.0,
                ElevationMm = 0.0
            }
        };
        var actual1PtSlab = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Slab:S_1PT"] = new()
            {
                Key = "Doc#Slab:S_1PT", CategoryName = "Slab", HasGeometry = true,
                Centroid = new Point3D(100.0, 200.0, 0.0), TopElevationMm = 0.0, ThicknessMm = 150.0
            }
        };
        var audit1Pt = engine.AuditItem(item1PtLoop, actual1PtSlab);
        Assert(audit1Pt.Status == GeometricVerificationStatus.Verified,
            "1.2 1-point outer loop fallback averages point coordinates cleanly");

        // 1.3 Zero thickness planned
        var itemZeroThick = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Footing, "F_0THICK"),
            Category = CoordinationCategory.Footing,
            Mark = "F_0THICK",
            Geometry = new ProfileGeometrySpec
            {
                OuterLoop = new[] { new Point3D(0, 0, 0), new Point3D(1000, 0, 0), new Point3D(1000, 1000, 0), new Point3D(0, 1000, 0) },
                ThicknessMm = 0.0,
                ElevationMm = -1000.0
            }
        };
        var actualZeroThick = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Footing:F_0THICK"] = new()
            {
                Key = "Doc#Footing:F_0THICK", CategoryName = "Footing", HasGeometry = true,
                Centroid = new Point3D(500, 500, -1000), TopElevationMm = -1000.0, ThicknessMm = null
            }
        };
        var auditZeroThick = engine.AuditItem(itemZeroThick, actualZeroThick);
        Assert(auditZeroThick.Status == GeometricVerificationStatus.Verified,
            "1.3 Planned zero thickness skips thickness audit and passes when elevation/centroid match");

        // 1.4 Missing bounding box / unmeasurable geometry (HasGeometry = false)
        var actualNoGeom = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Footing:F_0THICK"] = new()
            {
                Key = "Doc#Footing:F_0THICK", CategoryName = "Footing", HasGeometry = false
            }
        };
        var auditNoGeom = engine.AuditItem(itemZeroThick, actualNoGeom);
        Assert(auditNoGeom.Status == GeometricVerificationStatus.Inconclusive,
            "1.4 Missing geometry (HasGeometry=false) returns Inconclusive under Rule 0");

        // 1.5 Pile with missing Cutoff Elevation
        var itemPile = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P_CUTOFF"),
            Category = CoordinationCategory.Pile,
            Mark = "P_CUTOFF",
            Geometry = new PointGeometrySpec { Location = new Point3D(1000, 1000, -2500), OffsetZMm = -2500.0 }
        };
        var actualPileNoElev = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P_CUTOFF"] = new()
            {
                Key = "Doc#Pile:P_CUTOFF", CategoryName = "Pile", HasGeometry = true,
                Centroid = new Point3D(1000, 1000, -2500), TopElevationMm = null
            }
        };
        var auditPileNoElev = engine.AuditItem(itemPile, actualPileNoElev);
        Assert(auditPileNoElev.Status == GeometricVerificationStatus.Inconclusive,
            "1.5 Pile with null TopElevationMm returns Inconclusive");

        // 1.6 Beam with missing CurveEnd
        var itemBeam = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B_NULL_CURVE"),
            Category = CoordinationCategory.Beam,
            Mark = "B_NULL_CURVE",
            Geometry = new LineGeometrySpec { Start = new(0, 0, 0), End = new(3000, 0, 0), ElevationMm = 0.0 }
        };
        var actualBeamNoEnd = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B_NULL_CURVE"] = new()
            {
                Key = "Doc#Beam:B_NULL_CURVE", CategoryName = "Beam", HasGeometry = true,
                CurveStart = new(0, 0, 0), CurveEnd = null, TopElevationMm = 0.0
            }
        };
        var auditBeamNoEnd = engine.AuditItem(itemBeam, actualBeamNoEnd);
        Assert(auditBeamNoEnd.Status == GeometricVerificationStatus.Inconclusive,
            "1.6 Beam with null CurveEnd returns Inconclusive");
    }

    private static void TestToleranceBoundaries()
    {
        Console.WriteLine("\n--- Group 2: Tolerance Boundary Behavior ---");
        var tol = new CoordinationTolerances
        {
            SectionDimensionMm = 0.0,
            ElevationMm = 1.0,
            PlanPositionMm = 1.0,
            PileCenterMm = 2.0
        };
        var engine = new MeasuredGeometricEngine(tol);

        // 2.1 Elevation exactly at tolerance (delta = 1.0mm) -> PASS
        var itemBeam = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
            Category = CoordinationCategory.Beam,
            Mark = "B1",
            Geometry = new LineGeometrySpec { Start = new(0, 0, 3000), End = new(4000, 0, 3000), ElevationMm = 3000.0 }
        };
        var actExactElev = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B1"] = new()
            {
                Key = "Doc#Beam:B1", CategoryName = "Beam", HasGeometry = true,
                CurveStart = new(0, 0, 3000), CurveEnd = new(4000, 0, 3000), TopElevationMm = 3001.0
            }
        };
        Assert(engine.AuditItem(itemBeam, actExactElev).Status == GeometricVerificationStatus.Verified,
            "2.1 Beam elevation delta exactly equal to 1.0mm passes (strict <=)");

        // 2.2 Elevation just over tolerance (delta = 1.001mm) -> DEFECT
        var actOverElev = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B1"] = new()
            {
                Key = "Doc#Beam:B1", CategoryName = "Beam", HasGeometry = true,
                CurveStart = new(0, 0, 3000), CurveEnd = new(4000, 0, 3000), TopElevationMm = 3001.001
            }
        };
        Assert(engine.AuditItem(itemBeam, actOverElev).Status == GeometricVerificationStatus.Defect,
            "2.2 Beam elevation delta 1.001mm fails (exceeds tolerance)");

        // 2.3 Pile Centerline exactly at tolerance (delta = 2.0mm) -> PASS
        var itemPile = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0), OffsetZMm = 0.0 }
        };
        var actExactPile = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new()
            {
                Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true,
                Centroid = new Point3D(2.0, 0, 0), TopElevationMm = 0.0
            }
        };
        Assert(engine.AuditItem(itemPile, actExactPile).Status == GeometricVerificationStatus.Verified,
            "2.3 Pile centerline offset exactly 2.0mm passes (strict <=)");

        // 2.4 Pile Centerline just over tolerance (delta = 2.001mm) -> DEFECT
        var actOverPile = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P1"] = new()
            {
                Key = "Doc#Pile:P1", CategoryName = "Pile", HasGeometry = true,
                Centroid = new Point3D(2.001, 0, 0), TopElevationMm = 0.0
            }
        };
        Assert(engine.AuditItem(itemPile, actOverPile).Status == GeometricVerificationStatus.Defect,
            "2.4 Pile centerline offset 2.001mm fails");

        // 2.5 Section dimension with SectionDimensionMm = 0.0:
        // delta == 0.0 -> PASS; delta == 0.001 -> DEFECT
        var itemCol = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
            Resource = new ResourceResolutionSpec
            {
                DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220.0, ["h"] = 220.0 }
            }
        };
        var actExactDim = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Column:C1"] = new()
            {
                Key = "Doc#Column:C1", CategoryName = "Column", HasGeometry = true,
                Centroid = new Point3D(0, 0, 0), WidthMm = 220.0, HeightMm = 220.0
            }
        };
        var actOverDim = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Column:C1"] = new()
            {
                Key = "Doc#Column:C1", CategoryName = "Column", HasGeometry = true,
                Centroid = new Point3D(0, 0, 0), WidthMm = 220.001, HeightMm = 220.0
            }
        };
        Assert(engine.AuditItem(itemCol, actExactDim).Status == GeometricVerificationStatus.Verified,
            "2.5 Section dimension delta 0.0mm with 0.0mm tolerance passes");
        Assert(engine.AuditItem(itemCol, actOverDim).Status == GeometricVerificationStatus.Defect,
            "2.6 Section dimension delta 0.001mm with 0.0mm tolerance fails");

        // 2.7 Orthogonal coupling: dx=0.8mm (<=1.0), dz=0.8mm (<=1.0).
        // Distance3D = sqrt(0.8^2 + 0.8^2) = 1.131mm > 1.0mm!
        var itemCoupled = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B_COUPLED"),
            Category = CoordinationCategory.Beam,
            Mark = "B_COUPLED",
            Geometry = new LineGeometrySpec { Start = new(0, 0, 3000), End = new(4000, 0, 3000), ElevationMm = 3000.0 }
        };
        var actCoupled = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B_COUPLED"] = new()
            {
                Key = "Doc#Beam:B_COUPLED", CategoryName = "Beam", HasGeometry = true,
                CurveStart = new(0.8, 0, 3000.8), CurveEnd = new(4000.8, 0, 3000.8), TopElevationMm = 3000.8
            }
        };
        var auditCoupled = engine.AuditItem(itemCoupled, actCoupled);
        // Note: deltaElev = 0.8mm <= 1.0mm, plan dx = 0.8mm <= 1.0mm, but Distance3D is 1.13mm!
        bool distanceCouplingFailed = auditCoupled.Status == GeometricVerificationStatus.Defect;
        Console.WriteLine($"[ADVERSARIAL COUPLING] Beam with dx=0.8mm (<=1mm) and dz=0.8mm (<=1mm) resulted in: {auditCoupled.Status} (DeltaCurveStart={auditCoupled.DeltaCurveStartMm:F3}mm, DistanceCouplingFails={distanceCouplingFailed})");

        // 2.8 Beam with 2D plan coordinates (Z=0 in spec) on elevated level (Z=3800 in Revit)
        var itemPlan2D = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B_ELEV2D"),
            Category = CoordinationCategory.Beam,
            Mark = "B_ELEV2D",
            Geometry = new LineGeometrySpec { Start = new(0, 0, 0), End = new(4000, 0, 0), ElevationMm = 3800.0 }
        };
        var actElevRvt = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B_ELEV2D"] = new()
            {
                Key = "Doc#Beam:B_ELEV2D", CategoryName = "Beam", HasGeometry = true,
                CurveStart = new(0, 0, 3800), CurveEnd = new(4000, 0, 3800), TopElevationMm = 3800.0
            }
        };
        var auditPlan2D = engine.AuditItem(itemPlan2D, actElevRvt);
        bool plan2DFails = auditPlan2D.Status == GeometricVerificationStatus.Defect;
        Console.WriteLine($"[ADVERSARIAL 2D/3D] Beam with planned Start.Z=0 and Revit CurveStart.Z=3800 resulted in: {auditPlan2D.Status} (DeltaCurveStart={auditPlan2D.DeltaCurveStartMm:F2}mm, Fails={plan2DFails})");
    }

    private static void TestAmbiguousAndDuplicateMarks()
    {
        Console.WriteLine("\n--- Group 3: Ambiguous or Duplicate Marks vs TargetUniqueId ---");
        var engine = new MeasuredGeometricEngine();

        // Scenario: Two columns have IDENTICAL Mark = "C1", but different coordinates and TargetUniqueIds
        var itemColA = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1_North"),
            TargetUniqueId = "REVIT-UID-NORTH",
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 10000, 0) }
        };
        var itemColB = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1_South"),
            TargetUniqueId = "REVIT-UID-SOUTH",
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["REVIT-UID-NORTH"] = new()
            {
                Key = "Doc#Column:C1_North", UniqueId = "REVIT-UID-NORTH", CategoryName = "Column", HasGeometry = true,
                Centroid = new Point3D(0, 10000, 0)
            },
            ["REVIT-UID-SOUTH"] = new()
            {
                Key = "Doc#Column:C1_South", UniqueId = "REVIT-UID-SOUTH", CategoryName = "Column", HasGeometry = true,
                Centroid = new Point3D(0, 0, 0)
            }
        };

        var auditA = engine.AuditItem(itemColA, actuals);
        var auditB = engine.AuditItem(itemColB, actuals);

        Assert(auditA.Status == GeometricVerificationStatus.Verified,
            "3.1 Column North matched exact instance via TargetUniqueId despite duplicate Mark 'C1'");
        Assert(auditA.TargetUniqueId == "REVIT-UID-NORTH",
            "3.2 Column North TargetUniqueId preserved");
        Assert(auditB.Status == GeometricVerificationStatus.Verified,
            "3.3 Column South matched exact instance via TargetUniqueId despite duplicate Mark 'C1'");
        Assert(auditB.TargetUniqueId == "REVIT-UID-SOUTH",
            "3.4 Column South TargetUniqueId preserved");

        // Now test AuditPlan with list overload
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "plan-dup-marks",
            Items = new[] { itemColA, itemColB }
        };
        var actualsList = new List<ActualElementGeometry>
        {
            actuals["REVIT-UID-NORTH"],
            actuals["REVIT-UID-SOUTH"]
        };
        var report = engine.AuditPlan(plan, actualsList);
        Assert(report.OverallPassed,
            "3.5 AuditPlan list overload correctly resolves both items with duplicate marks via TargetUniqueId");
        Assert(report.VerifiedCount == 2,
            "3.6 Both elements successfully verified without cross-contamination");
    }

    private static void TestExtremeAndPathologicalInputs()
    {
        Console.WriteLine("\n--- Group 4: Extreme & Pathological Inputs ---");
        var engine = new MeasuredGeometricEngine();

        // 4.1 VN-2000 Extreme Geographic Coordinates
        double bigX = 584_000_000.0;
        double bigY = 1_195_000_000.0;
        var itemVn2000 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("VN2000", CoordinationCategory.Column, "C_BIG"),
            Category = CoordinationCategory.Column,
            Mark = "C_BIG",
            Geometry = new PointGeometrySpec { Location = new Point3D(bigX, bigY, 0) }
        };
        var actVn2000 = new Dictionary<string, ActualElementGeometry>
        {
            ["VN2000#Column:C_BIG"] = new()
            {
                Key = "VN2000#Column:C_BIG", CategoryName = "Column", HasGeometry = true,
                Centroid = new Point3D(bigX + 0.5, bigY - 0.5, 0)
            }
        };
        var auditVn2000 = engine.AuditItem(itemVn2000, actVn2000);
        Assert(auditVn2000.Status == GeometricVerificationStatus.Verified,
            "4.1 Extreme coordinate scale (10^9 mm) computes precision accurately within 1mm");

        // 4.2 Negative dimensions / elevations
        var itemDeepPile = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P_DEEP"),
            Category = CoordinationCategory.Pile,
            Mark = "P_DEEP",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, -45000.0), OffsetZMm = -45000.0 }
        };
        var actDeepPile = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Pile:P_DEEP"] = new()
            {
                Key = "Doc#Pile:P_DEEP", CategoryName = "Pile", HasGeometry = true,
                Centroid = new Point3D(0, 0, -45000.0), TopElevationMm = -45000.8
            }
        };
        var auditDeepPile = engine.AuditItem(itemDeepPile, actDeepPile);
        Assert(auditDeepPile.Status == GeometricVerificationStatus.Verified,
            "4.2 Deep negative elevation (-45m) computes cutoff error cleanly (0.8mm <= 1.0mm)");

        // 4.3 Element missing in model
        var itemMissing = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C_GHOST"),
            Category = CoordinationCategory.Column,
            Mark = "C_GHOST",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };
        var auditMissing = engine.AuditItem(itemMissing, new Dictionary<string, ActualElementGeometry>());
        Assert(auditMissing.Status == GeometricVerificationStatus.Missing,
            "4.3 Non-existent element returns Missing status");
    }

    private static void TestAuditReportEdgeCases()
    {
        Console.WriteLine("\n--- Group 5: Audit Report Aggregate Edge Cases ---");
        var engine = new MeasuredGeometricEngine();

        // 5.1 Empty Plan items cannot pass
        var emptyPlan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "empty-plan",
            Items = Array.Empty<ElementPlanItem>()
        };
        var emptyReport = engine.AuditPlan(emptyPlan, new Dictionary<string, ActualElementGeometry>());
        Assert(!emptyReport.OverallPassed,
            "5.1 Empty plan report OverallPassed is strictly false");
        Assert(emptyReport.TotalPlannedElements == 0,
            "5.2 TotalPlannedElements is 0");

        // 5.2 Plan with 1 Verified and 1 Inconclusive -> OverallPassed must be false
        var item1 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Level, "L1"),
            Category = CoordinationCategory.Level,
            Mark = "L1",
            Geometry = new LevelGeometrySpec { ElevationM = 0.0 }
        };
        var item2 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", (CoordinationCategory)888, "GEN"),
            Category = (CoordinationCategory)888,
            Mark = "GEN",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };

        var mixedActuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Level:L1"] = new() { Key = "Doc#Level:L1", CategoryName = "Level", HasGeometry = true, TopElevationMm = 0.0 },
            ["Doc#888:GEN"] = new() { Key = "Doc#888:GEN", HasGeometry = true, Centroid = new Point3D(0, 0, 0) }
        };

        var mixedPlan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "mixed-plan",
            Items = new[] { item1, item2 }
        };

        var mixedReport = engine.AuditPlan(mixedPlan, mixedActuals);
        Assert(!mixedReport.OverallPassed,
            "5.3 Plan with 1 Verified and 1 Inconclusive yields OverallPassed = false");
        Assert(mixedReport.VerifiedCount == 1,
            "5.4 VerifiedCount is 1");
        Assert(mixedReport.InconclusiveCount == 1,
            "5.5 InconclusiveCount is 1");
    }
}
