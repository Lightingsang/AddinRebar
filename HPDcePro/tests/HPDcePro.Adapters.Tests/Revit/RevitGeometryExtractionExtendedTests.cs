using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;
using HPDcePro.Revit;
using HPDcePro.Revit.Generators;
using Xunit;

namespace HPDcePro.Adapters.Tests.Revit
{
    public sealed class RevitGeometryExtractionExtendedTests
    {
        [Fact]
        public void GridGenerator_GeneratesAccurateSpacingsAndFeetCoordinates()
        {
            var defs = new List<GridItemDef>
            {
                new("1", new Point3D(0, 0, 0), new Point3D(0, 10000, 0)),
                new("2", new Point3D(4000, 0, 0), new Point3D(4000, 10000, 0)),
                new("3", new Point3D(8000, 0, 0), new Point3D(8000, 10000, 0))
            };

            var plan = RevitGridGenerator.CreatePlan(defs);
            Assert.Equal(3, plan.Items.Count);
            Assert.Equal(30.0, plan.TotalLengthM, 2);

            var first = plan.Items[0];
            Assert.Equal(0.0, first.StartFeet.X, 3);
            Assert.Equal(10000.0 / 304.8, first.EndFeet.Y, 3);

            double spacing = RevitGridGenerator.CalculateGridSpacing(defs[0].StartMm, defs[1].StartMm);
            Assert.Equal(4000.0, spacing, 2);

            var names = RevitGridGenerator.GenerateAxisNames(4, true);
            Assert.Equal(new[] { "A", "B", "C", "D" }, names);
        }

        [Fact]
        public void FramingGenerator_CalculatesLengthVolumeAndBreaksSpans()
        {
            var defs = new List<BeamItemDef>
            {
                new("D1", new Point3D(0, 0, 0), new Point3D(6000, 0, 0), 300, 600, "L2"),
                new("D2", new Point3D(6000, 0, 0), new Point3D(12000, 0, 0), 300, 600, "L2")
            };

            var plan = RevitFramingGenerator.CreatePlan(defs);
            Assert.Equal(2, plan.Items.Count);
            Assert.Equal(12.0, plan.TotalLengthM, 2);
            // 0.3 * 0.6 * 12.0 = 2.16 m3
            Assert.Equal(2.16, plan.TotalConcreteVolumeM3, 3);

            var contBeam = new ContinuousBeamDef("D_Main", new Point3D(0, 0, 0), new Point3D(12000, 0, 0), 300, 600, "L2");
            // Pass duplicate collinear identical support points to verify deduplication resilience
            var supports = new[] { new Point3D(6000, 0, 0), new Point3D(6000, 0, 0), new Point3D(6000, 0, 0) };
            var breakPlan = RevitFramingGenerator.BreakBeamAtSupports(contBeam, supports);

            Assert.Equal(2, breakPlan.Spans.Count);
            Assert.Equal(6000.0, breakPlan.Spans[0].SpanLengthMm, 1);
            Assert.Equal(6000.0, breakPlan.Spans[1].SpanLengthMm, 1);
        }

        [Fact]
        public void ColumnGenerator_ConvertsOffsetsAndRadiansCorrectly()
        {
            var defs = new List<ColumnItemDef>
            {
                new("C1", new Point3D(0, 0, 0), 400, 400, 90.0, "L1", "L2", 0, 0, 3300.0),
                new("C2", new Point3D(4000, 0, 0), 400, 400, 0.0, "L1", "L2", 0, 0, 3300.0)
            };

            var plan = RevitColumnGenerator.CreatePlan(defs);
            Assert.Equal(2, plan.TotalCount);
            Assert.Equal(Math.PI / 2.0, plan.Items[0].RotationRad, 3);
            Assert.Equal(0.0, plan.Items[1].RotationRad, 3);
            // 2 * 0.4 * 0.4 * 3.3 = 1.056 m3
            Assert.Equal(1.056, plan.TotalVolumeM3, 3);
        }

        [Fact]
        public void SlabWallGenerator_CalculatesPolygonAreasAndVolumesAccurately()
        {
            var slabBoundary = new List<Point3D>
            {
                new(0, 0, 0),
                new(5000, 0, 0),
                new(5000, 4000, 0),
                new(0, 4000, 0)
            };
            var opening = new List<Point3D>
            {
                new(1000, 1000, 0),
                new(2000, 1000, 0),
                new(2000, 2000, 0),
                new(1000, 2000, 0)
            };

            var slabDefs = new[] { new SlabItemDef("S1", slabBoundary, new[] { opening }, 120.0, "L2") };
            var slabPlan = RevitSlabWallGenerator.CreateSlabPlan(slabDefs);

            // Gross = 20 m2, opening = 1 m2, net = 19 m2. Vol = 19 * 0.12 = 2.28 m3
            Assert.Equal(19.0, slabPlan.TotalAreaM2, 2);
            Assert.Equal(2.28, slabPlan.TotalVolumeM3, 3);

            var wallDefs = new[] { new WallItemDef("W1", new Point3D(0, 0, 0), new Point3D(4000, 0, 0), 250.0, 3300.0, "L1", "L2") };
            var wallPlan = RevitSlabWallGenerator.CreateWallPlan(wallDefs);

            // Wall: 4m * 3.3m = 13.2 m2, vol = 13.2 * 0.25 = 3.3 m3
            Assert.Equal(4.0, wallPlan.TotalLengthM, 2);
            Assert.Equal(3.3, wallPlan.TotalVolumeM3, 3);
        }

