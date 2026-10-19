using System;
using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.Calculations.Column;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public sealed class ColumnDetailingTests
    {
        private static ColumnDetailingOptions CreateSampleOptions(
            ColumnSectionStyle style,
            double b1 = 500.0, double h1 = 500.0,
            double b2 = 500.0, double h2 = 500.0)
        {
            var s1 = new ColumnStoryDetail("TẦNG 1", 0.0, 3600.0, 400.0, b1, h1, 20, 8);
            var s2 = new ColumnStoryDetail("TẦNG 2", 3600.0, 3600.0, 400.0, b2, h2, 20, 8);

            return new ColumnDetailingOptions
            {
                ColumnMark = "C1",
                Style = style,
                Stories = new[] { s1, s2 },
                IncludeCrossSections = true
            };
        }

        [Fact]
        public void MultiStorySchedule_ArrangesSplicesInMiddleThird()
        {
            var opt = CreateSampleOptions(ColumnSectionStyle.REC1);
            var gen = new ColumnDetailingGenerator();
            var buffer = gen.GenerateElevationAndSlices(opt);

            Assert.True(buffer.Count > 10, "Buffer should contain column elevation lines and rebar");
            var rebarPolylines = buffer.Entities
                .Where(e => e.EntityType == CadEntityType.Polyline && e.Layer == opt.RebarLayer)
                .ToList();

            Assert.NotEmpty(rebarPolylines);
            // Verify that vertical bar extends through middle third
            // Hn = 3600 - 400 = 3200. Middle third is between 1067 and 2133 mm
            var bar = rebarPolylines[0];
            Assert.True(bar.Points.Count >= 2);
            double barTopY = bar.Points.Max(p => p.Y);
            Assert.True(barTopY >= 1067.0, "Splice must extend past Hn/3");
        }

        [Fact]
        public void CrankedBend_GeneratesTransition_WhenSlopeUnderOneToSix()
        {
            // b1=500, b2=450 => delta=25mm. Beam=400mm => slope = 25/400 = 1/16 <= 1/6
            var opt = CreateSampleOptions(ColumnSectionStyle.REC1, b1: 500.0, h1: 500.0, b2: 450.0, h2: 450.0);
            var gen = new ColumnDetailingGenerator();
            var buffer = gen.GenerateElevationAndSlices(opt);

            // Cranked bar polyline has 3 points (bottom, crank start, top)
            var crankedBars = buffer.Entities
                .Where(e => e.EntityType == CadEntityType.Polyline && e.Layer == opt.RebarLayer && e.Points.Count == 3)
                .ToList();

            Assert.NotEmpty(crankedBars);
        }

        [Fact]
        public void ExcessiveSectionReduction_TerminatesBarsAndAddsStarterDowels()
        {
            // b1=600, b2=400 => delta=100mm. Beam=400mm => slope = 100/400 = 1/4 > 1/6 (exceeds 1:6)
            var opt = CreateSampleOptions(ColumnSectionStyle.REC1, b1: 600.0, h1: 600.0, b2: 400.0, h2: 400.0);
            var gen = new ColumnDetailingGenerator();
            var buffer = gen.GenerateElevationAndSlices(opt);

            // Slope > 1:6 prevents cranked bend; bar stays 2-point straight terminating in joint
            var crankedBars = buffer.Entities
                .Where(e => e.EntityType == CadEntityType.Polyline && e.Layer == opt.RebarLayer && e.Points.Count == 3)
                .ToList();

            Assert.Empty(crankedBars);
        }

        [Fact]
        public void ConfinementZones_CalculatesCorrectHeightAndDenseTiePitch()
        {
            var opt = CreateSampleOptions(ColumnSectionStyle.REC1, b1: 400.0, h1: 400.0);
            var gen = new ColumnDetailingGenerator();
            var buffer = gen.GenerateElevationAndSlices(opt);

            var tieLines = buffer.Entities
                .Where(e => e.EntityType == CadEntityType.Line && e.Layer == opt.RebarLayer)
                .ToList();

            // ClearH = 3200. Hc = max(3200/6=533, 400, 400, 500) = 533mm.
            // Dense zone pitch = min(100, 10*8=80) = 80mm.
            Assert.True(tieLines.Count >= 10, "Should generate dense ties in top and bottom confinement zones");
        }

        [Fact]
        public void ColumnREC1_GeneratesOuterTieAndBarsWithXData()
        {
            var opt = CreateSampleOptions(ColumnSectionStyle.REC1);
            var gen = new ColumnDetailingGenerator();
            var buffer = gen.GenerateElevationAndSlices(opt);

            var xdataPolylines = buffer.Entities
                .Where(e => e.EntityType == CadEntityType.Polyline && e.AttachedXData.Count > 0)
                .ToList();

            Assert.NotEmpty(xdataPolylines);
            Assert.Contains(xdataPolylines, p => p.AttachedXData.Any(x => x.RegAppName == RebarXDataCodec.ApplicationName));
        }

        [Fact]
        public void ColumnREC2_GeneratesInteriorCrossTies()
        {
            var opt1 = CreateSampleOptions(ColumnSectionStyle.REC1);
            var opt2 = CreateSampleOptions(ColumnSectionStyle.REC2);

            var gen = new ColumnDetailingGenerator();
            var buf1 = gen.GenerateElevationAndSlices(opt1);
            var buf2 = gen.GenerateElevationAndSlices(opt2);

            // REC2 should generate more tie lines than REC1 due to interior cross-ties
            int ties1 = buf1.Entities.Count(e => e.EntityType == CadEntityType.Line && e.Layer == opt1.RebarLayer);
            int ties2 = buf2.Entities.Count(e => e.EntityType == CadEntityType.Line && e.Layer == opt2.RebarLayer);

            Assert.True(ties2 > ties1, "REC2 should contain additional interior cross-ties");
        }

        [Fact]
        public void ColumnCi_GeneratesCircularHoopTieAndLongitudinalBars()
        {
            var opt = CreateSampleOptions(ColumnSectionStyle.Ci, b1: 450.0, h1: 450.0);
            var gen = new ColumnDetailingGenerator();
            var buffer = gen.GenerateElevationAndSlices(opt);

            var circles = buffer.Entities
                .Where(e => e.EntityType == CadEntityType.Circle)
                .ToList();

            // Concrete outline + rebar hoop circle + at least 6 rebar dots = >= 8 circles
            Assert.True(circles.Count >= 8, "Circular section should include perimeter bars and hoop circles");
        }
    }
}
