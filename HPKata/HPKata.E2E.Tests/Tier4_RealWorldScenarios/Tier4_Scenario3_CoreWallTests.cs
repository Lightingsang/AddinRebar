using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Walls.Calculators;
using HPKata.Core.Walls.Models;
using HPKata.Core.Walls.Persistence;
using Xunit;

namespace HPKata.E2E.Tests.Tier4_RealWorldScenarios
{
    /// <summary>
    /// E2E Scenario: Elevator Core Wall Detailing (ve_loi_vach, ve_vach, ve_vach_shop).
    /// Exercises boundary element cages, double-curtain web mesh, door opening trimmer bars,
    /// schedule aggregation, and ClosedXML export.
    /// </summary>
    public sealed class Tier4_Scenario3_CoreWallTests
    {
        [Fact]
        public void ElevatorCoreWall_EndToEndDetailing_GeneratesCompliantModelAndWorkbook()
        {
            var wall = BuildSampleCoreWallModel();

            // Verify Trimmers
            var trimmers = WallOpeningTrimmerCalculator.CalculateTrimmers(wall.Openings[0]);
            Assert.Equal(8, trimmers.DiagonalBarCount);
            Assert.Equal(4, trimmers.JambExtraBarCount);

            // Verify Schedule Aggregation
            var items = WallScheduleAggregator.AggregateSchedule(wall);
            Assert.True(items.Count > 0);

            // Verify Excel Workbook Generation
            using var ms = new MemoryStream();
            WallPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<ShearWallModel> { wall }, ms);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet("Vach");
            Assert.NotNull(ws);
            Assert.Equal("CORE-ELEV-1", ws.Cell(3, 3).GetString());
            Assert.Equal(350.0, ws.Cell(3, 4).GetDouble());
        }

        private static ShearWallModel BuildSampleCoreWallModel()
        {
            var wall = new ShearWallModel
            {
                WallMark = "CORE-ELEV-1",
                ShapeType = WallShapeType.C_Shape,
                ThicknessMm = 350.0,
                LengthMm = 3600.0
            };

            for (int s = 0; s < 3; s++)
            {
                wall.Stories.Add(new WallStorySegment
                {
                    StoryIndex = s, StoryName = $"Tầng {s + 1}",
                    BottomElevationMm = s * 3600.0, StoryHeightMm = 3600.0,
                    ThicknessMm = 350.0, LengthMm = 3600.0
                });
            }

            wall.BoundaryElements.Add(WallBoundaryCalculator.CreateBoundaryElement("BE-L", WallBoundaryPosition.Left, 3600.0, 350.0, 3000.0));
            wall.BoundaryElements.Add(WallBoundaryCalculator.CreateBoundaryElement("BE-R", WallBoundaryPosition.Right, 3600.0, 350.0, 3000.0));

            wall.WebReinforcement = new WallWebReinforcementModel
            {
                IsDoubleCurtain = true,
                VerticalBarDiameterMm = 14, VerticalBarSpacingMm = 200.0,
                HorizontalBarDiameterMm = 14, HorizontalBarSpacingMm = 200.0,
                CrossTieDiameterMm = 8
            };

            wall.Openings.Add(new WallOpeningModel
            {
                OpeningMark = "DOOR-ELEV",
                XOffsetMm = 1200.0, YOffsetMm = 0.0, WidthMm = 1200.0, HeightMm = 2400.0,
                HasDiagonalBars = true, DiagonalBarDiameterMm = 16, DiagonalBarsPerCorner = 2
            });

            return wall;
        }
    }
}
