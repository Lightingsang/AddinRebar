using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.Core.SmartPlot.Services;
using Xunit;

namespace HPAutoCad.Tests.SmartPlot;

public sealed class PlotOrderServiceTests
{
    private readonly PlotOrderService _service = new();

    private static PlotItem CreateFrame(string id, double minX, double minY, double width = 841.0, double height = 594.0)
    {
        return new PlotItem
        {
            Id = id,
            DisplayName = $"Frame {id}",
            Bounds = new PlotBounds(minX, minY, minX + width, minY + height),
            SourceHandle = $"H_{id}"
        };
    }

    [Fact]
    public void Sort_NullOrEmpty_ReturnsEmpty()
    {
        var nullResult = _service.OrderFrames(null!);
        Assert.Empty(nullResult);

        var emptyResult = _service.OrderFrames(Array.Empty<PlotItem>());
        Assert.Empty(emptyResult);
    }

    [Fact]
    public void Sort_SingleItem_ReturnsItemWithOrder1()
    {
        var item = CreateFrame("1", 100, 100);
        var result = _service.OrderFrames(new[] { item });

        Assert.Single(result);
        Assert.Equal(1, result[0].Order);
        Assert.Equal("1", result[0].Id);
    }

    [Fact]
    public void Sort_Grid_3x3_TopToBottomLeftToRight()
    {
        // 3 rows (Y = 2000, 1000, 0), 3 columns (X = 0, 1000, 2000)
        // Shuffled input order to verify sorting correctness
        var items = new List<PlotItem>
        {
            CreateFrame("R3C2", 1000, 0),
            CreateFrame("R1C1", 0, 2000),
            CreateFrame("R2C3", 2000, 1000),
            CreateFrame("R1C3", 2000, 2000),
            CreateFrame("R3C1", 0, 0),
            CreateFrame("R2C1", 0, 1000),
            CreateFrame("R1C2", 1000, 2000),
            CreateFrame("R3C3", 2000, 0),
            CreateFrame("R2C2", 1000, 1000),
        };

        var sorted = _service.OrderFrames(items);

        Assert.Equal(9, sorted.Count);

        // Row 1: Top (Y=2000) -> Left to Right
        Assert.Equal("R1C1", sorted[0].Id);
        Assert.Equal(1, sorted[0].Order);
        Assert.Equal("R1C2", sorted[1].Id);
        Assert.Equal(2, sorted[1].Order);
        Assert.Equal("R1C3", sorted[2].Id);
        Assert.Equal(3, sorted[2].Order);

        // Row 2: Middle (Y=1000) -> Left to Right
        Assert.Equal("R2C1", sorted[3].Id);
        Assert.Equal(4, sorted[3].Order);
        Assert.Equal("R2C2", sorted[4].Id);
        Assert.Equal(5, sorted[4].Order);
        Assert.Equal("R2C3", sorted[5].Id);
        Assert.Equal(6, sorted[5].Order);

        // Row 3: Bottom (Y=0) -> Left to Right
        Assert.Equal("R3C1", sorted[6].Id);
        Assert.Equal(7, sorted[6].Order);
        Assert.Equal("R3C2", sorted[7].Id);
        Assert.Equal(8, sorted[7].Order);
        Assert.Equal("R3C3", sorted[8].Id);
        Assert.Equal(9, sorted[8].Order);
    }

