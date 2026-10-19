namespace HPKata.Core.QuantitySurveying.Models
{
    /// <summary>
    /// Aggregated line item for QS schedule tables and Excel export.
    /// Columns match Kata_QS: Drawing Name, Item Mark, Count, Area, Length, Height, Total Area, Total Length, Total Volume, Formwork Area.
    /// </summary>
    public class QsTakeoffItem
    {
        public string DrawingName { get; set; } = string.Empty;
        public string Story { get; set; } = string.Empty;
        public string ItemMark { get; set; } = string.Empty;
        public QsMemberType MemberType { get; set; }
        public int Count { get; set; } = 1;
        public double AreaM2 { get; set; }
        public double LengthM { get; set; }
        public double HeightM { get; set; }
        public double TotalAreaM2 { get; set; }
        public double TotalLengthM { get; set; }
        public double TotalVolumeM3 { get; set; }
        public double FormworkAreaM2 { get; set; }
    }
}
