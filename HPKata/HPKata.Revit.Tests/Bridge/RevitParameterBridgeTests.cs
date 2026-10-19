using HPKata.Core.Models;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Bridge;
using Xunit;

namespace HPKata.Revit.Tests.Bridge;

public sealed class RevitParameterBridgeTests
{
    [Fact]
    public void ToRebarItem_MapsPartitionToComponentMark()
    {
        var dto = new RevitRebarDto
        {
            Partition = "Dam_D1",
            Mark = "3",
            Quantity = 2,
            DiameterMm = 20,
            SingleBarLengthMm = 3500
        };

        var item = RevitParameterBridge.ToRebarItem(dto);
        Assert.Equal("Dam_D1", item.ComponentMark);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ToRebarItem_DefaultPartition_WhenEmptyOrNull(string? partition)
    {
        var dto = new RevitRebarDto
        {
            Partition = partition!,
            Mark = "1",
            Quantity = 1,
            DiameterMm = 16,
            SingleBarLengthMm = 2000
        };

        var item = RevitParameterBridge.ToRebarItem(dto);
        Assert.Equal("CK", item.ComponentMark);
    }

    [Fact]
    public void ToRebarItem_MapsBarMark()
    {
        var dto = new RevitRebarDto
        {
            Mark = "12",
            Quantity = 4,
            DiameterMm = 18,
            SingleBarLengthMm = 4500
        };

        var item = RevitParameterBridge.ToRebarItem(dto);
        Assert.Equal("12", item.BarMark);
    }

    [Fact]
    public void ToRebarItem_SingleBarCuttingLength_NeverTotalLength()
    {
        // 4 bars in set, each 2500 mm.
        var dto = new RevitRebarDto
        {
            Quantity = 4,
            SingleBarLengthMm = 2500.0,
            DiameterMm = 20.0
        };

        var item = RevitParameterBridge.ToRebarItem(dto);

        // Single bar cutting length must be 2500 mm, NOT 10000 mm!
        Assert.Equal(2500.0, item.CuttingLengthMm);
        Assert.Equal(4, item.TotalCount);
        // TotalLengthM = (2500 * 4) / 1000 = 10.0 m
        Assert.Equal(10.0, item.TotalLengthM, 3);
    }

    [Fact]
    public void ToRebarItem_CalculatesUnitMassAndWeight()
    {
        var dto = new RevitRebarDto
        {
            Quantity = 2,
            DiameterMm = 16.0,
            SingleBarLengthMm = 2000.0 // 2 * 2m = 4m total
        };

        var item = RevitParameterBridge.ToRebarItem(dto);

        // For D16, TCVN unit mass ~ 1.578 kg/m
        Assert.True(item.UnitMassKgPerM > 1.5 && item.UnitMassKgPerM < 1.6);
        // Total weight = 4.0m * 1.578 = ~6.31 kg
        Assert.True(item.TotalWeightKg > 6.0 && item.TotalWeightKg < 6.5);
    }

    [Fact]
    public void ToRebarDto_RoundtripsProperties()
    {
        var item = new RebarItem
        {
            ComponentMark = "COT_C1",
            BarMark = "7",
            ShapeCode = "dang02",
            DiameterMm = 25,
            CountPerMember = 6,
            CuttingLengthMm = 4200.0,
            Notes = "Thép chủ"
        };

        var dto = RevitParameterBridge.ToRebarDto(item, 101L);

        Assert.Equal(101L, dto.ElementId);
        Assert.Equal("COT_C1", dto.Partition);
        Assert.Equal("7", dto.Mark);
        Assert.Equal(6, dto.Quantity);
        Assert.Equal(25.0, dto.DiameterMm);
        Assert.Equal(4200.0, dto.SingleBarLengthMm);
        Assert.Equal("M_T1", dto.ShapeName);
        Assert.Equal("Thép chủ", dto.Comments);
    }
}
