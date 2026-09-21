using System.Diagnostics;
using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.Core.SmartPlot.Services;
using Xunit;

namespace HPAutoCad.Tests.SmartPlot;

/// <summary>
/// Adversarial stress test suite for PlotBounds and PlotOrderService.
/// Challenges boundary conditions, floating-point stability, degenerate inputs,
/// precision thresholds (49.999% vs 50.001%), and extreme topologies.
/// </summary>
public sealed class PlotOrderAdversarialStressTests
{
    private readonly PlotOrderService _service = new();

    private static PlotItem CreateFrame(string id, double minX, double minY, double width = 841.0, double height = 594.0)
    {
        return new PlotItem
        {
            Id = id,
            DisplayName = $"Frame_{id}",
            Bounds = new PlotBounds(minX, minY, minX + width, minY + height),
            SourceHandle = $"H_{id}"
        };
    }

    #region 1. Extreme Topology Stress (100+ frames, randomized, jittered)

    [Fact]
    public void Stress_150Frames_10RowsBy15Cols_WithVerticalJitter_SortedFlawlessly()
    {
        // 10 rows x 15 columns = 150 frames
        // Frame size: 841 x 594 mm
        // Spacing: X step = 1000 mm, Y step = 1000 mm (leaving 406mm gap between nominal row bounds)
        // Add random jitter of [-80, +80] mm to Y.
        // Within-row overlap will be at least 594 - 160 = 434mm (434/594 = 73% > 50% threshold).
        // Across-row overlap: nominal gap is 406mm, max jitter closes it by at most 160mm -> gap >= 246mm > 0.
        // Therefore, rows are provably disjoint and each frame belongs to its unique row.

        const int numRows = 10;
        const int numCols = 15;
        var rng = new Random(42); // Deterministic seed for reproducible testing

        var items = new List<PlotItem>(numRows * numCols);

        for (var r = 0; r < numRows; r++)
        {
            // r = 0 is TOP row (highest Y), r = 9 is BOTTOM row (lowest Y)
            var nominalY = (numRows - 1 - r) * 1000.0;

            for (var c = 0; c < numCols; c++)
            {
                var nominalX = c * 1000.0;
                var jitterY = (rng.NextDouble() * 160.0) - 80.0;
                var id = $"R{r:D2}_C{c:D2}";
                items.Add(CreateFrame(id, nominalX, nominalY + jitterY));
            }
        }

        // Shuffle items into random order
        var shuffled = items.OrderBy(_ => rng.Next()).ToList();

        var sorted = _service.OrderFrames(shuffled, overlapRatioThreshold: 0.5);

        Assert.Equal(numRows * numCols, sorted.Count);

        // Verify sequential 1..150 ordering and exact row/col structure
        for (var i = 0; i < sorted.Count; i++)
        {
            Assert.Equal(i + 1, sorted[i].Order);

            var expectedRow = i / numCols;
            var expectedCol = i % numCols;
            var expectedId = $"R{expectedRow:D2}_C{expectedCol:D2}";

            Assert.Equal(expectedId, sorted[i].Id);
        }
    }

    [Fact]
    public void Stress_1000Frames_ScalePerformance_CompletesUnder100ms()
    {
        // 20 rows x 50 columns = 1000 frames
        const int numRows = 20;
        const int numCols = 50;
        var rng = new Random(12345);

        var items = new List<PlotItem>(numRows * numCols);
        for (var r = 0; r < numRows; r++)
        {
            var y = (numRows - 1 - r) * 1000.0;
            for (var c = 0; c < numCols; c++)
            {
                items.Add(CreateFrame($"F_{r}_{c}", c * 1000.0, y));
            }
        }

        var shuffled = items.OrderBy(_ => rng.Next()).ToList();

        var sw = Stopwatch.StartNew();
        var sorted = _service.OrderFrames(shuffled);
        sw.Stop();

        Assert.Equal(1000, sorted.Count);
        Assert.True(sw.ElapsedMilliseconds < 250, $"Elapsed {sw.ElapsedMilliseconds}ms exceeded target budget");

        // Verify strictly increasing Order from 1 to 1000
        for (var i = 0; i < sorted.Count; i++)
        {
            Assert.Equal(i + 1, sorted[i].Order);
        }
    }

