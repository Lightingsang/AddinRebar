using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Manages bidirectional handle linking (1005 XData) between rebar polylines and tag blocks,
    /// clone re-binding (cl), and link severing (dl).
    /// </summary>
    public static class HandleLinkService
    {
        public static void LinkRecords(
            KataXDataRecord rebarRecord,
            string rebarHandle,
            KataXDataRecord tagRecord,
            string tagHandle)
        {
            if (rebarRecord == null) throw new ArgumentNullException(nameof(rebarRecord));
            if (tagRecord == null) throw new ArgumentNullException(nameof(tagRecord));

            rebarRecord.LinkedHandle = tagHandle ?? string.Empty;
            rebarRecord.LinkType = "THEP_DON";

            tagRecord.LinkedHandle = rebarHandle ?? string.Empty;
            tagRecord.LinkType = "THEP_DON";
        }

        public static void SeverRecordLink(KataXDataRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            record.LinkedHandle = string.Empty;
            record.LinkType = string.Empty;
        }

        public static void RebindRecordHandles(
            KataXDataRecord record,
            IReadOnlyDictionary<string, string> oldToNewHandleMap)
        {
            if (record == null || oldToNewHandleMap == null) return;

            if (!string.IsNullOrEmpty(record.LinkedHandle) &&
                oldToNewHandleMap.TryGetValue(record.LinkedHandle, out string? newHandle) &&
                newHandle != null)
            {
                record.LinkedHandle = newHandle;
            }
        }

        public static void CreateLink(
            Entity rebarPolyline,
            BlockReference tagBlock,
            Transaction tr)
        {
            if (rebarPolyline == null) throw new ArgumentNullException(nameof(rebarPolyline));
            if (tagBlock == null) throw new ArgumentNullException(nameof(tagBlock));
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            rebarPolyline.UpgradeOpen();
            tagBlock.UpgradeOpen();

            string barHandle = rebarPolyline.Handle.ToString();
            string tagHandle = tagBlock.Handle.ToString();

            KataXDataRecord barRecord = XDataService.ReadXData(rebarPolyline);
            KataXDataRecord tagRecord = XDataService.ReadXData(tagBlock);

            LinkRecords(barRecord, barHandle, tagRecord, tagHandle);

            XDataService.WriteXData(rebarPolyline, barRecord);
            XDataService.WriteXData(tagBlock, tagRecord);

            SyncInitialAttributes(rebarPolyline, tagBlock, barRecord, tr);
        }

        private static void SyncInitialAttributes(
            Entity rebarPolyline,
            BlockReference tagBlock,
            KataXDataRecord barRecord,
            Transaction tr)
        {
            var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (barRecord.STT > 0) updates["SH"] = barRecord.STT.ToString();
            if (barRecord.Phi > 0) updates["DK"] = barRecord.Phi.ToString();
            if (!string.IsNullOrEmpty(barRecord.KhoangCach)) updates["KC"] = barRecord.KhoangCach;
            if (!string.IsNullOrEmpty(barRecord.SoLuong)) updates["SL"] = barRecord.SoLuong;

            if (rebarPolyline is Polyline pl)
            {
                double cutLen = RebarGeometryService.CalculateCuttingLength(pl, barRecord.Phi, false);
                updates["CD1T"] = cutLen.ToString("F0");
            }

            string combined = $"{barRecord.SoLuong}Ø{barRecord.Phi}{barRecord.KhoangCach}".Trim();
            updates["DKKC"] = combined;

            BlockAttributeService.UpdateAttributes(tagBlock, updates, tr);
        }

        public static bool SeverLink(Entity entity, Transaction tr)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            KataXDataRecord record = XDataService.ReadXData(entity);
            string linkedHandle = record.LinkedHandle;

            if (string.IsNullOrEmpty(linkedHandle)) return false;

            entity.UpgradeOpen();
            SeverRecordLink(record);
            XDataService.WriteXData(entity, record);

            Entity? partner = FindLinkedEntity(entity.Database, linkedHandle, tr);
            if (partner != null && !partner.IsErased)
            {
                partner.UpgradeOpen();
                KataXDataRecord partnerRecord = XDataService.ReadXData(partner);
                SeverRecordLink(partnerRecord);
                XDataService.WriteXData(partner, partnerRecord);
            }
            return true;
        }

        public static void RebindClonedEntities(
            IReadOnlyDictionary<string, Entity> oldHandleToCloneMap,
            Transaction tr)
        {
            if (oldHandleToCloneMap == null || oldHandleToCloneMap.Count == 0) return;
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            var handleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in oldHandleToCloneMap)
            {
                handleMap[kvp.Key] = kvp.Value.Handle.ToString();
            }

            foreach (var kvp in oldHandleToCloneMap)
            {
                Entity clone = kvp.Value;
                KataXDataRecord record = XDataService.ReadXData(clone);

                if (!string.IsNullOrEmpty(record.LinkedHandle))
                {
                    RebindRecordHandles(record, handleMap);
                    clone.UpgradeOpen();
                    XDataService.WriteXData(clone, record);
                }
            }
        }

        public static Entity? FindLinkedEntity(Database db, string handleStr, Transaction tr)
        {
            if (db == null || string.IsNullOrWhiteSpace(handleStr) || tr == null) return null;

            try
            {
                long val = Convert.ToInt64(handleStr, 16);
                var handle = new Handle(val);
                if (db.TryGetObjectId(handle, out ObjectId id) && !id.IsNull && !id.IsErased)
                {
                    return tr.GetObject(id, OpenMode.ForRead) as Entity;
                }
            }
            catch (Exception)
            {
                // Return null if handle cannot be resolved in active database
            }
            return null;
        }
    }
}
