using System.Collections.Generic;

namespace HPKata.Core.Scheduling
{
    public class DiameterSummaryItem
    {
        public int DiameterMm { get; set; }
        public double TotalLengthM { get; set; }
        public double UnitMassKgPerM { get; set; }
        public double TotalWeightKg { get; set; }
    }

    public class TcvnDiameterBins
    {
        public double WireLe10Kg { get; set; }
        public double Deformed10To18Kg { get; set; }
        public double DeformedGt18Kg { get; set; }
        public double TotalProjectWeightKg => WireLe10Kg + Deformed10To18Kg + DeformedGt18Kg;
    }

    public class CouplerSummaryItem
    {
        public string ComponentMark { get; set; } = string.Empty;
        public int DiameterMm { get; set; }
        public int MemberCount { get; set; } = 1;
        public int CouplersPerMember { get; set; } = 1;
        public int TotalCouplers => MemberCount * CouplersPerMember;
        public string Notes { get; set; } = string.Empty;
    }

    public class RebarScheduleReport
    {
        public List<Models.RebarItem> Items { get; set; } = new();
        public List<DiameterSummaryItem> DiameterSummaries { get; set; } = new();
        public TcvnDiameterBins Bins { get; set; } = new();
        public List<CouplerSummaryItem> Couplers { get; set; } = new();
    }
}
