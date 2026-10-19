using System;
using HPKata.Core.Columns.Models;
using HPKata.Core.Excel.Data;

namespace HPKata.Core.Excel.Mappers
{
    /// <summary>
    /// Bi-directional mapper between KataColumnSheetData and ColumnRebarLayout.
    /// </summary>
    public static class KataColumnDataMapper
    {
        public static ColumnRebarLayout ToRebarLayout(KataColumnSheetData sheetData)
        {
            if (sheetData == null) throw new ArgumentNullException(nameof(sheetData));

            var layout = new ColumnRebarLayout
            {
                ColumnMark = sheetData.ColumnMark,
                FoundationBottomElevationMm = sheetData.FoundationBottomElevationM * 1000.0
            };

            double currentElevation = 0.0;
            for (int i = 0; i < sheetData.Stories.Count; i++)
            {
                var s = sheetData.Stories[i];
                var seg = MapStorySegment(s, sheetData, currentElevation);
                layout.Stories.Add(seg);
                layout.StoryTies[i] = seg.TieDistribution;
                currentElevation += seg.StoryHeightMm;
            }

            return layout;
        }

        private static ColumnStorySegment MapStorySegment(KataColumnStoryData s, KataColumnSheetData sheetData, double bottomElevationMm)
        {
            return new ColumnStorySegment
            {
                StoryIndex = s.StoryIndex,
                StoryName = s.StoryName,
                BottomElevationMm = bottomElevationMm,
                StoryHeightMm = s.StoryHeightMm,
                BeamDepthMm = s.BeamDepthMm,
                WidthMm = s.WidthMm,
                DepthMm = s.DepthMm,
                ConcreteGrade = sheetData.ConcreteGrade,
                ConcreteCoverMm = sheetData.ConcreteCoverMm,
                Section = new ColumnSectionModel
                {
                    WidthMm = s.WidthMm,
                    DepthMm = s.DepthMm,
                    CornerDiameterMm = s.CornerDiameter,
                    SideCountB = s.SideCountB,
                    SideDiameterB = s.SideDiameterB,
                    SideCountH = s.SideCountH,
                    SideDiameterH = s.SideDiameterH
                },
                TieDistribution = new ColumnTieDistributionModel
                {
                    TieDiameterMm = sheetData.TieDiameter,
                    DenseSpacingMm = s.DenseSpacing,
                    SparseSpacingMm = s.SparseSpacing,
                    ConfinementHeightMm = Math.Max(s.WidthMm, s.DepthMm)
                }
            };
        }

        public static KataColumnSheetData ToSheetData(ColumnRebarLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));

            var data = new KataColumnSheetData
            {
                ColumnMark = layout.ColumnMark,
                FoundationBottomElevationM = layout.FoundationBottomElevationMm / 1000.0
            };

            foreach (var seg in layout.Stories)
            {
                data.Stories.Add(new KataColumnStoryData
                {
                    StoryIndex = seg.StoryIndex,
                    StoryName = seg.StoryName,
                    StoryHeightMm = seg.StoryHeightMm,
                    BeamDepthMm = seg.BeamDepthMm,
                    WidthMm = seg.WidthMm,
                    DepthMm = seg.DepthMm,
                    CornerDiameter = seg.Section.CornerDiameterMm,
                    SideCountB = seg.Section.SideCountB,
                    SideDiameterB = seg.Section.SideDiameterB,
                    SideCountH = seg.Section.SideCountH,
                    SideDiameterH = seg.Section.SideDiameterH,
                    DenseSpacing = seg.TieDistribution.DenseSpacingMm,
                    SparseSpacing = seg.TieDistribution.SparseSpacingMm,
                    OuterTieSpec = $"d{seg.TieDistribution.TieDiameterMm}a{(int)seg.TieDistribution.DenseSpacingMm}/{(int)seg.TieDistribution.SparseSpacingMm}"
                });
            }

            return data;
        }
    }
}
