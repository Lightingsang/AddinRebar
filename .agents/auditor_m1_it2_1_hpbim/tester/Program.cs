using System;
using System.Collections.Generic;
using System.Text.Json;
using HPBim.Core.Algorithms;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using HPBim.Core.Serialization;
using HPBim.Core.Tcvn;

int passed = 0;
int failed = 0;

void Assert(string testName, bool condition, string detail = "")
{
    if (condition)
    {
        Console.WriteLine($"[PASS] {testName}");
        passed++;
    }
    else
    {
        Console.WriteLine($"[FAIL] {testName} - {detail}");
        failed++;
    }
}

Console.WriteLine("=== AUDITOR INDEPENDENT FORENSIC TEST SUITE ===");

// 1. Vector2D Serialization & Math
try
{
    var v = new Vector2D(3.0, 4.0);
    Assert("Vector2D.Length", Math.Abs(v.Length - 5.0) < 1e-6);
    Assert("Vector2D.Normalize", Math.Abs(v.Normalize().Length - 1.0) < 1e-6);
    Assert("Vector2D.LeftNormal", v.LeftNormal.Dot(v) == 0.0);
    Assert("Vector2D.RightNormal", v.RightNormal.Dot(v) == 0.0);

    // Serialization cycle check
    string json = JsonSerializer.Serialize(v);
    Assert("Vector2D.NoCycleInJson", !json.Contains("leftNormal") && !json.Contains("length"), $"JSON was: {json}");
    var vRound = JsonSerializer.Deserialize<Vector2D>(json);
    Assert("Vector2D.Roundtrip", Math.Abs(vRound.X - 3.0) < 1e-6 && Math.Abs(vRound.Y - 4.0) < 1e-6);
}
catch (Exception ex)
{
    Assert("Vector2D Exception", false, ex.Message);
}

// 2. Point2D & Point3D Serialization
try
{
    var p2 = new Point2D(123.456, 789.012);
    string jsonP2 = JsonSerializer.Serialize(p2);
    var p2Round = JsonSerializer.Deserialize<Point2D>(jsonP2);
    Assert("Point2D.Roundtrip", Math.Abs(p2Round.X - 123.456) < 1e-6 && Math.Abs(p2Round.Y - 789.012) < 1e-6);

    var p3 = new Point3D(11.1, 22.2, 33.3);
    string jsonP3 = JsonSerializer.Serialize(p3);
    var p3Round = JsonSerializer.Deserialize<Point3D>(jsonP3);
    Assert("Point3D.Roundtrip", Math.Abs(p3Round.X - 11.1) < 1e-6 && Math.Abs(p3Round.Y - 22.2) < 1e-6 && Math.Abs(p3Round.Z - 33.3) < 1e-6);
}
catch (Exception ex)
{
    Assert("Point Roundtrip Exception", false, ex.Message);
}

// 3. Polygon2D & Shoelace & Contains
try
{
    var square = new Polygon2D(new[]
    {
        new Point2D(0, 0),
        new Point2D(100, 0),
        new Point2D(100, 50),
        new Point2D(0, 50)
    });
    Assert("Polygon2D.Area", Math.Abs(square.Area - 5000.0) < 1e-6);
    Assert("Polygon2D.Perimeter", Math.Abs(square.Perimeter - 300.0) < 1e-6);
    Assert("Polygon2D.ContainsInside", square.Contains(new Point2D(50, 25)));
    Assert("Polygon2D.ContainsOutside", !square.Contains(new Point2D(150, 25)));
    // Boundary check
    Assert("Polygon2D.BoundaryIncludeTrue", square.Contains(new Point2D(50, 0), includeBoundary: true));
    Assert("Polygon2D.BoundaryIncludeFalse", !square.Contains(new Point2D(50, 0), includeBoundary: false));

    // Polygon JSON
    string jsonPoly = JsonSerializer.Serialize(square);
    Assert("Polygon2D.JsonExcludesComputed", !jsonPoly.Contains("perimeter") && !jsonPoly.Contains("signedArea"));
    var polyRound = JsonSerializer.Deserialize<Polygon2D>(jsonPoly);
    Assert("Polygon2D.RoundtripVertices", polyRound != null && polyRound.Vertices.Count == 4 && Math.Abs(polyRound.Area - 5000.0) < 1e-6);
}
catch (Exception ex)
{
    Assert("Polygon2D Exception", false, ex.Message);
}

