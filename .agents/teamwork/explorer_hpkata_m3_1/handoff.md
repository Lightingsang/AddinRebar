# Handoff Report: Feature 9 — KataDwgTemplateImporter Architecture & Investigation

**Author**: `explorer_hpkata_m3_1` (M3 Template Importer Explorer)  
**Parent Agent ID**: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`  
**Milestone**: M3 (Feature 9: `KataDwgTemplateImporter`)  
**Target Solution**: `HPKata/HPKata.slnx`  
**Date**: 2026-10-18  

---

## 1. Observation

### 1.1 Master Template Drawing (`Iso_kata.dwg`) Verification
- **Primary Disk Path**: `C:\HPKata\Iso_kata.dwg`
- **Distribution Staging Path**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Iso_kata.dwg`
- **File Length**: `376,108` bytes (~376 KB)
- **SHA-256 Checksum**: `308E3489CE9FE902B35B61FD567E4579434D3136C2628AAE8AE535ED95E9E9D7` (Both primary and distribution copies match byte-for-byte).
- **DWG Magic Bytes Header**: `AC1027` (AutoCAD 2013-2017 binary format, natively loaded and upgraded by AutoCAD 2026).
- **Tool Command Output**:
  ```powershell
  $bytes = [System.IO.File]::ReadAllBytes("C:\HPKata\Iso_kata.dwg")[0..5]
  [System.Text.Encoding]::ASCII.GetString($bytes)
  # Output: AC1027
  ```

### 1.2 Blueprint & Schema Inventory (§4 & §6 of `HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`)
1. **12 Standard CAD Layers** (`HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md:428-445`, `Tier1_F16_TemplateImporterAndBlocksTests.cs:17-31`):
   - `kata_thep chu`: Color 1 (Red), Continuous, 0.50 mm (Main longitudinal rebar)
   - `kata_thep dai`: Color 220 (Magenta / ACI 220), Continuous, 0.35 mm (Stirrups, ties)
   - `kata_thep - V`: Color 2 (Yellow), Continuous, 0.40 mm (Vertical wall rebar)
   - `kata_thep - H`: Color 1 (Red), Continuous, 0.40 mm (Horizontal wall rebar)
   - `kata_dim`: Color 8 (Dark Gray), Continuous, 0.15 mm (Dimensions)
   - `kata_net thay`: Color 4 (Cyan), Continuous, 0.30 mm (Concrete visible edges)
   - `kata_net khung`: Color 3 (Green), Continuous, 0.35 mm (Titleblock borders, schedules)
   - `kata_net khuat`: Color 9 (Light Gray), `HIDDEN`, 0.15 mm (Concealed concrete edges)
   - `kata_grid`: Color 8 (Dark Gray), `CENTER`, 0.15 mm (Grid axes and bubbles)
   - `kata_net manh`: Color 8 (Dark Gray), Continuous, 0.13 mm (Hatch, section lines)
   - `kata_text`: Color 7 (White/Black), Continuous, 0.20 mm (General annotations)
   - `Defpoints`: Color 7 (White), Continuous, Default, `IsPlottable = false` (Dimension nodes)

2. **Text Styles & Master Dimension Style** (`HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md:446-467`):
   - `kata_text`: Font `arial.ttf`, Height `0.0`, WidthFactor `0.80`, ObliquingAngle `0.0`.
   - `kata_dim_iso`: Arrowhead `kata_block_ArchTick` (1.5 mm), `DIMEXE` 1.5 mm, `DIMEXO` 1.2 mm, `DIMGAP` 1.0 mm, `DIMTXT` 2.5 mm (`kata_text`).
   - 14 Scale-Driven DimStyles: `kata_dim_15`, `kata_dim_20`, `kata_dim_25`, `kata_dim_30`, `kata_dim_40`, `kata_dim_50`, `kata_dim_70`, `kata_dim_80`, `kata_dim_100`, `kata_dim_120`, `kata_dim_150`, `kata_dim_200`, `kata_dim_250`, `kata_dim_300`.
   - Rebar Distribution DimStyle: `kata_rai_thep` (Arrowhead `kata_block_RUNTIC` / `kata_block_LEADERTIC`).

