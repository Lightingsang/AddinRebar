namespace HPBim.Core.Tests;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using Xunit;

public sealed class MultiLevelClashIsolationTests
{
    [Fact]
    public void MultiStoryIsolation_OnlyLevelWithClashIsFlagged()
    {
        // 5-story building: columns at (2000, 2000) on all 5 levels
        var levels = new[] { "Level 1", "Level 2", "Level 3", "Level 4", "Level 5" };
        var columns = new List<ColumnDef>();
        var walls = new List<WallDef>();

        for (int i = 0; i < levels.Length; i++)
        {
            string lvl = levels[i];
            columns.Add(new ColumnDef { Id = $"C_{lvl}", CenterLocation = new Point2D(2000, 2000), WidthMm = 220, DepthMm = 220, BaseLevel = lvl });

            var openings = new List<OpeningDef>();
            if (lvl == "Level 3")
            {
                // ONLY Level 3 has an opening directly penetrating the column
                openings.Add(new OpeningDef { Id = "D_Lvl3", DistanceAlongWallMm = 550, WidthMm = 900, Kind = OpeningKind.Door });
            }

            walls.Add(new WallDef { Id = $"W_{lvl}", StartPoint = new Point2D(1000, 2000), EndPoint = new Point2D(5000, 2000), BaseLevel = lvl, Openings = openings });
        }

        var model = new BimModel { Columns = columns, Walls = walls };
        var engine = new QcEngine();
        var report = engine.Validate(model, new QcOptions(CheckClashes: true, CheckClearances: false, CheckTcvnCompliance: false));

        // Exactly 1 clash must be detected, located on Level 3
        Assert.Single(report.Clashes);
        var clash = report.Clashes[0];
        Assert.Equal("C_Level 3", clash.ElementIdA);
        Assert.Equal("D_Lvl3", clash.ElementIdB);
        Assert.Equal("ColumnVsOpening", clash.ClashType);
    }

