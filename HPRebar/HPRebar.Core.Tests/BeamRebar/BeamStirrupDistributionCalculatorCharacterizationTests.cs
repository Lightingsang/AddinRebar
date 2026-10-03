using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

/// <summary>
///     Pins the runs <see cref="BeamStirrupDistributionCalculator.ComputeSpanRuns"/> returns for every layout path —
///     cantilever, uniform, both three-zone layouts, the short-span collapse and the one-stirrup midspan — so
///     restructuring it cannot change its output unnoticed. The expected hashes were captured before it was restructured.
/// </summary>
public sealed class BeamStirrupDistributionCalculatorCharacterizationTests
{
    public static TheoryData<string, double, BeamStirrupSpec, bool, string> Scenarios() => new()
    {
        { "cantilever", 1850.0, TestBeamData.UniformStirrupSpec(spacing: 110.0), true, "cantilever:1EF0E596085DEA06DFC1F8E9" },
        { "cantilever-too-short", 60.0, TestBeamData.UniformStirrupSpec(spacing: 110.0), true, "cantilever-too-short:4F53CDA18C2BAA0C0354BB5F" },
        { "uniform", 5730.0, TestBeamData.UniformStirrupSpec(spacing: 140.0), false, "uniform:BD5541AB25BE3AE90591AAF6" },
        { "three-zone-l4", 6120.0, TestBeamData.ThreeZoneL4StirrupSpec(s1: 95.0, s2: 210.0), false, "three-zone-l4:AB121F0FD023108367B40858" },
        { "three-zone-l3", 5470.0, TestBeamData.ThreeZoneL3StirrupSpec(s1: 105.0, s2: 190.0), false, "three-zone-l3:B8DECF5632E5B252DBB29388" },
        { "short-span-collapse", 580.0, TestBeamData.ThreeZoneL3StirrupSpec(s1: 100.0, s2: 200.0), false, "short-span-collapse:72A0F49C98C932470DB6D7F5" },
        {
            "one-midspan-stirrup", 600.0, TestBeamData.ThreeZoneL3StirrupSpec(s1: 150.0, s2: 200.0) with { StartOffset = 50.0 },
            false, "one-midspan-stirrup:A85348D8AF734A5AB1DB72E0"
        }
    };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void ComputeSpanRuns_FixedInput_ProducesTheRecordedRuns(
        string scenario, double clearSpanMm, BeamStirrupSpec spec, bool isCantilever, string expectedHash)
    {
        // Act
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(clearSpanMm, spec, isCantilever);

        // Assert
        Assert.Equal(expectedHash, $"{scenario}:{CharacterizationText.Hash(runs)}");
    }

    [Theory]
    [InlineData(400.0, 25.0, 150.0, "node-standard:68CFB8FC9B4F5327066B13B7")]
    [InlineData(630.0, 40.0, 95.0, "node-wide:529DCB886F87479A81288DF1")]
    [InlineData(450.0, 30.0, 1000.0, "node-one-stirrup:1B448281123639AC60025A45")]
    [InlineData(40.0, 25.0, 100.0, "node-no-room:83CDA8F5750120A2183034E8")]
    public void ComputeNodeRun_FixedInput_ProducesTheRecordedRun(
        double supportWidthMm, double coverMm, double spacingMm, string expected)
    {
        // Act
        var run = BeamStirrupDistributionCalculator.ComputeNodeRun(supportWidthMm, coverMm, spacingMm);

        // Assert
        string scenario = expected.Split(':')[0];
        Assert.Equal(expected, $"{scenario}:{CharacterizationText.Hash(run)}");
    }
}
