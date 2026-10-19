using System.Collections.Generic;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.Core.Shapes;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for coupler thread deductions, sleeve lengths, and note parsing bounds.
/// </summary>
public sealed class Tier2_CouplerDeductionBoundsTests
{
    [Fact]
    public void CouplerSleeve_BarShorterThanSleeve_MaintainsPositiveNominalLength()
    {
        // Arrange: 50mm bar with 80mm sleeve
        double sleeveLen = RebarShapeCatalog.CalculateCuttingLength(
            RebarShapeType.DangCoupler, [], 0.0, 25, false);

        // Assert: standard sleeve length 80mm
        Assert.Equal(80.0, sleeveLen);
    }

    [Fact]
    public void CouplerSleeve_CustomLengthParameter_OverridesDefault()
    {
        // Arrange: custom 120mm sleeve
        double sleeveLen = RebarShapeCatalog.CalculateCuttingLength(
            RebarShapeType.DangCoupler, [120.0], 0.0, 32, false);

        // Assert
        Assert.Equal(120.0, sleeveLen);
    }

    [Fact]
    public void ExtractCouplers_CaseInsensitiveNoteMatching_DetectsAllVariants()
    {
        // Arrange: uppercase, lowercase, mixed case notes
        var items = new List<RebarItem>
        {
            new() { ComponentMark = "C1", DiameterMm = 25, CountPerMember = 2, MemberCount = 1, Notes = "COUPLER D25" },
            new() { ComponentMark = "C1", DiameterMm = 25, CountPerMember = 2, MemberCount = 1, Notes = "coupler D25" },
            new() { ComponentMark = "C2", DiameterMm = 28, CountPerMember = 4, MemberCount = 1, Notes = "Đầu tiện ren ngoài" }
        };

        // Act
        var couplers = RebarScheduleAggregator.ExtractCouplers(items);

        // Assert: 3 distinct coupler groups preserved
        Assert.Equal(3, couplers.Count);
    }

    [Fact]
    public void ExtractCouplers_MultiMemberMultiplication_CalculatesTotalCouplers()
    {
        // Arrange: 5 columns, each with 8 couplers
        var items = new List<RebarItem>
        {
            new() { ComponentMark = "C1..C5", DiameterMm = 32, CountPerMember = 8, MemberCount = 5, Notes = "Coupler D32" }
        };

        // Act
        var couplers = RebarScheduleAggregator.ExtractCouplers(items);

        // Assert: 5 members * 8 per member = 40 total couplers
        Assert.Single(couplers);
        Assert.Equal(40, couplers[0].TotalCouplers);
    }

    [Fact]
    public void ExtractCouplers_WithoutMatchingKeywords_ReturnsEmptyList()
    {
        // Arrange
        var items = new List<RebarItem>
        {
            new() { ComponentMark = "B1", DiameterMm = 20, CountPerMember = 4, MemberCount = 1, Notes = "Thép thường" },
            new() { ComponentMark = "B1", DiameterMm = 20, CountPerMember = 4, MemberCount = 1, Notes = "" }
        };

        // Act
        var couplers = RebarScheduleAggregator.ExtractCouplers(items);

        // Assert
        Assert.Empty(couplers);
    }

    [Fact]
    public void CouplerSummaryItem_DefaultMemberCount_IsOne()
    {
        // Arrange & Act
        var coupler = new CouplerSummaryItem
        {
            CouplersPerMember = 6
        };

        // Assert: 1 * 6 = 6
        Assert.Equal(1, coupler.MemberCount);
        Assert.Equal(6, coupler.TotalCouplers);
    }
}