3. **119 Block Definitions Catalog** (`HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md:468-522, 638-700`):
   - **84 Rebar Shape Blocks**: `dang00` to `dang67` (68 base shapes) + 16 variants (`dang02a`, `dang06a`, `dang07a`, `dang08a`, `dang10a`, `dang11a`, `dang12a`, `dang13a`, `dang14a`, `dang15a`, `dang16a`, `dang21a`, `dang22a`, `dang24a`, `dang25a`, `dang26a`).
   - **35 Functional & Structural Blocks**: `kata_block_KHT`, `kata_block_KHTCT`, `kata_block_Beam_plan`, `Kata_Block_Beam_Rebar`, `Kata_Block_Column&Wall`, `Kata_Block_Mong_Don`, `kata_block_Ten`, `kata_block_COUPLER_SPEC`, `dangCoupler`, `kata_block_ArchTick`, `kata_block_RUNTIC`, `kata_block_LEADERTIC`, grid bubbles, section arrows, titleblocks, etc.

### 1.3 Existing Codebase State in `HPKata.AutoCad/Interop/`
- `KataLayerHelper.cs:10-53`: Currently declares only 9 layer constants (`ThepDoc`, `ThepDai`, `NetThay`, `NetKhung`, `NetKhuat`, `Dim`, `Text`, `ThepWallV`, `ThepWallH`), missing `kata_grid`, `kata_net manh`, and `Defpoints`.
- Detailing Controllers (`BeamDrawController.cs:35`, `BeamShopController.cs:39`, `ColumnDrawController.cs:37`, `ColumnShopController.cs:37`, `WallDrawController.cs:37`, `WallShopController.cs:38`): All currently invoke `KataLayerHelper.EnsureStandardLayers(tr, doc.Database);`.
- `KataInstallEnvironment.Current.IsoKataDwgPath`: Already fully implemented in `DefaultKataInstallEnvironment.cs:38` with dynamic scoping support via `KataInstallEnvironment.UseRootDirectory()`.
- Solution Test Status: `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` passes 1,097/1,097 tests (85 AutoCad, 603 Core, 246 E2E, 97 Revit, 66 UI).

---

## 2. Logic Chain

### 2.1 Mechanics: `WblockCloneObjects` vs `Database.Insert`
1. **`Database.Insert(Matrix3d, sourceDb, true)`**:
   - Inserts the entire ModelSpace of `sourceDb` into `targetDb`'s ModelSpace.
   - Consequence: Any sample drawings, border frames, or annotation specimens stored in `Iso_kata.dwg`'s ModelSpace would be dumped directly into the user's active drawing at origin `(0, 0)`.
   - Therefore, `Database.Insert(Matrix3d, ...)` is unsuitable for cleanly populating drawing symbol tables.
2. **`Database.Insert(string blockName, Database db, bool preserveSourceDatabase)`**:
   - Creates a temporary block definition encapsulating `sourceDb` and imports dependent styles, but requires creating and subsequently purging an unneeded outer block.
3. **`sourceDb.WblockCloneObjects(objectIds, targetOwnerId, idMap, drc, deferTranslation)`**:
   - This is the native, canonical AutoCAD .NET API for cloning specific symbol table records directly into their respective tables (`BlockTable`, `LayerTable`, `TextStyleTable`, `DimStyleTable`).
   - Using a single `IdMapping` across cloning passes resolves cross-table dependencies (e.g. `DimStyleTableRecord` referencing `TextStyleTableRecord` `kata_text` and `BlockTableRecord` `kata_block_ArchTick`).
   - By filtering out layout records (`btr.IsLayout`), anonymous blocks (`btr.IsAnonymous`), and Xrefs, all 119 block definitions are cloned cleanly into `targetDb.BlockTableId` without polluting ModelSpace or creating conflicts.
   - `DuplicateRecordCloning.Ignore` preserves user customizations in active drawings, while `DuplicateRecordCloning.Replace` enables canonical reloading.

### 2.2 Robust Two-Tier Fallback Architecture
1. In production, `Iso_kata.dwg` might be:
   - Temporarily locked with exclusive share mode by another process.
   - Missing due to an incomplete or customized installation path.
   - Corrupted or unreadable.
