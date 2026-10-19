using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Columns.Calculators;
using HPKata.Core.Columns.Models;
using HPKata.Core.Columns.Persistence;
using Xunit;

namespace HPKata.E2E.Tests.Tier4_RealWorldScenarios
{
    /// <summary>
    /// E2E Scenario: Multi-Story High-Rise Column Detailing (800x800 -> 700x700 -> 600x600).
    /// Exercises crank bends (1:6 slope), 11.7m commercial bar division, staggered splices,
    /// seismic confinement zones, schedule aggregation, and ClosedXML export.
    /// </summary>
    public sealed class Tier4_Scenario2_MultiStoryColumnTests
    {
        [Fact]
        public void MultiStoryColumn_EndToEndDetailing_GeneratesCompliantLayoutAndWorkbook()
        {
            var layout = BuildSampleTowerColumnLayout();

            // Verify transition strategies
            double step1 = ColumnCrankBendCalculator.CalculateStepDown(800.0, 700.0, true);
            Assert.Equal(50.0, step1);
            Assert.Equal(ColumnTransitionStrategy.CrankBend, ColumnCrankBendCalculator.DetermineStrategy(step1));
            Assert.Equal(300.0, ColumnCrankBendCalculator.CalculateCrankLength(step1));

            // Verify commercial bar division
            double totalH = layout.TotalHeightMm; // 18000mm
            Assert.Equal(18000.0, totalH);
            var segments = ColumnSpliceCalculator.SegmentCommercialColumnBars(
                0.0, totalH, 25, 3600.0, isStaggeredTier: true);
            Assert.True(segments.Count >= 2);

            // Verify Schedule Aggregation
            var items = ColumnScheduleAggregator.AggregateSchedule(layout);
            Assert.True(items.Count > 0);

            // Verify Excel Workbook Generation
            using var ms = new MemoryStream();
            ColumnPlanScheduleWorkbookGenerator.GenerateWorkbookStream(new List<ColumnRebarLayout> { layout }, ms);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet("Cot");
            Assert.NotNull(ws);
            Assert.Equal("C-TOWER-1", ws.Cell(3, 3).GetString());
        }

        private static ColumnRebarLayout BuildSampleTowerColumnLayout()
        {
            var layout = new ColumnRebarLayout { ColumnMark = "C-TOWER-1" };
            double[] widths = { 800.0, 800.0, 700.0, 700.0, 600.0 };
            double curElev = 0.0;

            for (int i = 0; i < widths.Length; i++)
            {
                var story = new ColumnStorySegment
                {
                    StoryIndex = i,
                    StoryName = $"Tầng {i + 1}",
                    BottomElevationMm = curElev,
                    StoryHeightMm = 3600.0,
                    BeamDepthMm = 650.0,
                    WidthMm = widths[i],
                    DepthMm = widths[i],
                    Section = TcvnColumnRebarOptimizer.Optimize(widths[i], widths[i], 3200.0),
                    TieDistribution = new ColumnTieDistributionModel
                    {
                        DenseSpacingMm = 100.0,
                        SparseSpacingMm = 200.0,
                        JointSpacingMm = 150.0,
                        ConfinementHeightMm = ColumnConfinementCalculator.CalculateConfinementHeight(widths[i], widths[i], 2950.0)
                    }
                };
                layout.Stories.Add(story);
                layout.StoryTies[i] = story.TieDistribution;
                curElev += story.StoryHeightMm;
            }

            return layout;
        }
    }
}
