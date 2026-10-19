using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Etabs;
using HPKata.Core.Beams.Models;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class BeamCurtailmentStressTests
    {
        [Fact]
        public void Stress_CommercialBars_3Span24mBeamRun_DivisionAndSplices()
        {
            double totalLength = 24000.0;
            int dia = 20;
            double expectedLap = 30.0 * dia;

            var segsUnstaggered = BeamCurtailmentCalculator.SegmentCommercialBars(
                totalLength, dia, isTopBar: true, staggerOffsetMm: 0.0);

            Assert.True(segsUnstaggered.Count >= 3);
            Assert.All(segsUnstaggered, s => Assert.True(s.LengthMm <= BeamCurtailmentCalculator.CommercialStockLengthMm));

            var lastSeg = segsUnstaggered.Last();
            Assert.Equal(totalLength, lastSeg.StartXMm + lastSeg.LengthMm);

            for (int i = 1; i < segsUnstaggered.Count; i++)
            {
                Assert.True(segsUnstaggered[i].IsLapSplice);
                Assert.Equal(expectedLap, segsUnstaggered[i].LapLengthMm);
            }

            var segsStaggered = BeamCurtailmentCalculator.SegmentCommercialBars(
                totalLength, dia, isTopBar: true, staggerOffsetMm: 2000.0);

            Assert.True(segsStaggered.Count >= 3);
            Assert.Equal(BeamCurtailmentCalculator.CommercialStockLengthMm - 2000.0, segsStaggered[0].LengthMm);

            double splicePos1 = segsUnstaggered[0].LengthMm;
            double splicePos2 = segsStaggered[0].LengthMm;
            Assert.Equal(2000.0, Math.Abs(splicePos1 - splicePos2));
        }

        [Fact]
        public void Stress_CommercialBars_DeadParameter_IsTopBarIgnored()
        {
            var topSegs = BeamCurtailmentCalculator.SegmentCommercialBars(24000.0, 20, isTopBar: true);
            var botSegs = BeamCurtailmentCalculator.SegmentCommercialBars(24000.0, 20, isTopBar: false);

            bool identical = topSegs.Count == botSegs.Count &&
                             topSegs[0].LengthMm == botSegs[0].LengthMm &&
                             topSegs[1].StartXMm == botSegs[1].StartXMm;

            Assert.True(identical, "isTopBar produces identical segments because it is currently dead code in BeamCurtailmentCalculator.");
        }

        [Theory]
        [InlineData(6000.0, 600.0, 1500.0, 3000.0)]
        [InlineData(4000.0, 600.0, 1200.0, 1600.0)]
        [InlineData(2400.0, 600.0, 1200.0, 0.0)]
        public void Stress_StirrupZones_DenseAndSparseLengths(
            double span, double height, double expectedDense, double expectedMidspan)
        {
            var zones = BeamStirrupZoneCalculator.CalculateZones(0, span, 300.0, height);
            Assert.Equal(expectedDense, zones.LeftDenseZoneLengthMm);
            Assert.Equal(expectedDense, zones.RightDenseZoneLengthMm);
            Assert.Equal(expectedMidspan, zones.MidspanSparseZoneLengthMm);
        }

        [Fact]
        public void Stress_StirrupZones_RoundingOverflowBug_DenseExceedsHalfSpan()
        {
            var zones = BeamStirrupZoneCalculator.CalculateZones(0, 1350.0, 300.0, 500.0);
            double totalDense = zones.LeftDenseZoneLengthMm + zones.RightDenseZoneLengthMm;
            Assert.True(totalDense <= 1350.0,
                $"CRITICAL DEFECT: Left dense ({zones.LeftDenseZoneLengthMm}mm) + Right dense ({zones.RightDenseZoneLengthMm}mm) = {totalDense}mm EXCEEDS clear span 1350mm!");
        }

        [Fact]
        public void Stress_EtabsImport_DualDiameterRebarAreaLost_Bug()
        {
            var opt = TcvnBeamRebarOptimizer.Optimize(1350.0, 300.0, isTopBar: true);

            var frame = new EtabsFrameDesign
            {
                FrameName = "B1",
                Story = "STORY1",
                LengthMm = 6000.0,
                WidthMm = 300.0,
                HeightMm = 600.0
            };
            frame.Stations.Add(new EtabsStationData { StationMm = 0.0, AsTopMm2 = 1350.0, AsBotMm2 = 900.0 });
            frame.Stations.Add(new EtabsStationData { StationMm = 3000.0, AsTopMm2 = 500.0, AsBotMm2 = 900.0 });
            frame.Stations.Add(new EtabsStationData { StationMm = 6000.0, AsTopMm2 = 1350.0, AsBotMm2 = 900.0 });

            var layout = EtabsBeamImportService.ConvertFrameToBeamLayout(frame, 0);

            double totalTopArea = 0.0;
            foreach (var run in layout.TopContinuousBars)
            {
                totalTopArea += run.BarCount * (Math.PI * run.DiameterMm * run.DiameterMm / 4.0);
            }

            if (opt.SecondaryCount > 0)
            {
                Assert.True(totalTopArea >= 1350.0,
                    $"CRITICAL DEFECT: EtabsBeamImportService dropped secondary bars! Required: 1350mm2, Provided in layout: {totalTopArea:F1}mm2 (Optimizer had selected {opt.Notation})");
            }
        }

        [Fact]
        public void Stress_CantileverOverhang_StirrupAndCutoffDefect()
        {
            var cantileverSpan = new BeamSpanModel
            {
                SpanIndex = 1,
                SpanName = "Cantilever_1",
                ClearLengthMm = 2000.0,
                WidthMm = 300.0,
                HeightMm = 500.0,
                LeftSupportWidthMm = 400.0,
                RightSupportWidthMm = 0.0,
                IsCantileverRight = true
            };

            var stirrupZones = BeamStirrupZoneCalculator.CalculateZones(cantileverSpan);
            Assert.True(stirrupZones.RightDenseZoneLengthMm == 0.0,
                $"DEFECT: BeamStirrupZoneCalculator generates a {stirrupZones.RightDenseZoneLengthMm}mm dense stirrup zone at the cantilever free tip!");

            double topCutoff = BeamCurtailmentCalculator.CalculateTopCutoff(cantileverSpan, 0.25);
            Assert.True(topCutoff >= cantileverSpan.ClearLengthMm,
                $"DEFECT: Top cutoff is only {topCutoff}mm for a {cantileverSpan.ClearLengthMm}mm cantilever, cutting tension steel prematurely at L/4!");
        }

        [Fact]
        public void Stress_StirrupStations_StationsWithinSpanBounds()
        {
            var zones = BeamStirrupZoneCalculator.CalculateZones(0, 5000.0, 300.0, 500.0);
            var stations = BeamStirrupZoneCalculator.GenerateStirrupStations(5000.0, zones, firstOffsetMm: 50.0);

            Assert.NotEmpty(stations);
            Assert.Equal(50.0, stations.First());
            Assert.True(stations.Last() <= 4950.0, $"Last station {stations.Last()} exceeds clear span minus first offset (4950mm)");

            for (int i = 0; i < stations.Count - 1; i++)
            {
                double step = stations[i + 1] - stations[i];
                Assert.True(step > 0, $"Non-positive spacing {step} at station index {i}");
            }
        }
    }
}
