using System;
using System.Collections.Generic;

namespace HPKata.Core.Stairs.Models
{
    public class SectionBarPointModel
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int DiameterMm { get; set; } = 16;
        public string BarMark { get; set; } = "1";
        public int LayerIndex { get; set; } = 1;
    }

    public class SectionHoopModel
    {
        public List<(double X, double Y)> Vertices { get; set; } = new();
        public int DiameterMm { get; set; } = 8;
        public string HookAngle { get; set; } = "135";
        public string Mark { get; set; } = "d1";
    }

    /// <summary>
    /// Represents a universal generic cross-section cut (vemc) across structural elements.
    /// </summary>
    public class GenericSectionModel
    {
        public string SectionTitle { get; set; } = "MẶT CẮT 1-1";
        public int ScaleRatio { get; set; } = 20;
        public double WidthMm { get; set; } = 300.0;
        public double HeightMm { get; set; } = 600.0;
        public double ConcreteCoverMm { get; set; } = 25.0;

        public List<SectionBarPointModel> LongitudinalBars { get; set; } = new();
        public List<SectionHoopModel> StirrupHoops { get; set; } = new();
        public string ComponentMark { get; set; } = "D1";

        public void Validate()
        {
            if (WidthMm <= 0 || HeightMm <= 0)
                throw new ArgumentException("Section dimensions must be positive.");
            if (ConcreteCoverMm <= 0)
                throw new ArgumentException("Cover must be positive.");
        }
    }
}
