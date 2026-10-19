using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HPKata.Core.Walls.Calculators;
using HPKata.Core.Walls.Etabs;
using HPKata.Core.Walls.Models;
using HPKata.Core.Walls.Persistence;
using Xunit;

namespace HPKata.Core.Tests.Walls
{
    /// <summary>
    /// Empirical challenge and stress tests for Shear Wall and Core Wall domain (Milestone M2 Phase 3).
    /// Author: challenger_m2_2 (Empirical Challenger).
    /// </summary>
    public sealed class WallEmpiricalChallengeTests
    {
        [Theory]
        [InlineData(2000.0, 200.0, 400.0)]
        [InlineData(5000.0, 250.0, 750.0)]
        [InlineData(2000.0, 400.0, 600.0)]
        [InlineData(10000.0, 500.0, 1500.0)]
        [InlineData(800.0, 150.0, 400.0)]
        [InlineData(3200.0, 300.0, 480.0)]
        public void BoundaryZoneLength_MatrixTests_SatisfiesMaxFormula(
            double wallLen, double wallThick, double expectedBbz)
        {
            double actual = WallBoundaryCalculator.CalculateBoundaryLength(wallLen, wallThick);
            double requiredMin = Math.Max(0.15 * wallLen, Math.Max(1.5 * wallThick, 400.0));

            Assert.Equal(expectedBbz, actual);
            Assert.True(actual >= requiredMin);
        }

        [Theory]
        [InlineData(120.0, 10, 60.0)]
        [InlineData(160.0, 14, 80.0)]
        [InlineData(200.0, 16, 96.0)]
        [InlineData(300.0, 20, 100.0)]
        [InlineData(500.0, 25, 100.0)]
        [InlineData(80.0, 8, 50.0)]
        public void BoundaryTieSpacing_MatrixTests_ClampsStrictlyBetween50And100(
            double tw, int db, double expectedSpacing)
        {
            double spacing = WallBoundaryCalculator.CalculateBoundaryTieSpacing(tw, db);

            Assert.Equal(expectedSpacing, spacing);
            Assert.True(spacing <= 100.0, $"Tie spacing {spacing} exceeds 100mm");
            Assert.True(spacing >= 50.0, $"Tie spacing {spacing} below 50mm floor");
        }

        [Fact]
        public void BoundaryElement_ReinforcementRatio_EnforcesMinimum1PercentAndEvenBars()
        {
            var beDefault = WallBoundaryCalculator.CreateBoundaryElement(
                "BE1", WallBoundaryPosition.Left, wallLengthMm: 3500.0, wallThicknessMm: 300.0, requiredAsbMm2: 0.0);
            Assert.True(beDefault.ReinforcementRatioPercent >= 1.0);
            Assert.True(beDefault.VerticalBarCount >= 6);
            Assert.True(beDefault.VerticalBarCount % 2 == 0);

            var beHeavy = WallBoundaryCalculator.CreateBoundaryElement(
                "BE2", WallBoundaryPosition.Right, wallLengthMm: 3500.0, wallThicknessMm: 300.0, requiredAsbMm2: 4500.0);
            Assert.True(beHeavy.ReinforcementRatioPercent >= 1.0);
            Assert.True(beHeavy.TotalSteelAreaMm2 >= 4500.0);
        }

        [Fact]
        public void WebMesh_DoubleCurtainArrays_CalculatesBarCountsCorrectly()
        {
            int vDouble = WallWebMeshCalculator.CalculateVerticalBarCount(4000.0, 600.0, 600.0, 200.0, isDoubleCurtain: true);
            int vSingle = WallWebMeshCalculator.CalculateVerticalBarCount(4000.0, 600.0, 600.0, 200.0, isDoubleCurtain: false);
            Assert.Equal(30, vDouble);
            Assert.Equal(15, vSingle);

            int hDouble = WallWebMeshCalculator.CalculateHorizontalBarCount(3300.0, 200.0, isDoubleCurtain: true);
            int hSingle = WallWebMeshCalculator.CalculateHorizontalBarCount(3300.0, 200.0, isDoubleCurtain: false);
            Assert.Equal(34, hDouble);
            Assert.Equal(17, hSingle);
        }

        [Fact]
        public void WebMesh_CrossTieDensity_EnforcesAtLeastFourPerSquareMeter()
        {
            int ties1 = WallWebMeshCalculator.CalculateCrossTieCount(2000.0, 3000.0, 4.0);
            Assert.Equal(24, ties1);

            int tiesSmall = WallWebMeshCalculator.CalculateCrossTieCount(500.0, 300.0, 4.0);
            Assert.Equal(1, tiesSmall);

            double areaM2 = (500.0 / 1000.0) * (300.0 / 1000.0);
            Assert.True((tiesSmall / areaM2) >= 4.0);
        }

        [Theory]
        [InlineData(8, 300.0)]
        [InlineData(10, 300.0)]
        [InlineData(12, 360.0)]
        [InlineData(16, 480.0)]
        [InlineData(20, 600.0)]
        [InlineData(25, 750.0)]
        public void UBarDimensions_LapLength_SatisfiesAtLeastThirtyBarDiameters(int dia, double expectedLeg)
        {
            var (width, leg, total) = WallWebMeshCalculator.CalculateUBarDimensions(300.0, 25.0, dia);

            Assert.Equal(250.0, width);
            Assert.Equal(expectedLeg, leg);
            Assert.True(leg >= 30.0 * dia);
            Assert.True(leg >= 300.0);
            Assert.Equal(width + 2.0 * leg, total);
        }

