using System;
using HPKata.Core.Calculators;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for commercial extreme diameters (D6 to D50) and unlisted diameter formula fallbacks.
/// </summary>
public sealed class Tier2_ExtremeDiametersTests
{
    [Fact]
    public void SmallestCommercialBar_D6_CalculatesExactTcvnUnitMass()
    {
        // Act
        double massD6 = RebarCalculator.GetUnitMass(6);

        // Assert: standard 0.222 kg/m
        Assert.Equal(0.222, massD6);
    }

    [Fact]
    public void LargestCommercialBar_D50_CalculatesExactTcvnUnitMass()
    {
        // Act
        double massD50 = RebarCalculator.GetUnitMass(50);

        // Assert: standard 15.420 kg/m
        Assert.Equal(15.420, massD50);
    }

    [Fact]
    public void IntermediateStandardDiameters_AllMatchTcvnTable()
    {
        // Act & Assert
        Assert.Equal(0.395, RebarCalculator.GetUnitMass(8));
        Assert.Equal(0.617, RebarCalculator.GetUnitMass(10));
        Assert.Equal(0.888, RebarCalculator.GetUnitMass(12));
        Assert.Equal(1.580, RebarCalculator.GetUnitMass(16));
        Assert.Equal(2.470, RebarCalculator.GetUnitMass(20));
        Assert.Equal(3.850, RebarCalculator.GetUnitMass(25));
        Assert.Equal(6.310, RebarCalculator.GetUnitMass(32));
        Assert.Equal(7.990, RebarCalculator.GetUnitMass(36));
        Assert.Equal(9.860, RebarCalculator.GetUnitMass(40));
    }

    [Fact]
    public void UnlistedDiameter_D45_UsesStandardFormulaFallback()
    {
        // Act: (45^2) / 162.2 = 2025 / 162.2 approx 12.4845
        double massD45 = RebarCalculator.GetUnitMass(45);
        double expected = (45.0 * 45.0) / 162.2;

        // Assert
        Assert.Equal(expected, massD45, 4);
        Assert.True(massD45 > 12.0 && massD45 < 13.0);
    }

    [Fact]
    public void LargeDiameter_D50_HookAndLapComputesProportionally()
    {
        // Act: D50 hook90 = 10*50 = 500mm; lap40d = 40*50 = 2000mm
        double hook90 = RebarCalculator.Hook90Length(50);
        double lap40d = RebarCalculator.LapSpliceLength(50, 40.0);

        // Assert
        Assert.Equal(500.0, hook90);
        Assert.Equal(2000.0, lap40d);
    }

    [Fact]
    public void BendDeduction_LargeDiameterD40_ScalesAppropriately()
    {
        // Act
        double deductionD40 = RebarCalculator.BendDeduction(40, 90.0, true);
        double deductionD16 = RebarCalculator.BendDeduction(16, 90.0, true);

        // Assert: deduction increases with diameter
        Assert.True(deductionD40 > deductionD16);
        Assert.True(deductionD40 > 50.0);
    }
}
