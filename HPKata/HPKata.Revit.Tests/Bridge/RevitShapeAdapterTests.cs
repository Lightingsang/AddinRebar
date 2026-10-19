using HPKata.Revit.Bridge;
using Xunit;

namespace HPKata.Revit.Tests.Bridge;

public sealed class RevitShapeAdapterTests
{
    [Theory]
    [InlineData("M_00", "dang01")]
    [InlineData("00", "dang01")]
    [InlineData("Rebar Shape 00", "dang01")]
    [InlineData("Straight", "dang01")]
    [InlineData("dang01", "dang01")]
    public void ResolveKataShape_StraightShapes_MapsToDang01(string revitName, string expectedKata)
    {
        string result = RevitShapeAdapter.ResolveKataShape(revitName);
        Assert.Equal(expectedKata, result);
    }

    [Theory]
    [InlineData("M_02", "dang03")]
    [InlineData("02", "dang03")]
    [InlineData("Rebar Shape 02", "dang03")]
    public void ResolveKataShape_LShape_MapsToDang03(string revitName, string expectedKata)
    {
        string result = RevitShapeAdapter.ResolveKataShape(revitName);
        Assert.Equal(expectedKata, result);
    }

    [Theory]
    [InlineData("M_11", "dang22")]
    [InlineData("11", "dang22")]
    [InlineData("M_51", "dang22")]
    [InlineData("51", "dang22")]
    public void ResolveKataShape_ClosedStirrup_MapsToDang22(string revitName, string expectedKata)
    {
        string result = RevitShapeAdapter.ResolveKataShape(revitName);
        Assert.Equal(expectedKata, result);
    }

    [Fact]
    public void ResolveKataShape_SymmetricUBar_MapsToDang02()
    {
        double[] parametersMm = [500.0, 300.0, 500.2]; // |A - C| = 0.2 < 1.0mm
        string result = RevitShapeAdapter.ResolveKataShape("M_21", parametersMm);
        Assert.Equal("dang02", result);
    }

    [Fact]
    public void ResolveKataShape_AsymmetricUBar_MapsToDang04()
    {
        double[] parametersMm = [500.0, 300.0, 650.0]; // |A - C| = 150.0 >= 1.0mm
        string result = RevitShapeAdapter.ResolveKataShape("M_21", parametersMm);
        Assert.Equal("dang04", result);
    }

    [Theory]
    [InlineData("dang01", "M_00")]
    [InlineData("01", "M_00")]
    [InlineData("dang02", "M_T1")]
    [InlineData("dang03", "M_02")]
    [InlineData("dang04", "M_21")]
    [InlineData("dang22", "M_11")]
    [InlineData("dang40", "M_77")]
    public void ResolveRevitShape_KataCodes_MapsToStandardRevitNames(string kataCode, string expectedRevit)
    {
        string result = RevitShapeAdapter.ResolveRevitShape(kataCode);
        Assert.Equal(expectedRevit, result);
    }
}
