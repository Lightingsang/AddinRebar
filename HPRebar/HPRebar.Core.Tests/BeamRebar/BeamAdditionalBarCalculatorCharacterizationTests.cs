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
    public static TheoryData<string, BeamContinuousStack, bool, string> Scenarios() => new()
    {
        { "single-span", TestBeamData.SingleSpan(), false, "single-span:0810C162C15562DD7CA3FD53" },
        { "two-span", TestBeamData.TwoSpan(l1: 5400, l2: 6600), false, "two-span:52ACF439A63E6D0A82A09A30" },
        { "three-span", TestBeamData.ThreeSpan(), false, "three-span:D225C58FD73CAFDB7B163839" },
        { "cantilever-left", TestBeamData.CantileverLeft(), false, "cantilever-left:155106CC44B8A8ADC6F2DCFD" },
        { "variable-depth", TestBeamData.VariableDepth(), false, "variable-depth:B9FE1682876681DF55906E7D" },
        { "single-support", SingleSupport(), false, "single-support:4E075D2939644C8C20CACF0F" },
        { "layer-2-only", TestBeamData.ThreeSpan(), true, "layer-2-only:3EE2E235B57512F387A0E36A" }
    };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void ComputeSupportTopBars_FixedInput_ProducesTheRecordedBars(
        string scenario, BeamContinuousStack stack, bool layer2Only, string expectedHash)
    {
        // Arrange
        var spec = new BeamAdditionalTopBarSpec { SupportTopBars = ConfigsFor(stack, layer2Only) };

        // Act
        var bars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack, spec, stirrupDiameterMm: 10.0);

        // Assert
        Assert.NotEmpty(bars);
        Assert.Equal(expectedHash, $"{scenario}:{CharacterizationText.Hash(bars)}");
    }

    /// <summary>A beam with one support: support 0 is both the start and the end support.</summary>
    private static BeamContinuousStack SingleSupport()
    {
        var stack = TestBeamData.SingleSpan();
        return stack with { Supports = new[] { stack.Supports[0] } };
    }

    /// <summary>
    ///     One config per support with every branch exercised: both layers, defaulted and explicit ratios,
    ///     defaulted and explicit layer gap and exterior hook, plus an out-of-range support that must be skipped.
    ///     <paramref name="layer2Only"/> leaves layer 1 empty, so layer 2 is set out under a layer that is not there.
    /// </summary>
    private static IReadOnlyList<SupportAdditionalTopBarConfig> ConfigsFor(BeamContinuousStack stack, bool layer2Only)
    {
        var configs = Enumerable.Range(0, stack.Supports.Count)
            .Select(index => new SupportAdditionalTopBarConfig
            {
                SupportIndex = index,
                Layer1Count = layer2Only ? 0 : 2 + (index % 2),
                Layer1Diameter = 20.0 + (2 * index),
                Layer1ExtensionRatio = index % 2 == 0 ? 0.0 : 0.3,
                Layer2Count = !layer2Only && index == 1 ? 0 : 2,
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
