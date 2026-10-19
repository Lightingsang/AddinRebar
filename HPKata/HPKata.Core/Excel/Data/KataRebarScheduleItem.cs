using System;

namespace HPKata.Core.Excel.Data
{
    /// <summary>
    /// Data transfer object representing an individual rebar schedule item for sheet 'Thong_ke'
    /// and Iso_QHPlus.xls export.
    /// </summary>
    public sealed class KataRebarScheduleItem
    {
        public string ComponentMark { get; set; } = string.Empty;
        public string BarMark { get; set; } = "1";
        public string ShapeCode { get; set; } = "00";
        public double[] SegmentsMm { get; set; } = Array.Empty<double>();
        public string Notes { get; set; } = string.Empty;
        public int DiameterMm { get; set; } = 20;
        public double UnlappedLengthMm { get; set; }
        public double CuttingLengthMm { get; set; }
        public int MemberCount { get; set; } = 1;
        public int CountPerMember { get; set; } = 1;
        public int TotalCount => MemberCount * CountPerMember;
        public double TotalLengthM => (CuttingLengthMm * TotalCount) / 1000.0;
        public double UnitMassKgPerM { get; set; }
        public double TotalWeightKg => TotalLengthM * UnitMassKgPerM;
        public double WeightPerMemberKg => MemberCount > 0 ? TotalWeightKg / MemberCount : 0.0;
        public bool IsCoupler { get; set; }
        public string SubLayer { get; set; } = string.Empty;
        public string FloorElevation { get; set; } = string.Empty;
    }
}
