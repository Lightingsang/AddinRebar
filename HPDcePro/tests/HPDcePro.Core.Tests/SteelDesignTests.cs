using HPDcePro.Core.Calculations.Steel;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class SteelDesignTests
    {
        [Fact]
        public void CreateISection_CalculatesCorrectGeometricProperties()
        {
            // I 300x150x6.5x9
            var sect = SteelSectionProperties.CreateISection(300.0, 150.0, 6.5, 9.0);

            Assert.True(sect.AreaMm2 > 4000.0 && sect.AreaMm2 < 5000.0);
            Assert.True(sect.IxMm4 > sect.IyMm4); // Major axis Ix >> Iy
            Assert.True(sect.WxMm3 > sect.WyMm3);
            Assert.True(sect.RadiusGyrationXmm > sect.RadiusGyrationYmm);
            Assert.True(sect.TorsionalConstantItMm4 > 0.0);
            Assert.True(sect.WarpingConstantIwMm6 > 0.0);
        }

        [Fact]
        public void CreateBoxSection_CalculatesCorrectGeometricProperties()
        {
            // Box 200x200x8
            var box = SteelSectionProperties.CreateBoxSection(200.0, 200.0, 8.0);

            double expectedArea = (200.0 * 200.0) - (184.0 * 184.0);
            Assert.Equal(expectedArea, box.AreaMm2, 2);
            Assert.Equal(box.IxMm4, box.IyMm4, 2); // Symmetric square
            Assert.Equal(box.WxMm3, box.WyMm3, 2);
            Assert.Equal(box.RadiusGyrationXmm, box.RadiusGyrationYmm, 2);
        }

        [Fact]
        public void CreatePipeSection_CalculatesCorrectGeometricProperties()
        {
            // Pipe D219.1 x 6.0
            var pipe = SteelSectionProperties.CreatePipeSection(219.1, 6.0);

            Assert.True(pipe.AreaMm2 > 3500.0 && pipe.AreaMm2 < 4500.0);
            Assert.Equal(pipe.IxMm4, pipe.IyMm4, 2);
            Assert.Equal(pipe.WxMm3, pipe.WyMm3, 2);
            Assert.Equal(pipe.RadiusGyrationXmm, pipe.RadiusGyrationYmm, 2);
        }

        [Fact]
        public void SteelStabilityCalculator_UnderSafeLoads_VerifiesStability()
        {
            var sect = SteelSectionProperties.CreateISection(300.0, 150.0, 6.5, 9.0);
            var input = new SteelStabilityInput(
                axialForceKn: 100.0,
                bendingMomentXknm: 15.0,
                section: sect,
                effectiveLengthXm: 3.0,
                effectiveLengthYm: 1.5,
                yieldStrengthMpa: 235.0,
                gammaC: 1.0)
            {
                TotalDepthMm = 300.0,
                FlangeThicknessMm = 9.0
            };

            var result = SteelStabilityCalculator.Calculate(input);

            Assert.True(result.PhiE > 0.0 && result.PhiE <= 1.0);
            Assert.True(result.PhiY > 0.0 && result.PhiY <= 1.0);
            Assert.True(result.PhiB > 0.0 && result.PhiB <= 1.0);
            Assert.True(result.InPlaneRatio > 0.0);
            Assert.True(result.OutOfPlaneRatio > 0.0);
            Assert.True(result.InPlaneRatio < 1.0);
            Assert.True(result.OutOfPlaneRatio < 1.0);
            Assert.True(result.IsCompliant);
        }

        [Fact]
        public void SteelStabilityCalculator_UnderOverloadedForces_FailsStability()
        {
            var sect = SteelSectionProperties.CreateISection(300.0, 150.0, 6.5, 9.0);
            // Overloaded forces: N = 2000 kN, Mx = 300 kNm
            var input = new SteelStabilityInput(
                axialForceKn: 2000.0,
                bendingMomentXknm: 300.0,
                section: sect,
                effectiveLengthXm: 6.0,
                effectiveLengthYm: 6.0,
                yieldStrengthMpa: 235.0,
                gammaC: 1.0)
            {
                TotalDepthMm = 300.0,
                FlangeThicknessMm = 9.0
            };

            var result = SteelStabilityCalculator.Calculate(input);

            Assert.True(result.InPlaneRatio > 1.0 || result.OutOfPlaneRatio > 1.0);
            Assert.False(result.IsCompliant);
        }

        [Fact]
        public void SteelStabilityCalculator_PureCompression_ZeroMoment()
        {
            var sect = SteelSectionProperties.CreateISection(300.0, 150.0, 6.5, 9.0);
            var input = new SteelStabilityInput(
                axialForceKn: 300.0,
                bendingMomentXknm: 0.0,
                section: sect,
                effectiveLengthXm: 3.0,
                effectiveLengthYm: 2.0);

            var result = SteelStabilityCalculator.Calculate(input);

            Assert.Equal(0.0, result.RelativeEccentricityM);
            Assert.Equal(1.0, result.CoefficientC);
            Assert.True(result.InPlaneRatio > 0.0);
            Assert.True(result.OutOfPlaneRatio > 0.0);
        }

        [Fact]
        public void SteelSectionProperties_InvalidGeometry_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SteelSectionProperties.CreateISection(-100.0, 100.0, 5.0, 8.0));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SteelSectionProperties.CreateBoxSection(100.0, 100.0, 60.0)); // 2*t > dimension

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SteelSectionProperties.CreatePipeSection(100.0, 60.0)); // 2*t > diameter
        }
    }
}