    [Theory]
    [InlineData("Tang 2", "tang 2", true)]     // Case-insensitive match -> clash
    [InlineData("LEVEL_1", "level_1", true)]   // Upper vs Lower -> clash
    [InlineData("Level 1", "Level 2", false)]  // Different levels -> NO clash
    [InlineData("Tang 1", "Tang 3", false)]    // Different levels -> NO clash
    public void ColumnOpeningClash_LevelNameMatching_IsStrictAndCaseInsensitive(
        string colLevel, string wallLevel, bool shouldClash)
    {
        var col = new ColumnDef { Id = "C1", CenterLocation = new Point2D(1000, 1000), WidthMm = 220, BaseLevel = colLevel };
        var wall = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 1000),
            EndPoint = new Point2D(3000, 1000),
            BaseLevel = wallLevel,
            Openings = new[] { new OpeningDef { Id = "OP1", DistanceAlongWallMm = 550, WidthMm = 900, Kind = OpeningKind.Door } }
        };

        var detector = new QcClashDetector();
        var clashes = detector.DetectColumnOpeningClashes(new[] { col }, new[] { wall });

        if (shouldClash)
        {
            Assert.Single(clashes);
        }
        else
        {
            Assert.Empty(clashes);
        }
    }

    [Fact]
    public void ColumnOpeningClash_PenetrationDepth_MatchesCalculatedGeometry()
    {
        // Col: center (1000, 0), width 220 -> halfW = 110
        // Wall: (0, 0) to (3000, 0), direction (1, 0)
        // Opening: width 900 -> halfW = 450.
        // We want opening center at (500, 0).
        // Distance along wall = 500 - 450 = 50.
        // Op center = (500, 0).
        // Distance to col center = 1000 - 500 = 500.
        // Clash limit = 450 + 110 = 560.
        // Penetration = 560 - 500 = 60.0mm.
        var col = new ColumnDef
        {
            Id = "C1",
            CenterLocation = new Point2D(1000, 0),
            WidthMm = 220,
            BaseLevel = "L1"
        };

        var wall = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(3000, 0),
            BaseLevel = "L1",
            Openings = new[]
            {
                new OpeningDef
                {
                    Id = "D1",
                    DistanceAlongWallMm = 50,
                    WidthMm = 900,
                    Kind = OpeningKind.Door
                }
            }
        };

        var detector = new QcClashDetector();
        var clashes = detector.DetectColumnOpeningClashes(new[] { col }, new[] { wall });

        Assert.Single(clashes);
        Assert.Equal(60.0, clashes[0].PenetrationMm, precision: 3);
    }

    [Fact]
    public void ColumnOpeningClash_ExactContactBoundary_DoesNotClash()
    {
        // Col: center (1000, 0), width 220 -> halfW = 110
        // Opening: width 900 -> halfW = 450. Clash limit = 560.
        // If dist == 560.0 -> dist < clashLimit is FALSE -> NO clash
        // If dist == 559.9 -> dist < clashLimit is TRUE -> CLASH!
        var col = new ColumnDef
        {
            Id = "C1",
            CenterLocation = new Point2D(1000, 0),
            WidthMm = 220,
            BaseLevel = "L1"
        };

        // Wall start (0,0). Opening distanceAlongWall:
        // OpCenter = distAlongWall + 450.
        // For dist = 560: OpCenter = 1000 - 560 = 440 -> distAlongWall = 440 - 450 = -10 (or start wall at 100)
        // Let's place wall start at (0, 0).
        // Exactly at contact limit: OpCenter = (440, 0) -> dist = 560.0
        var wallExactContact = new WallDef
        {
            Id = "W_Exact",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(3000, 0),
            BaseLevel = "L1",
            Openings = new[]
            {
                new OpeningDef
                {
                    Id = "D_Exact",
                    DistanceAlongWallMm = 440 - 450, // -10 is clamped or allowed? Wall starts at 0, opCenter = (0 + 1 * ( -10 + 450 )) = 440.
                    WidthMm = 900
                }
            }
        };

        var detector = new QcClashDetector();
        var clashesExact = detector.DetectColumnOpeningClashes(new[] { col }, new[] { wallExactContact });
        Assert.Empty(clashesExact);

        // Slightly penetrating: OpCenter = (440.1, 0) -> dist = 559.9 < 560.0
        var wallPenetrating = new WallDef
        {
            Id = "W_Pen",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(3000, 0),
            BaseLevel = "L1",
            Openings = new[]
            {
                new OpeningDef
                {
                    Id = "D_Pen",
                    DistanceAlongWallMm = 440.1 - 450,
                    WidthMm = 900
                }
            }
        };

        var clashesPen = detector.DetectColumnOpeningClashes(new[] { col }, new[] { wallPenetrating });
        Assert.Single(clashesPen);
    }

    [Fact]
    public void BeamSlabClash_MultiLevelIsolation_And_TieBeamExclusion()
    {
        var slabPoly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(5000, 0),
            new Point2D(5000, 5000),
            new Point2D(0, 5000)
        });

        var slabLvl2 = new SlabDef { Id = "S_L2", Boundary = slabPoly, Level = "Level 2" };

        // Beam 1: Secondary beam on Level 2 right through slab center (2500, 2500)
        var beamSameLevel = new BeamDef
        {
            Id = "B_Same",
            StartPoint = new Point2D(1000, 2500),
            EndPoint = new Point2D(4000, 2500),
            Level = "Level 2",
            IsTieBeam = false
        };

        // Beam 2: Secondary beam on Level 3 through slab center
        var beamDiffLevel = new BeamDef
        {
            Id = "B_Diff",
            StartPoint = new Point2D(1000, 2500),
            EndPoint = new Point2D(4000, 2500),
            Level = "Level 3",
            IsTieBeam = false
        };

        // Beam 3: Tie beam on Level 2 (tie beams are foundation/ground ties, exempt from slab clash)
        var tieBeam = new BeamDef
        {
            Id = "B_Tie",
            StartPoint = new Point2D(1000, 2500),
            EndPoint = new Point2D(4000, 2500),
            Level = "Level 2",
            IsTieBeam = true
        };

        var detector = new QcClashDetector();
        var clashes = detector.DetectBeamSlabClashes(
            new[] { beamSameLevel, beamDiffLevel, tieBeam },
            new[] { slabLvl2 });

        // Only B_Same should clash
        Assert.Single(clashes);
        Assert.Equal("B_Same", clashes[0].ElementIdA);
        Assert.Equal("S_L2", clashes[0].ElementIdB);
        Assert.Equal("SecondaryBeamSlicesUndividedSlab", clashes[0].ClashType);
    }

    [Fact]
    public void SpatialGridIndex_QueryCorrectness_WithDenseGrid()
    {
        var grid = new SpatialGridIndex<string>(cellSizeMm: 1000.0);
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                double px = x * 1000.0;
                double py = y * 1000.0;
                grid.Insert($"Item_{x}_{y}", new BoundingBox2D(px, py, px + 200, py + 200));
            }
        }

        Assert.Equal(100, grid.Count);

        // Query small box near (5000, 5000)
        var queryBox = new BoundingBox2D(4900, 4900, 5300, 5300);
        var results = new List<string>(grid.Query(queryBox));

        // Should return only items that intersect query box: (5,5) at (5000, 5000)
        Assert.Contains("Item_5_5", results);
        Assert.DoesNotContain("Item_0_0", results);
        Assert.DoesNotContain("Item_9_9", results);
    }
}
