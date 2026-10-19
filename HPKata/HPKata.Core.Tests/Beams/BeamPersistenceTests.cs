using System.Collections.Generic;
using System.IO;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Models;
using HPKata.Core.Beams.Persistence;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class BeamPersistenceTests
    {
        [Fact]
        public void BeamXRecordSerializer_RoundTrip_PreservesLayoutData()
        {
            var original = new BeamRebarLayout
            {
                BeamName = "D101",
                TotalLengthMm = 6000.0
            };
            original.Spans.Add(new BeamSpanModel
            {
                SpanIndex = 0,
                SpanName = "N1",
                ClearLengthMm = 6000.0,
                WidthMm = 300.0,
                HeightMm = 600.0
            });
            original.TopContinuousBars.Add(new RebarRunModel
            {
                Mark = "1",
                DiameterMm = 20,
                BarCount = 2,
                LengthMm = 6000.0
            });

            string json = BeamXRecordSerializer.SerializeToJson(original);
            Assert.NotEmpty(json);

            var deserialized = BeamXRecordSerializer.DeserializeFromJson(json);
            Assert.NotNull(deserialized);
            Assert.Equal("D101", deserialized!.BeamName);
            Assert.Equal(6000.0, deserialized.TotalLengthMm);
            Assert.Single(deserialized.Spans);
            Assert.Single(deserialized.TopContinuousBars);
            Assert.Equal(20, deserialized.TopContinuousBars[0].DiameterMm);
        }

        [Fact]
        public void BeamScheduleAggregator_FullLayout_AggregatesAllItemsAndWeight()
        {
            var layout = new BeamRebarLayout
            {
                BeamName = "D1",
                TotalLengthMm = 6000.0
            };
            layout.Spans.Add(new BeamSpanModel
            {
                SpanIndex = 0,
                ClearLengthMm = 6000.0,
                WidthMm = 300.0,
                HeightMm = 600.0
            });
            layout.TopContinuousBars.Add(new RebarRunModel
            {
                Mark = "1",
                DiameterMm = 20,
                BarCount = 2,
                LengthMm = 6000.0
            });
            layout.BottomContinuousBars.Add(new RebarRunModel
            {
                Mark = "2",
                DiameterMm = 20,
                BarCount = 2,
                LengthMm = 6000.0
            });

            var stirrup = BeamStirrupZoneCalculator.CalculateZones(0, 6000.0, 300.0, 600.0);
            layout.StirrupZones[0] = stirrup;

            var items = BeamScheduleAggregator.AggregateSchedule(layout, beamCount: 1);
            Assert.Equal(3, items.Count); // Top, Bot, Stirrup

            var summary = BeamScheduleAggregator.CalculateWeightSummary(items);
            Assert.True(summary.TotalKg > 0.0);
            Assert.True(summary.Gt18Kg > 0.0); // 20mm bars
            Assert.True(summary.Le10Kg > 0.0); // 8mm stirrup
        }

        [Fact]
        public void BeamPlanScheduleWorkbookGenerator_GeneratesValidClosedXmlStream()
        {
            var layout = new BeamRebarLayout
            {
                BeamName = "D1",
                TotalLengthMm = 6000.0
            };
            layout.Spans.Add(new BeamSpanModel
            {
                SpanIndex = 0,
                ClearLengthMm = 6000.0,
                WidthMm = 300.0,
                HeightMm = 600.0
            });

            using var ms = new MemoryStream();
            BeamPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<BeamRebarLayout> { layout }, ms);

            Assert.True(ms.Length > 0);
        }
    }
}
