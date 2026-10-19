# Dispatch Specification — Project Orchestrator (orchestrator_23)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_23\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T02:28:56Z`)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Existing Baseline: 926 automated tests passing 100%, 0 warnings, 0 errors.

## Mission: Transform HPKata into a 100% Standalone Structural Detailing Suite at `C:\HPKata\`

Transform `HPKata` (`HPKata/HPKata.slnx`) into a 100% standalone, self-contained structural rebar detailing & fabrication suite for AutoCAD 2026 and Revit 2026 installed centrally at `C:\HPKata\`, bundled with its own complete CAD/Excel/LISP/Icon/Shape library and a dedicated standalone installer (`HPKata_Setup.exe`), operating identically to original KataPro while having zero dependency on `C:\kata_pro\`.

### R1. Standalone `C:\HPKata\` Installation Directory & Complete Asset Migration (Zero `C:\kata_pro\` Dependency)
1. Create a central `KataInstallEnvironment` service in `HPKata.Core` (default root `C:\HPKata\`, overridable for isolated unit tests) providing canonical paths for all library assets:
   - `C:\HPKata\Kata.xlsm`, `C:\HPKata\Kata ENG.xlsm`, `C:\HPKata\Iso_QHPlus.xls`
   - `C:\HPKata\Iso_kata.dwg` (master CAD template with 119 blocks, 12 layers, 15 dimstyles)
   - `C:\HPKata\Goi lenh.lsp` and `C:\HPKata\GoiLenh_HPKata.lsp` (86-parameter config + 118 AutoLISP command aliases)
   - `C:\HPKata\menukata.cuix`, `C:\HPKata\menukata.cui`, `C:\HPKata\menukata.mnr`, `C:\HPKata\menukata_light.mnr`, `C:\HPKata\kata.ico`, `C:\HPKata\ISO_KATA.EDB`
   - `C:\HPKata\dangthep\` (`00.wmf` .. `67.wmf` rebar shape diagrams)
   - `C:\HPKata\Cot\` (`*.wmf` column splice diagrams)
   - `C:\HPKata\Icon\` (including `C:\HPKata\Icon\Ribbon\*.png`) and `C:\HPKata\IMG\` (`*.png` guide diagrams)
   - `C:\HPKata\Temp\`
2. Copy/stage the library assets from `C:\kata_pro\` into `C:\HPKata\` (and into a distribution staging folder `HPKata/Distribution/Assets/` in the repository), and rewrite all path references inside `C:\HPKata\Goi lenh.lsp`, `C:\HPKata\GoiLenh_HPKata.lsp`, and `C:\HPKata\menukata.cui` so that every `C:\kata_pro` or `C:\\Kata_pro` reference points to `C:\HPKata` (`(command "netload" "C:\\HPKata\\HPKata.AutoCad.dll")` and `(command "_cuiload" "C:\\HPKata\\menukata.cuix")`).
3. Ensure **zero occurrences** of `kata_pro` (case-insensitive) exist anywhere in `HPKata/` source files (`.cs`, `.xaml`, `.lsp`, `.csproj`, `.xml`, `.addin`) or in `C:\HPKata\Goi lenh.lsp`.

### R2. Full KataPro Operational Parity Using the `C:\HPKata\` Library (Excel + DWG + Config + Visual Diagrams)
1. **Bi-Directional Excel Integration (`C:\HPKata\Kata.xlsm` & `C:\HPKata\Iso_QHPlus.xls`)**:
   - Implement clean, modular services in `HPKata.Core/Excel/` (each file `<= 250` lines) that read and write `C:\HPKata\Kata.xlsm` sheets `Dam` (beam spans, supports, top/bottom continuous & curtailment bars, stirrups, skin bars, hanger bars), `Cot` (column stories, cross-sections, longitudinal bars, ties), `Vach` (shear wall geometry, boundary cages, web mesh), and `Thong ke` / `Iso_QHPlus.xls` schedules. Support attaching to a running Excel instance (`Kata.xlsm` open on desktop) with automatic ClosedXML file fallback on `C:\HPKata\Kata.xlsm`.
   - Wire `BeamDetailingViewModel`, `ColumnDetailingViewModel`, and `WallDetailingViewModel` (plus `BeamDetailingWindow.xaml`, `ColumnDetailingWindow.xaml`, `WallDetailingWindow.xaml`) with commands and toolbar buttons to **Open `C:\HPKata\Kata.xlsm`**, **Read from `Kata.xlsm`**, and **Write to `Kata.xlsm`** (and auto-sync on Apply).
   - Ensure AutoCAD commands (`ve_dam`/`vd`, `ve_dam_shop`/`vds`, `loadbeam`, `ve_cot`/`vc`, `ve_cot_shop`/`vcs`, `loadcolumn`, `ve_vach`/`vv`, `ve_vach_shop`/`vvs`, `loadwall`, `toexcel`) and Revit commands (`FramingRebarCommands`, `ColumnRebarCommands`, `WallRebarCommands`) read/write `C:\HPKata\Kata.xlsm` and generate full shop drawings (elevation + cross-sections + `KATA` XData + callouts in AutoCAD; 3D `Rebar` with real element dimensions in Revit).
2. **AutoCAD Template & Block Importer (`C:\HPKata\Iso_kata.dwg`)**:
   - Implement `KataDwgTemplateImporter` in `HPKata.AutoCad/Interop/` that loads standard layers, text styles, dimension styles, and block definitions (`thep`, section symbols, elevation symbols, grid bubbles) from `C:\HPKata\Iso_kata.dwg` into the active AutoCAD `Database` (`WblockCloneObjects`) whenever detailing commands execute (with safe fallback layer creation if `Iso_kata.dwg` is locked).
3. **86-Parameter Setup (`C:\HPKata\Goi lenh.lsp`) & Visual Asset Binding (`dangthep\`, `Cot\`, `Icon\`, `IMG\`)**:
   - Connect `SetupViewModel`, `SetupController` (AutoCAD `setupkata`), and `SetupCommand` (Revit) to load and save the 86 configuration parameters directly from/to `C:\HPKata\Goi lenh.lsp`.
   - Load Ribbon icons in `HPKata.AutoCad` and `HPKata.Revit` from `C:\HPKata\Icon\Ribbon\*.png` (with vector icon fallback), and display rebar shape diagrams (`C:\HPKata\dangthep\*.wmf` or `C:\HPKata\IMG\Kata-*.png`) and column splice diagrams (`C:\HPKata\Cot\*.wmf`) inside `HPKata.UI` dialogs (`DetailingView`, `BeamDetailingWindow`, `ColumnDetailingWindow`, etc.).

### R3. Centralized DLL Deployment at `C:\HPKata\` & Standalone Installer (`HPKata_Setup.exe`)
1. Add a new installer project `HPKata/HPKata.Installer/HPKata.Installer.csproj` (`net8.0-windows`, `UseWPF=true`, `AssemblyName=HPKata_Setup`) to `HPKata/HPKata.slnx`.
2. Implement `HPKata_Setup.exe` with both a clean WPF Installer UI (Install / Repair / Uninstall buttons, target path `C:\HPKata\`, status log) AND headless CLI switches (`--install`, `--uninstall`, `--target-dir <path>`) for automated verification.
3. Configure both MSBuild post-build targets (`HPKata.AutoCad.csproj` and `HPKata.Revit.csproj`) and `HPKata_Setup.exe --install` so that:
   - All compiled managed assemblies (`HPKata.Core.dll`, `HPKata.UI.dll`, `HPKata.AutoCad.dll`, `HPKata.Revit.dll`, and all NuGet runtime dependencies `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll`, `CommunityToolkit.Mvvm.dll`, `ClosedXML.dll`, `QRCoder.dll`, etc.) and all library assets (`Iso_kata.dwg`, `Kata.xlsm`, `Kata ENG.xlsm`, `Iso_QHPlus.xls`, `Goi lenh.lsp`, `GoiLenh_HPKata.lsp`, `menukata.cuix`, `dangthep\`, `Cot\`, `Icon\`, `IMG\`, `Temp\`) are deployed centrally into `C:\HPKata\`.
   - `%AppData%\Autodesk\Revit\Addins\2026\HPKata.Revit.addin` is written with `<Assembly>C:\HPKata\HPKata.Revit.dll</Assembly>`.
   - `%AppData%\Autodesk\ApplicationPlugins\HPKata.bundle\PackageContents.xml` is written with `ModuleName="C:\HPKata\HPKata.AutoCad.dll"` and `ModuleName="C:\HPKata\Goi lenh.lsp"`, and `C:\HPKata` is registered in AutoCAD's `TrustedPaths` in `HKCU\Software\Autodesk\AutoCAD`.

## Constraints & Architectural Rules
- **UI Framework Constraint**: Keep the existing `System.Windows.Window` + `MaterialDesignThemes` (`v5.2.1`) UI framework in `HPKata.UI`, `HPKata.Revit`, and `HPKata.AutoCad`. Do **NOT** switch to `MahApps.Metro`. Keep the existing `Theme.xaml` StaticResource keys (`StandardDataGrid`, `Button.Primary`, `Button.Secondary`, `GroupBox.Header`, `Text.Label`, etc.) intact so all windows instantiate without `StaticResourceExtension` errors.
- **Clean Code Governance**:
  - Every `.cs` file in `HPKata/` must be `<= 300` lines (Rule C2).
  - Every method must be `<= 50` lines (Rule M3).
  - Zero compiler warnings (`TreatWarningsAsErrors=true`), no commented-out code (Q-B1), no empty catch blocks without a reason comment (Q-B2).
- **Single-Node Build Rule**: Always run `dotnet build` and `dotnet test` with `--disable-build-servers -m:1`.

## Acceptance Criteria
- [ ] `C:\HPKata\` contains `HPKata.Core.dll`, `HPKata.UI.dll`, `HPKata.AutoCad.dll`, `HPKata.Revit.dll`, `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `ClosedXML.dll`, `Iso_kata.dwg`, `Kata.xlsm`, `Kata ENG.xlsm`, `Iso_QHPlus.xls`, `Goi lenh.lsp`, `GoiLenh_HPKata.lsp`, `menukata.cuix`, and non-empty directories `C:\HPKata\dangthep\`, `C:\HPKata\Cot\`, `C:\HPKata\Icon\`, `C:\HPKata\IMG\`, `C:\HPKata\Temp\`.
- [ ] Zero occurrences of `kata_pro` (case-insensitive) exist in any source file under `HPKata/` or in `C:\HPKata\Goi lenh.lsp` and `C:\HPKata\GoiLenh_HPKata.lsp`.
- [ ] `%AppData%\Autodesk\Revit\Addins\2026\HPKata.Revit.addin` points `<Assembly>` directly to `C:\HPKata\HPKata.Revit.dll`, and `%AppData%\Autodesk\ApplicationPlugins\HPKata.bundle\PackageContents.xml` points `ModuleName` directly to `C:\HPKata\HPKata.AutoCad.dll`.
- [ ] All 13 WPF windows in `HPKata.UI/Views/` inherit from `System.Windows.Window`, use `MaterialDesignThemes`, and instantiate on an STA thread with both Dark and Light themes without throwing any `StaticResourceExtension` or XAML exceptions.
- [ ] `HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, and `HPKata.E2E.Tests` verify round-trip reading/writing of `C:\HPKata\Kata.xlsm` (`Dam`, `Cot`, `Vach`), 86-parameter config round-trip on `C:\HPKata\Goi lenh.lsp`, asset resolution from `C:\HPKata\`, and CLI execution of `HPKata_Setup.exe --install` into a target directory.
- [ ] `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` succeeds with `0 warnings, 0 errors`, `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` passes 100%, and every `.cs` file in `HPKata/` is `<= 300` lines.
