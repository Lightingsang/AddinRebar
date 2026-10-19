namespace HPBim.Core.Tests;

using System;
using System.Text.Json;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Serialization;
using Xunit;

public sealed class JsonSerializationStressTests
{
    private static BimModel CreateComprehensiveTestModel()
    {
        return new BimModel
        {
            ProjectName = "Townhouse Benchmark 5x18m",
            Levels = new[]
            {
                new LevelDef { Name = "Level 1", ElevationM = 0.150, HeightMm = 3650, FloorNumber = 1 },
                new LevelDef { Name = "Level 2", ElevationM = 3.800, HeightMm = 3400, FloorNumber = 2 },
                new LevelDef { Name = "Roof", ElevationM = 13.400, HeightMm = 3000, FloorNumber = 5 }
            },
            Grids = new[]
            {
                new AxisGrid
                {
                    Name = "X1",
                    Direction = GridDirection.X,
                    CoordinateMm = 0.0,
                    StartPoint = new Point2D(0.0, -1500.0),
                    EndPoint = new Point2D(0.0, 19500.0)
                },
                new AxisGrid
                {
                    Name = "YA",
                    Direction = GridDirection.Y,
                    CoordinateMm = 0.0,
                    StartPoint = new Point2D(-1500.0, 0.0),
                    EndPoint = new Point2D(6500.0, 0.0)
                }
            },
            Columns = new[]
            {
                new ColumnDef
                {
                    Id = "C1",
                    CenterLocation = new Point2D(0, 0),
                    WidthMm = 220,
                    DepthMm = 220,
                    BaseLevel = "Level 1",
                    TopLevel = "Level 2"
                }
            },
            Walls = new[]
            {
                new WallDef
                {
                    Id = "W1",
                    StartPoint = new Point2D(0, 0),
                    EndPoint = new Point2D(5000, 0),
                    ThicknessMm = 220,
                    BaseLevel = "Level 1",
                    Openings = new[]
                    {
                        new OpeningDef
                        {
                            Id = "D1",
                            DistanceAlongWallMm = 1000,
                            WidthMm = 900,
                            HeightMm = 2200,
                            Kind = OpeningKind.Door,
                            Swing = OpeningSwing.Left
                        },
                        new OpeningDef
                        {
                            Id = "WND1",
                            DistanceAlongWallMm = 2500,
                            WidthMm = 1200,
                            HeightMm = 1500,
                            Kind = OpeningKind.Window,
                            SillHeightMm = 900
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
                    EndPoint = new Point2D(5000, 0),
                    WidthMm = 200,
                    HeightMm = 400,
                    Level = "Level 2",
                    IsTieBeam = false
                }
            },
            Slabs = new[]
            {
                new SlabDef
                {
                    Id = "S1",
                    Level = "Level 2",
                    ThicknessMm = 100,
                    Boundary = new Polygon2D(new[]
                    {
                        new Point2D(0, 0),
                        new Point2D(5000, 0),
                        new Point2D(5000, 5000),
                        new Point2D(0, 5000)
                    }),
                    Voids = new[]
                    {
                        new Polygon2D(new[]
                        {
                            new Point2D(1000, 1000),
                            new Point2D(2000, 1000),
                            new Point2D(2000, 2000),
                            new Point2D(1000, 2000)
                        })
                    }
                }
            },
            Rooms = new[]
            {
                new RoomDef
                {
                    Name = "Phòng Khách",
                    Level = "Level 1",
                    TagLocation = new Point2D(2500, 2500),
                    TheoreticalAreaM2 = 25.0
                }
            },
            Fixtures = new[]
            {
                new MepFixtureDef
                {
                    Id = "WC1",
                    Category = MepCategory.Sanitary,
                    FamilyName = "Water Closet",
                    Location = new Point3D(1500, 1500, 0),
                    Level = "Level 1"
                }
            },
            Pipes = new[]
            {
                new PipeRunDef
                {
                    Id = "P1",
                    SystemType = PipeSystemType.SoilDrain,
                    DiameterMm = 110,
                    Level = "Level 1",
                    Points = new[] { new Point3D(0, 0, 0), new Point3D(0, 0, -3000) }
                }
            },
            Rebars = new[]
            {
                new RebarDef
                {
                    Id = "RB1",
                    BarMark = 1,
                    DiameterMm = 16.0,
                    LengthMm = 5500.0,
                    Quantity = 4,
                    SteelGrade = "CB400-V",
                    CoverMm = 25.0,
                    ShapeType = RebarShapeType.HookBoth
                }
            }
        };
    }

    [Fact]
    public void BimModelSerializer_CompleteModel_RoundTripFidelity()
    {
        var original = CreateComprehensiveTestModel();

        string json = BimModelSerializer.Serialize(original, indented: true);
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        var restored = BimModelSerializer.Deserialize(json);
        Assert.NotNull(restored);

        // Verify top-level properties
        Assert.Equal(original.ProjectName, restored.ProjectName);
        Assert.Equal(original.Levels.Count, restored.Levels.Count);
        Assert.Equal(original.Grids.Count, restored.Grids.Count);
        Assert.Equal(original.Columns.Count, restored.Columns.Count);
        Assert.Equal(original.Walls.Count, restored.Walls.Count);
        Assert.Equal(original.Beams.Count, restored.Beams.Count);
        Assert.Equal(original.Slabs.Count, restored.Slabs.Count);
        Assert.Equal(original.Rooms.Count, restored.Rooms.Count);
        Assert.Equal(original.Fixtures.Count, restored.Fixtures.Count);
        Assert.Equal(original.Pipes.Count, restored.Pipes.Count);
        Assert.Equal(original.Rebars.Count, restored.Rebars.Count);

        // Verify Slab with Voids
        var slab = restored.Slabs[0];
        Assert.Equal(25.0, slab.GrossAreaM2, precision: 3); // 5m x 5m = 25m2
        Assert.Single(slab.Voids);
        Assert.Equal(24.0, slab.NetAreaM2, precision: 3);   // 25 - 1 = 24m2

        // Verify Wall Openings
        var wall = restored.Walls[0];
        Assert.Equal(2, wall.Openings.Count);
        Assert.Equal(OpeningKind.Door, wall.Openings[0].Kind);
        Assert.Equal(OpeningKind.Window, wall.Openings[1].Kind);

        // Verify Rebar Weight Calculation
        Assert.Equal(original.TotalRebarWeightKg, restored.TotalRebarWeightKg, precision: 3);

        // Re-serialize and verify exact string idempotence
        string reJson = BimModelSerializer.Serialize(restored, indented: true);
        Assert.Equal(json, reJson);
    }

    [Fact]
    public void BimModelSerializer_EnumCamelCaseSerialization()
    {
        var model = new BimModel
        {
            Walls = new[]
            {
                new WallDef
                {
                    Openings = new[]
                    {
                        new OpeningDef { Kind = OpeningKind.Door, Swing = OpeningSwing.Left },
                        new OpeningDef { Kind = OpeningKind.Door2, Swing = OpeningSwing.Both },
                        new OpeningDef { Kind = OpeningKind.Window, Swing = OpeningSwing.None }
                    }
                }
            },
            Pipes = new[]
            {
                new PipeRunDef { SystemType = PipeSystemType.WaterSupply },
                new PipeRunDef { SystemType = PipeSystemType.SoilDrain },
                new PipeRunDef { SystemType = PipeSystemType.WasteDrain }
            }
        };

        string json = BimModelSerializer.Serialize(model);

        // Assert that enum strings are camelCase in JSON
        Assert.Contains("\"kind\": \"door\"", json);
        Assert.Contains("\"kind\": \"door2\"", json);
        Assert.Contains("\"kind\": \"window\"", json);
        Assert.Contains("\"swing\": \"left\"", json);
        Assert.Contains("\"swing\": \"both\"", json);
        Assert.Contains("\"systemType\": \"waterSupply\"", json);
        Assert.Contains("\"systemType\": \"soilDrain\"", json);
        Assert.Contains("\"systemType\": \"wasteDrain\"", json);
    }

    [Fact]
    public void BimModelSerializer_EmptyAndCompactOptions()
    {
        var model = new BimModel { ProjectName = "Compact" };

        string compact = BimModelSerializer.Serialize(model, indented: false);
        Assert.DoesNotContain("\n", compact);

        var restored = BimModelSerializer.Deserialize(compact);
        Assert.Equal("Compact", restored.ProjectName);
    }

    [Fact]
    public void BimModelSerializer_MalformedInputs_ThrowExpectedExceptions()
    {
        Assert.Throws<ArgumentNullException>(() => BimModelSerializer.Serialize(null!));
        Assert.Throws<ArgumentException>(() => BimModelSerializer.Deserialize(""));
        Assert.Throws<ArgumentException>(() => BimModelSerializer.Deserialize("   "));
        Assert.Throws<ArgumentException>(() => BimModelSerializer.Deserialize(null!));
        Assert.Throws<JsonException>(() => BimModelSerializer.Deserialize("{ invalid json:"));
    }

    [Fact]
    public void BimModelSerializer_EmptyJsonObject_ProducesValidModel()
    {
        var model = BimModelSerializer.Deserialize("{}");
        Assert.NotNull(model);
        Assert.Empty(model.Levels);
        Assert.Empty(model.Walls);
        Assert.Empty(model.Columns);
        Assert.Empty(model.Rebars);
    }
}
