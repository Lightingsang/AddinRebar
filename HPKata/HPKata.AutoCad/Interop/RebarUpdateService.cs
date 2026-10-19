using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Measures modified polyline geometry, recalculates cutting lengths, and updates
    /// linked block attributes for command 'ud' (update_thep).
    /// </summary>
    public static class RebarUpdateService
    {
        public static bool UpdateRebarAndLinkedTag(
            Polyline polyline,
            Transaction tr,
            bool applyBendDeduction = false)
        {
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            KataXDataRecord record = XDataService.ReadXData(polyline);
            List<double> segmentLengths = RebarGeometryService.MeasureSegmentLengths(polyline);
            double cutLength = RebarGeometryService.CalculateCuttingLength(polyline, record.Phi, applyBendDeduction);

            // Update polyline XData with new segment lengths
            polyline.UpgradeOpen();
            record.ChieuDaiCacDoanMm = segmentLengths.ToArray();
            XDataService.WriteXData(polyline, record);

            // Check linked dimension line for count update
            UpdateFromLinkedDimension(polyline.Database, record, tr);

            // Update linked block reference attributes
            if (!string.IsNullOrEmpty(record.LinkedHandle))
            {
                Entity? linked = HandleLinkService.FindLinkedEntity(polyline.Database, record.LinkedHandle, tr);
                if (linked is BlockReference tagBlock)
                {
                    PushUpdatesToTag(tagBlock, record, cutLength, tr);
                    return true;
                }
            }

            return false;
        }

        private static void UpdateFromLinkedDimension(Database db, KataXDataRecord record, Transaction tr)
        {
            if (string.IsNullOrEmpty(record.LinkedDimHandle)) return;

            Entity? dimEntity = HandleLinkService.FindLinkedEntity(db, record.LinkedDimHandle, tr);
            if (dimEntity is Dimension dim)
            {
                double measuredMm = dim.Measurement;
                int spacingMm = ExtractSpacing(record.KhoangCach);
                if (spacingMm > 0 && measuredMm > 0.0)
                {
                    int calculatedCount = (int)Math.Floor(measuredMm / spacingMm) + 1;
                    record.SoLuong = calculatedCount.ToString();
                }
            }
        }

        private static void PushUpdatesToTag(
            BlockReference tagBlock,
            KataXDataRecord record,
            double cutLength,
            Transaction tr)
        {
            var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CD1T"] = cutLength.ToString("F0")
            };

            if (record.STT > 0) updates["SH"] = record.STT.ToString();
            if (record.Phi > 0) updates["DK"] = record.Phi.ToString();
            if (!string.IsNullOrEmpty(record.KhoangCach)) updates["KC"] = record.KhoangCach;
            if (!string.IsNullOrEmpty(record.SoLuong)) updates["SL"] = record.SoLuong;

            string combined = $"{record.SoLuong}Ø{record.Phi}{record.KhoangCach}".Trim();
            updates["DKKC"] = combined;

            BlockAttributeService.UpdateAttributes(tagBlock, updates, tr);
        }

        private static int ExtractSpacing(string? spacingStr)
        {
            if (string.IsNullOrWhiteSpace(spacingStr)) return 0;
            string clean = spacingStr.Trim().TrimStart('@', 'a', 'A');
            return int.TryParse(clean, out int val) ? val : 0;
        }
    }
}
