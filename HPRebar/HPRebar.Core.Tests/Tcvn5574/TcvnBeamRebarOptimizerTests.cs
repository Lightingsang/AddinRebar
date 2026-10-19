using System;
using HPRebar.Core.Tcvn5574.Calculators;
using Xunit;

namespace HPRebar.Core.Tests.Tcvn5574;

public sealed class TcvnBeamRebarOptimizerTests
{
    [Fact]
    public void ClearSpacingValidation_WhenBarsAreCrowded_FailsWithCorrectSpacing()
    {
        // Beam width b = 200 mm, cover = 25 mm, stirrup = 8 mm, 4 bars d25
        // W_clear = 200 - 2 * (25 + 8) = 134 mm
        // W_bars = 4 * 25 = 100 mm
        // t = (134 - 100) / 3 = 11.333 mm < 25 mm => Fails
        double b = 200.0;
        int count = 4;
        double diameter = 25.0;

        var (spacing, isCompliant, requiredSpacing) = TcvnBeamRebarOptimizer.ValidateClearSpacing(
            count, diameter, b, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        Assert.Equal(34.0 / 3.0, spacing, precision: 2);
        Assert.False(isCompliant);
        Assert.Equal(25.0, requiredSpacing);
    }

    [Fact]
    public void ClearSpacingValidation_BottomBars_RequiresAtLeastMax25AndDiameter()
    {
        // Beam b = 300 mm, cover = 25 mm, stirrup = 8 mm, 3 bars d20
        // W_clear = 300 - 66 = 234 mm
        // W_bars = 3 * 20 = 60 mm
        // t = (234 - 60) / 2 = 87 mm >= max(25, 20) = 25 mm => Passes
        double b = 300.0;
        int count = 3;
        double diameter = 20.0;

        var (spacing, isCompliant, requiredSpacing) = TcvnBeamRebarOptimizer.ValidateClearSpacing(
            count, diameter, b, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        Assert.Equal(87.0, spacing, precision: 2);
        Assert.True(isCompliant);
        Assert.Equal(25.0, requiredSpacing);
    }

    [Fact]
    public void ClearSpacingValidation_TopBars_RequiresAtLeastMax30AndDiameter()
    {
        // Beam b = 300 mm, cover = 25 mm, stirrup = 8 mm, 4 bars d32
        // W_clear = 234 mm
        // W_bars = 4 * 32 = 128 mm
        // t = (234 - 128) / 3 = 35.33 mm >= max(30, 32) = 32 mm => Passes
        double b = 300.0;
        int count = 4;
        double diameter = 32.0;

        var (spacing, isCompliant, requiredSpacing) = TcvnBeamRebarOptimizer.ValidateClearSpacing(
            count, diameter, b, cover: 25.0, stirrupDiameter: 8.0, isTop: true);

        Assert.Equal(106.0 / 3.0, spacing, precision: 2);
        Assert.True(isCompliant);
        Assert.Equal(32.0, requiredSpacing);
    }

    [Fact]
    public void Optimize_SelectsCompliantLayout_MeetingRequiredArea()
    {
        // Require As = 815 mm2 for b = 300, h = 600
        double asReq = 815.0;
        double b = 300.0;
        double h = 600.0;

        var option = TcvnBeamRebarOptimizer.Optimize(asReq, b, h, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        Assert.True(option.IsValid);
        Assert.True(option.IsSpacingCompliant);
        Assert.True(option.ProvidedArea >= asReq);
        Assert.True(option.WasteRatioPercent >= 0.0);
        Assert.True(option.MinClearSpacing >= 25.0);
    }

    [Fact]
    public void Optimize_ExtremeSteelArea_SelectsTwoLayersOrCrowdedFallback()
    {
        // Extreme As required: 3000 mm2 in narrow beam b = 250 mm
        double asReq = 3000.0;
        double b = 250.0;
        double h = 600.0;

        var option = TcvnBeamRebarOptimizer.Optimize(asReq, b, h, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        // If it fits, it must be multi-layer; if not, flagged as not valid
        if (option.IsValid)
        {
            Assert.True(option.LayerCount >= 2);
            Assert.True(option.ProvidedArea >= asReq);
        }
        else
        {
            Assert.False(option.IsSpacingCompliant);
        }
    }

    [Fact]
    public void GenerateCandidates_ReturnsNonEmptyListOfCandidates()
    {
        double asReq = 500.0;
        double b = 300.0;

        var candidates = TcvnBeamRebarOptimizer.GenerateCandidates(asReq, b, 25.0, 8.0, false);

        Assert.NotEmpty(candidates);
        Assert.All(candidates, c => Assert.True(c.ProvidedArea >= asReq));
    }
}
