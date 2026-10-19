using System.Collections.Generic;
using HPKata.Core.Beams.Models;
using HPKata.Core.Columns.Models;
using HPKata.Core.Excel.Data;
using HPKata.Core.Excel.Mappers;
using HPKata.Core.Models;
using HPKata.Core.Walls.Models;
using Xunit;

namespace HPKata.Core.Tests.Excel
{
    public class KataExcelMapperTests
    {
        [Fact]
        public void BeamDataMapper_MapsToRebarLayoutAndBack()
        {
            var sheetData = new KataBeamSheetData
            {
                BeamMark = "B-TEST",
                DefaultWidthMm = 350.0,
                DefaultHeightMm = 700.0,
                TopContinuousRebar = "3f22",
                BotContinuousRebar = "3f25"
            };
            sheetData.Spans.Add(new KataBeamSpanData
            {
                SpanIndex = 0,
                SpanName = "Nhịp 1",
                ClearLengthMm = 6000.0,
                WidthMm = 350.0,
                HeightMm = 700.0
            });
            sheetData.Supports.Add(new KataBeamSupportData
            {
                Index = 0,
                Name = "Gối 1",
                WidthMm = 400.0
            });

            var layout = KataBeamDataMapper.ToRebarLayout(sheetData);
            Assert.Equal("B-TEST", layout.BeamName);
            Assert.Single(layout.Spans);
            Assert.Equal(6000.0, layout.Spans[0].ClearLengthMm);
            Assert.Single(layout.TopContinuousBars);
            Assert.Equal(22, layout.TopContinuousBars[0].DiameterMm);
            Assert.Equal(3, layout.TopContinuousBars[0].BarCount);

            var mappedBack = KataBeamDataMapper.ToSheetData(layout);
            Assert.Equal("B-TEST", mappedBack.BeamMark);
            Assert.Single(mappedBack.Spans);
            Assert.Equal("3f22", mappedBack.TopContinuousRebar);
        }

        [Fact]
        public void ColumnDataMapper_MapsToRebarLayoutAndBack()
        {
            var sheetData = new KataColumnSheetData
            {
                ColumnMark = "C-MAPPER",
                FoundationBottomElevationM = -3.5
            };
            sheetData.Stories.Add(new KataColumnStoryData
            {
                StoryIndex = 0,
                StoryName = "Tầng 1",
                StoryHeightMm = 3600.0,
                WidthMm = 500.0,
                DepthMm = 500.0,
                CornerDiameter = 22,
                SideCountB = 2,
                SideDiameterB = 20,
                SideCountH = 2,
                SideDiameterH = 20
            });

            var layout = KataColumnDataMapper.ToRebarLayout(sheetData);
            Assert.Equal("C-MAPPER", layout.ColumnMark);
            Assert.Equal(-3500.0, layout.FoundationBottomElevationMm);
            Assert.Single(layout.Stories);
            Assert.Equal(500.0, layout.Stories[0].WidthMm);
            Assert.Equal(22, layout.Stories[0].Section.CornerDiameterMm);

            var mappedBack = KataColumnDataMapper.ToSheetData(layout);
            Assert.Equal("C-MAPPER", mappedBack.ColumnMark);
            Assert.Single(mappedBack.Stories);
            Assert.Equal(500.0, mappedBack.Stories[0].WidthMm);
        }

        [Fact]
        public void WallDataMapper_MapsToShearWallModelAndBack()
        {
            var sheetData = new KataWallSheetData
            {
                WallMark = "SW-MAPPER",
                ThicknessMm = 350.0,
                LengthMm = 3500.0,
                BoundaryBarCount = 8,
                BoundaryBarDiameter = 22
            };
            sheetData.Stories.Add(new KataWallStoryData
            {
                StoryIndex = 0,
                StoryName = "Tầng 1",
                StoryHeightM = 3.6,
                LengthLwMm = 3500.0,
                ThicknessTwMm = 350.0
            });

            var model = KataWallDataMapper.ToShearWallModel(sheetData);
            Assert.Equal("SW-MAPPER", model.WallMark);
            Assert.Equal(350.0, model.ThicknessMm);
            Assert.Equal(3500.0, model.LengthMm);
            Assert.Equal(2, model.BoundaryElements.Count);

            var mappedBack = KataWallDataMapper.ToSheetData(model);
            Assert.Equal("SW-MAPPER", mappedBack.WallMark);
            Assert.Equal(350.0, mappedBack.ThicknessMm);
            Assert.Single(mappedBack.Stories);
        }

        [Fact]
        public void ScheduleItemMapper_ConvertsBetweenRebarItemAndScheduleItem()
        {
            var item = new RebarItem
            {
                ComponentMark = "B01",
                BarMark = "1",
                ShapeCode = "01",
                DiameterMm = 20,
                ParametersMm = new[] { 500.0, 5000.0, 500.0 },
                CuttingLengthMm = 6000.0,
                MemberCount = 2,
                CountPerMember = 3,
                UnitMassKgPerM = 2.466
            };

            var dto = KataScheduleItemMapper.ToScheduleItem(item);
            Assert.Equal("B01", dto.ComponentMark);
            Assert.Equal("1", dto.BarMark);
            Assert.Equal(20, dto.DiameterMm);
            Assert.Equal(6, dto.TotalCount);
            Assert.Equal(36.0, dto.TotalLengthM);

            var back = KataScheduleItemMapper.ToRebarItem(dto);
            Assert.Equal(item.ComponentMark, back.ComponentMark);
            Assert.Equal(item.DiameterMm, back.DiameterMm);
            Assert.Equal(item.CuttingLengthMm, back.CuttingLengthMm);
        }
    }
}
