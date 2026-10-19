using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.AutoCad.Controllers;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Models;
using HPKata.Core.Beams.Persistence;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.AutoCad.Tests.Controllers
{
    public sealed class BeamControllerTests
    {
        [Fact]
        public void BeamTakeoffRegistry_RegistersAndRetrievesLayouts()
        {
            BeamTakeoffRegistry.Clear();
            var layout = new BeamRebarLayout
            {
                BeamName = "D2-REG",
                TotalLengthMm = 8000.0
            };

            BeamTakeoffRegistry.Register(layout);

            Assert.True(BeamTakeoffRegistry.TryGet("D2-REG", out var retrieved));
            Assert.NotNull(retrieved);
            Assert.Equal("D2-REG", retrieved.BeamName);
            Assert.Equal(8000.0, retrieved.TotalLengthMm);

            var all = BeamTakeoffRegistry.GetAll();
            Assert.Single(all);

            BeamTakeoffRegistry.Clear();
            Assert.Empty(BeamTakeoffRegistry.GetAll());
        }

        [Fact]
        public void BeamPersistence_DWGDictionary_RoundtripsLayoutViaSerializer()
        {
            var layout = new BeamRebarLayout
            {
                BeamName = "D100_TEST",
                TotalLengthMm = 12000.0
            };
            layout.Spans.Add(new BeamSpanModel
            {
                SpanIndex = 0,
                SpanName = "D100_TEST",
                ClearLengthMm = 5600.0,
                WidthMm = 300.0,
                HeightMm = 650.0
            });
            layout.TopContinuousBars.Add(new RebarRunModel { DiameterMm = 22, BarCount = 3, LengthMm = 12000.0 });
            layout.BottomContinuousBars.Add(new RebarRunModel { DiameterMm = 20, BarCount = 2, LengthMm = 12000.0 });
            layout.StirrupZones[0] = new StirrupZoneDistributionModel
            {
                SpanIndex = 0,
                StirrupDiameterMm = 10,
                LeftDenseSpacingMm = 100.0,
                MidspanSparseSpacingMm = 200.0
            };

            string json = BeamXRecordSerializer.SerializeToJson(layout);
            Assert.False(string.IsNullOrWhiteSpace(json));

            var roundtripped = BeamXRecordSerializer.DeserializeFromJson(json);
            Assert.NotNull(roundtripped);
            Assert.Equal("D100_TEST", roundtripped.BeamName);
            Assert.Equal(12000.0, roundtripped.TotalLengthMm);
            Assert.Single(roundtripped.Spans);
            Assert.Equal(650.0, roundtripped.Spans[0].HeightMm);
            Assert.Single(roundtripped.TopContinuousBars);
            Assert.Equal(3, roundtripped.TopContinuousBars[0].BarCount);
            Assert.Equal(22, roundtripped.TopContinuousBars[0].DiameterMm);
            Assert.True(roundtripped.StirrupZones.ContainsKey(0));
            Assert.Equal(10, roundtripped.StirrupZones[0].StirrupDiameterMm);
        }

        [Fact]
        public void BeamScheduleAggregator_PopulatesTableData_MatchesCoreModels()
        {
            var layout = new BeamRebarLayout
            {
                BeamName = "D1",
                TotalLengthMm = 6000.0
            };
            layout.Spans.Add(new BeamSpanModel
            {
                SpanIndex = 0,
                SpanName = "D1",
                ClearLengthMm = 5600.0,
                WidthMm = 300.0,
                HeightMm = 600.0
            });
            layout.TopContinuousBars.Add(new RebarRunModel { DiameterMm = 20, BarCount = 2, LengthMm = 6000.0 });
            layout.BottomContinuousBars.Add(new RebarRunModel { DiameterMm = 20, BarCount = 2, LengthMm = 6000.0 });
            layout.StirrupZones[0] = new StirrupZoneDistributionModel
            {
                SpanIndex = 0,
                StirrupDiameterMm = 8,
                LeftDenseSpacingMm = 100.0,
                MidspanSparseSpacingMm = 200.0,
                LeftDenseZoneLengthMm = 1400.0,
                MidspanSparseZoneLengthMm = 2800.0,
                RightDenseZoneLengthMm = 1400.0
            };

            var items = BeamScheduleAggregator.AggregateSchedule(layout);

            Assert.NotEmpty(items);
            Assert.Contains(items, i => i.DiameterMm == 20 && i.CountPerMember == 2);
            Assert.Contains(items, i => i.DiameterMm == 8 && i.ShapeCode == "22");

            var (le10, mid, gt18, total) = BeamScheduleAggregator.CalculateWeightSummary(items);
            Assert.True(total > 0.0);
            Assert.True(gt18 > 0.0); // 20mm bars
            Assert.True(le10 > 0.0); // 8mm stirrups
        }

        [Fact]
        public void BeamPlanScheduleWorkbookGenerator_GeneratesValidExcelWorkbook()
        {
            var layout = new BeamRebarLayout
            {
                BeamName = "D_EXCEL",
                TotalLengthMm = 7000.0
            };
            layout.Spans.Add(new BeamSpanModel
            {
                SpanIndex = 0,
                SpanName = "D_EXCEL",
                ClearLengthMm = 6600.0,
                WidthMm = 350.0,
                HeightMm = 700.0
            });
            layout.TopContinuousBars.Add(new RebarRunModel { DiameterMm = 25, BarCount = 3, LengthMm = 7000.0 });
            layout.BottomContinuousBars.Add(new RebarRunModel { DiameterMm = 22, BarCount = 2, LengthMm = 7000.0 });

            using var ms = new MemoryStream();
            BeamPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<BeamRebarLayout> { layout }, ms);

            ms.Position = 0;
            using var workbook = new XLWorkbook(ms);
            var ws = workbook.Worksheet("ThepDamAllMBKC");
            Assert.NotNull(ws);

            // Verify Header
            Assert.Equal("Name", ws.Cell(2, 2).GetString());
            Assert.Equal("Width", ws.Cell(2, 4).GetString());
            Assert.Equal("Height", ws.Cell(2, 5).GetString());

            // Verify Row 3 data
            Assert.Equal("D_EXCEL", ws.Cell(3, 2).GetString());
            Assert.Equal(350.0, ws.Cell(3, 4).GetDouble());
            Assert.Equal(700.0, ws.Cell(3, 5).GetDouble());
        }

        [Fact]
        public void AssignLoads_StructuresLoadAttributesCorrectly()
        {
            var record = new KataXDataRecord
            {
                TenCauKien = "D1",
                ChuThich = "DEAD_LOAD=14.5 kN/m"
            };

            var typedVals = KataXDataSerializer.SerializeToDxf(record);
            var deserialized = KataXDataSerializer.DeserializeFromDxf(typedVals);

            Assert.Equal("D1", deserialized.TenCauKien);
            Assert.Equal("DEAD_LOAD=14.5 kN/m", deserialized.ChuThich);
            Assert.Contains("14.5", deserialized.ChuThich);
        }
    }
}
