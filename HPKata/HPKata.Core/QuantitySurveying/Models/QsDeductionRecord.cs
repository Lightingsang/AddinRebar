namespace HPKata.Core.QuantitySurveying.Models
{
    /// <summary>
    /// Represents an individual volume or surface deduction between two intersecting structural members.
    /// </summary>
    public class QsDeductionRecord
    {
        public string IntersectingElementId { get; set; } = string.Empty;
        public string IntersectingMark { get; set; } = string.Empty;
        public QsMemberType IntersectingType { get; set; }
        public double DeductedVolumeM3 { get; set; }
        public double DeductedAreaM2 { get; set; }
        public string Reason { get; set; } = string.Empty;

        public QsDeductionRecord()
        {
        }

        public QsDeductionRecord(
            string intersectingId,
            string mark,
            QsMemberType type,
            double volumeM3,
            double areaM2,
            string reason)
        {
            IntersectingElementId = intersectingId;
            IntersectingMark = mark;
            IntersectingType = type;
            DeductedVolumeM3 = volumeM3;
            DeductedAreaM2 = areaM2;
            Reason = reason;
        }
    }
}
