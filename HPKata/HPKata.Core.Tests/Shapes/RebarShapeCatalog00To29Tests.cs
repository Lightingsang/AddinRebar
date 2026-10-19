using System;
using HPKata.Core.Calculators;
using HPKata.Core.Shapes;
using Xunit;

namespace HPKata.Core.Tests.Shapes
{
    public class RebarShapeCatalog00To29Tests
    {
        [Fact]
        public void Dang00_GenericFreeformRebar_CalculatesSumOfSegments()
        {
            double[] p = { 100.0, 200.0, 300.0, 400.0, 500.0 };
            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang00, p);
            Assert.Equal(1500.0, len);
        }

        [Fact]
        public void Dang01_StraightBar_ReturnsFirstSegment()
        {
            double[] p = { 5000.0 };
            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang01, p);
            Assert.Equal(5000.0, len);
        }

        [Fact]
        public void Dang02_SymmetricUBar_OutToOutAndBendDeduction()
        {
            double[] p = { 500.0, 1000.0 }; // 2 legs of 500, base of 1000
            int dia = 20;

            double lenOutToOut = RebarShapeCatalog.CalculateCuttingLength(
                RebarShapeType.Dang02, p, angleDeg: 0, diameterMm: dia, applyBendDeduction: false);
            Assert.Equal(2000.0, lenOutToOut);

            double lenWithDed = RebarShapeCatalog.CalculateCuttingLength(
                RebarShapeType.Dang02, p, angleDeg: 0, diameterMm: dia, applyBendDeduction: true);
            double ded = 2.0 * RebarCalculator.BendDeduction(dia, 90.0, true);
            Assert.Equal(2000.0 - ded, lenWithDed, 2);
            Assert.True(lenWithDed < lenOutToOut);
        }

        [Fact]
        public void Dang02a_UBarWith135Hooks_CalculatesLegsBaseAndHooks()
        {
            double[] p = { 400.0, 800.0 }; // l1=400, l2=800, l3=0 (auto hook)
            int dia = 10;
            double hook = RebarCalculator.Hook135Length(dia); // max(7.5*10, 75) = 75
            double expected = 2.0 * 400.0 + 800.0 + 2.0 * hook;

            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang02a, p, diameterMm: dia);
            Assert.Equal(expected, len);
        }

        [Fact]
        public void Dang03_LBar_OutToOutAndBendDeduction()
        {
            double[] p = { 600.0, 1200.0 };
            int dia = 16;
            double lenOut = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang03, p, applyBendDeduction: false);
            Assert.Equal(1800.0, lenOut);

            double lenDed = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang03, p, diameterMm: dia, applyBendDeduction: true);
            double expectedDed = 1800.0 - RebarCalculator.BendDeduction(dia, 90.0, true);
            Assert.Equal(expectedDed, lenDed, 2);
        }

        [Fact]
        public void Dang04And05_AsymmetricUBarAndZBar_CalculateCorrectly()
        {
            double[] p4 = { 300.0, 800.0, 500.0 };
            double len4 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang04, p4);
            Assert.Equal(1600.0, len4);

            double[] p5 = { 250.0, 1000.0, 250.0 };
            double len5 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang05, p5);
            Assert.Equal(1500.0, len5);
        }

        [Theory]
        [InlineData(30.0, 1000.0, 200.0, 1000.0, 2400.0)] // l2 / sin(30) = 200 / 0.5 = 400
        [InlineData(0.0, 1000.0, 200.0, 1000.0, 2200.0)]  // degenerate clamp: SafeSin=0 => mid=l2=200
        [InlineData(90.0, 1000.0, 200.0, 1000.0, 2200.0)] // sin(90)=1 => mid=200
        public void Dang06_CrankedBar_SafeSinClampingAndAngles(double angle, double l1, double l2, double l3, double expected)
        {
            double[] p = { l1, l2, l3 };
            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang06, p, angleDeg: angle);
            Assert.Equal(expected, len, 1);
        }

        [Fact]
        public void Dang08And08a_ColumnOffsetCrank_CalculatesPythagoras()
        {
            double[] p = { 500.0, 300.0, 400.0, 1000.0 }; // sqrt(300^2 + 400^2) = 500
            double expected = 500.0 + 500.0 + 1000.0; // 2000

            double len8 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang08, p);
            double len8a = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang08a, p);
            Assert.Equal(expected, len8);
            Assert.Equal(expected, len8a);
        }

        [Fact]
        public void Dang09And09a_UBarWithCogsAndHooks_CalculatesExpected()
        {
            double[] p = { 400.0, 800.0 };
            int dia = 16;

            double cog9 = RebarCalculator.Hook90Length(dia); // max(10*16, 75) = 160
            double expected9 = 2.0 * 400.0 + 800.0 + 2.0 * cog9;
            double len9 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang09, p, diameterMm: dia);
            Assert.Equal(expected9, len9);

            double hook9a = RebarCalculator.Hook180Length(dia); // max(9*16, 60) = 144
            double expected9a = 2.0 * 400.0 + 800.0 + 2.0 * hook9a;
            double len9a = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang09a, p, diameterMm: dia);
            Assert.Equal(expected9a, len9a);
        }

        [Fact]
        public void Dang14_ClosedRectangularRingWelded_CalculatesPerimeter()
        {
            double[] p = { 300.0, 500.0 };
            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang14, p);
            Assert.Equal(1600.0, len);
        }

        [Fact]
        public void Dang19_SemiCircularUBar_CalculatesSemiCircle()
        {
            double[] p = { 500.0, 400.0 }; // l1=500, l2=diameter 400
            double expected = 2.0 * 500.0 + Math.PI * 400.0;
            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang19, p);
            Assert.Equal(expected, len, 2);
        }

        [Fact]
        public void Dang22_StandardClosedStirrup135_OutToOutAndBendDeductions()
        {
            double[] p = { 200.0, 400.0 }; // b=200, h=400
            int dia = 8;
            double hook = RebarCalculator.Hook135Length(dia); // max(7.5*8, 75) = 75
            double outToOut = 2.0 * (200.0 + 400.0) + 2.0 * hook;

            double lenOut = RebarShapeCatalog.CalculateCuttingLength(
                RebarShapeType.Dang22, p, diameterMm: dia, applyBendDeduction: false);
            Assert.Equal(outToOut, lenOut);

            double lenDed = RebarShapeCatalog.CalculateCuttingLength(
                RebarShapeType.Dang22, p, diameterMm: dia, applyBendDeduction: true);
            double expectedDed = outToOut - 4.0 * RebarCalculator.BendDeduction(dia, 90.0, true);
            Assert.Equal(expectedDed, lenDed, 2);
        }

        [Fact]
        public void Dang23And23a_SpiralOverlapClosedStirrup_IncludesLapSplice()
        {
            double[] p = { 300.0, 600.0 };
            int dia = 10;
            double lap = RebarCalculator.LapSpliceLength(dia, 30.0); // 300
            double expected = 2.0 * (300.0 + 600.0) + lap;

            double len23 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang23, p, diameterMm: dia);
            double len23a = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang23a, p, diameterMm: dia);
            Assert.Equal(expected, len23);
            Assert.Equal(expected, len23a);
        }

        [Fact]
        public void Dang24And25_DiamondAndOctagonalStirrups_CalculateCorrectly()
        {
            double[] p24 = { 250.0, 0.0 };
            int dia = 10;
            double hook = RebarCalculator.Hook135Length(dia); // 75
            double expected24 = 4.0 * 250.0 + 2.0 * hook;
            double len24 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang24, p24, diameterMm: dia);
            Assert.Equal(expected24, len24);

            double[] p25 = { 200.0, 400.0, 100.0 };
            double expected25 = 2.0 * (200.0 + 400.0) + 4.0 * 100.0 + 2.0 * hook;
            double len25 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang25, p25, diameterMm: dia);
            Assert.Equal(expected25, len25);
        }
    }
}
