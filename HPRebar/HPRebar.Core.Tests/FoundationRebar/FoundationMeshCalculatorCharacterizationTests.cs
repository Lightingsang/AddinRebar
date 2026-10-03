using HPRebar.Core.FoundationRebar.Calculators;
using HPRebar.Core.FoundationRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.FoundationRebar;

/// <summary>
///     Pins the whole mesh — every bar's layer, order, diameter, hook and world/local points, plus the statistics —
///     so restructuring <see cref="FoundationMeshCalculator"/> cannot change its output unnoticed.
///     The expected hashes were captured from the calculator before it was restructured.
/// </summary>
public sealed class FoundationMeshCalculatorCharacterizationTests
{
    [Theory]
    [InlineData("axis-no-hooks", 0.0, 600.0, false, 0.0, false, false, "axis-no-hooks:E3A0ED9256A9EA1F7D29ED60")]
    [InlineData("axis-hooks-top", 0.0, 600.0, true, 0.0, true, false, "axis-hooks-top:6C6D1B8CE6DF7FE44A25B711")]
    [InlineData("rotated-hooks-bottom-only", 30.0, 600.0, true, 0.0, false, false, "rotated-hooks-bottom-only:2D2977A83EAD214110AF3851")]
    [InlineData("rotated-equal-spacing", 47.5, 600.0, true, 0.0, true, true, "rotated-equal-spacing:84127F350FEC276FD22CDDAF")]
    [InlineData("thin-clamped-hooks", 0.0, 250.0, true, 400.0, true, false, "thin-clamped-hooks:C4C833AFF43E5B6FBD265DAD")]
    public void Calculate_FixedInput_ProducesTheRecordedMesh(
        string scenario,
        double angleDegrees,
        double thickness,
        bool hooks,
        double hookLength,
        bool topMat,
        bool equalSpacing,
        string expectedHash)
    {
        // Arrange
        var snapshot = angleDegrees == 0.0
            ? FoundationTestData.StandardSnapshot(length: 3150.0, width: 2240.0, thickness: thickness)
            : FoundationTestData.OrientedSnapshot(angleDegrees, length: 3150.0, width: 2240.0, thickness: thickness);
        var spec = FoundationTestData.StandardSpec(
            spacingBottomX: 140.0,
            spacingBottomY: 160.0,
            spacingTopX: 190.0,
            spacingTopY: 210.0,
            diameterBottomX: 16.0,
            diameterBottomY: 14.0,
            diameterTopX: 12.0,
            diameterTopY: 10.0,
            isTopMatEnabled: topMat,
            hookType: hooks ? FoundationHookType.Hook90Degrees : FoundationHookType.None,
            hookLength: hookLength);

        // Act
        var result = FoundationMeshCalculator.Calculate(snapshot, spec, equalSpacing);

        // Assert
        Assert.Equal(expectedHash, $"{scenario}:{CharacterizationText.Hash(result)}");
    }
}
