using System.Collections.Generic;

namespace HPKata.Core.Beams.Models
{
    /// <summary>
    /// Geometric and detailing parameters for secondary beam intersections (dam_giao, giao_dam, cat_dam).
    /// </summary>
    public sealed class BeamIntersectionModel
    {
        public int SpanIndex { get; set; }
        public double DistanceAlongSpanMm { get; set; }
        public string SecondaryBeamName { get; set; } = string.Empty;
        public double SecondaryBeamWidthMm { get; set; } = 220.0;
        public double SecondaryBeamHeightMm { get; set; } = 350.0;
        public int HangerStirrupCount { get; set; } = 5;
        public int HangerStirrupDiameterMm { get; set; } = 10;
        public double HangerStirrupSpacingMm { get; set; } = 50.0;
        public List<double> HangerOffsetsMm { get; set; } = new List<double>();
        public bool RequiresAntiCrackBars { get; set; }
        public int AntiCrackBarDiameterMm { get; set; } = 14;
        public int AntiCrackBarCount { get; set; } = 2;
        public double AntiCrackBarLengthMm { get; set; } = 1200.0;

        public double TotalHangerZoneWidthMm =>
            (HangerStirrupCount > 1 ? (HangerStirrupCount - 1) * HangerStirrupSpacingMm : 0.0);
    }
}
