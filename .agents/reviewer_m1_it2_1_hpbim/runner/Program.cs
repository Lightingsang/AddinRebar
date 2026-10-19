using System;
using System.Collections.Generic;
using System.Text.Json;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using HPBim.Core.Serialization;

namespace ReviewerRunner;

public static class Program
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Main()
    {
        Console.WriteLine("=== REVIEWER & ADVERSARIAL CRITIC INDEPENDENT VERIFICATION ===");

        TestVector2DSerialization();
        TestPoint2DAnd3DSerialization();
        TestPolygon2DSerialization();
        TestPolygon2DContainsEdgeCases();
        TestQcClashDetectorStoreyLevelAndZeroLength();
        TestClearanceCheckerStoreyLevelAndZeroLength();
        TestBimModelSerializerRoundTrip();

        Console.WriteLine($"\nSUMMARY: Passed={_passed}, Failed={_failed}");
        if (_failed > 0)
        {
            Console.WriteLine("VERIFICATION FAILED!");
            return 1;
        }

        Console.WriteLine("ALL INDEPENDENT VERIFICATION TESTS PASSED SUCCESSFULLY!");
        return 0;
    }

    private static void Assert(bool condition, string testName, string? detail = null)
    {
        if (condition)
        {
            Console.WriteLine($"[PASS] {testName}");
            _passed++;
        }
        else
        {
            Console.WriteLine($"[FAIL] {testName} - {detail ?? "Assertion failed"}");
            _failed++;
        }
    }

    private static void TestVector2DSerialization()
    {
        Console.WriteLine("\n--- Testing Vector2D Serialization ---");
        var v = new Vector2D(3.0, 4.0);

        string json = "";
        try
        {
            json = JsonSerializer.Serialize(v);
            Assert(true, "Vector2D: Serializes without JsonException (no cycle detected)");
        }
        catch (Exception ex)
        {
            Assert(false, "Vector2D: Serializes without JsonException", ex.Message);
            return;
        }

        Assert(!json.Contains("LeftNormal", StringComparison.OrdinalIgnoreCase), "Vector2D: JSON excludes LeftNormal");
        Assert(!json.Contains("RightNormal", StringComparison.OrdinalIgnoreCase), "Vector2D: JSON excludes RightNormal");
        Assert(!json.Contains("Length", StringComparison.OrdinalIgnoreCase), "Vector2D: JSON excludes Length");
        Assert(!json.Contains("SquaredLength", StringComparison.OrdinalIgnoreCase), "Vector2D: JSON excludes SquaredLength");
        Assert(!json.Contains("AngleRad", StringComparison.OrdinalIgnoreCase), "Vector2D: JSON excludes AngleRad");
        Assert(!json.Contains("AngleDeg", StringComparison.OrdinalIgnoreCase), "Vector2D: JSON excludes AngleDeg");

        var v2 = JsonSerializer.Deserialize<Vector2D>(json);
        Assert(Math.Abs(v2.X - 3.0) < 1e-9 && Math.Abs(v2.Y - 4.0) < 1e-9, "Vector2D: Roundtrip preserves X=3, Y=4");
        Assert(Math.Abs(v2.Length - 5.0) < 1e-9, "Vector2D: Computed Length is 5.0");
        Assert(Math.Abs(v2.SquaredLength - 25.0) < 1e-9, "Vector2D: Computed SquaredLength is 25.0");
        Assert(v2.LeftNormal.X == -4.0 && v2.LeftNormal.Y == 3.0, "Vector2D: LeftNormal is (-4, 3)");
        Assert(v2.RightNormal.X == 4.0 && v2.RightNormal.Y == -3.0, "Vector2D: RightNormal is (4, -3)");
    }

    private static void TestPoint2DAnd3DSerialization()
    {
        Console.WriteLine("\n--- Testing Point2D & Point3D Serialization ---");
        var pt2 = new Point2D(123.456, -789.012);
        string json2 = JsonSerializer.Serialize(pt2);
        var pt2Round = JsonSerializer.Deserialize<Point2D>(json2);
        Assert(Math.Abs(pt2Round.X - 123.456) < 1e-9 && Math.Abs(pt2Round.Y - (-789.012)) < 1e-9,
            "Point2D: Roundtrip preserves non-zero coordinates accurately (not 0,0)");

        var pt3 = new Point3D(10.5, -20.25, 30.125);
        string json3 = JsonSerializer.Serialize(pt3);
        var pt3Round = JsonSerializer.Deserialize<Point3D>(json3);
        Assert(Math.Abs(pt3Round.X - 10.5) < 1e-9 &&
               Math.Abs(pt3Round.Y - (-20.25)) < 1e-9 &&
               Math.Abs(pt3Round.Z - 30.125) < 1e-9,
            "Point3D: Roundtrip preserves X, Y, Z accurately (not 0,0,0)");
    }

    private static void TestPolygon2DSerialization()
    {
        Console.WriteLine("\n--- Testing Polygon2D Serialization ---");
        var vertices = new[]
        {
            new Point2D(0.0, 0.0),
            new Point2D(100.0, 0.0),
            new Point2D(100.0, 50.0),
            new Point2D(0.0, 50.0)
        };
        var poly = new Polygon2D(vertices);

        string json = JsonSerializer.Serialize(poly);
        Assert(!json.Contains("Perimeter", StringComparison.OrdinalIgnoreCase), "Polygon2D: JSON excludes Perimeter");
        Assert(!json.Contains("Area", StringComparison.OrdinalIgnoreCase), "Polygon2D: JSON excludes Area");
        Assert(!json.Contains("SignedArea", StringComparison.OrdinalIgnoreCase), "Polygon2D: JSON excludes SignedArea");
        Assert(!json.Contains("Centroid", StringComparison.OrdinalIgnoreCase), "Polygon2D: JSON excludes Centroid");
        Assert(!json.Contains("BoundingBox", StringComparison.OrdinalIgnoreCase), "Polygon2D: JSON excludes BoundingBox");
        Assert(!json.Contains("IsClockwise", StringComparison.OrdinalIgnoreCase), "Polygon2D: JSON excludes IsClockwise");

        var polyRound = JsonSerializer.Deserialize<Polygon2D>(json);
        Assert(polyRound != null, "Polygon2D: Deserializes non-null");
        Assert(polyRound!.Vertices.Count == 4, "Polygon2D: Deserializes 4 vertices");
        Assert(Math.Abs(polyRound.Area - 5000.0) < 1e-6, "Polygon2D: Recomputes Area = 5000.0");
        Assert(Math.Abs(polyRound.Perimeter - 300.0) < 1e-6, "Polygon2D: Recomputes Perimeter = 300.0");
        Assert(Math.Abs(polyRound.Centroid.X - 50.0) < 1e-6 && Math.Abs(polyRound.Centroid.Y - 25.0) < 1e-6,
            "Polygon2D: Recomputes Centroid = (50, 25)");
    }

    private static void TestPolygon2DContainsEdgeCases()
    {
        Console.WriteLine("\n--- Testing Polygon2D.Contains Edge Cases ---");
        var vertices = new[]
        {
            new Point2D(0.0, 0.0),
            new Point2D(100.0, 0.0),
            new Point2D(100.0, 50.0),
            new Point2D(0.0, 50.0)
        };
        var poly = new Polygon2D(vertices);

        // Point inside
        Assert(poly.Contains(new Point2D(50, 25), true) == true, "Polygon2D: Inside point with includeBoundary=true is true");
        Assert(poly.Contains(new Point2D(50, 25), false) == true, "Polygon2D: Inside point with includeBoundary=false is true");

        // Point outside
        Assert(poly.Contains(new Point2D(150, 25), true) == false, "Polygon2D: Outside point with includeBoundary=true is false");
        Assert(poly.Contains(new Point2D(150, 25), false) == false, "Polygon2D: Outside point with includeBoundary=false is false");

        // Point on boundary segment
        var onSegment = new Point2D(50.0, 0.0);
        Assert(poly.Contains(onSegment, includeBoundary: true) == true, "Polygon2D: Boundary midpoint with includeBoundary=true is true");
        Assert(poly.Contains(onSegment, includeBoundary: false) == false, "Polygon2D: Boundary midpoint with includeBoundary=false is FALSE (key fix)");

        // Point on vertex
        var onVertex = new Point2D(100.0, 50.0);
        Assert(poly.Contains(onVertex, includeBoundary: true) == true, "Polygon2D: Vertex with includeBoundary=true is true");
        Assert(poly.Contains(onVertex, includeBoundary: false) == false, "Polygon2D: Vertex with includeBoundary=false is FALSE");

        // Collinear point outside segment
        var collinearOutside = new Point2D(-10.0, 0.0);
        Assert(poly.Contains(collinearOutside, includeBoundary: true) == false, "Polygon2D: Collinear outside point is false");
    }

    private static void TestQcClashDetectorStoreyLevelAndZeroLength()
    {
        Console.WriteLine("\n--- Testing QcClashDetector Storey Level & Zero-Length Guards ---");
        var detector = new QcClashDetector();

        // 1. Column vs Opening across different storeys
        var colLevel1 = new ColumnDef
        {
            Id = "C1",
            CenterLocation = new Point2D(1000, 1000),
            WidthMm = 220,
            DepthMm = 220,
            BaseLevel = "Tang 1"
        };
        var wallLevel2 = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 1000),
            EndPoint = new Point2D(2000, 1000),
            ThicknessMm = 220,
            BaseLevel = "Tang 2",
            Openings = new[]
            {
                new OpeningDef { Id = "D1", Kind = OpeningKind.Door, DistanceAlongWallMm = 900, WidthMm = 200 }
            }
        };

        var clashesDiffLevel = detector.DetectColumnOpeningClashes(new[] { colLevel1 }, new[] { wallLevel2 });
        Assert(clashesDiffLevel.Count == 0, "QcClashDetector: Multi-storey Column vs Opening does NOT clash (0 clashes)");

        // 2. Column vs Opening on same storey
        var wallLevel1 = new WallDef
        {
            Id = "W2",
            StartPoint = new Point2D(0, 1000),
            EndPoint = new Point2D(2000, 1000),
            ThicknessMm = 220,
            BaseLevel = "Tang 1",
            Openings = new[]
            {
                new OpeningDef { Id = "D1", Kind = OpeningKind.Door, DistanceAlongWallMm = 900, WidthMm = 200 }
            }
        };
        var clashesSameLevel = detector.DetectColumnOpeningClashes(new[] { colLevel1 }, new[] { wallLevel1 });
        Assert(clashesSameLevel.Count == 1, "QcClashDetector: Same-storey Column vs Opening CLASHES (1 clash detected)");

        // 3. Case-insensitive storey matching
        var wallCaseInsensitive = new WallDef
        {
            Id = "W3",
            StartPoint = new Point2D(0, 1000),
            EndPoint = new Point2D(2000, 1000),
            ThicknessMm = 220,
            BaseLevel = "tang 1", // lowercase
            Openings = new[]
            {
                new OpeningDef { Id = "D1", Kind = OpeningKind.Door, DistanceAlongWallMm = 900, WidthMm = 200 }
            }
        };
        var clashesCase = detector.DetectColumnOpeningClashes(new[] { colLevel1 }, new[] { wallCaseInsensitive });
        Assert(clashesCase.Count == 1, "QcClashDetector: Case-insensitive level comparison works");

        // 4. Zero-length wall guard
        var zeroWall = new WallDef
        {
            Id = "W_Zero",
            StartPoint = new Point2D(1000, 1000),
            EndPoint = new Point2D(1000, 1000), // length = 0
            ThicknessMm = 220,
            BaseLevel = "Tang 1",
            Openings = new[]
            {
                new OpeningDef { Id = "D_Z", Kind = OpeningKind.Door, DistanceAlongWallMm = 0, WidthMm = 200 }
            }
        };
        try
        {
            var clashesZero = detector.DetectColumnOpeningClashes(new[] { colLevel1 }, new[] { zeroWall });
            Assert(clashesZero.Count == 0, "QcClashDetector: Zero-length wall does NOT throw and returns 0 clashes");
        }
        catch (Exception ex)
        {
            Assert(false, "QcClashDetector: Zero-length wall guard failed", ex.Message);
        }

        // 5. Beam vs Slab across different storeys
        var slabLevel2 = new SlabDef
        {
            Id = "S1",
            Level = "Tang 2",
            Boundary = new Polygon2D(new[]
            {
                new Point2D(0, 0),
                new Point2D(4000, 0),
                new Point2D(4000, 4000),
                new Point2D(0, 4000)
            })
        };
        var beamLevel1 = new BeamDef
        {
            Id = "B1",
            Level = "Tang 1",
            StartPoint = new Point2D(0, 2000),
            EndPoint = new Point2D(4000, 2000),
            WidthMm = 200,
            HeightMm = 400
        };
        var beamSlabDiff = detector.DetectBeamSlabClashes(new[] { beamLevel1 }, new[] { slabLevel2 });
        Assert(beamSlabDiff.Count == 0, "QcClashDetector: Multi-storey Beam vs Slab does NOT clash (0 clashes)");

        // 6. Secondary Beam slicing undivided slab on same storey
        var beamLevel2Interior = new BeamDef
        {
            Id = "B2",
            Level = "Tang 2",
            StartPoint = new Point2D(0, 2000),
            EndPoint = new Point2D(4000, 2000), // midpoint (2000, 2000) inside slab
            WidthMm = 200,
            HeightMm = 400
        };
        var beamSlabSame = detector.DetectBeamSlabClashes(new[] { beamLevel2Interior }, new[] { slabLevel2 });
        Assert(beamSlabSame.Count == 1, "QcClashDetector: Interior beam on same storey triggers slicing clash (1 clash)");

        // 7. Perimeter Edge Beam along slab boundary
        var beamLevel2Edge = new BeamDef
        {
            Id = "B_Edge",
            Level = "Tang 2",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(4000, 0), // midpoint (2000, 0) ON boundary
            WidthMm = 200,
            HeightMm = 400
        };
        var beamSlabEdge = detector.DetectBeamSlabClashes(new[] { beamLevel2Edge }, new[] { slabLevel2 });
        Assert(beamSlabEdge.Count == 0, "QcClashDetector: Perimeter edge beam does NOT trigger slicing clash (0 clashes)");
    }

    private static void TestClearanceCheckerStoreyLevelAndZeroLength()
    {
        Console.WriteLine("\n--- Testing ClearanceChecker Storey Level & Zero-Length Guards ---");
        var checker = new ClearanceChecker();

        var colLevel1 = new ColumnDef
        {
            Id = "C1",
            CenterLocation = new Point2D(500, 500),
            WidthMm = 220,
            DepthMm = 220,
            BaseLevel = "Tang 1"
        };

        // Opening close to column on different level (50mm clearance distance)
        var wallLevel2 = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 500),
            EndPoint = new Point2D(2000, 500),
            ThicknessMm = 220,
            BaseLevel = "Tang 2",
            Openings = new[]
            {
                new OpeningDef { Id = "D1", Kind = OpeningKind.Door, DistanceAlongWallMm = 140, WidthMm = 200 }
            }
        };

        var violDiff = checker.CheckOpeningToColumnClearances(new[] { wallLevel2 }, new[] { colLevel1 });
        Assert(violDiff.Count == 0, "ClearanceChecker: Opening on Tang 2 vs Column on Tang 1 produces 0 warnings");

        // Same level opening close to column (50mm clearance: opEnd at 340, col edge at 390)
        var wallLevel1 = new WallDef
        {
            Id = "W2",
            StartPoint = new Point2D(0, 500),
            EndPoint = new Point2D(2000, 500),
            ThicknessMm = 220,
            BaseLevel = "Tang 1",
            Openings = new[]
            {
                new OpeningDef { Id = "D1", Kind = OpeningKind.Door, DistanceAlongWallMm = 140, WidthMm = 200 }
            }
        };
        var violSame = checker.CheckOpeningToColumnClearances(new[] { wallLevel1 }, new[] { colLevel1 });
        Assert(violSame.Count == 1, "ClearanceChecker: Same-level Opening close to Column flags CLR-001 warning");

        // Zero-length wall in clearance checker
        var zeroWall = new WallDef
        {
            Id = "W_Zero",
            StartPoint = new Point2D(500, 500),
            EndPoint = new Point2D(500, 500),
            ThicknessMm = 220,
            BaseLevel = "Tang 1",
            Openings = new[]
            {
                new OpeningDef { Id = "D_Z", Kind = OpeningKind.Door, DistanceAlongWallMm = 0, WidthMm = 200 }
            }
        };
        try
        {
            var violZero = checker.CheckOpeningToColumnClearances(new[] { zeroWall }, new[] { colLevel1 });
            Assert(violZero.Count == 0, "ClearanceChecker: Zero-length wall does not throw");
        }
        catch (Exception ex)
        {
            Assert(false, "ClearanceChecker: Zero-length wall threw exception", ex.Message);
        }

        // Door swing check against rooms across levels (hinge at (140, 500), swing radius 200, threshold 180)
        var roomLevel2 = new RoomDef
        {
            Name = "Phong Ngu",
            Level = "Tang 2",
            TagLocation = new Point2D(150, 510)
        };
        var swingDiff = checker.CheckDoorSwingClearances(new[] { wallLevel1 }, Array.Empty<MepFixtureDef>(), new[] { roomLevel2 });
        Assert(swingDiff.Count == 0, "ClearanceChecker: Door on Tang 1 vs Room on Tang 2 does NOT flag door swing warning");

        var roomLevel1 = new RoomDef
        {
            Name = "Phong Khach",
            Level = "Tang 1",
            TagLocation = new Point2D(150, 510)
        };
        var swingSame = checker.CheckDoorSwingClearances(new[] { wallLevel1 }, Array.Empty<MepFixtureDef>(), new[] { roomLevel1 });
        Assert(swingSame.Count == 1, "ClearanceChecker: Door on Tang 1 vs Room on Tang 1 flags door swing warning CLR-002");
    }

    private static void TestBimModelSerializerRoundTrip()
    {
        Console.WriteLine("\n--- Testing BimModelSerializer Round-Trip ---");
        var model = new BimModel
        {
            ProjectName = "AdversarialTestProject",
            Levels = new[]
            {
                new LevelDef { Name = "Tang 1", ElevationM = 0.0 },
                new LevelDef { Name = "Tang 2", ElevationM = 3.6 }
            },
            Columns = new[]
            {
                new ColumnDef { Id = "C1", BaseLevel = "Tang 1", CenterLocation = new Point2D(2000, 3000), WidthMm = 220, DepthMm = 220 }
            },
            Walls = new[]
            {
                new WallDef { Id = "W1", BaseLevel = "Tang 1", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(4000, 0), ThicknessMm = 220 }
            },
            Slabs = new[]
            {
                new SlabDef
                {
                    Id = "S1",
                    Level = "Tang 1",
                    Boundary = new Polygon2D(new[]
                    {
                        new Point2D(0, 0),
                        new Point2D(4000, 0),
                        new Point2D(4000, 3000),
                        new Point2D(0, 3000)
                    })
                }
            }
        };

        string json = BimModelSerializer.Serialize(model);
        Assert(!string.IsNullOrEmpty(json), "BimModelSerializer: Serialized string is non-empty");

        var roundTrip = BimModelSerializer.Deserialize(json);
        Assert(roundTrip.ProjectName == "AdversarialTestProject", "BimModelSerializer: Preserves ProjectName");
        Assert(roundTrip.Levels.Count == 2, "BimModelSerializer: Preserves Levels count");
        Assert(roundTrip.Columns.Count == 1, "BimModelSerializer: Preserves Columns count");
        Assert(roundTrip.Columns[0].CenterLocation.X == 2000 && roundTrip.Columns[0].CenterLocation.Y == 3000,
            "BimModelSerializer: Column coordinates preserved exactly");
        Assert(roundTrip.Slabs.Count == 1, "BimModelSerializer: Preserves Slabs count");
        Assert(Math.Abs(roundTrip.Slabs[0].Boundary.Area - 12000000.0) < 1e-3,
            "BimModelSerializer: Slab Boundary area preserved exactly");
    }
}
