using System;
using System.Linq;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class ColumnBarPolylinesTests
{
    private static readonly ColumnSection Lower = TestSections.Rectangle(top: 3000);
    private static readonly ColumnSection Upper = TestSections.Rectangle(b: 350, h: 500, bottom: 3000, top: 6000);

    [Fact]
    public void Compute_SegmentUnderANarrowerOne_ProducesTheRecordedBars()
    {
        var polylines = ColumnBarPolylines.Compute(Lower, TestSections.Grid(), DefaultSplices(), Upper, 8, 10, "D20");

        Assert.Equal(TestSections.Grid().BarCount, polylines.Count);
        Assert.Equal("05A9291F9F4F203D2EA7CDB9", CharacterizationText.Hash(polylines));
    }

    [Fact]
    public void Compute_TopOfTheStack_ProducesTheRecordedBars()
    {
        var polylines = ColumnBarPolylines.Compute(Lower, TestSections.Grid(), DefaultSplices(), above: null, 8, 8);

        Assert.Equal(TestSections.Grid().BarCount, polylines.Count);
        Assert.Equal("213E606D6F4C9F0A6D856C92", CharacterizationText.Hash(polylines));
    }

    /// <summary>
    /// The bar geometry alone (points of every bar), pinned apart from the full hash so a change of how the splice
    /// settings are represented can be shown not to move a single bar.
    /// </summary>
    [Theory]
    [InlineData(true, "6E603DCAD246B5A319E09260")]
    [InlineData(false, "DDF1820910A1202D3148D607")]
    public void Compute_RecordedCases_KeepTheirBarGeometry(bool withColumnAbove, string expectedHash)
    {
        var polylines = withColumnAbove
            ? ColumnBarPolylines.Compute(Lower, TestSections.Grid(), DefaultSplices(), Upper, 8, 10, "D20")
            : ColumnBarPolylines.Compute(Lower, TestSections.Grid(), DefaultSplices(), above: null, 8, 8);

        Assert.Equal(expectedHash, CharacterizationText.Hash(polylines.Select(p => p.Points).ToList()));
    }

    [Fact]
    public void Compute_OneSpliceShort_Throws()
    {
        var splices = DefaultSplices().Take(TestSections.Grid().BarCount - 1).ToList();

        Assert.ThrowsAny<ArgumentException>(() => ColumnBarPolylines.Compute(Lower, TestSections.Grid(), splices, Upper, 8, 10));
    }

    private static System.Collections.Generic.List<SpliceSpec> DefaultSplices()
    {
        var layout = TestSections.Grid();
        return Enumerable.Range(1, layout.BarCount)
            .Select(n => SpliceSpec.Default(n, layout.BarDiameter, layout.SplitOverlap, layout.OverlapFactor))
            .ToList();
    }
}