// 4. LiangBarskyClipping
try
{
    var box = new BoundingBox2D(0, 0, 100, 100);
    Assert("LiangBarsky.Inside", LiangBarskyClipping.Intersects(new Point2D(10, 10), new Point2D(90, 90), box));
    Assert("LiangBarsky.Outside", !LiangBarskyClipping.Intersects(new Point2D(110, 10), new Point2D(190, 90), box));
    Assert("LiangBarsky.Crossing", LiangBarskyClipping.Clip(new Point2D(-50, 50), new Point2D(150, 50), box, out var cp0, out var cp1));
    Assert("LiangBarsky.ClippedEndpoints", Math.Abs(cp0.X - 0.0) < 1e-6 && Math.Abs(cp1.X - 100.0) < 1e-6);
}
catch (Exception ex)
{
    Assert("LiangBarsky Exception", false, ex.Message);
}

// 5. WallSegmenter
try
{
    var wall = new WallDef
    {
        Id = "W1",
        StartPoint = new Point2D(0, 0),
        EndPoint = new Point2D(6000, 0),
        ThicknessMm = 220,
        Openings = new[]
        {
            new OpeningDef { Id = "D1", DistanceAlongWallMm = 1000, WidthMm = 900, HeightMm = 2200, Kind = OpeningKind.Door },
            new OpeningDef { Id = "WND1", DistanceAlongWallMm = 3000, WidthMm = 1400, HeightMm = 1400, Kind = OpeningKind.Window }
        }
    };
    var segmenter = new WallSegmenter();
    var segments = segmenter.Segment(wall);
    Assert("WallSegmenter.Count", segments.Count == 3);
    Assert("WallSegmenter.Seg1", Math.Abs(segments[0].StartOffsetMm - 0) < 1e-3 && Math.Abs(segments[0].EndOffsetMm - 1000) < 1e-3);
    Assert("WallSegmenter.Seg2", Math.Abs(segments[1].StartOffsetMm - 1900) < 1e-3 && Math.Abs(segments[1].EndOffsetMm - 3000) < 1e-3);
    Assert("WallSegmenter.Seg3", Math.Abs(segments[2].StartOffsetMm - 4400) < 1e-3 && Math.Abs(segments[2].EndOffsetMm - 6000) < 1e-3);
}
catch (Exception ex)
{
    Assert("WallSegmenter Exception", false, ex.Message);
}

