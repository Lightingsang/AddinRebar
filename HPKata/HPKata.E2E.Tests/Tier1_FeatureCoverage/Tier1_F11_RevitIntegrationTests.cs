using HPKata.E2E.Tests.Common;
using HPKata.Revit.Bridge;
using HPKata.Revit.Runners;
using HPKata.Revit.Units;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 11: Revit Rebar parameter bridging, shape adapting, schedule table creation, and Rule R5 unit quarantine.
/// </summary>
public sealed class Tier1_F11_RevitIntegrationTests
{
    [Fact]
    public void RevitParameterBridge_ToRebarItem_PreservesGeometricParameters()
    {
        // Arrange
        var dto = E2EDataFactory.CreateRevitRebarDto(
            101, "Col_C1", "1", "M_00", 25, 4, 4500.0, [4500.0]);

        // Act
        var item = RevitParameterBridge.ToRebarItem(dto);

        // Assert
        Assert.Equal("Col_C1", item.ComponentMark);
        Assert.Equal("1", item.BarMark);
        Assert.Equal(25, item.DiameterMm);
        Assert.Equal(4, item.CountPerMember);
        Assert.Equal(4500.0, item.CuttingLengthMm);
        Assert.Equal("dang01", item.ShapeCode);
    }

    [Fact]
    public void RevitParameterBridge_ToRebarDto_PreservesHostAgnosticValues()
    {
        // Arrange
        var item = new HPKata.Core.Models.RebarItem
        {
            ComponentMark = "Beam_B1",
            BarMark = "2",
            ShapeCode = "02",
            DiameterMm = 20,
            CountPerMember = 3,
            CuttingLengthMm = 6200.0,
            ParametersMm = [500.0, 5200.0, 500.0]
        };

        // Act
        var dto = RevitParameterBridge.ToRebarDto(item, 5001);

        // Assert
        Assert.Equal(5001, dto.ElementId);
        Assert.Equal("Beam_B1", dto.Partition);
        Assert.Equal("2", dto.Mark);
        Assert.Equal(20, dto.DiameterMm);
        Assert.Equal(6200.0, dto.SingleBarLengthMm);
    }

    [Fact]
    public void RevitScheduleRunner_Execute_CreatesScheduleTableInContext()
    {
        // Arrange
        var context = new E2ERevitContext();
        context.AllRebars.Add(E2EDataFactory.CreateRevitRebarDto(
            1, "B1", "1", "RebarShape 00", 20, 2, 5000.0));
        context.AllRebars.Add(E2EDataFactory.CreateRevitRebarDto(
            2, "B1", "2", "RebarShape 21", 8, 25, 1400.0));

        // Act
        var table = RevitScheduleRunner.Execute(context);

        // Assert
        Assert.NotNull(table);
        Assert.Single(context.CreatedTables);
        Assert.Equal(2, table.Rows.Count);
        Assert.Contains(context.Notifications, n => n.Contains("thống kê"));
    }

    [Fact]
    public void RevitCuttingStockRunner_Execute_SolvesOptimizationAcrossAllDiameters()
    {
        // Arrange
        var context = new E2ERevitContext();
        // D20: 3 bars of 3900mm (11700 exact)
        context.AllRebars.Add(E2EDataFactory.CreateRevitRebarDto(
            1, "B1", "1", "RebarShape 00", 20, 3, 3900.0));

        // Act
        var results = RevitCuttingStockRunner.Execute(context, 11700.0);

        // Assert
        Assert.NotEmpty(results);
        Assert.Single(context.CreatedTables);
        Assert.Equal(1, results[0].NewStandardBarsCount);
        Assert.Equal(0.0, results[0].GrossWastePercent, 2);
    }

    [Fact]
    public void RevitUnitConverter_RuleR5_QuarantinesFootToMmConversions()
    {
        // Arrange
        double feet = 10.0;
        double mm = 3048.0;

        // Act & Assert
        Assert.Equal(mm, RevitUnitConverter.FeetToMm(feet), 4);
        Assert.Equal(feet, RevitUnitConverter.MmToFeet(mm), 4);
        var coords = RevitUnitConverter.CoordinatesFeetToMm(1.0, 2.0, 3.0);
        Assert.Equal(304.8, coords.X, 4);
        Assert.Equal(609.6, coords.Y, 4);
        Assert.Equal(914.4, coords.Z, 4);
    }
}
