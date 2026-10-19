using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Foundations.Calculators;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Foundations.Persistence;
using HPKata.Core.Slabs.Calculators;
using HPKata.Core.Slabs.Models;
using HPKata.Core.Slabs.Persistence;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;
using HPKata.Core.Stairs.Persistence;
using Xunit;

namespace HPKata.E2E.Tests.Tier3_CrossFeature
{
    public sealed class Tier3_FoundationSlabStairPipelineTests
    {
        [Fact]
        public void Foundation_EndToEndPipeline_CalculatesSerializesAndExportsWorkbook()
        {
            var (piles, _, _) = PileCoordinateGenerator.GenerateLayout(5, 400.0, isSquare: false);
            var cap = new PileCapModel
            {
                CapMark = "MC-E2E",
                LengthMm = 2400.0,
                WidthMm = 2400.0,
                TotalHeightMm = 1000.0,
                IsStepped = true,
                Tier2LengthMm = 1600.0,
                Tier2WidthMm = 1600.0,
                Tier2HeightMm = 400.0,
                Piles = piles,
                PileDiameterMm = 400.0
            };

            double vol = FootingGeometryCalculator.CalculatePileCapVolume(cap);
            Assert.True(vol > 0.0);

            var items = PileCapRebarCalculator.CalculatePileCapRebar(cap);
            Assert.True(items.Count >= 4);

            var layout = FoundationRebarLayout.FromPileCap(cap);
            string json = FoundationXRecordSerializer.SerializeToJson(layout);
            Assert.False(string.IsNullOrWhiteSpace(json));

            var restored = FoundationXRecordSerializer.DeserializeFromJson(json);
            Assert.NotNull(restored);
            Assert.Equal("MC-E2E", restored.FoundationMark);

            using var stream = new MemoryStream();
            FoundationPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<FoundationRebarLayout> { layout }, stream);
            stream.Position = 0;

            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet("Mong");
            Assert.NotNull(ws);
            Assert.Equal("MC-E2E", ws.Cell(3, 3).GetString());
        }

        [Fact]
        public void Slab_EndToEndPipeline_CalculatesSerializesAndExportsWorkbook()
        {
            var panel = new SlabPanelModel
            {
                PanelMark = "S-E2E",
                SpanXMm = 4200.0,
                SpanYMm = 5400.0,
                ThicknessMm = 130.0,
                CoverMm = 15.0
            };

            var items = SlabRebarCalculator.CalculateSlabRebar(panel);
            Assert.True(items.Count >= 3);

            var stool = SlabChairCalculator.CalculateStool(
                panel.ThicknessMm, panel.CoverMm, panel.CoverMm,
                10, 10, 10, 1.2, 22.68);
            Assert.True(stool.StoolHeightMm > 30.0);
            Assert.True(stool.TotalCount > 0);

            var poly = new List<(double X, double Y)>
            {
                (0.0, 0.0),
                (4000.0, 0.0),
                (3000.0, 3000.0),
                (0.0, 3000.0)
            };
            var stepped = SlabPolylineSteppingCalculator.CalculateSteppingRebar(
                "S-E2E", poly, spacingMm: 200.0, coverMm: 15.0, diameterMm: 10, deltaLMm: 50.0);
            Assert.True(stepped.Count > 1);
            Assert.Equal("1a", stepped[0].BarMark);

            var layout = SlabRebarLayout.FromPanel(panel);
            string json = SlabXRecordSerializer.SerializeToJson(layout);
            var restored = SlabXRecordSerializer.DeserializeFromJson(json);
            Assert.NotNull(restored);

            using var stream = new MemoryStream();
            SlabPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<SlabRebarLayout> { layout }, stream);
            stream.Position = 0;

            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet("San");
            Assert.NotNull(ws);
            Assert.Equal("S-E2E", ws.Cell(3, 3).GetString());
        }

        [Fact]
        public void StairLintel_EndToEndPipeline_CalculatesSerializesAndExportsWorkbook()
        {
            var flight = new StairFlightModel
            {
                FlightMark = "ST-E2E",
                RiserCount = 11,
                RiserHeightMm = 160.0,
                TreadWidthMm = 280.0,
                BottomTensionDiaMm = 12
            };

            bool ergOk = StairGeometryCalculator.CheckErgonomics(flight.RiserHeightMm, flight.TreadWidthMm, out double blondel);
            Assert.True(ergOk);
            Assert.Equal(600.0, blondel);

            var stairItems = StairRebarCalculator.CalculateStairRebar(flight);
            Assert.Equal(6, stairItems.Count);

            var lintel = new LintelModel { LintelMark = "LT-E2E", ClearSpanMm = 1400.0 };
            var lintelItems = LintelRebarCalculator.CalculateLintelRebar(lintel);
            Assert.Equal(3, lintelItems.Count);

            var sec = GenericSectionCalculator.CreateRectangularSection(
                "SEC-E2E", 300.0, 600.0, 25.0, 3, 18, 4, 20, 8);
            var secItems = GenericSectionCalculator.SectionToRebarItems(sec);
            Assert.Equal(8, secItems.Count);

            var model = StairReinforcementModel.FromFlight(flight);
            string json = StairXRecordSerializer.SerializeToJson(model);
            var restored = StairXRecordSerializer.DeserializeFromJson(json);
            Assert.NotNull(restored);

            using var stream = new MemoryStream();
            StairPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<StairReinforcementModel> { model }, stream);
            stream.Position = 0;

            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet("Thang");
            Assert.NotNull(ws);
            Assert.Equal("ST-E2E", ws.Cell(3, 3).GetString());
        }
    }
}
