using System;

namespace HPKata.Core.Beams.Models
{
    /// <summary>
    /// Type of foundation framing beam.
    /// </summary>
    public enum FoundationBeamType
    {
        FoundationGradeBeam = 0, // ve_dam_mong
        FoundationTieBeam = 1    // ve_giang_mong / ve_giang_shop
    }

    /// <summary>
    /// Model for foundation grade beams and ground tie beams connecting footings or pile caps.
    /// </summary>
    public sealed class TieBeamModel
    {
        public string BeamName { get; set; } = string.Empty;
        public FoundationBeamType BeamType { get; set; } = FoundationBeamType.FoundationTieBeam;
        public string LeftFootingName { get; set; } = string.Empty;
        public string RightFootingName { get; set; } = string.Empty;
        public double ClearSpanMm { get; set; }
        public double WidthMm { get; set; } = 300.0;
        public double HeightMm { get; set; } = 500.0;
        public double TopCoverMm { get; set; } = 50.0;
        public double BottomCoverMm { get; set; } = 50.0;
        public double SideCoverMm { get; set; } = 40.0;
        public int TopBarCount { get; set; } = 2;
        public int TopBarDiameterMm { get; set; } = 18;
        public int BottomBarCount { get; set; } = 2;
        public int BottomBarDiameterMm { get; set; } = 18;
        public int StirrupDiameterMm { get; set; } = 8;
        public double StirrupSpacingDenseMm { get; set; } = 100.0;
        public double StirrupSpacingSparseMm { get; set; } = 200.0;
        public double AnchorageMultiplier { get; set; } = 40.0;

        public double CalculateAnchorageLengthMm(int diameterMm) =>
            AnchorageMultiplier * diameterMm;

        public void Validate()
        {
            if (ClearSpanMm <= 0.0)
            {
                throw new ArgumentException("Clear span must be greater than zero.", nameof(ClearSpanMm));
            }

            if (WidthMm <= 0.0 || HeightMm <= 0.0)
            {
                throw new ArgumentException("Dimensions must be positive.");
            }
        }
    }
}
