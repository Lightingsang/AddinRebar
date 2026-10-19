# DISPATCH — Worker M3 (HPKata Standalone Implementation & Installer)

## Working Directory
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m3\`

## Mandatory Reference Files to Read
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (headers ## 2026-10-18T02:28:56Z and ## 2026-10-18T05:29:18Z)
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_1\report.md` & `handoff.md`
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-kata-pro\REA_6_6_0_KATA_PRO_AUDIT.md`

## Mission & Implementation Tasks
1. **AutoCAD Template & Block Importer**:
   - Create `HPKata/src/HPKata.AutoCad/Interop/KataDwgTemplateImporter.cs` (<= 300 lines, methods <= 50 lines).
   - Loads standard layers, text styles, dimension styles, and block definitions from `C:\HPKata\Iso_kata.dwg` into active AutoCAD `Database` (`WblockCloneObjects`) when detailing commands execute, with safe fallback.
2. **Revit 86-Parameter Config Sync**:
   - Update `HPKata/src/HPKata.Revit/Commands/SetupCommand.cs` to bind to `KataInstallEnvironment.GetConfigFilePath()` (`C:\HPKata\Goi lenh.lsp`) and load/save configuration using `SetupViewModel`.
3. **Visual Asset Binding**:
   - Create `HPKata/src/HPKata.Core/Assets/KataVisualAssetService.cs` (<= 300 lines) providing typed resolution and stream/file access to `dangthep\*.wmf`, `Cot\*.wmf`, `Icon\Ribbon\*.png`, and `IMG\*.png`.
4. **Standalone Installer (`HPKata_Setup.exe`)**:
   - Create project `HPKata/src/HPKata.Installer/HPKata.Installer.csproj` (`net8.0-windows`, `UseWPF=true`, `AssemblyName=HPKata_Setup`).
   - Add project to `HPKata/HPKata.slnx`.
   - Implement `MainWindow.xaml` + `MainWindow.xaml.cs` (WPF UI with MaterialDesignThemes 5.2.1: Install, Repair, Uninstall, target path `C:\HPKata\`, log).
   - Implement headless CLI `--install`, `--uninstall`, `--target-dir <path>`.
   - Deploys compiled assemblies (`HPKata.Core.dll`, `HPKata.UI.dll`, `HPKata.AutoCad.dll`, `HPKata.Revit.dll`, NuGet dependencies) and library assets to `C:\HPKata\`.
   - Writes `%AppData%\Autodesk\Revit\Addins\2026\HPKata.Revit.addin` and `%AppData%\Autodesk\ApplicationPlugins\HPKata.bundle\PackageContents.xml`.
5. **Unit & Integration Tests**:
   - Add tests covering `KataDwgTemplateImporter`, Revit `SetupCommand`, `KataVisualAssetService`, and `HPKata.Installer` in test projects (`HPKata.Core.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, `HPKata.E2E.Tests`).
6. **Build & Verify**:
   - Run `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` (0 warnings, 0 errors).
   - Run `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build` (100% pass).
   - Execute `HPKata_Setup.exe --install` to verify deployment to `C:\HPKata\`.

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## 2026-10-18T06:06:18Z
You are teamwork_preview_worker_m3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m3\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m3\DISPATCH.md
Read the authoritative request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (headers ## 2026-10-18T02:28:56Z and ## 2026-10-18T05:29:18Z)
Read Explorer M3-1 handoff at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_1\handoff.md

