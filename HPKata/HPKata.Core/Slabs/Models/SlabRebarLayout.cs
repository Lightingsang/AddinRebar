using System;
using System.Collections.Generic;
using HPKata.Core.Models;

namespace HPKata.Core.Slabs.Models
{
    /// <summary>
    /// Aggregated slab rebar layout holding panel geometry, mesh layers, chair stools, and takeoff items.
    /// </summary>
    public class SlabRebarLayout
    {
        public string PanelMark { get; set; } = "S1";
        public SlabPanelModel? Panel { get; set; }
        public List<SlabMeshModel> MeshLayers { get; set; } = new();
        public SlabChairStoolModel? ChairStool { get; set; }
        public List<RebarItem> ScheduleItems { get; set; } = new();

        public double ConcreteVolumeM3 { get; set; }
        public double FormworkAreaM2 { get; set; }
        public double TotalSteelWeightKg { get; set; }

        public static SlabRebarLayout FromPanel(SlabPanelModel panel)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            return new SlabRebarLayout
            {
                PanelMark = panel.PanelMark,
                Panel = panel
            };
        }
    }
}
