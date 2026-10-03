using System;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamSpanAssemblyTests
{
    private static readonly BeamPieceSection Section =
        new(Width: 300, Height: 600, TopElevation: 3500, ElementUniqueId: "beam-1");

    [Fact]
    public void Between_TwoColumns_SpansFaceToFace()
    {
        // Arrange
        var supports = new[] { Column(0, 0, 400), Column(1, 6000, 400) };

        // Act
        var span = BeamSpanAssembly.Between(0, supports, Section);

        // Assert
        Assert.Equal(6000.0, span.LengthCenter);
        Assert.Equal(5600.0, span.LengthClear);
        Assert.Equal(200.0, span.StartX);
        Assert.Equal(CantileverPosition.None, span.Cantilever);
        Assert.Equal("Span 1", span.Name);
        Assert.Equal(0, span.Index);
    }

    [Fact]
    public void Between_Section_IsCarriedOntoTheSpanWithTheDefaultCover()
    {
        var span = BeamSpanAssembly.Between(0, new[] { Column(0, 0, 400), Column(1, 6000, 400) }, Section);

        Assert.Equal(300.0, span.Width);
        Assert.Equal(600.0, span.Height);
        Assert.Equal(3500.0, span.TopElevation);
        Assert.Equal("beam-1", span.ElementUniqueId);
        Assert.Equal(25.0, span.Cover);
    }

    [Fact]
    public void Between_TipThenColumn_IsALeftCantilever()
    {
        var supports = new[] { Tip(0, 0), Column(1, 2000, 400) };

        var span = BeamSpanAssembly.Between(0, supports, Section);

        Assert.Equal(CantileverPosition.Left, span.Cantilever);
        Assert.Equal(1800.0, span.LengthClear);
        Assert.Equal(0.0, span.StartX);
    }

    [Fact]
    public void Between_ColumnThenTip_IsARightCantilever()
    {
        var supports = new[] { Column(0, 0, 400), Column(1, 6000, 400), Tip(2, 8000) };

        var span = BeamSpanAssembly.Between(1, supports, Section);

        Assert.Equal(CantileverPosition.Right, span.Cantilever);
        Assert.Equal("Span 2", span.Name);
        Assert.Equal(1800.0, span.LengthClear);
    }

    [Fact]
    public void Between_TipAtBothEnds_IsACantileverAtBothEnds()
    {
        var span = BeamSpanAssembly.Between(0, new[] { Tip(0, 0), Tip(1, 3000) }, Section);

        Assert.Equal(CantileverPosition.Both, span.Cantilever);
    }

    [Fact]
    public void Between_NoRightSupport_Assumes4000MillimetresCentreToCentre()
    {
        var supports = new[] { Column(0, 0, 400), Column(1, 6000, 400) };

        var span = BeamSpanAssembly.Between(1, supports, Section);

        Assert.Equal(4000.0, span.LengthCenter);
        Assert.Equal(3800.0, span.LengthClear);
        Assert.Equal(6200.0, span.StartX);
    }

    [Fact]
    public void Between_NoSupportAtAll_StartsAtZeroWith4000Millimetres()
    {
        var span = BeamSpanAssembly.Between(3, Array.Empty<BeamSupportNode>(), Section);

        Assert.Equal(0.0, span.StartX);
        Assert.Equal(4000.0, span.LengthCenter);
        Assert.Equal(4000.0, span.LengthClear);
    }

    [Fact]
    public void Between_SupportsWiderThanTheirSpacing_FallsBackToTheCentreToCentreLength()
    {
        var supports = new[] { Column(0, 0, 4000), Column(1, 1000, 4000) };

        var span = BeamSpanAssembly.Between(0, supports, Section);

        // The span still starts at the left face, so it ends past the right support's centre.
        Assert.Equal(1000.0, span.LengthClear);
        Assert.Equal(2000.0, span.StartX);
        Assert.Equal(3000.0, span.EndX);
    }

    [Fact]
    public void Between_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => BeamSpanAssembly.Between(0, null!, Section));
        Assert.Throws<ArgumentNullException>(() => BeamSpanAssembly.Between(0, Array.Empty<BeamSupportNode>(), null!));
    }

    private static BeamSupportNode Column(int index, double centerX, double width) =>
        new(index, $"Support {index + 1}", centerX, width, SupportType.Column);

    private static BeamSupportNode Tip(int index, double centerX) =>
        new(index, $"Tip {index + 1}", centerX, 0.0, SupportType.CantileverEnd);
}
