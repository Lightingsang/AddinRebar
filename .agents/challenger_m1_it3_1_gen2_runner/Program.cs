using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HPBim.Core.Algorithms;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using HPBim.Core.Serialization;
using HPBim.Core.Tcvn;

namespace ChallengerRunner;

public static class Program
{
    private static int _passCount = 0;
    private static int _failCount = 0;
    private static readonly List<string> _failures = new();

    public static int Main(string[] args)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  EMPIRICAL CHALLENGER TEST RUNNER - MILESTONE 1 ITERATION 3 VERIFICATION");
        Console.WriteLine("  Target: HPBim.Core.dll (.NET Standard 2.0 / .NET 10.0 runtime)");
        Console.WriteLine("================================================================================");
        Console.WriteLine($"Start Time (UTC): {DateTime.UtcNow:O}");
        Console.WriteLine();

        // 1. Mission Mandate: QcEngine Default Argument & Model Validation Tests
        RunQcEngineDefaultArgumentTests();
        RunQcEngineValidModelTests();
        RunQcEngineBadModelTests();
        RunQcEngineOptionsGranularityTests();

        // 2. Geometry Math & Algorithm Tests
        RunPoint2DTests();
        RunPoint3DTests();
        RunVector2DTests();
        RunPolygon2DShoelaceAndContainmentTests();
        RunLiangBarskyClippingTests();
        RunRoomAreaCalculatorTests();
        RunWallSegmenterTests();
        RunOpeningPlacementEngineTests();

        // 3. TCVN 5574:2018 Rules Tests
        RunTcvnCoverRulesTests();
        RunTcvnRebarCalculatorTests();

        // 4. QC Clash & Clearance Engine Tests
        RunQcClashDetectorTests();
        RunClearanceCheckerTests();

        // 5. JSON Serialization & Model Round-Trip Tests
        RunJsonSerializationTests();

        Console.WriteLine();
        Console.WriteLine("================================================================================");
        Console.WriteLine($"TOTAL ASSERTIONS: Passed = {_passCount}, Failed = {_failCount}");
        if (_failCount > 0)
        {
            Console.WriteLine("FAILURES ENCOUNTERED:");
            foreach (var f in _failures)
            {
                Console.WriteLine($"  [FAIL] {f}");
            }
            Console.WriteLine("VERDICT: REQUEST_CHANGES");
            return 1;
        }

