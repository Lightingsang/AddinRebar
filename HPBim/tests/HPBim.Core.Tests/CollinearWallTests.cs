namespace HPBim.Core.Tests;

using System;
using System.Linq;
using HPBim.Core.Algorithms;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using Xunit;

public sealed class CollinearWallTests
{
    [Fact]
    public void CollinearWalls_HorizontalRun_SegmentsBothWallsSeparately()
    {
        // Wall 1: (0, 0) to (5000, 0), Wall 2: (5000, 0) to (10000, 0)
        var wall1 = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            ThicknessMm = 220.0,
            Openings = new[]
            {
                new OpeningDef { Id = "D1", DistanceAlongWallMm = 1000, WidthMm = 900 }
            }
        };

        var wall2 = new WallDef
        {
            Id = "W2",
            StartPoint = new Point2D(5000, 0),
            EndPoint = new Point2D(10000, 0),
            ThicknessMm = 220.0,
            Openings = new[]
            {
                new OpeningDef { Id = "WND1", DistanceAlongWallMm = 2000, WidthMm = 1200 }
            }
        };

        var segmenter = new WallSegmenter();
        var segs1 = segmenter.Segment(wall1);
        var segs2 = segmenter.Segment(wall2);

        // Wall 1: 0..1000 and 1900..5000 -> 2 segments
        Assert.Equal(2, segs1.Count);
        Assert.Equal(1000.0, segs1[0].LengthMm, precision: 2);
        Assert.Equal(3100.0, segs1[1].LengthMm, precision: 2);

        // Wall 2: 0..2000 and 3200..5000 -> 2 segments
        Assert.Equal(2, segs2.Count);
        Assert.Equal(2000.0, segs2[0].LengthMm, precision: 2);
        Assert.Equal(1800.0, segs2[1].LengthMm, precision: 2);

