using HPFoundation.Core.Sheets;
using Xunit;

namespace HPFoundation.Core.Tests.Sheets;

public sealed class FoundationInputBuilderTests
{
    [Fact]
    public void Build_FourColumnSheet_ReadsLoadsAtCumulativeAxisPositions()
    {
        var input = BuildInput(FourColumnSheetSample.Build(), out var issues);

        Assert.Empty(issues);
        Assert.Equal(new[] { "A", "B", "C", "D" }, input!.Columns.Select(column => column.Axis));
        Assert.Equal(new[] { 1.6, 5.3, 9.0, 11.4 }, input.Columns.Select(column => Math.Round(column.AxisPositionM, 6)));
        Assert.Equal(11.7, input.FootingLengthM, 6);
        var axisC = input.Columns[2];
        Assert.Equal(100.5, axisC.AxialT);
        Assert.Equal(1.1, axisC.MomentXTm);
        Assert.Equal(1.5, axisC.ShearXT);
        Assert.Equal(3.1, axisC.MomentYTm);
        Assert.Equal(2.4, axisC.ShearYT);
    }

    [Fact]
    public void Build_FourColumnSheet_ReadsSectionMaterialsAndSprings()
    {
        var input = BuildInput(FourColumnSheetSample.Build(), out _);

        Assert.Equal(2.4, input!.Section.FootingWidthM);
        Assert.Equal(0.5, input.Section.BeamWidthM);
        Assert.Equal(0.8, input.Section.BeamDepthM);
        Assert.Equal(0.6, input.Section.FlangeDepthM);
        Assert.Equal(0.35, input.Section.ColumnWidthM);
        Assert.Equal("B20", input.ConcreteGrade);
        Assert.Equal("CB400-V", input.SteelGrade);
        Assert.Equal(159.72466, input.Springs.InteriorStiffnessTPerM);
        Assert.Equal(79.862329, input.Springs.EndStiffnessTPerM);
        Assert.Equal(0.1, input.Springs.InteriorSpacingM);
        Assert.Equal(0.05, input.Springs.EndTributaryLengthM);
    }

    [Fact]
    public void Build_ErrorInSpringCell_ReportsTheCell()
    {
        var grid = FourColumnSheetSample.Build(edit: cells =>
            cells[CellAddress.Parse("F128")] = SheetCell.FromError("#DIV/0!"));

        var input = BuildInput(grid, out var issues);

        Assert.Null(input);
        Assert.Contains(issues, issue => issue.Cell == "F128" && issue.Message.Contains("#DIV/0!"));
    }

    [Fact]
    public void Build_TextInNumberCell_ReportsTheCell()
    {
        var grid = FourColumnSheetSample.Build(edit: cells =>
            cells[CellAddress.Parse("D24")] = SheetCell.FromText("kiểm tra lại"));

        var input = BuildInput(grid, out var issues);

        Assert.Null(input);
        Assert.Contains(issues, issue => issue.Cell == "D24");
    }

    [Fact]
    public void Build_BlankInterSpan_ReportsTheCell()
    {
        var grid = FourColumnSheetSample.Build(edit: cells => cells.Remove(CellAddress.Parse("C29")));

        var input = BuildInput(grid, out var issues);

        Assert.Null(input);
        Assert.Contains(issues, issue => issue.Cell == "C29");
    }

    [Fact]
    public void Build_AxesNumberedInsteadOfLettered_UsesTheNumberAsLabel()
    {
        var grid = FourColumnSheetSample.Build(edit: cells =>
            cells[CellAddress.Parse("B18")] = SheetCell.FromNumber(2));

        var input = BuildInput(grid, out var issues);

        Assert.Empty(issues);
        Assert.Equal("2", input!.Columns[1].Axis);
    }

    private static Model.FoundationInput? BuildInput(SheetGrid grid, out IReadOnlyList<SheetIssue> issues)
    {
        var layout = SheetLayoutLocator.Locate(grid).Layout!;
        var result = FoundationInputBuilder.Build(grid, layout);
        issues = result.Issues;
        return result.Input;
    }
}
