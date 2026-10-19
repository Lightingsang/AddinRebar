using System;
using System.Collections.Generic;
using HPKata.Core.Models;

namespace HPKata.Core.Foundations.Models
{
    /// <summary>
    /// Aggregated foundation rebar layout holding footing specifications, rebar schedules, and geometric metadata.
    /// </summary>
    public class FoundationRebarLayout
    {
        public string FoundationMark { get; set; } = "F1";
        public bool IsPileCap { get; set; }
        public IsolatedFootingModel? Footing { get; set; }
        public PileCapModel? PileCap { get; set; }
        public List<RebarItem> ScheduleItems { get; set; } = new();
        public double ConcreteVolumeM3 { get; set; }
        public double LeanConcreteVolumeM3 { get; set; }
        public double FormworkAreaM2 { get; set; }
        public double TotalSteelWeightKg { get; set; }

        public static FoundationRebarLayout FromFooting(IsolatedFootingModel footing)
        {
            if (footing == null) throw new ArgumentNullException(nameof(footing));
            return new FoundationRebarLayout
            {
                FoundationMark = footing.FootingMark,
                IsPileCap = false,
                Footing = footing
            };
        }

        public static FoundationRebarLayout FromPileCap(PileCapModel cap)
        {
            if (cap == null) throw new ArgumentNullException(nameof(cap));
            return new FoundationRebarLayout
            {
                FoundationMark = cap.CapMark,
                IsPileCap = true,
                PileCap = cap
            };
        }
    }
}
