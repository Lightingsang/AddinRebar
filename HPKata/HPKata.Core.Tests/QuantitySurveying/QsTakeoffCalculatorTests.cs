using System;
using HPKata.Core.QuantitySurveying.Calculators;
using HPKata.Core.QuantitySurveying.Models;
using Xunit;

namespace HPKata.Core.Tests.QuantitySurveying
{
    public class QsTakeoffCalculatorTests
    {
        [Fact]
        public void CalculateGrossVolume_Column_ReturnsCorrectM3()
        {
            var col = new QsElementModel
            {
                MemberType = QsMemberType.Column,
                WidthMm = 400,
                HeightMm = 400,
                LengthMm = 3500,
                MemberCount = 2
            };

            double vol = QsTakeoffCalculator.CalculateGrossVolume(col);
            // 0.4 * 0.4 * 3.5 * 2 = 1.12 m3
            Assert.Equal(1.12, Math.Round(vol, 4));
        }

        [Fact]
        public void CalculateGrossVolume_Beam_ReturnsCorrectM3()
        {
            var beam = new QsElementModel
            {
                MemberType = QsMemberType.Beam,
                WidthMm = 250,
                HeightMm = 600,
                LengthMm = 6000,
                MemberCount = 1
            };

            double vol = QsTakeoffCalculator.CalculateGrossVolume(beam);
            // 0.25 * 0.6 * 6.0 = 0.9 m3
            Assert.Equal(0.9, Math.Round(vol, 4));
        }

        [Fact]
        public void CalculateGrossVolume_Slab_ReturnsCorrectM3()
        {
            var slab = new QsElementModel
            {
                MemberType = QsMemberType.Slab,
                LengthMm = 6000,
                WidthMm = 5000,
                ThicknessMm = 120,
                MemberCount = 1
            };

            double vol = QsTakeoffCalculator.CalculateGrossVolume(slab);
            // 6.0 * 5.0 * 0.12 = 3.6 m3
            Assert.Equal(3.6, Math.Round(vol, 4));
        }

        [Fact]
        public void CalculateGrossVolume_Footing_ReturnsCorrectM3()
        {
            var footing = new QsElementModel
            {
                MemberType = QsMemberType.Footing,
                LengthMm = 2000,
                WidthMm = 1800,
                HeightMm = 800,
                MemberCount = 1
            };

            double vol = QsTakeoffCalculator.CalculateGrossVolume(footing);
            // 2.0 * 1.8 * 0.8 = 2.88 m3
            Assert.Equal(2.88, Math.Round(vol, 4));
        }

        [Fact]
        public void CalculateGrossVolume_Wall_ReturnsCorrectM3()
        {
            var wall = new QsElementModel
            {
                MemberType = QsMemberType.Wall,
                LengthMm = 4000,
                ThicknessMm = 200,
                HeightMm = 3300,
                MemberCount = 1
            };

            double vol = QsTakeoffCalculator.CalculateGrossVolume(wall);
            // 4.0 * 0.2 * 3.3 = 2.64 m3
            Assert.Equal(2.64, Math.Round(vol, 4));
        }

        [Fact]
        public void CalculateGrossFormwork_Beam_SoffitAndSides()
        {
            var beam = new QsElementModel
            {
                MemberType = QsMemberType.Beam,
                WidthMm = 250,
                HeightMm = 500,
                LengthMm = 5000,
                MemberCount = 1
            };

            var (soffit, side) = QsTakeoffCalculator.CalculateGrossFormwork(beam);
            // Soffit: 0.25 * 5.0 = 1.25 m2
            // Sides: 2 * 0.5 * 5.0 = 5.0 m2
            Assert.Equal(1.25, Math.Round(soffit, 4));
            Assert.Equal(5.0, Math.Round(side, 4));
        }

        [Fact]
        public void CalculateGrossFormwork_BeamWithSlabContact_AdjustsSideFormwork()
        {
            var beam = new QsElementModel
            {
                MemberType = QsMemberType.Beam,
                WidthMm = 250,
                HeightMm = 500,
                LengthMm = 5000,
                MemberCount = 1,
                HasSlabLeft = true,
                SlabThicknessLeftMm = 100,
                HasSlabRight = false
            };

            var (soffit, side) = QsTakeoffCalculator.CalculateGrossFormwork(beam);
            // Soffit: 1.25 m2
            // Sides: (0.4 + 0.5) * 5.0 = 4.5 m2
            Assert.Equal(1.25, Math.Round(soffit, 4));
            Assert.Equal(4.5, Math.Round(side, 4));
        }

        [Fact]
        public void CalculateGrossVolume_NullElement_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => QsTakeoffCalculator.CalculateGrossVolume(null!));
        }

        [Fact]
        public void CalculateGrossVolume_NegativeDimensions_ClampsToZeroVolume()
        {
            var beam = new QsElementModel
            {
                MemberType = QsMemberType.Beam,
                WidthMm = -250,
                HeightMm = 500,
                LengthMm = 6000
            };
            double vol = QsTakeoffCalculator.CalculateGrossVolume(beam);
            Assert.Equal(0.0, vol);
        }

        [Fact]
        public void CalculateGrossVolume_DoubleNegativeDimensions_ClampsToZeroWithoutFalsePositive()
        {
            var col = new QsElementModel
            {
                MemberType = QsMemberType.Column,
                WidthMm = -400,
                HeightMm = -400,
                LengthMm = 3600
            };
            double vol = QsTakeoffCalculator.CalculateGrossVolume(col);
            Assert.Equal(0.0, vol);
        }

        [Fact]
        public void CalculateGrossFormwork_NegativeDimensions_ClampsToNonNegative()
        {
            var beam = new QsElementModel
            {
                MemberType = QsMemberType.Beam,
                WidthMm = -250,
                HeightMm = 500,
                LengthMm = 6000
            };
            var (soffit, side) = QsTakeoffCalculator.CalculateGrossFormwork(beam);
            Assert.Equal(0.0, soffit);
            Assert.True(side >= 0.0);
        }

        [Fact]
        public void CalculateGrossVolume_ZeroDimensions_ReturnsZero()
        {
            var footing = new QsElementModel
            {
                MemberType = QsMemberType.Footing,
                WidthMm = 0,
                HeightMm = 800,
                LengthMm = 2000
            };
            double vol = QsTakeoffCalculator.CalculateGrossVolume(footing);
            Assert.Equal(0.0, vol);
        }
    }
}
