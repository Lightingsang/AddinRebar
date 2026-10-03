using System.Linq;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class ColumnBarPolylinesTests
{
    [Fact]
    public void Compute_TwoStoreyStack_MatchesTheThreeStepPipeline()
    {
        // Arrange
        var lower = TestSections.Rectangle(top: 3000);
        var upper = TestSections.Rectangle(b: 350, h: 500, bottom: 3000, top: 6000);
        var layout = TestSections.Grid();
        var splices = Enumerable.Range(1, layout.BarCount)
            .Select(n => SpliceSpec.Default(n, layout.BarDiameter, layout.SplitOverlap, layout.OverlapFactor))
            .ToList();

        var bars = BarLayoutCalculator.Compute(lower, layout);
        var upperPositions = SpliceCalculator.ComputeUpperPositions(upper, layout, 8, 10, bars, splices);
        var expected = bars.Select((bar, b) => BarPolylineBuilder.Build(lower, layout, bar, splices[b], upperPositions[b], "D20")).ToList();

        // Act
        var polylines = ColumnBarPolylines.Compute(lower, layout, splices, upper, 8, 10, "D20");

        // Assert
        Assert.Equal(CharacterizationText.Hash(expected), CharacterizationText.Hash(polylines));
        Assert.Equal(layout.BarCount, polylines.Count);
    }
}
