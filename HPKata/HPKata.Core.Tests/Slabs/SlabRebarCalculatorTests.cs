using System;
using HPKata.Core.Slabs.Calculators;
using HPKata.Core.Slabs.Models;
using Xunit;

namespace HPKata.Core.Tests.Slabs
{
    public class SlabRebarCalculatorTests
    {
        [Fact]
        public void SlabPanelModel_Validation_ThrowsOnInvalidParameters()
        {
            var p = new SlabPanelModel { SpanXMm = -500 };
            Assert.Throws<ArgumentException>(() => p.Validate());

            p.SpanXMm = 4000;
            p.ThicknessMm = 0;
            Assert.Throws<ArgumentException>(() => p.Validate());
        }

        [Fact]
        public void CalculateSlabRebar_TwoWayPanel_GeneratesBottomMeshAndTopHats()
        {
            var panel = new SlabPanelModel
            {
                PanelMark = "S1",
                SpanXMm = 4000.0,
                SpanYMm = 5000.0,
                ThicknessMm = 120.0,
                CoverMm = 15.0,
                BottomMeshXDiaMm = 10,
                BottomMeshXSpacingMm = 150.0,
                BottomMeshYDiaMm = 10,
                BottomMeshYSpacingMm = 150.0,
                TopSupportDiaMm = 10,
                TopSupportSpacingMm = 150.0
            };

            var items = SlabRebarCalculator.CalculateSlabRebar(panel);
            Assert.True(items.Count >= 4);

            Assert.Contains(items, it => it.BarMark == "1" && it.DiameterMm == 10 && it.ShapeCode == "02"); // B1
            Assert.Contains(items, it => it.BarMark == "2" && it.DiameterMm == 10 && it.ShapeCode == "02"); // B2
            Assert.Contains(items, it => it.BarMark == "3" && it.DiameterMm == 10 && it.ShapeCode == "02"); // Top X
            Assert.Contains(items, it => it.BarMark == "4" && it.DiameterMm == 10 && it.ShapeCode == "02"); // Top Y
        }

        [Fact]
        public void CalculateSlabRebar_Cantilever_IncludesCantileverBars()
        {
            var panel = new SlabPanelModel
            {
                PanelMark = "SC1",
                SpanXMm = 3000.0,
                SpanYMm = 4000.0,
                ThicknessMm = 100.0,
                IsCantilever = true,
                CantileverLengthMm = 1200.0
            };

            var items = SlabRebarCalculator.CalculateSlabRebar(panel);
            Assert.Contains(items, it => it.BarMark == "5" && it.Notes.Contains("công xôn"));
        }

        [Fact]
        public void CalculateSlabRebar_CornerCrack_IncludesCornerBars()
        {
            var panel = new SlabPanelModel
            {
                PanelMark = "SCorner",
                SpanXMm = 4000.0,
                SpanYMm = 4000.0,
                HasCornerCrackBars = true,
                CornerBarDiaMm = 10,
                CornerBarSpacingMm = 150.0
            };

            var items = SlabRebarCalculator.CalculateSlabRebar(panel);
            Assert.Contains(items, it => it.BarMark == "6" && it.Notes.Contains("góc sàn"));
        }
    }
}
