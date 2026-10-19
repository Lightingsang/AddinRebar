using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Column;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Geometry;
using HPDcePro.Revit;
using HPDcePro.Revit.Generators;
using Xunit;

namespace HPDcePro.Adapters.Tests.Revit
{
    public sealed class RevitRebarPlacementExtendedTests
    {
        [Fact]
        public void MapBeamRebar_GeneratesRebarDescriptorsAndIdempotentTags()
        {
            var beamResult = new ContinuousBeamResult
            {
                BeamMark = "D101",
                ContinuousTopBars = new List<RebarItem>
                {
                    new() { BarMark = "1", DiameterMm = 20, LengthMm = 6500.0, BarCount = 2, StartX = 0, EndX = 6500.0 }
                },
                ContinuousBottomBars = new List<RebarItem>
                {
                    new() { BarMark = "2", DiameterMm = 20, LengthMm = 6500.0, BarCount = 3, StartX = 0, EndX = 6500.0 }
                },
                StirrupZones = new List<StirrupZone>
                {
                    new() { SpanIndex = 1, DiameterMm = 8.0, SpacingMm = 100.0, StartX = 0, EndX = 1500.0 },
                    new() { SpanIndex = 1, DiameterMm = 8.0, SpacingMm = 200.0, StartX = 1500.0, EndX = 4500.0 },
                    new() { SpanIndex = 1, DiameterMm = 8.0, SpacingMm = 100.0, StartX = 4500.0, EndX = 6000.0 }
                }
            };

            var placement = RevitRebarPlacementService.MapBeamRebar(beamResult, new Point3D(0, 0, 0));
            Assert.True(placement.IsSuccess);
            Assert.True(placement.TotalRebarCount > 0);
            Assert.True(placement.TotalWeightKg > 0.0);
            Assert.NotEmpty(placement.Rebars);

            string tagPrefix = RevitRebarPlacementService.BuildIdempotentTagPrefix("D101");
            Assert.Equal("HPDcePro_Rebar_D101_", tagPrefix);
            foreach (var rebar in placement.Rebars)
            {
                Assert.StartsWith(tagPrefix, rebar.CommentsTag);
            }
        }

        [Fact]
        public void MapColumnRebar_GeneratesMainAndTieDescriptors()
        {
            var colOptions = new ColumnDetailingOptions
            {
                ColumnMark = "C1",
                Stories = new List<ColumnStoryDetail>
                {
                    new("T1", 0.0, 3300.0, 400.0, 400.0, 400.0, 20, 8, 20, 4, 8, 100.0, 200.0)
                }
            };

            var placement = RevitRebarPlacementService.MapColumnRebar(colOptions, new Point3D(0, 0, 0));
            Assert.True(placement.IsSuccess);
            Assert.Contains(placement.Rebars, r => r.ShapeType == RevitShapeType.CrankedLap);
            Assert.Contains(placement.Rebars, r => r.ShapeType == RevitShapeType.ClosedStirrup);
        }

        [Fact]
        public void MapPileCapRebar_GeneratesOrthogonalMeshDescriptors()
        {
            var capResult = new PileCapResult { Mark = "DC1" };
            var capInput = new PileCapInput(
                "DC1", 2000, 2000, 900, 400, 400,
                MaterialLibrary.GetConcrete(ConcreteGrade.B25),
                MaterialLibrary.GetRebar(RebarGrade.CB400V),
                new List<PileCoordinateItem> { new("P1", -500, -500), new("P2", 500, 500) },
                1600.0, 60.0, 40.0, 600.0);

            var placement = RevitRebarPlacementService.MapPileCapRebar(capResult, capInput, new Point3D(0, 0, 0));
            Assert.True(placement.IsSuccess);
            Assert.Equal(2, placement.Rebars.Count); // BottomX and BottomY
            Assert.All(placement.Rebars, r => Assert.Equal(RevitShapeType.StandardHook90, r.ShapeType));
        }

        [Fact]
        public void WallRebarGenerator_GeneratesTwoLayersVerticalAndHorizontal()
        {
            var input = new WallRebarInput("W1", 250.0, 4000.0, 3300.0);
            var res = WallRebarGenerator.GenerateWallRebar(input, new Point3D(0, 0, 0));

            Assert.True(res.IsSuccess);
            Assert.Equal(4, res.Rebars.Count); // Vert L1, Vert L2, Horiz L1, Horiz L2
            Assert.Contains(res.Rebars, r => r.BarMark == "Wall_Vert_Layer1");
            Assert.Contains(res.Rebars, r => r.BarMark == "Wall_Horiz_Layer1");
        }

        [Fact]
        public void SlabRebarGenerator_GeneratesTwoWayOrthogonalMesh()
        {
            var input = new SlabRebarInput("S1", new[] { new Point3D(0, 0, 0), new Point3D(4000, 0, 0) }, 120.0);
            var res = SlabRebarGenerator.GenerateSlabRebar(input, new Point3D(0, 0, 0));

            Assert.True(res.IsSuccess);
            Assert.Equal(2, res.Rebars.Count); // Bot X and Bot Y
            Assert.True(res.TotalRebarCount > 10);

            // Edge-case resilience: Degenerate span where span < 2 * cover clamps bar lengths to 100.0mm
            var degenInput = new SlabRebarInput("S_Degen", new[] { new Point3D(0, 0, 0) }, 100.0, SpanXMm: 30.0, SpanYMm: 30.0, CoverMm: 20.0);
            var degenRes = SlabRebarGenerator.GenerateSlabRebar(degenInput, new Point3D(0, 0, 0));
            Assert.True(degenRes.IsSuccess);
            Assert.All(degenRes.Rebars, r => Assert.Equal(100.0, r.LengthMm));
        }

        [Fact]
        public void RebarTags_ConformToIdempotentRegexPattern()
        {
            var regex = new Regex(@"^HPDcePro_Rebar_[A-Za-z0-9\-_]+_");
            string[] testMarks = { "D1", "C-2", "W_Core", "DC_01" };

            foreach (var mark in testMarks)
            {
                string tag = RevitRebarPlacementService.BuildIdempotentTagPrefix(mark);
                Assert.Matches(regex, tag);
            }
        }

        [Fact]
        public void CurvePointsFeet_ConvertsAccuratelyFromMillimeters()
        {
            var desc = new RevitRebarDescriptor
            {
                CurvePointsMm = new List<Point3D>
                {
                    new(304.8, 609.6, 914.4)
                }
            };

            var feet = desc.CurvePointsFeet.ToList();
            Assert.Single(feet);
            Assert.Equal(1.0, feet[0].X, 4);
            Assert.Equal(2.0, feet[0].Y, 4);
            Assert.Equal(3.0, feet[0].Z, 4);
        }
    }
}
