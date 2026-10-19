using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Columns.Calculators;
using HPKata.Core.Columns.Models;

namespace HPKata.Core.Columns.Etabs
{
    /// <summary>
    /// Service converting imported ETABS column structural designs into fully detailed ColumnRebarLayouts.
    /// </summary>
    public sealed class EtabsColumnImportService
    {
        public List<ColumnRebarLayout> ImportFromTableText(string tableText)
        {
            var designs = EtabsColumnTableMapper.MapTableContent(tableText);
            return ProcessDesigns(designs);
        }

        public List<ColumnRebarLayout> ProcessDesigns(List<EtabsColumnDesign> designs)
        {
            var layouts = new List<ColumnRebarLayout>();
            if (designs == null || designs.Count == 0) return layouts;

            var groupedByMark = designs.GroupBy(d => d.ColumnMark, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groupedByMark)
            {
                var layout = BuildColumnLayout(group.Key, group.ToList());
                layouts.Add(layout);
            }

            return layouts;
        }

        private static ColumnRebarLayout BuildColumnLayout(string mark, List<EtabsColumnDesign> storyDesigns)
        {
            var layout = new ColumnRebarLayout { ColumnMark = mark };
            double currentElevation = 0.0;

            for (int i = 0; i < storyDesigns.Count; i++)
            {
                var d = storyDesigns[i];
                var story = new ColumnStorySegment
                {
                    StoryIndex = i,
                    StoryName = string.IsNullOrWhiteSpace(d.StoryName) ? $"Story{i + 1}" : d.StoryName,
                    BottomElevationMm = currentElevation,
                    StoryHeightMm = d.HeightMm > 0.0 ? d.HeightMm : 3600.0,
                    BeamDepthMm = 600.0,
                    WidthMm = d.WidthMm,
                    DepthMm = d.DepthMm,
                    Section = TcvnColumnRebarOptimizer.Optimize(d.WidthMm, d.DepthMm, d.RequiredAstMm2)
                };

                var conf = ColumnConfinementCalculator.CalculateConfinement(
                    d.WidthMm, d.DepthMm, story.ClearHeightMm, story.BeamDepthMm, story.Section.CornerDiameterMm);

                story.TieDistribution = new ColumnTieDistributionModel
                {
                    DenseSpacingMm = conf.DenseSpacingA1Mm,
                    SparseSpacingMm = conf.SparseSpacingA2Mm,
                    JointSpacingMm = conf.JointSpacingAJointMm,
                    ConfinementHeightMm = conf.ConfinementHeightL0Mm
                };

                layout.Stories.Add(story);
                layout.StoryTies[i] = story.TieDistribution;
                currentElevation += story.StoryHeightMm;
            }

            GenerateLongitudinalBarRuns(layout);
            return layout;
        }

        private static void GenerateLongitudinalBarRuns(ColumnRebarLayout layout)
        {
            if (layout.Stories.Count == 0) return;
            var bottomStory = layout.Stories[0];
            int cornerDia = bottomStory.Section.CornerDiameterMm;
            double totalH = layout.TotalHeightMm;

            var segments = ColumnSpliceCalculator.SegmentCommercialColumnBars(
                0.0, totalH, cornerDia, bottomStory.StoryHeightMm, isStaggeredTier: false);

            int barCounter = 1;
            foreach (var seg in segments)
            {
                layout.LongitudinalBars.Add(new ColumnBarRunModel
                {
                    BarMark = $"C-{barCounter++}",
                    DiameterMm = cornerDia,
                    LengthMm = seg.LengthMm,
                    StartZMm = seg.StartZMm,
                    EndZMm = seg.EndZMm,
                    SpliceType = seg.IsLapSplice ? ColumnSpliceType.LapSplice : ColumnSpliceType.ContinuousStraight,
                    LapLengthMm = seg.LapLengthMm,
                    HasBottomHook = (seg.StartZMm <= 0.0),
                    BottomHookLengthMm = 250.0
                });
            }

            layout.FoundationDowels.Add(new ColumnStarterDowelModel
            {
                DowelMark = "DW-1",
                DiameterMm = cornerDia,
                Count = 4,
                EmbedmentDepthMm = 800.0,
                ProjectionLengthMm = 900.0,
                HasHook90 = true,
                HookLengthMm = 250.0
            });
        }
    }
}