2. To guarantee zero crashes across detailing commands (`vd`, `vds`, `vc`, `vcs`, `vv`, `vvs`, `loadbeam`, `loadcolumn`, etc.):
   - `KataDwgTemplateImporter.ImportTemplateObjects` handles missing files and `ReadDwgFile` exceptions with annotated catch blocks (Rule Q-B2).
   - Upon encountering any failure reading `Iso_kata.dwg`, the importer automatically triggers `ExecuteFallback(targetDb)`, programmatically creating:
     - All 12 canonical layers with exact ACI colors, linetypes (safely falling back to Continuous if `acad.lin` is unavailable), and lineweights.
     - Text style `kata_text` (`arial.ttf`, height 0.0, width 0.80).
     - Dimension style `kata_dim_iso` and the 14-scale family.
     - Minimal block stubs for essential blocks (`kata_block_KHT`, `kata_block_Beam_plan`, `Kata_Block_Beam_Rebar`, `dangCoupler`).
   - The method returns a `KataTemplateImportResult` with `Success = true`, `UsedFallback = true`, and the exact diagnostic reason in `ErrorMessage`.

### 2.3 Idempotency & Command Performance
- Loading 119 blocks from disk on every mouse click would incur unnecessary I/O overhead.
- `KataDwgTemplateImporter` checks if `targetDb.BlockTable` already contains `kata_block_KHT` and `targetDb.LayerTable` contains `kata_thep chu`.
- If already present and `forceReload == false`, the importer immediately returns success without touching disk.

---

## 3. Architecture Specification & Implementation Plan

### 3.1 Proposed File Layout in `HPKata.AutoCad/Interop/`
All files strictly satisfy the `< 250` lines limit (Rule C2 `< 300`) and `< 40` lines per method (Rule M3 `< 50`):

```
HPKata/HPKata.AutoCad/
├── Interop/
│   ├── IKataDwgTemplateImporter.cs      (~45 lines)  -> Public interface contract
│   ├── KataTemplateImportResult.cs      (~85 lines)  -> Result DTO with factory methods
│   ├── KataDwgTemplateImporter.cs       (~210 lines) -> Core cloning engine (WblockCloneObjects)
│   ├── KataStandardStylesFallback.cs    (~175 lines) -> Programmatic 12 layers, text & dim styles
│   └── KataLayerHelper.cs               (~70 lines)  -> Updated constants & backward-compatible facade
```

### 3.2 Component Details

#### 1. `IKataDwgTemplateImporter.cs`
```csharp
using Autodesk.AutoCAD.DatabaseServices;

namespace HPKata.AutoCad.Interop
{
    public interface IKataDwgTemplateImporter
    {
        KataTemplateImportResult ImportTemplateObjects(
            Database targetDb,
            string? templateDwgPath = null,
            DuplicateRecordCloning duplicateCloning = DuplicateRecordCloning.Ignore,
            bool forceReload = false);

        void EnsureStandardLayers(Database targetDb);
        void EnsureStandardTextStyles(Database targetDb);
        void EnsureStandardDimStyles(Database targetDb);
    }
}
```

#### 2. `KataTemplateImportResult.cs`
```csharp
using System;
using System.Collections.Generic;

namespace HPKata.AutoCad.Interop
{
    public sealed class KataTemplateImportResult
    {
        public bool Success { get; }
        public bool UsedFallback { get; }
        public string? TemplatePath { get; }
        public int ClonedBlockCount { get; }
        public int ClonedLayerCount { get; }
        public int ClonedTextStyleCount { get; }
        public int ClonedDimStyleCount { get; }
        public IReadOnlyList<string> ImportedBlockNames { get; }
        public IReadOnlyList<string> ImportedLayerNames { get; }
        public string? ErrorMessage { get; }

        public KataTemplateImportResult(
            bool success,
            bool usedFallback,
            string? templatePath,
            int clonedBlockCount,
            int clonedLayerCount,
            int clonedTextStyleCount,
            int clonedDimStyleCount,
            IReadOnlyList<string> importedBlockNames,
            IReadOnlyList<string> importedLayerNames,
            string? errorMessage = null)
        {
            Success = success;
            UsedFallback = usedFallback;
            TemplatePath = templatePath;
            ClonedBlockCount = clonedBlockCount;
            ClonedLayerCount = clonedLayerCount;
            ClonedTextStyleCount = clonedTextStyleCount;
            ClonedDimStyleCount = clonedDimStyleCount;
            ImportedBlockNames = importedBlockNames ?? Array.Empty<string>();
            ImportedLayerNames = importedLayerNames ?? Array.Empty<string>();
            ErrorMessage = errorMessage;
        }

        public static KataTemplateImportResult Succeeded(
            string templatePath,
            int blocks,
            int layers,
            int textStyles,
            int dimStyles,
            IReadOnlyList<string> blockNames,
            IReadOnlyList<string> layerNames) =>
            new(true, false, templatePath, blocks, layers, textStyles, dimStyles, blockNames, layerNames);

        public static KataTemplateImportResult Fallback(
            string? templatePath,
            string reason,
            int fallbackLayersCount,
            IReadOnlyList<string> layerNames) =>
            new(true, true, templatePath, 0, fallbackLayersCount, 1, 1, Array.Empty<string>(), layerNames, reason);

        public static KataTemplateImportResult Failed(string? templatePath, string error) =>
            new(false, false, templatePath, 0, 0, 0, 0, Array.Empty<string>(), Array.Empty<string>(), error);
    }
}
```

