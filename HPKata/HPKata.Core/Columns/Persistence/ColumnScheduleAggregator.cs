using System;
using System.Collections.Generic;
using HPKata.Core.Calculators;
using HPKata.Core.Columns.Models;
using HPKata.Core.Models;

namespace HPKata.Core.Columns.Persistence
{
    /// <summary>
    /// Aggregates multi-story column rebar components into standard schedule items and steel summaries.
    /// </summary>
    public static class ColumnScheduleAggregator
    {
        public static List<RebarItem> AggregateSchedule(ColumnRebarLayout layout, int memberCount = 1)
        {
            var items = new List<RebarItem>();
            if (layout == null) return items;

            int markCounter = 1;
            string compMark = string.IsNullOrWhiteSpace(layout.ColumnMark) ? "COL" : layout.ColumnMark;

            AddLongitudinalItems(items, compMark, ref markCounter, layout.LongitudinalBars, memberCount);
            AddDowelItems(items, compMark, ref markCounter, layout.FoundationDowels, memberCount);
            AddTieItems(items, compMark, ref markCounter, layout, memberCount);

            return items;
        }

        private static void AddLongitudinalItems(
            List<RebarItem> items, string compMark, ref int mark, IEnumerable<ColumnBarRunModel> bars, int count)
        {
            foreach (var bar in bars)
            {
                items.Add(new RebarItem
                {
                    ComponentMark = compMark,
                    BarMark = mark++.ToString(),
                    ShapeCode = (bar.HasBottomHook || bar.HasTopHook) ? "02" : "01",
                    DiameterMm = bar.DiameterMm,
                    CountPerMember = 1,
                    MemberCount = count,
                    CuttingLengthMm = bar.TotalCuttingLengthMm,
                    StructuralLengthMm = bar.LengthMm,
                    UnitMassKgPerM = RebarCalculator.GetUnitMass(bar.DiameterMm)
                });
            }
        }

        private static void AddDowelItems(
            List<RebarItem> items, string compMark, ref int mark, IEnumerable<ColumnStarterDowelModel> dowels, int count)
        {
            foreach (var d in dowels)
            {
                items.Add(new RebarItem
                {
                    ComponentMark = compMark,
                    BarMark = mark++.ToString(),
                    ShapeCode = d.HasHook90 ? "02" : "01",
                    DiameterMm = d.DiameterMm,
                    CountPerMember = d.Count,
                    MemberCount = count,
                    CuttingLengthMm = d.TotalCuttingLengthMm,
                    StructuralLengthMm = d.TotalLengthMm,
                    UnitMassKgPerM = RebarCalculator.GetUnitMass(d.DiameterMm)
                });
            }
        }

        private static void AddTieItems(
            List<RebarItem> items, string compMark, ref int mark, ColumnRebarLayout layout, int count)
        {
            foreach (var story in layout.Stories)
            {
                layout.StoryTies.TryGetValue(story.StoryIndex, out var tie);
                var dist = tie ?? story.TieDistribution;
                int totalSets = dist.CalculateTotalSets(story.ClearHeightMm, story.BeamDepthMm);
                if (totalSets <= 0) continue;

                double perimeter = 2.0 * (story.WidthMm + story.DepthMm) + 200.0;
                items.Add(new RebarItem
                {
                    ComponentMark = $"{compMark}-{story.StoryName}",
                    BarMark = mark++.ToString(),
                    ShapeCode = "51",
                    DiameterMm = dist.TieDiameterMm,
                    CountPerMember = totalSets,
                    MemberCount = count,
                    CuttingLengthMm = Math.Round(perimeter, 1),
                    StructuralLengthMm = perimeter,
                    UnitMassKgPerM = RebarCalculator.GetUnitMass(dist.TieDiameterMm)
                });
            }
        }
    }
}
