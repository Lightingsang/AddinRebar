using System;

namespace HPKata.Core.Foundations.Models
{
    public enum PedestalType
    {
        Vertical,
        Sloped,
        Stepped
    }

    /// <summary>
    /// Represents an isolated pad footing (móng đơn) with base slab and pedestal.
    /// Supports vertical, sloped (vát móng), and stepped (giật cấp) pedestals.
    /// </summary>
    public class IsolatedFootingModel
    {
        public string FootingMark { get; set; } = "M1";
        public double LengthMm { get; set; } = 1600.0;
        public double WidthMm { get; set; } = 1600.0;
        public double TotalHeightMm { get; set; } = 800.0;
        public double BaseSlabHeightMm { get; set; } = 350.0;
        public double PedestalHeightMm => Math.Max(0.0, TotalHeightMm - BaseSlabHeightMm);
        public double PedestalLengthMm { get; set; } = 500.0;
        public double PedestalWidthMm { get; set; } = 500.0;
        public PedestalType PedestalStyle { get; set; } = PedestalType.Sloped;

        public double LeanConcreteThicknessMm { get; set; } = 100.0;
        public double LeanConcreteOverhangMm { get; set; } = 100.0;

        public double CoverBottomMm { get; set; } = 50.0;
        public double CoverSideMm { get; set; } = 50.0;
        public double CoverTopMm { get; set; } = 50.0;

        public int BottomMeshXDiaMm { get; set; } = 14;
        public double BottomMeshXSpacingMm { get; set; } = 150.0;
        public int BottomMeshYDiaMm { get; set; } = 14;
        public double BottomMeshYSpacingMm { get; set; } = 150.0;

        public int StarterDowelCount { get; set; } = 4;
        public int StarterDowelDiaMm { get; set; } = 18;
        public double StarterDowelLapMm { get; set; } = 600.0;

        public int PedestalTieDiaMm { get; set; } = 8;
        public double PedestalTieSpacingMm { get; set; } = 150.0;

        public int MemberCount { get; set; } = 1;
        public string ConcreteGrade { get; set; } = "B25";
        public string SteelGrade { get; set; } = "CB400-V";

        public void Validate()
        {
            if (LengthMm <= 0 || WidthMm <= 0)
                throw new ArgumentException("Footing length and width must be positive.");
            if (TotalHeightMm <= BaseSlabHeightMm)
                throw new ArgumentException("Total height must exceed base slab height.");
            if (PedestalLengthMm >= LengthMm || PedestalWidthMm >= WidthMm)
                throw new ArgumentException("Pedestal dimensions cannot exceed base slab dimensions.");
            if (BottomMeshXSpacingMm <= 0 || BottomMeshYSpacingMm <= 0)
                throw new ArgumentException("Mesh bar spacings must be positive.");
            if (MemberCount < 1) MemberCount = 1;
        }
    }
}
