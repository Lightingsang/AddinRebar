using System;
using System.Linq;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using Xunit;
using static HPRebar.Core.Tests.KataExport.TestKataData;

namespace HPRebar.Core.Tests.KataExport;

public sealed class KataEdgeCaseTests
{
    private const int Precision = 6;

    private static KataRunInput Run(KataBeamPiece[] pieces, KataSupport[] supports, KataGridCrossing[]? grids = null) =>
        new(pieces, supports, grids ?? Array.Empty<KataGridCrossing>(), Header);

    private static readonly KataSupport[] ThreeColumns = { Column(-200, 200), Column(5800, 6200), Column(11800, 12200) };

    [Fact]
    public void NaNBeamEndIsRejectedInsteadOfDroppingEverySupport()
    {
        var input = Run(new[] { Piece(0, double.NaN) }, ThreeColumns);

        Assert.Throws<ArgumentException>(() => KataRowBuilder.Build(input));
    }

    [Fact]
    public void NaNSupportIsRejected()
    {
        var input = Run(new[] { Piece(0, 6000) }, new[] { Column(-200, double.NaN), Column(5800, 6200) });

        Assert.Throws<ArgumentException>(() => KataRowBuilder.Build(input));
    }

    [Fact]
    public void ZeroLengthBeamIsRejected()
    {
        var input = Run(new[] { Piece(0, 6000), Piece(6000, 6000) }, ThreeColumns);

        Assert.Throws<ArgumentException>(() => KataRowBuilder.Build(input));
    }

    [Fact]
    public void NegativeSectionIsRejected()
    {
        var input = Run(new[] { Piece(0, 6000, b: -220) }, ThreeColumns);

        Assert.Throws<ArgumentException>(() => KataRowBuilder.Build(input));
    }

    [Fact]
    public void MissingSupportListIsAnArgumentError()
    {
        var input = new KataRunInput(new[] { Piece(0, 6000) }, null!, Array.Empty<KataGridCrossing>(), Header);

        Assert.Throws<ArgumentNullException>(() => KataRowBuilder.Build(input));
    }

    [Fact]
    public void BeamLyingInsideALongerBeamAddsNoJointAndIsReported()
    {
        var input = Run(new[] { Piece(0, 12000, key: "LONG"), Piece(3000, 4000, key: "DUP") }, ThreeColumns);

        var result = KataSegmenter.Segment(input);

        Assert.DoesNotContain(result.Segments, s => s.Kind == KataSegmentKind.Joint);
        Assert.Contains(result.Warnings, w => w.Contains("overlaps"));
    }

    [Fact]
    public void GapBetweenBeamsIsReported()
    {
        var input = Run(new[] { Piece(0, 3000), Piece(3500, 12000, key: "B2") }, ThreeColumns);

        var sheet = KataRowBuilder.Build(input);

        Assert.Contains(sheet.Warnings, w => w.Contains("Gap of 500 mm"));
    }

    [Fact]
    public void SupportOutsideTheRunIsIgnoredAndReported()
    {
        var input = Run(new[] { Piece(0, 6000) }, new[] { Column(-200, 200), Column(5800, 6200), Column(20000, 20400) });

        var sheet = KataRowBuilder.Build(input);

        Assert.Equal(3, sheet.ColumnCount);
        Assert.Contains(sheet.Warnings, w => w.Contains("outside"));
    }

    [Theory]
    [InlineData(200.0)]
    [InlineData(200.5)]
    public void SupportsThatTouchOrNearlyTouchMergeIntoOne(double girderStart)
    {
        var input = Run(new[] { Piece(0, 6000) }, new[] { Column(-200, 200), Girder(girderStart, 400, "300x600"), Column(5800, 6200) });

        var result = KataSegmenter.Segment(input);

        Assert.Equal(3, result.Segments.Count);
        Assert.Equal(KataSupportKind.Column, result.Segments[0].Support!.Kind);
        Assert.Equal(400, result.Segments[0].Extent.End, Precision);
    }

    [Fact]
    public void TwoGridsOnOneSupportWriteTheNearestAndWarn()
    {
        var sheet = KataRowBuilder.Build(TwoSpansOnColumns(new[] { Grid("A", 0), Grid("A'", 150), Grid("B", 6000), Grid("C", 12000) }));

        Assert.Equal("A", sheet.Row22[0]);
        Assert.Contains(sheet.Warnings, w => w.Contains("A'"));
    }

    [Fact]
    public void JointJustOutsideASupportFaceIsSnappedIntoTheSupport()
    {
        var input = Run(new[] { Piece(0, 5770), Piece(5770, 12000) }, ThreeColumns);

        var result = KataSegmenter.Segment(input, new KataBuildOptions { InsertJoints = true });

        Assert.DoesNotContain(result.Segments, s => s.Kind == KataSegmentKind.Joint);
        Assert.Equal(5, result.Segments.Count);
    }