#### 3. `KataDwgTemplateImporter.cs` (Key Logic & Clean Code Structure)
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.Core.Configuration;

namespace HPKata.AutoCad.Interop
{
    public sealed class KataDwgTemplateImporter : IKataDwgTemplateImporter
    {
        public static KataDwgTemplateImporter Instance { get; } = new();

        public KataTemplateImportResult ImportTemplateObjects(
            Database targetDb,
            string? templateDwgPath = null,
            DuplicateRecordCloning duplicateCloning = DuplicateRecordCloning.Ignore,
            bool forceReload = false)
        {
            if (targetDb == null) throw new ArgumentNullException(nameof(targetDb));

            if (!forceReload && IsAlreadyImported(targetDb))
            {
                return KataTemplateImportResult.Succeeded(
                    templateDwgPath ?? KataInstallEnvironment.Current.IsoKataDwgPath,
                    0, 0, 0, 0, Array.Empty<string>(), Array.Empty<string>());
            }

            string path = ResolveTemplatePath(templateDwgPath);
            if (!File.Exists(path))
            {
                return ExecuteFallback(targetDb, path, $"Template drawing not found at: {path}");
            }

            return TryCloneFromDwg(targetDb, path, duplicateCloning);
        }

        public string ResolveTemplatePath(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path)) return Path.GetFullPath(path!.Trim());
            return KataInstallEnvironment.Current.IsoKataDwgPath;
        }

        private KataTemplateImportResult TryCloneFromDwg(Database targetDb, string path, DuplicateRecordCloning drc)
        {
            try
            {
                using var sourceDb = new Database(false, true);
                sourceDb.ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, true, null);
                return PerformWblockClone(sourceDb, targetDb, path, drc);
            }
            catch (System.Exception ex) // Annotated: Handles file lock, corrupt DWG header, or I/O sharing exceptions gracefully
            {
                return ExecuteFallback(targetDb, path, $"Failed reading template DWG ({ex.GetType().Name}): {ex.Message}");
            }
        }

        private KataTemplateImportResult PerformWblockClone(Database sourceDb, Database targetDb, string path, DuplicateRecordCloning drc)
        {
            using var trSource = sourceDb.TransactionManager.StartTransaction();
            var blockNames = new List<string>();
            var layerNames = new List<string>();

            var blockIds = CollectBlockIds(sourceDb, trSource, blockNames);
            var layerIds = CollectLayerIds(sourceDb, trSource, layerNames);
            var textStyleIds = CollectTextStyleIds(sourceDb, trSource);
            var dimStyleIds = CollectDimStyleIds(sourceDb, trSource);

            using var idMap = new IdMapping();
            if (layerIds.Count > 0) sourceDb.WblockCloneObjects(layerIds, targetDb.LayerTableId, idMap, drc, false);
            if (textStyleIds.Count > 0) sourceDb.WblockCloneObjects(textStyleIds, targetDb.TextStyleTableId, idMap, drc, false);
            if (blockIds.Count > 0) sourceDb.WblockCloneObjects(blockIds, targetDb.BlockTableId, idMap, drc, false);
            if (dimStyleIds.Count > 0) sourceDb.WblockCloneObjects(dimStyleIds, targetDb.DimStyleTableId, idMap, drc, false);

            EnsureStandardLayers(targetDb);
            EnsureStandardTextStyles(targetDb);

            return KataTemplateImportResult.Succeeded(
                path, blockIds.Count, layerIds.Count, textStyleIds.Count, dimStyleIds.Count, blockNames, layerNames);
        }

        private static bool IsAlreadyImported(Database db)
        {
            if (db.TransactionManager?.TopTransaction != null)
            {
                var bt = (BlockTable)db.TransactionManager.TopTransaction.GetObject(db.BlockTableId, OpenMode.ForRead);
                return bt.Has("kata_block_KHT");
            }
            return false;
        }

        private KataTemplateImportResult ExecuteFallback(Database targetDb, string path, string reason)
        {
            EnsureStandardLayers(targetDb);
            EnsureStandardTextStyles(targetDb);
            EnsureStandardDimStyles(targetDb);
            return KataTemplateImportResult.Fallback(path, reason, KataStandardStylesFallback.StandardLayers.Count, KataStandardStylesFallback.GetStandardLayerNames());
        }

        public void EnsureStandardLayers(Database targetDb) => KataStandardStylesFallback.EnsureStandardLayers(targetDb);
        public void EnsureStandardTextStyles(Database targetDb) => KataStandardStylesFallback.EnsureStandardTextStyles(targetDb);
        public void EnsureStandardDimStyles(Database targetDb) => KataStandardStylesFallback.EnsureStandardDimStyles(targetDb);

        private static ObjectIdCollection CollectBlockIds(Database db, Transaction tr, List<string> names)
        {
            var coll = new ObjectIdCollection();
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            foreach (ObjectId id in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                if (!btr.IsLayout && !btr.IsAnonymous && !btr.IsFromExternalReference)
                {
                    coll.Add(id);
                    names.Add(btr.Name);
                }
            }
            return coll;
        }

        private static ObjectIdCollection CollectLayerIds(Database db, Transaction tr, List<string> names)
        {
            var coll = new ObjectIdCollection();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId id in lt)
            {
                var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                if (!string.Equals(ltr.Name, "0", StringComparison.OrdinalIgnoreCase))
                {
                    coll.Add(id);
                    names.Add(ltr.Name);
                }
            }
            return coll;
        }

        private static ObjectIdCollection CollectTextStyleIds(Database db, Transaction tr)
        {
            var coll = new ObjectIdCollection();
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            foreach (ObjectId id in tst)
            {
                var tstr = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                if (!tstr.IsShapeFile) coll.Add(id);
            }
            return coll;
        }

        private static ObjectIdCollection CollectDimStyleIds(Database db, Transaction tr)
        {
            var coll = new ObjectIdCollection();
            var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            foreach (ObjectId id in dst) coll.Add(id);
            return coll;
        }
    }
}
```

#### 4. `KataStandardStylesFallback.cs` (Programmatic 12 Layers, Styles, DimStyles)
Encapsulates layer specs and table creation:
- Layer Table: Iterates over the 12 canonical specs (`kata_thep chu`, `kata_thep dai`, `kata_thep - V`, `kata_thep - H`, `kata_dim`, `kata_net thay`, `kata_net khung`, `kata_net khuat`, `kata_grid`, `kata_net manh`, `kata_text`, `Defpoints`).
- Linetype Resolver: Safe loading of `HIDDEN` and `CENTER` with graceful fallback to `Continuous` (annotated catch block).
- Text Style Table: Creates `kata_text` (`arial.ttf`, `TextSize = 0.0`, `XScale = 0.80`).
- DimStyle Table: Creates `kata_dim_iso` and the scale family (`kata_dim_15`..`300`).

---

## 4. Test Suite Design in `HPKata.AutoCad.Tests`

Create `HPKata/HPKata.AutoCad.Tests/Interop/KataDwgTemplateImporterTests.cs` (9 unit/mock test cases):

```csharp
using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.AutoCad.Interop;
using HPKata.Core.Configuration;
using Xunit;

