using HPFoundation.Core.Modeling;
using Xunit;

namespace HPFoundation.Core.Tests.Modeling;

public sealed class ConcreteCatalogTests
{
    [Theory]
    [InlineData("B15", 24000)]
    [InlineData("B20", 27500)]
    [InlineData("B22.5", 29000)]
    [InlineData("B25", 30000)]
    [InlineData("B30", 32500)]
    [InlineData("B35", 34500)]
    [InlineData("B40", 36000)]
    [InlineData("B45", 37000)]
    [InlineData("B50", 38000)]
    [InlineData("B60", 39500)]
    public void ElasticModulusMpa_KnownGrade_MatchesTheCompanyTable(string grade, double expectedMpa)
    {
        Assert.Equal(expectedMpa, ConcreteCatalog.ElasticModulusMpa(grade));
    }

    [Fact]
    public void ElasticModulusTPerM2_B20_ConvertsFromMegapascals()
    {
        Assert.Equal(2_804_219.6, ConcreteCatalog.ElasticModulusTPerM2("B20"), 1);
    }

    [Fact]
    public void ElasticModulusMpa_UnknownGrade_Throws()
    {
        Assert.Throws<ArgumentException>(() => ConcreteCatalog.ElasticModulusMpa("B70"));
    }
}