    [Fact]
    public void FoundationWinsOverAGirderItOverlaps()
    {
        var input = Run(new[] { Piece(0, 6000) }, new[] { Girder(-150, 150, "300x600"), Footing(-300, 300), Column(5800, 6200) });

        var result = KataSegmenter.Segment(input);

        Assert.Equal(KataSupportKind.Foundation, result.Segments[0].Support!.Kind);
    }

    [Fact]
    public void CantileverAtStartOnlyIsPaddedAtStart()
    {
        var sheet = KataRowBuilder.Build(Run(new[] { Piece(-1500, 6000) }, new[] { Column(-200, 200), Column(5800, 6200) }));

        Assert.Equal(new object?[] { 0.0, 1300.0, 400.0, 5600.0, 400.0 }, sheet.Row11);
    }

    [Fact]
    public void CantileverAtEndOnlyIsPaddedAtEnd()
    {
        var sheet = KataRowBuilder.Build(Run(new[] { Piece(0, 7500) }, new[] { Column(-200, 200), Column(5800, 6200) }));

        Assert.Equal(new object?[] { 400.0, 5600.0, 400.0, 1300.0, 0.0 }, sheet.Row11);
    }

    [Fact]
    public void SeventyFiveColumnsFitTheSheet()
    {
        var supports = Enumerable.Range(0, 38).Select(i => Column(i * 1000 - 100, i * 1000 + 100)).ToArray();

        var sheet = KataRowBuilder.Build(Run(new[] { Piece(0, 37000) }, supports));

        Assert.Equal(75, sheet.ColumnCount);
    }

    [Fact]
    public void NegativeHalvesRoundAwayFromZeroAndTinyNegativesBecomePlainZero()
    {
        var sheet = KataRowBuilder.Build(TwoSpansOnColumns(new[] { Grid("1", -50.5), Grid("2", 5999.6) }));

        Assert.Equal(-51.0, sheet.Row23[0]);
        Assert.Equal(0.0, sheet.Row23[2]);
        Assert.False(double.IsNegative((double)sheet.Row23[2]!));
    }

    [Fact]
    public void ReverseNegatesGridOffsetsInRow21ButNotSoffitSteps()
    {
        var input = Run(
            new[] { Piece(0, 6000, h: 500), Piece(6000, 12000, h: 600, zOffset: -100) },
            ThreeColumns,
            new[] { Grid("1", 50), Grid("2", 6100) });

        var sheet = KataRowBuilder.Build(input, new KataBuildOptions { Reverse = true });

        Assert.Equal(new object?[] { "", -200.0, -100.0, 0.0, -50.0 }, sheet.Row21);
    }

    [Fact]
    public void ContinuousBeamRunFormedByTwoPiecesWithoutMiddleSupportBecomesSingleSpanAndHidesMiddleGrid()
    {
        // Real-world scenario from Revit model: T1-DY40 (AD.1a to AG.1a) formed by 2 pieces meeting at AF.1a
        // When crossing beam T1-DX24 at AF.1a is hidden, no support exists at AF.1a.
        var pieces = new[]
        {
            Piece(53600, 57200, b: 200, h: 350, key: "9797022"),
            Piece(57200, 60800, b: 200, h: 350, key: "9798811")
        };
        var supports = new[]
        {
            Column(53375, 53775, key: "Col_AD1a"),
            Column(60575, 60975, key: "Col_AG1a")
        };
        var grids = new[]
        {
            Grid("AD.1a", 53600),
            Grid("AF.1a", 57200),
            Grid("AG.1a", 60800)
        };

        var input = new KataRunInput(pieces, supports, grids, Header);
        var options = new KataBuildOptions();
        var sheet = KataRowBuilder.Build(input, options);
        var elevation = KataElevationBuilder.Build(input, options, sheet);

        // 1. Sheet has exactly 3 columns: Support AD.1a, Continuous Span, Support AG.1a (No 0-width support at AF.1a)
        Assert.Equal(3, sheet.ColumnCount);
        Assert.Equal(new object?[] { 400.0, 6800.0, 400.0 }, sheet.Row11);
        Assert.Equal(new object?[] { "AD.1a", "", "AG.1a" }, sheet.Row22);

        // 2. Elevation drawing only draws grids on actual supports (AF.1a inside the span is omitted)
        Assert.Equal(new[] { "AD.1a", "AG.1a" }, elevation.Grids.Select(g => g.Name));
        Assert.DoesNotContain(elevation.Grids, g => g.Name == "AF.1a");
    }
}