        [Fact]
        public void OpeningTrimmers_CornerDiagonalsAndExtraBars_CalculatedCorrectly()
        {
            var op = new WallOpeningModel
            {
                OpeningMark = "OP-TEST",
                WidthMm = 1200.0,
                HeightMm = 2400.0,
                HasDiagonalBars = true,
                DiagonalBarsPerCorner = 2,
                DiagonalBarDiameterMm = 16,
                DiagonalBarLengthMm = 0.0,
                JambExtraBarCountPerSide = 3,
                JambExtraBarDiameterMm = 20,
                HeaderExtraBarCountPerEdge = 2,
                HeaderExtraBarDiameterMm = 18
            };

            var trimmers = WallOpeningTrimmerCalculator.CalculateTrimmers(op, anchorageLengthMm: 500.0);

            Assert.Equal(8, trimmers.DiagonalBarCount);
            Assert.Equal(1000.0, trimmers.DiagonalBarLengthMm);
            Assert.Equal(6, trimmers.JambExtraBarCount);
            Assert.Equal(3400.0, trimmers.JambExtraBarLengthMm);
            Assert.Equal(4, trimmers.HeaderSillExtraBarCount);
            Assert.Equal(2200.0, trimmers.HeaderSillExtraBarLengthMm);
        }

        [Fact]
        public void EtabsWallTableMapper_MalformedAndMessyTable_ParsesGracefully()
        {
            string messy = "  STORY ; PIER MARK ; TW ; LENGTH ; P ; V2 ; M3 ; ASB ; AVH \n" +
                           "  Story1 ; P-WALL1 ; 350 ; 4200 ; -4500.5 ; 520.2 ; 2100.0 ; 3200 ; 650 \n" +
                           "  Story2 ; P-WALL1 ; 350 ; 4200 ; -3800.0 ; 480.0 ; 1850.0 ; 2800 ; 600 \n";

            var designs = EtabsWallTableMapper.MapTableContent(messy);

            Assert.Equal(2, designs.Count);
            Assert.Equal("P-WALL1", designs[0].PierMark);
            Assert.Equal(350.0, designs[0].ThicknessMm);
            Assert.Equal(4200.0, designs[0].LengthMm);
            Assert.Equal(-4500.5, designs[0].AxialForceP_kN);
            Assert.Equal(3200.0, designs[0].RequiredBoundaryAsbMm2);
        }

        [Fact]
        public void EtabsWallImportService_MultiPierMultiStory_BuildsDetailedShearWalls()
        {
            string raw = "Story\tPier\tThickness\tLength\tP\tV2\tM3\tAsb\tAvh\n" +
                         "Story1\tW1\t300\t3200\t-3000\t350\t1200\t2200\t500\n" +
                         "Story2\tW1\t300\t3200\t-2500\t300\t1000\t1800\t450\n" +
                         "Story1\tW2\t350\t4500\t-4500\t500\t2200\t3500\t700\n";

            var service = new EtabsWallImportService();
            var walls = service.ImportFromTableText(raw);

            Assert.Equal(2, walls.Count);
            var w1 = walls.First(w => w.WallMark == "W1");
            Assert.Equal(2, w1.Stories.Count);
            Assert.Equal(2, w1.BoundaryElements.Count);
            Assert.True(w1.BoundaryElements[0].TotalSteelAreaMm2 >= 2200.0);
            Assert.True(w1.WebReinforcement.IsDoubleCurtain);
        }

        [Fact]
        public void WallPlanScheduleWorkbookGenerator_GeneratesVachSheetWithCompleteFormatting()
        {
            var wall = new ShearWallModel { WallMark = "V-TEST", ThicknessMm = 300.0, LengthMm = 3600.0 };
            wall.Stories.Add(new WallStorySegment { StoryIndex = 0, StoryName = "Tầng 1", ThicknessMm = 300.0, LengthMm = 3600.0 });
            wall.Stories.Add(new WallStorySegment { StoryIndex = 1, StoryName = "Tầng 2", ThicknessMm = 300.0, LengthMm = 3600.0 });
            wall.BoundaryElements.Add(WallBoundaryCalculator.CreateBoundaryElement("BE-L", WallBoundaryPosition.Left, 3600.0, 300.0, 2000.0));
            wall.WebReinforcement = new WallWebReinforcementModel { VerticalBarDiameterMm = 12, HorizontalBarDiameterMm = 12 };

            using var ms = new MemoryStream();
            WallPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<ShearWallModel> { wall }, ms);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet("Vach");
            Assert.NotNull(ws);
            Assert.Equal("Story", ws.Cell(2, 2).GetString());
            Assert.Equal("TotalWeightKg", ws.Cell(2, 12).GetString());
            Assert.Equal("Tầng 1", ws.Cell(3, 2).GetString());
            Assert.Equal("V-TEST", ws.Cell(3, 3).GetString());
            Assert.True(ws.Cell(3, 12).GetDouble() > 0.0);
        }
    }
}
