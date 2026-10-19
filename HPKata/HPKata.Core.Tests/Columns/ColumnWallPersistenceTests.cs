using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Columns.Models;
using HPKata.Core.Columns.Persistence;
using HPKata.Core.Walls.Models;
using HPKata.Core.Walls.Persistence;
using Xunit;

namespace HPKata.Core.Tests.Columns
{
    public class ColumnWallPersistenceTests
    {
        [Fact]
        public void ColumnScheduleAggregator_AggregatesAllBarsAndTies()
        {
            var layout = new ColumnRebarLayout { ColumnMark = "C1" };
            layout.Stories.Add(new ColumnStorySegment
            {
                StoryIndex = 0,
                StoryName = "Tầng 1",
                WidthMm = 400.0,
                DepthMm = 400.0,
                StoryHeightMm = 3600.0,
                BeamDepthMm = 600.0,
                TieDistribution = new ColumnTieDistributionModel { DenseSpacingMm = 100.0, SparseSpacingMm = 200.0 }
            });
            layout.LongitudinalBars.Add(new ColumnBarRunModel
            {
                BarMark = "1",
                DiameterMm = 20,
                LengthMm = 3600.0,
                HasBottomHook = true,
                BottomHookLengthMm = 250.0
            });
            layout.FoundationDowels.Add(new ColumnStarterDowelModel
            {
                DowelMark = "DW1",
                DiameterMm = 20,
                Count = 4,
                EmbedmentDepthMm = 800.0,
                ProjectionLengthMm = 900.0
            });

            var items = ColumnScheduleAggregator.AggregateSchedule(layout);
            Assert.True(items.Count >= 3);
            Assert.Contains(items, i => i.ShapeCode == "02"); // Hooked bar
            Assert.Contains(items, i => i.ShapeCode == "51"); // Tie hoop
        }

        [Fact]
        public void ColumnPlanScheduleWorkbookGenerator_GeneratesValidExcelWorksheet()
        {
            var layout = new ColumnRebarLayout { ColumnMark = "C1" };
            layout.Stories.Add(new ColumnStorySegment
            {
                StoryIndex = 0,
                StoryName = "Tầng 1",
                WidthMm = 400.0,
                DepthMm = 400.0,
                Section = new ColumnSectionModel { CornerDiameterMm = 20, SideCountB = 1, SideDiameterB = 18, SideCountH = 1, SideDiameterH = 18 },
                TieDistribution = new ColumnTieDistributionModel { TieDiameterMm = 8, DenseSpacingMm = 100.0, SparseSpacingMm = 200.0, JointSpacingMm = 150.0 }
            });

            using var ms = new MemoryStream();
            ColumnPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<ColumnRebarLayout> { layout }, ms);

            ms.Position = 0;
            using var workbook = new XLWorkbook(ms);
            var ws = workbook.Worksheet("Cot");
            Assert.NotNull(ws);
            Assert.Equal("C1", ws.Cell(3, 3).GetString());
            Assert.Equal(400.0, ws.Cell(3, 4).GetDouble());
        }

        [Fact]
        public void WallPlanScheduleWorkbookGenerator_GeneratesValidExcelWorksheet()
        {
            var wall = new ShearWallModel
            {
                WallMark = "V1",
                ThicknessMm = 300.0,
                LengthMm = 3200.0
            };
            wall.Stories.Add(new WallStorySegment { StoryIndex = 0, StoryName = "Tầng 1", ThicknessMm = 300.0, LengthMm = 3200.0 });
            wall.BoundaryElements.Add(new WallBoundaryElementModel { LengthBbMm = 500.0, VerticalBarCount = 8, VerticalBarDiameterMm = 20, TieDiameterMm = 8, TieSpacingMm = 100.0 });
            wall.WebReinforcement = new WallWebReinforcementModel { VerticalBarDiameterMm = 12, VerticalBarSpacingMm = 200.0, HorizontalBarDiameterMm = 12, HorizontalBarSpacingMm = 200.0 };

            using var ms = new MemoryStream();
            WallPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<ShearWallModel> { wall }, ms);

            ms.Position = 0;
            using var workbook = new XLWorkbook(ms);
            var ws = workbook.Worksheet("Vach");
            Assert.NotNull(ws);
            Assert.Equal("V1", ws.Cell(3, 3).GetString());
            Assert.Equal(300.0, ws.Cell(3, 4).GetDouble());
        }

        [Fact]
        public void WallScheduleAggregator_IncludesUBarsAndOpeningJambHeaderTrimmers()
        {
            var wall = new ShearWallModel
            {
                WallMark = "SW1",
                ThicknessMm = 300.0,
                LengthMm = 4000.0
            };
            wall.Stories.Add(new WallStorySegment { StoryIndex = 0, ThicknessMm = 300.0, LengthMm = 4000.0, StoryHeightMm = 3600.0 });
            wall.BoundaryElements.Add(new WallBoundaryElementModel { LengthBbMm = 500.0, VerticalBarCount = 8, VerticalBarDiameterMm = 20, TieDiameterMm = 8, TieSpacingMm = 100.0 });
            wall.WebReinforcement = new WallWebReinforcementModel
            {
                VerticalBarDiameterMm = 12,
                VerticalBarSpacingMm = 200.0,
                HorizontalBarDiameterMm = 12,
                HorizontalBarSpacingMm = 200.0,
                HasUBarEdgeCaps = true,
                UBarDiameterMm = 12
            };
            wall.Openings.Add(new WallOpeningModel
            {
                OpeningMark = "OP1",
                WidthMm = 1000.0,
                HeightMm = 2100.0,
                HasDiagonalBars = true,
                DiagonalBarsPerCorner = 2,
                DiagonalBarDiameterMm = 16,
                JambExtraBarCountPerSide = 2,
                JambExtraBarDiameterMm = 16,
                HeaderExtraBarCountPerEdge = 2,
                HeaderExtraBarDiameterMm = 16
            });

            var items = WallScheduleAggregator.AggregateSchedule(wall);

            Assert.Contains(items, i => i.ComponentMark.EndsWith("-UBar") && i.CountPerMember > 0);
            Assert.Contains(items, i => i.ComponentMark.EndsWith("-Jamb") && i.CountPerMember > 0);
            Assert.Contains(items, i => i.ComponentMark.EndsWith("-Header") && i.CountPerMember > 0);
            Assert.Contains(items, i => i.ComponentMark.EndsWith("-Diag") && i.CountPerMember > 0);
        }

        [Fact]
        public void XRecordSerializers_RoundTripSerialization_PreservesData()
        {
            var col = new ColumnRebarLayout { ColumnMark = "C10" };
            col.Stories.Add(new ColumnStorySegment { StoryIndex = 0, StoryName = "Story1", WidthMm = 450.0, DepthMm = 450.0 });
            string colJson = ColumnXRecordSerializer.SerializeToJson(col);
            var colDeserialized = ColumnXRecordSerializer.DeserializeFromJson(colJson);
            Assert.NotNull(colDeserialized);
            Assert.Equal("C10", colDeserialized.ColumnMark);
            Assert.Equal(450.0, colDeserialized.Stories[0].WidthMm);

            var wall = new ShearWallModel { WallMark = "W2", ThicknessMm = 350.0, LengthMm = 4000.0 };
            string wallJson = WallXRecordSerializer.SerializeToJson(wall);
            var wallDeserialized = WallXRecordSerializer.DeserializeFromJson(wallJson);
            Assert.NotNull(wallDeserialized);
            Assert.Equal("W2", wallDeserialized.WallMark);
            Assert.Equal(350.0, wallDeserialized.ThicknessMm);
        }
    }
}
