using System;
using System.Collections.Generic;

namespace HPKata.Core.QuantitySurveying.Models
{
    /// <summary>
    /// Host-neutral geometric representation of a structural member for Quantity Surveying (QS).
    /// Dimensions are stored in millimeters (mm) and converted to m³ and m² during calculation.
    /// </summary>
    public class QsElementModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Mark { get; set; } = string.Empty;
        public string Story { get; set; } = "Story 1";
        public QsMemberType MemberType { get; set; } = QsMemberType.Beam;
        public int MemberCount { get; set; } = 1;

        // Metric millimeter dimensions
        public double LengthMm { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double ThicknessMm { get; set; }
        public double TopElevationMm { get; set; }
        public double BottomElevationMm { get; set; }
        public double PlanAreaMm2 { get; set; }
        public double PerimeterMm { get; set; }

        public bool IsPrimary { get; set; } = true;
        public List<string> IntersectingElementIds { get; set; } = new List<string>();

        // Overhang or embedment options
        public double EmbedmentDepthMm { get; set; }
        public int EmbeddedPileCount { get; set; }
        public double EmbeddedPileAreaMm2 { get; set; }

        // Contact slab properties for beams
        public bool HasSlabLeft { get; set; }
        public double SlabThicknessLeftMm { get; set; }
        public bool HasSlabRight { get; set; }
        public double SlabThicknessRightMm { get; set; }

        public QsElementModel()
        {
        }
    }
}