    [Fact]
    public void Sort_MisalignedRow_WithinToleranceBand_GroupsInSameRow()
    {
        // Two frames in the same visual row, but slightly shifted vertically:
        // Frame A: Y = 1000..1594 (Height 594), X = 0..841
        // Frame B: Y = 960..1554 (Height 594, shifted by 40mm down), X = 900..1741
        // Vertical overlap = 1554 - 1000 = 554mm -> 554/594 = 93.3% > 50%
        var frameB = CreateFrame("B", 900, 960);
        var frameA = CreateFrame("A", 0, 1000);

        var result = _service.OrderFrames(new[] { frameB, frameA }, overlapRatioThreshold: 0.5);

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Id);
        Assert.Equal(1, result[0].Order);
        Assert.Equal("B", result[1].Id);
        Assert.Equal(2, result[1].Order);
    }

    [Fact]
    public void Sort_ZicZacStaggered_CorrectlyClustersDistinctRows()
    {
        // Row 1: Frames staggered by 30mm-40mm (Heights 594mm)
        // Row 2: Frames staggered by 20mm, located at Y ~ 200mm (Clear vertical gap > 300mm from Row 1)
        var items = new List<PlotItem>
        {
            CreateFrame("R2_2", 900, 220),
            CreateFrame("R1_1", 0, 1000),
            CreateFrame("R2_1", 0, 200),
            CreateFrame("R1_3", 1800, 970),
            CreateFrame("R1_2", 900, 1030),
            CreateFrame("R2_3", 1800, 190),
        };

        var sorted = _service.OrderFrames(items, overlapRatioThreshold: 0.5);

        Assert.Equal(6, sorted.Count);

        // Row 1 should be Top, sorted Left to Right
        Assert.Equal("R1_1", sorted[0].Id);
        Assert.Equal(1, sorted[0].Order);
        Assert.Equal("R1_2", sorted[1].Id);
        Assert.Equal(2, sorted[1].Order);
        Assert.Equal("R1_3", sorted[2].Id);
        Assert.Equal(3, sorted[2].Order);

        // Row 2 should be Bottom, sorted Left to Right
        Assert.Equal("R2_1", sorted[3].Id);
        Assert.Equal(4, sorted[3].Order);
        Assert.Equal("R2_2", sorted[4].Id);
        Assert.Equal(5, sorted[4].Order);
        Assert.Equal("R2_3", sorted[5].Id);
        Assert.Equal(6, sorted[5].Order);
    }

    [Fact]
    public void Sort_VerticalColumn_OrdersTopToBottom()
    {
        var items = new List<PlotItem>
        {
            CreateFrame("B3", 0, 0),
            CreateFrame("B1", 0, 2000),
            CreateFrame("B2", 0, 1000),
        };

        var sorted = _service.Sort(items);

        Assert.Equal(3, sorted.Count);
        Assert.Equal("B1", sorted[0].Id);
        Assert.Equal("B2", sorted[1].Id);
        Assert.Equal("B3", sorted[2].Id);
    }

    [Fact]
    public void Sort_HorizontalRow_OrdersLeftToRight()
    {
        var items = new List<PlotItem>
        {
            CreateFrame("C3", 2000, 500),
            CreateFrame("C1", 0, 500),
            CreateFrame("C2", 1000, 500),
        };

        var sorted = _service.Sort(items);

        Assert.Equal(3, sorted.Count);
        Assert.Equal("C1", sorted[0].Id);
        Assert.Equal("C2", sorted[1].Id);
        Assert.Equal("C3", sorted[2].Id);
    }

    [Fact]
    public void Sort_PreservesItemProperties()
    {
        var original = new PlotItem
        {
            Id = "frame_x",
            Bounds = new PlotBounds(0, 0, 841, 594),
            DisplayName = "Special Sheet",
            LayoutName = "Layout1",
            SheetNumber = "S-01",
            SheetTitle = "General Notes",
            SourceHandle = "HANDLE_123",
            Rotation = 90.0,
            IsSelected = true
        };

        var sorted = _service.OrderFrames(new[] { original });

        Assert.Single(sorted);
        var result = sorted[0];
        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.DisplayName, result.DisplayName);
        Assert.Equal(original.LayoutName, result.LayoutName);
        Assert.Equal(original.SheetNumber, result.SheetNumber);
        Assert.Equal(original.SheetTitle, result.SheetTitle);
        Assert.Equal(original.SourceHandle, result.SourceHandle);
        Assert.Equal(original.Rotation, result.Rotation);
        Assert.Equal(original.IsSelected, result.IsSelected);
        Assert.Equal(1, result.Order);
    }

    [Fact]
    public void Sort_FiltersInvalidBounds()
    {
        var valid = CreateFrame("valid", 0, 0);
        var invalid = new PlotItem
        {
            Id = "invalid",
            Bounds = new PlotBounds(double.NaN, 0, 100, 100)
        };

        var sorted = _service.OrderFrames(new[] { valid, invalid });

        Assert.Single(sorted);
        Assert.Equal("valid", sorted[0].Id);
    }
}
