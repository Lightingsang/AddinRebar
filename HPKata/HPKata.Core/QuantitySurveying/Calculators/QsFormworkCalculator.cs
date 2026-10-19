using System;
using System.Collections.Generic;
using HPKata.Core.QuantitySurveying.Models;

namespace HPKata.Core.QuantitySurveying.Calculators
{
    /// <summary>
    /// Calculates net shuttering contact areas and evaluates deductions
    /// caused by adjacent structural elements contacting formwork faces.
    /// </summary>
    public static class QsFormworkCalculator
    {
        private const double Mm2ToM2 = 1e-6;

        public static Dictionary<string, QsFormworkResult> Evaluate(IEnumerable<QsElementModel> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            var results = new Dictionary<string, QsFormworkResult>();
            var elementList = new List<QsElementModel>(elements);

            foreach (var elem in elementList)
            {
                var (soffit, side) = QsTakeoffCalculator.CalculateGrossFormwork(elem);
                results[elem.Id] = new QsFormworkResult(soffit, side);
            }

            for (int i = 0; i < elementList.Count; i++)
            {
                for (int j = 0; j < elementList.Count; j++)
                {
                    if (i == j) continue;
                    EvaluateContactPair(elementList[i], elementList[j], results);
                }
            }

            return results;
        }

        private static void EvaluateContactPair(
            QsElementModel target,
            QsElementModel other,
            Dictionary<string, QsFormworkResult> results)
        {
            if (!target.IntersectingElementIds.Contains(other.Id) &&
                !other.IntersectingElementIds.Contains(target.Id))
            {
                return;
            }

            DeductColumnContact(target, other, results);
            DeductBeamContact(target, other, results);
            DeductFootingContact(target, other, results);
        }

        private static void DeductColumnContact(
            QsElementModel target,
            QsElementModel other,
            Dictionary<string, QsFormworkResult> results)
        {
            if (target.MemberType != QsMemberType.Column) return;

            if (other.MemberType == QsMemberType.Beam)
            {
                double w = Math.Max(0.0, other.WidthMm);
                double h = Math.Max(0.0, other.HeightMm);
                double contactAreaM2 = w * h * Mm2ToM2 * Math.Max(1, other.MemberCount);
                results[target.Id].AddDeduction(new QsDeductionRecord(
                    other.Id, other.Mark, QsMemberType.Beam, 0.0, contactAreaM2,
                    "Beam end contact deduction on column formwork"));
            }
            else if (other.MemberType == QsMemberType.Slab)
            {
                double slabThick = Math.Max(0.0, other.ThicknessMm > 0 ? other.ThicknessMm : other.HeightMm);
                double perim = 2.0 * (Math.Max(0.0, target.WidthMm) + Math.Max(0.0, target.HeightMm));
                double contactAreaM2 = perim * slabThick * Mm2ToM2 * Math.Max(1, target.MemberCount);
                results[target.Id].AddDeduction(new QsDeductionRecord(
                    other.Id, other.Mark, QsMemberType.Slab, 0.0, contactAreaM2,
                    "Slab thickness contact on column perimeter formwork"));
            }
        }

        private static void DeductBeamContact(
            QsElementModel target,
            QsElementModel other,
            Dictionary<string, QsFormworkResult> results)
        {
            if (target.MemberType != QsMemberType.Beam) return;

            if (target.IsPrimary && other.MemberType == QsMemberType.Beam && !other.IsPrimary)
            {
                double w = Math.Max(0.0, other.WidthMm);
                double h = Math.Max(0.0, other.HeightMm);
                double contactAreaM2 = w * h * Mm2ToM2 * Math.Max(1, other.MemberCount);
                results[target.Id].AddDeduction(new QsDeductionRecord(
                    other.Id, other.Mark, QsMemberType.Beam, 0.0, contactAreaM2,
                    "Secondary beam end contact on primary beam formwork"));
            }
            else if (other.MemberType == QsMemberType.Slab)
            {
                double slabThick = Math.Max(0.0, other.ThicknessMm > 0 ? other.ThicknessMm : other.HeightMm);
                double len = Math.Max(0.0, target.LengthMm);
                double contactAreaM2 = len * slabThick * Mm2ToM2 * Math.Max(1, target.MemberCount);
                results[target.Id].AddDeduction(new QsDeductionRecord(
                    other.Id, other.Mark, QsMemberType.Slab, 0.0, contactAreaM2,
                    "Slab thickness contact on beam side formwork"));
            }
        }

        private static void DeductFootingContact(
            QsElementModel target,
            QsElementModel other,
            Dictionary<string, QsFormworkResult> results)
        {
            if (target.MemberType == QsMemberType.Footing && other.MemberType == QsMemberType.Beam)
            {
                double w = Math.Max(0.0, other.WidthMm);
                double h = Math.Max(0.0, other.HeightMm);
                double contactAreaM2 = w * h * Mm2ToM2 * Math.Max(1, other.MemberCount);
                results[target.Id].AddDeduction(new QsDeductionRecord(
                    other.Id, other.Mark, QsMemberType.Beam, 0.0, contactAreaM2,
                    "Beam framing into footing side formwork"));
            }
        }
    }
}
