# Project: HPKata Standalone Structural Detailing Suite

## Architecture
HPKata is a standalone, self-contained structural rebar detailing & fabrication suite installed centrally at `C:\HPKata\`, operating with 100% operational parity to KataPro while having zero dependency on `C:\kata_pro\`.

### System Structure
1. **HPKata.Core** (`netstandard2.0;net8.0`):
   - Central `KataInstallEnvironment` providing canonical, thread-safe (overridable via `AsyncLocal<T>`) paths for all library assets (DWG, XLSM, XLS, LSP, CUIX, shapes `dangthep\`, column splices `Cot\`, icons `Icon\`, diagrams `IMG\`, `Temp\`).
   - Modular Bi-directional Excel engine (`HPKata.Core/Excel/`, each file <= 250 lines): handles sheets `Dam`, `Cot`, `Vach`, `Thong_ke` in `Kata.xlsm` and `Iso_QHPlus.xls` with active COM desktop attachment and headless ClosedXML fallback.
   - 86-parameter configuration parser & serializer for `Goi lenh.lsp`.
2. **HPKata.UI** (`net8.0-windows`):
   - 13 WPF windows based on `System.Windows.Window` + `MaterialDesignThemes` (v5.2.1) with dark/light dynamic styling.
   - ViewModels (`BeamDetailingViewModel`, `ColumnDetailingViewModel`, `WallDetailingViewModel`) wired with Open, Read, Write, and Auto-Sync commands for `Kata.xlsm`.
   - `KataVisualAssetService` displaying shape diagrams (`dangthep\*.wmf`, `IMG\Kata-*.png`) and column splice diagrams (`Cot\*.wmf`).
3. **HPKata.AutoCad** (`net8.0-windows`):
   - Detailing commands (`vd`, `vds`, `vc`, `vcs`, `vv`, `vvs`, `loadbeam`, `loadcolumn`, `loadwall`, `toexcel`, `toexcelqh`).
   - `KataDwgTemplateImporter` in `HPKata.AutoCad/Interop/` using `WblockCloneObjects` to clone layers, text/dim styles, and 119 blocks from `C:\HPKata\Iso_kata.dwg` into active AutoCAD `Database` with programmatic fallback.
   - Ribbon icons loaded from `C:\HPKata\Icon\Ribbon\*.png` with vector fallback.
4. **HPKata.Revit** (`net8.0-windows`):
   - Framing, column, and wall 3D rebar generation commands reading/writing `C:\HPKata\Kata.xlsm`.
   - Ribbon icons loaded from `C:\HPKata\Icon\Ribbon\*.png` with vector fallback.
5. **HPKata.Installer** (`net8.0-windows`, `HPKata_Setup.exe`):
   - Dual-mode installer: MaterialDesign WPF UI and headless CLI flags (`--install`, `--uninstall`, `--target-dir <path>`).
   - Centralized deployment of 4 managed DLLs, 13 runtime NuGet dependencies, and all library assets to `C:\HPKata\`.
   - Manifest authoring for `%AppData%\Autodesk\Revit\Addins\2026\HPKata.Revit.addin` and `%AppData%\Autodesk\ApplicationPlugins\HPKata.bundle\PackageContents.xml`.
   - Recursive registration in AutoCAD's `TRUSTEDPATHS` registry key.

---

## Feature Inventory

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | `KataInstallEnvironment` Service | Canonical path provider in `HPKata.Core` with `AsyncLocal` override for isolated unit testing | M1 | Survey |
| 2 | Asset Migration & Staging | Copy 3.43 MB library assets from `C:\kata_pro\` to `HPKata/Distribution/Assets/` and `C:\HPKata\` | M1 | Survey |
| 3 | LISP & Menu Path Rewriting | Rewrite paths in `Goi lenh.lsp`, `GoiLenh_HPKata.lsp`, and `menukata.cui` to `C:\HPKata` | M1 | Survey |
| 4 | Codebase `kata_pro` Purge | Eliminate all 9 occurrences of `kata_pro` across `.cs`, `.lsp`, and `.ps1` files in `HPKata/` | M1 | Survey |
| 5 | Bi-Directional Excel Engine | Modular services in `HPKata.Core/Excel/` (COM + ClosedXML) for `Kata.xlsm` (`Dam`, `Cot`, `Vach`) and `Iso_QHPlus.xls` | M2 | Survey |
| 6 | ViewModel Excel Integration | Add Open, Read, Write, Sync commands and toolbar buttons in Beam, Column, and Wall ViewModels/Views | M2 | Survey |
| 7 | AutoCAD Detailing Commands Sync | Wire `vd`, `vds`, `vc`, `vcs`, `vv`, `vvs`, `loadbeam`, `loadcolumn`, `loadwall`, `toexcel`, `toexcelqh` to `Kata.xlsm` | M2 | Survey |
| 8 | Revit Detailing Commands Sync | Wire `FramingRebarCommands`, `ColumnRebarCommands`, `WallRebarCommands` to `Kata.xlsm` | M2 | Survey |
| 9 | `KataDwgTemplateImporter` | Clone 119 blocks, 12 layers, text/dim styles from `Iso_kata.dwg` into active AutoCAD DB with fallback | M3 | Survey |
| 10 | 86-Parameter Config Sync | Connect `SetupViewModel`, `SetupController` (`setupkata`), `SetupCommand` to `C:\HPKata\Goi lenh.lsp` | M3 | Survey |
| 11 | Ribbon Icon Loading | Load icons from `C:\HPKata\Icon\Ribbon\*.png` with dynamic vector fallback in AutoCAD and Revit | M3 | Survey |
| 12 | Visual Diagram Binding | Bind `dangthep\*.wmf`, `IMG\Kata-*.png`, `Cot\*.wmf` to `HPKata.UI` windows | M3 | Survey |
| 13 | `HPKata.Installer` Project | Add `HPKata/HPKata.Installer/HPKata.Installer.csproj` (`net8.0-windows`) to `HPKata.slnx` | M4 | Survey |
| 14 | WPF Installer GUI | Polished MaterialDesign installer dialog with Install, Repair, Uninstall, TargetDir, and log | M4 | Survey |
| 15 | Headless Installer CLI | Command line support for `--install`, `--uninstall`, `--target-dir <path>` | M4 | Survey |
| 16 | Centralized DLL & Asset Copy | Deploy all compiled binaries, NuGet dependencies, and assets to `C:\HPKata\` | M4 | Survey |
| 17 | Revit Add-in Manifest | Write `HPKata.Revit.addin` with `<Assembly>C:\HPKata\HPKata.Revit.dll</Assembly>` | M4 | Survey |
| 18 | AutoCAD Bundle Manifest | Write `PackageContents.xml` with `ModuleName="C:\HPKata\HPKata.AutoCad.dll"` and `Goi lenh.lsp` | M4 | Survey |
| 19 | AutoCAD `TRUSTEDPATHS` Reg | Register `C:\HPKata;C:\HPKata\...` idempotently in AutoCAD HKCU registry | M4 | Survey |
| 20 | E2E Regression Pass | Pass 100% of the 4-tier E2E test suite in `HPKata.E2E.Tests` | M5 | Survey |
| 21 | Adversarial Hardening (Tier 5) | White-box stress testing, corner cases, and forensic integrity audit | M5 | Survey |

---

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Standalone `C:\HPKata\` Environment & Asset Migration | Features 1–4: `KataInstallEnvironment`, asset staging to `HPKata/Distribution/Assets/` and `C:\HPKata\`, LISP/CUI path rewriting, purge 9 `kata_pro` occurrences | none | DONE (`KataInstallEnvironment`, 358 assets staged, 0 `kata_pro` occurrences, 1019 tests pass) |
| M2 | Bi-Directional Excel Integration & Detailing Parity | Features 5–8: `HPKata.Core/Excel/` (Dam, Cot, Vach, Iso_QHPlus.xls), ViewModel commands/views, AutoCAD & Revit commands Excel sync | M1 | PLANNED |
| M3 | Template Importer, 86-Param Config & Visual Assets | Features 9–12: `KataDwgTemplateImporter`, `SetupViewModel`/`SetupController`/`SetupCommand` sync with `Goi lenh.lsp`, Ribbon PNG icons, shape & splice diagrams in `HPKata.UI` | M1 | PLANNED |
| M4 | Centralized DLL Deployment & Standalone Installer | Features 13–19: `HPKata.Installer` (`HPKata_Setup.exe` GUI + CLI), MSBuild post-build targets, manifests deployment, AutoCAD `TRUSTEDPATHS` | M1, M2, M3 | PLANNED |
| M5 | Final Verification & E2E Validation | Features 20–21: Pass 100% of E2E tests (`TEST_READY.md`) across Tiers 1–4, plus Tier 5 adversarial hardening | M4, TEST_READY.md | PLANNED |

---

## Interface Contracts

### `HPKata.Core` ↔ Host Add-Ins (`HPKata.AutoCad`, `HPKata.Revit`, `HPKata.UI`, `HPKata.Installer`)
```csharp
namespace HPKata.Core.Configuration
{
    public interface IKataInstallEnvironment
    {
        string RootDirectory { get; }
        string KataWorkbookPath { get; }
        string KataEngWorkbookPath { get; }
        string IsoQhPlusPath { get; }
        string IsoKataDwgPath { get; }
        string IsoKataEdbPath { get; }
        string KataIconPath { get; }
        string GoiLenhLspPath { get; }
        string GoiLenhHpKataLspPath { get; }
        string MenuKataCuixPath { get; }
        string MenuKataCuiPath { get; }
        string MenuKataMnrPath { get; }
        string MenuKataLightMnrPath { get; }
        string DangThepDirectory { get; }
        string CotDirectory { get; }
        string IconDirectory { get; }
        string RibbonIconDirectory { get; }
        string ImgDirectory { get; }
        string TempDirectory { get; }
        string GetCanonicalPath(string relativePath);
        bool IsInstalled { get; }
        void EnsureDirectoriesExist();
    }

