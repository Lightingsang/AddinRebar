using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataExport;

public sealed class KataExcelCellTests
{
    [Theory]
    [InlineData("+3.300", "'+3.300")]
    [InlineData("-0.050", "'-0.050")]
    [InlineData("+0.000", "'+0.000")]
    public void ElevationTextStaysTextEvenWhenItLooksLikeANumber(string elevation, string expected)
    {
        Assert.Equal(expected, KataExcelCell.ToCellValue(new KataText(elevation)));
    }

    [Theory]
    [InlineData("1", 1.0)]
    [InlineData("-75", -75.0)]
    [InlineData("12.5", 12.5)]
    public void PlainNumberStringsBecomeNumbers(string text, double expected)
    {
        Assert.Equal(expected, KataExcelCell.ToCellValue(text));
    }

    [Theory]
    [InlineData("1-2", "'1-2")]
    [InlineData("3/4", "'3/4")]
    [InlineData("300x600", "'300x600")]
    [InlineData("400;-50", "'400;-50")]
    [InlineData("1E5", "'1E5")]
    [InlineData("T1-DX12", "'T1-DX12")]
    [InlineData("01", "'01")]
    [InlineData("١٢", "'١٢")]
    public void OtherTextIsPrefixedSoExcelCannotTurnItIntoADateOrNumber(string text, string expected)
    {
        Assert.Equal(expected, KataExcelCell.ToCellValue(text));
    }

    [Fact]
    public void EmptyAndNullStayTrulyEmpty()
    {
        Assert.Equal("", KataExcelCell.ToCellValue(null));
        Assert.Equal("", KataExcelCell.ToCellValue(""));
        Assert.Equal("", KataExcelCell.ToCellValue(new KataText("")));
    }

    [Fact]
    public void NumbersPassThroughUnchanged()
    {
        Assert.Equal(350.0, KataExcelCell.ToCellValue(350.0));
        Assert.Equal(1, KataExcelCell.ToCellValue(1));
    }
}
