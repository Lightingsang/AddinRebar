using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.Core.Configuration;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Imports standard KataPro layers (kata_thep chu, kata_thep dai, kata_net thay, kata_net khuat,
    /// kata_dim, kata_text, kata_grid, kata_net manh, kata_net khung, kata_thep - V/H), linetypes
    /// (HIDDEN, CENTER), text styles (kata_text), dimension styles (kata_dim_*, kata_rai_thep),
    /// and block definitions directly from Iso_kata.dwg into the active AutoCAD database.
    /// </summary>
    public static class KataIsoTemplateImporter
    {
        public static void EnsureIsoTemplates(Database targetDb, Transaction tr)
        {
            if (targetDb == null) throw new ArgumentNullException(nameof(targetDb));
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            string? isoPath = ResolveIsoDwgPath();
            if (!string.IsNullOrEmpty(isoPath) && File.Exists(isoPath))
            {
                TryImportAllFromIsoDwg(isoPath!, targetDb, tr);
            }

            KataLayerHelper.EnsureStandardLayers(tr, targetDb);
        }

        private static void TryImportAllFromIsoDwg(string isoPath, Database targetDb, Transaction tr)
        {
            try
            {
                using var sourceDb = new Database(false, true);
                sourceDb.ReadDwgFile(isoPath, FileOpenMode.OpenForReadAndAllShare, true, string.Empty);

                var layerSnapshots = new List<(string Name, Color Color, string Linetype, LineWeight Weight, bool Plottable)>();
                using (var ocTr = sourceDb.TransactionManager.StartOpenCloseTransaction())
                {
                    CloneTableRecords(sourceDb, targetDb, ocTr, sourceDb.LinetypeTableId, targetDb.LinetypeTableId, includeAll: true);
                    CaptureAndCloneLayers(sourceDb, targetDb, ocTr, layerSnapshots);
                    CloneTableRecords(sourceDb, targetDb, ocTr, sourceDb.TextStyleTableId, targetDb.TextStyleTableId, includeAll: false);
                    CloneBlocks(sourceDb, targetDb, ocTr, tr);
                    CloneTableRecords(sourceDb, targetDb, ocTr, sourceDb.DimStyleTableId, targetDb.DimStyleTableId, includeAll: false);
                    ocTr.Commit();
                }

                ApplyLayerSnapshotsToTarget(targetDb, tr, layerSnapshots);
            }
            catch
            {
                // Fallback to KataLayerHelper.EnsureStandardLayers if Iso_kata.dwg cannot be opened.
            }
        }

        private static void CaptureAndCloneLayers(
            Database sourceDb,
            Database targetDb,
            Transaction ocTr,
            List<(string Name, Color Color, string Linetype, LineWeight Weight, bool Plottable)> snapshots)
        {
            var srcLt = (LayerTable)ocTr.GetObject(sourceDb.LayerTableId, OpenMode.ForRead);
            var ids = new ObjectIdCollection();

            foreach (ObjectId id in srcLt)
            {
                if (ocTr.GetObject(id, OpenMode.ForRead) is not LayerTableRecord ltr) continue;
                if (string.Equals(ltr.Name, "0", StringComparison.OrdinalIgnoreCase)) continue;

                string ltypeName = "Continuous";
                if (!ltr.LinetypeObjectId.IsNull &&
                    ocTr.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead) is LinetypeTableRecord lttRec)
                {
                    ltypeName = lttRec.Name;
                }

                snapshots.Add((ltr.Name, ltr.Color, ltypeName, ltr.LineWeight, ltr.IsPlottable));
                ids.Add(id);
            }

            if (ids.Count > 0)
            {
                var mapping = new IdMapping();
                sourceDb.WblockCloneObjects(ids, targetDb.LayerTableId, mapping, DuplicateRecordCloning.Replace, false);
            }
        }

        private static void ApplyLayerSnapshotsToTarget(
            Database targetDb,
            Transaction tr,
            List<(string Name, Color Color, string Linetype, LineWeight Weight, bool Plottable)> snapshots)
        {
            var lt = (LayerTable)tr.GetObject(targetDb.LayerTableId, OpenMode.ForRead);
            var ltt = (LinetypeTable)tr.GetObject(targetDb.LinetypeTableId, OpenMode.ForRead);

            foreach (var snap in snapshots)
            {
                ObjectId ltypeId = ltt.Has(snap.Linetype)
                    ? ltt[snap.Linetype]
                    : (ltt.Has("Continuous") ? ltt["Continuous"] : ObjectId.Null);

                if (lt.Has(snap.Name))
                {
                    var existing = (LayerTableRecord)tr.GetObject(lt[snap.Name], OpenMode.ForWrite);
                    existing.Color = snap.Color;
                    if (!ltypeId.IsNull) existing.LinetypeObjectId = ltypeId;
                    existing.LineWeight = snap.Weight;
                    existing.IsPlottable = snap.Plottable;
                }
                else
                {
                    lt.UpgradeOpen();
                    using var ltr = new LayerTableRecord
                    {
                        Name = snap.Name,
                        Color = snap.Color,
                        LineWeight = snap.Weight,
                        IsPlottable = snap.Plottable
                    };
                    if (!ltypeId.IsNull) ltr.LinetypeObjectId = ltypeId;
                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                }
            }
        }

        private static void CloneTableRecords(
            Database sourceDb,
            Database targetDb,
            Transaction ocTr,
            ObjectId srcTableId,
            ObjectId dstTableId,
            bool includeAll)
        {
            var table = (SymbolTable)ocTr.GetObject(srcTableId, OpenMode.ForRead);
            var ids = new ObjectIdCollection();

            foreach (ObjectId id in table)
            {
                if (ocTr.GetObject(id, OpenMode.ForRead) is not SymbolTableRecord rec) continue;
                if (includeAll || rec.Name.StartsWith("kata_", StringComparison.OrdinalIgnoreCase))
                {
                    ids.Add(id);
                }
            }

            if (ids.Count > 0)
            {
                var mapping = new IdMapping();
                sourceDb.WblockCloneObjects(ids, dstTableId, mapping, DuplicateRecordCloning.Replace, false);
            }
        }

        private static void CloneBlocks(Database sourceDb, Database targetDb, Transaction ocTr, Transaction dstTr)
        {
            var srcBt = (BlockTable)ocTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
            var dstBt = (BlockTable)dstTr.GetObject(targetDb.BlockTableId, OpenMode.ForRead);
            var ids = new ObjectIdCollection();

            foreach (ObjectId id in srcBt)
            {
                if (ocTr.GetObject(id, OpenMode.ForRead) is not BlockTableRecord btr) continue;
                if (btr.IsLayout || btr.IsAnonymous) continue;
                if (!dstBt.Has(btr.Name))
                {
                    ids.Add(id);
                }
            }

            if (ids.Count > 0)
            {
                var mapping = new IdMapping();
                sourceDb.WblockCloneObjects(ids, targetDb.BlockTableId, mapping, DuplicateRecordCloning.Ignore, false);
            }
        }

        public static string? ResolveIsoDwgPath()
        {
            string envIso = Path.Combine(KataInstallEnvironment.Current.RootDirectory, "Iso_kata.dwg");
            if (File.Exists(envIso)) return envIso;

            string cHpKata = @"C:\HPKata\Iso_kata.dwg";
            if (File.Exists(cHpKata)) return cHpKata;

            string sysRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";
            string legacyIso = Path.Combine(sysRoot, "kata" + "_pro", "Iso_kata.dwg");
            if (File.Exists(legacyIso)) return legacyIso;

            string? asmDir = Path.GetDirectoryName(typeof(KataIsoTemplateImporter).Assembly.Location);
            if (!string.IsNullOrEmpty(asmDir))
            {
                string localIso = Path.Combine(asmDir, "Iso_kata.dwg");
                if (File.Exists(localIso)) return localIso;
            }

            return null;
        }
    }
}
