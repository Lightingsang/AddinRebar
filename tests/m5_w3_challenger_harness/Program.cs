namespace M5W3ChallengerHarness;

using System;
using System.Collections.Generic;
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
        Console.WriteLine("======================================================================");
        Console.WriteLine(" EMPIRICAL CHALLENGER HARNESS: Milestone M5 Geometric Verification   ");
        Console.WriteLine("======================================================================");

        TestMissingPlannedGeometry();
        TestMismatchedPlannedGeometryType();
        TestNaNAndInfinityMeasurements();
        TestDegenerateProfileLoops();
        TestRequiredMeasurementsContract();
        TestTargetUniqueIdResolutionPriority();
        TestReportAggregationAndRule0ExitCode();

        Console.WriteLine("\n======================================================================");
        Console.WriteLine($" RESULTS: Total: {_total} | Passed: {_passed} | Failed: {_failed}");
        Console.WriteLine("======================================================================");

        if (_findings.Count > 0)
        {
            Console.WriteLine("\nCRITICAL EMPIRICAL FINDINGS & VULNERABILITIES:");
            for (int i = 0; i < _findings.Count; i++)
            {
                Console.WriteLine($" [{i + 1}] {_findings[i]}");
            }
        }

        return _failed == 0 ? 0 : 1;
    }

    private static void Check(bool condition, string testName, string? failureReason = null)
    {
        _total++;
        if (condition)
        {
            _passed++;
            Console.WriteLine($" [PASS] {testName}");
        }
        else
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" [FAIL] {testName} - {failureReason ?? "Condition failed"}");
            Console.ResetColor();
            _findings.Add($"{testName}: {failureReason ?? "Condition failed"}");
        }
    }

    private static MeasuredGeometricEngine CreateEngine() => new(new CoordinationTolerances
    {
        SectionDimensionMm = 0.0,
        ElevationMm = 1.0,
        PlanPositionMm = 1.0,
        PileCenterMm = 2.0
    });

    private static void TestMissingPlannedGeometry()
    {
        Console.WriteLine("\n--- Attack Surface 1: Null Planned Geometry (item.Geometry == null) ---");
        var engine = CreateEngine();

        // 1.1 Column with Geometry == null
        var colItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = null
        };
        var colActual = new ActualElementGeometry
        {
            Key = "Doc#Column:C1",
            CategoryName = "Column",
            HasGeometry = true,
            Centroid = new Point3D(5000, 5000, 0),
            WidthMm = 220,
            HeightMm = 220
        };
        var colAudit = engine.AuditItem(colItem, new Dictionary<string, ActualElementGeometry> { [colActual.Key] = colActual });
        // Under Rule 0: missing baseline geometry cannot be verified!
        bool colIsSafe = colAudit.Status != GeometricVerificationStatus.Verified;
        Check(colIsSafe, "1.1 Column with Geometry == null must NOT be Verified",
            $"Actual status was '{colAudit.Status}' (falsely verified with 0 discrepancies despite missing planned geometry)");

        // 1.2 Pile with Geometry == null
        var pileItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = null
        };
        var pileActual = new ActualElementGeometry
        {
            Key = "Doc#Pile:P1",
            CategoryName = "Pile",
            HasGeometry = true,
            Centroid = new Point3D(1000, 2000, -2500),
            TopElevationMm = -2500
        };
        var pileAudit = engine.AuditItem(pileItem, new Dictionary<string, ActualElementGeometry> { [pileActual.Key] = pileActual });
        bool pileIsSafe = pileAudit.Status != GeometricVerificationStatus.Verified;
        Check(pileIsSafe, "1.2 Pile with Geometry == null must NOT be Verified",
            $"Actual status was '{pileAudit.Status}' (falsely verified with 0 discrepancies despite missing planned geometry)");

        // 1.3 Beam with Geometry == null
        var beamItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
            Category = CoordinationCategory.Beam,
            Mark = "B1",
            Geometry = null
        };
        var beamActual = new ActualElementGeometry
        {
            Key = "Doc#Beam:B1",
            CategoryName = "Beam",
            HasGeometry = true,
            CurveStart = new Point3D(0, 0, 3800),
            CurveEnd = new Point3D(5000, 0, 3800),
            TopElevationMm = 3800
        };
        var beamAudit = engine.AuditItem(beamItem, new Dictionary<string, ActualElementGeometry> { [beamActual.Key] = beamActual });
        bool beamIsSafe = beamAudit.Status != GeometricVerificationStatus.Verified;
        Check(beamIsSafe, "1.3 Beam with Geometry == null must NOT be Verified",
            $"Actual status was '{beamAudit.Status}' (falsely verified with 0 discrepancies despite missing planned geometry)");

        // 1.4 Slab with Geometry == null
        var slabItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, "S1"),
            Category = CoordinationCategory.Slab,
            Mark = "S1",
            Geometry = null
        };
        var slabActual = new ActualElementGeometry
        {
            Key = "Doc#Slab:S1",
            CategoryName = "Slab",
            HasGeometry = true,
            Centroid = new Point3D(2500, 2500, 0),
            TopElevationMm = 0,
            ThicknessMm = 120
        };
        var slabAudit = engine.AuditItem(slabItem, new Dictionary<string, ActualElementGeometry> { [slabActual.Key] = slabActual });
        bool slabIsSafe = slabAudit.Status != GeometricVerificationStatus.Verified;
        Check(slabIsSafe, "1.4 Slab with Geometry == null must NOT be Verified",
            $"Actual status was '{slabAudit.Status}' (falsely verified with 0 discrepancies despite missing planned geometry)");

        // 1.5 Wall with Geometry == null
        var wallItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Wall, "W1"),
            Category = CoordinationCategory.Wall,
            Mark = "W1",
            Geometry = null
        };
        var wallActual = new ActualElementGeometry
        {
            Key = "Doc#Wall:W1",
            CategoryName = "Wall",
            HasGeometry = true,
            CurveStart = new Point3D(0, 0, 0),
            CurveEnd = new Point3D(3000, 0, 0),
            ThicknessMm = 220
        };
        var wallAudit = engine.AuditItem(wallItem, new Dictionary<string, ActualElementGeometry> { [wallActual.Key] = wallActual });
        bool wallIsSafe = wallAudit.Status != GeometricVerificationStatus.Verified;
        Check(wallIsSafe, "1.5 Wall with Geometry == null must NOT be Verified",
            $"Actual status was '{wallAudit.Status}' (falsely verified with 0 discrepancies despite missing planned geometry)");

        // 1.6 Level with Geometry == null
        var lvlItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Level, "L1"),
            Category = CoordinationCategory.Level,
            Mark = "L1",
            Geometry = null
        };
        var lvlActual = new ActualElementGeometry
        {
            Key = "Doc#Level:L1",
            CategoryName = "Level",
            HasGeometry = true,
            TopElevationMm = 3800
        };
        var lvlAudit = engine.AuditItem(lvlItem, new Dictionary<string, ActualElementGeometry> { [lvlActual.Key] = lvlActual });
        bool lvlIsSafe = lvlAudit.Status != GeometricVerificationStatus.Verified;
        Check(lvlIsSafe, "1.6 Level with Geometry == null must NOT be Verified",
            $"Actual status was '{lvlAudit.Status}' (falsely verified with 0 discrepancies despite missing planned geometry)");

        // 1.7 Grid with Geometry == null
        var gridItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Grid, "G1"),
            Category = CoordinationCategory.Grid,
            Mark = "G1",
            Geometry = null
        };
        var gridActual = new ActualElementGeometry
        {
            Key = "Doc#Grid:G1",
            CategoryName = "Grid",
            HasGeometry = true,
            CurveStart = new Point3D(0, 0, 0),
            CurveEnd = new Point3D(0, 18000, 0)
        };
        var gridAudit = engine.AuditItem(gridItem, new Dictionary<string, ActualElementGeometry> { [gridActual.Key] = gridActual });
        bool gridIsSafe = gridAudit.Status != GeometricVerificationStatus.Verified;
        Check(gridIsSafe, "1.7 Grid with Geometry == null must NOT be Verified",
            $"Actual status was '{gridAudit.Status}' (falsely verified with 0 discrepancies despite missing planned geometry)");
    }

    private static void TestMismatchedPlannedGeometryType()
    {
        Console.WriteLine("\n--- Attack Surface 2: Mismatched Planned Geometry Types ---");
        var engine = CreateEngine();

        // 2.1 Column with LineGeometrySpec instead of PointGeometrySpec
        var colItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(0, 0, 3800) }
        };
        var colActual = new ActualElementGeometry
        {
            Key = "Doc#Column:C1",
            CategoryName = "Column",
            HasGeometry = true,
            Centroid = new Point3D(9999, 9999, 0)
        };
        var audit = engine.AuditItem(colItem, new Dictionary<string, ActualElementGeometry> { [colActual.Key] = colActual });
        bool isSafe = audit.Status != GeometricVerificationStatus.Verified;
        Check(isSafe, "2.1 Column with LineGeometrySpec instead of PointGeometrySpec must NOT be Verified",
            $"Actual status was '{audit.Status}' at position (9999, 9999) because type mismatch bypassed location check");

        // 2.2 Beam with PointGeometrySpec instead of LineGeometrySpec
        var beamItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
            Category = CoordinationCategory.Beam,
            Mark = "B1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };
        var beamActual = new ActualElementGeometry
        {
            Key = "Doc#Beam:B1",
            CategoryName = "Beam",
            HasGeometry = true,
            CurveStart = new Point3D(5000, 5000, 3800),
            CurveEnd = new Point3D(9000, 9000, 3800),
            TopElevationMm = 3800
        };
        var beamAudit = engine.AuditItem(beamItem, new Dictionary<string, ActualElementGeometry> { [beamActual.Key] = beamActual });
        bool beamIsSafe = beamAudit.Status != GeometricVerificationStatus.Verified;
        Check(beamIsSafe, "2.2 Beam with PointGeometrySpec instead of LineGeometrySpec must NOT be Verified",
            $"Actual status was '{beamAudit.Status}' because type mismatch bypassed endpoint and elevation check");
    }

    private static void TestNaNAndInfinityMeasurements()
    {
        Console.WriteLine("\n--- Attack Surface 3: NaN and Infinity Measurements in Actuals ---");
        var engine = CreateEngine();

        // 3.1 Pile with NaN Centroid
        var pileItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = new PointGeometrySpec { Location = new Point3D(1000, 2000, -2500), OffsetZMm = -2500.0 }
        };
        var nanPileActual = new ActualElementGeometry
        {
            Key = "Doc#Pile:P1",
            CategoryName = "Pile",
            HasGeometry = true,
            Centroid = new Point3D(double.NaN, 2000, -2500),
            TopElevationMm = -2500.0
        };
        var pileAudit = engine.AuditItem(pileItem, new Dictionary<string, ActualElementGeometry> { [nanPileActual.Key] = nanPileActual });
        bool pileNanSafe = pileAudit.Status == GeometricVerificationStatus.Defect || pileAudit.Status == GeometricVerificationStatus.Inconclusive;
        Check(pileNanSafe, "3.1 Pile with NaN Centroid must NOT be Verified",
            $"Actual status was '{pileAudit.Status}', delta={pileAudit.DeltaCenterLineMm}");

        // 3.2 Pile with NaN Cutoff Elevation
        var nanElevPileActual = new ActualElementGeometry
        {
            Key = "Doc#Pile:P1",
            CategoryName = "Pile",
            HasGeometry = true,
            Centroid = new Point3D(1000, 2000, -2500),
            TopElevationMm = double.NaN
        };
        var pileElevAudit = engine.AuditItem(pileItem, new Dictionary<string, ActualElementGeometry> { [nanElevPileActual.Key] = nanElevPileActual });
        bool pileElevSafe = pileElevAudit.Status == GeometricVerificationStatus.Defect || pileElevAudit.Status == GeometricVerificationStatus.Inconclusive;
        Check(pileElevSafe, "3.2 Pile with NaN TopElevationMm must NOT be Verified",
            $"Actual status was '{pileElevAudit.Status}', delta={pileElevAudit.DeltaElevationMm}");

        // 3.3 Column with NaN Width
        var colItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
            Resource = new ResourceResolutionSpec
            {
                DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220, ["h"] = 220 }
            }
        };
        var nanColActual = new ActualElementGeometry
        {
            Key = "Doc#Column:C1",
            CategoryName = "Column",
            HasGeometry = true,
            Centroid = new Point3D(0, 0, 0),
            WidthMm = double.NaN,
            HeightMm = 220
        };
        var colAudit = engine.AuditItem(colItem, new Dictionary<string, ActualElementGeometry> { [nanColActual.Key] = nanColActual });
        bool colNanSafe = colAudit.Status == GeometricVerificationStatus.Defect || colAudit.Status == GeometricVerificationStatus.Inconclusive;
        Check(colNanSafe, "3.3 Column with NaN WidthMm must NOT be Verified",
            $"Actual status was '{colAudit.Status}', deltaWidth={colAudit.DeltaWidthMm}");

        // 3.4 Beam with Infinity Elevation
        var beamItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
            Category = CoordinationCategory.Beam,
            Mark = "B1",
            Geometry = new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(5000, 0, 0), ElevationMm = 3800 }
        };
        var infBeamActual = new ActualElementGeometry
        {
            Key = "Doc#Beam:B1",
            CategoryName = "Beam",
            HasGeometry = true,
            CurveStart = new Point3D(0, 0, 0),
            CurveEnd = new Point3D(5000, 0, 0),
            TopElevationMm = double.PositiveInfinity
        };
        var beamAudit = engine.AuditItem(beamItem, new Dictionary<string, ActualElementGeometry> { [infBeamActual.Key] = infBeamActual });
        bool beamInfSafe = beamAudit.Status == GeometricVerificationStatus.Defect;
        Check(beamInfSafe, "3.4 Beam with Infinity TopElevationMm must be Defect",
            $"Actual status was '{beamAudit.Status}'");
    }

    private static void TestDegenerateProfileLoops()
    {
        Console.WriteLine("\n--- Attack Surface 4: Degenerate Profile Outer Loops ---");
        var engine = CreateEngine();

        // 4.1 Closed loop with 3 points where last == first (P0, P1, P0)
        // This causes Polygon2D to trim to 2 points and throw ArgumentException!
        var itemP0P1P0 = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, "S_DEG1"),
            Category = CoordinationCategory.Slab,
            Mark = "S_DEG1",
            Geometry = new ProfileGeometrySpec
            {
                OuterLoop = new[] { new Point3D(0, 0, 0), new Point3D(1000, 0, 0), new Point3D(0, 0, 0) },
                ElevationMm = 0,
                ThicknessMm = 150
            }
        };
        var actualSlab = new ActualElementGeometry
        {
            Key = "Doc#Slab:S_DEG1",
            CategoryName = "Slab",
            HasGeometry = true,
            Centroid = new Point3D(500, 0, 0),
            TopElevationMm = 0,
            ThicknessMm = 150
        };

        bool threwCrash = false;
        try
        {
            var audit = engine.AuditItem(itemP0P1P0, new Dictionary<string, ActualElementGeometry> { [actualSlab.Key] = actualSlab });
        }
        catch (ArgumentException ex)
        {
            threwCrash = true;
            Console.WriteLine($"   [CRASH REPRODUCED] Polygon2D threw: {ex.Message}");
        }
        catch (Exception ex)
        {
            threwCrash = true;
            Console.WriteLine($"   [UNEXPECTED EXCEPTION]: {ex}");
        }

        Check(!threwCrash, "4.1 Slab with 3-point closed loop (P0, P1, P0) must NOT crash with unhandled ArgumentException",
            "Unhandled ArgumentException occurred inside Polygon2D constructor when loop has 3 points and last equals first");

        // 4.2 Collinear 3 points in loop (zero area polygon)
        var itemCollinear = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, "S_COLL"),
            Category = CoordinationCategory.Slab,
            Mark = "S_COLL",
            Geometry = new ProfileGeometrySpec
            {
                OuterLoop = new[] { new Point3D(0, 0, 0), new Point3D(2000, 0, 0), new Point3D(4000, 0, 0) },
                ElevationMm = 0,
                ThicknessMm = 150
            }
        };
        bool collinearSurvived = false;
        try
        {
            var audit = engine.AuditItem(itemCollinear, new Dictionary<string, ActualElementGeometry> { [actualSlab.Key] = actualSlab });
            collinearSurvived = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   [COLLINEAR CRASH]: {ex.Message}");
        }
        Check(collinearSurvived, "4.2 Collinear 3 points in profile loop must be handled without crash");
    }

    private static void TestRequiredMeasurementsContract()
    {
        Console.WriteLine("\n--- Attack Surface 5: Required Measurements Contract (Rule 0) ---");
        var engine = CreateEngine();

        // 5.1 Pile without TopElevationMm -> must be Inconclusive
        var pileItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = new PointGeometrySpec { Location = new Point3D(1000, 2000, -2500), OffsetZMm = -2500 }
        };
        var pileNoElev = new ActualElementGeometry
        {
            Key = "Doc#Pile:P1",
            CategoryName = "Pile",
            HasGeometry = true,
            Centroid = new Point3D(1000, 2000, -2500),
            TopElevationMm = null
        };
        var audit51 = engine.AuditItem(pileItem, new Dictionary<string, ActualElementGeometry> { [pileNoElev.Key] = pileNoElev });
        Check(audit51.Status == GeometricVerificationStatus.Inconclusive, "5.1 Pile without TopElevationMm must be Inconclusive",
            $"Status was {audit51.Status}");

        // 5.2 Column without Centroid -> must be Inconclusive
        var colItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };
        var colNoCentroid = new ActualElementGeometry
        {
            Key = "Doc#Column:C1",
            CategoryName = "Column",
            HasGeometry = true,
            Centroid = null
        };
        var audit52 = engine.AuditItem(colItem, new Dictionary<string, ActualElementGeometry> { [colNoCentroid.Key] = colNoCentroid });
        Check(audit52.Status == GeometricVerificationStatus.Inconclusive, "5.2 Column without Centroid must be Inconclusive",
            $"Status was {audit52.Status}");

        // 5.3 Beam without CurveStart / CurveEnd -> must be Inconclusive
        var beamItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
            Category = CoordinationCategory.Beam,
            Mark = "B1",
            Geometry = new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(5000, 0, 0) }
        };
        var beamNoCurves = new ActualElementGeometry
        {
            Key = "Doc#Beam:B1",
            CategoryName = "Beam",
            HasGeometry = true,
            CurveStart = null,
            CurveEnd = null,
            TopElevationMm = 3800
        };
        var audit53 = engine.AuditItem(beamItem, new Dictionary<string, ActualElementGeometry> { [beamNoCurves.Key] = beamNoCurves });
        Check(audit53.Status == GeometricVerificationStatus.Inconclusive, "5.3 Beam without CurveStart/CurveEnd must be Inconclusive",
            $"Status was {audit53.Status}");

        // 5.4 Beam without TopElevationMm -> must be Inconclusive
        var beamNoElev = new ActualElementGeometry
        {
            Key = "Doc#Beam:B1",
            CategoryName = "Beam",
            HasGeometry = true,
            CurveStart = new Point3D(0, 0, 0),
            CurveEnd = new Point3D(5000, 0, 0),
            TopElevationMm = null
        };
        var audit54 = engine.AuditItem(beamItem, new Dictionary<string, ActualElementGeometry> { [beamNoElev.Key] = beamNoElev });
        Check(audit54.Status == GeometricVerificationStatus.Inconclusive, "5.4 Beam without TopElevationMm must be Inconclusive",
            $"Status was {audit54.Status}");

        // 5.5 Wall without CurveStart / CurveEnd -> must be Inconclusive
        var wallItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Wall, "W1"),
            Category = CoordinationCategory.Wall,
            Mark = "W1",
            Geometry = new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(3000, 0, 0) }
        };
        var wallNoCurves = new ActualElementGeometry
        {
            Key = "Doc#Wall:W1",
            CategoryName = "Wall",
            HasGeometry = true,
            CurveStart = null,
            CurveEnd = null
        };
        var audit55 = engine.AuditItem(wallItem, new Dictionary<string, ActualElementGeometry> { [wallNoCurves.Key] = wallNoCurves });
        Check(audit55.Status == GeometricVerificationStatus.Inconclusive, "5.5 Wall without CurveStart/CurveEnd must be Inconclusive",
            $"Status was {audit55.Status}");

        // 5.6 Slab with ThicknessMm > 0 in plan but null in actual -> must be Inconclusive
        var slabItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, "S1"),
            Category = CoordinationCategory.Slab,
            Mark = "S1",
            Geometry = new ProfileGeometrySpec
            {
                OuterLoop = new[] { new Point3D(0, 0, 0), new Point3D(2000, 0, 0), new Point3D(2000, 2000, 0), new Point3D(0, 2000, 0) },
                ThicknessMm = 150.0,
                ElevationMm = 0.0
            }
        };
        var slabNoThick = new ActualElementGeometry
        {
            Key = "Doc#Slab:S1",
            CategoryName = "Slab",
            HasGeometry = true,
            Centroid = new Point3D(1000, 1000, 0),
            TopElevationMm = 0.0,
            ThicknessMm = null
        };
        var audit56 = engine.AuditItem(slabItem, new Dictionary<string, ActualElementGeometry> { [slabNoThick.Key] = slabNoThick });
        Check(audit56.Status == GeometricVerificationStatus.Inconclusive, "5.6 Slab missing required ThicknessMm must be Inconclusive",
            $"Status was {audit56.Status}");

        // 5.7 Unmeasured category (e.g. Opening, Rebar, Pipe, Generic) -> must be Inconclusive
        var genericItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Opening, "OP1"),
            Category = CoordinationCategory.Opening,
            Mark = "OP1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };
        var genericActual = new ActualElementGeometry
        {
            Key = "Doc#Opening:OP1",
            CategoryName = "Opening",
            HasGeometry = true,
            Centroid = new Point3D(0, 0, 0)
        };
        var audit57 = engine.AuditItem(genericItem, new Dictionary<string, ActualElementGeometry> { [genericActual.Key] = genericActual });
        Check(audit57.Status == GeometricVerificationStatus.Inconclusive, "5.7 Unmeasured Opening category must be Inconclusive under Rule 0",
            $"Status was {audit57.Status}");
    }

    private static void TestTargetUniqueIdResolutionPriority()
    {
        Console.WriteLine("\n--- Attack Surface 6: TargetUniqueId Direct Resolution Priority ---");
        var engine = CreateEngine();

        // 6.1 TargetUniqueId takes precedence when Mark is duplicate or ambiguous
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            TargetUniqueId = "revit-guid-correct-column",
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
            Resource = new ResourceResolutionSpec
            {
                DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220, ["h"] = 220 }
            }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["revit-guid-correct-column"] = new()
            {
                Key = "revit-guid-correct-column",
                UniqueId = "revit-guid-correct-column",
                CategoryName = "Column",
                HasGeometry = true,
                Centroid = new Point3D(0, 0, 0),
                WidthMm = 220,
                HeightMm = 220
            },
            ["C1"] = new()
            {
                Key = "C1",
                UniqueId = "revit-guid-wrong-column-by-mark",
                CategoryName = "Column",
                HasGeometry = true,
                Centroid = new Point3D(9999, 9999, 0),
                WidthMm = 500,
                HeightMm = 500
            }
        };

        var audit = engine.AuditItem(item, actuals);
        Check(audit.Status == GeometricVerificationStatus.Verified && audit.TargetUniqueId == "revit-guid-correct-column",
            "6.1 TargetUniqueId takes precedence over duplicate Mark lookup in actuals",
            $"Resolved TargetUniqueId was '{audit.TargetUniqueId}', status={audit.Status}");

        // 6.2 TargetUniqueId missing in actuals falls back to SourceKey/Mark
        var itemFallback = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            TargetUniqueId = "non-existent-guid",
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };
        var auditFallback = engine.AuditItem(itemFallback, actuals);
        // "non-existent-guid" is not in dict, so FindActual falls back to "C1" (wrong column at 9999, 9999) -> Defect!
        Check(auditFallback.Status == GeometricVerificationStatus.Defect,
            "6.2 When TargetUniqueId is non-existent, fallback to Mark detects out-of-tolerance defect",
            $"Status was {auditFallback.Status}");
    }

    private static void TestReportAggregationAndRule0ExitCode()
    {
        Console.WriteLine("\n--- Attack Surface 7: GeometricAuditReport Aggregation & Rule 0 ---");
        var engine = CreateEngine();

        var itemVerified = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Level, "L1"),
            Category = CoordinationCategory.Level,
            Mark = "L1",
            Geometry = new LevelGeometrySpec { ElevationM = 0.0 }
        };
        var itemInconclusive = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Opening, "OP1"),
            Category = CoordinationCategory.Opening,
            Mark = "OP1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };

        var actuals = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Level:L1"] = new() { Key = "Doc#Level:L1", CategoryName = "Level", HasGeometry = true, TopElevationMm = 0.0 },
            ["Doc#Opening:OP1"] = new() { Key = "Doc#Opening:OP1", CategoryName = "Opening", HasGeometry = true, Centroid = new Point3D(0, 0, 0) }
        };

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-agg",
            Items = new[] { itemVerified, itemInconclusive }
        };

        var report = engine.AuditPlan(plan, actuals);

        // OverallPassed must be FALSE because of the Inconclusive item!
        Check(!report.OverallPassed, "7.1 OverallPassed must be FALSE when an Inconclusive item exists (Rule 0)",
            $"OverallPassed was {report.OverallPassed}");
        Check(report.InconclusiveCount == 1, "7.2 InconclusiveCount must be exactly 1",
            $"Count was {report.InconclusiveCount}");
        Check(report.VerifiedCount == 1, "7.3 VerifiedCount must be exactly 1",
            $"Count was {report.VerifiedCount}");
    }
}
