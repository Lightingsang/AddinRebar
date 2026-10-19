using System;
using HPKata.Core.Calculators;
using HPKata.Core.Shapes;
using Xunit;

namespace HPKata.Core.Tests.Shapes
{
    public class RebarShapeCatalog60ToCouplerTests
    {
        [Fact]
        public void Dang60_PrecastPileSpliceRing_CalculatesSumOf4Segments()
        {
            double[] p = { 150.0, 300.0, 150.0, 300.0 };
            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang60, p);
            Assert.Equal(900.0, len);
        }

        [Fact]
        public void Dang61And65And66_SixSegmentShapes_CalculateCorrectly()
        {
            double[] p = { 100.0, 200.0, 300.0, 400.0, 500.0, 600.0 }; // sum = 2100
            double len61 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang61, p);
            double len65 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang65, p);
            double len65a = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang65a, p);
            double len66 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang66, p);

            Assert.Equal(2100.0, len61);
            Assert.Equal(2100.0, len65);
            Assert.Equal(2100.0, len65a);
            Assert.Equal(2100.0, len66);
        }

        [Fact]
        public void Dang62And62aAnd64_FiveSegmentShapes_CalculateCorrectly()
        {
            double[] p = { 200.0, 400.0, 300.0, 400.0, 200.0 }; // sum = 1500
            double len62 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang62, p);
            double len62a = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang62a, p);
            double len64 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang64, p);

            Assert.Equal(1500.0, len62);
            Assert.Equal(1500.0, len62a);
            Assert.Equal(1500.0, len64);
        }

        [Fact]
        public void Dang63_MasonryWallDowelPin_CalculatesTwoSegments()
        {
            double[] p = { 250.0, 500.0 };
            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang63, p);
            Assert.Equal(750.0, len);
        }

        [Fact]
        public void Dang67_HelicalSpiralRebar_CalculatesTurnsAndHooks()
        {
            double[] p = { 400.0, 100.0, 1000.0 }; // dia=400, pitch=100, height=1000 => n = 10 turns
            int barDia = 10;
            double turnLen = Math.Sqrt((Math.PI * 400.0) * (Math.PI * 400.0) + 100.0 * 100.0);
            double hooks = 2.0 * RebarCalculator.Hook135Length(barDia);
            double expected = 10.0 * turnLen + hooks;

            double len = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang67, p, diameterMm: barDia);
            Assert.Equal(expected, len, 2);
        }

        [Fact]
        public void DangCoupler_MechanicalCouplerSleeve_DefaultAndCustomLength()
        {
            // Default sleeve length is 80mm
            double lenDefault = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.DangCoupler, Array.Empty<double>());
            Assert.Equal(80.0, lenDefault);

            // Custom sleeve length
            double[] p = { 110.0 };
            double lenCustom = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.DangCoupler, p);
            Assert.Equal(110.0, lenCustom);
        }

        [Fact]
        public void CalculateCuttingLength_NullParametersArray_HandlesSafely()
        {
            double len01 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang01, null!);
            Assert.Equal(0.0, len01);

            double lenCoupler = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.DangCoupler, null!);
            Assert.Equal(80.0, lenCoupler);
        }
    }
}