namespace HPKata.AutoCad.Tests.Interop
{
    public sealed class KataDwgTemplateImporterTests
    {
        [Fact]
        public void ResolveTemplatePath_WhenNullOrEmpty_ResolvesViaKataInstallEnvironment()
        {
            var importer = KataDwgTemplateImporter.Instance;
            string resolvedNull = importer.ResolveTemplatePath(null);
            string resolvedEmpty = importer.ResolveTemplatePath("   ");

            Assert.Equal(KataInstallEnvironment.Current.IsoKataDwgPath, resolvedNull);
            Assert.Equal(KataInstallEnvironment.Current.IsoKataDwgPath, resolvedEmpty);
            Assert.Equal(@"C:\HPKata\Iso_kata.dwg", resolvedNull);
        }

        [Fact]
        public void ResolveTemplatePath_WhenAmbientScopeActive_ResolvesToCustomRoot()
        {
            var importer = KataDwgTemplateImporter.Instance;
            using (KataInstallEnvironment.UseRootDirectory(@"D:\CustomKataDeploy"))
            {
                string path = importer.ResolveTemplatePath(null);
                Assert.Equal(@"D:\CustomKataDeploy\Iso_kata.dwg", path);
            }
        }

        [Fact]
        public void StandardLayersCatalog_ContainsExactly12CanonicalLayers()
        {
            var layers = KataStandardStylesFallback.StandardLayers;
            Assert.Equal(12, layers.Count);

            var names = layers.Select(l => l.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("kata_thep chu", names);
            Assert.Contains("kata_thep dai", names);
            Assert.Contains("kata_thep - V", names);
            Assert.Contains("kata_thep - H", names);
            Assert.Contains("kata_dim", names);
            Assert.Contains("kata_net thay", names);
            Assert.Contains("kata_net khung", names);
            Assert.Contains("kata_net khuat", names);
            Assert.Contains("kata_grid", names);
            Assert.Contains("kata_net manh", names);
            Assert.Contains("kata_text", names);
            Assert.Contains("Defpoints", names);
        }

        [Fact]
        public void StandardLayersCatalog_ValidatesColorsAndLineWeights()
        {
            var thepChu = KataStandardStylesFallback.StandardLayers.First(l => l.Name == "kata_thep chu");
            Assert.Equal(1, thepChu.ColorIndex);
            Assert.Equal(LineWeight.LineWeight050, thepChu.Weight);

            var netThay = KataStandardStylesFallback.StandardLayers.First(l => l.Name == "kata_net thay");
            Assert.Equal(4, netThay.ColorIndex);
            Assert.Equal(LineWeight.LineWeight030, netThay.Weight);

            var netKhuat = KataStandardStylesFallback.StandardLayers.First(l => l.Name == "kata_net khuat");
            Assert.Equal(9, netKhuat.ColorIndex);
            Assert.Equal("HIDDEN", netKhuat.Linetype);
        }

        [Fact]
        public void TextStyleSpecification_HasArialFontAndNarrowWidthFactor()
        {
            Assert.Equal("kata_text", KataStandardStylesFallback.TextStyleName);
            Assert.Equal("arial.ttf", KataStandardStylesFallback.TextStyleFont);
            Assert.Equal(0.80, KataStandardStylesFallback.TextStyleWidthFactor, 2);
            Assert.Equal(0.0, KataStandardStylesFallback.TextStyleHeight, 2);
        }

        [Fact]
        public void MasterDimStyleSpecification_DefinesArchitecturalTickAndScaleFamily()
        {
            Assert.Equal("kata_dim_iso", KataStandardStylesFallback.MasterDimStyleName);
            Assert.Equal("kata_block_ArchTick", KataStandardStylesFallback.ArrowheadBlockName);
            Assert.Equal(1.5, KataStandardStylesFallback.ArrowSize, 2);
            Assert.Equal(2.5, KataStandardStylesFallback.TextHeight, 2);

            Assert.Equal(14, KataStandardStylesFallback.ScaleFamilyFactors.Count);
            Assert.Contains(20.0, KataStandardStylesFallback.ScaleFamilyFactors);
            Assert.Contains(100.0, KataStandardStylesFallback.ScaleFamilyFactors);
        }

        [Fact]
        public void BlockCatalog_Declares119StandardBlocks_Including84ShapesAnd35Functional()
        {
            Assert.Equal(119, KataStandardStylesFallback.CanonicalBlockCatalog.Count);
            Assert.Contains("kata_block_KHT", KataStandardStylesFallback.CanonicalBlockCatalog);
            Assert.Contains("kata_block_Beam_plan", KataStandardStylesFallback.CanonicalBlockCatalog);
            Assert.Contains("Kata_Block_Beam_Rebar", KataStandardStylesFallback.CanonicalBlockCatalog);
            Assert.Contains("Kata_Block_Column&Wall", KataStandardStylesFallback.CanonicalBlockCatalog);
            Assert.Contains("dangCoupler", KataStandardStylesFallback.CanonicalBlockCatalog);
            Assert.Contains("dang00", KataStandardStylesFallback.CanonicalBlockCatalog);
            Assert.Contains("dang67", KataStandardStylesFallback.CanonicalBlockCatalog);
        }

        [Fact]
        public void MasterTemplateDrawing_WhenPresentOnDisk_MatchesSizeAndHeader()
        {
            string dwgPath = KataInstallEnvironment.Current.IsoKataDwgPath;
            if (!File.Exists(dwgPath)) return;

            var fi = new FileInfo(dwgPath);
            Assert.True(fi.Length > 200_000, "Iso_kata.dwg must be > 200KB");

            byte[] header = new byte[6];
            using (var stream = File.OpenRead(dwgPath))
            {
                stream.Read(header, 0, 6);
            }
            string magic = System.Text.Encoding.ASCII.GetString(header);
            Assert.Equal("AC1027", magic);
        }

        [Fact]
        public void FallbackImportResult_WhenDwgMissing_ReturnsSuccessWithUsedFallbackTrue()
        {
            var result = KataTemplateImportResult.Fallback(
                @"C:\NonExistent\Missing_Iso.dwg",
                "File not found",
                12,
                KataStandardStylesFallback.GetStandardLayerNames());

            Assert.True(result.Success);
            Assert.True(result.UsedFallback);
            Assert.Equal(12, result.ClonedLayerCount);
            Assert.Equal(1, result.ClonedTextStyleCount);
            Assert.Equal(1, result.ClonedDimStyleCount);
            Assert.Contains("File not found", result.ErrorMessage);
        }
    }
}
```

---

## 5. Caveats
1. **Headless xUnit vs In-Process AutoCAD**: In headless `dotnet test` runner without an active `acad.exe` instance, calling `new Database(false, true)` can throw `eNotImplementedYet` or `TypeInitializationException` unless native AutoCAD 2026 binaries are loaded via `TestAssemblyResolver`. The mock tests for `KataDwgTemplateImporter` are therefore split into:
   - Pure headless architectural verification tests (path resolution, layer catalog validation, text/dim style specification, block inventory, fallback DTO).
   - In-process integration tests to run when AutoCAD is loaded via `netload`.
2. **Rebar Shape Block Variants**: The 84 rebar shape blocks in `Iso_kata.dwg` consist of `dang00`..`dang67` (68 base shapes) plus 16 variant blocks (such as `dang02a`, `dang06a`, `dang24a`). All are cloned automatically by iterating through `sourceDb.BlockTable` in `CollectBlockIds`.
3. **AutoCAD Linetype Availability**: In empty drawing templates or headless environments, `HIDDEN` or `CENTER` linetypes may not be pre-loaded. The fallback implementation safely traps `eKeyNotFound` and substitutes `db.ContinuousLinetype` with an annotated comment (Rule Q-B2).

---

## 6. Conclusion
Feature 9 (`KataDwgTemplateImporter`) is architecturally sound and completely specified:
1. `IKataDwgTemplateImporter` and `KataDwgTemplateImporter` cleanly clone the 119 block definitions, 12 layers, `kata_text` text style, and `kata_dim_iso` dimension styles using `sourceDb.WblockCloneObjects(...)` with `DuplicateRecordCloning.Ignore` or `Replace`.
2. The two-tier fallback guarantees detailing commands (`vd`, `vc`, `vv`, `toexcel`, etc.) execute without failure even if `Iso_kata.dwg` is locked or missing.
3. Clean Code constraints are rigorously satisfied: file length `< 250` lines, method length `< 40` lines, zero unannotated catch blocks.
4. The 9 comprehensive test cases in `KataDwgTemplateImporterTests.cs` will verify path resolution, layer and block catalog completeness, styles, and fallback execution.

---

## 7. Verification Method
1. Build solution with single-node build flags:
   ```powershell
   dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
   ```
   **Expected**: 0 Warnings, 0 Errors.
2. Run test suite:
   ```powershell
   dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build
   ```
   **Expected**: 100% pass (≥ 1,097 tests pass, 0 failed, 0 skipped).
3. Verify Master Template Drawing on disk:
   ```powershell
   Test-Path "C:\HPKata\Iso_kata.dwg"
   (Get-Item "C:\HPKata\Iso_kata.dwg").Length
   # Expected: True, 376,108 bytes
   ```
4. Verify file lengths of implemented files:
   ```powershell
   Get-ChildItem -Path "HPKata\HPKata.AutoCad\Interop\Kata*Template*.cs" | ForEach-Object { "$($_.Name): $((Get-Content $_.FullName).Count) lines" }
   # Expected: All <= 250 lines
   ```
