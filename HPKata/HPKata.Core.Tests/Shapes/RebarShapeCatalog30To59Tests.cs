using System;
using HPKata.Core.Calculators;
using HPKata.Core.Shapes;
using Xunit;

namespace HPKata.Core.Tests.Shapes
{
    public class RebarShapeCatalog30To59Tests
    {
        [Fact]
        public void Dang31_CircularArcBar_CalculatesArcSegmentLength()
        {
            double[] p = { 200.0, 500.0 }; // l1=200, l2=diameter 500
            double theta = 60.0;
            double expected = 2.0 * 200.0 + Math.PI * 500.0 * (60.0 / 180.0);

            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang31, p, angleDeg: theta);
            Assert.Equal(expected, len, 2);
        }

        [Fact]
        public void Dang34_InclinedDiagonalBar_CalculatesHypotenuseWithMultiplicity()
        {
            double[] p = { 300.0, 400.0, 3.0 }; // sqrt(300^2+400^2)=500, n=3 => 2*3*500 = 3000
            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang34, p);
            Assert.Equal(3000.0, len);
        }

        [Fact]
        public void Dang35And35a_SymmetricStirrupCaps_CalculatePerimeter()
        {
            double[] p = { 250.0, 350.0, 100.0 };
            double expected = 2.0 * 250.0 + 2.0 * 350.0 + 100.0; // 1300
            double len35 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang35, p);
            double len35a = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang35a, p);
            Assert.Equal(expected, len35);
            Assert.Equal(expected, len35a);
        }

        [Fact]
        public void Dang37And38_ArcBarsWithLegsAndHoops_CalculateCorrectly()
        {
            double[] p37 = { 300.0, 600.0, 0.0, 0.0, 300.0 };
            double expected37 = 300.0 + Math.PI * 600.0 * (90.0 / 180.0) + 300.0;
            double len37 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang37, p37, angleDeg: 90.0);
            Assert.Equal(expected37, len37, 2);

            double[] p38 = { 400.0, 500.0 };
            double expected38 = 2.0 * 400.0 + Math.PI * 500.0;
            double len38 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang38, p38);
            Assert.Equal(expected38, len38, 2);
        }

        [Fact]
        public void Dang40And40a_CircularRingWithLapAndHooks_CalculateCorrectly()
        {
            double[] p40 = { 1000.0, 0.0 }; // dia=1000, auto lap
            int dia = 20;
            double lap = RebarCalculator.LapSpliceLength(dia, 30.0); // 600
            double expected40 = Math.PI * 1000.0 + lap;
            double len40 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang40, p40, diameterMm: dia);
            Assert.Equal(expected40, len40, 2);

            double hook40a = RebarCalculator.Hook135Length(dia); // 150
            double expected40a = Math.PI * 1000.0 + 2.0 * hook40a;
            double len40a = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang40a, p40, diameterMm: dia);
            Assert.Equal(expected40a, len40a, 2);
        }

        [Fact]
        public void Dang45And45a_DiagonalOffsetCrank_CalculatesPythagoras()
        {
            double[] p45 = { 400.0, 600.0, 800.0, 500.0 }; // sqrt(600^2+800^2)=1000 => 400+1000+500 = 1900
            double len45 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang45, p45);
            Assert.Equal(1900.0, len45);

            double[] p45a = { 400.0, 600.0, 500.0 };
            double len45a = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang45a, p45a);
            Assert.Equal(1500.0, len45a);
        }

        [Theory]
        [InlineData(30.0, 500.0, 150.0, 500.0, 1300.0)] // mid = 150 / 0.5 = 300 => 500 + 300 + 500 = 1300
        [InlineData(0.0, 500.0, 150.0, 500.0, 1150.0)]  // degenerate clamp: mid=150 => 1150
        [InlineData(90.0, 500.0, 150.0, 500.0, 1150.0)] // sin(90)=1 => mid=150 => 1150
        public void Dang49_CrankedBar_SafeSinClampingAndAngles(double angle, double l1, double l2, double l3, double expected)
        {
            double[] p = { l1, l2, l3 };
            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang49, p, angleDeg: angle);
            Assert.Equal(expected, len, 1);
        }

        [Fact]
        public void Dang51And51a_OpenStirrupWithHooks_CalculatesHooks()
        {
            double[] p = { 200.0, 400.0, 200.0 };
            int dia = 10;
            double h135 = RebarCalculator.Hook135Length(dia); // 75
            double h90 = RebarCalculator.Hook90Length(dia);   // 100

            double expected51 = 200.0 + 400.0 + 200.0 + 2.0 * h135;
            double len51 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang51, p, diameterMm: dia);
            Assert.Equal(expected51, len51);

            double expected51a = 200.0 + 400.0 + 200.0 + h90 + h135;
            double len51a = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang51a, p, diameterMm: dia);
            Assert.Equal(expected51a, len51a);
        }

        [Fact]
        public void Dang52_CrossTie_Calculates90And135Hooks()
        {
            double[] p = { 500.0, 0.0 };
            int dia = 12;
            double h135 = RebarCalculator.Hook135Length(dia); // max(7.5*12, 75) = 90
            double h90 = RebarCalculator.Hook90Length(dia);   // max(10*12, 75) = 120
            double expected = 500.0 + h135 + h90;

            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang52, p, diameterMm: dia);
            Assert.Equal(expected, len);
        }

        [Fact]
        public void Dang53And53a_ClosedCapStirrups_CalculateCorrectly()
        {
            double[] p53 = { 300.0, 500.0, 150.0 };
            double expected53 = 2.0 * 300.0 + 2.0 * 500.0 + 150.0;
            double len53 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang53, p53);
            Assert.Equal(expected53, len53);

            double[] p53a = { 300.0, 500.0, 150.0, 80.0 };
            double expected53a = 2.0 * 300.0 + 2.0 * 500.0 + 150.0 + 2.0 * 80.0;
            double len53a = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang53a, p53a);
            Assert.Equal(expected53a, len53a);
        }

        [Fact]
        public void Dang58And59_LAndUAnchorBars_CalculateCorrectly()
        {
            double[] p58 = { 800.0, 200.0 };
            double expected58 = 800.0 + 2.0 * 200.0; // 1200
            double len58 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang58, p58);
            Assert.Equal(expected58, len58);

            double[] p59 = { 250.0, 1000.0 };
            double expected59 = 2.0 * 250.0 + 1000.0; // 1500
            double len59 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang59, p59);
            Assert.Equal(expected59, len59);
        }
    }
}