    #endregion

    #region 2. Degenerate and Boundary Geometries

    [Fact]
    public void Degenerate_NegativeDimensions_FilteredOutAsInvalid()
    {
        var invertedX = new PlotItem
        {
            Id = "invX",
            Bounds = new PlotBounds(500.0, 100.0, 200.0, 600.0) // MaxX < MinX
        };
        var invertedY = new PlotItem
        {
            Id = "invY",
            Bounds = new PlotBounds(100.0, 600.0, 500.0, 200.0) // MaxY < MinY
        };
        var valid = CreateFrame("valid", 0, 0);

        var result = _service.OrderFrames(new[] { invertedX, valid, invertedY });

        Assert.Single(result);
        Assert.Equal("valid", result[0].Id);
        Assert.Equal(1, result[0].Order);
    }

    [Fact]
    public void Degenerate_InfiniteAndNaNCoordinates_FilteredOutAsInvalid()
    {
        var nanX = new PlotItem { Id = "nanX", Bounds = new PlotBounds(double.NaN, 0, 100, 100) };
        var nanY = new PlotItem { Id = "nanY", Bounds = new PlotBounds(0, double.NaN, 100, 100) };
        var infMaxX = new PlotItem { Id = "infMaxX", Bounds = new PlotBounds(0, 0, double.PositiveInfinity, 100) };
        var infMinY = new PlotItem { Id = "infMinY", Bounds = new PlotBounds(0, double.NegativeInfinity, 100, 100) };
        var valid = CreateFrame("valid", 100, 100);

        var result = _service.OrderFrames(new[] { nanX, valid, nanY, infMaxX, infMinY });

        Assert.Single(result);
        Assert.Equal("valid", result[0].Id);
        Assert.Equal(1, result[0].Order);
    }

    [Fact]
    public void Degenerate_ZeroWidthOrZeroHeight_DoesNotCrash()
    {
        // Zero-width (vertical line segment) and zero-height (horizontal line segment)
        var zeroWidth = new PlotItem
        {
            Id = "zeroW",
            Bounds = new PlotBounds(500.0, 0.0, 500.0, 594.0) // Width = 0, Height = 594
        };
        var zeroHeight = new PlotItem
        {
            Id = "zeroH",
            Bounds = new PlotBounds(0.0, 300.0, 841.0, 300.0) // Width = 841, Height = 0
        };
        var singlePoint = new PlotItem
        {
            Id = "point",
            Bounds = new PlotBounds(200.0, 200.0, 200.0, 200.0) // Width = 0, Height = 0
        };
        var normalFrame = CreateFrame("normal", 0, 0);

        // Verify IsValid behaviour
        Assert.True(zeroWidth.Bounds.IsValid);
        Assert.True(zeroHeight.Bounds.IsValid);
        Assert.True(singlePoint.Bounds.IsValid);

        // Sorting must execute smoothly without division-by-zero, NaN, or unhandled exceptions
        var result = _service.OrderFrames(new[] { zeroWidth, normalFrame, zeroHeight, singlePoint });

        Assert.Equal(4, result.Count);
        for (var i = 0; i < result.Count; i++)
        {
            Assert.Equal(i + 1, result[i].Order);
        }
    }

    #endregion

    #region 3. Precision Boundary (49.999% vs 50.001% Vertical Overlap)

