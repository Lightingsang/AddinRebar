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
        Assert.Equal("927063C378CECE431FF8FFF4", CharacterizationText.Hash(polylines));
    }

    [Fact]
    public void Compute_TopOfTheStack_ProducesTheRecordedBars()
    {
        var polylines = ColumnBarPolylines.Compute(Lower, TestSections.Grid(), DefaultSplices(), above: null, 8, 8);

        Assert.Equal(TestSections.Grid().BarCount, polylines.Count);
        Assert.Equal("B65D5F1475A814BE424CBAFA", CharacterizationText.Hash(polylines));
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
