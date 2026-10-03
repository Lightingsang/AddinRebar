using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class ViewNamingTests
{
    [Theory]
    [InlineData(null, null, "Detail", "MC")]
    [InlineData("", "  ", "Detail", "MC")]
    [InlineData("  C1 ", " MC2 ", "C1", "MC2")]
    [InlineData("Cot", "S", "Cot", "S")]
    public void From_TypedNames_TrimsAndFallsBackToDefaults(string? detail, string? suffix, string expectedDetail, string expectedSuffix)
    {
        var naming = ViewNaming.From(detail, suffix);

        Assert.Equal(expectedDetail, naming.DetailViewName);
        Assert.Equal(expectedSuffix, naming.SectionSuffix);
    }

    [Fact]
    public void ElevationNames_DefaultNaming_AppendsXAndY()
    {
        Assert.Equal(("DetailX", "DetailY"), ViewNaming.Default.ElevationNames());
    }

    [Fact]
    public void SectionName_DefaultNaming_JoinsNameColumnNumberAndSuffix()
    {
        Assert.Equal("Detail 3 MC", ViewNaming.Default.SectionName(3));
    }

    [Theory]
    [InlineData("C1:Detail", "MC", ':')]
    [InlineData("Detail", "MC{1}", '{')]
    public void TryFindForbiddenCharacter_NameRevitRefuses_ReturnsTheCharacter(string detail, string suffix, char expected)
    {
        Assert.True(new ViewNaming(detail, suffix).TryFindForbiddenCharacter(out var found));
        Assert.Equal(expected, found);
    }

    [Fact]
    public void TryFindForbiddenCharacter_PlainNames_ReturnsFalse()
    {
        Assert.False(new ViewNaming("Cột C1-2", "MC").TryFindForbiddenCharacter(out _));
    }
}
