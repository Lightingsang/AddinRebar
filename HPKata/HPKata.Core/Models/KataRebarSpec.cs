using System;

namespace HPKata.Core.Models
{
    /// <summary>
    /// Rebar array distribution layout rules.
    /// </summary>
    public enum KataLayoutRule
    {
        Single = 0,
        MaximumSpacing = 1,
        FixedNumber = 2
    }

    /// <summary>
    /// Host-neutral structural rebar specification model in metric millimeters.
    /// </summary>
    public class KataRebarSpec
    {
        public KataPoint3D OriginMm { get; set; } = KataPoint3D.Zero;
        public KataVector3D DirectionL { get; set; } = KataVector3D.UnitX;
        public KataVector3D DirectionH { get; set; } = KataVector3D.UnitZ;
        public string Partition { get; set; } = string.Empty;
        public string ShapeName { get; set; } = string.Empty;
        public string ScheduleMark { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int DiameterMm { get; set; }
        public KataLayoutRule LayoutRule { get; set; } = KataLayoutRule.MaximumSpacing;
        public int Quantity { get; set; }
        public double SpacingMm { get; set; }
        public double ArrayLengthMm { get; set; }
        public double[] ParametersMm { get; set; } = Array.Empty<double>();
    }
}
