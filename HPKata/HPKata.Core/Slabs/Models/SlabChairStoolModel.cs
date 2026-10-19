using System;

namespace HPKata.Core.Slabs.Models
{
    /// <summary>
    /// Represents a chair/spacer bar stool (thép chân chó kê sàn) supporting top slab reinforcement.
    /// Supports 3D stools (dang35), U-stools (dang02), and Z-stools (dang05).
    /// </summary>
    public class SlabChairStoolModel
    {
        public double SlabThicknessMm { get; set; } = 120.0;
        public double TopCoverMm { get; set; } = 15.0;
        public double BottomCoverMm { get; set; } = 15.0;
        public int TopMeshDiaMm { get; set; } = 10;
        public int BottomMeshDiaMm { get; set; } = 10;
        public int ChairDiaMm { get; set; } = 10;

        public double StoolHeightMm { get; set; } = 70.0;
        public double FootLengthMm { get; set; } = 150.0;
        public double SaddleLengthMm { get; set; } = 200.0;

        public double DensityPerM2 { get; set; } = 1.2;
        public int TotalCount { get; set; } = 24;
        public string ShapeCode { get; set; } = "35";

        public double CuttingLengthMm => ShapeCode switch
        {
            "35" => 2.0 * FootLengthMm + 2.0 * StoolHeightMm + SaddleLengthMm,
            "02" => 2.0 * StoolHeightMm + SaddleLengthMm,
            "05" => FootLengthMm + StoolHeightMm + SaddleLengthMm,
            _ => 2.0 * FootLengthMm + 2.0 * StoolHeightMm + SaddleLengthMm
        };
    }
}
