using System.Collections.Generic;
using HPKata.Core.Slabs.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Slabs
{
    public class SlabPolylineSteppingCalculatorTests
    {
        [Fact]
        public void CalculateSteppingRebar_Trapezoid_GeneratesSteppedLengthsIn50mmSteps()
        {
            var poly = new List<(double X, double Y)>
            {
                (0.0, 0.0),
                (4000.0, 0.0),
                (3000.0, 3000.0),
                (0.0, 3000.0)
            };

            var items = SlabPolylineSteppingCalculator.CalculateSteppingRebar(
                "S_TRAP", poly, spacingMm: 200.0, deltaLMm: 50.0);

            Assert.NotEmpty(items);
            foreach (var it in items)
            {
                // Each cutting length must be a multiple of 50mm
                Assert.Equal(0, it.CuttingLengthMm % 50.0);
            }
        }

        [Fact]
        public void CalculateSteppingRebar_ClustersVariableMarksAlphabetically()
        {
            var poly = new List<(double X, double Y)>
            {
                (0.0, 0.0),
                (5000.0, 0.0),
                (2500.0, 4000.0)
            };

            var items = SlabPolylineSteppingCalculator.CalculateSteppingRebar(
                "S_TRI", poly, spacingMm: 300.0, deltaLMm: 50.0);

            Assert.NotEmpty(items);
            Assert.Equal("1a", items[0].BarMark);
            if (items.Count > 1) Assert.Equal("1b", items[1].BarMark);
        }
    }
}
