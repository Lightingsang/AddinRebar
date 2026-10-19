using System;
using HPKata.Core.Foundations.Calculators;
using HPKata.Core.Foundations.Models;
using Xunit;

namespace HPKata.Core.Tests.Foundations
{
    public class PileCapRebarTests
    {
        [Fact]
        public void CalculatePileCapVolume_DeductsPileEmbedment()
        {
            var (piles, _, _) = PileCoordinateGenerator.GenerateLayout(4, 400.0);
            var cap = new PileCapModel
            {
                LengthMm = 2000.0,
                WidthMm = 2000.0,
                TotalHeightMm = 1000.0,
                Piles = piles,
                PileDiameterMm = 400.0,
                PileEmbedmentDepthMm = 100.0,
                MemberCount = 1
            };

            double vol = FootingGeometryCalculator.CalculatePileCapVolume(cap);
            double grossVol = 2.0 * 2.0 * 1.0; // 4.0 m3
            double pileDed = 4 * Math.PI * 0.2 * 0.2 * 0.1; // ~0.0503 m3
            double expectedNet = grossVol - pileDed;

            Assert.Equal(expectedNet, vol, 2);
        }

        [Fact]
        public void CalculatePileCapRebar_DeepCap_AddsSkinTiesAndConfinement()
        {
            var (piles, _, _) = PileCoordinateGenerator.GenerateLayout(4, 400.0);
            var cap = new PileCapModel
            {
                CapMark = "ĐC-1",
                LengthMm = 2000.0,
                WidthMm = 2000.0,
                TotalHeightMm = 1000.0,
                Piles = piles,
                HasSkinTies = true,
                HasTopMesh = true,
                StarterDowelCount = 8
            };

            var items = PileCapRebarCalculator.CalculatePileCapRebar(cap);
            Assert.Contains(items, it => it.BarMark == "1" && it.ShapeCode == "02"); // Bottom X
            Assert.Contains(items, it => it.BarMark == "2" && it.ShapeCode == "02"); // Bottom Y
            Assert.Contains(items, it => it.BarMark == "3" && it.ShapeCode == "02"); // Top X
            Assert.Contains(items, it => it.BarMark == "4" && it.ShapeCode == "02"); // Top Y
            Assert.Contains(items, it => it.BarMark == "5" && it.ShapeCode == "51"); // Skin tie
            Assert.Contains(items, it => it.BarMark == "6" && it.DiameterMm == 20);  // Starter dowel
            Assert.Contains(items, it => it.BarMark == "7" && it.ShapeCode == "74"); // Pile confinement
        }

        [Fact]
        public void CalculatePileCapRebar_ShallowCap_OmitsSkinTies()
        {
            var cap = new PileCapModel
            {
                CapMark = "ĐC-S",
                LengthMm = 1600.0,
                WidthMm = 1600.0,
                TotalHeightMm = 500.0, // < 600mm
                HasSkinTies = true
            };

            var items = PileCapRebarCalculator.CalculatePileCapRebar(cap);
            Assert.DoesNotContain(items, it => it.ShapeCode == "51");
        }
    }
}
