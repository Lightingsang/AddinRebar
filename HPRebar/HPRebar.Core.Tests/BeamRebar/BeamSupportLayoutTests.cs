using System;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamSupportLayoutTests
{
    private static readonly BeamPieceExtent[] TwoSpans = { new(0, 6000), new(6000, 12000) };

    [Fact]
    public void Arrange_ThreeColumnsUnderTwoSpans_ClassesTheEndsExteriorAndTheMiddleInterior()
    {
        // Arrange
        var measured = new[] { Column(0, "c0"), Column(6000, "c1"), Column(12000, "c2") };

        // Act
        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        // Assert
        Assert.Equal(
            new[] { SupportType.ExteriorColumn, SupportType.InteriorColumn, SupportType.ExteriorColumn },
            nodes.Select(n => n.Type));
        Assert.Equal(new[] { "Support 1", "Support 2", "Support 3" }, nodes.Select(n => n.Name));
        Assert.Equal(new[] { "c0", "c1", "c2" }, nodes.Select(n => n.ElementUniqueId));
        Assert.Equal(new[] { true, false, true }, nodes.Select(n => n.IsExterior));
        Assert.Equal(new[] { 0, 1, 2 }, nodes.Select(n => n.Index));
    }

    [Fact]
    public void Arrange_SupportsFoundOutOfOrder_AreSortedAlongTheRun()
    {
        var measured = new[] { Column(12000, "c2"), Column(0, "c0"), Column(6000, "c1") };

        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        Assert.Equal(new[] { 0.0, 6000.0, 12000.0 }, nodes.Select(n => n.CenterX));
    }

    [Fact]
    public void Arrange_SameColumnFoundTwiceAFewMillimetresApart_KeepsTheOneNearerTheStart()
    {
        var measured = new[] { Column(0, "c0"), Column(6030, "late"), Column(6010, "early"), Column(12000, "c2") };

        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        Assert.Equal(new[] { "c0", "early", "c2" }, nodes.Select(n => n.ElementUniqueId));
    }

    [Fact]
    public void Arrange_TwoFindsTwentyMillimetresApartAcrossAHundredMillimetreMark_AreOneSupport()
    {
        var measured = new[] { Column(0, "c0"), Column(6040, "a"), Column(6060, "b"), Column(12000, "c2") };

        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        Assert.Equal(new[] { "c0", "a", "c2" }, nodes.Select(n => n.ElementUniqueId));
    }

    [Theory]
    [InlineData(6099.9, "c0,a,c2")]     // 99.9 mm apart: one support, the start-most kept
    [InlineData(6100.0, "c0,a,b,c2")]   // 100 mm apart: two supports
    public void Arrange_FindsAtTheMergeDistance_MergeOnlyBelowIt(double secondCenter, string expectedIds)
    {
        var measured = new[] { Column(0, "c0"), Column(6000, "a"), Column(secondCenter, "b"), Column(12000, "c2") };

        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        Assert.Equal(expectedIds.Split(','), nodes.Select(n => n.ElementUniqueId));
    }

    [Fact]
    public void Arrange_FindsEitherSideOfTheRunOrigin_MergeTheSameWayAsElsewhere()
    {
        var measured = new[] { Column(-30, "left"), Column(30, "right"), Column(6000, "c1"), Column(12000, "c2") };

        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        Assert.Equal(new[] { "left", "c1", "c2" }, nodes.Select(n => n.ElementUniqueId));
    }

    [Fact]
    public void Arrange_RowOfCloseFinds_MeasuresFromTheKeptSupportSoTheRowDoesNotChain()
    {
        // Arrange: 60 mm steps; 6060 is within 100 mm of 6000, 6120 is not
        var measured = new[]
        {
            Column(0, "c0"), Column(6000, "a"), Column(6060, "b"), Column(6120, "c"), Column(12000, "c2")
        };

        // Act
        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        // Assert
        Assert.Equal(new[] { "c0", "a", "c", "c2" }, nodes.Select(n => n.ElementUniqueId));
    }

    [Fact]
    public void Arrange_RunOverhangingTheFirstColumn_StartsWithACantileverTip()
    {
        // Arrange: run starts at 0, the first column face is at 1800
        var measured = new[] { Column(2000, "c1"), Column(8000, "c2") };
        var pieces = new[] { new BeamPieceExtent(0, 8000) };

        // Act
        var nodes = BeamSupportLayout.Arrange(measured, pieces);

        // Assert
        Assert.Equal(3, nodes.Count);
        Assert.Equal(SupportType.CantileverEnd, nodes[0].Type);
        Assert.Equal("Tip 1", nodes[0].Name);
        Assert.Equal(0.0, nodes[0].CenterX);
        Assert.Equal(0.0, nodes[0].Width);
        Assert.Equal(SupportType.InteriorColumn, nodes[1].Type);
        Assert.Equal(SupportType.ExteriorColumn, nodes[2].Type);
    }

    [Fact]
    public void Arrange_RunOverhangingTheLastColumn_EndsWithACantileverTip()
    {
        var measured = new[] { Column(0, "c0"), Column(6000, "c1") };
        var pieces = new[] { new BeamPieceExtent(0, 8000) };

        var nodes = BeamSupportLayout.Arrange(measured, pieces);

        Assert.Equal(SupportType.CantileverEnd, nodes[nodes.Count - 1].Type);
        Assert.Equal("Tip 3", nodes[nodes.Count - 1].Name);
        Assert.Equal(8000.0, nodes[nodes.Count - 1].CenterX);
    }

    [Theory]
    [InlineData(400.0, false)]   // face 200 mm past the run start: not a cantilever
    [InlineData(400.1, true)]
    public void Arrange_OverhangAtTheThreshold_AddsATipOnlyBeyondIt(double firstColumnCenter, bool expectTip)
    {
        var measured = new[] { Column(firstColumnCenter, "c1"), Column(6000, "c2") };
        var pieces = new[] { new BeamPieceExtent(0, 6000) };

        var nodes = BeamSupportLayout.Arrange(measured, pieces);

        Assert.Equal(expectTip, nodes[0].Type == SupportType.CantileverEnd);
    }

    [Fact]
    public void Arrange_NothingFoundUnderAJoint_AddsAStandInColumnClassedInterior()
    {
        var measured = new[] { Column(0, "c0"), Column(12000, "c2") };

        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        Assert.Equal(3, nodes.Count);
        Assert.Equal(6000.0, nodes[1].CenterX);
        Assert.Equal(300.0, nodes[1].Width);
        Assert.Equal(300.0, nodes[1].Depth);
        Assert.Equal(SupportType.InteriorColumn, nodes[1].Type);
        Assert.Equal(string.Empty, nodes[1].ElementUniqueId);
    }

    [Fact]
    public void Arrange_JointWithASupportWithin300Millimetres_GetsNoStandIn()
    {
        // Arrange: three spans, a column 250 mm from the joint at 6000, nothing near the joint at 12000
        var measured = new[] { Column(0, "c0"), Column(6250, "c1"), Column(18000, "c3") };
        var pieces = new BeamPieceExtent[] { new(0, 6000), new(6000, 12000), new(12000, 18000) };

        // Act
        var nodes = BeamSupportLayout.Arrange(measured, pieces);

        // Assert
        Assert.Equal(new[] { 0.0, 6250.0, 12000.0, 18000.0 }, nodes.Select(n => n.CenterX));
    }

    [Fact]
    public void Arrange_EnoughNodesAlready_AddsNoStandInEvenWhereAJointIsBare()
    {
        // Arrange: three nodes for two spans, none near the joint at 6000
        var measured = new[] { Column(0, "c0"), Column(9000, "c1"), Column(12000, "c2") };

        // Act
        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        // Assert
        Assert.Equal(3, nodes.Count);
    }

    [Fact]
    public void Arrange_WallsAndGirders_KeepTheirTypeAtEndsAndInBetween()
    {
        var measured = new[]
        {
            Measured(0, SupportType.Wall), Measured(6000, SupportType.Girder), Measured(12000, SupportType.Girder)
        };

        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        Assert.Equal(new[] { SupportType.Wall, SupportType.Girder, SupportType.Girder }, nodes.Select(n => n.Type));
    }

    [Fact]
    public void Arrange_ExteriorColumnBetweenTheEnds_IsClassedInterior()
    {
        var measured = new[] { Column(0, "c0"), Measured(6000, SupportType.ExteriorColumn), Column(12000, "c2") };

        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        Assert.Equal(SupportType.InteriorColumn, nodes[1].Type);
        Assert.False(nodes[1].IsExterior);
    }

    [Fact]
    public void Arrange_NoSupportMeasured_Throws()
    {
        Assert.Throws<ArgumentException>(() => BeamSupportLayout.Arrange(Array.Empty<MeasuredSupport>(), TwoSpans));
    }

    [Fact]
    public void Arrange_NoPiece_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => BeamSupportLayout.Arrange(new[] { Column(0, "c0") }, Array.Empty<BeamPieceExtent>()));
    }

    [Fact]
    public void Synthesize_TwoBeams_PlacesStandInColumnsAtTheStartAndAfterEachBeam()
    {
        var nodes = BeamSupportLayout.Synthesize(new[] { 6000.0, 5000.0 });

        Assert.Equal(new[] { 0.0, 6000.0, 11000.0 }, nodes.Select(n => n.CenterX));
        Assert.Equal(
            new[] { SupportType.ExteriorColumn, SupportType.Column, SupportType.ExteriorColumn },
            nodes.Select(n => n.Type));
        Assert.Equal(new[] { true, false, true }, nodes.Select(n => n.IsExterior));
        Assert.Equal(new[] { "Support 1", "Support 2", "Support 3" }, nodes.Select(n => n.Name));
        Assert.All(nodes, n => Assert.Equal(300.0, n.Width));
    }

    [Fact]
    public void Synthesize_NoBeams_ReturnsNoNodes()
    {
        Assert.Empty(BeamSupportLayout.Synthesize(Array.Empty<double>()));
    }

    [Fact]
    public void Arrange_InteriorColumnAtAnEnd_IsClassedExterior()
    {
        var measured = new[] { Measured(0, SupportType.InteriorColumn), Column(6000, "c1"), Column(12000, "c2") };

        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        Assert.Equal(SupportType.ExteriorColumn, nodes[0].Type);
    }

    [Theory]
    [InlineData(5600.0, false)]   // face 200 mm short of the run end: not a cantilever
    [InlineData(5599.9, true)]
    public void Arrange_OverhangPastTheLastColumnAtTheThreshold_AddsATipOnlyBeyondIt(
        double lastColumnCenter, bool expectTip)
    {
        var measured = new[] { Column(0, "c0"), Column(lastColumnCenter, "c1") };
        var pieces = new[] { new BeamPieceExtent(0, 6000) };

        var nodes = BeamSupportLayout.Arrange(measured, pieces);

        Assert.Equal(expectTip, nodes[nodes.Count - 1].Type == SupportType.CantileverEnd);
    }

    [Fact]
    public void Arrange_TwoFindsAtTheSameCentre_KeepsTheFirstFound()
    {
        var measured = new[] { Column(0, "c0"), Column(6000, "first"), Column(6000, "second"), Column(12000, "c2") };

        var nodes = BeamSupportLayout.Arrange(measured, TwoSpans);

        Assert.Equal("first", nodes[1].ElementUniqueId);
    }

    [Fact]
    public void Arrange_PiecesGivenOutOfOrder_FindsTheSameJointsAndEnds()
    {
        var measured = new[] { Column(0, "c0"), Column(12000, "c2") };
        var pieces = new[] { new BeamPieceExtent(6000, 12000), new BeamPieceExtent(0, 6000) };

        var nodes = BeamSupportLayout.Arrange(measured, pieces);

        Assert.Equal(new[] { 0.0, 6000.0, 12000.0 }, nodes.Select(n => n.CenterX));
    }

    [Fact]
    public void Arrange_EnoughNodesReachedPartWay_LeavesTheLaterBareJointWithoutAStandIn()
    {
        // Arrange: three spans need four nodes; a cantilever tip and the stand-in at 6000 complete them first
        var measured = new[] { Column(1000, "c0"), Column(18000, "c3") };
        var pieces = new BeamPieceExtent[] { new(0, 6000), new(6000, 12000), new(12000, 18000) };

        // Act
        var nodes = BeamSupportLayout.Arrange(measured, pieces);

        // Assert
        Assert.Equal(new[] { 0.0, 1000.0, 6000.0, 18000.0 }, nodes.Select(n => n.CenterX));
    }

    [Fact]
    public void Arrange_FoundSupports_CarryTheirWidthAndDepth()
    {
        var measured = new[] { new MeasuredSupport(0, 450, 350, SupportType.Column, "c0"), Column(6000, "c1") };
        var pieces = new[] { new BeamPieceExtent(0, 6000) };

        var nodes = BeamSupportLayout.Arrange(measured, pieces);

        Assert.Equal(450.0, nodes[0].Width);
        Assert.Equal(350.0, nodes[0].Depth);
    }

    [Fact]
    public void Arrange_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => BeamSupportLayout.Arrange(null!, TwoSpans));
        Assert.Throws<ArgumentNullException>(() => BeamSupportLayout.Arrange(new[] { Column(0, "c0") }, null!));
        Assert.Throws<ArgumentNullException>(() => BeamSupportLayout.Synthesize(null!));
    }

    [Fact]
    public void Synthesize_StandIns_AreSquareUnlinkedAndIndexedInOrder()
    {
        var nodes = BeamSupportLayout.Synthesize(new[] { 6000.0, 5000.0 });

        Assert.Equal(new[] { 0, 1, 2 }, nodes.Select(n => n.Index));
        Assert.All(nodes, n => Assert.Equal(300.0, n.Depth));
        Assert.All(nodes, n => Assert.Equal(string.Empty, n.ElementUniqueId));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Arrange_SupportCentreNotAFiniteNumber_Throws(double centerX)
    {
        var measured = new[] { Column(0, "c0"), Column(centerX, "bad"), Column(12000, "c2") };

        var error = Assert.Throws<ArgumentException>(() => BeamSupportLayout.Arrange(measured, TwoSpans));

        Assert.Equal("measured", error.ParamName);
    }

    private static MeasuredSupport Column(double centerX, string id) => new(centerX, 400, 400, SupportType.Column, id);

    private static MeasuredSupport Measured(double centerX, SupportType type) =>
        new(centerX, 400, 300, type, string.Empty);
}
