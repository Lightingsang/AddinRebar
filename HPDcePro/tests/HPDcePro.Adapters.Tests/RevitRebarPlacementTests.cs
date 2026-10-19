using System;
using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Column;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Geometry;
using HPDcePro.Revit;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public sealed class RevitRebarPlacementTests
    {
        [Fact]
        public void MapBeamRebar_GeneratesExpectedDescriptorsAndTags()
        {
            var beamResult = new ContinuousBeamResult
            {
                BeamMark = "D1",
                ContinuousTopBars = new List<RebarItem>
                {
                    new() { BarMark = "1", DiameterMm = 20, BarCount = 3, LengthMm = 6000, StartX = 0, EndX = 6000 }
                },
                ContinuousBottomBars = new List<RebarItem>
                {
                    new() { BarMark = "2", DiameterMm = 20, BarCount = 3, LengthMm = 6000, StartX = 0, EndX = 6000 }
                },
                StirrupZones = new List<StirrupZone>
                {
                    new() { SpanIndex = 0, StartX = 0, EndX = 1500, DiameterMm = 8, SpacingMm = 100 },
                    new() { SpanIndex = 0, StartX = 1500, EndX = 4500, DiameterMm = 8, SpacingMm = 200 }
                }
            };

            var placement = RevitRebarPlacementService.MapBeamRebar(beamResult, new Point3D(0, 0, 3600));

            Assert.True(placement.IsSuccess);
            Assert.Equal("D1", placement.ElementMark);
            Assert.Equal(4, placement.Rebars.Count); // Top, Bot, 2 Stirrup zones
            Assert.True(placement.TotalRebarCount > 6);
            Assert.True(placement.TotalWeightKg > 0);
            Assert.Contains(placement.PreviousRebarTagsToDelete, t => t.Contains("D1"));

            var topBar = placement.Rebars.First(r => r.BarMark == "Top_1");
            Assert.Equal(RevitShapeType.StandardHook90, topBar.ShapeType);
            Assert.NotNull(topBar.StartHook);
            Assert.Equal(90.0, topBar.StartHook!.HookAngleDeg);
            Assert.StartsWith("HPDcePro_Rebar_D1_", topBar.CommentsTag);
        }

        [Fact]
        public void MapColumnRebar_MultiStory_GeneratesCrankedAndTies()
        {
            var colOptions = new ColumnDetailingOptions
            {
                ColumnMark = "C1",
                Stories = new List<ColumnStoryDetail>
                {
                    new("Story1", 0.0, 3600.0, 400.0, 400.0, 400.0, 20, 8, 20, 4, 8, 100.0, 200.0),
                    new("Story2", 3600.0, 3600.0, 400.0, 400.0, 400.0, 20, 8, 20, 4, 8, 100.0, 200.0)
                }
            };

            var placement = RevitRebarPlacementService.MapColumnRebar(colOptions, new Point3D(0, 0, 0));

            Assert.True(placement.IsSuccess);
            Assert.Equal("C1", placement.ElementMark);
            Assert.Equal(4, placement.Rebars.Count); // 2 stories * (1 Main + 1 Tie)

            var mainBarStory1 = placement.Rebars.First(r => r.BarMark == "Story1_Main");
            Assert.Equal(RevitShapeType.CrankedLap, mainBarStory1.ShapeType);
            Assert.Equal(8, mainBarStory1.Quantity);
            Assert.Equal(4200.0, mainBarStory1.LengthMm); // 3600 + 600

            var tieStory1 = placement.Rebars.First(r => r.BarMark == "Story1_Tie");
            Assert.Equal(RevitShapeType.ClosedStirrup, tieStory1.ShapeType);
            Assert.Equal(18, tieStory1.Quantity); // 3600 / 200
        }

        [Fact]
        public void MapPileCapRebar_GeneratesMeshBottomLayers()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            var piles = new List<PileCoordinateItem>
            {
                new("P1", 400, 400),
                new("P2", 1400, 1400)
            };
            var capInput = new PileCapInput(
                "DC-1", 1800, 1800, 900, 400, 400, concrete, rebar, piles, 1200, 40, 30, 600);

            var capResult = new PileCapResult
            {
                Mark = "DC-1",
                PileCount = 2,
                IsSafe = true
            };

            var placement = RevitRebarPlacementService.MapPileCapRebar(capResult, capInput, new Point3D(0, 0, 0));

            Assert.True(placement.IsSuccess);
            Assert.Equal("DC-1", placement.ElementMark);
            Assert.Equal(2, placement.Rebars.Count);

            var botX = placement.Rebars.First(r => r.BarMark == "BottomX");
            Assert.Equal(RevitShapeType.StandardHook90, botX.ShapeType);
            Assert.Equal(12, botX.Quantity); // 1800 / 150 = 12
            Assert.NotNull(botX.StartHook);
            Assert.Equal(90.0, botX.StartHook!.HookAngleDeg);
        }

        [Fact]
        public void CoordinateConversion_FeetValuesAccurate()
        {
            var rebar = new RevitRebarDescriptor
            {
                DiameterMm = 25.4,
                LengthMm = 3048.0,
                SpacingMm = 152.4,
                CurvePointsMm = new List<Point3D>
                {
                    new(0, 0, 0),
                    new(3048.0, 0, 0)
                }
            };

            Assert.Equal(10.0, rebar.LengthFeet, precision: 4);
            Assert.Equal(0.5, rebar.SpacingFeet, precision: 4);
            Assert.Equal(1.0 / 12.0, rebar.DiameterFeet, precision: 4);

            var feetPoints = rebar.CurvePointsFeet.ToList();
            Assert.Equal(2, feetPoints.Count);
            Assert.Equal(10.0, feetPoints[1].X, precision: 4);
        }

        [Fact]
        public void IdempotentTagPrefix_BuildsConsistentPrefix()
        {
            string prefix = RevitRebarPlacementService.BuildIdempotentTagPrefix("B101");
            Assert.Equal("HPDcePro_Rebar_B101_", prefix);
        }

        [Fact]
        public void MapRebar_ThrowsOnNullArgument()
        {
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B20);
            var rebar = MaterialLibrary.GetRebar(RebarGrade.CB300V);

            Assert.Throws<ArgumentNullException>(() =>
                RevitRebarPlacementService.MapBeamRebar(null!, new Point3D(0, 0, 0)));

            Assert.Throws<ArgumentNullException>(() =>
                RevitRebarPlacementService.MapColumnRebar(null!, new Point3D(0, 0, 0)));

            Assert.Throws<ArgumentNullException>(() =>
                RevitRebarPlacementService.MapPileCapRebar(null!, new PileCapInput("M", 1000, 1000, 500, 300, 300, concrete, rebar, Array.Empty<PileCoordinateItem>(), 100, 10, 10, 200), new Point3D(0, 0, 0)));
        }

        [Fact]
        public void MapRebar_CalculatesValidTotalWeightAndQuantity()
        {
            var beamResult = new ContinuousBeamResult
            {
                BeamMark = "B-TEST",
                ContinuousTopBars = new List<RebarItem>
                {
                    new() { BarMark = "T1", DiameterMm = 18, BarCount = 2, LengthMm = 4000, StartX = 0, EndX = 4000 }
                }
            };

            var placement = RevitRebarPlacementService.MapBeamRebar(beamResult, new Point3D(0, 0, 0));
            Assert.Equal(2, placement.TotalRebarCount);
            Assert.True(placement.TotalWeightKg > 10.0);
        }
    }
}
