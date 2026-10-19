using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Stairs.Models;
using HPKata.Core.Stairs.Persistence;
using Xunit;

namespace HPKata.Core.Tests.Stairs
{
    public class StairPersistenceTests
    {
        [Fact]
        public void StairXRecordSerializer_RoundTrip_PreservesAllFlightProperties()
        {
            var flight = new StairFlightModel
            {
                FlightMark = "ST-TEST",
                RiserCount = 15,
                RiserHeightMm = 165.0,
                TreadWidthMm = 270.0,
                FlightWidthMm = 1300.0,
                ThroatThicknessMm = 130.0,
                BottomTensionDiaMm = 14,
                TopNegativeDiaMm = 12
            };

            string json = StairXRecordSerializer.SerializeFlight(flight);
            Assert.False(string.IsNullOrWhiteSpace(json));

            var restored = StairXRecordSerializer.DeserializeFlight(json);
            Assert.NotNull(restored);
            Assert.Equal("ST-TEST", restored.FlightMark);
            Assert.Equal(15, restored.RiserCount);
            Assert.Equal(165.0, restored.RiserHeightMm);
            Assert.Equal(14, restored.BottomTensionDiaMm);
        }

        [Fact]
        public void LintelXRecordSerializer_RoundTrip_PreservesProperties()
        {
            var lintel = new LintelModel
            {
                LintelMark = "LT-02",
                ClearSpanMm = 1500.0,
                BearingLengthMm = 300.0,
                WidthMm = 200.0,
                HeightMm = 200.0,
                TopHangerCount = 2,
                BottomTensionCount = 3
            };

            string json = LintelXRecordSerializer.SerializeToJson(lintel);
            Assert.False(string.IsNullOrWhiteSpace(json));

            var restored = LintelXRecordSerializer.DeserializeFromJson(json);
            Assert.NotNull(restored);
            Assert.Equal("LT-02", restored.LintelMark);
            Assert.Equal(1500.0, restored.ClearSpanMm);
            Assert.Equal(300.0, restored.BearingLengthMm);
            Assert.Equal(3, restored.BottomTensionCount);
        }

        [Fact]
        public void StairPlanScheduleWorkbookGenerator_GeneratesValidThangWorksheet()
        {
            var flight1 = new StairFlightModel { FlightMark = "ST1", RiserCount = 11, MemberCount = 1 };
            var flight2 = new StairFlightModel { FlightMark = "ST2", RiserCount = 13, MemberCount = 2 };

            var models = new List<StairReinforcementModel>
            {
                new StairReinforcementModel { Flight = flight1 },
                new StairReinforcementModel { Flight = flight2 }
            };

            using var ms = new MemoryStream();
            StairPlanScheduleWorkbookGenerator.GenerateWorkbookStream(models, ms);
            ms.Position = 0;

            using var workbook = new XLWorkbook(ms);
            var sheet = workbook.Worksheet("Thang");
            Assert.NotNull(sheet);

            // Row 2: Headers
            Assert.Equal("STT", sheet.Cell(2, 2).GetString());
            Assert.Equal("Tên Cấu Kiện", sheet.Cell(2, 3).GetString());

            // Row 3: ST1
            Assert.Equal("ST1", sheet.Cell(3, 3).GetString());
            Assert.Equal("Vế Thang", sheet.Cell(3, 4).GetString());

            // Row 4: ST2
            Assert.Equal("ST2", sheet.Cell(4, 3).GetString());
        }

        [Fact]
        public void StairPlanScheduleWorkbookGenerator_NullOrEmptyArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => StairPlanScheduleWorkbookGenerator.GenerateWorkbookStream(null!, new MemoryStream()));
            Assert.Throws<ArgumentNullException>(() => StairPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<StairReinforcementModel>(), null!));
        }
    }
}
