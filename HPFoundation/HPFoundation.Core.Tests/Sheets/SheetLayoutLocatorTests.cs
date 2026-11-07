using HPFoundation.Core.Sheets;
using Xunit;

namespace HPFoundation.Core.Tests.Sheets;

public sealed class SheetLayoutLocatorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Locate_LabelsMovedDown_FindsSameCells(int rowShift)
    {
        var result = SheetLayoutLocator.Locate(FourColumnSheetSample.Build(rowShift));

        Assert.Empty(result.MissingAnchors);
        var layout = result.Layout!;
        Assert.Equal(17 + rowShift, layout.FirstLoadRow);
        Assert.Equal(4, layout.LoadRowCount);
        Assert.Equal(29 + rowShift, layout.SpanRow);
        Assert.Equal(FourColumnSheetSample.At("D24", rowShift), layout.BeamDepth);
        Assert.Equal(FourColumnSheetSample.At("D25", rowShift), layout.FootingWidth);
        Assert.Equal(FourColumnSheetSample.At("D68", rowShift), layout.ColumnWidth);
        Assert.Equal(FourColumnSheetSample.At("B69", rowShift), layout.BeamWidth);
        Assert.Equal(FourColumnSheetSample.At("D72", rowShift), layout.FlangeDepth);
        Assert.Equal(FourColumnSheetSample.At("C110", rowShift), layout.ConcreteGrade);
        Assert.Equal(FourColumnSheetSample.At("C111", rowShift), layout.SteelGrade);
        Assert.Equal(128 + rowShift, layout.InteriorSpringRow);
        Assert.Equal(FourColumnSheetSample.At("F34", rowShift), layout.LongDirectionMoment);
        Assert.Equal(FourColumnSheetSample.At("D39", rowShift), layout.ShortDirectionMoment);
    }

    [Fact]
    public void Locate_LabelMissing_NamesTheAnchorAndGivesNoLayout()
    {
        var grid = FourColumnSheetSample.Build(edit: cells => cells.Remove(CellAddress.Parse("A69")));

        var result = SheetLayoutLocator.Locate(grid);

        Assert.Null(result.Layout);
        Assert.Contains("chọn bd =", result.MissingAnchors);
    }

    [Fact]
    public void Locate_TwoMatchingMomentFormulas_RefusesToGuess()
    {
        var grid = FourColumnSheetSample.Build(edit: cells =>
            cells[CellAddress.Parse("H34")] = cells[CellAddress.Parse("F34")]);

        var result = SheetLayoutLocator.Locate(grid);

        Assert.Null(result.Layout);
        Assert.Contains(result.MissingAnchors, anchor => anchor.Contains("long direction moment"));
    }

    [Fact]
    public void Locate_MomentFormulaSumsOtherRows_RefusesToGuess()
    {
        var grid = FourColumnSheetSample.Build(edit: cells =>
            cells[CellAddress.Parse("F34")] = SheetCell.FromNumber(1) with
            {
                Formula = "=ROUND(SUM(D17:D19)+ABS(C17*F29)+SUM(E17:E20)*D24,2)",
            });

        var result = SheetLayoutLocator.Locate(grid);

        Assert.Null(result.Layout);
        Assert.Contains(result.MissingAnchors, anchor => anchor.Contains("SUM(D17:D20)"));
    }

    [Fact]
    public void Locate_LoadHeaderColumnsDiffer_RefusesToGuess()
    {
        var grid = FourColumnSheetSample.Build(edit: cells =>
            cells[CellAddress.Parse("E15")] = SheetCell.FromText("Mttox"));

        var result = SheetLayoutLocator.Locate(grid);

        Assert.Null(result.Layout);
        Assert.Contains(result.MissingAnchors, anchor => anchor.Contains("Qttox"));
    }
}
