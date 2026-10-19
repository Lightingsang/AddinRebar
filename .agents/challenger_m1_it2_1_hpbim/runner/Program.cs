using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        Console.WriteLine("  EMPIRICAL CHALLENGER TEST RUNNER - MILESTONE 1 ITERATION 2 VERIFICATION");
        Console.WriteLine("  Target: HPBim.Core.dll (.NET Standard 2.0 / .NET 10.0 runtime)");
        Console.WriteLine("================================================================================");
        Console.WriteLine($"Start Time (UTC): {DateTime.UtcNow:O}");
        Console.WriteLine();

        RunVector2DJsonTests();
        RunPoint2DJsonTests();
        RunPoint3DJsonTests();
        RunPolygon2DJsonTests();
        RunFullBimModelJsonTests();
        RunPolygonContainsBoundaryTests();
        RunQcClashDetectorMultiStoreyTests();
        RunQcClashDetectorPerimeterBeamTests();
        RunQcClashDetectorZeroLengthWallTests();
        RunClearanceCheckerMultiStoreyAndZeroLengthTests();
        RunTcvnRegressionTests();
        RunAdversarialStressAndCodeGovernanceTests();

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

    // -------------------------------------------------------------------------
    // 1. Vector2D JSON Tests
    // -------------------------------------------------------------------------
    private static void RunVector2DJsonTests()
    {
        Console.WriteLine("--- 1. Vector2D JSON Serialization & Deserialization Tests ---");

        var v1 = new Vector2D(123.456, -789.012);
        string json = "";
        try
        {
            json = JsonSerializer.Serialize(v1, BimModelSerializer.CreateOptions(false));
            Assert(true, "Vector2D: Serialize does not throw cycle exception", "");
        }
        catch (Exception ex)
        {
            Assert(false, "Vector2D: Serialize does not throw cycle exception", ex.ToString());
            return;
        }

        // Verify JSON contents only has x and y
        Assert(json.Contains("\"x\":123.456") || json.Contains("\"x\": 123.456"),
            "Vector2D: JSON contains property 'x'", json);
        Assert(json.Contains("\"y\":-789.012") || json.Contains("\"y\": -789.012"),
            "Vector2D: JSON contains property 'y'", json);
        Assert(!json.Contains("leftNormal", StringComparison.OrdinalIgnoreCase),
            "Vector2D: JSON does NOT contain 'leftNormal' (cycle eliminated)", json);
        Assert(!json.Contains("rightNormal", StringComparison.OrdinalIgnoreCase),
            "Vector2D: JSON does NOT contain 'rightNormal' (cycle eliminated)", json);
        Assert(!json.Contains("length", StringComparison.OrdinalIgnoreCase),
            "Vector2D: JSON does NOT contain computed 'length'", json);
        Assert(!json.Contains("angleRad", StringComparison.OrdinalIgnoreCase),
            "Vector2D: JSON does NOT contain computed 'angleRad'", json);

        // Deserialization roundtrip
        var v1Des = JsonSerializer.Deserialize<Vector2D>(json, BimModelSerializer.CreateOptions(false));
        Assert(Math.Abs(v1Des.X - 123.456) < 1e-6 && Math.Abs(v1Des.Y - (-789.012)) < 1e-6,
            "Vector2D: Deserialized coordinates match exactly", $"Got ({v1Des.X}, {v1Des.Y})");

        // Zero and Unit vectors
        var vZero = Vector2D.Zero;
        string jsonZero = JsonSerializer.Serialize(vZero, BimModelSerializer.CreateOptions(false));
        var vZeroDes = JsonSerializer.Deserialize<Vector2D>(jsonZero, BimModelSerializer.CreateOptions(false));
        Assert(vZeroDes.X == 0.0 && vZeroDes.Y == 0.0, "Vector2D: Zero roundtrip preserves 0.0", "");

        // Large coordinates
        var vLarge = new Vector2D(10000000.5, -99999999.25);
        string jsonLarge = JsonSerializer.Serialize(vLarge, BimModelSerializer.CreateOptions(false));
        var vLargeDes = JsonSerializer.Deserialize<Vector2D>(jsonLarge, BimModelSerializer.CreateOptions(false));
        Assert(Math.Abs(vLargeDes.X - 10000000.5) < 1e-4 && Math.Abs(vLargeDes.Y - (-99999999.25)) < 1e-4,
            "Vector2D: Large coordinates roundtrip cleanly", $"Got ({vLargeDes.X}, {vLargeDes.Y})");
    }

    // -------------------------------------------------------------------------
    // 2. Point2D JSON Tests
    // -------------------------------------------------------------------------
    private static void RunPoint2DJsonTests()
    {
        Console.WriteLine("--- 2. Point2D JSON Serialization & Deserialization Tests ---");

        var pt = new Point2D(456.789, 987.654);
        string json = JsonSerializer.Serialize(pt, BimModelSerializer.CreateOptions(false));
        var ptDes = JsonSerializer.Deserialize<Point2D>(json, BimModelSerializer.CreateOptions(false));

        Assert(ptDes.X != 0.0 && ptDes.Y != 0.0,
            "Point2D: Coordinates are NOT defaulted to (0, 0)", $"Got ({ptDes.X}, {ptDes.Y})");
        Assert(Math.Abs(ptDes.X - 456.789) < 1e-6 && Math.Abs(ptDes.Y - 987.654) < 1e-6,
            "Point2D: Roundtrip preserves exact coordinates", $"Got ({ptDes.X}, {ptDes.Y})");

        // Test deserializing from camelCase JSON explicitly
        string camelCaseJson = "{\"x\": 3500.5, \"y\": 7200.25}";
        var ptCamel = JsonSerializer.Deserialize<Point2D>(camelCaseJson, BimModelSerializer.CreateOptions(false));
        Assert(Math.Abs(ptCamel.X - 3500.5) < 1e-6 && Math.Abs(ptCamel.Y - 7200.25) < 1e-6,
            "Point2D: Deserialization from camelCase json string binds parameters correctly", $"Got ({ptCamel.X}, {ptCamel.Y})");

        // Test deserializing from PascalCase JSON explicitly
        string pascalCaseJson = "{\"X\": 1200.0, \"Y\": 2400.0}";
        var ptPascal = JsonSerializer.Deserialize<Point2D>(pascalCaseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert(Math.Abs(ptPascal.X - 1200.0) < 1e-6 && Math.Abs(ptPascal.Y - 2400.0) < 1e-6,
            "Point2D: Deserialization from PascalCase json binds parameters correctly", $"Got ({ptPascal.X}, {ptPascal.Y})");
    }

    // -------------------------------------------------------------------------
    // 3. Point3D JSON Tests
    // -------------------------------------------------------------------------
    private static void RunPoint3DJsonTests()
    {
        Console.WriteLine("--- 3. Point3D JSON Serialization & Deserialization Tests ---");

        var pt3 = new Point3D(100.5, 200.75, 300.125);
        string json = JsonSerializer.Serialize(pt3, BimModelSerializer.CreateOptions(false));
        var pt3Des = JsonSerializer.Deserialize<Point3D>(json, BimModelSerializer.CreateOptions(false));

        Assert(pt3Des.X != 0.0 && pt3Des.Y != 0.0 && pt3Des.Z != 0.0,
            "Point3D: Coordinates are NOT defaulted to (0, 0, 0)", $"Got ({pt3Des.X}, {pt3Des.Y}, {pt3Des.Z})");
        Assert(Math.Abs(pt3Des.X - 100.5) < 1e-6 && Math.Abs(pt3Des.Y - 200.75) < 1e-6 && Math.Abs(pt3Des.Z - 300.125) < 1e-6,
            "Point3D: Roundtrip preserves exact coordinates", $"Got ({pt3Des.X}, {pt3Des.Y}, {pt3Des.Z})");

        // Test deserializing from camelCase JSON
        string camelJson = "{\"x\": 15.0, \"y\": 30.0, \"z\": 45.0}";
        var ptCamel = JsonSerializer.Deserialize<Point3D>(camelJson, BimModelSerializer.CreateOptions(false));
        Assert(ptCamel.X == 15.0 && ptCamel.Y == 30.0 && ptCamel.Z == 45.0,
            "Point3D: Deserialization from camelCase json binds x, y, z accurately", $"Got ({ptCamel.X}, {ptCamel.Y}, {ptCamel.Z})");
    }

    // -------------------------------------------------------------------------
    // 4. Polygon2D JSON Tests
    // -------------------------------------------------------------------------
    private static void RunPolygon2DJsonTests()
    {
        Console.WriteLine("--- 4. Polygon2D JSON Serialization & Deserialization Tests ---");

        var poly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(5000, 0),
            new Point2D(5000, 4000),
            new Point2D(0, 4000)
        });

        string json = "";
        try
        {
            json = JsonSerializer.Serialize(poly, BimModelSerializer.CreateOptions(false));
            Assert(true, "Polygon2D: Serialize does not throw exception", "");
        }
        catch (Exception ex)
        {
            Assert(false, "Polygon2D: Serialize does not throw exception", ex.ToString());
            return;
        }

        // Verify computed properties are excluded from JSON
        Assert(!json.Contains("area", StringComparison.OrdinalIgnoreCase),
            "Polygon2D: JSON does NOT contain computed 'area'", json);
        Assert(!json.Contains("perimeter", StringComparison.OrdinalIgnoreCase),
            "Polygon2D: JSON does NOT contain computed 'perimeter'", json);
        Assert(!json.Contains("signedArea", StringComparison.OrdinalIgnoreCase),
            "Polygon2D: JSON does NOT contain computed 'signedArea'", json);
        Assert(!json.Contains("centroid", StringComparison.OrdinalIgnoreCase),
            "Polygon2D: JSON does NOT contain computed 'centroid'", json);
        Assert(!json.Contains("boundingBox", StringComparison.OrdinalIgnoreCase),
            "Polygon2D: JSON does NOT contain computed 'boundingBox'", json);

        // Deserialization
        Polygon2D? polyDes = null;
        try
        {
            polyDes = JsonSerializer.Deserialize<Polygon2D>(json, BimModelSerializer.CreateOptions(false));
            Assert(true, "Polygon2D: Deserialization succeeds without InvalidOperationException", "");
        }
        catch (Exception ex)
        {
            Assert(false, "Polygon2D: Deserialization succeeds without InvalidOperationException", ex.ToString());
            return;
        }

        Assert(polyDes != null && polyDes.VertexCount == 4,
            "Polygon2D: Deserialized vertex count matches 4", $"Got {polyDes?.VertexCount}");
        Assert(Math.Abs(polyDes!.Area - 20_000_000.0) < 1e-4,
            "Polygon2D: Computed Area is 20,000,000 mm2 on deserialized instance", $"Got {polyDes.Area}");
        Assert(Math.Abs(polyDes.Perimeter - 18_000.0) < 1e-4,
            "Polygon2D: Computed Perimeter is 18,000 mm on deserialized instance", $"Got {polyDes.Perimeter}");
        Assert(Math.Abs(polyDes.Centroid.X - 2500.0) < 1e-4 && Math.Abs(polyDes.Centroid.Y - 2000.0) < 1e-4,
            "Polygon2D: Computed Centroid is (2500, 2000) on deserialized instance", $"Got {polyDes.Centroid}");

        // Empty Polygon test
        var emptyPoly = Polygon2D.Empty;
        string emptyJson = JsonSerializer.Serialize(emptyPoly, BimModelSerializer.CreateOptions(false));
        var emptyPolyDes = JsonSerializer.Deserialize<Polygon2D>(emptyJson, BimModelSerializer.CreateOptions(false));
        Assert(emptyPolyDes != null && emptyPolyDes.VertexCount == 0,
            "Polygon2D: Empty polygon roundtrip preserves 0 vertices", $"Got {emptyPolyDes?.VertexCount}");
    }

    // -------------------------------------------------------------------------
    // 5. Full BimModel JSON Tests
    // -------------------------------------------------------------------------
    private static void RunFullBimModelJsonTests()
    {
        Console.WriteLine("--- 5. Full BimModel JSON Serialization & Deserialization Tests ---");

        var model = new BimModel
        {
            ProjectName = "Adversarial_Townhouse_Test",
            Levels = new[]
            {
                new LevelDef { Name = "Tang 1", ElevationM = 0.0, FloorNumber = 1 },
                new LevelDef { Name = "Tang 2", ElevationM = 3.8, FloorNumber = 2 }
            },
            Grids = new[]
            {
                new AxisGrid { Name = "X1", StartPoint = new Point2D(0, -1000), EndPoint = new Point2D(0, 7000), Direction = GridDirection.X },
                new AxisGrid { Name = "YA", StartPoint = new Point2D(-1000, 0), EndPoint = new Point2D(7000, 0), Direction = GridDirection.Y }
            },
            Columns = new[]
            {
                new ColumnDef { Id = "C1", CenterLocation = new Point2D(2000, 3000), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 1", TopLevel = "Tang 2" },
                new ColumnDef { Id = "C2", CenterLocation = new Point2D(5000, 3000), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 2", TopLevel = "Tang 3" }
            },
            Walls = new[]
            {
                new WallDef
                {
                    Id = "W1",
                    StartPoint = new Point2D(0, 3000),
                    EndPoint = new Point2D(6000, 3000),
                    ThicknessMm = 220,
                    BaseLevel = "Tang 1",
                    TopLevel = "Tang 2",
                    Openings = new[]
                    {
                        new OpeningDef
                        {
                            Id = "D1",
                            HostWallId = "W1",
                            DistanceAlongWallMm = 500,
                            WidthMm = 900,
                            HeightMm = 2200,
                            Kind = OpeningKind.Door
                        }
                    }
                }
            },
            Beams = new[]
            {
                new BeamDef
                {
                    Id = "B1",
                    StartPoint = new Point2D(0, 0),
                    EndPoint = new Point2D(6000, 0),
                    WidthMm = 200,
                    HeightMm = 400,
                    Level = "Tang 2"
                }
            },
            Slabs = new[]
            {
                new SlabDef
                {
                    Id = "S1",
                    Boundary = new Polygon2D(new[]
                    {
                        new Point2D(0, 0),
                        new Point2D(6000, 0),
                        new Point2D(6000, 4000),
                        new Point2D(0, 4000)
                    }),
                    ThicknessMm = 100,
                    Level = "Tang 2"
                }
            },
            Rooms = new[]
            {
                new RoomDef
                {
                    Name = "Phong Khach",
                    Level = "Tang 1",
                    TagLocation = new Point2D(3000, 2000),
                    BoundaryPolygon = new Polygon2D(new[]
                    {
                        new Point2D(0, 0),
                        new Point2D(6000, 0),
                        new Point2D(6000, 4000),
                        new Point2D(0, 4000)
                    })
                }
            },
            Fixtures = new[]
            {
                new MepFixtureDef
                {
                    Id = "FX1",
                    FamilyName = "WC_Bidet",
                    Location = new Point3D(1200, 1500, 0),
                    Level = "Tang 1"
                }
            },
            Pipes = new[]
            {
                new PipeRunDef
                {
                    Id = "P1",
                    Points = new[] { new Point3D(100, 100, 0), new Point3D(100, 100, 3800) },
                    DiameterMm = 60,
                    SystemType = PipeSystemType.SoilDrain
                }
            },
            Rebars = new[]
            {
                new RebarDef
                {
                    Id = "RB1",
                    HostElementId = "B1",
                    DiameterMm = 18,
                    LengthMm = 15600,
                    Quantity = 2
                }
            }
        };

        string modelJson = "";
        try
        {
            modelJson = BimModelSerializer.Serialize(model, indented: true);
            Assert(true, "BimModel: Full model serializes without exception", "");
        }
        catch (Exception ex)
        {
            Assert(false, "BimModel: Full model serializes without exception", ex.ToString());
            return;
        }

        BimModel? modelDes = null;
        try
        {
            modelDes = BimModelSerializer.Deserialize(modelJson);
            Assert(true, "BimModel: Full model deserializes cleanly", "");
        }
        catch (Exception ex)
        {
            Assert(false, "BimModel: Full model deserializes cleanly", ex.ToString());
            return;
        }

        // Verify counts on all 11 collections
        Assert(modelDes!.Levels.Count == 2, "BimModel: Levels count matches (2)", $"Got {modelDes.Levels.Count}");
        Assert(modelDes.Grids.Count == 2, "BimModel: Grids count matches (2)", $"Got {modelDes.Grids.Count}");
        Assert(modelDes.Columns.Count == 2, "BimModel: Columns count matches (2)", $"Got {modelDes.Columns.Count}");
        Assert(modelDes.Walls.Count == 1, "BimModel: Walls count matches (1)", $"Got {modelDes.Walls.Count}");
        Assert(modelDes.Beams.Count == 1, "BimModel: Beams count matches (1)", $"Got {modelDes.Beams.Count}");
        Assert(modelDes.Slabs.Count == 1, "BimModel: Slabs count matches (1)", $"Got {modelDes.Slabs.Count}");
        Assert(modelDes.Rooms.Count == 1, "BimModel: Rooms count matches (1)", $"Got {modelDes.Rooms.Count}");
        Assert(modelDes.Fixtures.Count == 1, "BimModel: Fixtures count matches (1)", $"Got {modelDes.Fixtures.Count}");
        Assert(modelDes.Pipes.Count == 1, "BimModel: Pipes count matches (1)", $"Got {modelDes.Pipes.Count}");
        Assert(modelDes.Rebars.Count == 1, "BimModel: Rebars count matches (1)", $"Got {modelDes.Rebars.Count}");

        // Verify non-zero coordinates preserved on deserialized model elements
        var col1 = modelDes.Columns[0];
        Assert(col1.CenterLocation.X == 2000 && col1.CenterLocation.Y == 3000,
            "BimModel: Column coordinates preserved (not 0,0)", $"Got ({col1.CenterLocation.X}, {col1.CenterLocation.Y})");

        var wall1 = modelDes.Walls[0];
        Assert(wall1.StartPoint.X == 0 && wall1.StartPoint.Y == 3000 && wall1.EndPoint.X == 6000 && wall1.EndPoint.Y == 3000,
            "BimModel: Wall coordinates preserved", $"Got ({wall1.StartPoint}) to ({wall1.EndPoint})");
        Assert(wall1.Openings.Count == 1 && wall1.Openings[0].DistanceAlongWallMm == 500,
            "BimModel: Wall opening preserved", "");

        var beam1 = modelDes.Beams[0];
        Assert(beam1.StartPoint.X == 0 && beam1.EndPoint.X == 6000,
            "BimModel: Beam coordinates preserved", $"Got ({beam1.StartPoint}) to ({beam1.EndPoint})");

        var slab1 = modelDes.Slabs[0];
        Assert(slab1.Boundary.VertexCount == 4 && slab1.Boundary.Area == 24_000_000,
            "BimModel: Slab boundary vertices & area preserved", $"Got {slab1.Boundary.Area}");

        var room1 = modelDes.Rooms[0];
        Assert(room1.TagLocation.X == 3000 && room1.TagLocation.Y == 2000,
            "BimModel: Room tag location preserved", $"Got ({room1.TagLocation})");

        var fix1 = modelDes.Fixtures[0];
        Assert(fix1.Location.X == 1200 && fix1.Location.Y == 1500 && fix1.Location.Z == 0,
            "BimModel: Fixture 3D location preserved", $"Got ({fix1.Location})");

        var pipe1 = modelDes.Pipes[0];
        Assert(pipe1.Points.Count == 2 && pipe1.Points[0].Z == 0 && pipe1.Points[1].Z == 3800,
            "BimModel: Pipe 3D coordinates preserved", $"Got points {pipe1.Points.Count}");

        double expectedRebarWeight = (18.0 * 18.0 / 162.2) * 15.6 * 2;
        Assert(Math.Abs(modelDes.TotalRebarWeightKg - expectedRebarWeight) < 1e-2,
            "BimModel: TotalRebarWeightKg computed accurately", $"Got {modelDes.TotalRebarWeightKg:F2} vs expected {expectedRebarWeight:F2}");
    }

    // -------------------------------------------------------------------------
    // 6. Polygon2D.Contains(pt, includeBoundary) Boundary Tests
    // -------------------------------------------------------------------------
    private static void RunPolygonContainsBoundaryTests()
    {
        Console.WriteLine("--- 6. Polygon2D.Contains(pt, includeBoundary) Boundary Tests ---");

        var poly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(10000, 0),
            new Point2D(10000, 6000),
            new Point2D(0, 6000)
        });

        // 1. Interior point
        var ptInside = new Point2D(5000, 3000);
        Assert(poly.Contains(ptInside, includeBoundary: true), "Polygon2D: Interior point with includeBoundary=true is TRUE", "");
        Assert(poly.Contains(ptInside, includeBoundary: false), "Polygon2D: Interior point with includeBoundary=false is TRUE", "");

        // 2. Exterior point
        var ptOutside = new Point2D(12000, 3000);
        Assert(!poly.Contains(ptOutside, includeBoundary: true), "Polygon2D: Exterior point with includeBoundary=true is FALSE", "");
        Assert(!poly.Contains(ptOutside, includeBoundary: false), "Polygon2D: Exterior point with includeBoundary=false is FALSE", "");

        // 3. Edge midpoints: MUST BE FALSE when includeBoundary = false!
        var ptBottomMid = new Point2D(5000, 0);
        Assert(poly.Contains(ptBottomMid, includeBoundary: true),
            "Polygon2D: Bottom edge midpoint with includeBoundary=true is TRUE", "");
        Assert(!poly.Contains(ptBottomMid, includeBoundary: false),
            "Polygon2D: Bottom edge midpoint with includeBoundary=false is FALSE (CRITICAL FIX)", "");

        var ptRightMid = new Point2D(10000, 3000);
        Assert(poly.Contains(ptRightMid, includeBoundary: true),
            "Polygon2D: Right edge midpoint with includeBoundary=true is TRUE", "");
        Assert(!poly.Contains(ptRightMid, includeBoundary: false),
            "Polygon2D: Right edge midpoint with includeBoundary=false is FALSE", "");

        var ptTopMid = new Point2D(5000, 6000);
        Assert(poly.Contains(ptTopMid, includeBoundary: true),
            "Polygon2D: Top edge midpoint with includeBoundary=true is TRUE", "");
        Assert(!poly.Contains(ptTopMid, includeBoundary: false),
            "Polygon2D: Top edge midpoint with includeBoundary=false is FALSE", "");

        var ptLeftMid = new Point2D(0, 3000);
        Assert(poly.Contains(ptLeftMid, includeBoundary: true),
            "Polygon2D: Left edge midpoint with includeBoundary=true is TRUE", "");
        Assert(!poly.Contains(ptLeftMid, includeBoundary: false),
            "Polygon2D: Left edge midpoint with includeBoundary=false is FALSE", "");

        // 4. Vertices (Corners)
        var corner0 = new Point2D(0, 0);
        Assert(poly.Contains(corner0, includeBoundary: true), "Polygon2D: Corner (0,0) includeBoundary=true is TRUE", "");
        Assert(!poly.Contains(corner0, includeBoundary: false), "Polygon2D: Corner (0,0) includeBoundary=false is FALSE", "");

        var corner1 = new Point2D(10000, 0);
        Assert(poly.Contains(corner1, includeBoundary: true), "Polygon2D: Corner (10000,0) includeBoundary=true is TRUE", "");
        Assert(!poly.Contains(corner1, includeBoundary: false), "Polygon2D: Corner (10000,0) includeBoundary=false is FALSE", "");

        var corner2 = new Point2D(10000, 6000);
        Assert(poly.Contains(corner2, includeBoundary: true), "Polygon2D: Corner (10000,6000) includeBoundary=true is TRUE", "");
        Assert(!poly.Contains(corner2, includeBoundary: false), "Polygon2D: Corner (10000,6000) includeBoundary=false is FALSE", "");

        var corner3 = new Point2D(0, 6000);
        Assert(poly.Contains(corner3, includeBoundary: true), "Polygon2D: Corner (0,6000) includeBoundary=true is TRUE", "");
        Assert(!poly.Contains(corner3, includeBoundary: false), "Polygon2D: Corner (0,6000) includeBoundary=false is FALSE", "");

        // 5. Diagonal edge triangle
        var tri = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(6000, 0),
            new Point2D(3000, 4000)
        });
        var ptDiag = new Point2D(1500, 2000); // Exactly on segment (0,0) -> (3000,4000)
        Assert(tri.Contains(ptDiag, includeBoundary: true), "Polygon2D: Diagonal segment point includeBoundary=true is TRUE", "");
        Assert(!tri.Contains(ptDiag, includeBoundary: false), "Polygon2D: Diagonal segment point includeBoundary=false is FALSE", "");

        // 6. Concave L-shaped polygon
        var lPoly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(6000, 0),
            new Point2D(6000, 3000),
            new Point2D(3000, 3000),
            new Point2D(3000, 6000),
            new Point2D(0, 6000)
        });
        var reflexCorner = new Point2D(3000, 3000);
        Assert(lPoly.Contains(reflexCorner, includeBoundary: true), "Polygon2D: L-shape reflex corner includeBoundary=true is TRUE", "");
        Assert(!lPoly.Contains(reflexCorner, includeBoundary: false), "Polygon2D: L-shape reflex corner includeBoundary=false is FALSE", "");

        var reflexEdgeMid = new Point2D(4500, 3000);
        Assert(lPoly.Contains(reflexEdgeMid, includeBoundary: true), "Polygon2D: L-shape inner edge mid includeBoundary=true is TRUE", "");
        Assert(!lPoly.Contains(reflexEdgeMid, includeBoundary: false), "Polygon2D: L-shape inner edge mid includeBoundary=false is FALSE", "");

        var cutoutPoint = new Point2D(4500, 4500); // outside in the cutout
        Assert(!lPoly.Contains(cutoutPoint, includeBoundary: true), "Polygon2D: L-shape cutout exterior point is FALSE", "");
        Assert(!lPoly.Contains(cutoutPoint, includeBoundary: false), "Polygon2D: L-shape cutout exterior point is FALSE", "");
    }

    // -------------------------------------------------------------------------
    // 7. QcClashDetector Multi-Storey Tests
    // -------------------------------------------------------------------------
    private static void RunQcClashDetectorMultiStoreyTests()
    {
        Console.WriteLine("--- 7. QcClashDetector Multi-Storey Column vs Opening Tests ---");

        var detector = new QcClashDetector();

        var colL1 = new ColumnDef
        {
            Id = "C_L1",
            CenterLocation = new Point2D(2000, 0),
            WidthMm = 220,
            DepthMm = 220,
            BaseLevel = "Tang 1"
        };

        var wallL2 = new WallDef
        {
            Id = "W_L2",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            ThicknessMm = 220,
            BaseLevel = "Tang 2",
            Openings = new[]
            {
                new OpeningDef
                {
                    Id = "D_L2",
                    DistanceAlongWallMm = 1800, // op center = 1800 + 450 = 2250, distance to col = 250mm < (450 + 110 = 560mm) -> 2D CLASH!
                    WidthMm = 900,
                    Kind = OpeningKind.Door
                }
            }
        };

        // Multi-storey test: Column on Tang 1, Wall/Opening on Tang 2
        var clashesMultiStorey = detector.DetectColumnOpeningClashes(new[] { colL1 }, new[] { wallL2 });
        Assert(clashesMultiStorey.Count == 0,
            "QcClashDetector: Multi-storey column on Level 1 vs opening on Level 2 does NOT clash (0 clashes)",
            $"Detected {clashesMultiStorey.Count} clashes");

        // Same-storey test: Change Wall to Tang 1
        var wallL1 = wallL2 with { BaseLevel = "Tang 1" };
        var clashesSameStorey = detector.DetectColumnOpeningClashes(new[] { colL1 }, new[] { wallL1 });
        Assert(clashesSameStorey.Count == 1,
            "QcClashDetector: Same-storey column vs opening detects 1 clash",
            $"Detected {clashesSameStorey.Count} clashes");
        if (clashesSameStorey.Count > 0)
        {
            Assert(clashesSameStorey[0].ClashType == "ColumnVsOpening",
                "QcClashDetector: Clash type is 'ColumnVsOpening'", clashesSameStorey[0].ClashType);
        }

        // Case-insensitivity test: "tang 1" vs "TANG 1"
        var colLower = colL1 with { BaseLevel = "tang 1" };
        var wallUpper = wallL2 with { BaseLevel = "TANG 1" };
        var clashesCaseInsensitive = detector.DetectColumnOpeningClashes(new[] { colLower }, new[] { wallUpper });
        Assert(clashesCaseInsensitive.Count == 1,
            "QcClashDetector: Level comparison is case-insensitive ('tang 1' vs 'TANG 1')",
            $"Detected {clashesCaseInsensitive.Count} clashes");

        // Null level handling (should safely compare empty strings)
        var colNull = colL1 with { BaseLevel = "" };
        var wallNull = wallL2 with { BaseLevel = "" };
        var clashesNullLevel = detector.DetectColumnOpeningClashes(new[] { colNull }, new[] { wallNull });
        Assert(clashesNullLevel.Count == 1,
            "QcClashDetector: Empty base levels compare safely and detect clash on same empty level",
            $"Detected {clashesNullLevel.Count} clashes");
    }

    // -------------------------------------------------------------------------
    // 8. QcClashDetector Perimeter Beam Tests
    // -------------------------------------------------------------------------
    private static void RunQcClashDetectorPerimeterBeamTests()
    {
        Console.WriteLine("--- 8. QcClashDetector Perimeter Beam vs Slab Tests ---");

        var detector = new QcClashDetector();

        var slab = new SlabDef
        {
            Id = "S_L2",
            Level = "Tang 2",
            Boundary = new Polygon2D(new[]
            {
                new Point2D(0, 0),
                new Point2D(6000, 0),
                new Point2D(6000, 4000),
                new Point2D(0, 4000)
            })
        };

        // Perimeter beam 1: Bottom edge (0,0) -> (6000,0), mid = (3000, 0)
        var beamPerimeterBottom = new BeamDef
        {
            Id = "B_PERIMETER_BOTTOM",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(6000, 0),
            Level = "Tang 2",
            WidthMm = 200,
            HeightMm = 400
        };

        var clashesBottom = detector.DetectBeamSlabClashes(new[] { beamPerimeterBottom }, new[] { slab });
        Assert(clashesBottom.Count == 0,
            "QcClashDetector: Bottom perimeter beam does NOT trigger slicing clash (0 clashes)",
            $"Detected {clashesBottom.Count} clashes");

        // Perimeter beam 2: Top edge (0,4000) -> (6000,4000), mid = (3000, 4000)
        var beamPerimeterTop = new BeamDef
        {
            Id = "B_PERIMETER_TOP",
            StartPoint = new Point2D(0, 4000),
            EndPoint = new Point2D(6000, 4000),
            Level = "Tang 2",
            WidthMm = 200,
            HeightMm = 400
        };
        var clashesTop = detector.DetectBeamSlabClashes(new[] { beamPerimeterTop }, new[] { slab });
        Assert(clashesTop.Count == 0,
            "QcClashDetector: Top perimeter beam does NOT trigger slicing clash (0 clashes)",
            $"Detected {clashesTop.Count} clashes");

        // Perimeter beam 3: Left edge (0,0) -> (0,4000), mid = (0, 2000)
        var beamPerimeterLeft = new BeamDef
        {
            Id = "B_PERIMETER_LEFT",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(0, 4000),
            Level = "Tang 2",
            WidthMm = 200,
            HeightMm = 400
        };
        var clashesLeft = detector.DetectBeamSlabClashes(new[] { beamPerimeterLeft }, new[] { slab });
        Assert(clashesLeft.Count == 0,
            "QcClashDetector: Left perimeter beam does NOT trigger slicing clash (0 clashes)",
            $"Detected {clashesLeft.Count} clashes");

        // Perimeter beam 4: Right edge (6000,0) -> (6000,4000), mid = (6000, 2000)
        var beamPerimeterRight = new BeamDef
        {
            Id = "B_PERIMETER_RIGHT",
            StartPoint = new Point2D(6000, 0),
            EndPoint = new Point2D(6000, 4000),
            Level = "Tang 2",
            WidthMm = 200,
            HeightMm = 400
        };
        var clashesRight = detector.DetectBeamSlabClashes(new[] { beamPerimeterRight }, new[] { slab });
        Assert(clashesRight.Count == 0,
            "QcClashDetector: Right perimeter beam does NOT trigger slicing clash (0 clashes)",
            $"Detected {clashesRight.Count} clashes");

        // Interior beam (Slicing): (3000,0) -> (3000,4000), mid = (3000, 2000) -> INSIDE!
        var beamInterior = new BeamDef
        {
            Id = "B_INTERIOR",
            StartPoint = new Point2D(3000, 0),
            EndPoint = new Point2D(3000, 4000),
            Level = "Tang 2",
            WidthMm = 200,
            HeightMm = 400
        };
        var clashesInterior = detector.DetectBeamSlabClashes(new[] { beamInterior }, new[] { slab });
        Assert(clashesInterior.Count == 1,
            "QcClashDetector: Interior secondary beam correctly triggers 1 slicing clash",
            $"Detected {clashesInterior.Count} clashes");

        // Multi-storey beam vs slab: Interior beam on Tang 1, slab on Tang 2
        var beamInteriorTang1 = beamInterior with { Level = "Tang 1" };
        var clashesBeamMultiStorey = detector.DetectBeamSlabClashes(new[] { beamInteriorTang1 }, new[] { slab });
        Assert(clashesBeamMultiStorey.Count == 0,
            "QcClashDetector: Multi-storey beam on Tang 1 does NOT clash with slab on Tang 2",
            $"Detected {clashesBeamMultiStorey.Count} clashes");

        // Tie beam check: beam marked IsTieBeam=true does NOT clash
        var beamTie = beamInterior with { IsTieBeam = true };
        var clashesTie = detector.DetectBeamSlabClashes(new[] { beamTie }, new[] { slab });
        Assert(clashesTie.Count == 0,
            "QcClashDetector: Tie beam (IsTieBeam=true) is skipped from slab slicing clashes",
            $"Detected {clashesTie.Count} clashes");
    }

    // -------------------------------------------------------------------------
    // 9. QcClashDetector Zero-Length Wall Tests
    // -------------------------------------------------------------------------
    private static void RunQcClashDetectorZeroLengthWallTests()
    {
        Console.WriteLine("--- 9. QcClashDetector Zero-Length Wall Robustness Tests ---");

        var detector = new QcClashDetector();

        var col = new ColumnDef
        {
            Id = "C1",
            CenterLocation = new Point2D(1000, 1000),
            WidthMm = 220,
            DepthMm = 220,
            BaseLevel = "Tang 1"
        };

        // Exact zero-length wall (StartPoint == EndPoint)
        var zeroWall = new WallDef
        {
            Id = "W_ZERO",
            StartPoint = new Point2D(1000, 1000),
            EndPoint = new Point2D(1000, 1000),
            BaseLevel = "Tang 1",
            Openings = new[]
            {
                new OpeningDef { Id = "OP1", DistanceAlongWallMm = 0, WidthMm = 900 }
            }
        };

        try
        {
            var clashesZero = detector.DetectColumnOpeningClashes(new[] { col }, new[] { zeroWall });
            Assert(true, "QcClashDetector: Zero-length wall (length = 0) does NOT throw InvalidOperationException", "");
            Assert(clashesZero.Count == 0, "QcClashDetector: Zero-length wall returns 0 clashes", $"Got {clashesZero.Count}");
        }
        catch (Exception ex)
        {
            Assert(false, "QcClashDetector: Zero-length wall (length = 0) does NOT throw InvalidOperationException", ex.ToString());
        }

        // Sub-millimeter wall (length < 1e-4)
        var tinyWall = new WallDef
        {
            Id = "W_TINY",
            StartPoint = new Point2D(1000, 1000),
            EndPoint = new Point2D(1000.00001, 1000.00001),
            BaseLevel = "Tang 1",
            Openings = new[]
            {
                new OpeningDef { Id = "OP2", DistanceAlongWallMm = 0, WidthMm = 900 }
            }
        };

        try
        {
            var clashesTiny = detector.DetectColumnOpeningClashes(new[] { col }, new[] { tinyWall });
            Assert(true, "QcClashDetector: Sub-millimeter wall (length < 1e-4) does NOT throw InvalidOperationException", "");
            Assert(clashesTiny.Count == 0, "QcClashDetector: Sub-millimeter wall returns 0 clashes", $"Got {clashesTiny.Count}");
        }
        catch (Exception ex)
        {
            Assert(false, "QcClashDetector: Sub-millimeter wall (length < 1e-4) does NOT throw InvalidOperationException", ex.ToString());
        }
    }

    // -------------------------------------------------------------------------
    // 10. ClearanceChecker Multi-Storey and Zero-Length Tests
    // -------------------------------------------------------------------------
    private static void RunClearanceCheckerMultiStoreyAndZeroLengthTests()
    {
        Console.WriteLine("--- 10. ClearanceChecker Multi-Storey & Zero-Length Wall Tests ---");

        var checker = new ClearanceChecker();

        var col = new ColumnDef
        {
            Id = "C1",
            CenterLocation = new Point2D(1000, 0),
            WidthMm = 220,
            DepthMm = 220,
            BaseLevel = "Tang 1"
        };

        // Wall on Tang 2 with opening near Column C1 in 2D
        var wallL2 = new WallDef
        {
            Id = "W_L2",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            BaseLevel = "Tang 2",
            Openings = new[]
            {
                new OpeningDef
                {
                    Id = "D_L2",
                    DistanceAlongWallMm = 800, // opStart = 800, colCenter = 1000, col edge = 890, dist = 90mm (< 100mm threshold)
                    WidthMm = 900,
                    Kind = OpeningKind.Door
                }
            }
        };

        // Multi-storey opening vs column clearance: Tang 1 vs Tang 2
        var violsMultiStorey = checker.CheckOpeningToColumnClearances(new[] { wallL2 }, new[] { col });
        Assert(violsMultiStorey.Count == 0,
            "ClearanceChecker: Multi-storey opening on Level 2 vs column on Level 1 produces 0 violations",
            $"Got {violsMultiStorey.Count}");

        // Same-storey opening vs column clearance: Tang 1 vs Tang 1
        var wallL1 = wallL2 with { BaseLevel = "Tang 1" };
        var violsSameStorey = checker.CheckOpeningToColumnClearances(new[] { wallL1 }, new[] { col });
        Assert(violsSameStorey.Count == 1,
            "ClearanceChecker: Same-storey opening near column produces 1 violation (CLR-001)",
            $"Got {violsSameStorey.Count}");
        if (violsSameStorey.Count > 0)
        {
            Assert(violsSameStorey[0].RuleId == "CLR-001",
                "ClearanceChecker: Violation code is CLR-001", violsSameStorey[0].RuleId);
        }

        // Multi-storey door swing vs room tag
        var roomL2 = new RoomDef
        {
            Name = "Phong Ngu",
            Level = "Tang 2",
            TagLocation = new Point2D(900, 100) // Near door hinge at (800, 0)
        };
        var violsDoorSwingMulti = checker.CheckDoorSwingClearances(new[] { wallL1 }, Array.Empty<MepFixtureDef>(), new[] { roomL2 });
        Assert(violsDoorSwingMulti.Count == 0,
            "ClearanceChecker: Multi-storey door swing on Tang 1 vs room on Tang 2 produces 0 violations",
            $"Got {violsDoorSwingMulti.Count}");

        var roomL1 = roomL2 with { Level = "Tang 1" };
        var violsDoorSwingSame = checker.CheckDoorSwingClearances(new[] { wallL1 }, Array.Empty<MepFixtureDef>(), new[] { roomL1 });
        Assert(violsDoorSwingSame.Count == 1,
            "ClearanceChecker: Same-storey door swing touching room tag produces 1 violation (CLR-002)",
            $"Got {violsDoorSwingSame.Count}");

        // Zero-length wall in ClearanceChecker
        var zeroWall = new WallDef
        {
            Id = "W_ZERO",
            StartPoint = new Point2D(1000, 1000),
            EndPoint = new Point2D(1000, 1000),
            BaseLevel = "Tang 1",
            Openings = new[]
            {
                new OpeningDef { Id = "OP_Z", DistanceAlongWallMm = 0, WidthMm = 900, Kind = OpeningKind.Door }
            }
        };

        try
        {
            var violsColZero = checker.CheckOpeningToColumnClearances(new[] { zeroWall }, new[] { col });
            Assert(true, "ClearanceChecker: CheckOpeningToColumnClearances on zero-length wall does NOT throw", "");
            Assert(violsColZero.Count == 0, "ClearanceChecker: Zero-length wall produces 0 column clearance violations", "");
        }
        catch (Exception ex)
        {
            Assert(false, "ClearanceChecker: CheckOpeningToColumnClearances on zero-length wall does NOT throw", ex.ToString());
        }

        try
        {
            var violsDoorZero = checker.CheckDoorSwingClearances(new[] { zeroWall }, Array.Empty<MepFixtureDef>(), new[] { roomL1 });
            Assert(true, "ClearanceChecker: CheckDoorSwingClearances on zero-length wall does NOT throw", "");
            Assert(violsDoorZero.Count == 0, "ClearanceChecker: Zero-length wall produces 0 door swing violations", "");
        }
        catch (Exception ex)
        {
            Assert(false, "ClearanceChecker: CheckDoorSwingClearances on zero-length wall does NOT throw", ex.ToString());
        }

        // Full CheckAll with zero-length wall model
        var modelWithZeroWall = new BimModel
        {
            Columns = new[] { col },
            Walls = new[] { zeroWall, wallL1 },
            Rooms = new[] { roomL1 }
        };
        try
        {
            var allViols = checker.CheckAll(modelWithZeroWall);
            Assert(true, "ClearanceChecker: CheckAll does NOT throw on model with zero-length walls", "");
        }
        catch (Exception ex)
        {
            Assert(false, "ClearanceChecker: CheckAll does NOT throw on model with zero-length walls", ex.ToString());
        }
    }

    // -------------------------------------------------------------------------
    // 11. TCVN Detailing Regression Tests
    // -------------------------------------------------------------------------
    private static void RunTcvnRegressionTests()
    {
        Console.WriteLine("--- 11. TCVN Detailing & Math Regression Tests ---");

        // Verify standard bar unit weights: W = D^2 / 162.2
        int[] diameters = { 6, 8, 10, 12, 14, 16, 18, 20, 22, 25 };
        foreach (int d in diameters)
        {
            double w = TcvnRebarCalculator.CalculateUnitWeight(d);
            double expected = (d * d) / 162.2;
            Assert(Math.Abs(w - expected) < 1e-4, $"TCVN Rebar Weight: D={d} matches D^2/162.2 ({w:F4} kg/m)", "");
        }

        // Commercial splice logic
        var spliceShort = TcvnRebarCalculator.CalculateCommercialBarSplices(10000, 20, LapZone.Tension);
        Assert(spliceShort.SpliceCount == 0 && spliceShort.DevelopedLengthMm == 10000,
            "TCVN Splices: 10m bar has 0 splices", "");

        var splice15m = TcvnRebarCalculator.CalculateCommercialBarSplices(15000, 20, LapZone.Tension);
        Assert(splice15m.SpliceCount == 1 && splice15m.SingleLapLengthMm == 800 && splice15m.DevelopedLengthMm == 15800,
            "TCVN Splices: 15m D20 bar has 1 splice (lap=800mm, developed=15800mm)", "");
    }

    // -------------------------------------------------------------------------
    // 12. Adversarial Stress & Code Governance Checks
    // -------------------------------------------------------------------------
    public static void RunAdversarialStressAndCodeGovernanceTests()
    {
        Console.WriteLine("--- 12. Adversarial Stress & Code Governance Checks ---");

        // 12.1 Clockwise Polygon Contains Edge Points
        var cwPoly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(0, 6000),
            new Point2D(10000, 6000),
            new Point2D(10000, 0)
        });
        Assert(cwPoly.IsClockwise, "Clockwise Polygon: IsClockwise is TRUE", "");
        Assert(cwPoly.Contains(new Point2D(5000, 3000), includeBoundary: false),
            "Clockwise Polygon: Interior point with includeBoundary=false is TRUE", "");
        Assert(!cwPoly.Contains(new Point2D(5000, 0), includeBoundary: false),
            "Clockwise Polygon: Bottom edge midpoint with includeBoundary=false is FALSE", "");
        Assert(!cwPoly.Contains(new Point2D(10000, 3000), includeBoundary: false),
            "Clockwise Polygon: Right edge midpoint with includeBoundary=false is FALSE", "");
        Assert(!cwPoly.Contains(new Point2D(0, 0), includeBoundary: false),
            "Clockwise Polygon: Corner (0,0) with includeBoundary=false is FALSE", "");

        // 12.2 Reversed Beam Direction Perimeter Beam
        var slab = new SlabDef
        {
            Id = "S_REV",
            Level = "Tang 1",
            Boundary = new Polygon2D(new[]
            {
                new Point2D(0, 0),
                new Point2D(6000, 0),
                new Point2D(6000, 4000),
                new Point2D(0, 4000)
            })
        };
        var beamReversed = new BeamDef
        {
            Id = "B_REV",
            StartPoint = new Point2D(6000, 0),
            EndPoint = new Point2D(0, 0),
            Level = "Tang 1",
            WidthMm = 200,
            HeightMm = 400
        };
        var detector = new QcClashDetector();
        var clashesRev = detector.DetectBeamSlabClashes(new[] { beamReversed }, new[] { slab });
        Assert(clashesRev.Count == 0,
            "Reversed Perimeter Beam: Endpoints (6000,0) -> (0,0) does NOT clash with slab (0 clashes)",
            $"Got {clashesRev.Count}");

        // 12.3 Null vs Populated Level Combinations
        var colWithLevel = new ColumnDef { Id = "C_LVL", CenterLocation = new Point2D(1000, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "Tang 1" };
        var colNullLevel = new ColumnDef { Id = "C_NULL", CenterLocation = new Point2D(1000, 0), WidthMm = 220, DepthMm = 220, BaseLevel = null! };
        var wallWithLevel = new WallDef
        {
            Id = "W_LVL", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(5000, 0), BaseLevel = "Tang 1",
            Openings = new[] { new OpeningDef { Id = "OP1", DistanceAlongWallMm = 900, WidthMm = 900 } }
        };
        var wallNullLevel = new WallDef
        {
            Id = "W_NULL", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(5000, 0), BaseLevel = null!,
            Openings = new[] { new OpeningDef { Id = "OP2", DistanceAlongWallMm = 900, WidthMm = 900 } }
        };

        var clashNullCol = detector.DetectColumnOpeningClashes(new[] { colNullLevel }, new[] { wallWithLevel });
        Assert(clashNullCol.Count == 0,
            "Null Level Mismatch: Column with null level vs Wall with 'Tang 1' produces 0 clashes", "");

        var clashNullWall = detector.DetectColumnOpeningClashes(new[] { colWithLevel }, new[] { wallNullLevel });
        Assert(clashNullWall.Count == 0,
            "Null Level Mismatch: Column with 'Tang 1' vs Wall with null level produces 0 clashes", "");

        var clashBothNull = detector.DetectColumnOpeningClashes(new[] { colNullLevel }, new[] { wallNullLevel });
        Assert(clashBothNull.Count == 1,
            "Null Level Match: Both column and wall with null level match safely and detect 1 clash", "");

        // 12.4 File I/O Serialization & Deserialization
        string tempFile = Path.Combine(Path.GetTempPath(), $"hpbim_test_{Guid.NewGuid():N}.json");
        try
        {
            var testModel = new BimModel
            {
                ProjectName = "Disk_Serialization_Test",
                Columns = new[] { colWithLevel }
            };
            BimModelSerializer.SerializeToFile(testModel, tempFile, indented: true);
            Assert(File.Exists(tempFile), "BimModelSerializer: SerializeToFile writes file to disk", "");

            var loadedModel = BimModelSerializer.DeserializeFromFile(tempFile);
            Assert(loadedModel.Columns.Count == 1 && loadedModel.Columns[0].Id == "C_LVL",
                "BimModelSerializer: DeserializeFromFile loads model intact from disk", "");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }

        // 12.5 Clean Code Governance: Check Autodesk.Revit in HPBim.Core
        string coreSrcDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\HPBim\src\HPBim.Core"));
        if (!Directory.Exists(coreSrcDir))
        {
            coreSrcDir = Path.GetFullPath(@"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core");
        }

        var csFiles = Directory.GetFiles(coreSrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
            .ToList();

        bool hasRevitRef = false;
        bool hasOver300Lines = false;
        string over300FileName = "";

        foreach (var file in csFiles)
        {
            var lines = File.ReadAllLines(file);
            if (lines.Length > 300)
            {
                hasOver300Lines = true;
                over300FileName = $"{Path.GetFileName(file)} ({lines.Length} lines)";
            }
            foreach (var line in lines)
            {
                if (line.Contains("Autodesk.Revit"))
                {
                    hasRevitRef = true;
                }
            }
        }

        Assert(!hasRevitRef, "Clean Code: 0 references to Autodesk.Revit in HPBim.Core", "");
        Assert(!hasOver300Lines, "Clean Code: All classes in HPBim.Core are <= 300 lines", over300FileName);
    }
}

