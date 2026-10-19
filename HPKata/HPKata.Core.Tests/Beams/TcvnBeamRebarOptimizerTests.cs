using HPKata.Core.Beams.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class TcvnBeamRebarOptimizerTests
    {
        [Fact]
        public void Optimize_ZeroAs_ReturnsMinimumTwoCornerBars()
        {
            var opt = TcvnBeamRebarOptimizer.Optimize(0.0, 300.0, isTopBar: true);
            Assert.Equal(2, opt.TotalCount);
            Assert.Equal(18, opt.PrimaryDiameterMm);
            Assert.True(opt.IsCompliant);
            Assert.Equal("2Ø18", opt.Notation);
        }

        [Fact]
        public void Optimize_StandardAsDemand_SelectsSingleLayerSolution()
        {
            // 900 mm2 on width 300mm: 3d20 gives 3 * 314 = 942 mm2
            var opt = TcvnBeamRebarOptimizer.Optimize(900.0, 300.0, isTopBar: true);

            Assert.True(opt.IsCompliant);
            Assert.True(opt.AreaProvidedMm2 >= 900.0);
            Assert.Equal(1, opt.LayerCount);
            Assert.True(opt.ClearGapMm >= 30.0);
        }

        [Fact]
        public void Optimize_HeavyAsDemandOnNarrowBeam_SplitsToTwoLayers()
        {
            // 3500 mm2 on narrow width 220mm cannot fit in 1 layer with clear gaps >= 30mm
            var opt = TcvnBeamRebarOptimizer.Optimize(3500.0, 220.0, isTopBar: true);

            Assert.Equal(2, opt.LayerCount);
            Assert.True(opt.AreaProvidedMm2 >= 3500.0);
        }

        [Fact]
        public void Optimize_BottomBar_Enforces25mmClearGapMinimum()
        {
            var opt = TcvnBeamRebarOptimizer.Optimize(1200.0, 300.0, isTopBar: false);
            Assert.True(opt.IsCompliant);
            Assert.True(opt.ClearGapMm >= 25.0);
        }
    }
}
