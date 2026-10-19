using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Reads and writes AutoCAD BlockReference attribute tags (e.g., kata_block_KHT, thep, dangCoupler).
    /// </summary>
    public static class BlockAttributeService
    {
        public static Dictionary<string, string> ReadAttributes(BlockReference blockRef, Transaction tr)
        {
            if (blockRef == null) throw new ArgumentNullException(nameof(blockRef));
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (ObjectId attId in blockRef.AttributeCollection)
            {
                if (tr.GetObject(attId, OpenMode.ForRead) is AttributeReference attRef)
                {
                    dict[attRef.Tag] = attRef.TextString ?? string.Empty;
                }
            }
            return dict;
        }

        public static void UpdateAttributes(
            BlockReference blockRef,
            IReadOnlyDictionary<string, string> updates,
            Transaction tr)
        {
            if (blockRef == null) throw new ArgumentNullException(nameof(blockRef));
            if (updates == null || updates.Count == 0) return;
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            foreach (ObjectId attId in blockRef.AttributeCollection)
            {
                if (tr.GetObject(attId, OpenMode.ForRead) is AttributeReference attRef)
                {
                    if (updates.TryGetValue(attRef.Tag, out string? newVal) && newVal != null)
                    {
                        if (attRef.TextString != newVal)
                        {
                            attRef.UpgradeOpen();
                            attRef.TextString = newVal;
                            attRef.AdjustAlignment(blockRef.Database);
                        }
                    }
                }
            }
        }

        public static KataTagBlockData ReadTagBlock(BlockReference blockRef, Transaction tr)
        {
            Dictionary<string, string> attrs = ReadAttributes(blockRef, tr);
            return KataBlockAttributeSerializer.ReadTagBlock(attrs);
        }

        public static void WriteTagBlock(BlockReference blockRef, KataTagBlockData data, Transaction tr)
        {
            Dictionary<string, string> updates = KataBlockAttributeSerializer.WriteTagBlock(data);
            UpdateAttributes(blockRef, updates, tr);
        }

        public static KataCouplerBlockData ReadCoupler(BlockReference blockRef, Transaction tr)
        {
            Dictionary<string, string> attrs = ReadAttributes(blockRef, tr);
            return KataBlockAttributeSerializer.ReadCoupler(attrs);
        }

        public static void WriteCoupler(BlockReference blockRef, KataCouplerBlockData data, Transaction tr)
        {
            Dictionary<string, string> updates = KataBlockAttributeSerializer.WriteCoupler(data);
            UpdateAttributes(blockRef, updates, tr);
        }
    }
}
