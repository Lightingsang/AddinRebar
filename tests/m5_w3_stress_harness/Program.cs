namespace M5W3StressHarness;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        Console.WriteLine("==========================================================================");
        Console.WriteLine(" EMPIRICAL ADVERSARIAL STRESS HARNESS: M5 Geometric Verification & Polygon2D");
        Console.WriteLine("==========================================================================");

        TestPolygon2DDegenerateConditions();
        TestPolygon2DCollinearAndDuplicateVertices();
        TestPolygon2DScaleAndLargeVertexCounts();
        TestPolygon2DNaNAndInfinityBehavior();
        TestMeasuredGeometricEngineProfileDegenerateLoops();
        TestMeasuredGeometricEngineCategoryNaNPropagation();
        TestMeasuredGeometricEngineLargePlanStress();

        Console.WriteLine("\n==========================================================================");
        Console.WriteLine($" STRESS HARNESS RESULTS: Total: {_total} | Passed: {_passed} | Failed: {_failed}");
        Console.WriteLine("==========================================================================");

        if (_findings.Count > 0)
        {
            Console.WriteLine("\nFAILURE DETAILS:");
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

    private static void TestPolygon2DDegenerateConditions()
    {
        Console.WriteLine("\n--- Stress Group 1: Polygon2D Degenerate Conditions ---");

        // 1.1 Empty polygon
        var emptyPoly = Polygon2D.Empty;
        Check(emptyPoly.VertexCount == 0 && emptyPoly.Area == 0.0 && emptyPoly.Perimeter == 0.0 && emptyPoly.Centroid == Point2D.Zero,
            "1.1 Polygon2D.Empty has 0 vertices, 0 area, 0 perimeter, and Centroid at Zero");

        // 1.2 Null argument throws ArgumentNullException
        bool nullThrows = false;
        try { _ = new Polygon2D((IReadOnlyList<Point2D>)null!); }
        catch (ArgumentNullException) { nullThrows = true; }
        Check(nullThrows, "1.2 Constructor with null vertices throws ArgumentNullException");

        // 1.3 1 vertex throws ArgumentException
        bool onePtThrows = false;
        try { _ = new Polygon2D(new[] { new Point2D(100, 100) }); }
        catch (ArgumentException) { onePtThrows = true; }
        Check(onePtThrows, "1.3 Constructor with 1 vertex throws ArgumentException");

        // 1.4 2 distinct vertices throws ArgumentException
        bool twoPtThrows = false;
        try { _ = new Polygon2D(new[] { new Point2D(0, 0), new Point2D(100, 100) }); }
        catch (ArgumentException) { twoPtThrows = true; }
        Check(twoPtThrows, "1.4 Constructor with 2 distinct vertices throws ArgumentException");

        // 1.5 3 vertices with first == last (collapsed segment) throws ArgumentException
        bool closedTwoPtThrows = false;
        try { _ = new Polygon2D(new[] { new Point2D(0, 0), new Point2D(100, 100), new Point2D(0, 0) }); }
        catch (ArgumentException) { closedTwoPtThrows = true; }
        Check(closedTwoPtThrows, "1.5 Constructor with [P0, P1, P0] throws ArgumentException");

        // 1.6 3 identical vertices [P0, P0, P0] throws ArgumentException
        bool threeIdenticalThrows = false;
        try { _ = new Polygon2D(new[] { new Point2D(50, 50), new Point2D(50, 50), new Point2D(50, 50) }); }
        catch (ArgumentException) { threeIdenticalThrows = true; }
        Check(threeIdenticalThrows, "1.6 Constructor with [P0, P0, P0] throws ArgumentException");
    }

    private static void TestPolygon2DCollinearAndDuplicateVertices()
    {
        Console.WriteLine("\n--- Stress Group 2: Polygon2D Collinear & Duplicate Vertices ---");

        // 2.1 3 collinear vertices does NOT throw, has Area = 0.0, and Centroid is first vertex
        var collinealPts = new[] { new Point2D(0, 0), new Point2D(100, 0), new Point2D(200, 0) };
        var collinealPoly = new Polygon2D(collinealPts);
        Check(collinealPoly.Area == 0.0 && collinealPoly.SignedArea == 0.0,
            "2.1 Collinear 3 vertices has Area == 0.0 and SignedArea == 0.0");
        Check(collinealPoly.Centroid == collinealPts[0],
            "2.2 Collinear 3 vertices Centroid gracefully returns first vertex without division by zero");

        // 2.3 4 vertices with duplicate closing vertex [P0, P1, P2, P0] trims closing vertex to 3 vertices
        var closedTriPts = new[] { new Point2D(0, 0), new Point2D(300, 0), new Point2D(0, 400), new Point2D(0, 0) };
        var triangle = new Polygon2D(closedTriPts);
        Check(triangle.VertexCount == 3, "2.3 Closed 4-point loop [P0, P1, P2, P0] correctly trims to 3 vertices");
        Check(Math.Abs(triangle.Area - 60000.0) < 1e-4, $"2.4 Triangle (300x400) area is 60,000 (actual {triangle.Area})");
        Check(Math.Abs(triangle.Centroid.X - 100.0) < 1e-4 && Math.Abs(triangle.Centroid.Y - (400.0 / 3.0)) < 1e-4,
            $"2.5 Triangle centroid is (100, 133.33) (actual {triangle.Centroid})");

        // 2.6 Self-intersecting bowtie polygon [0,0 -> 100,100 -> 0,100 -> 100,0]
        var bowtiePts = new[] { new Point2D(0, 0), new Point2D(100, 100), new Point2D(0, 100), new Point2D(100, 0) };
        var bowtie = new Polygon2D(bowtiePts);
        Check(bowtie.VertexCount == 4, "2.6 Self-intersecting polygon constructs successfully without exception");
        Check(bowtie.Perimeter > 0.0, "2.7 Bowtie perimeter is positive");

        // 2.8 Contains check for point on boundary and vertex
        Check(triangle.Contains(new Point2D(0, 0), includeBoundary: true), "2.8 Contains detects vertex when includeBoundary = true");
        Check(!triangle.Contains(new Point2D(0, 0), includeBoundary: false), "2.9 Contains excludes vertex when includeBoundary = false");
        Check(triangle.Contains(new Point2D(50, 50)), "2.10 Contains detects interior point");
        Check(!triangle.Contains(new Point2D(500, 500)), "2.11 Contains returns false for exterior point");
    }

    private static void TestPolygon2DScaleAndLargeVertexCounts()
    {
        Console.WriteLine("\n--- Stress Group 3: Scale & Large Vertex Counts ---");

        // 3.1 Astronomical scale coordinates (1e14 mm)
        double largeScale = 1e14;
        var largePts = new[]
        {
            new Point2D(0, 0),
            new Point2D(largeScale, 0),
            new Point2D(largeScale, largeScale),
            new Point2D(0, largeScale)
        };
        var largePoly = new Polygon2D(largePts);
        Check(largePoly.VertexCount == 4 && largePoly.Area > 0.0, "3.1 Astronomical coordinates (1e14) handle area calculation");
        Check(Math.Abs(largePoly.Centroid.X - largeScale / 2) / largeScale < 1e-6,
            "3.2 Astronomical coordinates centroid is accurate");

        // 3.3 Microscopic scale coordinates (1e-4 mm, 0.1 micron)
        double smallScale = 1e-4;
        var smallPts = new[]
        {
            new Point2D(0, 0),
            new Point2D(smallScale, 0),
            new Point2D(smallScale, smallScale),
            new Point2D(0, smallScale)
        };
        var smallPoly = new Polygon2D(smallPts);
        Check(smallPoly.VertexCount == 4, $"3.3 Microscopic coordinates (0.1 micron) construct 4-point polygon (count: {smallPoly.VertexCount})");

        // 3.4 Large vertex polygon (10,000 vertices circle approximation)
        int vertexCount = 10000;
        double radius = 1000.0;
        var circlePts = new Point2D[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            double angle = 2.0 * Math.PI * i / vertexCount;
            circlePts[i] = new Point2D(radius * Math.Cos(angle), radius * Math.Sin(angle));
        }

        var sw = Stopwatch.StartNew();
        var circlePoly = new Polygon2D(circlePts);
        double computedArea = circlePoly.Area;
        var centroid = circlePoly.Centroid;
        sw.Stop();

        double expectedArea = Math.PI * radius * radius;
        double errorPct = Math.Abs(computedArea - expectedArea) / expectedArea;
        Check(circlePoly.VertexCount == 10000, "3.4 10,000-vertex polygon constructs correctly");
        Check(errorPct < 1e-4, $"3.5 10,000-vertex area accuracy is within 0.01% (error {errorPct:P4})");
        Check(Math.Abs(centroid.X) < 1e-4 && Math.Abs(centroid.Y) < 1e-4, "3.6 10,000-vertex centroid is at origin (0, 0)");
        Check(sw.ElapsedMilliseconds < 500, $"3.7 10,000-vertex calculation completed in {sw.ElapsedMilliseconds}ms (< 500ms)");
    }

    private static void TestPolygon2DNaNAndInfinityBehavior()
    {
        Console.WriteLine("\n--- Stress Group 4: NaN and Infinity Behavior in Polygon2D ---");

        // 4.1 Polygon with NaN vertex
        var nanPts = new[] { new Point2D(0, 0), new Point2D(double.NaN, 100), new Point2D(0, 100) };
        var nanPoly = new Polygon2D(nanPts);
        Check(double.IsNaN(nanPoly.Area), "4.1 Polygon with NaN vertex produces NaN Area without crashing");
        Check(double.IsNaN(nanPoly.Centroid.X) || double.IsNaN(nanPoly.Centroid.Y),
            "4.2 Polygon with NaN vertex produces NaN Centroid without crashing");

        // 4.3 DistanceToBoundary with NaN vertex finds distance to valid edge without throwing
        double distNaN = nanPoly.DistanceToBoundary(new Point2D(50, 50));
        Check(!double.IsNaN(distNaN) && Math.Abs(distNaN - 50.0) < 1e-4,
            $"4.3 DistanceToBoundary with NaN vertex computes valid edge distance (was {distNaN})");

        // 4.4 Contains with NaN point
        bool containsNaN = nanPoly.Contains(new Point2D(double.NaN, 50));
        Check(!containsNaN, "4.4 Contains with NaN point returns false without throwing");
    }

    private static void TestMeasuredGeometricEngineProfileDegenerateLoops()
    {
        Console.WriteLine("\n--- Stress Group 5: MeasuredGeometricEngine Profile Degenerate Loops ---");
        var engine = CreateEngine();

        // 5.1 Empty OuterLoop
        var itemEmptyLoop = CreateSlabItem("S_EMPTY", 150, 0, Array.Empty<Point3D>());
        var actualSlab = CreateSlabActual("S_EMPTY", 500, 500, 0, 150);
        var auditEmpty = engine.AuditItem(itemEmptyLoop, new Dictionary<string, ActualElementGeometry> { [actualSlab.Key] = actualSlab });
        Check(auditEmpty.Status == GeometricVerificationStatus.Verified || auditEmpty.Status == GeometricVerificationStatus.Defect,
            "5.1 Empty OuterLoop handled gracefully without exception");

        // 5.2 1-point loop
        var item1Pt = CreateSlabItem("S_1PT", 150, 0, new[] { new Point3D(100, 100, 0) });
        var actual1Pt = CreateSlabActual("S_1PT", 500, 500, 0, 150);
        var audit1Pt = engine.AuditItem(item1Pt, new Dictionary<string, ActualElementGeometry> { [actual1Pt.Key] = actual1Pt });
        Check(audit1Pt.Status != GeometricVerificationStatus.Inconclusive || audit1Pt.DefectReason != null,
            "5.2 1-point profile loop handled gracefully without crash");

        // 5.3 2-point loop
        var item2Pt = CreateSlabItem("S_2PT", 150, 0, new[] { new Point3D(0, 0, 0), new Point3D(1000, 0, 0) });
        var actual2Pt = CreateSlabActual("S_2PT", 500, 500, 0, 150);
        var audit2Pt = engine.AuditItem(item2Pt, new Dictionary<string, ActualElementGeometry> { [actual2Pt.Key] = actual2Pt });
        Check(audit2Pt != null, "5.3 2-point profile loop handled gracefully without crash");

        // 5.4 3 identical points [P0, P0, P0]
        var item3Identical = CreateSlabItem("S_3ID", 150, 0, new[] { new Point3D(500, 500, 0), new Point3D(500, 500, 0), new Point3D(500, 500, 0) });
        var actual3Id = CreateSlabActual("S_3ID", 500, 500, 0, 150);
        var audit3Identical = engine.AuditItem(item3Identical, new Dictionary<string, ActualElementGeometry> { [actual3Id.Key] = actual3Id });
        Check(audit3Identical != null, "5.4 3 identical points profile loop handled via fallback without crash");

        // 5.5 Profile loop with NaN coordinates in planned spec
        var itemNaNLoop = CreateSlabItem("S_NAN", 150, 0, new[] { new Point3D(double.NaN, 0, 0), new Point3D(1000, 0, 0), new Point3D(0, 1000, 0) });
        var actualNaN = CreateSlabActual("S_NAN", 500, 500, 0, 150);
        var auditNaNLoop = engine.AuditItem(itemNaNLoop, new Dictionary<string, ActualElementGeometry> { [actualNaN.Key] = actualNaN });
        Check(auditNaNLoop.Status == GeometricVerificationStatus.Defect,
            $"5.5 Profile loop with NaN coordinates is detected as Defect (actual status {auditNaNLoop.Status})");

        // 5.6 Profile with Infinity thickness
        var itemInfThick = CreateSlabItem("S_INF_THICK", double.PositiveInfinity, 0, new[] { new Point3D(0, 0, 0), new Point3D(1000, 0, 0), new Point3D(0, 1000, 0) });
        var actualInf = CreateSlabActual("S_INF_THICK", 500, 500, 0, 150);
        var auditInfThick = engine.AuditItem(itemInfThick, new Dictionary<string, ActualElementGeometry> { [actualInf.Key] = actualInf });
        Check(auditInfThick.Status == GeometricVerificationStatus.Defect,
            $"5.6 Profile with Infinity thickness is detected as Defect (actual status {auditInfThick.Status})");
    }

    private static void TestMeasuredGeometricEngineCategoryNaNPropagation()
    {
        Console.WriteLine("\n--- Stress Group 6: Strict NaN Propagation Across All Categories ---");
        var engine = CreateEngine();

        // 6.1 Column: Planned Location is NaN
        var colItemNaN = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C_NAN"),
            Category = CoordinationCategory.Column,
            Mark = "C_NAN",
            Geometry = new PointGeometrySpec { Location = new Point3D(double.NaN, 0, 0) }
        };
        var colActual = new ActualElementGeometry
        {
            Key = "Doc#Column:C_NAN",
            CategoryName = "Column",
            HasGeometry = true,
            Centroid = new Point3D(0, 0, 0),
            WidthMm = 220,
            HeightMm = 220
        };
        var colAuditNaN = engine.AuditItem(colItemNaN, new Dictionary<string, ActualElementGeometry> { [colActual.Key] = colActual });
        Check(colAuditNaN.Status == GeometricVerificationStatus.Defect,
            "6.1 Column with planned NaN Location is flagged as Defect (non-finite delta)");

        // 6.2 Beam: Planned Start is NaN
        var beamItemNaN = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B_NAN"),
            Category = CoordinationCategory.Beam,
            Mark = "B_NAN",
            Geometry = new LineGeometrySpec { Start = new Point3D(double.NaN, 0, 0), End = new Point3D(5000, 0, 0), ElevationMm = 3800 }
        };
        var beamActual = new ActualElementGeometry
        {
            Key = "Doc#Beam:B_NAN",
            CategoryName = "Beam",
            HasGeometry = true,
            CurveStart = new Point3D(0, 0, 0),
            CurveEnd = new Point3D(5000, 0, 0),
            TopElevationMm = 3800
        };
        var beamAuditNaN = engine.AuditItem(beamItemNaN, new Dictionary<string, ActualElementGeometry> { [beamActual.Key] = beamActual });
        Check(beamAuditNaN.Status == GeometricVerificationStatus.Defect,
            "6.2 Beam with planned NaN Start coordinate is flagged as Defect");

        // 6.3 Wall: Actual Thickness is NaN
        var wallItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Wall, "W_NAN"),
            Category = CoordinationCategory.Wall,
            Mark = "W_NAN",
            Geometry = new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(5000, 0, 0) }
        };
        var wallActualNaN = new ActualElementGeometry
        {
            Key = "Doc#Wall:W_NAN",
            CategoryName = "Wall",
            HasGeometry = true,
            CurveStart = new Point3D(0, 0, 0),
            CurveEnd = new Point3D(5000, 0, 0),
            ThicknessMm = double.NaN
        };
        var wallAuditNaN = engine.AuditItem(wallItem, new Dictionary<string, ActualElementGeometry> { [wallActualNaN.Key] = wallActualNaN });
        Check(wallAuditNaN.Status == GeometricVerificationStatus.Inconclusive,
            "6.3 Wall with NaN Thickness is flagged as Inconclusive under Rule 0");

        // 6.4 Level: Actual TopElevation is NaN
        var lvlItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Level, "L_NAN"),
            Category = CoordinationCategory.Level,
            Mark = "L_NAN",
            Geometry = new LevelGeometrySpec { ElevationM = 3.8 }
        };
        var lvlActualNaN = new ActualElementGeometry
        {
            Key = "Doc#Level:L_NAN",
            CategoryName = "Level",
            HasGeometry = true,
            TopElevationMm = double.NaN
        };
        var lvlAuditNaN = engine.AuditItem(lvlItem, new Dictionary<string, ActualElementGeometry> { [lvlActualNaN.Key] = lvlActualNaN });
        Check(lvlAuditNaN.Status == GeometricVerificationStatus.Inconclusive,
            "6.4 Level with NaN TopElevation is flagged as Inconclusive under Rule 0");

        // 6.5 Grid: Actual CurveStart is NaN
        var gridItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Grid, "G_NAN"),
            Category = CoordinationCategory.Grid,
            Mark = "G_NAN",
            Geometry = new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(0, 18000, 0) }
        };
        var gridActualNaN = new ActualElementGeometry
        {
            Key = "Doc#Grid:G_NAN",
            CategoryName = "Grid",
            HasGeometry = true,
            CurveStart = new Point3D(double.NaN, 0, 0),
            CurveEnd = new Point3D(0, 18000, 0)
        };
        var gridAuditNaN = engine.AuditItem(gridItem, new Dictionary<string, ActualElementGeometry> { [gridActualNaN.Key] = gridActualNaN });
        Check(gridAuditNaN.Status == GeometricVerificationStatus.Inconclusive,
            "6.5 Grid with NaN CurveStart is flagged as Inconclusive under Rule 0");

        // 6.6 Pile: Planned OffsetZ is NaN
        var pileItemNaN = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P_NAN"),
            Category = CoordinationCategory.Pile,
            Mark = "P_NAN",
            Geometry = new PointGeometrySpec { Location = new Point3D(1000, 2000, -2500), OffsetZMm = double.NaN }
        };
        var pileActual = new ActualElementGeometry
        {
            Key = "Doc#Pile:P_NAN",
            CategoryName = "Pile",
            HasGeometry = true,
            Centroid = new Point3D(1000, 2000, -2500),
            TopElevationMm = -2500.0
        };
        var pileAuditNaN = engine.AuditItem(pileItemNaN, new Dictionary<string, ActualElementGeometry> { [pileActual.Key] = pileActual });
        Check(pileAuditNaN.Status == GeometricVerificationStatus.Defect,
            "6.6 Pile with planned NaN OffsetZMm is flagged as Defect (non-finite delta)");
    }

    private static void TestMeasuredGeometricEngineLargePlanStress()
    {
        Console.WriteLine("\n--- Stress Group 7: Massive Plan Execution (1,000 Elements) ---");
        var engine = CreateEngine();

        int elementCount = 1000;
        var items = new List<ElementPlanItem>(elementCount);
        var actuals = new Dictionary<string, ActualElementGeometry>(elementCount);

        for (int i = 0; i < elementCount; i++)
        {
            string mark = $"C_{i:D4}";
            var key = new CompositeSourceKey("Doc", CoordinationCategory.Column, mark);
            items.Add(new ElementPlanItem
            {
                SourceKey = key,
                Category = CoordinationCategory.Column,
                Mark = mark,
                Geometry = new PointGeometrySpec { Location = new Point3D(i * 100.0, i * 100.0, 0) }
            });

            actuals[key.ToString()] = new ActualElementGeometry
            {
                Key = key.ToString(),
                CategoryName = "Column",
                HasGeometry = true,
                Centroid = new Point3D(i * 100.0, i * 100.0, 0),
                WidthMm = 220,
                HeightMm = 220
            };
        }

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "HASH123",
            TargetDoc = new DocumentIdentitySnapshot { Title = "TestDoc" },
            Items = items
        };

        var sw = Stopwatch.StartNew();
        var report = engine.AuditPlan(plan, actuals);
        sw.Stop();

        Check(report.Audits.Count == 1000, "7.1 Report contains all 1,000 audited items");
        Check(report.VerifiedCount == 1000, "7.2 All 1,000 items verified successfully");
        Check(report.OverallPassed, "7.3 OverallPassed is true for clean 1,000-item plan");
        Check(sw.ElapsedMilliseconds < 1000, $"7.4 1,000-element plan audited in {sw.ElapsedMilliseconds}ms (< 1000ms)");
    }

    private static ElementPlanItem CreateSlabItem(string mark, double thickMm, double elevMm, IReadOnlyList<Point3D> loop) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Slab, mark),
        Category = CoordinationCategory.Slab,
        Mark = mark,
        Geometry = new ProfileGeometrySpec
        {
            OuterLoop = loop,
            ThicknessMm = thickMm,
            ElevationMm = elevMm
        }
    };

    private static ActualElementGeometry CreateSlabActual(string mark, double cx, double cy, double elevMm, double thickMm) => new()
    {
        Key = $"Doc#Slab:{mark}",
        CategoryName = "Slab",
        HasGeometry = true,
        Centroid = new Point3D(cx, cy, elevMm),
        TopElevationMm = elevMm,
        ThicknessMm = thickMm
    };
}
