using HPFoundation.Core.Sheets;
using Xunit;

namespace HPFoundation.Core.Tests.Sheets;

public sealed class CellAddressTests
{
    [Theory]
    [InlineData("A1", 1, 1)]
    [InlineData("F34", 34, 6)]
    [InlineData("AC202", 202, 29)]
    public void Parse_ValidAddress_ReturnsRowAndColumn(string text, int row, int column)
    {
        var address = CellAddress.Parse(text);

        Assert.Equal(new CellAddress(row, column), address);
        Assert.Equal(text, address.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("34F")]
    [InlineData("A0")]
    [InlineData("$A$1")]
    public void Parse_InvalidAddress_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() => CellAddress.Parse(text));
    }
}
