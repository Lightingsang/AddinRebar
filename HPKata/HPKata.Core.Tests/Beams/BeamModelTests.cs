using System;
using HPKata.Core.Beams.Models;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class BeamModelTests
    {
        [Fact]
        public void BeamSpanModel_ValidGeometry_CalculatesCenterToCenter()
        {
            var span = new BeamSpanModel
            {
                SpanIndex = 0,
                SpanName = "N1",
                ClearLengthMm = 6000.0,
                LeftSupportWidthMm = 400.0,
                RightSupportWidthMm = 400.0,
                WidthMm = 300.0,
                HeightMm = 600.0
            };

            span.Validate();
            Assert.Equal(6400.0, span.CenterToCenterLengthMm);
        }

        [Theory]
        [InlineData(0, 300, 600)]
        [InlineData(5000, 0, 600)]
        [InlineData(5000, 300, 0)]
        public void BeamSpanModel_InvalidDimensions_ThrowsArgumentException(double len, double w, double h)
        {
            var span = new BeamSpanModel
            {
                ClearLengthMm = len,
                WidthMm = w,
                HeightMm = h
            };

            Assert.Throws<ArgumentException>(() => span.Validate());
        }

        [Fact]
        public void BeamSectionModel_EffectiveDepth_CalculatesH0()
        {
            var sec = new BeamSectionModel
            {
                WidthMm = 300.0,
                HeightMm = 600.0,
                ConcreteCoverMainMm = 30.0
            };

            sec.Validate();
            Assert.Equal(570.0, sec.EffectiveDepthH0Mm);
        }

        [Fact]
        public void BeamSupportModel_ExteriorCheck_IdentifiesExteriorSupports()
        {
            var left = new BeamSupportModel { SupportType = BeamSupportType.ExteriorLeft };
            var mid = new BeamSupportModel { SupportType = BeamSupportType.Interior };
            var right = new BeamSupportModel { SupportType = BeamSupportType.ExteriorRight };

            Assert.True(left.IsExterior);
            Assert.False(mid.IsExterior);
            Assert.True(right.IsExterior);
        }

        [Fact]
        public void TieBeamModel_AnchorageCalculation_Returns40Dia()
        {
            var tie = new TieBeamModel
            {
                ClearSpanMm = 4500.0,
                WidthMm = 300.0,
                HeightMm = 500.0,
                AnchorageMultiplier = 40.0
            };

            tie.Validate();
            double anchor = tie.CalculateAnchorageLengthMm(20);
            Assert.Equal(800.0, anchor);
        }

        [Fact]
        public void InclinedCurvedBeamModel_Inclined_CalculatesSlopeAndAngle()
        {
            var inclined = new InclinedCurvedBeamModel
            {
                IsInclined = true,
                HorizontalRunMm = 4000.0,
                ElevationDeltaMm = 3000.0,
                WidthMm = 250.0,
                HeightMm = 400.0
            };

            inclined.Validate();
            Assert.Equal(5000.0, inclined.SlopeLengthMm);
            Assert.True(inclined.SlopeAngleDeg > 36.0 && inclined.SlopeAngleDeg < 37.0);
        }

        [Fact]
        public void InclinedCurvedBeamModel_Curved_CalculatesArcLength()
        {
            var curved = new InclinedCurvedBeamModel
            {
                IsCurved = true,
                RadiusMm = 6000.0,
                SweepAngleDeg = 90.0,
                WidthMm = 300.0,
                HeightMm = 600.0
            };

            curved.Validate();
            double expected = Math.PI * 6000.0 * 90.0 / 180.0;
            Assert.Equal(expected, curved.ArcLengthMm, 2);
        }
    }
}
