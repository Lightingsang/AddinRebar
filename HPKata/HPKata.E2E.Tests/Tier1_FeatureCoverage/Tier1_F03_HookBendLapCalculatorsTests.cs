using System;
using HPKata.Core.Calculators;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 03: Hook lengths, bend deductions, lap splices, and unit weights per TCVN 5574 / 1651.
/// </summary>
public sealed class Tier1_F03_HookBendLapCalculatorsTests
{
    [Fact]
    public void HookLengths_90_135_180_AdhereToMinimumAndMultipliers()
    {
        // Arrange
        int dia20 = 20;

        // Act
        double hook90 = RebarCalculator.Hook90Length(dia20);
        double hook135 = RebarCalculator.Hook135Length(dia20);
        double hook180 = RebarCalculator.Hook180Length(dia20);

        // Assert: 90 => 10*d = 200; 135 => 7.5*d = 150; 180 => 9*d = 180
        Assert.Equal(200.0, hook90);
        Assert.Equal(150.0, hook135);
        Assert.Equal(180.0, hook180);
    }

    [Fact]
    public void HookLengths_SmallDiameter_ClampsTo75MmMinimum()
    {
        // Arrange
        int dia6 = 6;

        // Act
        double hook90 = RebarCalculator.Hook90Length(dia6);
        double hook135 = RebarCalculator.Hook135Length(dia6);

        // Assert: 10*6=60 < 75 => 75mm minimum
        Assert.Equal(75.0, hook90);
        Assert.Equal(75.0, hook135);
    }

    [Fact]
    public void BendDeduction_90Degree_MatchesStandardBendElongation()
    {
        // Arrange
        int dia16 = 16;

        // Act
        double deduction = RebarCalculator.BendDeduction(dia16, 90.0, true);

        // Assert: Bend deduction for 90 deg deformed bar is positive
        Assert.True(deduction > 0.0);
    }

    [Fact]
    public void LapSpliceAndAnchorage_ComputesAccordingToDesignMultipliers()
    {
        // Arrange
        int dia25 = 25;

        // Act
        double lap40d = RebarCalculator.LapSpliceLength(dia25, 40.0);
        double anchor35d = RebarCalculator.TensionAnchorageLength(dia25, 35.0);

        // Assert
        Assert.Equal(1000.0, lap40d);
        Assert.Equal(875.0, anchor35d);
    }

    [Fact]
    public void UnitMass_MatchesStandardTcvnTable()
    {
        // Arrange & Act & Assert
        Assert.Equal(0.617, RebarCalculator.GetUnitMass(10), 3);
        Assert.Equal(1.580, RebarCalculator.GetUnitMass(16), 3);
        Assert.Equal(2.470, RebarCalculator.GetUnitMass(20), 3);
        Assert.Equal(3.850, RebarCalculator.GetUnitMass(25), 3);

        double weight = RebarCalculator.CalculateWeight(10.0, 20); // 10m of D20
        Assert.Equal(24.70, weight, 2);
    }
}
