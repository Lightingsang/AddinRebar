using System;

namespace HPKata.Core.Slabs.Models
{
    /// <summary>
    /// Represents a two-way reinforced concrete slab panel with orthogonal spans, boundary supports, and design parameters.
    /// </summary>
    public class SlabPanelModel
    {
        public string PanelMark { get; set; } = "S1";
        public double SpanXMm { get; set; } = 4000.0;
        public double SpanYMm { get; set; } = 5000.0;
        public double ThicknessMm { get; set; } = 120.0;
        public double CoverMm { get; set; } = 15.0;

        public double SupportWidthLeftMm { get; set; } = 220.0;
        public double SupportWidthRightMm { get; set; } = 220.0;
        public double SupportWidthBottomMm { get; set; } = 220.0;
        public double SupportWidthTopMm { get; set; } = 220.0;

        public int BottomMeshXDiaMm { get; set; } = 10;
        public double BottomMeshXSpacingMm { get; set; } = 150.0;
        public int BottomMeshYDiaMm { get; set; } = 10;
        public double BottomMeshYSpacingMm { get; set; } = 150.0;

        public double TopSupportRatio { get; set; } = 0.25;
        public int TopSupportDiaMm { get; set; } = 10;
        public double TopSupportSpacingMm { get; set; } = 150.0;

        public bool IsCantilever { get; set; }
        public double CantileverLengthMm { get; set; }

        public bool HasCornerCrackBars { get; set; }
        public int CornerBarDiaMm { get; set; } = 10;
        public double CornerBarSpacingMm { get; set; } = 150.0;

        public int MemberCount { get; set; } = 1;
        public string ConcreteGrade { get; set; } = "B25";
        public string SteelGrade { get; set; } = "CB300-T";

        public double AspectRatio => Math.Max(SpanXMm, SpanYMm) / Math.Min(SpanXMm, SpanYMm);
        public bool IsTwoWay => AspectRatio <= 2.0;

        public void Validate()
        {
            if (SpanXMm <= 0 || SpanYMm <= 0)
                throw new ArgumentException("Slab spans must be positive.");
            if (ThicknessMm <= 0)
                throw new ArgumentException("Slab thickness must be positive.");
            if (CoverMm <= 0)
                throw new ArgumentException("Concrete cover must be positive.");
            if (BottomMeshXSpacingMm <= 0 || BottomMeshYSpacingMm <= 0)
                throw new ArgumentException("Mesh bar spacings must be positive.");
            if (MemberCount < 1) MemberCount = 1;
        }
    }
}
