using System;
using HPKata.Core.QuantitySurveying.Calculators;
using Xunit;

namespace HPKata.Core.Tests.QuantitySurveying
{
    public sealed class PanelTileLayoutCalculatorTests
    {
        [Fact]
        public void OptimizeLayout_ExactMultiple_ZeroCutPieces()
        {
            var res = PanelTileLayoutCalculator.OptimizeLayout(2400.0, 1800.0, 600.0, 600.0, 0.0);

            Assert.Equal(12, res.FullCount);
            Assert.Equal(0, res.CutCount);
            Assert.Equal(12, res.TotalPieces);
            Assert.Equal(4.32, res.NetAreaM2, 3);
            Assert.Equal(4.32, res.GrossAreaM2, 3);
            Assert.Equal(0.0, res.WasteRatio, 4);
        }

        [Fact]
        public void OptimizeLayout_WithPartialEdges_CountsFullAndCutCorrectly()
        {
            // 4800 x 3600 with 1220 x 2440 plywood panels:
            // Cols = ceil(4800 / 1220) = 4 (3 full widths of 1220 + 1 cut width of 1140)
            // Rows = ceil(3600 / 2440) = 2 (1 full height of 2440 + 1 cut height of 1160)
            var res = PanelTileLayoutCalculator.OptimizeLayout(4800.0, 3600.0, 1220.0, 2440.0, 0.0);

            Assert.Equal(8, res.TotalPieces);
            Assert.Equal(3, res.FullCount);
            Assert.Equal(5, res.CutCount);
            Assert.True(res.WasteRatio > 0.0 && res.WasteRatio < 0.5);
        }

        [Fact]
        public void OptimizeLayout_InvalidDimensions_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PanelTileLayoutCalculator.OptimizeLayout(0, 3000, 600, 600));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PanelTileLayoutCalculator.OptimizeLayout(3000, -100, 600, 600));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PanelTileLayoutCalculator.OptimizeLayout(3000, 3000, 0, 600));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PanelTileLayoutCalculator.OptimizeLayout(3000, 3000, 600, 0));
        }
    }
}