    [Fact]
    public void Precision_VerticalOverlap_At49Point999Percent_SplitsIntoSeparateRows()
    {
        // Two frames with height H = 1000mm.
        // Frame A is on the right: X = [1000, 1841], Y = [0, 1000] (CenterY = 500, MaxY = 1000)
        // Frame B is on the left:  X = [0, 841],     Y = [-500.01, 499.99]
        // Overlap region: Y in [0, 499.99] -> overlap = 499.99mm.
        // Overlap ratio = 499.99 / 1000.0 = 0.49999 (49.999%).
        // With threshold = 0.5 (50%), 0.49999 < 0.5:
        // -> Frame B does NOT match Frame A's row.
        // -> Two separate rows: Row 1 (Top, Frame A) and Row 2 (Bottom, Frame B).
        // Since Row 1 is higher (CenterY = 500 vs -0.01), Frame A must be Order 1, Frame B must be Order 2.

        var frameA = CreateFrame("RightTop_A", 1000.0, 0.0, width: 841.0, height: 1000.0);
        var frameB = CreateFrame("LeftBottom_B", 0.0, -500.01, width: 841.0, height: 1000.0);

        var boundsA = frameA.Bounds;
        var boundsB = frameB.Bounds;

        Assert.Equal(499.99, boundsA.VerticalOverlap(boundsB), precision: 2);
        Assert.False(boundsA.OverlapsVertically(boundsB, ratioThreshold: 0.5));

        var sorted = _service.OrderFrames(new[] { frameB, frameA }, overlapRatioThreshold: 0.5);

        Assert.Equal(2, sorted.Count);
        Assert.Equal("RightTop_A", sorted[0].Id);
        Assert.Equal(1, sorted[0].Order);
        Assert.Equal("LeftBottom_B", sorted[1].Id);
        Assert.Equal(2, sorted[1].Order);
    }

    [Fact]
    public void Precision_VerticalOverlap_At50Point001Percent_MergesIntoSameRow_SortingLeftToRight()
    {
        // Exactly analogous to previous test, but shift Frame B upward by 0.02mm:
        // Frame A is on the right: X = [1000, 1841], Y = [0, 1000]
        // Frame B is on the left:  X = [0, 841],     Y = [-499.99, 500.01]
        // Overlap region: Y in [0, 500.01] -> overlap = 500.01mm.
        // Overlap ratio = 500.01 / 1000.0 = 0.50001 (50.001%).
        // With threshold = 0.5 (50%), 0.50001 >= 0.5:
        // -> Frame B MATCHES Frame A's row.
        // -> Both frames are clustered in the SAME ROW.
        // Within this row, sorting is Left-to-Right by MinX:
        // Frame B (MinX = 0) is to the left of Frame A (MinX = 1000).
        // Therefore: Frame B MUST BE Order 1, Frame A MUST BE Order 2!
        // This is the definitive discriminator: the order inverted because the frames merged into one row!

        var frameA = CreateFrame("RightTop_A", 1000.0, 0.0, width: 841.0, height: 1000.0);
        var frameB = CreateFrame("LeftBottom_B", 0.0, -499.99, width: 841.0, height: 1000.0);

        var boundsA = frameA.Bounds;
        var boundsB = frameB.Bounds;

        Assert.Equal(500.01, boundsA.VerticalOverlap(boundsB), precision: 2);
        Assert.True(boundsA.OverlapsVertically(boundsB, ratioThreshold: 0.5));

        var sorted = _service.OrderFrames(new[] { frameB, frameA }, overlapRatioThreshold: 0.5);

        Assert.Equal(2, sorted.Count);
        Assert.Equal("LeftBottom_B", sorted[0].Id);
        Assert.Equal(1, sorted[0].Order);
        Assert.Equal("RightTop_A", sorted[1].Id);
        Assert.Equal(2, sorted[1].Order);
    }

    [Fact]
    public void Precision_VerticalOverlap_Exactly50Percent_MergesIntoSameRow()
    {
        // Exactly at 50.0% overlap
        var frameA = CreateFrame("RightTop_A", 1000.0, 0.0, width: 841.0, height: 1000.0);
        var frameB = CreateFrame("LeftBottom_B", 0.0, -500.0, width: 841.0, height: 1000.0);

        Assert.Equal(500.0, frameA.Bounds.VerticalOverlap(frameB.Bounds));
        Assert.True(frameA.Bounds.OverlapsVertically(frameB.Bounds, ratioThreshold: 0.5));

        var sorted = _service.OrderFrames(new[] { frameB, frameA }, overlapRatioThreshold: 0.5);

        Assert.Equal("LeftBottom_B", sorted[0].Id);
        Assert.Equal("RightTop_A", sorted[1].Id);
    }

