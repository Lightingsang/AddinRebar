using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Shapes;

namespace HPKata.Core.Scheduling
{
    /// <summary>
    /// Aggregation engine for rebar schedules, diameter summaries, and coupler schedules.
    /// </summary>
    public static class RebarScheduleAggregator
    {
        public static RebarScheduleReport GenerateReport(IEnumerable<RebarItem> items, bool applyBendDeduction = false)
        {
            var processedItems = new List<RebarItem>();
            foreach (var item in items)
            {
                var shape = RebarShapeCatalog.NormalizeShapeCode(item.ShapeCode);
                if (item.CuttingLengthMm <= 0.0)
                {
                    item.CuttingLengthMm = RebarShapeCatalog.CalculateCuttingLength(
                        shape,
                        item.ParametersMm,
                        item.SpecialAngleDeg,
                        item.DiameterMm,
                        applyBendDeduction);
                }

                if (item.UnitMassKgPerM <= 0.0)
                {
                    item.UnitMassKgPerM = RebarCalculator.GetUnitMass(item.DiameterMm);
                }

                processedItems.Add(item);
            }

            var summaries = SummarizeByDiameter(processedItems);
            var bins = CalculateTcvnBins(summaries);
            var couplers = ExtractCouplers(processedItems);

            return new RebarScheduleReport
            {
                Items = processedItems,
                DiameterSummaries = summaries,
                Bins = bins,
                Couplers = couplers
            };
        }

        public static List<DiameterSummaryItem> SummarizeByDiameter(IEnumerable<RebarItem> items)
        {
            var groups = items
                .GroupBy(i => i.DiameterMm)
                .OrderBy(g => g.Key);

            var list = new List<DiameterSummaryItem>();
            foreach (var g in groups)
            {
                int dia = g.Key;
                double totalLen = g.Sum(i => i.TotalLengthM);
                double mass = RebarCalculator.GetUnitMass(dia);
                list.Add(new DiameterSummaryItem
                {
                    DiameterMm = dia,
                    TotalLengthM = totalLen,
                    UnitMassKgPerM = mass,
                    TotalWeightKg = totalLen * mass
                });
            }

            return list;
        }

        public static TcvnDiameterBins CalculateTcvnBins(IEnumerable<DiameterSummaryItem> summaries)
        {
            var bins = new TcvnDiameterBins();
            foreach (var s in summaries)
            {
                if (s.DiameterMm <= 10)
                {
                    bins.WireLe10Kg += s.TotalWeightKg;
                }
                else if (s.DiameterMm <= 18)
                {
                    bins.Deformed10To18Kg += s.TotalWeightKg;
                }
                else
                {
                    bins.DeformedGt18Kg += s.TotalWeightKg;
                }
            }
            return bins;
        }

        public static List<CouplerSummaryItem> ExtractCouplers(IEnumerable<RebarItem> items)
        {
            var list = new List<CouplerSummaryItem>();
            var couplerItems = items.Where(i =>
                !string.IsNullOrEmpty(i.Notes) &&
                (i.Notes.IndexOf("coupler", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 i.Notes.IndexOf("ren", StringComparison.OrdinalIgnoreCase) >= 0));

            var grouped = couplerItems.GroupBy(i => new { i.ComponentMark, i.DiameterMm, i.Notes });
            foreach (var g in grouped)
            {
                int memberCount = g.First().MemberCount;
                int couplersPerMember = g.Sum(x => x.CountPerMember);
                list.Add(new CouplerSummaryItem
                {
                    ComponentMark = g.Key.ComponentMark,
                    DiameterMm = g.Key.DiameterMm,
                    MemberCount = memberCount,
                    CouplersPerMember = couplersPerMember,
                    Notes = g.Key.Notes
                });
            }

            return list;
        }
    }
}