        [Fact]
        public void JoinCutPlanner_EnforcesHierarchyColumnOverWallOverBeamOverSlab()
        {
            var colBBox = new BoundingBox3D(new Point3D(-200, -200, 0), new Point3D(200, 200, 3300));
            var beamBBox = new BoundingBox3D(new Point3D(0, -150, 2700), new Point3D(3000, 150, 3300));
            var slabBBox = new BoundingBox3D(new Point3D(-500, -500, 3180), new Point3D(3500, 3500, 3300));

            var elements = new[]
            {
                new StructuralElementProxy("Slab_1", StructuralCategoryRank.Slab, slabBBox),
                new StructuralElementProxy("Beam_1", StructuralCategoryRank.Beam, beamBBox),
                new StructuralElementProxy("Col_1", StructuralCategoryRank.Column, colBBox)
            };

            var joins = RevitJoinCutPlanner.DetermineJoins(elements);
            Assert.NotEmpty(joins);

            // Column joins Beam -> Column must be Primary
            var colBeamJoin = System.Linq.Enumerable.FirstOrDefault(joins, j => (j.PrimaryId == "Col_1" && j.SecondaryId == "Beam_1") || (j.PrimaryId == "Beam_1" && j.SecondaryId == "Col_1"));
            Assert.NotNull(colBeamJoin);
            Assert.Equal("Col_1", colBeamJoin.PrimaryId);

            // Beam joins Slab -> Beam must be Primary
            var beamSlabJoin = System.Linq.Enumerable.FirstOrDefault(joins, j => (j.PrimaryId == "Beam_1" && j.SecondaryId == "Slab_1") || (j.PrimaryId == "Slab_1" && j.SecondaryId == "Beam_1"));
            Assert.NotNull(beamSlabJoin);
            Assert.Equal("Beam_1", beamSlabJoin.PrimaryId);
        }

        [Fact]
        public void NamingAndAnalyticalGenerators_ProduceConsistentOutput()
        {
            var beams = new[]
            {
                new BeamNamingItem("ID_1", "Old_1", new Point3D(0, 4000, 0), new Point3D(6000, 4000, 0), "T2"),
                new BeamNamingItem("ID_2", "Old_2", new Point3D(0, 0, 0), new Point3D(6000, 0, 0), "T2")
            };

            var names = RevitNamingGenerator.GenerateNames(beams, "D");
            Assert.Equal(2, names.Count);
            Assert.Equal("T2D-1", names[0].ProposedMark);
            Assert.Equal("T2D-2", names[1].ProposedMark);

            var members = new[]
            {
                new AnalyticalMemberDef("M1", "Framing", new Point3D(0, 0, 0), new Point3D(5000, 0, 0)),
                new AnalyticalMemberDef("M2", "Framing", new Point3D(5005, 0, 0), new Point3D(10000, 0, 0))
            };

            var analytical = RevitAnalyticalGenerator.GenerateAnalyticalModel(members, 20.0);
            Assert.Equal(2, analytical.Members.Count);
            // Points (5000,0,0) and (5005,0,0) must merge into 1 joint
            Assert.Equal(3, analytical.Nodes.Count);
        }

        [Fact]
        public void TakeoffGenerator_SummarizesConcreteFormworkAndThreeSteelTiers()
        {
            var framings = new[] { new RevitFramingElement { LengthMm = 6000.0, WidthMm = 300.0, HeightMm = 600.0 } };
            var columns = new[] { new RevitColumnElement { WidthMm = 400.0, HeightMm = 400.0 } };
            var rebars = new[]
            {
                new RevitRebarDescriptor { DiameterMm = 8.0, LengthMm = 1500.0, Quantity = 10 },   // <=10 small
                new RevitRebarDescriptor { DiameterMm = 16.0, LengthMm = 6000.0, Quantity = 4 },   // 12-18 medium
                new RevitRebarDescriptor { DiameterMm = 22.0, LengthMm = 6000.0, Quantity = 4 }    // >18 large
            };

            var report = RevitTakeoffGenerator.GenerateReport(framings, columns, null, null, rebars);
            Assert.True(report.TotalConcreteM3 > 0.0);
            Assert.True(report.TotalFormworkM2 > 0.0);
            Assert.True(report.SteelDiaSmallKg > 0.0);
            Assert.True(report.SteelDiaMediumKg > 0.0);
            Assert.True(report.SteelDiaLargeKg > 0.0);
            Assert.True(report.TotalSteelKg > report.SteelDiaSmallKg);
            Assert.Contains("BẢNG BÓC TÁCH KHỐI LƯỢNG", report.FormattedTextTable);
        }
    }
}
