using System;
using System.Collections.Generic;

namespace HPKata.Core.QuantitySurveying.Models
{
    /// <summary>
    /// Holds gross formwork surface area, deductions, and calculated net contact area in square meters (m²).
    /// </summary>
    public class QsFormworkResult
    {
        public double GrossFormworkM2 { get; set; }
        public double SoffitFormworkM2 { get; set; }
        public double SideFormworkM2 { get; set; }
        public double NetFormworkM2 { get; set; }
        public double DeductedAreaM2 => Math.Max(0.0, GrossFormworkM2 - NetFormworkM2);
        public List<QsDeductionRecord> Deductions { get; set; } = new List<QsDeductionRecord>();

        public QsFormworkResult()
        {
        }

        public QsFormworkResult(double soffitM2, double sideM2)
        {
            SoffitFormworkM2 = soffitM2;
            SideFormworkM2 = sideM2;
            GrossFormworkM2 = soffitM2 + sideM2;
            NetFormworkM2 = GrossFormworkM2;
        }

        public void AddDeduction(QsDeductionRecord deduction)
        {
            if (deduction == null) return;
            Deductions.Add(deduction);
            NetFormworkM2 = Math.Max(0.0, NetFormworkM2 - deduction.DeductedAreaM2);
        }
    }
}
