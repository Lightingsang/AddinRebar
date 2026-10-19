using System;

namespace HPKata.Core.Tagging
{
    /// <summary>
    /// Fabrication shop bundle tag payload data.
    /// </summary>
    public class QrTagPayload
    {
        public string Project { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public string Order { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public string Mark { get; set; } = string.Empty;
        public string ShapeCode { get; set; } = "01";
        public int DiameterMm { get; set; }
        public double CutLengthMm { get; set; }
        public int Quantity { get; set; }
        public double WeightKg { get; set; }
        public string SteelGrade { get; set; } = "CB400-V";
        public double[] SegmentsMm { get; set; } = Array.Empty<double>();

        public override string ToString() =>
            $"Tag: {Project}/{Component} #{Mark} D{DiameterMm} (Qty: {Quantity})";
    }
}
