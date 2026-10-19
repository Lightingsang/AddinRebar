using System.Collections.Generic;
using HPKata.AutoCad.Commands;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.Core.Shapes;
using HPKata.E2E.Tests.Common;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 07: Coupler tracking, sleeve deductions, coupler scheduling, and CAD summary tables.
/// </summary>
public sealed class Tier1_F07_CouplerTrackingTests
{
    [Fact]
    public void ExtractCouplers_IdentifiesItemsWithCouplerNotes()
    {
        // Arrange
        var items = new List<RebarItem>
        {
            new() { ComponentMark = "C1", DiameterMm = 25, CountPerMember = 4, MemberCount = 2, Notes = "Nối coupler D25" },
            new() { ComponentMark = "C1", DiameterMm = 25, CountPerMember = 4, MemberCount = 2, Notes = "Thẳng không nối" },
            new() { ComponentMark = "C2", DiameterMm = 28, CountPerMember = 6, MemberCount = 1, Notes = "Mối nối ren" }
        };

        // Act
        var couplers = RebarScheduleAggregator.ExtractCouplers(items);

        // Assert: 2 coupler groups (C1 D25, C2 D28)
        Assert.Equal(2, couplers.Count);
        var c1 = couplers.Find(c => c.DiameterMm == 25);
        Assert.NotNull(c1);
        Assert.Equal(8, c1.TotalCouplers); // 4 per member * 2 members
    }

    [Fact]
    public void MechanicalCouplerShape_ReturnsStandardSleeveLength()
    {
        // Arrange
        var shape = RebarShapeType.DangCoupler;
        double[] parameters = [90.0];

        // Act
        double length = RebarShapeCatalog.CalculateCuttingLength(shape, parameters, 0, 25, false);

        // Assert
        Assert.Equal(90.0, length);
    }

    [Fact]
    public void MechanicalCouplerShape_WithDefaultZeroParameter_Uses80MmStandard()
    {
        // Arrange
        var shape = RebarShapeType.DangCoupler;
        double[] parameters = [];

        // Act
        double length = RebarShapeCatalog.CalculateCuttingLength(shape, parameters, 0, 25, false);

        // Assert: 80mm standard fallback
        Assert.Equal(80.0, length);
    }

    [Fact]
    public void ExecuteCouplerSummary_WithCouplerItems_InsertsSummaryTable()
    {
        // Arrange
        var context = new E2ECadContext();
        context.Entities.Add(E2EDataFactory.CreateCadRebarEntity(
            "H_COL1", "01", 1, 32, "8", 2, [4000.0], "COL_T1"));
        context.Entities[0].XDataRecord.ChuThich = "Nối coupler D32";

        // Act
        var table = SummaryCommandRunner.ExecuteCouplerSummary(context);

        // Assert
        Assert.NotNull(table);
        Assert.Single(context.InsertedTables);
        Assert.Contains("COUPLER", table.Title);
        Assert.Contains("COL_T1", table.Rows[0].Cells[1]);
        Assert.Contains("32", table.Rows[0].Cells[2]);
    }

    [Fact]
    public void ExecuteCouplerSummary_WithoutCouplers_ReportsZeroGracefully()
    {
        // Arrange
        var context = new E2ECadContext();
        context.Entities.Add(E2EDataFactory.CreateCadRebarEntity(
            "H_BEAM", "01", 1, 16, "2", 1, [3000.0], "B1"));

        // Act
        var table = SummaryCommandRunner.ExecuteCouplerSummary(context);

        // Assert
        Assert.Contains("0", table.Title);
        Assert.Empty(context.InsertedTables);
        Assert.Contains(context.Messages, m => m.Contains("Không tìm thấy cốt thép nối ren"));
    }
}
