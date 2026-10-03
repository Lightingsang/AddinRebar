using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

/// <summary>
///     Pins every bar <see cref="BeamAdditionalBarCalculator.ComputeSupportTopBars"/> returns (points, hooks,
///     extensions, indices, host support/span) so restructuring it cannot change its output unnoticed.
///     The expected hashes were captured from the calculator before it was restructured.
/// </summary>
public sealed class BeamAdditionalBarCalculatorCharacterizationTests
{
    [Theory]
    [InlineData("single-span", "single-span:0810C162C15562DD7CA3FD53")]
    [InlineData("two-span", "two-span:52ACF439A63E6D0A82A09A30")]
    [InlineData("three-span", "three-span:D225C58FD73CAFDB7B163839")]
    [InlineData("cantilever-left", "cantilever-left:155106CC44B8A8ADC6F2DCFD")]
    [InlineData("variable-depth", "variable-depth:B9FE1682876681DF55906E7D")]
    public void ComputeSupportTopBars_FixedInput_ProducesTheRecordedBars(string scenario, string expectedHash)
    {
        // Arrange
        var stack = scenario switch
        {
            "single-span" => TestBeamData.SingleSpan(),
            "two-span" => TestBeamData.TwoSpan(l1: 5400, l2: 6600),
            "three-span" => TestBeamData.ThreeSpan(),
            "cantilever-left" => TestBeamData.CantileverLeft(),
            _ => TestBeamData.VariableDepth()
        };
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = ConfigsFor(stack) };

        // Act
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, stirrupDiameterMm: 10.0);

        // Assert
        Assert.NotEmpty(bars);
        Assert.Equal(expectedHash, $"{scenario}:{CharacterizationText.Hash(bars)}");
    }

    /// <summary>
    ///     One config per support with every branch exercised: both layers, defaulted and explicit ratios,
    ///     defaulted and explicit layer gap and exterior hook, plus an out-of-range support that must be skipped.
    /// </summary>
    private static IReadOnlyList<SupportAdditionalTopBarConfig> ConfigsFor(BeamContinuousStack stack)
    {
        var configs = Enumerable.Range(0, stack.Supports.Count)
            .Select(index => new SupportAdditionalTopBarConfig
            {
                SupportIndex = index,
                Layer1Count = 2 + (index % 2),
                Layer1Diameter = 20.0 + (2 * index),
                Layer1ExtensionRatio = index % 2 == 0 ? 0.0 : 0.3,
                Layer2Count = index == 1 ? 0 : 2,
                Layer2Diameter = 16.0,
                Layer2ExtensionRatio = index % 2 == 0 ? 0.22 : 0.0,
                LayerGap = index % 2 == 0 ? 0.0 : 45.0,
                ExteriorHookLength = index == 0 ? 0.0 : 260.0,
                BarTypeName = $"T{index}"
            })
            .ToList();

        configs.Add(new SupportAdditionalTopBarConfig { SupportIndex = stack.Supports.Count, Layer1Count = 2, Layer1Diameter = 20.0 });
        return configs;
    }
}