    [Theory]
    [InlineData(0.01)] // Below lower clamp 0.05
    [InlineData(0.99)] // Above upper clamp 0.95
    public void Precision_ThresholdClamping_HandlesExtremeThresholdValuesSafely(double extremeThreshold)
    {
        var f1 = CreateFrame("1", 0, 100);
        var f2 = CreateFrame("2", 1000, 100);

        var sorted = _service.OrderFrames(new[] { f2, f1 }, overlapRatioThreshold: extremeThreshold);

        Assert.Equal(2, sorted.Count);
        Assert.Equal("1", sorted[0].Id);
        Assert.Equal("2", sorted[1].Id);
    }

    #endregion

    #region 4. Massive Coordinates & Floating-Point Stability

    [Fact]
    public void MassiveCoordinates_CivilDrawing_MillionsUnitsFromOrigin_OrdersIdentically()
    {
        // CAD civil drawings in mm (UTM / VN-2000):
        // Northing Y ~ 2,000,000,000 mm (2,000 km)
        // Easting  X ~   500,000,000 mm (  500 km)
        const double offsetX = 500_000_000.0;
        const double offsetY = 2_000_000_000.0;

        // 3x3 grid placed at massive offset
        var items = new List<PlotItem>
        {
            CreateFrame("R3C2", offsetX + 1000, offsetY + 0),
            CreateFrame("R1C1", offsetX + 0,    offsetY + 2000),
            CreateFrame("R2C3", offsetX + 2000, offsetY + 1000),
            CreateFrame("R1C3", offsetX + 2000, offsetY + 2000),
            CreateFrame("R3C1", offsetX + 0,    offsetY + 0),
            CreateFrame("R2C1", offsetX + 0,    offsetY + 1000),
            CreateFrame("R1C2", offsetX + 1000, offsetY + 2000),
            CreateFrame("R3C3", offsetX + 2000, offsetY + 0),
            CreateFrame("R2C2", offsetX + 1000, offsetY + 1000),
        };

        var sorted = _service.OrderFrames(items);

        Assert.Equal(9, sorted.Count);

        var expectedIds = new[]
        {
            "R1C1", "R1C2", "R1C3",
            "R2C1", "R2C2", "R2C3",
            "R3C1", "R3C2", "R3C3"
        };

        for (var i = 0; i < 9; i++)
        {
            Assert.Equal(expectedIds[i], sorted[i].Id);
            Assert.Equal(i + 1, sorted[i].Order);
        }
    }

    [Fact]
    public void MassiveCoordinates_TrillionUnits_FloatingPointAccuracyPreserved()
    {
        // 10^12 units (1,000,000,000,000)
        const double offset = 1_000_000_000_000.0;

        var f1 = CreateFrame("TopLeft",  offset + 0.0,    offset + 2000.0);
        var f2 = CreateFrame("TopRight", offset + 1000.0, offset + 2000.0);
        var f3 = CreateFrame("BotLeft",  offset + 0.0,    offset + 0.0);
        var f4 = CreateFrame("BotRight", offset + 1000.0, offset + 0.0);

        var sorted = _service.OrderFrames(new[] { f4, f1, f3, f2 });

        Assert.Equal("TopLeft", sorted[0].Id);
        Assert.Equal("TopRight", sorted[1].Id);
        Assert.Equal("BotLeft", sorted[2].Id);
        Assert.Equal("BotRight", sorted[3].Id);
    }

    [Fact]
    public void MassiveCoordinates_NegativeQuadrants_OrderedCorrectly()
    {
        // Negative coordinates: Y = -1000 (Top) vs Y = -2000 (Bottom)
        // In Cartesian coords, Y = -1000 is higher (greater) than Y = -2000
        const double offsetX = -500_000_000.0;
        const double offsetY = -2_000_000_000.0;

        var top = CreateFrame("Top", offsetX, offsetY + 1000.0);
        var bottom = CreateFrame("Bottom", offsetX, offsetY);

        var sorted = _service.OrderFrames(new[] { bottom, top });

        Assert.Equal("Top", sorted[0].Id);
        Assert.Equal(1, sorted[0].Order);
        Assert.Equal("Bottom", sorted[1].Id);
        Assert.Equal(2, sorted[1].Order);
    }

