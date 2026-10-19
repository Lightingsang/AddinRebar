using System;
using HPKata.Core.Calculators;
using Xunit;

namespace HPKata.Core.Tests.Calculators
{
    public class RebarCalculatorTests
    {
        [Theory]
        [InlineData(6, 0.222)]
        [InlineData(8, 0.395)]
        [InlineData(10, 0.617)]
        [InlineData(12, 0.888)]
        [InlineData(14, 1.210)]
        [InlineData(16, 1.580)]
        [InlineData(18, 2.000)]
        [InlineData(20, 2.470)]
        [InlineData(22, 2.980)]
        [InlineData(25, 3.850)]
        [InlineData(28, 4.830)]
        [InlineData(32, 6.310)]
        [InlineData(36, 7.990)]
        [InlineData(38, 8.900)]
        [InlineData(40, 9.860)]
        [InlineData(41, 10.360)]
        [InlineData(43, 11.400)]
        [InlineData(50, 15.420)]
        [InlineData(51, 16.040)]
        public void GetUnitMass_StandardTcvnDiameters_MatchesTcvnTable(int dia, double expectedMass)
        {
            double actualMass = RebarCalculator.GetUnitMass(dia);
            Assert.Equal(expectedMass, actualMass, 3);

            // Also verify that table values match theoretical formula W = d^2 / 162.2 within 1%
            double formulaMass = (dia * dia) / 162.2;
            double percentDiff = Math.Abs(actualMass - formulaMass) / formulaMass * 100.0;
            Assert.True(percentDiff < 1.0, $"Diameter {dia} diff {percentDiff}% exceeds 1.0%");
        }

        [Fact]
        public void GetUnitMass_NonStandardAndNegativeDiameters_CalculatesOrReturnsZero()
        {
            double customMass = RebarCalculator.GetUnitMass(30);
            double expected = (30.0 * 30.0) / 162.2;
            Assert.Equal(expected, customMass, 4);

            Assert.Equal(0.0, RebarCalculator.GetUnitMass(0));
            Assert.Equal(0.0, RebarCalculator.GetUnitMass(-12));
        }

        [Fact]
        public void CalculateWeight_LengthAndDiameter_CalculatesAccurately()
        {
            double weight = RebarCalculator.CalculateWeight(11.7, 20); // 11.7m of d20
            double expected = 11.7 * 2.470;
            Assert.Equal(expected, weight, 3);
        }

        [Theory]
        [InlineData(6, 75.0)]   // 10 * 6 = 60 < 75 => 75
        [InlineData(8, 80.0)]   // 10 * 8 = 80 >= 75 => 80
        [InlineData(16, 160.0)] // 10 * 16 = 160 >= 75 => 160
        [InlineData(25, 250.0)] // 10 * 25 = 250 >= 75 => 250
        public void Hook90Length_DefaultMultiplier_Enforces75mmMinimum(int dia, double expected)
        {
            double hook = RebarCalculator.Hook90Length(dia);
            Assert.Equal(expected, hook);
        }

        [Fact]
        public void Hook90Length_CustomMultiplierAndNegative_HandlesCorrectly()
        {
            double hookCustom = RebarCalculator.Hook90Length(10, 12.0); // 12 * 10 = 120
            Assert.Equal(120.0, hookCustom);

            Assert.Equal(0.0, RebarCalculator.Hook90Length(0));
            Assert.Equal(0.0, RebarCalculator.Hook90Length(-10));
        }

        [Theory]
        [InlineData(6, 75.0)]   // 7.5 * 6 = 45 < 75 => 75
        [InlineData(10, 75.0)]  // 7.5 * 10 = 75 >= 75 => 75
        [InlineData(16, 120.0)] // 7.5 * 16 = 120 >= 75 => 120
        [InlineData(20, 150.0)] // 7.5 * 20 = 150 >= 75 => 150
        public void Hook135Length_DefaultMultiplier_Enforces75mmMinimum(int dia, double expected)
        {
            double hook = RebarCalculator.Hook135Length(dia);
            Assert.Equal(expected, hook);
        }

        [Theory]
        [InlineData(6, 60.0)]   // 9 * 6 = 54 < 60 => 60
        [InlineData(8, 72.0)]   // 9 * 8 = 72 >= 60 => 72
        [InlineData(10, 90.0)]  // 9 * 10 = 90 >= 60 => 90
        [InlineData(16, 144.0)] // 9 * 16 = 144 >= 60 => 144
        public void Hook180Length_DefaultMultiplier_Enforces60mmMinimum(int dia, double expected)
        {
            double hook = RebarCalculator.Hook180Length(dia);
            Assert.Equal(expected, hook);
        }

        [Fact]
        public void BendRadiusInner_DifferentAnglesAndDiameters_AppliesTcvnRules()
        {
            // 90 deg: d <= 16 yields 2d, while d > 16 yields 3.5d
            Assert.Equal(20.0, RebarCalculator.BendRadiusInner(10, 90.0));
            Assert.Equal(32.0, RebarCalculator.BendRadiusInner(16, 90.0));
            Assert.Equal(70.0, RebarCalculator.BendRadiusInner(20, 90.0));

            // 135 deg: d <= 10 yields 1.5d, while d > 10 yields 2d
            Assert.Equal(12.0, RebarCalculator.BendRadiusInner(8, 135.0));
            Assert.Equal(15.0, RebarCalculator.BendRadiusInner(10, 135.0));
            Assert.Equal(24.0, RebarCalculator.BendRadiusInner(12, 135.0));

            // Other angles: 1.25d
            Assert.Equal(20.0, RebarCalculator.BendRadiusInner(16, 45.0));
        }

        [Theory]
        [InlineData(10, 90.0, true)]
        [InlineData(20, 90.0, true)]
        [InlineData(10, 135.0, true)]
        [InlineData(16, 45.0, true)]
        [InlineData(16, 180.0, true)]
        public void BendDeduction_TrueAndFalse_CalculatesPositiveDeductionOrZero(int dia, double angle, bool apply)
        {
            double ded = RebarCalculator.BendDeduction(dia, angle, apply);
            Assert.True(ded > 0.0, $"Bend deduction for {angle} deg must be positive");

            double noDed = RebarCalculator.BendDeduction(dia, angle, false);
            Assert.Equal(0.0, noDed);
        }

        [Fact]
        public void LapSpliceAndAnchorageLength_Multipliers_CalculateCorrectly()
        {
            Assert.Equal(600.0, RebarCalculator.LapSpliceLength(20)); // 30 * 20
            Assert.Equal(800.0, RebarCalculator.LapSpliceLength(20, 40.0)); // 40 * 20
            Assert.Equal(0.0, RebarCalculator.LapSpliceLength(0));

            Assert.Equal(800.0, RebarCalculator.TensionAnchorageLength(20)); // 40 * 20
            Assert.Equal(1000.0, RebarCalculator.TensionAnchorageLength(20, 50.0)); // 50 * 20
            Assert.Equal(0.0, RebarCalculator.TensionAnchorageLength(-5));
        }

        [Theory]
        [InlineData(1234.0, 10.0, 1240.0)]
        [InlineData(1230.0, 10.0, 1230.0)]
        [InlineData(1231.0, 5.0, 1235.0)]
        [InlineData(1234.0, 0.0, 1234.0)]
        public void RoundLength_VariousSteps_RoundsUp(double len, double step, double expected)
        {
            double rounded = RebarCalculator.RoundLength(len, step);
            Assert.Equal(expected, rounded);
        }
    }
}
