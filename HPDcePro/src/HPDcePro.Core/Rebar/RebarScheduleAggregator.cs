using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDcePro.Core.Rebar
{
    public sealed class ScheduleItem
    {
        public string ElementMark { get; set; } = string.Empty;
        public string BarMark { get; set; } = string.Empty;
        public int DiameterMm { get; set; }
        public string ShapeCode { get; set; } = "00";
        public double LengthMm { get; set; }
        public int QuantityPerElement { get; set; } = 1;
        public int ElementCount { get; set; } = 1;

        public int TotalQuantity => QuantityPerElement * ElementCount;
        public double TotalLengthM => (LengthMm * TotalQuantity) / 1000.0;
        public double UnitMassKgPerM => RebarScheduleAggregator.GetUnitMass(DiameterMm);
        public double TotalWeightKg => TotalLengthM * UnitMassKgPerM;
    }

    public sealed class ElementScheduleSummary
    {
        public string ElementMark { get; set; } = string.Empty;
        public List<ScheduleItem> Items { get; set; } = new();
        public double TotalLengthM => Items.Sum(i => i.TotalLengthM);
        public double TotalWeightKg => Items.Sum(i => i.TotalWeightKg);
    }

    public sealed class DiameterScheduleSummary
    {
        public int DiameterMm { get; set; }
        public int TotalBarCount { get; set; }
        public double TotalLengthM { get; set; }
        public double UnitMassKgPerM { get; set; }
        public double TotalWeightKg { get; set; }
    }

    public sealed class ScheduleSummaryReport
    {
        public List<ScheduleItem> Items { get; set; } = new();
        public List<ElementScheduleSummary> ElementSummaries { get; set; } = new();
        public List<DiameterScheduleSummary> DiameterSummaries { get; set; } = new();
        public double TotalWeightKg => DiameterSummaries.Sum(d => d.TotalWeightKg);
        public double TotalWeightTons => TotalWeightKg / 1000.0;
        public double WeightD10AndUnderKg => DiameterSummaries.Where(d => d.DiameterMm <= 10).Sum(d => d.TotalWeightKg);
        public double WeightD12ToD18Kg => DiameterSummaries.Where(d => d.DiameterMm > 10 && d.DiameterMm <= 18).Sum(d => d.TotalWeightKg);
        public double WeightAboveD18Kg => DiameterSummaries.Where(d => d.DiameterMm > 18).Sum(d => d.TotalWeightKg);
    }

    /// <summary>
    /// Rebar Schedule Aggregator (ThongKeThep) per TCVN 1651:2018.
    /// </summary>
    public static class RebarScheduleAggregator
    {
        private static readonly Dictionary<int, double> Tcvn1651MassTable = new()
        {
            { 6, 0.222 },
            { 8, 0.395 },
            { 10, 0.617 },
            { 12, 0.888 },
            { 14, 1.210 },
            { 16, 1.580 },
            { 18, 2.000 },
            { 20, 2.470 },
            { 22, 2.980 },
            { 25, 3.850 },
            { 28, 4.830 },
            { 32, 6.310 },
            { 36, 7.990 },
            { 40, 9.860 }
        };

        public static double GetUnitMass(int diameterMm)
        {
            if (diameterMm <= 0) return 0.0;
            if (Tcvn1651MassTable.TryGetValue(diameterMm, out double mass))
            {
                return mass;
            }
            return (diameterMm * diameterMm) / 162.2;
        }

        public static ScheduleSummaryReport Aggregate(IEnumerable<ScheduleItem> items)
        {
            var itemList = (items ?? Enumerable.Empty<ScheduleItem>()).ToList();
            var elementSummaries = BuildElementSummaries(itemList);
            var diameterSummaries = BuildDiameterSummaries(itemList);

            return new ScheduleSummaryReport
            {
                Items = itemList,
                ElementSummaries = elementSummaries,
                DiameterSummaries = diameterSummaries
            };
        }

        private static List<ElementScheduleSummary> BuildElementSummaries(List<ScheduleItem> items)
        {
            return items
                .GroupBy(i => i.ElementMark)
                .OrderBy(g => g.Key)
                .Select(g => new ElementScheduleSummary
                {
                    ElementMark = g.Key,
                    Items = g.OrderBy(i => i.BarMark).ToList()
                })
                .ToList();
        }

        private static List<DiameterScheduleSummary> BuildDiameterSummaries(List<ScheduleItem> items)
        {
            return items
                .GroupBy(i => i.DiameterMm)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    int d = g.Key;
                    double unitMass = GetUnitMass(d);
                    double lenM = g.Sum(x => x.TotalLengthM);
                    return new DiameterScheduleSummary
                    {
                        DiameterMm = d,
                        TotalBarCount = g.Sum(x => x.TotalQuantity),
                        TotalLengthM = lenM,
                        UnitMassKgPerM = unitMass,
                        TotalWeightKg = lenM * unitMass
                    };
                })
                .ToList();
        }
    }
}
