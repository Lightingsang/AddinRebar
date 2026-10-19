using HPDcePro.Core.Calculations.Wind;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class WindLoadTests
    {
        [Fact]
        public void GetBasicPressureByZone_MatchesTCVN2737Table1()
        {
            Assert.Equal(0.48, WindLoadCalculator.GetBasicPressureByZone(WindZone.ZoneI));
            Assert.Equal(0.71, WindLoadCalculator.GetBasicPressureByZone(WindZone.ZoneII));
            Assert.Equal(0.98, WindLoadCalculator.GetBasicPressureByZone(WindZone.ZoneIII));
            Assert.Equal(1.30, WindLoadCalculator.GetBasicPressureByZone(WindZone.ZoneIV));
            Assert.Equal(1.59, WindLoadCalculator.GetBasicPressureByZone(WindZone.ZoneV));
        }

        [Fact]
        public void CalculateVelocityPressure_MatchesStandardFormula()
        {
            // V10 = 40 m/s -> q0 = 0.613 * 10^-3 * 40^2 = 0.9808 kN/m2
            double q0 = WindLoadCalculator.CalculateVelocityPressure(40.0);
            Assert.Equal(0.9808, q0, 4);
        }

        [Fact]
        public void CalculateExposureCoefficient_CategoryOrdering_IsMonotonic()
        {
            // At 20 m height, Category A (open sea) > B (suburban) > C (urban) > D (dense city)
            double kA = WindLoadCalculator.CalculateExposureCoefficient(TerrainCategory.A, 20.0);
            double kB = WindLoadCalculator.CalculateExposureCoefficient(TerrainCategory.B, 20.0);
            double kC = WindLoadCalculator.CalculateExposureCoefficient(TerrainCategory.C, 20.0);
            double kD = WindLoadCalculator.CalculateExposureCoefficient(TerrainCategory.D, 20.0);

            Assert.True(kA > kB);
            Assert.True(kB > kC);
            Assert.True(kC > kD);
        }

        [Fact]
        public void CalculateExposureCoefficient_ClampsBelowZmin()
        {
            // Category B has zmin = 4 m. At 2 m, k(z) should equal k(4m)
            double kAt2M = WindLoadCalculator.CalculateExposureCoefficient(TerrainCategory.B, 2.0);
            double kAt4M = WindLoadCalculator.CalculateExposureCoefficient(TerrainCategory.B, 4.0);

            Assert.Equal(kAt4M, kAt2M, 4);
        }

        [Fact]
        public void Calculate_WindwardAndLeeward_ProducesCorrectWk()
        {
            // Zone II (q0 = 0.71 kN/m2), Category B, height 15 m
            double q0 = 0.71;
            double k15 = WindLoadCalculator.CalculateExposureCoefficient(TerrainCategory.B, 15.0);

            // Windward wall (cpe = +0.8, cpi = 0)
            var windward = WindLoadCalculator.Calculate(q0, TerrainCategory.B, 15.0, cpe: 0.8, cpi: 0.0);
            double expectedWkWindward = q0 * k15 * 0.8 * 1.0;
            Assert.Equal(expectedWkWindward, windward.CharacteristicPressureWkKnm2, 3);
            Assert.Equal(expectedWkWindward * 1.3, windward.DesignPressureWKnm2, 3);

            // Leeward wall (cpe = -0.5, cpi = 0)
            var leeward = WindLoadCalculator.Calculate(q0, TerrainCategory.B, 15.0, cpe: -0.5, cpi: 0.0);
            double expectedWkLeeward = q0 * k15 * (-0.5) * 1.0;
            Assert.Equal(expectedWkLeeward, leeward.CharacteristicPressureWkKnm2, 3);
            Assert.True(leeward.CharacteristicPressureWkKnm2 < 0.0); // Suction
        }

        [Fact]
        public void CalculateByZone_MatchesDirectCalculation()
        {
            var res = WindLoadCalculator.CalculateByZone(WindZone.ZoneIII, TerrainCategory.A, 30.0, cpe: 0.8, cpi: 0.2);

            Assert.Equal(0.98, res.BasicPressureQ0Knm2);
            Assert.Equal(0.8 - 0.2, res.NetAerodynamicCoefficientC, 4);
            Assert.True(res.CharacteristicPressureWkKnm2 > 0.0);
            Assert.True(res.DesignPressureWKnm2 > res.CharacteristicPressureWkKnm2);
        }

        [Fact]
        public void CalculateVelocityPressure_ZeroAndNegative_ReturnsZero()
        {
            Assert.Equal(0.0, WindLoadCalculator.CalculateVelocityPressure(0.0));
            Assert.Equal(0.0, WindLoadCalculator.CalculateVelocityPressure(-10.0));
        }
    }
}