// 6. QcClashDetector Multi-Storey & Edge Beam & Zero-length Wall
try
{
    var detector = new QcClashDetector();

    // Column vs Opening Multi-storey
    var col1 = new ColumnDef { Id = "C1", CenterLocation = new Point2D(1000, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 1" };
    var col2 = new ColumnDef { Id = "C2", CenterLocation = new Point2D(1000, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 2" };
    var wallTang1 = new WallDef
    {
        Id = "W_T1",
        StartPoint = new Point2D(0, 0),
        EndPoint = new Point2D(5000, 0),
        BaseLevel = "Tang 1",
        Openings = new[] { new OpeningDef { Id = "D_T1", DistanceAlongWallMm = 900, WidthMm = 900, Kind = OpeningKind.Door } }
    };
    var wallTang2 = new WallDef
    {
        Id = "W_T2",
        StartPoint = new Point2D(0, 0),
        EndPoint = new Point2D(5000, 0),
        BaseLevel = "Tang 2",
        Openings = new[] { new OpeningDef { Id = "D_T2", DistanceAlongWallMm = 900, WidthMm = 900, Kind = OpeningKind.Door } }
    };

    // col1 (Tang 1) with wallTang2 (Tang 2) -> Should NOT clash
    var clashesCrossLevel = detector.DetectColumnOpeningClashes(new[] { col1 }, new[] { wallTang2 });
    Assert("QcClash.CrossLevelNoClash", clashesCrossLevel.Count == 0, $"Got {clashesCrossLevel.Count} clashes");

    // col1 (Tang 1) with wallTang1 (Tang 1) -> Should clash
    var clashesSameLevel = detector.DetectColumnOpeningClashes(new[] { col1 }, new[] { wallTang1 });
    Assert("QcClash.SameLevelClash", clashesSameLevel.Count == 1, $"Got {clashesSameLevel.Count} clashes");

    // Zero-length wall check
    var zeroWall = new WallDef
    {
        Id = "W_ZERO",
        StartPoint = new Point2D(100, 100),
        EndPoint = new Point2D(100, 100),
        BaseLevel = "Tang 1",
        Openings = new[] { new OpeningDef { Id = "D_Z", DistanceAlongWallMm = 0, WidthMm = 900 } }
    };
    var clashesZeroWall = detector.DetectColumnOpeningClashes(new[] { col1 }, new[] { zeroWall });
    Assert("QcClash.ZeroLengthWallNoThrow", clashesZeroWall.Count == 0);

    // Beam vs Slab Edge vs Interior
    var slab = new SlabDef
    {
        Id = "S1",
        Level = "Tang 2",
        Boundary = new Polygon2D(new[] { new Point2D(0, 0), new Point2D(5000, 0), new Point2D(5000, 5000), new Point2D(0, 5000) })
    };
    var edgeBeam = new BeamDef
    {
        Id = "B_EDGE",
        Level = "Tang 2",
        StartPoint = new Point2D(0, 0),
        EndPoint = new Point2D(5000, 0),
        WidthMm = 200,
        HeightMm = 350
    };
    var interiorBeam = new BeamDef
    {
        Id = "B_INTERIOR",
        Level = "Tang 2",
        StartPoint = new Point2D(0, 2500),
        EndPoint = new Point2D(5000, 2500),
        WidthMm = 200,
        HeightMm = 350
    };
    var crossLevelBeam = new BeamDef
    {
        Id = "B_T1",
        Level = "Tang 1",
        StartPoint = new Point2D(0, 2500),
        EndPoint = new Point2D(5000, 2500),
        WidthMm = 200,
        HeightMm = 350
    };

    var beamClashesEdge = detector.DetectBeamSlabClashes(new[] { edgeBeam }, new[] { slab });
    Assert("QcClash.EdgeBeamNoClash", beamClashesEdge.Count == 0, $"Got {beamClashesEdge.Count} clashes");

    var beamClashesInterior = detector.DetectBeamSlabClashes(new[] { interiorBeam }, new[] { slab });
    Assert("QcClash.InteriorBeamClash", beamClashesInterior.Count == 1, $"Got {beamClashesInterior.Count} clashes");

    var beamClashesCross = detector.DetectBeamSlabClashes(new[] { crossLevelBeam }, new[] { slab });
    Assert("QcClash.CrossLevelBeamNoClash", beamClashesCross.Count == 0, $"Got {beamClashesCross.Count} clashes");
}
catch (Exception ex)
{
    Assert("QcClashDetector Exception", false, ex.Message);
}

// 7. ClearanceChecker Multi-Storey & Zero-length Wall
try
{
    var checker = new ClearanceChecker();
    var colTang1 = new ColumnDef { Id = "C1", CenterLocation = new Point2D(1000, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 1" };
    var wallTang2 = new WallDef
    {
        Id = "W2",
        StartPoint = new Point2D(0, 0),
        EndPoint = new Point2D(5000, 0),
        BaseLevel = "Tang 2",
        Openings = new[] { new OpeningDef { Id = "OP2", DistanceAlongWallMm = 800, WidthMm = 80 } }
    };
    var vCross = checker.CheckOpeningToColumnClearances(new[] { wallTang2 }, new[] { colTang1 });
    Assert("ClearanceChecker.CrossLevelNoViolation", vCross.Count == 0);

    var zeroWall = new WallDef
    {
        Id = "W_Z",
        StartPoint = new Point2D(0, 0),
        EndPoint = new Point2D(0, 0),
        Openings = new[] { new OpeningDef { Id = "OP_Z", DistanceAlongWallMm = 0, WidthMm = 100 } }
    };
    var vZero = checker.CheckOpeningToColumnClearances(new[] { zeroWall }, new[] { colTang1 });
    Assert("ClearanceChecker.ZeroWallNoThrow", vZero.Count == 0);
}
catch (Exception ex)
{
    Assert("ClearanceChecker Exception", false, ex.Message);
}

// 8. TcvnRebarCalculator Detailing Math
try
{
    // D20 weight: 20^2 / 162.2 = 2.46609
    double w20 = TcvnRebarCalculator.CalculateUnitWeight(20);
    Assert("Tcvn.WeightD20", Math.Abs(w20 - 2.46609) < 0.01);
    Assert("Tcvn.NominalD20", Math.Abs(TcvnRebarCalculator.GetNominalUnitWeight(20) - 2.466) < 1e-3);
    Assert("Tcvn.AnchorageD20", Math.Abs(TcvnRebarCalculator.CalculateAnchorageLengthMm(20) - 600.0) < 1e-3);
    Assert("Tcvn.Hook90D20", Math.Abs(TcvnRebarCalculator.CalculateHook90LengthMm(20) - 240.0) < 1e-3);
    Assert("Tcvn.TensionLapD20", Math.Abs(TcvnRebarCalculator.CalculateTensionLapLengthMm(20) - 800.0) < 1e-3);
    Assert("Tcvn.CompressionLapD20", Math.Abs(TcvnRebarCalculator.CalculateCompressionLapLengthMm(20) - 600.0) < 1e-3);

    var splices = TcvnRebarCalculator.CalculateCommercialBarSplices(15000, 20, LapZone.Tension);
    Assert("Tcvn.SpliceCount", splices.SpliceCount == 1);
    Assert("Tcvn.SpliceDevLength", Math.Abs(splices.DevelopedLengthMm - 15800.0) < 1e-3);
}
catch (Exception ex)
{
    Assert("TcvnRebarCalculator Exception", false, ex.Message);
}

// 9. Full BimModelSerializer Roundtrip
try
{
    var model = new BimModel
    {
        ProjectName = "Test Townhouse 5x18m",
        Levels = new[]
        {
            new LevelDef { Name = "Tang 1", ElevationM = 0.150 },
            new LevelDef { Name = "Tang 2", ElevationM = 3.800 }
        },
        Grids = new[]
        {
            new AxisGrid { Name = "X1", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(0, 5000) },
            new AxisGrid { Name = "YA", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(18000, 0) }
        },
        Columns = new[]
        {
            new ColumnDef { Id = "C1", CenterLocation = new Point2D(0, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 1", TopLevel = "Tang 2" }
        },
        Walls = new[]
        {
            new WallDef
            {
                Id = "W1", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(4000, 0), BaseLevel = "Tang 1",
                Openings = new[] { new OpeningDef { Id = "D1", DistanceAlongWallMm = 1000, WidthMm = 900, HeightMm = 2200, Kind = OpeningKind.Door } }
            }
        },
        Rebars = new[]
        {
            new RebarDef { BarMark = 1, DiameterMm = 20, LengthMm = 4000, Quantity = 4, HostElementId = "C1" }
        }
    };

    string json = BimModelSerializer.Serialize(model);
    var round = BimModelSerializer.Deserialize(json);
    Assert("BimModelSerializer.ProjectName", round.ProjectName == "Test Townhouse 5x18m");
    Assert("BimModelSerializer.LevelsCount", round.Levels.Count == 2);
    Assert("BimModelSerializer.GridsCount", round.Grids.Count == 2);
    Assert("BimModelSerializer.ColumnsCount", round.Columns.Count == 1 && round.Columns[0].WidthMm == 220);
    Assert("BimModelSerializer.WallsCount", round.Walls.Count == 1 && round.Walls[0].Openings.Count == 1);
    Assert("BimModelSerializer.TotalRebarWeight", round.TotalRebarWeightKg > 0.0);
}
catch (Exception ex)
{
    Assert("BimModelSerializer Exception", false, ex.Message);
}

// 10. Adversarial Stress-Tests
try
{
    // A. Extreme coordinates (100,000 m = 100,000,000 mm)
    var farPoint = new Point2D(100_000_000, 100_000_000);
    var pZero = Point2D.Zero;
    double farDist = pZero.DistanceTo(farPoint);
    Assert("Stress.ExtremeCoords", farDist > 141_000_000);

    // B. Negative coordinates
    var negPoly = new Polygon2D(new[]
    {
        new Point2D(-100, -100),
        new Point2D(-50, -100),
        new Point2D(-50, -50),
        new Point2D(-100, -50)
    });
    Assert("Stress.NegativePolygonArea", Math.Abs(negPoly.Area - 2500.0) < 1e-6);

    // C. Collinear / 2-point polygon rejection
    bool caughtShortPoly = false;
    try
    {
        new Polygon2D(new[] { new Point2D(0, 0), new Point2D(10, 10) });
    }
    catch (ArgumentException)
    {
        caughtShortPoly = true;
    }
    Assert("Stress.RejectTwoPointPoly", caughtShortPoly);

    // D. Empty BimModel validation
    var qcEngine = new QcEngine();
    var emptyModel = new BimModel();
    var report = qcEngine.Validate(emptyModel);
    Assert("Stress.EmptyModelQcReport", report.IsValid && report.Errors.Count == 0 && report.Clashes.Count == 0);

    // E. Rebar minimum cover violation detection
    var badCoverModel = new BimModel
    {
        Rebars = new[]
        {
            new RebarDef { BarMark = 99, DiameterMm = 16, LengthMm = 1000, Quantity = 1, CoverMm = 10.0 } // < 15mm
        }
    };
    var badReport = qcEngine.Validate(badCoverModel, new QcOptions(true, true, true));
    Assert("Stress.CoverViolationDetected_ExplicitOptions", !badReport.IsValid && badReport.Errors.Count == 1, $"IsValid={badReport.IsValid}, Errors={badReport.Errors.Count}");

    // Also assert that calling with default null options FAILS to run checks due to C# struct default bug
    var defaultReport = qcEngine.Validate(badCoverModel);
    Assert("Stress.QcOptionsStructBug_DefaultNullRunsZeroChecks", defaultReport.TotalChecksRun == 0 && defaultReport.IsValid, $"TotalChecksRun={defaultReport.TotalChecksRun}");
}
catch (Exception ex)
{
    Assert("Stress Tests Exception", false, ex.Message);
}

Console.WriteLine($"\nSUMMARY: {passed} PASSED, {failed} FAILED.");
if (failed > 0) Environment.Exit(1);
