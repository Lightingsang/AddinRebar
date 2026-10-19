using HPKata.Revit.Abstractions;
using HPKata.Revit.Runners;
using HPKata.Revit.Tests.Mocks;
using Xunit;

namespace HPKata.Revit.Tests.Runners;

public sealed class RevitScheduleRunnerTests
{
    private static MockRevitDocumentContext CreatePopulatedContext()
    {
        var context = new MockRevitDocumentContext();
        context.AllRebars.Add(new RevitRebarDto
        {
            ElementId = 1,
            Partition = "D1",
            Mark = "1",
            Quantity = 2,
            DiameterMm = 20,
            SingleBarLengthMm = 6000,
            ShapeName = "M_00"
        });
        context.AllRebars.Add(new RevitRebarDto
        {
            ElementId = 2,
            Partition = "D1",
            Mark = "2",
            Quantity = 4,
            DiameterMm = 16,
            SingleBarLengthMm = 3000,
            ShapeName = "M_02"
        });
        context.AllRebars.Add(new RevitRebarDto
        {
            ElementId = 3,
            Partition = "D2",
            Mark = "1",
            Quantity = 10,
            DiameterMm = 8,
            SingleBarLengthMm = 1200,
            ShapeName = "M_11"
        });
        return context;
    }

    [Fact]
    public void Execute_GeneratesScheduleTableWithCorrectHeaders()
    {
        var context = CreatePopulatedContext();
        var table = RevitScheduleRunner.Execute(context);

        Assert.Equal(9, table.Headers.Count);
        Assert.Equal("STT", table.Headers[0]);
        Assert.Equal("Cấu kiện", table.Headers[1]);
        Assert.Equal("Trọng lượng (kg)", table.Headers[8]);
    }

    [Fact]
    public void Execute_AggregatesCountsAndWeightsCorrectly()
    {
        var context = CreatePopulatedContext();
        var table = RevitScheduleRunner.Execute(context);

        // Bar counts: 2 + 4 + 10 = 16 bars total
        Assert.Equal(16, table.TotalBarCount);
        Assert.True(table.TotalWeightKg > 40.0, "Total weight should be positive and substantial.");
        Assert.Equal(3, table.Rows.Count);
    }

    [Fact]
    public void Execute_PopulatesWeightByDiameterDictionary()
    {
        var context = CreatePopulatedContext();
        var table = RevitScheduleRunner.Execute(context);

        Assert.True(table.WeightByDiameter.ContainsKey("D20"));
        Assert.True(table.WeightByDiameter.ContainsKey("D16"));
        Assert.True(table.WeightByDiameter.ContainsKey("D8"));
    }

    [Fact]
    public void Execute_EmptyDocument_WritesNotificationAndReturnsEmptyTable()
    {
        var context = new MockRevitDocumentContext();
        var table = RevitScheduleRunner.Execute(context);

        Assert.Equal(0, table.TotalBarCount);
        Assert.Empty(table.Rows);
        Assert.Single(context.Notifications);
        Assert.Contains("Không tìm thấy", context.Notifications[0]);
    }

    [Fact]
    public void Execute_SelectedOnly_FiltersToSelectedRebars()
    {
        var context = CreatePopulatedContext();
        context.SelectedRebars.Add(context.AllRebars[0]); // Only the D20 rebar

        var table = RevitScheduleRunner.Execute(context, selectedOnly: true);

        Assert.Equal(2, table.TotalBarCount);
        Assert.Single(table.Rows);
        Assert.True(table.WeightByDiameter.ContainsKey("D20"));
        Assert.False(table.WeightByDiameter.ContainsKey("D16"));
    }

    [Fact]
    public void Execute_AllRebars_WhenSelectedEmpty()
    {
        var context = CreatePopulatedContext();
        // Selected is empty, selectedOnly is true -> falls back to all rebars
        var table = RevitScheduleRunner.Execute(context, selectedOnly: true);

        Assert.Equal(16, table.TotalBarCount);
        Assert.Equal(3, table.Rows.Count);
    }
}