        Console.WriteLine("ALL EMPIRICAL ASSERTIONS PASSED CLEANLY (100% GREEN)!");
        Console.WriteLine("VERDICT: APPROVE");
        Console.WriteLine("================================================================================");
        return 0;
    }

    private static void Assert(bool condition, string testName, string details = "")
    {
        if (condition)
        {
            _passCount++;
            Console.WriteLine($"  [PASS] {testName}");
        }
        else
        {
            _failCount++;
            string msg = $"{testName} - {details}";
            _failures.Add(msg);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {msg}");
            Console.ResetColor();
        }
    }

    // =========================================================================
    // 1. QcEngine Default Argument & Model Validation Tests
    // =========================================================================
    private static void RunQcEngineDefaultArgumentTests()
    {
        Console.WriteLine("--- 1. QcEngine Default Argument Behavior Tests ---");

        var engine = new QcEngine();

        // Verify null model throws ArgumentNullException
        bool threwNull = false;
        try
        {
            engine.Validate(null!);
        }
        catch (ArgumentNullException)
        {
            threwNull = true;
        }
        Assert(threwNull, "QcEngine: Validate(null) throws ArgumentNullException");

        // Verify default struct/record constructor has all checks enabled
        var defaultOpts = new QcOptions();
        Assert(defaultOpts.CheckClashes == true, "QcOptions: Parameterless ctor CheckClashes is true");
        Assert(defaultOpts.CheckClearances == true, "QcOptions: Parameterless ctor CheckClearances is true");
        Assert(defaultOpts.CheckTcvnCompliance == true, "QcOptions: Parameterless ctor CheckTcvnCompliance is true");

        // Bad model with illegal rebar cover (< 15mm)
        var badCoverModel = new BimModel
        {
            Rebars = new List<RebarDef>
            {
                new() { Id = "RB-01", BarMark = 1, CoverMm = 10.0, DiameterMm = 16, LengthMm = 1000, Quantity = 1 }
            }
        };

        // Calling Validate with default arguments (options = null)
        var reportDefault = engine.Validate(badCoverModel);
        Assert(reportDefault.TotalChecksRun == 6, "QcEngine: Validate(badModel) runs exactly 6 checks by default", $"TotalChecksRun = {reportDefault.TotalChecksRun}");
        Assert(!reportDefault.IsValid, "QcEngine: Validate(badModel) reports IsValid == false", $"IsValid = {reportDefault.IsValid}");
        Assert(reportDefault.Errors.Count == 1, "QcEngine: Validate(badModel) produces 1 error", $"Errors.Count = {reportDefault.Errors.Count}");
        Assert(reportDefault.Errors[0].RuleId == "TCVN-COVER-001", "QcEngine: Error rule ID is TCVN-COVER-001", $"RuleId = {reportDefault.Errors[0].RuleId}");

        // Calling Validate with explicit new QcOptions() parameterless instance
        var reportExplicitDefault = engine.Validate(badCoverModel, new QcOptions());
        Assert(reportExplicitDefault.TotalChecksRun == 6, "QcEngine: Validate(badModel, new QcOptions()) runs 6 checks", $"TotalChecksRun = {reportExplicitDefault.TotalChecksRun}");
        Assert(!reportExplicitDefault.IsValid, "QcEngine: Validate(badModel, new QcOptions()) reports IsValid == false");
    }

    private static void RunQcEngineValidModelTests()
    {
        Console.WriteLine("--- 2. QcEngine Valid Model Tests ---");

        var engine = new QcEngine();

        // 1. Empty model
        var emptyModel = new BimModel();
        var emptyReport = engine.Validate(emptyModel);
        Assert(emptyReport.TotalChecksRun == 6, "QcEngine: Empty model runs 6 checks", $"TotalChecksRun = {emptyReport.TotalChecksRun}");
        Assert(emptyReport.IsValid, "QcEngine: Empty model reports IsValid == true", $"IsValid = {emptyReport.IsValid}");
        Assert(emptyReport.Errors.Count == 0, "QcEngine: Empty model has 0 errors");
        Assert(emptyReport.Clashes.Count == 0, "QcEngine: Empty model has 0 clashes");

        // 2. Fully populated compliant model
        var validModel = new BimModel
        {
            Levels = new List<LevelDef>
            {
                new() { Name = "Tang 1", ElevationM = 0.0 },
                new() { Name = "Tang 2", ElevationM = 3.6 }
            },
            Columns = new List<ColumnDef>
            {
                new() { Id = "C1", CenterLocation = new Point2D(0, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 1" }
            },
            Walls = new List<WallDef>
            {
                new()
                {
                    Id = "W1",
                    StartPoint = new Point2D(200, 0),
                    EndPoint = new Point2D(5000, 0),
                    ThicknessMm = 220,
                    BaseLevel = "Tang 1",
                    Openings = new List<OpeningDef>
                    {
                        new() { Id = "D1", DistanceAlongWallMm = 1500, WidthMm = 900, HeightMm = 2200, Kind = OpeningKind.Door }
                    }
                }
            },
            Beams = new List<BeamDef>
            {
                new() { Id = "B1", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(5000, 0), WidthMm = 200, HeightMm = 400, Level = "Tang 1" }
            },
            Slabs = new List<SlabDef>
            {
                new()
                {
                    Id = "S1",
                    Level = "Tang 1",
                    Boundary = new Polygon2D(new[]
                    {
                        new Point2D(0, 0),
                        new Point2D(5000, 0),
                        new Point2D(5000, 4000),
                        new Point2D(0, 4000)
                    })
                }
            },
            Rebars = new List<RebarDef>
            {
                new() { Id = "RB-01", BarMark = 1, CoverMm = 20.0, DiameterMm = 16, LengthMm = 4500, Quantity = 4 }
            }
        };

        var validReport = engine.Validate(validModel);
        Assert(validReport.TotalChecksRun == 6, "QcEngine: Valid model runs 6 checks", $"TotalChecksRun = {validReport.TotalChecksRun}");
        Assert(validReport.IsValid, "QcEngine: Valid model reports IsValid == true", $"IsValid = {validReport.IsValid}");
        Assert(validReport.Errors.Count == 0, "QcEngine: Valid model has 0 errors");
        Assert(validReport.Clashes.Count == 0, "QcEngine: Valid model has 0 clashes");
    }

    private static void RunQcEngineBadModelTests()
    {
        Console.WriteLine("--- 3. QcEngine Bad Model Failure Modes ---");

        var engine = new QcEngine();

        // Bad Model: Column vs Opening physical overlap
        var clashModel = new BimModel
        {
            Columns = new List<ColumnDef>
            {
                new() { Id = "C-CLASH", CenterLocation = new Point2D(500, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 1" }
            },
            Walls = new List<WallDef>
            {
                new()
                {
                    Id = "W-CLASH",
                    StartPoint = new Point2D(0, 0),
                    EndPoint = new Point2D(4000, 0),
                    ThicknessMm = 220,
                    BaseLevel = "Tang 1",
                    Openings = new List<OpeningDef>
                    {
                        // Opening center at X=500mm, right on top of Column C-CLASH!
                        new() { Id = "D-CLASH", DistanceAlongWallMm = 50, WidthMm = 900, HeightMm = 2200, Kind = OpeningKind.Door }
                    }
                }
            }
        };

        var clashReport = engine.Validate(clashModel);
        Assert(clashReport.TotalChecksRun == 6, "QcEngine: Column clash model runs 6 checks");
        Assert(!clashReport.IsValid, "QcEngine: Column clash model reports IsValid == false");
        Assert(clashReport.Clashes.Count >= 1, "QcEngine: Detects >= 1 clash", $"Clashes = {clashReport.Clashes.Count}");
        Assert(clashReport.Clashes.Any(c => c.ClashType == "ColumnVsOpening"), "QcEngine: Clash type is ColumnVsOpening");

        // Bad Model: Secondary beam slicing undivided slab interior
        var beamSlabModel = new BimModel
        {
            Beams = new List<BeamDef>
            {
                new()
                {
                    Id = "B-SLICE",
                    StartPoint = new Point2D(2500, 0),
                    EndPoint = new Point2D(2500, 4000),
                    WidthMm = 200,
                    HeightMm = 350,
                    Level = "Tang 1",
                    IsTieBeam = false
                }
            },
            Slabs = new List<SlabDef>
            {
                new()
                {
                    Id = "S-WHOLE",
                    Level = "Tang 1",
                    Boundary = new Polygon2D(new[]
                    {
                        new Point2D(0, 0),
                        new Point2D(5000, 0),
                        new Point2D(5000, 4000),
                        new Point2D(0, 4000)
                    })
                }
            }
        };

        var beamSlabReport = engine.Validate(beamSlabModel);
        Assert(!beamSlabReport.IsValid, "QcEngine: Beam-slab slice model reports IsValid == false");
        Assert(beamSlabReport.Clashes.Any(c => c.ClashType == "SecondaryBeamSlicesUndividedSlab"),
            "QcEngine: Clash type is SecondaryBeamSlicesUndividedSlab");

        // Combined multi-defect model
        var combinedBadModel = new BimModel
        {
            Columns = clashModel.Columns,
            Walls = clashModel.Walls,
            Beams = beamSlabModel.Beams,
            Slabs = beamSlabModel.Slabs,
            Rebars = new List<RebarDef>
            {
                new() { Id = "RB-ILLEGAL", BarMark = 99, CoverMm = 8.0, DiameterMm = 12, LengthMm = 2000, Quantity = 2 }
            }
        };

        var combinedReport = engine.Validate(combinedBadModel);
        Assert(!combinedReport.IsValid, "QcEngine: Multi-defect model reports IsValid == false");
        Assert(combinedReport.Errors.Count >= 1, "QcEngine: Multi-defect model has >= 1 error");
        Assert(combinedReport.Clashes.Count >= 2, "QcEngine: Multi-defect model has >= 2 clashes", $"Clashes = {combinedReport.Clashes.Count}");
    }

    private static void RunQcEngineOptionsGranularityTests()
    {
        Console.WriteLine("--- 4. QcEngine Options Granularity Tests ---");

        var engine = new QcEngine();
        var badCoverModel = new BimModel
        {
            Rebars = new List<RebarDef>
            {
                new() { Id = "RB-01", BarMark = 1, CoverMm = 10.0, DiameterMm = 16, LengthMm = 1000, Quantity = 1 }
            }
        };

        // All checks disabled
        var disabledOpts = new QcOptions(CheckClashes: false, CheckClearances: false, CheckTcvnCompliance: false);
        var disabledReport = engine.Validate(badCoverModel, disabledOpts);
        Assert(disabledReport.TotalChecksRun == 0, "QcEngine: All checks disabled runs 0 checks");
        Assert(disabledReport.IsValid, "QcEngine: All checks disabled reports IsValid == true (checks bypassed)");

        // Only TCVN enabled (1 check)
        var tcvnOnly = new QcOptions(CheckClashes: false, CheckClearances: false, CheckTcvnCompliance: true);
        var tcvnReport = engine.Validate(badCoverModel, tcvnOnly);
        Assert(tcvnReport.TotalChecksRun == 1, "QcEngine: TcvnOnly runs 1 check", $"TotalChecksRun = {tcvnReport.TotalChecksRun}");
        Assert(!tcvnReport.IsValid, "QcEngine: TcvnOnly reports IsValid == false on illegal cover");

        // Only Clashes enabled (2 checks)
        var clashOnly = new QcOptions(CheckClashes: true, CheckClearances: false, CheckTcvnCompliance: false);
        var clashReport = engine.Validate(badCoverModel, clashOnly);
        Assert(clashReport.TotalChecksRun == 2, "QcEngine: ClashOnly runs 2 checks", $"TotalChecksRun = {clashReport.TotalChecksRun}");
        Assert(clashReport.IsValid, "QcEngine: ClashOnly on cover defect reports IsValid == true (clashes are clean)");

        // Only Clearances enabled (3 checks)
        var clearanceOnly = new QcOptions(CheckClashes: false, CheckClearances: true, CheckTcvnCompliance: false);
        var clearanceReport = engine.Validate(badCoverModel, clearanceOnly);
        Assert(clearanceReport.TotalChecksRun == 3, "QcEngine: ClearanceOnly runs 3 checks", $"TotalChecksRun = {clearanceReport.TotalChecksRun}");
    }

    // =========================================================================
    // 2. Geometry Math & Algorithm Tests
    // =========================================================================
    private static void RunPoint2DTests()
    {
        Console.WriteLine("--- 5. Point2D Geometry Math Tests ---");

        var p1 = new Point2D(100.0, 200.0);
        var p2 = new Point2D(400.0, 600.0);

        Assert(Math.Abs(p1.DistanceTo(p2) - 500.0) < 1e-9, "Point2D: Distance 3-4-5 triangle is 500.0");
        Assert(Math.Abs(p1.DistanceSquaredTo(p2) - 250000.0) < 1e-9, "Point2D: DistanceSquaredTo is 250,000.0");
        Assert(p1.IsAlmostEqualTo(new Point2D(100.0000001, 200.0000001), 1e-5), "Point2D: IsAlmostEqualTo with tolerance");
        Assert(!p1.IsAlmostEqualTo(new Point2D(100.1, 200.0), 1e-3), "Point2D: IsAlmostEqualTo rejects different coords");

        var mid = new Point2D((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);
        Assert(Math.Abs(mid.X - 250.0) < 1e-9 && Math.Abs(mid.Y - 400.0) < 1e-9, "Point2D: Midpoint is (250, 400)");

        var v = new Vector2D(50.0, -20.0);
        var moved = p1 + v;
        Assert(Math.Abs(moved.X - 150.0) < 1e-9 && Math.Abs(moved.Y - 180.0) < 1e-9, "Point2D: Point + Vector translation");

        var translated = p1.Translate(50.0, -20.0);
        Assert(Math.Abs(translated.X - 150.0) < 1e-9 && Math.Abs(translated.Y - 180.0) < 1e-9, "Point2D: Translate method");

        var diff = p2 - p1;
        Assert(Math.Abs(diff.X - 300.0) < 1e-9 && Math.Abs(diff.Y - 400.0) < 1e-9, "Point2D: Point - Point yields displacement vector");
    }

    private static void RunPoint3DTests()
    {
        Console.WriteLine("--- 6. Point3D Geometry Math Tests ---");

        var p1 = new Point3D(0.0, 0.0, 0.0);
        var p2 = new Point3D(2.0, 3.0, 6.0);

        // sqrt(4 + 9 + 36) = sqrt(49) = 7.0
        Assert(Math.Abs(p1.DistanceTo(p2) - 7.0) < 1e-9, "Point3D: Distance 2-3-6 is 7.0");
        Assert(Math.Abs(p1.Distance2DTo(p2) - Math.Sqrt(13.0)) < 1e-9, "Point3D: Distance2DTo is sqrt(13)");
        Assert(p1.ToPoint2D().Equals(Point2D.Zero), "Point3D: ToPoint2D drops Z accurately");
        Assert(p1.WithZ(10.0).Z == 10.0, "Point3D: WithZ updates Z accurately");
        Assert(p1.Translate(1.0, 2.0, 3.0).Equals(new Point3D(1.0, 2.0, 3.0)), "Point3D: Translate shifts x,y,z");
    }

    private static void RunVector2DTests()
    {
        Console.WriteLine("--- 7. Vector2D Math & Direction Tests ---");

        var v1 = new Vector2D(3.0, 4.0);
        Assert(Math.Abs(v1.Length - 5.0) < 1e-9, "Vector2D: Length of (3,4) is 5.0");
        Assert(Math.Abs(v1.LengthSquared - 25.0) < 1e-9, "Vector2D: LengthSquared is 25.0");

        var u = v1.Normalize();
        Assert(Math.Abs(u.Length - 1.0) < 1e-9, "Vector2D: Normalize produces unit length 1.0");
        Assert(Math.Abs(u.X - 0.6) < 1e-9 && Math.Abs(u.Y - 0.8) < 1e-9, "Vector2D: Normalized direction is (0.6, 0.8)");

        // Zero vector normalization guards
        var zero = Vector2D.Zero;
        bool normZeroThrew = false;
        try { zero.Normalize(); } catch (InvalidOperationException) { normZeroThrew = true; }
        Assert(normZeroThrew, "Vector2D: Normalize on zero vector throws InvalidOperationException");

        bool tryNormZero = zero.TryNormalize(out var unitRes);
        Assert(!tryNormZero && unitRes.Equals(Vector2D.Zero), "Vector2D: TryNormalize on zero returns false with Zero vector");

        // Dot and Cross products
        var vx = Vector2D.UnitX;
        var vy = Vector2D.UnitY;
        Assert(Math.Abs(vx.Dot(vy)) < 1e-9, "Vector2D: Perpendicular vectors dot product is 0.0");
        Assert(Math.Abs(vx.Cross(vy) - 1.0) < 1e-9, "Vector2D: UnitX x UnitY 2D cross product is 1.0");
        Assert(Math.Abs(vy.Cross(vx) - (-1.0)) < 1e-9, "Vector2D: UnitY x UnitX 2D cross product is -1.0");

        // Normals
        var leftN = vx.LeftNormal;
        Assert(Math.Abs(leftN.X) < 1e-9 && Math.Abs(leftN.Y - 1.0) < 1e-9, "Vector2D: UnitX LeftNormal is (0, 1) = UnitY");
        var rightN = vx.RightNormal;
        Assert(Math.Abs(rightN.X) < 1e-9 && Math.Abs(rightN.Y - (-1.0)) < 1e-9, "Vector2D: UnitX RightNormal is (0, -1) = -UnitY");
    }

    private static void RunPolygon2DShoelaceAndContainmentTests()
    {
        Console.WriteLine("--- 8. Polygon2D Shoelace, Centroid, and Containment Tests ---");

        // 1. CCW Rectangle: 5000 x 4000 mm
        var ccwBox = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(5000, 0),
            new Point2D(5000, 4000),
            new Point2D(0, 4000)
        });

        Assert(Math.Abs(ccwBox.Area - 20_000_000.0) < 1e-6, "Polygon2D: CCW Rectangle area is 20,000,000 mm2");
        Assert(ccwBox.IsCounterClockwise, "Polygon2D: IsCounterClockwise is true for CCW vertices");
        Assert(!ccwBox.IsClockwise, "Polygon2D: IsClockwise is false for CCW vertices");
        Assert(Math.Abs(ccwBox.Perimeter - 18_000.0) < 1e-6, "Polygon2D: Perimeter is 18,000 mm");
        Assert(Math.Abs(ccwBox.Centroid.X - 2500.0) < 1e-6 && Math.Abs(ccwBox.Centroid.Y - 2000.0) < 1e-6, "Polygon2D: Centroid is (2500, 2000)");

        // 2. CW Rectangle
        var cwBox = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(0, 4000),
            new Point2D(5000, 4000),
            new Point2D(5000, 0)
        });

        Assert(Math.Abs(cwBox.Area - 20_000_000.0) < 1e-6, "Polygon2D: CW Rectangle area is also 20,000,000 mm2");
        Assert(cwBox.IsClockwise, "Polygon2D: IsClockwise is true for CW vertices");

        // 3. Containment: Interior, Exterior, Boundary Edges, Corners
        var ptInside = new Point2D(2500, 2000);
        var ptOutside = new Point2D(6000, 2000);
        var ptBottomEdge = new Point2D(2500, 0);
        var ptTopEdge = new Point2D(2500, 4000);
        var ptLeftEdge = new Point2D(0, 2000);
        var ptRightEdge = new Point2D(5000, 2000);
        var ptCorner = new Point2D(0, 0);

        Assert(ccwBox.Contains(ptInside, includeBoundary: true), "Polygon2D: Interior point with includeBoundary=true is TRUE");
        Assert(ccwBox.Contains(ptInside, includeBoundary: false), "Polygon2D: Interior point with includeBoundary=false is TRUE");
        Assert(!ccwBox.Contains(ptOutside, includeBoundary: true), "Polygon2D: Exterior point with includeBoundary=true is FALSE");
        Assert(!ccwBox.Contains(ptOutside, includeBoundary: false), "Polygon2D: Exterior point with includeBoundary=false is FALSE");

        // Crucial regression check: Boundary edges MUST be false when includeBoundary=false!
        Assert(ccwBox.Contains(ptBottomEdge, includeBoundary: true), "Polygon2D: Bottom edge with includeBoundary=true is TRUE");
        Assert(!ccwBox.Contains(ptBottomEdge, includeBoundary: false), "Polygon2D: Bottom edge with includeBoundary=false is FALSE");
        Assert(ccwBox.Contains(ptTopEdge, includeBoundary: true), "Polygon2D: Top edge with includeBoundary=true is TRUE");
        Assert(!ccwBox.Contains(ptTopEdge, includeBoundary: false), "Polygon2D: Top edge with includeBoundary=false is FALSE");
        Assert(ccwBox.Contains(ptLeftEdge, includeBoundary: true), "Polygon2D: Left edge with includeBoundary=true is TRUE");
        Assert(!ccwBox.Contains(ptLeftEdge, includeBoundary: false), "Polygon2D: Left edge with includeBoundary=false is FALSE");
        Assert(ccwBox.Contains(ptRightEdge, includeBoundary: true), "Polygon2D: Right edge with includeBoundary=true is TRUE");
        Assert(!ccwBox.Contains(ptRightEdge, includeBoundary: false), "Polygon2D: Right edge with includeBoundary=false is FALSE");
        Assert(ccwBox.Contains(ptCorner, includeBoundary: true), "Polygon2D: Corner vertex with includeBoundary=true is TRUE");
        Assert(!ccwBox.Contains(ptCorner, includeBoundary: false), "Polygon2D: Corner vertex with includeBoundary=false is FALSE");

        // Verify CW polygon also respects boundary inclusion flags
        Assert(cwBox.Contains(ptInside, includeBoundary: false), "Polygon2D CW: Interior point is TRUE");
        Assert(!cwBox.Contains(ptBottomEdge, includeBoundary: false), "Polygon2D CW: Bottom edge with includeBoundary=false is FALSE");

        // 4. Triangle Shoelace Area
        var triangle = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(6000, 0),
            new Point2D(3000, 4000)
        });
        // 0.5 * 6000 * 4000 = 12,000,000
        Assert(Math.Abs(triangle.Area - 12_000_000.0) < 1e-6, "Polygon2D: Triangle area is 12,000,000 mm2");

        // 5. Automatic closing duplicate vertex removal
        var autoClosedPoly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(1000, 0),
            new Point2D(1000, 1000),
            new Point2D(0, 0) // Duplicate closing vertex
        });
        Assert(autoClosedPoly.VertexCount == 3, "Polygon2D: Duplicate closing vertex is stripped to 3 vertices");
    }

    private static void RunLiangBarskyClippingTests()
    {
        Console.WriteLine("--- 9. LiangBarskyClipping Tests ---");

        var box = new BoundingBox2D(0, 0, 1000, 1000);

        // Segment completely inside
        var p0In = new Point2D(200, 300);
        var p1In = new Point2D(800, 700);
        bool clipIn = LiangBarskyClipping.Clip(p0In, p1In, box, out var c0In, out var c1In);
        Assert(clipIn, "LiangBarsky: Segment inside box clips successfully");
        Assert(c0In.Equals(p0In) && c1In.Equals(p1In), "LiangBarsky: Inside segment endpoints unchanged");

        // Segment completely outside
        var p0Out = new Point2D(2000, 2000);
        var p1Out = new Point2D(3000, 3000);
        bool clipOut = LiangBarskyClipping.Clip(p0Out, p1Out, box, out _, out _);
        Assert(!clipOut, "LiangBarsky: Segment completely outside returns false");

        // Segment crossing box from (-500, 500) to (1500, 500)
        var p0Cross = new Point2D(-500, 500);
        var p1Cross = new Point2D(1500, 500);
        bool clipCross = LiangBarskyClipping.Clip(p0Cross, p1Cross, box, out var c0Cross, out var c1Cross);
        Assert(clipCross, "LiangBarsky: Crossing segment intersects");
        Assert(Math.Abs(c0Cross.X - 0.0) < 1e-6 && Math.Abs(c0Cross.Y - 500.0) < 1e-6, "LiangBarsky: Entry point clipped at X=0");
        Assert(Math.Abs(c1Cross.X - 1000.0) < 1e-6 && Math.Abs(c1Cross.Y - 500.0) < 1e-6, "LiangBarsky: Exit point clipped at X=1000");

        // Segment parallel outside
        var p0ParallelOut = new Point2D(-200, -200);
        var p1ParallelOut = new Point2D(-200, 1500);
        Assert(!LiangBarskyClipping.Intersects(p0ParallelOut, p1ParallelOut, box), "LiangBarsky: Parallel line outside box returns false");
    }

    private static void RunRoomAreaCalculatorTests()
    {
        Console.WriteLine("--- 10. RoomAreaCalculator Spatial Tests ---");

        var calc = new RoomAreaCalculator();

        var boundary = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(5000, 0),
            new Point2D(5000, 4000),
            new Point2D(0, 4000)
        });

        // 5000 * 4000 mm2 = 20 m2
        double areaM2 = calc.ComputeAreaM2(boundary);
        Assert(Math.Abs(areaM2 - 20.0) < 1e-6, "RoomAreaCalculator: 5x4m room area is 20.0 m2");
        Assert(calc.IsEnclosed(boundary), "RoomAreaCalculator: IsEnclosed is true");
        Assert(calc.ContainsPoint(boundary, new Point2D(2500, 2000)), "RoomAreaCalculator: ContainsPoint inside is true");
        Assert(!calc.ContainsPoint(boundary, new Point2D(6000, 2000)), "RoomAreaCalculator: ContainsPoint outside is false");

        // RoomDef validation with matching theoretical area
        var validRoom = new RoomDef
        {
            Name = "Phòng Khách",
            BoundaryPolygon = boundary,
            TagLocation = new Point2D(2500, 2000),
            TheoreticalAreaM2 = 20.0
        };
        var res = calc.ValidateRoom(validRoom);
        Assert(res.IsValid, "RoomAreaCalculator: ValidateRoom is valid when tag inside and area matches");

        // RoomDef validation with tag outside boundary
        var outsideTagRoom = new RoomDef
        {
            Name = "Phòng Lỗi",
            BoundaryPolygon = boundary,
            TagLocation = new Point2D(6000, 2000),
            TheoreticalAreaM2 = 20.0
        };
        var resOutside = calc.ValidateRoom(outsideTagRoom);
        Assert(!resOutside.IsValid, "RoomAreaCalculator: ValidateRoom fails when tag is outside");
        Assert(!resOutside.IsTagInside, "RoomAreaCalculator: IsTagInside is false");
    }

    private static void RunWallSegmenterTests()
    {
        Console.WriteLine("--- 11. WallSegmenter Openings Segmentation Tests ---");

        var segmenter = new WallSegmenter();

        // 6m wall with single 1m door at offset 2m
        var wall = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(6000, 0),
            ThicknessMm = 220,
            Openings = new List<OpeningDef>
            {
                new() { Id = "D1", DistanceAlongWallMm = 2000, WidthMm = 1000 }
            }
        };

        var segments = segmenter.Segment(wall);
        Assert(segments.Count == 2, "WallSegmenter: 1 opening divides wall into 2 solid spans", $"Count = {segments.Count}");
        Assert(Math.Abs(segments[0].LengthMm - 2000.0) < 1e-6, "WallSegmenter: Segment 1 length is 2000mm");
        Assert(Math.Abs(segments[1].LengthMm - 3000.0) < 1e-6, "WallSegmenter: Segment 2 length is 3000mm");

        // Overlapping openings merged correctly
        var wallOverlap = new WallDef
        {
            Id = "W-OVERLAP",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(8000, 0),
            ThicknessMm = 220,
            Openings = new List<OpeningDef>
            {
                new() { Id = "D1", DistanceAlongWallMm = 2000, WidthMm = 1500 }, // [2000, 3500]
                new() { Id = "D2", DistanceAlongWallMm = 3000, WidthMm = 1500 }  // [3000, 4500] -> merged [2000, 4500]
            }
        };

        var segsOverlap = segmenter.Segment(wallOverlap);
        Assert(segsOverlap.Count == 2, "WallSegmenter: Overlapping openings merged into 1 void (2 solid spans)");
        Assert(Math.Abs(segsOverlap[0].LengthMm - 2000.0) < 1e-6, "WallSegmenter: First span is 0..2000");
        Assert(Math.Abs(segsOverlap[1].LengthMm - 3500.0) < 1e-6, "WallSegmenter: Second span is 4500..8000 (3500mm)");

        // Zero-length wall throws ArgumentException
        var zeroWall = new WallDef
        {
            Id = "W-ZERO",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(0, 0),
            ThicknessMm = 220
        };
        bool threwZero = false;
        try { segmenter.Segment(zeroWall); } catch (ArgumentException) { threwZero = true; }
        Assert(threwZero, "WallSegmenter: Zero-length wall throws ArgumentException");
    }

    private static void RunOpeningPlacementEngineTests()
    {
        Console.WriteLine("--- 12. OpeningPlacementEngine Placement Tests ---");

        var engine = new OpeningPlacementEngine();

        var wall = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            ThicknessMm = 220,
            BaseOffsetMm = 0
        };

        var door = new OpeningDef
        {
            Id = "D1",
            DistanceAlongWallMm = 1000,
            WidthMm = 900,
            HeightMm = 2200,
            SillHeightMm = 0,
            Kind = OpeningKind.Door,
            Swing = OpeningSwing.Left
        };

        var placement = engine.ComputePlacement(wall, door, baseElevationMm: 150.0);
        Assert(placement.OpeningId == "D1", "OpeningPlacement: ID matches");
        Assert(Math.Abs(placement.CenterPoint2D.X - 1450.0) < 1e-6, "OpeningPlacement: CenterPoint2D X is 1450mm");
        Assert(Math.Abs(placement.InsertionPoint3D.Z - 150.0) < 1e-6, "OpeningPlacement: InsertionPoint3D Z is 150mm");
        Assert(placement.SwingClearancePolygon != null, "OpeningPlacement: Door swing clearance polygon generated");
    }

    // =========================================================================
    // 3. TCVN 5574:2018 Rules Tests
    // =========================================================================
    private static void RunTcvnCoverRulesTests()
    {
        Console.WriteLine("--- 13. ConcreteCoverRules TCVN 5574:2018 Tests ---");

        Assert(ConcreteCoverRules.SlabCoverMm == 15.0, "Cover: Slab minimum is 15mm");
        Assert(ConcreteCoverRules.BeamCoverMm == 25.0, "Cover: Beam minimum is 25mm");
        Assert(ConcreteCoverRules.ColumnCoverMm == 25.0, "Cover: Column minimum is 25mm");
        Assert(ConcreteCoverRules.FootingCoverMm == 50.0, "Cover: Footing minimum is 50mm");

        var slabPass = ConcreteCoverRules.ValidateCover(StructuralElementKind.Slab, 15.0);
        Assert(slabPass.IsCompliant, "Cover: 15mm on slab is compliant");

        var slabFail = ConcreteCoverRules.ValidateCover(StructuralElementKind.Slab, 14.9);
        Assert(!slabFail.IsCompliant, "Cover: 14.9mm on slab is non-compliant");

        var beamFail = ConcreteCoverRules.ValidateCover(StructuralElementKind.Beam, 20.0);
        Assert(!beamFail.IsCompliant, "Cover: 20mm on beam is non-compliant (requires 25mm)");
    }

    private static void RunTcvnRebarCalculatorTests()
    {
        Console.WriteLine("--- 14. TcvnRebarCalculator Formula & Detailing Tests ---");

        // Formula: W = D^2 / 162.2
        double w6 = TcvnRebarCalculator.CalculateUnitWeight(6.0);
        Assert(Math.Abs(w6 - (36.0 / 162.2)) < 1e-9, "Rebar: D6 unit weight matches D^2/162.2 (~0.222 kg/m)");

        double w20 = TcvnRebarCalculator.CalculateUnitWeight(20.0);
        Assert(Math.Abs(w20 - (400.0 / 162.2)) < 1e-9, "Rebar: D20 unit weight matches 400/162.2 (~2.466 kg/m)");

        // Total weight calculation
        double totW = TcvnRebarCalculator.CalculateTotalWeight(20.0, 5000.0, 4);
        double expectedTotW = (400.0 / 162.2) * 5.0 * 4;
        Assert(Math.Abs(totW - expectedTotW) < 1e-6, "Rebar: Total weight for 4 x 5m D20 bars is accurate");

        // Detailing rules
        Assert(Math.Abs(TcvnRebarCalculator.CalculateAnchorageLengthMm(20.0) - 600.0) < 1e-6, "Rebar: Anchorage length 30D is 600mm for D20");
        Assert(Math.Abs(TcvnRebarCalculator.CalculateHook90LengthMm(20.0) - 240.0) < 1e-6, "Rebar: Hook 90 length 12D is 240mm for D20");
        Assert(Math.Abs(TcvnRebarCalculator.CalculateTensionLapLengthMm(20.0) - 800.0) < 1e-6, "Rebar: Tension lap length 40D is 800mm for D20");
        Assert(Math.Abs(TcvnRebarCalculator.CalculateCompressionLapLengthMm(20.0) - 600.0) < 1e-6, "Rebar: Compression lap length 30D is 600mm for D20");

        // Stirrup hook: 10D min 75mm
        Assert(Math.Abs(TcvnRebarCalculator.CalculateStirrupHookLengthMm(6.0) - 75.0) < 1e-6, "Rebar: D6 stirrup hook clamps to minimum 75mm");
        Assert(Math.Abs(TcvnRebarCalculator.CalculateStirrupHookLengthMm(10.0) - 100.0) < 1e-6, "Rebar: D10 stirrup hook is 10D = 100mm");

        // Commercial bar splices: 11.7m
        var splicesShort = TcvnRebarCalculator.CalculateCommercialBarSplices(10_000.0, 20.0, LapZone.Tension);
        Assert(splicesShort.SpliceCount == 0, "Rebar: 10m bar requires 0 splices");

        var splicesLong = TcvnRebarCalculator.CalculateCommercialBarSplices(15_000.0, 20.0, LapZone.Tension);
        Assert(splicesLong.SpliceCount == 1, "Rebar: 15m bar requires 1 splice");
        Assert(Math.Abs(splicesLong.SingleLapLengthMm - 800.0) < 1e-6, "Rebar: Lap length is 800mm (40D)");
        Assert(Math.Abs(splicesLong.DevelopedLengthMm - 15_800.0) < 1e-6, "Rebar: Developed length is 15,800mm");

        // Beam stirrup zones
        var beamStirrups = TcvnRebarCalculator.CalculateBeamStirrupZones(clearSpanMm: 6000.0, beamHeightMm: 400.0);
        // denseZoneLength = min(2 * 400, 6000 / 3) = 800mm
        Assert(Math.Abs(beamStirrups.DenseZoneLengthMm - 800.0) < 1e-6, "Rebar: Beam dense zone length is 800mm");
        Assert(beamStirrups.DenseZoneCountStart == 8, "Rebar: Beam dense count start is 8 (800 / a100)");
    }

    // =========================================================================
    // 4. QC Clash & Clearance Engine Tests
    // =========================================================================
    private static void RunQcClashDetectorTests()
    {
        Console.WriteLine("--- 15. QcClashDetector Multi-Storey & Geometry Tests ---");

        var detector = new QcClashDetector();

        // Multi-storey isolation: Column on Tang 1, Opening on Tang 2 at same (X, Y)
        var colTang1 = new ColumnDef { Id = "C1", CenterLocation = new Point2D(500, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 1" };
        var wallTang2 = new WallDef
        {
            Id = "W1", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(2000, 0),
            ThicknessMm = 220, BaseLevel = "Tang 2",
            Openings = new List<OpeningDef> { new() { Id = "D1", DistanceAlongWallMm = 50, WidthMm = 900 } }
        };

        var crossStoreyClashes = detector.DetectColumnOpeningClashes(new[] { colTang1 }, new[] { wallTang2 });
        Assert(crossStoreyClashes.Count == 0, "QcClashDetector: Multi-storey column vs opening produces 0 clashes");

        // Same storey clash
        var wallTang1 = new WallDef
        {
            Id = "W1", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(2000, 0),
            ThicknessMm = 220, BaseLevel = "Tang 1",
            Openings = new List<OpeningDef> { new() { Id = "D1", DistanceAlongWallMm = 50, WidthMm = 900 } }
        };
        var sameStoreyClashes = detector.DetectColumnOpeningClashes(new[] { colTang1 }, new[] { wallTang1 });
        Assert(sameStoreyClashes.Count == 1, "QcClashDetector: Same-storey column vs opening detects 1 clash");

        // Case insensitivity of levels
        var wallTang1Upper = new WallDef
        {
            Id = "W1", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(2000, 0),
            ThicknessMm = 220, BaseLevel = "TANG 1",
            Openings = new List<OpeningDef> { new() { Id = "D1", DistanceAlongWallMm = 50, WidthMm = 900 } }
        };
        var caseInsensitiveClashes = detector.DetectColumnOpeningClashes(new[] { colTang1 }, new[] { wallTang1Upper });
        Assert(caseInsensitiveClashes.Count == 1, "QcClashDetector: Level comparison is case-insensitive");

        // Perimeter beam vs slab interior
        var slab = new SlabDef
        {
            Id = "S1", Level = "Tang 1",
            Boundary = new Polygon2D(new[]
            {
                new Point2D(0, 0), new Point2D(6000, 0), new Point2D(6000, 4000), new Point2D(0, 4000)
            })
        };

        // All 4 perimeter beams
        var bBottom = new BeamDef { Id = "B-BOT", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(6000, 0), Level = "Tang 1" };
        var bTop = new BeamDef { Id = "B-TOP", StartPoint = new Point2D(0, 4000), EndPoint = new Point2D(6000, 4000), Level = "Tang 1" };
        var bLeft = new BeamDef { Id = "B-LEFT", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(0, 4000), Level = "Tang 1" };
        var bRight = new BeamDef { Id = "B-RIGHT", StartPoint = new Point2D(6000, 0), EndPoint = new Point2D(6000, 4000), Level = "Tang 1" };
        var bReversed = new BeamDef { Id = "B-REV", StartPoint = new Point2D(6000, 0), EndPoint = new Point2D(0, 0), Level = "Tang 1" };

        var perimClashes = detector.DetectBeamSlabClashes(new[] { bBottom, bTop, bLeft, bRight, bReversed }, new[] { slab });
        Assert(perimClashes.Count == 0, "QcClashDetector: Perimeter boundary beams produce 0 slicing clashes", $"Clashes = {perimClashes.Count}");

        // Interior secondary beam
        var bInterior = new BeamDef { Id = "B-INT", StartPoint = new Point2D(3000, 0), EndPoint = new Point2D(3000, 4000), Level = "Tang 1" };
        var intClashes = detector.DetectBeamSlabClashes(new[] { bInterior }, new[] { slab });
        Assert(intClashes.Count == 1, "QcClashDetector: Interior secondary beam correctly triggers 1 slicing clash");

        // Tie beam is ignored from slicing clash
        var bTie = new BeamDef { Id = "B-TIE", StartPoint = new Point2D(3000, 0), EndPoint = new Point2D(3000, 4000), Level = "Tang 1", IsTieBeam = true };
        var tieClashes = detector.DetectBeamSlabClashes(new[] { bTie }, new[] { slab });
        Assert(tieClashes.Count == 0, "QcClashDetector: Tie beam (IsTieBeam=true) is skipped from slab slicing");

        // Zero-length wall does not throw
        var wallZero = new WallDef { Id = "W-0", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(0, 0), ThicknessMm = 220 };
        var zeroClashes = detector.DetectColumnOpeningClashes(new[] { colTang1 }, new[] { wallZero });
        Assert(zeroClashes.Count == 0, "QcClashDetector: Zero-length wall safely returns 0 clashes without throwing");
    }

    private static void RunClearanceCheckerTests()
    {
        Console.WriteLine("--- 16. ClearanceChecker Architectural & Standard Rules Tests ---");

        var checker = new ClearanceChecker();

        // 1. Opening to column clearance (< 100mm)
        var col = new ColumnDef { Id = "C1", CenterLocation = new Point2D(0, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 1" };
        // Wall from X=110 (col face) to X=2000. Opening at offset 50mm -> distance to col center is 110 + 50 = 160mm.
        // col edge is 110mm. Distance to col edge = 160 - 110 = 50mm (< 100mm).
        var wallNear = new WallDef
        {
            Id = "W1", StartPoint = new Point2D(110, 0), EndPoint = new Point2D(2000, 0),
            ThicknessMm = 220, BaseLevel = "Tang 1",
            Openings = new List<OpeningDef> { new() { Id = "D1", DistanceAlongWallMm = 50, WidthMm = 900 } }
        };

        var clrViolations = checker.CheckOpeningToColumnClearances(new[] { wallNear }, new[] { col });
        Assert(clrViolations.Count == 1, "ClearanceChecker: Opening within 50mm of column produces 1 warning");
        Assert(clrViolations[0].RuleId == "CLR-001", "ClearanceChecker: Warning rule is CLR-001");

        // 2. Door swing touching room tag (< 0.9 * width)
        var wallDoor = new WallDef
        {
            Id = "W-DOOR", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(4000, 0),
            ThicknessMm = 220, BaseLevel = "Tang 1",
            Openings = new List<OpeningDef> { new() { Id = "DOOR1", DistanceAlongWallMm = 1000, WidthMm = 900, Kind = OpeningKind.Door } }
        };
        // Hinge at X=1000, Y=0. Tag at X=1200, Y=200 (distance = sqrt(200^2 + 200^2) = 282mm < 810mm).
        var roomTagNear = new RoomDef { Name = "WC", Level = "Tang 1", TagLocation = new Point2D(1200, 200) };
        var swingViolations = checker.CheckDoorSwingClearances(new[] { wallDoor }, Array.Empty<MepFixtureDef>(), new[] { roomTagNear });
        Assert(swingViolations.Count == 1, "ClearanceChecker: Door swing touching room tag produces CLR-002");
        Assert(swingViolations[0].RuleId == "CLR-002", "ClearanceChecker: Warning rule is CLR-002");

        // 3. Wall thicknesses: standard 110 & 220 vs non-standard
        var walls = new List<WallDef>
        {
            new() { Id = "W-110", ThicknessMm = 110.0 },
            new() { Id = "W-220", ThicknessMm = 220.0 },
            new() { Id = "W-150", ThicknessMm = 150.0 } // Non-standard!
        };
        var thickViolations = checker.CheckWallThicknesses(walls);
        Assert(thickViolations.Count == 1, "ClearanceChecker: Non-standard 150mm wall produces 1 warning");
        Assert(thickViolations[0].RuleId == "TCVN-WALL-001", "ClearanceChecker: Warning rule is TCVN-WALL-001");
    }

    // =========================================================================
    // 5. JSON Serialization & Model Round-Trip Tests
    // =========================================================================
    private static void RunJsonSerializationTests()
    {
        Console.WriteLine("--- 17. JSON Serialization & Cycle-Free Round-Trip Tests ---");

        // 1. Vector2D cycle freedom
        var v = new Vector2D(123.456, -789.012);
        string vJson = JsonSerializer.Serialize(v, BimModelSerializer.CreateOptions(false));
        Assert(!vJson.Contains("leftNormal", StringComparison.OrdinalIgnoreCase), "Vector2D: JSON omits leftNormal (cycle broken)");
        Assert(!vJson.Contains("rightNormal", StringComparison.OrdinalIgnoreCase), "Vector2D: JSON omits rightNormal (cycle broken)");
        Assert(!vJson.Contains("length", StringComparison.OrdinalIgnoreCase), "Vector2D: JSON omits length");

        var vRoundtrip = JsonSerializer.Deserialize<Vector2D>(vJson, BimModelSerializer.CreateOptions(false));
        Assert(Math.Abs(vRoundtrip.X - v.X) < 1e-6 && Math.Abs(vRoundtrip.Y - v.Y) < 1e-6, "Vector2D: Deserialization preserves coordinates");

        // 2. Point2D coordinate preservation
        var pt = new Point2D(987.65, 432.10);
        string ptJson = JsonSerializer.Serialize(pt, BimModelSerializer.CreateOptions(false));
        var ptRoundtrip = JsonSerializer.Deserialize<Point2D>(ptJson, BimModelSerializer.CreateOptions(false));
        Assert(Math.Abs(ptRoundtrip.X - pt.X) < 1e-6 && Math.Abs(ptRoundtrip.Y - pt.Y) < 1e-6, "Point2D: Deserialization preserves coordinates");

        // 3. Polygon2D computed properties omitted
        var poly = new Polygon2D(new[]
        {
            new Point2D(0, 0), new Point2D(5000, 0), new Point2D(5000, 4000), new Point2D(0, 4000)
        });
        string polyJson = JsonSerializer.Serialize(poly, BimModelSerializer.CreateOptions(false));
        Assert(!polyJson.Contains("area", StringComparison.OrdinalIgnoreCase), "Polygon2D: JSON omits computed area");
        Assert(!polyJson.Contains("perimeter", StringComparison.OrdinalIgnoreCase), "Polygon2D: JSON omits computed perimeter");

        var polyRoundtrip = JsonSerializer.Deserialize<Polygon2D>(polyJson, BimModelSerializer.CreateOptions(false));
        Assert(polyRoundtrip != null && polyRoundtrip.VertexCount == 4, "Polygon2D: Deserialized polygon has 4 vertices");
        Assert(Math.Abs(polyRoundtrip!.Area - 20_000_000.0) < 1e-6, "Polygon2D: Deserialized polygon computes exact area");

        // 4. Full BimModel file round-trip
        var model = new BimModel
        {
            Levels = new List<LevelDef> { new() { Name = "Tang 1", ElevationM = 0.0 } },
            Columns = new List<ColumnDef> { new() { Id = "C1", CenterLocation = new Point2D(100, 200) } },
            Rebars = new List<RebarDef> { new() { Id = "R1", DiameterMm = 16, LengthMm = 5000, Quantity = 4 } }
        };

        string tempPath = Path.Combine(Path.GetTempPath(), $"hpbim_test_roundtrip_{Guid.NewGuid():N}.json");
        try
        {
            BimModelSerializer.SerializeToFile(model, tempPath);
            var loaded = BimModelSerializer.DeserializeFromFile(tempPath);
            Assert(loaded != null, "BimModel: Deserialized model is non-null");
            Assert(loaded!.Levels.Count == 1 && loaded.Columns.Count == 1 && loaded.Rebars.Count == 1,
                "BimModel: Loaded element collections match original counts");
            Assert(Math.Abs(loaded.Columns[0].CenterLocation.X - 100.0) < 1e-6,
                "BimModel: Column coordinate intact across file roundtrip");
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
