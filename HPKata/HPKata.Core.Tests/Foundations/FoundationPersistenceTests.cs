using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Foundations.Persistence;
using Xunit;

namespace HPKata.Core.Tests.Foundations
{
    public class FoundationPersistenceTests
    {
        [Fact]
        public void FoundationXRecordSerializer_RoundTripFooting_MatchesProperties()
        {
            var f = new IsolatedFootingModel
            {
                FootingMark = "M2",
                LengthMm = 1800.0,
                WidthMm = 1800.0,
                TotalHeightMm = 850.0,
                BottomMeshXDiaMm = 16
            };

            string json = FoundationXRecordSerializer.SerializeFooting(f);
            var deserialized = FoundationXRecordSerializer.DeserializeFooting(json);

            Assert.NotNull(deserialized);
            Assert.Equal("M2", deserialized.FootingMark);
            Assert.Equal(1800.0, deserialized.LengthMm);
            Assert.Equal(16, deserialized.BottomMeshXDiaMm);
        }

        [Fact]
        public void FoundationXRecordSerializer_RoundTripPileCap_MatchesPiles()
        {
            var cap = new PileCapModel
            {
                CapMark = "ĐC-4",
                LengthMm = 2200.0,
                WidthMm = 2200.0,
                Piles = new List<PileCoordinate>
                {
                    new(1, -500, -500),
                    new(2, 500, -500),
                    new(3, 500, 500),
                    new(4, -500, 500)
                }
            };

            string json = FoundationXRecordSerializer.SerializePileCap(cap);
            var deserialized = FoundationXRecordSerializer.DeserializePileCap(json);

            Assert.NotNull(deserialized);
            Assert.Equal("ĐC-4", deserialized.CapMark);
            Assert.Equal(4, deserialized.Piles.Count);
        }

        [Fact]
        public void FoundationPlanScheduleWorkbookGenerator_CreatesSheetMongWithValidData()
        {
            var f = new IsolatedFootingModel { FootingMark = "M1" };
            var cap = new PileCapModel { CapMark = "ĐC-1" };
            var layouts = new List<FoundationRebarLayout>
            {
                FoundationRebarLayout.FromFooting(f),
                FoundationRebarLayout.FromPileCap(cap)
            };

            using var ms = new MemoryStream();
            FoundationPlanScheduleWorkbookGenerator.GenerateWorkbookStream(layouts, ms);
            ms.Position = 0;

            using var workbook = new XLWorkbook(ms);
            Assert.True(workbook.Worksheets.Contains("Mong"));
            var ws = workbook.Worksheet("Mong");

            Assert.Equal("Tên Móng", ws.Cell(2, 3).GetString());
            Assert.Equal("M1", ws.Cell(3, 3).GetString());
            Assert.Equal("ĐC-1", ws.Cell(4, 3).GetString());
        }
    }
}
