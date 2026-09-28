using System;
using System.Linq;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using Xunit;
using static HPRebar.Core.Tests.KataExport.TestKataData;

namespace HPRebar.Core.Tests.KataExport;

public sealed class KataSegmenterTests
{
    private const int Precision = 6;

    [Fact]
    public void ColumnsAndSpansAlternateAlongTheAxis()
    {
        var result = KataSegmenter.Segment(TwoSpansOnColumns());

        Assert.Equal(
            new[] { KataSegmentKind.Support, KataSegmentKind.Span, KataSegmentKind.Support, KataSegmentKind.Span, KataSegmentKind.Support },
            result.Segments.Select(s => s.Kind));
        Assert.Equal(5600, result.Segments[1].Extent.Length, Precision);
        Assert.True(result.StartsWithSupport);
        Assert.True(result.EndsWithSupport);
    }

    [Fact]
    public void EndSupportKeepsItsFullWidthBeyondTheBeamEnd()
    {
        var result = KataSegmenter.Segment(TwoSpansOnColumns());

        Assert.Equal(400, result.Segments[0].Extent.Length, Precision);
        Assert.Equal(-200, result.Segments[0].Extent.Start, Precision);
    }

    [Fact]
    public void CantileverAtBothEndsIsFlaggedAsNotSupported()
    {
        var input = new KataRunInput(
            new[] { Piece(-1500, 7500) },
            new[] { Column(-200, 200), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var result = KataSegmenter.Segment(input);

        Assert.False(result.StartsWithSupport);
        Assert.False(result.EndsWithSupport);
        Assert.Equal(1300, result.Segments[0].Extent.Length, Precision);
        Assert.Equal(1300, result.Segments[^1].Extent.Length, Precision);
    }

    [Fact]
    public void OverlappingSupportsMergeAndColumnWinsOverGirder()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000) },
            new[] { Girder(-150, 150, "300x600"), Column(-200, 200), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var result = KataSegmenter.Segment(input);
        var first = result.Segments[0];

        Assert.Equal(KataSupportKind.Column, first.Support!.Kind);
        Assert.Equal(400, first.Extent.Length, Precision);
        Assert.Equal(3, result.Segments.Count);
    }

    [Fact]
    public void TwoBeamElementsWithoutSupportMergeIntoOneContinuousSpanByDefault()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 3000, key: "B1"), Piece(3000, 6000, h: 600, key: "B2") },
            new[] { Column(-200, 200), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var result = KataSegmenter.Segment(input);

        Assert.Equal(
            new[] { KataSegmentKind.Support, KataSegmentKind.Span, KataSegmentKind.Support },
            result.Segments.Select(s => s.Kind));
        Assert.Equal(5600, result.Segments[1].Extent.Length, Precision);
    }

    [Fact]
    public void JointBetweenTwoBeamElementsWithoutSupportBecomesZeroWidthJointWhenJointsEnabled()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 3000, key: "B1"), Piece(3000, 6000, h: 600, key: "B2") },
            new[] { Column(-200, 200), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var result = KataSegmenter.Segment(input, new KataBuildOptions { InsertJoints = true });

        Assert.Equal(
            new[] { KataSegmentKind.Support, KataSegmentKind.Span, KataSegmentKind.Joint, KataSegmentKind.Span, KataSegmentKind.Support },
            result.Segments.Select(s => s.Kind));
        Assert.Equal("B1", result.Segments[1].Piece!.ElementKey);
        Assert.Equal("B2", result.Segments[3].Piece!.ElementKey);
        Assert.Equal(3000, result.Segments[2].Extent.Start, Precision);
    }

    [Fact]
    public void OneBeamElementSpanningSeveralSupportsGivesOneSpanPerGap()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 12000, zOffset: -50, key: "LONG") },
            new[] { Column(-200, 200), Column(5800, 6200), Column(11800, 12200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var result = KataSegmenter.Segment(input);
        var spans = result.Segments.Where(s => s.Kind == KataSegmentKind.Span).ToList();

        Assert.Equal(2, spans.Count);
        Assert.All(spans, s => Assert.Equal("LONG", s.Piece!.ElementKey));
    }

    [Fact]
    public void RunWithoutPiecesIsRejected()
    {
        var input = new KataRunInput(Array.Empty<KataBeamPiece>(), Array.Empty<KataSupport>(), Array.Empty<KataGridCrossing>(), Header);

        Assert.Throws<ArgumentException>(() => KataSegmenter.Segment(input));
    }
}
