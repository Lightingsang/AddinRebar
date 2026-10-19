using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using HPDcePro.AutoCad.Jigs;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public class CadJigsGeometryTests
    {
        [Fact]
        public void VeMBKCJig_CalculateCorners_ReturnsValidBoundingPolygon()
        {
            var p1 = new Point3d(0, 0, 0);
            var p2 = new Point3d(4000, 0, 0);
            double width = 220.0;

            var (d1, d2, d3, d4, dText) = VeMBKCJig.CalculateCorners(p1, p2, width, "Mid");

            Assert.Equal(-110.0, d1.Y, 2);
            Assert.Equal(-110.0, d2.Y, 2);
            Assert.Equal(110.0, d3.Y, 2);
            Assert.Equal(110.0, d4.Y, 2);
            Assert.Equal(2000.0, dText.X, 2);

            // Alignment Top
            var top = VeMBKCJig.CalculateCorners(p1, p2, width, "Top");
            Assert.True(top.d1.Y <= 0.0);

            // Alignment Bot
            var bot = VeMBKCJig.CalculateCorners(p1, p2, width, "Bot");
            Assert.True(bot.d3.Y >= 0.0);
        }

        [Fact]
        public void JigSlabRebar_CalculateRebarVertices_ComputesHooksAndOffsets()
        {
            var p1 = new Point3d(0, 0, 0);
            var p2 = new Point3d(3000, 0, 0);

            var pts = JigSlabRebar.CalculateRebarVertices(p1, p2, 50.0, 100.0, 50.0, 100.0, 10);

            Assert.NotNull(pts);
            Assert.Equal(4, pts.Length);
            Assert.Equal(50.0, pts[1].X, 2);
            Assert.Equal(2950.0, pts[2].X, 2);
            Assert.Equal(-100.0, pts[0].Y, 2);
            Assert.Equal(-100.0, pts[3].Y, 2);
        }

        [Fact]
        public void TagLeaderJig_CalculateLeaderPoints_HandlesShelfAndMirror()
        {
            var anchor = new Point3d(100, 200, 0);
            var cursor = new Point3d(500, 600, 0);

            var (p1, p2, p3, textOrigin) = TagLeaderJig.CalculateLeaderPoints(anchor, cursor, 300.0, false);

            Assert.Equal(anchor, p1);
            Assert.Equal(cursor, p2);
            Assert.Equal(800.0, p3.X, 2);
            Assert.True(textOrigin.Y > cursor.Y);

            // Mirrored
            var mirrored = TagLeaderJig.CalculateLeaderPoints(anchor, cursor, 300.0, true);
            Assert.Equal(200.0, mirrored.p3.X, 2);
        }

        [Fact]
        public void TagChiaThepJig_CalculateDistributionTicks_DividesSpan()
        {
            var p1 = new Point3d(0, 0, 0);
            var p2 = new Point3d(2000, 0, 0);

            var (lineStart, lineEnd, ticks, count) = TagChiaThepJig.CalculateDistributionTicks(p1, p2, 200.0, 100.0);

            Assert.Equal(p1, lineStart);
            Assert.Equal(p2, lineEnd);
            Assert.Equal(10, count);
            Assert.Equal(11, ticks.Count);
        }

        [Fact]
        public void JigRaiThepChamTron_CalculateDotPositions_FiltersEndsCorrectly()
        {
            var p1 = new Point3d(0, 0, 0);
            var p2 = new Point3d(1500, 0, 0);

            var allDots = JigRaiThepChamTron.CalculateDotPositions(p1, p2, 50.0, 50.0, 30.0, 150.0, false, false);
            Assert.NotEmpty(allDots);

            var boDauDots = JigRaiThepChamTron.CalculateDotPositions(p1, p2, 50.0, 50.0, 30.0, 150.0, true, false);
            Assert.Equal(allDots.Count - 1, boDauDots.Count);
        }

        [Fact]
        public void JigVeDaiVong_CalculateHoopGeometry_ComputesHooks()
        {
            var center = new Point3d(1000, 1000, 0);
            var cursor = new Point3d(1400, 1000, 0);

            var (c, radius, hook1, hook2) = JigVeDaiVong.CalculateHoopGeometry(center, cursor, 150.0, 12);

            Assert.Equal(center, c);
            Assert.Equal(400.0, radius, 2);
            Assert.NotEqual(hook1, hook2);
        }

        [Fact]
        public void JigVeDaiC_CalculateCTiePoints_GeneratesFourVertices()
        {
            var p1 = new Point3d(0, 0, 0);
            var p2 = new Point3d(500, 0, 0);

            var pts = JigVeDaiC.CalculateCTiePoints(p1, p2, 80.0, 80.0, 10);

            Assert.Equal(4, pts.Length);
            Assert.Equal(p1, pts[1]);
            Assert.Equal(p2, pts[2]);
            Assert.NotEqual(0.0, pts[0].Y);
            Assert.NotEqual(0.0, pts[3].Y);
        }

        [Fact]
        public void VePolylineBatKyJig_CalculatesVerticesAndFillet()
        {
            var fixedPts = new List<Point3d> { new Point3d(0, 0, 0), new Point3d(1000, 0, 0) };
            var cursor = new Point3d(1000, 1000, 0);

            var vertices = VePolylineBatKyJig.CalculatePolylineVertices(fixedPts, cursor);
            Assert.Equal(3, vertices.Count);

            var (filletPts, bulges) = VePolylineBatKyJig.CalculateFilletedRebar(vertices, 50.0);
            Assert.NotEmpty(filletPts);
            Assert.Equal(filletPts.Count, bulges.Count);
        }

        [Fact]
        public void JigVeThepVachCt_CalculateBoundaryCage_ProducesWallAndCages()
        {
            var p1 = new Point3d(0, 0, 0);
            var p2 = new Point3d(4000, 0, 0);

            var (wall, leftCage, rightCage, dots) = JigVeThepVachCt.CalculateBoundaryCage(
                p1, p2, 300.0, 600.0, 30.0, 6, 8);

            Assert.Equal(4, wall.Length);
            Assert.Equal(4, leftCage.Length);
            Assert.Equal(4, rightCage.Length);
            Assert.NotEmpty(dots);
        }

        [Fact]
        public void JigVeVachHam_CalculateRetainingWallSection_ProducesWaterstop()
        {
            var basePt = new Point3d(0, 0, 0);

            var (outline, ws, eStart, eEnd, iStart, iEnd) = JigVeVachHam.CalculateRetainingWallSection(
                basePt, 3500.0, 400.0, 40.0, 30.0, true);

            Assert.Equal(4, outline.Length);
            Assert.NotEmpty(ws);
            Assert.True(eStart.Y < eEnd.Y);
            Assert.True(iStart.Y < iEnd.Y);
        }

        [Fact]
        public void DimNhanhMbkcJig_CalculateDimensionChains_GeneratesTiers()
        {
            var p1 = new Point3d(0, 0, 0);
            var p2 = new Point3d(6000, 0, 0);
            var intermediates = new[] { new Point3d(2000, 0, 0), new Point3d(4000, 0, 0) };
            var dimPt = new Point3d(3000, 500, 0);

            var (tier1, tier2) = DimNhanhMbkcJig.CalculateDimensionChains(
                p1, p2, intermediates, dimPt, 300.0, true);

            Assert.Equal(3, tier1.Count);
            Assert.NotNull(tier2);
            Assert.Equal(p1, tier2.Value.s);
            Assert.Equal(p2, tier2.Value.e);
        }
    }
}
