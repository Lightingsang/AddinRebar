# COMPREHENSIVE INVESTIGATION REPORT — HPKATA CODEBASE & C:\HPKATA\ STATE

- **Inspector**: `teamwork_preview_explorer_m3_1` (Teamwork Explorer Archetype)
- **Investigation Date**: 2026-10-18T06:05:00Z
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_1\`
- **Target Solution**: `HPKata/HPKata.slnx` (pinned .NET 10.0.300 SDK, target net8.0 / net8.0-windows / netstandard2.0)
- **Target Installation Directory**: `C:\HPKata\`
- **Reference Forensic Dossier**: `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`

---

## 1. Executive Summary

| Inspection Item | Objective / Requirement | Current State | Verification Evidence | Status |
|---|---|---|---|:---:|
| **Build Integrity** | Clean compilation of `HPKata.slnx` with zero warnings | Succeeded: 0 Warnings, 0 Errors | `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` | **PASS (100%)** |
| **Test Suite** | 100% green tests with single-node flags | Succeeded: **1,097 Passed, 0 Failed, 0 Skipped** | `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build` | **PASS (100%)** |
| **Purge of `kata_pro`** | Zero occurrences in production code & `C:\HPKata\` | **0 occurrences** in `HPKata.Core`, `HPKata.UI`, `HPKata.AutoCad`, `HPKata.Revit`, `Distribution/`, `C:\HPKata\` | Ripgrep case-insensitive search across codebase | **PASS (Clean)** |
| **`C:\HPKata\` Directory** | 100% standalone library assets present | 12 files + 5 subdirectories (162 WMF `dangthep`, 13 WMF `Cot`, 108 `Icon/Ribbon`, 37 PNG `IMG`, DWG, XLSM, LSP, CUIX) | Filesystem inspection of `C:\HPKata\` | **ASSETS PASS / DLLs PENDING** |
| **`KataDwgTemplateImporter`** | Clone 119 blocks, 12 layers, text/dim styles from `Iso_kata.dwg` into active AutoCAD DB | **Not yet implemented** in `HPKata.AutoCad/Interop/` (only basic `KataLayerHelper.cs` exists) | Inspection of `HPKata.AutoCad/Interop/` | **GAP (M3 Required)** |
| **86-Parameter Sync** | Sync 86 config parameters across `Goi lenh.lsp`, WPF UI, AutoCAD, Revit | Implemented in `HPKata.Core` & `HPKata.UI`; AutoCAD `SetupController` syncs; Revit `SetupCommand` **disconnected** | Inspection of `SetupController.cs` & `SetupCommand.cs` | **PARTIAL (Revit Sync Gap)** |
| **Visual Asset Binding** | Bind `dangthep`, `Cot`, `Icon/Ribbon`, `IMG` to WPF UI dialogs | `KataVisualAssetService` **not yet implemented** in `HPKata.UI`; assets exist on disk | Inspection of `HPKata.UI/` directory | **GAP (M3 Required)** |
| **`HPKata.Installer` / Setup** | Standalone installer project (`HPKata_Setup.exe`) with GUI & CLI switches | Project **not yet created** in `HPKata.slnx`; E2E tests have parsed CLI model | Inspection of `HPKata.slnx` & project tree | **GAP (M4 Required)** |

---

## 2. Codebase Architecture & Build/Test Audit

### 2.1 Solution Structure (`HPKata/HPKata.slnx`)
The solution contains 9 active projects (5 production libraries and 4 test suites):

```
HPKata/
├── Directory.Build.props              # Enforces TreatWarningsAsErrors, single-node build, MTP args
├── global.json                        # Pins SDK 10.0.300
├── HPKata.slnx                        # Solution manifest
├── HPKata.Core/                       # netstandard2.0;net8.0 pure C# domain (68 shapes, Excel, config, optimization)
│   └── Configuration/                 # KataConfig (86 params), KataConfigParser, KataInstallEnvironment
│   └── Excel/                         # Bi-directional Excel readers/writers for Dam, Cot, Vach, Thong_ke
│   └── Shapes/                        # dang00..dang67 rebar geometry & cut length calculators
│   └── Optimization/                  # to_hop_thep 1D 11.7m cutting stock optimizer
├── HPKata.Core.Tests/                 # net8.0 xUnit test suite (603 tests)
├── HPKata.UI/                         # net8.0-windows WPF MVVM (MaterialDesignThemes 5.2.1)
│   └── ViewModels/                    # SetupViewModel (86 params), Beam/Column/Wall/Schedule ViewModels
│   └── Views/                         # 13 WPF Window dialogs (SetupView, DetailingView, etc.)
│   └── Common/                        # ThemeManager (Dark/Light runtime host theme tracking)
├── HPKata.UI.Tests/                   # net8.0-windows xUnit test suite (66 tests)
├── HPKata.AutoCad/                    # net8.0-windows AutoCAD 2026 (.NET 8) add-in
│   └── Commands/                      # SetupController (setupkata1), Beam/Column/Wall commands
│   └── Interop/                       # CadDocumentContext, KataLayerHelper, HandleLinkService, XData
│   └── GoiLenh_HPKata.lsp             # 118 AutoLISP aliases + 86 config lines
├── HPKata.AutoCad.Tests/              # net8.0-windows xUnit test suite (85 tests)
├── HPKata.Revit/                      # net8.0-windows Revit 2026 (.NET 8) add-in
│   └── Commands/                      # SetupCommand, FramingRebarCommands, Column/Wall commands
│   └── Interop/                       # RevitUnitConverter, RevitWindowHelper, RevitRebarService
│   └── HPKata.Revit.addin             # Revit add-in manifest
├── HPKata.Revit.Tests/                # net8.0-windows xUnit test suite (97 tests)
└── HPKata.E2E.Tests/                  # net8.0-windows E2E test suite (246 tests across Tiers 1-4)
```

### 2.2 Single-Node Build & Test Execution
- **Build Command**:
  ```powershell
  dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
  ```
  **Result**: Clean compilation with **0 Warning(s), 0 Error(s)** in 2.58 seconds.

- **Test Command**:
  ```powershell
  dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build
  ```
  **Result Breakdown**:
  | Test Assembly | Target Framework | Passed | Failed | Skipped | Duration |
  |---|---|:---:|:---:|:---:|:---:|
  | `HPKata.AutoCad.Tests.dll` | net8.0-windows \| x64 | 85 | 0 | 0 | 0.95s |
  | `HPKata.Core.Tests.dll` | net8.0 \| x64 | 603 | 0 | 0 | 10.57s |
  | `HPKata.E2E.Tests.dll` | net8.0-windows \| x64 | 246 | 0 | 0 | 7.44s |
  | `HPKata.Revit.Tests.dll` | net8.0-windows \| x64 | 97 | 0 | 0 | 0.88s |
  | `HPKata.UI.Tests.dll` | net8.0-windows \| x64 | 66 | 0 | 0 | 1.08s |
  | **Total** | | **1,097** | **0** | **0** | **20.92s** |

---

## 3. Audit of `kata_pro` Occurrences

A case-insensitive ripgrep scan was conducted across the entire codebase.

### 3.1 Production Source Files
- `HPKata/HPKata.Core/**`: **0 occurrences**
- `HPKata/HPKata.UI/**`: **0 occurrences**
- `HPKata/HPKata.AutoCad/**`: **0 occurrences**
- `HPKata/HPKata.Revit/**`: **0 occurrences**
- `HPKata/Distribution/**`: **0 occurrences**
- `C:\HPKata\**`: **0 occurrences**

### 3.2 Test Files & Documentation (Non-Production)
Occurrences exist exclusively in:
1. **Documentation**: `PROJECT.md`, `TEST_INFRA.md`, `TEST_READY.md` (audit descriptions and checklists).
2. **Purge Test Assertion**: `HPKata.E2E.Tests/Tier1_FeatureCoverage/Tier1_F14_ZeroKataProPurgeTests.cs` (verifies production code does not contain `kata_pro`).
3. **Reference Oracle Paths** in E2E tests:
   - `Tier1_F13_StandaloneEnvironmentAndAssetsTests.cs` (line 15: `private const string SourceRoot = @"C:\kata_pro";`)
   - `Tier1_F15_ExcelRoundTripTests.cs` (line 15: `private const string SourceKataXlsm = @"C:\kata_pro\Kata.xlsm";`)
   - `Tier1_F16_TemplateImporterAndBlocksTests.cs` (line 15: `private const string SourceDwg = @"C:\kata_pro\Iso_kata.dwg";`)
   - `Tier1_F19_RibbonIconsAndVisualDiagramsTests.cs` (line 14: `private const string SourceRoot = @"C:\kata_pro";`)
   - `Tier4_Scenario6_StandaloneDeploymentScenarioTests.cs` (line 18: `private const string SourceRoot = @"C:\kata_pro";`)

**Recommendation**: Update these 5 test oracle paths to first check `KataInstallEnvironment.Current` (i.e. `C:\HPKata\`), falling back to `C:\kata_pro` only if present, to ensure 100% test independence on systems without `C:\kata_pro`.

---

## 4. Inspection of `C:\HPKata\` Installation State

### 4.1 Asset Inventory in `C:\HPKata\`
The physical directory `C:\HPKata\` is populated with 12 library files and 5 subdirectories:

| Path in `C:\HPKata\` | File/Folder Type | Count / Size | Status | Verification Detail |
|---|---|---|:---:|---|
| `Iso_kata.dwg` | AutoCAD DWG Template | 376,108 bytes | **OK** | Master template containing 119 block definitions, 12 layers, 15 dimstyles |
| `ISO_KATA.EDB` | CSI ETABS Model | 269,975 bytes | **OK** | ETABS v22.7.0 structural model |
| `Kata.xlsm` | Excel Macro Workbook | 474,860 bytes | **OK** | 15 sheets: `Dam`, `Cot`, `Vach`, `Thong ke`, etc. |
| `Kata ENG.xlsm` | Excel Macro Workbook | 186,618 bytes | **OK** | English version calculation sheets |
| `Iso_QHPlus.xls` | Excel Workbook | 562,176 bytes | **OK** | Coupler schedule template |
| `Goi lenh.lsp` | AutoLISP Configuration | 6,145 bytes | **OK** | 118 command aliases + 86 config parameters (purged of `kata_pro`) |
| `GoiLenh_HPKata.lsp` | AutoLISP Configuration | 6,030 bytes | **OK** | Native HPKata command aliases + 86 config parameters |
| `menukata.cui` / `.cuix` | AutoCAD Menu Files | 36,690 / 224,049 bytes | **OK** | Custom ribbon/menu definitions |
| `menukata.mnr` / `_light.mnr` | Compiled Menu Resources | 56,559 bytes each | **OK** | Dark & Light theme menu binaries |
| `kata.ico` | Application Icon | 16,958 bytes | **OK** | Multi-size Windows icon |
| `dangthep\` | Shape Diagram Directory | **162 WMF files** | **OK** | 86 numbered shapes (`00.wmf`..`67.wmf`) + 76 `dang*.wmf` variants |
| `Cot\` | Column Splice Directory | **13 WMF files** | **OK** | Column splice and lap diagrams (`Crank Lap Bar.wmf`, etc.) |
| `Icon\` | Icon Root Directory | **25 files** | **OK** | BMP & PNG action icons |
| `Icon\Ribbon\` | Ribbon Icon Directory | **108 files** | **OK** | 107 command PNG icons + `Ribbon_Command_Clicks.txt` |
| `IMG\` | Guide Diagram Directory | **37 PNG files** | **OK** | Detail diagrams (`Kata-00.png`..`Kata-78.png`) |
| `Temp\` | Temporary Workspace | Directory | **OK** | Scratch workspace for exports |

### 4.2 Missing Artifacts in `C:\HPKata\`
The following assemblies and executables are currently missing from `C:\HPKata\`:
1. `HPKata.Core.dll`
2. `HPKata.UI.dll`
3. `HPKata.AutoCad.dll`
4. `HPKata.Revit.dll`
5. NuGet runtime dependencies: `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll`, `CommunityToolkit.Mvvm.dll`, `ClosedXML.dll`, `QRCoder.dll`, `DocumentFormat.OpenXml.dll`, `ExcelNumberFormat.dll`
6. `HPKata_Setup.exe` (Installer)

---

## 5. Detailed Component Deep-Dive

### 5.1 `KataDwgTemplateImporter` (`HPKata.AutoCad/Interop/`)
- **Requirement**: Clone 119 blocks, 12 layers, text styles, and 15 dimstyles from `C:\HPKata\Iso_kata.dwg` into the active AutoCAD `Database` using `Database.WblockCloneObjects`, with automatic fallback generation if `Iso_kata.dwg` is inaccessible.
- **Current State**:
  - `HPKata.AutoCad/Interop/KataLayerHelper.cs` currently only creates 9 standard layers (`kata_thep chu`, `kata_thep dai`, `kata_net thay`, etc.) when detailing commands run.
  - `IKataDwgTemplateImporter.cs` and `KataDwgTemplateImporter.cs` are **NOT YET CREATED** in `HPKata.AutoCad/Interop/`.
- **Implementation Blueprint Needed**:
  - Create `IKataDwgTemplateImporter.cs`:
    ```csharp
    public interface IKataDwgTemplateImporter
    {
        DwgImportResult ImportTemplate(string templatePath, Database targetDb);
        bool EnsureLayersAndStyles(Database targetDb);
    }
    ```
  - Create `KataDwgTemplateImporter.cs` obeying Rule C2 ($\le 300$ lines) and Rule M3 ($\le 50$ lines/method):
    - Open side Database on `templatePath` (`Iso_kata.dwg`) in `FileShare.Read` mode.
    - Collect `BlockTableRecord` ObjectIds (filtering out `*Model_Space` and `*Paper_Space`).
    - Execute `targetDb.WblockCloneObjects(idCollection, targetBlockTableId, idMap, DuplicateRecordCloning.Ignore, false)`.
    - Clone layers, text styles (`kata_text` with `arial.ttf`), and dimstyles (`kata_dim_iso`).
    - Provide fallback generation when template file is absent.

### 5.2 86-Parameter Config Synchronization (`SetupViewModel`, `SetupController`, `SetupCommand`)
- **Domain & Parser**: `HPKata.Core/Configuration/KataConfig.cs` and `KataConfigParser.cs` fully parse and serialize all 86 parameters from/to `Goi lenh.lsp` lines 152–237.
- **WPF ViewModel**: `SetupViewModel` exposes observable properties for all 86 parameters organized into clean partial classes (`SetupViewModel.DrawingLayers.cs`, `SetupViewModel.RebarDetails.cs`), with `IsDirty` tracking, `LoadFromFileCommand`, and `SaveToFileCommand`.
- **AutoCAD Sync (`SetupController.cs`)**:
  - Loads config via `KataConfigParser.ParseFile(lspPath)` using `ResolveGoiLenhPath()` (checks local dir, then `KataInstallEnvironment.Current.GoiLenhLspPath`).
  - Displays `SetupView` dialog via `AcadApp.ShowModalWindow(view)`.
  - When dialog closes with `dialogResult == true || vm.IsDirty`, executes `SaveConfiguration(vm.ToConfig(), lspPath)` to update lines 152–237 in `Goi lenh.lsp`.
  - **AutoCAD sync is fully operational**.
- **Revit Sync (`SetupCommand.cs`)**:
  - In `HPKata.Revit/Commands/SetupCommand.cs`, lines 20–22:
    ```csharp
    var config = KataConfigDefaults.CreateDefault();
    var vm = new SetupViewModel(config);
    var view = new SetupView(vm);
    ```
  - **Defect**: It creates default configuration instead of loading from `KataInstallEnvironment.Current.GoiLenhLspPath`.
  - **Defect**: When the dialog closes, it does not persist updated parameters back to `Goi lenh.lsp`.
  - **Fix Required**: Update `SetupCommand.cs` to resolve `KataInstallEnvironment.Current.GoiLenhLspPath`, load existing config if present, and save back when dirty.

### 5.3 Visual Asset Binding (`dangthep\`, `Cot\`, `Icon\`, `IMG\`)
- **Requirement**: A centralized visual asset service in `HPKata.UI` that maps shape codes (`"00"`, `"01"`, etc.) and splice types to dynamic image sources (`dangthep\*.wmf`, `IMG\Kata-*.png`, `Cot\*.wmf`), and ribbon actions to `Icon\Ribbon\*.png` with vector icon fallback.
- **Current State**:
  - `IKataVisualAssetService.cs` and `KataVisualAssetService.cs` do **NOT YET EXIST** in `HPKata.UI`.
  - Currently, dialogs like `DetailingView.xaml` and `BeamDetailingWindow.xaml` do not bind their image placeholders to dynamic asset paths resolved via `KataInstallEnvironment`.
- **Implementation Blueprint Needed**:
  - Create `HPKata.UI/Visual/IKataVisualAssetService.cs`:
    ```csharp
    public interface IKataVisualAssetService
    {
        string GetShapeDiagramPath(string shapeCode);
        string GetColumnSpliceDiagramPath(string spliceType);
        string GetRibbonIconPath(string commandName);
        ImageSource? LoadImageSafe(string relativeOrAbsolutePath);
    }
    ```
  - Create `HPKata.UI/Visual/KataVisualAssetService.cs` using `KataInstallEnvironment.Current` for path resolution, with fallback when files are missing.

### 5.4 `HPKata.Installer` / `HPKata_Setup.exe` (Milestone M4)
- **Requirement**: Dedicated standalone installer executable (`HPKata_Setup.exe`) that deploys all assemblies, dependencies, and assets to `C:\HPKata\`, configures `%AppData%\Autodesk\Revit\Addins\2026\HPKata.Revit.addin`, writes `%AppData%\Autodesk\ApplicationPlugins\HPKata.bundle\PackageContents.xml`, and registers `C:\HPKata;C:\HPKata\...` in AutoCAD `TRUSTEDPATHS`.
- **Current State**:
  - The project `HPKata/HPKata.Installer/HPKata.Installer.csproj` has **NOT YET BEEN CREATED**.
  - Current post-build targets in `HPKata.AutoCad.csproj` and `HPKata.Revit.csproj` deploy directly to AutoCAD `ApplicationPlugins\HPKata.bundle\Contents\` and Revit `Addins\2026\HPKata.Revit\` rather than deploying centrally to `C:\HPKata\`.
  - E2E tests in `Tier1_F17_HeadlessInstallerCliTests.cs` already define and verify the CLI switch specification (`--install`, `--uninstall`, `--target-dir`) and XML manifest generation models.

---

## 6. Gap Analysis & Roadmap for Milestone M3 & M4

| Gap ID | Component | Required Action | Target Milestone | Priority |
|---|---|---|:---:|:---:|
| **GAP-01** | `KataDwgTemplateImporter` | Create `IKataDwgTemplateImporter.cs` & `KataDwgTemplateImporter.cs` in `HPKata.AutoCad/Interop/` to clone 119 blocks & 12 layers from `Iso_kata.dwg` | M3 | **P0** |
| **GAP-02** | Revit 86-Param Sync | Update `SetupCommand.cs` in `HPKata.Revit` to load from and save to `KataInstallEnvironment.Current.GoiLenhLspPath` | M3 | **P0** |
| **GAP-03** | Visual Asset Service | Create `IKataVisualAssetService.cs` & `KataVisualAssetService.cs` in `HPKata.UI/Visual/` and bind to detailing dialogs | M3 | **P0** |
| **GAP-04** | Test Oracle Paths | Update `C:\kata_pro` references in 5 E2E test files to prioritize `KataInstallEnvironment.Current` | M3 | **P1** |
| **GAP-05** | `HPKata.Installer` Project | Scaffold `HPKata.Installer.csproj` (.NET 8.0-windows, WPF) producing `HPKata_Setup.exe` with GUI & CLI switches | M4 | **P0** |
| **GAP-06** | Centralized DLL Deploy | Configure deployment of all compiled DLLs and dependencies to `C:\HPKata\` | M4 | **P0** |
| **GAP-07** | Manifest & Registry | Update `HPKata.Revit.addin` and `PackageContents.xml` to point directly to `C:\HPKata\`, register `TRUSTEDPATHS` | M4 | **P0** |

---

## 7. Conclusion

1. **Clean Code & Build Health**: `HPKata/HPKata.slnx` builds cleanly with **0 warnings, 0 errors** and passes **1,097 / 1,097 tests (100%)** under strict single-node flags.
2. **Zero `kata_pro` Integrity**: The production codebase, LISP files, and `C:\HPKata\` are **100% clean** of any `kata_pro` occurrences.
3. **Target `C:\HPKata\` State**: All 358 static library assets (DWG, XLSM, LSP, WMF shapes, PNG icons/diagrams) are in place. Managed DLLs will be deployed upon Milestone M4 installer implementation.
4. **Readiness for Milestone M3**: The foundations in `HPKata.Core` (config model, install environment, shape calculators) and `HPKata.UI` (WPF views/viewmodels) are robust. Implementation of `KataDwgTemplateImporter`, Revit 86-parameter sync, and `KataVisualAssetService` can proceed immediately to complete Milestone M3.
