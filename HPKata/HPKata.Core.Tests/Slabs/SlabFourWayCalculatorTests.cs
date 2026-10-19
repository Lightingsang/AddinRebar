using HPKata.Core.Slabs.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Slabs
{
    public class SlabFourWayCalculatorTests
    {
        [Fact]
        public void CalculateFourWayRebar_Generates4Layers()
        {
            var items = SlabFourWayCalculator.CalculateFourWayRebar(
                "S4D", lengthMm: 4000.0, widthMm: 4000.0, thicknessMm: 150.0);

            Assert.Equal(4, items.Count);
            Assert.Contains(items, it => it.BarMark == "4D-1" && it.Notes.Contains("0°"));
            Assert.Contains(items, it => it.BarMark == "4D-2" && it.Notes.Contains("90°"));
            Assert.Contains(items, it => it.BarMark == "4D-3" && it.Notes.Contains("+45°"));
            Assert.Contains(items, it => it.BarMark == "4D-4" && it.Notes.Contains("-45°"));
        }

        [Fact]
        public void CalculateDropPanelRebar_GeneratesSteppedBarsAndPerimeterTie()
        {
            var items = SlabFourWayCalculator.CalculateDropPanelRebar(
                "DP1", panelSizeMm: 1500.0, dropDepthMm: 100.0, slabThicknessMm: 150.0);

            Assert.Equal(3, items.Count);
            Assert.Contains(items, it => it.BarMark == "DP-1" && it.ShapeCode == "50");
            Assert.Contains(items, it => it.BarMark == "DP-2" && it.ShapeCode == "50");
            Assert.Contains(items, it => it.BarMark == "DP-T" && it.ShapeCode == "51");
        }
    }
}
