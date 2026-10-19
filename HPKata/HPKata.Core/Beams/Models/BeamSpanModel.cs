using System;
using System.Collections.Generic;

namespace HPKata.Core.Beams.Models
{
    /// <summary>
    /// Represents a single structural span within a multi-span continuous concrete beam.
    /// </summary>
    public sealed class BeamSpanModel
    {
        public int SpanIndex { get; set; }
        public string SpanName { get; set; } = string.Empty;
        public double ClearLengthMm { get; set; }
        public double LeftSupportWidthMm { get; set; }
        public double RightSupportWidthMm { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double TopElevationDeltaMm { get; set; }
        public double SoffitDropMm { get; set; }
        public double LeftSlabThicknessMm { get; set; }
        public double RightSlabThicknessMm { get; set; }
        public bool IsCantileverLeft { get; set; }
        public bool IsCantileverRight { get; set; }
        public List<BeamIntersectionModel> Intersections { get; } = new List<BeamIntersectionModel>();

        public double CenterToCenterLengthMm =>
            ClearLengthMm + (LeftSupportWidthMm + RightSupportWidthMm) / 2.0;

        public void Validate()
        {
            if (ClearLengthMm <= 0.0)
            {
                throw new ArgumentException("Span clear length must be greater than zero.", nameof(ClearLengthMm));
            }

            if (WidthMm <= 0.0)
            {
                throw new ArgumentException("Span width must be greater than zero.", nameof(WidthMm));
            }

            if (HeightMm <= 0.0)
            {
                throw new ArgumentException("Span height must be greater than zero.", nameof(HeightMm));
            }

            if (LeftSupportWidthMm < 0.0 || RightSupportWidthMm < 0.0)
            {
                throw new ArgumentException("Support widths cannot be negative.");
            }
        }
    }
}
