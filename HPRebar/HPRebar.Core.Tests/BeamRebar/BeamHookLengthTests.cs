using HPRebar.Core.BeamRebar.Calculators;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamHookLengthTests
{
    [Theory]
    [InlineData(6.0, 200.0)]
    [InlineData(20.0, 600.0)]
    [InlineData(32.0, 960.0)]
    public void Default_BarDiameter_ReturnsThirtyDiametersButAtLeast200(double diameterMm, double expectedMm)
    {
        Assert.Equal(expectedMm, BeamHookLength.Default(diameterMm), 6);
    }
}
