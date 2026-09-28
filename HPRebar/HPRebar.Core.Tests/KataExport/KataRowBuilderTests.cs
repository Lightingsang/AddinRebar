using System;
using System.Linq;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using Xunit;
using static HPRebar.Core.Tests.KataExport.TestKataData;

namespace HPRebar.Core.Tests.KataExport;

public sealed class KataRowBuilderTests
{
    [Fact]
    public void HeaderColumnLeavesAxisNameEmptyWhileHoldingSlabOffsetAndElevationWhenModelProvidesThem()
    {
        var header = Header with { SlabThicknessMm = 150, AxisGridName = "B", AxisOffsetMm = -100 };
        var input = TwoSpansOnColumns() with { Header = header };

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(new object?[] { "D1", 2, 500.0, 220.0, 150.0, "", -100.0, new KataText("+3.600") }, sheet.HeaderColumn);
        Assert.Empty(sheet.Warnings);
    }

    [Fact]
    public void AxisOffsetChangesSideWhenTheRunIsWrittenInReverse()
    {
        var input = TwoSpansOnColumns() with { Header = Header with { SlabThicknessMm = 150, AxisGridName = "B", AxisOffsetMm = -100 } };

        var sheet = KataRowBuilder.Build(input, new KataBuildOptions { Reverse = true });

        Assert.Equal(100.0, sheet.HeaderColumn[6]);
    }

    [Fact]
    public void HeaderFallsBackToTheOldConstantsAndWarnsWhenSlabAndAxisGridAreMissing()
    {
        var sheet = KataRowBuilder.Build(TwoSpansOnColumns());

        Assert.Equal(new object?[] { "D1", 2, 500.0, 220.0, 0.0, "", -110.0, new KataText("+3.600") }, sheet.HeaderColumn);
        Assert.Contains(sheet.Warnings, w => w.Contains("B7"));
        Assert.Contains(sheet.Warnings, w => w.Contains("B8"));
    }

    [Theory]
    [InlineData(3300.0, "+3.300")]
    [InlineData(-1200.0, "-1.200")]
    [InlineData(0.0, "+0.000")]
    [InlineData(-0.4, "+0.000")]
    [InlineData(12345.5, "+12.346")]
    public void ElevationIsWrittenLikeKataWithExplicitSign(double elevationMm, string expected)
    {
        Assert.Equal(expected, KataFormat.Elevation(elevationMm));
    }

