using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.QuantitySurveying.Calculators;
using HPKata.Core.QuantitySurveying.Models;

namespace HPKata.Core.QuantitySurveying.Persistence
{
    /// <summary>
    /// Aggregates element takeoffs, evaluates deductions, and produces a consolidated QsTakeoffSummary.
    /// </summary>
    public static class QsScheduleAggregator
    {
        public static QsTakeoffSummary Aggregate(IEnumerable<QsElementModel> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var elementList = new List<QsElementModel>(elements);

            var volumeDict = QsIntersectionDeductionEngine.Evaluate(elementList);
            var formworkDict = QsFormworkCalculator.Evaluate(elementList);

            var summary = new QsTakeoffSummary();
            var items = new List<QsTakeoffItem>();

            foreach (var elem in elementList)
            {
                var volResult = volumeDict.TryGetValue(elem.Id, out var vr) ? vr : new QsVolumeResult();
                var fwResult = formworkDict.TryGetValue(elem.Id, out var fr) ? fr : new QsFormworkResult();

                double areaM2 = (elem.PlanAreaMm2 > 0 ? elem.PlanAreaMm2 : elem.LengthMm * elem.WidthMm) * 1e-6;
                double lenM = elem.LengthMm * 1e-3;
                double heightM = elem.HeightMm * 1e-3;
                int count = Math.Max(1, elem.MemberCount);

                var item = new QsTakeoffItem
                {
                    DrawingName = elem.Story,
                    Story = elem.Story,
                    ItemMark = elem.Mark,
                    MemberType = elem.MemberType,
                    Count = count,
                    AreaM2 = areaM2,
                    LengthM = lenM,
                    HeightM = heightM,
                    TotalAreaM2 = areaM2 * count,
                    TotalLengthM = lenM * count,
                    TotalVolumeM3 = volResult.NetVolumeM3,
                    FormworkAreaM2 = fwResult.NetFormworkM2
                };

                items.Add(item);
                AccumulateCategoryTotals(summary, elem.MemberType, item.TotalVolumeM3, item.FormworkAreaM2);
            }

            summary.Items = items;
            summary.TotalVolumeM3 = items.Sum(i => i.TotalVolumeM3);
            summary.TotalFormworkM2 = items.Sum(i => i.FormworkAreaM2);

            return summary;
        }

        private static void AccumulateCategoryTotals(
            QsTakeoffSummary summary,
            QsMemberType type,
            double volumeM3,
            double formworkM2)
        {
            switch (type)
            {
                case QsMemberType.Footing:
                    summary.FootingVolumeM3 += volumeM3;
                    summary.FootingFormworkM2 += formworkM2;
                    break;
                case QsMemberType.Column:
                    summary.ColumnVolumeM3 += volumeM3;
                    summary.ColumnFormworkM2 += formworkM2;
                    break;
                case QsMemberType.Wall:
                    summary.WallVolumeM3 += volumeM3;
                    summary.WallFormworkM2 += formworkM2;
                    break;
                case QsMemberType.Beam:
                    summary.BeamVolumeM3 += volumeM3;
                    summary.BeamFormworkM2 += formworkM2;
                    break;
                case QsMemberType.Slab:
                    summary.SlabVolumeM3 += volumeM3;
                    summary.SlabFormworkM2 += formworkM2;
                    break;
                case QsMemberType.Stair:
                    summary.StairVolumeM3 += volumeM3;
                    summary.StairFormworkM2 += formworkM2;
                    break;
            }
        }
    }
}