    #endregion

    #region 5. Coincident, Nested, and Pathological Cases

    [Fact]
    public void Pathological_IdenticalCoincidentFrames_DeterministicallyOrdered()
    {
        // 4 frames sharing the exact same coordinate bounding box
        var f1 = CreateFrame("F1", 100, 100);
        var f2 = CreateFrame("F2", 100, 100);
        var f3 = CreateFrame("F3", 100, 100);
        var f4 = CreateFrame("F4", 100, 100);

        var sorted = _service.OrderFrames(new[] { f3, f1, f4, f2 });

        Assert.Equal(4, sorted.Count);
        // All orders must be 1, 2, 3, 4 with no duplicates or missing ranks
        var orders = sorted.Select(s => s.Order).ToList();
        Assert.Equal(new[] { 1, 2, 3, 4 }, orders);
    }

    [Fact]
    public void Pathological_NestedFrames_SmallDetailInsideLargeSheet_GroupsCohesively()
    {
        // Large sheet: X = [0, 2000], Y = [0, 2000] (CenterY = 1000, Height = 2000)
        // Small detail: X = [500, 1000], Y = [500, 1000] (CenterY = 750, Height = 500)
        // Overlap is full 500mm of small frame -> overlap / min(2000, 500) = 500/500 = 100%
        var large = CreateFrame("Large", 0, 0, width: 2000, height: 2000);
        var small = CreateFrame("Small", 500, 500, width: 500, height: 500);

        var sorted = _service.OrderFrames(new[] { large, small });

        Assert.Equal(2, sorted.Count);
        // Both in same row, large has MinX = 0, small has MinX = 500 -> large is 1, small is 2
        Assert.Equal("Large", sorted[0].Id);
        Assert.Equal("Small", sorted[1].Id);
    }

    [Fact]
    public void Pathological_SingleHorizontalRow_500Frames_OrderedLeftToRight()
    {
        const int count = 500;
        var items = Enumerable.Range(0, count)
            .Select(i => CreateFrame($"F_{i}", i * 1000.0, 500.0))
            .Reverse() // Reversed input
            .ToList();

        var sorted = _service.OrderFrames(items);

        Assert.Equal(count, sorted.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal($"F_{i}", sorted[i].Id);
            Assert.Equal(i + 1, sorted[i].Order);
        }
    }

    [Fact]
    public void Pathological_SingleVerticalColumn_500Frames_OrderedTopToBottom()
    {
        const int count = 500;
        var items = Enumerable.Range(0, count)
            .Select(i => CreateFrame($"F_{i}", 0.0, i * 1000.0)) // i = 499 is Top, i = 0 is Bottom
            .ToList(); // Input has Bottom first

        var sorted = _service.OrderFrames(items);

        Assert.Equal(count, sorted.Count);
        for (var i = 0; i < count; i++)
        {
            var expectedIndex = count - 1 - i;
            Assert.Equal($"F_{expectedIndex}", sorted[i].Id);
            Assert.Equal(i + 1, sorted[i].Order);
        }
    }

    [Fact]
    public void Adversarial_CollectionWithNullElement_ThrowsOrHandles()
    {
        // An adversarial caller might supply a collection containing a null PlotItem element:
        // new PlotItem[] { validItem, null!, validItem2 }
        // We verify empirically whether this throws NullReferenceException or behaves safely.
        var valid1 = CreateFrame("V1", 0, 1000);
        var valid2 = CreateFrame("V2", 0, 0);

        var listWithNull = new List<PlotItem> { valid1, null!, valid2 };

        // We record the empirical behavior
        var ex = Record.Exception(() => _service.OrderFrames(listWithNull));

        // It is expected to throw NullReferenceException because linq filter accesses i.Bounds directly
        Assert.NotNull(ex);
        Assert.IsType<NullReferenceException>(ex);
    }

    #endregion
}