    [Fact]
    public void Row21AtAColumnGivesTheCrossingBeamOffsetAndFallsBackToTheGrid()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000), Piece(6000, 12000) },
            new[]
            {
                Column(-200, 200), Girder(-80, 170, "250x500", "G0"),   // crossing beam centre at +45
                Column(5800, 6200),                                      // no crossing beam → grid
                Column(11800, 12200)
            },
            new[] { Grid("1", 0), Grid("2", 6100), Grid("3", 12000) },
            Header);

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(45.0, sheet.Row21[0]);
        Assert.Equal(0.0, sheet.Row23[0]);
        Assert.Equal(100.0, sheet.Row21[2]);
        Assert.Equal(400.0, sheet.Row11[0]);
    }

    [Fact]
    public void Row11ListsSupportWidthsAndSpanLengths()
    {
        var sheet = KataRowBuilder.Build(TwoSpansOnColumns());

        Assert.Equal(new object?[] { 400.0, 5600.0, 400.0, 5600.0, 400.0 }, sheet.Row11);
    }

    [Fact]
    public void GirderSupportWritesItsOwnSectionInRow11()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 3000), Piece(3000, 6000), Piece(6000, 9000) },
            new[] { Column(-200, 200), Girder(2850, 3150, "300x600", "G1"), Girder(5875, 6125, "250x450", "G2"), Column(8800, 9200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal("300x600", sheet.Row11[2]);
        Assert.Equal("250x450", sheet.Row11[4]);
    }

    [Fact]
    public void Row19GivesUpperColumnWidthAndOffsetAtSupportsAndZOffsetOnSpans()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000, zOffset: -50) },
            new[] { Column(-200, 200, upper: new Interval1D(-150, 150)), Column(5800, 6200, upper: new Interval1D(5850, 6250)) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(new object?[] { "300;0", -50.0, "400;50" }, sheet.Row19);
    }

    [Fact]
    public void SupportWithoutColumnAboveWritesZeroInRow19()
    {
        var sheet = KataRowBuilder.Build(TwoSpansOnColumns());

        Assert.Equal(0.0, sheet.Row19[0]);
    }

    [Fact]
    public void Rows22And23NameTheGridAndItsOffsetFromTheSupportCentre()
    {
        var sheet = KataRowBuilder.Build(TwoSpansOnColumns(new[] { Grid("A", 0), Grid("B", 6100), Grid("C", 11950) }));

        Assert.Equal(new object?[] { "A", "", "B", "", "C" }, sheet.Row22);
        Assert.Equal(new object?[] { 0.0, "", 100.0, "", -50.0 }, sheet.Row23);
    }

    [Fact]
    public void GridOutsideEverySupportIsIgnored()
    {
        var sheet = KataRowBuilder.Build(TwoSpansOnColumns(new[] { Grid("X", 3000) }));

        Assert.All(sheet.Row22, v => Assert.Equal("", v));
    }

    [Fact]
    public void Row21CarriesGridOffsetAtSupportsAndSoffitStepOnSpans()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000, h: 500), Piece(6000, 12000, h: 600, zOffset: -100) },
            new[] { Column(-200, 200), Column(5800, 6200), Column(11800, 12200) },
            new[] { Grid("1", 0), Grid("2", 6100) },
            Header);

        var sheet = KataRowBuilder.Build(input);

        // span 2: -(600 - 500) + (-100) = -200
        Assert.Equal(new object?[] { 0.0, 0.0, 100.0, -200.0, "" }, sheet.Row21);
    }

    [Fact]
    public void CantileverAtBothEndsIsPaddedWithAZeroSupportColumn()
    {
        var input = new KataRunInput(
            new[] { Piece(-1500, 7500) },
            new[] { Column(-200, 200), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(new object?[] { 0.0, 1300.0, 400.0, 5600.0, 400.0, 1300.0, 0.0 }, sheet.Row11);
        Assert.Equal(0.0, sheet.Row19[0]);
        Assert.Equal("", sheet.Row21[0]);
        Assert.Equal("", sheet.Row22[^1]);
        Assert.Equal("", sheet.Row23[^1]);
        Assert.Equal(7, sheet.ColumnCount);
    }

    [Fact]
    public void ReverseFlipsOrderAndNegatesOffsets()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000, zOffset: -50) },
            new[] { Column(-200, 200, upper: new Interval1D(-150, 250)), Column(5800, 6200) },
            new[] { Grid("1", 100), Grid("2", 6000) },
            Header);

        var normal = KataRowBuilder.Build(input);
        var reverse = KataRowBuilder.Build(input, new KataBuildOptions { Reverse = true });

        Assert.Equal(normal.Row11.Reverse(), reverse.Row11);
        Assert.Equal(new object?[] { 0.0, -50.0, "400;-50" }, reverse.Row19);
        Assert.Equal(new object?[] { 0.0, "", -100.0 }, reverse.Row23);
        Assert.Equal(new object?[] { "2", "", "1" }, reverse.Row22);
        Assert.Equal(normal.HeaderColumn, reverse.HeaderColumn);
    }

    [Fact]
    public void SpanTakesAttributesFromTheBeamElementUnderItsMidpointNotFromItsIndex()
    {
        // One element runs over two supports, a second element follows: index pairing would shift z offsets.
        var input = new KataRunInput(
            new[] { Piece(0, 12000, zOffset: -50, key: "LONG"), Piece(12000, 18000, zOffset: -150, key: "SHORT") },
            new[] { Column(-200, 200), Column(5800, 6200), Column(11800, 12200), Column(17800, 18200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(new object?[] { 0.0, -50.0, 0.0, -50.0, 0.0, -150.0, 0.0 }, sheet.Row19);
    }

    [Fact]
    public void TwoBeamElementsWithoutSupportMergeIntoOneContinuousSpanByDefault()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 3000), Piece(3000, 6000) },
            new[] { Column(-200, 200), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(new object?[] { 400.0, 5600.0, 400.0 }, sheet.Row11);
    }

    [Fact]
    public void JointBetweenElementsWritesZeroWidthSupportWhenJointsEnabled()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 3000), Piece(3000, 6000) },
            new[] { Column(-200, 200), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var sheet = KataRowBuilder.Build(input, new KataBuildOptions { InsertJoints = true });

        Assert.Equal(new object?[] { 400.0, 2800.0, 0.0, 2800.0, 400.0 }, sheet.Row11);
        Assert.Equal(0.0, sheet.Row19[2]);
        Assert.Equal("", sheet.Row21[2]);
    }

    [Fact]
    public void MidpointsRoundAwayFromZero()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000) },
            new[] { Column(-200, 200.5), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(401.0, sheet.Row11[0]);
        Assert.Equal(5600.0, sheet.Row11[1]);
    }

    [Fact]
    public void MoreColumnsThanTheKataSheetHoldsIsRejected()
    {
        var supports = Enumerable.Range(0, 40).Select(i => Column(i * 1000 - 100, i * 1000 + 100)).ToArray();
        var input = new KataRunInput(new[] { Piece(0, 39000) }, supports, Array.Empty<KataGridCrossing>(), Header);

        var ex = Assert.Throws<InvalidOperationException>(() => KataRowBuilder.Build(input));
        Assert.Contains("76", ex.Message);
    }
}