        // Verify segment vertices follow wall line
        Assert.Equal(0.0, segs1[0].StartPoint.X, precision: 2);
        Assert.Equal(1000.0, segs1[0].EndPoint.X, precision: 2);
        Assert.Equal(5000.0, segs2[0].StartPoint.X, precision: 2);
        Assert.Equal(7000.0, segs2[0].EndPoint.X, precision: 2);
    }

    [Fact]
    public void CollinearWalls_VerticalRun_SegmentsCorrectly()
    {
        // Wall 1: (2000, 0) to (2000, 4000), Wall 2: (2000, 4000) to (2000, 8000)
        var wall1 = new WallDef
        {
            Id = "WV1",
            StartPoint = new Point2D(2000, 0),
            EndPoint = new Point2D(2000, 4000),
            ThicknessMm = 110.0
        };

        var segmenter = new WallSegmenter();
        var segs = segmenter.Segment(wall1);

        Assert.Single(segs);
        Assert.Equal(4000.0, segs[0].LengthMm, precision: 2);
        Assert.Equal(2000.0, segs[0].StartPoint.X, precision: 2);
        Assert.Equal(4000.0, segs[0].EndPoint.Y, precision: 2);
    }

    [Fact]
    public void WallSegmenter_OpeningBoundaryConditions_Exhaustive()
    {
        var segmenter = new WallSegmenter();

        // 1. Opening at offset 0
        var wallStartOp = new WallDef
        {
            Id = "W_Start",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(4000, 0),
            Openings = new[] { new OpeningDef { DistanceAlongWallMm = 0, WidthMm = 1000 } }
        };
        var segsStart = segmenter.Segment(wallStartOp);
        Assert.Single(segsStart);
        Assert.Equal(1000.0, segsStart[0].StartOffsetMm, precision: 2);
        Assert.Equal(4000.0, segsStart[0].EndOffsetMm, precision: 2);

        // 2. Opening at very end
        var wallEndOp = new WallDef
        {
            Id = "W_End",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(4000, 0),
            Openings = new[] { new OpeningDef { DistanceAlongWallMm = 3000, WidthMm = 1000 } }
        };
        var segsEnd = segmenter.Segment(wallEndOp);
        Assert.Single(segsEnd);
        Assert.Equal(0.0, segsEnd[0].StartOffsetMm, precision: 2);
        Assert.Equal(3000.0, segsEnd[0].EndOffsetMm, precision: 2);

        // 3. Opening covers entire wall (0 to 4000)
        var wallFullOp = new WallDef
        {
            Id = "W_Full",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(4000, 0),
            Openings = new[] { new OpeningDef { DistanceAlongWallMm = 0, WidthMm = 4000 } }
        };
        var segsFull = segmenter.Segment(wallFullOp);
        Assert.Empty(segsFull);

        // 4. Overlapping openings: (1000..2500) and (2000..3500) -> merges to (1000..3500)
        var wallOverlap = new WallDef
        {
            Id = "W_Overlap",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            Openings = new[]
            {
                new OpeningDef { DistanceAlongWallMm = 1000, WidthMm = 1500 },
                new OpeningDef { DistanceAlongWallMm = 2000, WidthMm = 1500 }
            }
        };
        var segsOverlap = segmenter.Segment(wallOverlap);
        Assert.Equal(2, segsOverlap.Count);
        Assert.Equal(1000.0, segsOverlap[0].LengthMm, precision: 2); // 0..1000
        Assert.Equal(1500.0, segsOverlap[1].LengthMm, precision: 2); // 3500..5000

        // 5. Out of order openings: Op2 at 3000, Op1 at 1000
        var wallOutOfOrder = new WallDef
        {
            Id = "W_Order",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            Openings = new[]
            {
                new OpeningDef { DistanceAlongWallMm = 3000, WidthMm = 1000 },
                new OpeningDef { DistanceAlongWallMm = 1000, WidthMm = 1000 }
            }
        };
        var segsOrder = segmenter.Segment(wallOutOfOrder);
        Assert.Equal(3, segsOrder.Count);
        Assert.Equal(1000.0, segsOrder[0].LengthMm, precision: 2); // 0..1000
        Assert.Equal(1000.0, segsOrder[1].LengthMm, precision: 2); // 2000..3000
        Assert.Equal(1000.0, segsOrder[2].LengthMm, precision: 2); // 4000..5000
    }

    [Fact]
    public void WallSegmenter_ZeroLengthWall_ThrowsArgumentException()
    {
        var segmenter = new WallSegmenter();
        var zeroWall = new WallDef
        {
            Id = "W_Zero",
            StartPoint = new Point2D(100, 100),
            EndPoint = new Point2D(100, 100)
        };

        Assert.Throws<ArgumentException>(() => segmenter.Segment(zeroWall));
    }

    [Fact]
    public void ClearanceChecker_OpeningNearJunctionColumn_FlagsWarning()
    {
        // Collinear walls W1 (0,0)->(4000,0) and W2 (4000,0)->(8000,0)
        // Column at junction (4000, 0), width 220 -> edges at 3890 and 4110
        var col = new ColumnDef
        {
            Id = "C_Junction",
            CenterLocation = new Point2D(4000, 0),
            WidthMm = 220,
            BaseLevel = "L1"
        };

        // Door on W1 ending at 3850 (offset 2950, width 900)
        // End is at 3850. Col edge is at 3890. Distance = 40mm (< 100mm standard)
        var wall1 = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(4000, 0),
            BaseLevel = "L1",
            Openings = new[]
            {
                new OpeningDef { Id = "D_NearCol", DistanceAlongWallMm = 2950, WidthMm = 900 }
            }
        };

        var checker = new ClearanceChecker();
        var violations = checker.CheckOpeningToColumnClearances(new[] { wall1 }, new[] { col });

        Assert.Single(violations);
        Assert.Equal("CLR-001", violations[0].RuleId);
        Assert.Equal(QcSeverity.Warning, violations[0].Severity);
    }

    [Theory]
    [InlineData(110.0, true)]
    [InlineData(220.0, true)]
    [InlineData(108.0, true)] // Within 5mm of 110mm
    [InlineData(223.0, true)] // Within 5mm of 220mm
    [InlineData(150.0, false)] // Non-standard -> Warning
    [InlineData(300.0, false)] // Non-standard -> Warning
    [InlineData(80.0, false)]  // Non-standard -> Warning
    public void ClearanceChecker_WallThicknessCompliance_ChecksStandards(
        double thicknessMm, bool isCompliant)
    {
        var wall = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(4000, 0),
            ThicknessMm = thicknessMm
        };

        var checker = new ClearanceChecker();
        var violations = checker.CheckWallThicknesses(new[] { wall });

        if (isCompliant)
        {
            Assert.Empty(violations);
        }
        else
        {
            Assert.Single(violations);
            Assert.Equal("TCVN-WALL-001", violations[0].RuleId);
        }
    }
}
