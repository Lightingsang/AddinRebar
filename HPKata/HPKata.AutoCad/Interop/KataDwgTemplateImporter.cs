using System;
using System.IO;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.Core.Configuration;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Service that imports standard layers, text styles, dimension styles,
    /// and 119 block definitions from Iso_kata.dwg into an AutoCAD Database via WblockCloneObjects.
    /// </summary>
    public class KataDwgTemplateImporter : IKataDwgTemplateImporter
    {
        private static readonly (string Name, short ColorIndex)[] CanonicalLayers =
        [
            ("kata_thep chu", 2),
            ("kata_thep dai", 4),
            ("kata_thep - V", 2),
            ("kata_thep - H", 4),
            ("kata_dim", 1),
            ("kata_net thay", 7),
            ("kata_net khung", 6),
            ("kata_net khuat", 8),
            ("kata_grid", 8),
            ("kata_net manh", 9),
            ("kata_text", 3),
            ("Defpoints", 7)
        ];

        public const string StandardTextStyleName = "kata_text";
        public const string StandardFontFileName = "arial.ttf";
        public const double StandardTextWidthFactor = 0.80;

        public static System.Collections.Generic.IReadOnlyList<(string Name, short ColorIndex)> StandardLayers => CanonicalLayers;

        public static string ResolveTemplatePath(string? templateDwgPath = null) =>
            string.IsNullOrWhiteSpace(templateDwgPath)
                ? KataInstallEnvironment.Current.IsoKataDwgPath
                : templateDwgPath!;

        public KataTemplateImportResult ImportTemplateObjects(Database targetDb, string? templateDwgPath = null)
        {
            if (ReferenceEquals(targetDb, null)) throw new ArgumentNullException(nameof(targetDb));
            return ImportTemplateObjectsCore(targetDb, templateDwgPath);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private KataTemplateImportResult ImportTemplateObjectsCore(Database targetDb, string? templateDwgPath)
        {
            string dwgPath = ResolveTemplatePath(templateDwgPath);

            if (!File.Exists(dwgPath))
            {
                ApplyFallback(targetDb);
                return new KataTemplateImportResult
                {
                    Success = true,
                    UsedFallback = true,
                    Message = $"Template file not found at '{dwgPath}'. Applied standard fallback layers and styles."
                };
            }

            try
            {
                return CloneFromDwg(targetDb, dwgPath);
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: DWG locked or corrupted; gracefully apply programmatic fallback
                ApplyFallback(targetDb);
                return new KataTemplateImportResult
                {
                    Success = true,
                    UsedFallback = true,
                    Message = $"Failed to clone from '{dwgPath}' ({ex.Message}). Programmatic fallback applied."
                };
            }
        }

        public void EnsureStandardLayers(Database targetDb)
        {
            if (ReferenceEquals(targetDb, null)) throw new ArgumentNullException(nameof(targetDb));
            EnsureStandardLayersCore(targetDb);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void EnsureStandardLayersCore(Database targetDb)
        {
            using var tr = targetDb.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(targetDb.LayerTableId, OpenMode.ForRead);

            foreach (var (name, colorIdx) in CanonicalLayers)
            {
                if (!lt.Has(name))
                {
                    lt.UpgradeOpen();
                    using var ltr = new LayerTableRecord
                    {
                        Name = name,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, colorIdx)
                    };
                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                }
            }

            tr.Commit();
        }

        public void EnsureStandardTextStyles(Database targetDb)
        {
            if (ReferenceEquals(targetDb, null)) throw new ArgumentNullException(nameof(targetDb));
            EnsureStandardTextStylesCore(targetDb);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void EnsureStandardTextStylesCore(Database targetDb)
        {
            using var tr = targetDb.TransactionManager.StartTransaction();
            var tt = (TextStyleTable)tr.GetObject(targetDb.TextStyleTableId, OpenMode.ForRead);

            if (!tt.Has(StandardTextStyleName))
            {
                tt.UpgradeOpen();
                using var ttr = new TextStyleTableRecord
                {
                    Name = StandardTextStyleName,
                    FileName = StandardFontFileName,
                    XScale = StandardTextWidthFactor
                };
                tt.Add(ttr);
                tr.AddNewlyCreatedDBObject(ttr, true);
            }

            tr.Commit();
        }

        private void ApplyFallback(Database targetDb)
        {
            EnsureStandardLayersCore(targetDb);
            EnsureStandardTextStylesCore(targetDb);
        }

        private KataTemplateImportResult CloneFromDwg(Database targetDb, string dwgPath)
        {
            using var sourceDb = new Database(false, true);
            sourceDb.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndReadShare, false, null);

            var result = new KataTemplateImportResult { Success = true };
            result.ImportedBlocksCount = CloneBlocks(sourceDb, targetDb);
            result.ImportedLayersCount = CloneLayers(sourceDb, targetDb);
            result.ImportedTextStylesCount = CloneTextStyles(sourceDb, targetDb);
            result.ImportedDimStylesCount = CloneDimStyles(sourceDb, targetDb);

            // Ensure any missing canonical layers or text styles are present
            EnsureStandardLayers(targetDb);
            EnsureStandardTextStyles(targetDb);

            result.Message = $"Successfully imported {result.ImportedBlocksCount} blocks, {result.ImportedLayersCount} layers, {result.ImportedTextStylesCount} text styles, {result.ImportedDimStylesCount} dim styles from '{dwgPath}'.";
            return result;
        }

        private static int CloneBlocks(Database sourceDb, Database targetDb)
        {
            var ids = new ObjectIdCollection();
            using (var tr = sourceDb.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId id in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (!btr.IsLayout && !btr.IsAnonymous && !btr.Name.StartsWith("*", StringComparison.Ordinal))
                    {
                        ids.Add(id);
                    }
                }
                tr.Commit();
            }

            if (ids.Count == 0) return 0;
            var idMap = new IdMapping();
            sourceDb.WblockCloneObjects(ids, targetDb.BlockTableId, idMap, DuplicateRecordCloning.Ignore, false);
            return ids.Count;
        }

        private static int CloneLayers(Database sourceDb, Database targetDb)
        {
            var ids = new ObjectIdCollection();
            using (var tr = sourceDb.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(sourceDb.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (!string.Equals(ltr.Name, "0", StringComparison.OrdinalIgnoreCase))
                    {
                        ids.Add(id);
                    }
                }
                tr.Commit();
            }

            if (ids.Count == 0) return 0;
            var idMap = new IdMapping();
            sourceDb.WblockCloneObjects(ids, targetDb.LayerTableId, idMap, DuplicateRecordCloning.Ignore, false);
            return ids.Count;
        }

        private static int CloneTextStyles(Database sourceDb, Database targetDb)
        {
            var ids = new ObjectIdCollection();
            using (var tr = sourceDb.TransactionManager.StartTransaction())
            {
                var tt = (TextStyleTable)tr.GetObject(sourceDb.TextStyleTableId, OpenMode.ForRead);
                foreach (ObjectId id in tt)
                {
                    var ttr = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (!string.IsNullOrWhiteSpace(ttr.Name))
                    {
                        ids.Add(id);
                    }
                }
                tr.Commit();
            }

            if (ids.Count == 0) return 0;
            var idMap = new IdMapping();
            sourceDb.WblockCloneObjects(ids, targetDb.TextStyleTableId, idMap, DuplicateRecordCloning.Ignore, false);
            return ids.Count;
        }

        private static int CloneDimStyles(Database sourceDb, Database targetDb)
        {
            var ids = new ObjectIdCollection();
            using (var tr = sourceDb.TransactionManager.StartTransaction())
            {
                var dt = (DimStyleTable)tr.GetObject(sourceDb.DimStyleTableId, OpenMode.ForRead);
                foreach (ObjectId id in dt)
                {
                    var dtr = (DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (!string.IsNullOrWhiteSpace(dtr.Name))
                    {
                        ids.Add(id);
                    }
                }
                tr.Commit();
            }

            if (ids.Count == 0) return 0;
            var idMap = new IdMapping();
            sourceDb.WblockCloneObjects(ids, targetDb.DimStyleTableId, idMap, DuplicateRecordCloning.Ignore, false);
            return ids.Count;
        }
    }
}
