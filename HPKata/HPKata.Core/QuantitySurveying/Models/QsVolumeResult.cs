using System;
using System.Collections.Generic;

namespace HPKata.Core.QuantitySurveying.Models
{
    /// <summary>
    /// Holds gross volume, deductions, and calculated net volume in cubic meters (m³).
    /// </summary>
    public class QsVolumeResult
    {
        public double GrossVolumeM3 { get; set; }
        public double NetVolumeM3 { get; set; }
        public double DeductedVolumeM3 => Math.Max(0.0, GrossVolumeM3 - NetVolumeM3);
        public List<QsDeductionRecord> Deductions { get; set; } = new List<QsDeductionRecord>();

        public QsVolumeResult()
        {
        }

        public QsVolumeResult(double grossVolumeM3, double netVolumeM3)
        {
            GrossVolumeM3 = grossVolumeM3;
            NetVolumeM3 = netVolumeM3;
        }

        public void AddDeduction(QsDeductionRecord deduction)
        {
            if (deduction == null) return;
            Deductions.Add(deduction);
            NetVolumeM3 = Math.Max(0.0, NetVolumeM3 - deduction.DeductedVolumeM3);
        }
    }
}
