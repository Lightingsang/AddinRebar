using System.Collections.Generic;

namespace HPKata.Core.QuantitySurveying.Models
{
    /// <summary>
    /// Consolidated summary of QS takeoff results by category and project-wide.
    /// </summary>
    public class QsTakeoffSummary
    {
        public List<QsTakeoffItem> Items { get; set; } = new List<QsTakeoffItem>();

        public double TotalVolumeM3 { get; set; }
        public double TotalFormworkM2 { get; set; }

        public double FootingVolumeM3 { get; set; }
        public double FootingFormworkM2 { get; set; }

        public double ColumnVolumeM3 { get; set; }
        public double ColumnFormworkM2 { get; set; }

        public double WallVolumeM3 { get; set; }
        public double WallFormworkM2 { get; set; }

        public double BeamVolumeM3 { get; set; }
        public double BeamFormworkM2 { get; set; }

        public double SlabVolumeM3 { get; set; }
        public double SlabFormworkM2 { get; set; }

        public double StairVolumeM3 { get; set; }
        public double StairFormworkM2 { get; set; }
    }
}