    public static class KataInstallEnvironment
    {
        public const string DefaultRootDirectory = @"C:\HPKata";
        public static IKataInstallEnvironment Current { get; }
        public static void SetGlobal(IKataInstallEnvironment environment);
        public static IDisposable UseRootDirectory(string rootDirectory);
    }
}
```

### `HPKata.Core.Excel` ↔ ViewModels & Detailing Commands
```csharp
namespace HPKata.Core.Excel
{
    public interface IKataExcelAdapter
    {
        bool IsDesktopExcelOpen { get; }
        bool ReadBeamSheet(string workbookPath, out KataBeamSheetData data);
        bool WriteBeamSheet(string workbookPath, in KataBeamSheetData data);
        bool ReadColumnSheet(string workbookPath, out KataColumnSheetData data);
        bool WriteColumnSheet(string workbookPath, in KataColumnSheetData data);
        bool ReadWallSheet(string workbookPath, out KataWallSheetData data);
        bool WriteWallSheet(string workbookPath, in KataWallSheetData data);
        bool ExportSchedule(string workbookPath, IEnumerable<KataRebarScheduleItem> items);
    }
}
```

### `HPKata.AutoCad.Interop` Template Importer
```csharp
namespace HPKata.AutoCad.Interop
{
    public interface IKataDwgTemplateImporter
    {
        KataTemplateImportResult ImportTemplateObjects(Autodesk.AutoCAD.DatabaseServices.Database targetDb, string? templateDwgPath = null);
        void EnsureStandardLayers(Autodesk.AutoCAD.DatabaseServices.Database targetDb);
    }
}
```

---

## Code Layout

- `HPKata/`
  - `HPKata.Core/`
    - `Configuration/`: `KataInstallEnvironment.cs`, `IKataInstallEnvironment.cs`, `DefaultKataInstallEnvironment.cs`, `KataConfig.cs`, `KataConfigParser.cs`
    - `Excel/`: `IKataExcelAdapter.cs`, `KataExcelSessionProvider.cs`, `KataBeamSheetReader.cs`, `KataBeamSheetWriter.cs`, `KataColumnSheetReader.cs`, `KataColumnSheetWriter.cs`, `KataWallSheetReader.cs`, `KataWallSheetWriter.cs`, `KataQhPlusExporter.cs`
    - `Visual/`: `IKataVisualAssetService.cs`, `KataVisualAssetService.cs`
  - `HPKata.UI/`
    - `ViewModels/`: `BeamDetailingViewModel.cs`, `ColumnDetailingViewModel.cs`, `WallDetailingViewModel.cs`, `SetupViewModel.cs`
    - `Views/`: `BeamDetailingWindow.xaml`, `ColumnDetailingWindow.xaml`, `WallDetailingWindow.xaml`, `SetupWindow.xaml`
  - `HPKata.AutoCad/`
    - `Interop/`: `IKataDwgTemplateImporter.cs`, `KataDwgTemplateImporter.cs`
    - `Ribbon/`: `AutocadRibbonIconFactory.cs`
    - `Commands/`: `ContinuousBeamCommands.cs`, `ColumnCommands.cs`, `WallCommands.cs`, `SetupController.cs`
  - `HPKata.Revit/`
    - `Commands/`: `FramingRebarCommands.cs`, `ColumnRebarCommands.cs`, `WallRebarCommands.cs`, `SetupCommand.cs`
    - `Ribbon/`: `RevitRibbonIconFactory.cs`
  - `HPKata.Installer/`
    - `Program.cs`, `App.xaml`, `Views/MainWindow.xaml`, `ViewModels/InstallerViewModel.cs`
    - `Services/`: `InstallerEngine.cs`, `AutoCadRegistryService.cs`, `RevitManifestService.cs`, `AutoCadManifestService.cs`, `AssetStagingService.cs`
  - `Distribution/Assets/`: Staging directory for all library assets (DWG, XLSM, XLS, LSP, CUIX, `dangthep\`, `Cot\`, `Icon\`, `IMG\`, `Temp\`)
