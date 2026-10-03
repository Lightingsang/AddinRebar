using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
    [InlineData("axis-no-hooks", 0.0, false, false, false, "axis-no-hooks:765E8C9C81BAC96B3474E924")]
    [InlineData("axis-hooks-top", 0.0, true, true, false, "axis-hooks-top:D40AD556727EEF62342E4AB8")]
    [InlineData("rotated-hooks-bottom-only", 30.0, true, false, false, "rotated-hooks-bottom-only:63CFD51F085F46BDBFB6C276")]
    [InlineData("rotated-equal-spacing", 47.5, true, true, true, "rotated-equal-spacing:3BD4720D257AAA5176F87650")]
    public void Calculate_FixedInput_ProducesTheRecordedMesh(
        string scenario, double angleDegrees, bool hooks, bool topMat, bool equalSpacing, string expectedHash)
    {
        // Arrange
        var snapshot = angleDegrees == 0.0
            ? FoundationTestData.StandardSnapshot(length: 3150.0, width: 2240.0, thickness: 600.0)
            : FoundationTestData.OrientedSnapshot(angleDegrees, length: 3150.0, width: 2240.0, thickness: 600.0);
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
            hookType: hooks ? FoundationHookType.Hook90Degrees : FoundationHookType.None);

        // Act
        var result = FoundationMeshCalculator.Calculate(snapshot, spec, equalSpacing);

        // Assert
        Assert.Equal(expectedHash, $"{scenario}:{Hash(Describe(result))}");
    }

    private static string Describe(FoundationMeshResult result)
    {
        var text = new StringBuilder();
        foreach (var bar in result.Bars)
        {
            text.Append(bar.BarIndex).Append('|').Append(bar.Layer).Append('|').Append(bar.LayerName).Append('|')
                .Append(Round(bar.Diameter)).Append('|').Append(bar.HookType).Append('|').Append(Round(bar.HookLength))
                .Append("|W:");
            foreach (var point in bar.Polyline.Points)
            {
                text.Append(Round(point.X)).Append(',').Append(Round(point.Y)).Append(',').Append(Round(point.Z)).Append(';');
            }

            text.Append("|L:");
            foreach (var point in bar.LocalPolyline.Points)
            {
                text.Append(Round(point.X)).Append(',').Append(Round(point.Y)).Append(',').Append(Round(point.Z)).Append(';');
            }

            text.Append('\n');
        }

        var stats = result.Statistics;
        text.Append(stats.TotalBarCount).Append('|').Append(stats.BottomBarCountX).Append('|').Append(stats.BottomBarCountY)
            .Append('|').Append(stats.TopBarCountX).Append('|').Append(stats.TopBarCountY)
            .Append('|').Append(Round(stats.BottomLengthMmX)).Append('|').Append(Round(stats.BottomLengthMmY))
            .Append('|').Append(Round(stats.TopLengthMmX)).Append('|').Append(Round(stats.TopLengthMmY))
            .Append('|').Append(Round(stats.TotalLengthMm)).Append('|').Append(Round(stats.EstimatedWeightKg))
            .Append('|').Append(result.BottomBarsX.Count).Append('|').Append(result.BottomBarsY.Count)
            .Append('|').Append(result.TopBarsX.Count).Append('|').Append(result.TopBarsY.Count);
        return text.ToString();
    }

    private static string Round(double value) => Math.Round(value, 6).ToString("0.######", CultureInfo.InvariantCulture);

    private static string Hash(string text)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(bytes, 0, 12).Replace("-", string.Empty);
    }
}
