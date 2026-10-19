using System;
using System.Linq;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Etabs;
using HPKata.Core.Beams.Models;
using Xunit;

namespace HPKata.Core.Tests.Beams
{
    public class BeamEtabsAndStirrupStressTests
    {
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
