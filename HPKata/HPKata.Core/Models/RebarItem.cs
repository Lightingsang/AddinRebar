using System;

namespace HPKata.Core.Models
{
    /// <summary>
    /// Represents an individual rebar item within a detailing schedule or structural member.
    /// </summary>
    public class RebarItem
    {
        public string ComponentMark { get; set; } = string.Empty;
        public string BarMark { get; set; } = string.Empty;
        public string ShapeCode { get; set; } = "01";
        public int DiameterMm { get; set; }
        public string Spacing { get; set; } = string.Empty;
        public int CountPerMember { get; set; } = 1;
        public int MemberCount { get; set; } = 1;
        public int TotalCount => CountPerMember * MemberCount;
        public double[] ParametersMm { get; set; } = Array.Empty<double>();
        public double SpecialAngleDeg { get; set; }
        public string Notes { get; set; } = string.Empty;
        public double CuttingLengthMm { get; set; }
        public double StructuralLengthMm { get; set; }
        public double TotalLengthM => (CuttingLengthMm * TotalCount) / 1000.0;
        public double UnitMassKgPerM { get; set; }
        public double TotalWeightKg => TotalLengthM * UnitMassKgPerM;
        public double WeightPerMemberKg => MemberCount > 0 ? TotalWeightKg / MemberCount : TotalWeightKg;
        public string FloorElevation { get; set; } = string.Empty;
        public string SubLayer { get; set; } = string.Empty;

        public override string ToString() =>
            $"{ComponentMark} #{BarMark}: D{DiameterMm} ({ShapeCode}) x{TotalCount}";
    }
}
