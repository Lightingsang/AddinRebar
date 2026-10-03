using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

/// <summary>
///     Pins every top and bottom main bar <see cref="BeamMainBarCalculator"/> returns — unspliced, spliced with and
///     without stagger, cantilevers, a depth step, explicit, defaulted and negative hooks — so restructuring it
///     cannot change its output unnoticed. The expected hashes were captured before it was restructured.
/// </summary>
public sealed class BeamMainBarCalculatorCharacterizationTests
{
    public static TheoryData<string, BeamContinuousStack, BeamMainBarSpec, string> Scenarios() => new()
    {
        { "single-span", TestBeamData.SingleSpan(), TestBeamData.MainBarSpec(), "single-span:D95C4EF053DD1A6473103171" },
        {
            "shallow-default-hooks", TestBeamData.SingleSpan(height: 60),
            TestBeamData.MainBarSpec(hookLength: 0.0), "shallow-default-hooks:CACF1D0BBA317E307C1BD17C"
        },
        { "three-span-spliced", TestBeamData.ThreeSpan(), TestBeamData.MainBarSpec(topCount: 4, bottomCount: 5), "three-span-spliced:2D5D40DC654F112B2A486018" },
        {
            "three-span-no-stagger", TestBeamData.ThreeSpan(),
            TestBeamData.MainBarSpec(hookLength: 0.0) with { EnableStagger = false, MaxStockLength = 0.0 },
            "three-span-no-stagger:EA7CB79C2EB23E0C2E86FEFB"
        },
        { "two-span-spliced", TestBeamData.TwoSpan(), TestBeamData.MainBarSpec() with { MaxStockLength = 9000.0 }, "two-span-spliced:A2541AD65732690EBD192590" },
        { "cantilever-left", TestBeamData.CantileverLeft(), TestBeamData.MainBarSpec(), "cantilever-left:D9EFF9BA009964FD0547843C" },
        {
            "cantilever-left-spliced", TestBeamData.CantileverLeft(),
            TestBeamData.MainBarSpec(hookLength: 0.0) with { MaxStockLength = 5000.0 }, "cantilever-left-spliced:2B51961AB86570C6F1222E37"
        },
        { "variable-depth", TestBeamData.VariableDepth(), TestBeamData.MainBarSpec(), "variable-depth:EF63D51C32C2AF335B0ED2CE" },
        {
            "explicit-bottom-start-hook", TestBeamData.SingleSpan(),
            TestBeamData.MainBarSpec() with { BottomStartHookLength = 300.0, TopEndHookLength = 0.0 },
            "explicit-bottom-start-hook:"
        }
    };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void ComputeMainBars_FixedInput_ProducesTheRecordedBars(
        string scenario, BeamContinuousStack stack, BeamMainBarSpec spec, string expectedHash)
    {
        // Act
        var top = BeamMainBarCalculator.ComputeTopMainBars(stack, spec, stirrupDiameterMm: 10.0);
        var bottom = BeamMainBarCalculator.ComputeBottomMainBars(stack, spec, stirrupDiameterMm: 10.0);

        // Assert
        Assert.NotEmpty(top);
        Assert.NotEmpty(bottom);
        Assert.Equal(expectedHash, $"{scenario}:{CharacterizationText.Hash(new object[] { top, bottom })}");
    }
}
