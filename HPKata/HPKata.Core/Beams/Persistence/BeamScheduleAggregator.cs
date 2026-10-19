using System;
using System.Collections.Generic;
using HPKata.Core.Beams.Models;
using HPKata.Core.Calculators;
using HPKata.Core.Models;

namespace HPKata.Core.Beams.Persistence
{
    /// <summary>
    /// Aggregates continuous beam reinforcement components into schedule items and barem summaries.
    /// </summary>
    public static class BeamScheduleAggregator
    {
        public static List<RebarItem> AggregateSchedule(BeamRebarLayout layout, int beamCount = 1)
        {
            var items = new List<RebarItem>();
            if (layout == null) return items;

            int markCounter = 1;
            string beamMark = string.IsNullOrWhiteSpace(layout.BeamName) ? "BEAM" : layout.BeamName;

            AddLongitudinalItems(items, beamMark, ref markCounter, layout.TopContinuousBars, beamCount);
            AddLongitudinalItems(items, beamMark, ref markCounter, layout.BottomContinuousBars, beamCount);
            AddExtraBarItems(items, beamMark, ref markCounter, layout.SupportTopExtraBars, beamCount, isTop: true);
            AddExtraBarItems(items, beamMark, ref markCounter, layout.MidspanBottomExtraBars, beamCount, isTop: false);
            AddStirrupItems(items, beamMark, ref markCounter, layout, beamCount);
            AddLongitudinalItems(items, beamMark, ref markCounter, layout.SkinBars, beamCount);

            return items;
        }

        private static void AddLongitudinalItems(
            List<RebarItem> items, string beamMark, ref int markCounter, IEnumerable<RebarRunModel> runs, int beamCount)
        {
            foreach (var run in runs)
            {
                items.Add(CreateLongitudinalItem(beamMark, markCounter++, run, beamCount));
            }
        }

        private static void AddExtraBarItems(
            List<RebarItem> items, string beamMark, ref int markCounter,
            Dictionary<int, List<ExtraBarLayerModel>> dict, int beamCount, bool isTop)
        {
            foreach (var kvp in dict)
            {
                foreach (var extra in kvp.Value)
                {
                    items.Add(CreateExtraItem(beamMark, markCounter++, extra, beamCount, isTop));
                }
            }
        }

        private static void AddStirrupItems(
            List<RebarItem> items, string beamMark, ref int markCounter, BeamRebarLayout layout, int beamCount)
        {
            foreach (var kvp in layout.StirrupZones)
            {
                int spanIndex = kvp.Key;
                var zone = kvp.Value;
                var span = spanIndex < layout.Spans.Count ? layout.Spans[spanIndex] : null;
                double b = span?.WidthMm ?? 300.0;
                double h = span?.HeightMm ?? 600.0;
                items.Add(CreateStirrupItem(beamMark, markCounter++, zone, b, h, beamCount));
            }
        }

        private static RebarItem CreateLongitudinalItem(
            string compMark, int mark, RebarRunModel run, int memberCount)
        {
            double hookExtra = (run.HasStartHook ? run.StartHookLengthMm : 0.0) +
                               (run.HasEndHook ? run.EndHookLengthMm : 0.0);
            double cutLength = run.LengthMm + hookExtra;

            return new RebarItem
            {
                ComponentMark = compMark,
                BarMark = mark.ToString(),
                ShapeCode = (run.HasStartHook || run.HasEndHook) ? "02" : "01",
                DiameterMm = run.DiameterMm,
                CountPerMember = run.BarCount,
                MemberCount = memberCount,
                CuttingLengthMm = Math.Round(cutLength, 1),
                StructuralLengthMm = run.LengthMm,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(run.DiameterMm)
            };
        }

        private static RebarItem CreateExtraItem(
            string compMark, int mark, ExtraBarLayerModel extra, int memberCount, bool isTop)
        {
            double len = extra.TotalLengthMm;
            return new RebarItem
            {
                ComponentMark = compMark,
                BarMark = mark.ToString(),
                ShapeCode = "01",
                DiameterMm = extra.DiameterMm,
                CountPerMember = extra.BarCount,
                MemberCount = memberCount,
                CuttingLengthMm = Math.Round(len, 1),
                StructuralLengthMm = len,
                SubLayer = isTop ? $"T{extra.LayerIndex}" : $"B{extra.LayerIndex}",
                UnitMassKgPerM = RebarCalculator.GetUnitMass(extra.DiameterMm)
            };
        }

        private static RebarItem CreateStirrupItem(
            string compMark, int mark, StirrupZoneDistributionModel zone, double b, double h, int memberCount)
        {
            double cover = 25.0;
            double coreB = Math.Max(50.0, b - 2.0 * cover);
            double coreH = Math.Max(50.0, h - 2.0 * cover);
            double hook135 = 2.0 * Math.Max(75.0, 7.5 * zone.StirrupDiameterMm);
            double perimeter = (2.0 * coreB) + (2.0 * coreH) + hook135;

            return new RebarItem
            {
                ComponentMark = compMark,
                BarMark = mark.ToString(),
                ShapeCode = "22",
                DiameterMm = zone.StirrupDiameterMm,
                Spacing = $"a{zone.LeftDenseSpacingMm}/a{zone.MidspanSparseSpacingMm}",
                CountPerMember = Math.Max(1, zone.EstimatedStirrupCount),
                MemberCount = memberCount,
                CuttingLengthMm = Math.Round(perimeter, 1),
                StructuralLengthMm = perimeter,
                UnitMassKgPerM = RebarCalculator.GetUnitMass(zone.StirrupDiameterMm)
            };
        }

        public static (double Le10Kg, double Between10And18Kg, double Gt18Kg, double TotalKg)
            CalculateWeightSummary(List<RebarItem> items)
        {
            double le10 = 0.0;
            double mid = 0.0;
            double gt18 = 0.0;

            foreach (var item in items)
            {
                double weight = item.TotalWeightKg;
                if (item.DiameterMm <= 10)
                {
                    le10 += weight;
                }
                else if (item.DiameterMm <= 18)
                {
                    mid += weight;
                }
                else
                {
                    gt18 += weight;
                }
            }

            return (Math.Round(le10, 2), Math.Round(mid, 2), Math.Round(gt18, 2), Math.Round(le10 + mid + gt18, 2));
        }
    }
}
