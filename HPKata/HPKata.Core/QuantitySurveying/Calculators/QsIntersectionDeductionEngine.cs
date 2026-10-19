using System;
using System.Collections.Generic;
using HPKata.Core.QuantitySurveying.Models;

namespace HPKata.Core.QuantitySurveying.Calculators
{
    /// <summary>
    /// Evaluates 3D structural intersections and calculates volumetric deductions
    /// following the strict structural priority hierarchy:
    /// Footing > Column/Wall > Primary Beam > Secondary Beam > Slab.
    /// </summary>
    public static class QsIntersectionDeductionEngine
    {
        private const double Mm3ToM3 = 1e-9;

        public static Dictionary<string, QsVolumeResult> Evaluate(IEnumerable<QsElementModel> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            var results = new Dictionary<string, QsVolumeResult>();
            var elementList = new List<QsElementModel>(elements);

            foreach (var elem in elementList)
            {
                double gross = QsTakeoffCalculator.CalculateGrossVolume(elem);
                results[elem.Id] = new QsVolumeResult(gross, gross);
            }

            // Deduct embedded piles from footings
            DeductFootingPiles(elementList, results);

            // Deduct overlaps based on priority
            for (int i = 0; i < elementList.Count; i++)
            {
                for (int j = 0; j < elementList.Count; j++)
                {
                    if (i == j) continue;
                    EvaluateIntersectionPair(elementList[i], elementList[j], results);
                }
            }

            return results;
        }

        private static void DeductFootingPiles(
            List<QsElementModel> elements,
            Dictionary<string, QsVolumeResult> results)
        {
            foreach (var elem in elements)
            {
                if (elem.MemberType != QsMemberType.Footing) continue;
                if (elem.EmbeddedPileCount > 0 && elem.EmbeddedPileAreaMm2 > 0 && elem.EmbedmentDepthMm > 0)
                {
                    double pileVolM3 = elem.EmbeddedPileCount * elem.EmbeddedPileAreaMm2
                        * elem.EmbedmentDepthMm * Mm3ToM3 * Math.Max(1, elem.MemberCount);
                    results[elem.Id].AddDeduction(new QsDeductionRecord(
                        "PileGroup",
                        "Cọc",
                        QsMemberType.Footing,
                        pileVolM3,
                        0.0,
                        "Pile head embedment in footing"));
                }
            }
        }

        private static void EvaluateIntersectionPair(
            QsElementModel target,
            QsElementModel other,
            Dictionary<string, QsVolumeResult> results)
        {
            if (!IsIntersecting(target, other)) return;

            // Target = Column, Other = Footing (Footing > Column)
            if (target.MemberType == QsMemberType.Column && other.MemberType == QsMemberType.Footing)
            {
                DeductColumnFootingOverlap(target, other, results);
            }
            // Target = Beam, Other = Column/Wall (Column > Beam)
            else if (target.MemberType == QsMemberType.Beam &&
                     (other.MemberType == QsMemberType.Column || other.MemberType == QsMemberType.Wall))
            {
                DeductBeamColumnOverlap(target, other, results);
            }
            // Target = Secondary Beam, Other = Primary Beam (Primary > Secondary)
            else if (target.MemberType == QsMemberType.Beam && !target.IsPrimary &&
                     other.MemberType == QsMemberType.Beam && other.IsPrimary)
            {
                DeductSecondaryBeamOverlap(target, other, results);
            }
            // Target = Slab, Other = Column / Wall / Beam (All > Slab)
            else if (target.MemberType == QsMemberType.Slab)
            {
                DeductSlabIntersection(target, other, results);
            }
        }

        private static bool IsIntersecting(QsElementModel a, QsElementModel b)
        {
            if (a.IntersectingElementIds.Contains(b.Id) || b.IntersectingElementIds.Contains(a.Id))
            {
                return true;
            }
            return false;
        }

        private static void DeductColumnFootingOverlap(
            QsElementModel col,
            QsElementModel footing,
            Dictionary<string, QsVolumeResult> results)
        {
            double overlapDepth = Math.Min(col.EmbedmentDepthMm, footing.HeightMm);
            if (overlapDepth <= 0.0) return;

            double volM3 = col.WidthMm * col.HeightMm * overlapDepth * Mm3ToM3 * Math.Max(1, col.MemberCount);
            results[col.Id].AddDeduction(new QsDeductionRecord(
                footing.Id,
                footing.Mark,
                QsMemberType.Footing,
                volM3,
                0.0,
                "Footing takes precedence over embedded column base"));
        }

        private static void DeductBeamColumnOverlap(
            QsElementModel beam,
            QsElementModel col,
            Dictionary<string, QsVolumeResult> results)
        {
            double overlapLen = col.WidthMm > 0 ? col.WidthMm : col.HeightMm;
            double volM3 = beam.WidthMm * beam.HeightMm * overlapLen * 0.5 * Mm3ToM3 * Math.Max(1, beam.MemberCount);
            results[beam.Id].AddDeduction(new QsDeductionRecord(
                col.Id,
                col.Mark,
                col.MemberType,
                volM3,
                0.0,
                "Column/Wall takes precedence through joint"));
        }

        private static void DeductSecondaryBeamOverlap(
            QsElementModel secBeam,
            QsElementModel primBeam,
            Dictionary<string, QsVolumeResult> results)
        {
            double volM3 = secBeam.WidthMm * secBeam.HeightMm * primBeam.WidthMm * Mm3ToM3 * Math.Max(1, secBeam.MemberCount);
            results[secBeam.Id].AddDeduction(new QsDeductionRecord(
                primBeam.Id,
                primBeam.Mark,
                QsMemberType.Beam,
                volM3,
                0.0,
                "Primary beam takes precedence over secondary beam"));
        }

        private static void DeductSlabIntersection(
            QsElementModel slab,
            QsElementModel other,
            Dictionary<string, QsVolumeResult> results)
        {
            double slabThick = slab.ThicknessMm > 0 ? slab.ThicknessMm : slab.HeightMm;
            if (slabThick <= 0.0) return;

            double volM3 = 0.0;
            string reason = string.Empty;

            if (other.MemberType == QsMemberType.Column)
            {
                volM3 = other.WidthMm * other.HeightMm * slabThick * Mm3ToM3 * Math.Max(1, other.MemberCount);
                reason = "Column cross-section through slab";
            }
            else if (other.MemberType == QsMemberType.Beam)
            {
                volM3 = other.WidthMm * other.LengthMm * slabThick * Mm3ToM3 * Math.Max(1, other.MemberCount);
                reason = "Beam stem takes precedence over slab";
            }
            else if (other.MemberType == QsMemberType.Wall)
            {
                double wallThick = other.ThicknessMm > 0 ? other.ThicknessMm : other.WidthMm;
                volM3 = wallThick * other.LengthMm * slabThick * Mm3ToM3 * Math.Max(1, other.MemberCount);
                reason = "Wall thickness through slab";
            }

            if (volM3 > 0.0)
            {
                results[slab.Id].AddDeduction(new QsDeductionRecord(
                    other.Id,
                    other.Mark,
                    other.MemberType,
                    volM3,
                    0.0,
                    reason));
            }
        }
    }
}
