using System;
using HPKata.Core.Columns.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Columns
{
    public class TcvnColumnRebarOptimizerTests
    {
        [Fact]
        public void Optimize_FromRequiredAst_ProvidesSufficientSteel()
        {
            double reqAst = 2500.0;
            var sec = TcvnColumnRebarOptimizer.Optimize(500.0, 500.0, reqAst);

            Assert.True(sec.TotalSteelAreaMm2 >= reqAst);
            Assert.True(sec.ReinforcementRatioPercent >= 0.6);
            Assert.True(sec.ReinforcementRatioPercent <= 3.5);
        }

        [Fact]
        public void Optimize_ZeroRequiredAst_EnforcesMinimumReinforcementRatio()
        {
            // 400x400 => Area = 160000 mm2, min 0.6% => 960 mm2
            var sec = TcvnColumnRebarOptimizer.Optimize(400.0, 400.0, 0.0, minRho: 0.6);

            Assert.True(sec.TotalSteelAreaMm2 >= 960.0);
            Assert.True(sec.ReinforcementRatioPercent >= 0.6);
        }

        [Fact]
        public void Optimize_RespectsClearSpacingConstraint()
        {
            var sec = TcvnColumnRebarOptimizer.Optimize(400.0, 400.0, 2000.0);
            double clearGap = sec.CalculateMinClearSpacingMm(coverMm: 30.0, tieDiaMm: 8.0);

            Assert.True(clearGap >= 50.0);
        }

        [Fact]
        public void Optimize_InvalidDimensions_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                TcvnColumnRebarOptimizer.Optimize(0.0, 400.0, 1000.0));
        }

        [Fact]
        public void Optimize_NarrowSectionWithExtremeAst_SetsNonCompliantAndMaintainsPositiveClearSpacing()
        {
            // 200x200 section with extreme required Ast = 10000 mm2 (> 3.5% Ag = 1400 mm2)
            var sec = TcvnColumnRebarOptimizer.Optimize(200.0, 200.0, 10000.0);

            Assert.False(sec.IsCompliant);
            double clearGap = sec.CalculateMinClearSpacingMm(coverMm: 30.0, tieDiaMm: 8.0);
            Assert.True(clearGap >= 50.0);
            Assert.Equal(0, sec.SideCountB);
            Assert.Equal(0, sec.SideCountH);
        }
    }
}
