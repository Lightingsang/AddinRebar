using System;
using System.Collections.Generic;
using HPKata.Core.Models;
using HPKata.Core.Slabs.Calculators;
using HPKata.Core.Slabs.Models;

namespace HPKata.Core.Slabs.Persistence
{
    /// <summary>
    /// Aggregates slab panel models and layouts into standard List of RebarItem records.
    /// </summary>
    public static class SlabScheduleAggregator
    {
        public static List<RebarItem> AggregatePanel(SlabPanelModel panel)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            panel.Validate();

            var items = SlabRebarCalculator.CalculateSlabRebar(panel);

            // Add standard chair bar stool
            double areaM2 = (panel.SpanXMm / 1000.0) * (panel.SpanYMm / 1000.0);
            var stool = SlabChairCalculator.CalculateStool(
                panel.ThicknessMm, panel.CoverMm, panel.CoverMm,
                panel.TopSupportDiaMm, panel.BottomMeshXDiaMm, 10, 1.2, areaM2);
            items.Add(SlabChairCalculator.CreateChairRebarItem(panel.PanelMark, stool, panel.MemberCount));

            return items;
        }

        public static List<RebarItem> AggregateLayout(SlabRebarLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (layout.Panel != null)
                return AggregatePanel(layout.Panel);
            return new List<RebarItem>(layout.ScheduleItems);
        }
    }
}
