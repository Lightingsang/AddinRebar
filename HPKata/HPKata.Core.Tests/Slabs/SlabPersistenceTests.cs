using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Slabs.Models;
using HPKata.Core.Slabs.Persistence;
using Xunit;

namespace HPKata.Core.Tests.Slabs
{
    public class SlabPersistenceTests
    {
        [Fact]
        public void SlabXRecordSerializer_RoundTripPanel_PreservesProperties()
        {
            var p = new SlabPanelModel
            {
                PanelMark = "S10",
                SpanXMm = 4500.0,
                SpanYMm = 6000.0,
                ThicknessMm = 140.0
            };

            string json = SlabXRecordSerializer.SerializePanel(p);
            var deserialized = SlabXRecordSerializer.DeserializePanel(json);

            Assert.NotNull(deserialized);
            Assert.Equal("S10", deserialized.PanelMark);
            Assert.Equal(4500.0, deserialized.SpanXMm);
            Assert.Equal(140.0, deserialized.ThicknessMm);
        }

        [Fact]
        public void SlabPlanScheduleWorkbookGenerator_CreatesSheetSanWithValidData()
        {
            var p = new SlabPanelModel { PanelMark = "S1" };
            var layouts = new List<SlabRebarLayout> { SlabRebarLayout.FromPanel(p) };

            using var ms = new MemoryStream();
            SlabPlanScheduleWorkbookGenerator.GenerateWorkbookStream(layouts, ms);
            ms.Position = 0;

            using var workbook = new XLWorkbook(ms);
            Assert.True(workbook.Worksheets.Contains("San"));
            var ws = workbook.Worksheet("San");

            Assert.Equal("Tên Ô Sàn", ws.Cell(2, 3).GetString());
            Assert.Equal("S1", ws.Cell(3, 3).GetString());
        }
    }
}
