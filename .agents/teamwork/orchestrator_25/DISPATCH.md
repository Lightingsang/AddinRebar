# Dispatch Specification — Project Orchestrator (orchestrator_25)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T05:27:06Z`)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Read-Only Legacy Baseline: `C:\Dce Pro`
- Analysis Engines: `rea@6.6.0` (`C:\Users\STR-HP03\AppData\Roaming\npm\rea.cmd`) and `ilspycmd` (`C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe`)

## Mission
Analyze the legacy structural engineering software suite at `C:\Dce Pro` using `rea@6.6.0` and `ilspycmd`, record a complete and strictly accurate function-by-function compatibility matrix (`docs/rea-dce-pro/TRUTH_MATRIX.md`), migrate and normalize all static engineering templates and databases into `HPDcePro/assets/`, and build a 100% standalone, feature-complete C# replacement (`HPDcePro/HPDcePro.slnx`) operating identically to `DCE Pro` in AutoCAD 2026, Revit 2026, and Microsoft Excel with zero runtime dependency on `C:\Dce Pro`.

## Requirements

### R1. Comprehensive Binary Analysis, Package Extraction & Honest Truth Matrix (`docs/rea-dce-pro/TRUTH_MATRIX.md`)
- Extract `C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe` (Inno Setup) into a temporary scratch workspace outside `C:\Dce Pro`.
- Inspect all managed and native assemblies in `C:\Dce Pro` (`DCE Pro.dll`, `DceAnalyssiss64.dll`, `DceSteel.dll`, `DbimSteelAutocad.dll`, `DCE Excel.dll`, and extracted Revit 2026 DLLs) using `rea@6.6.0` and `ilspycmd`.
- Cross-reference every discovered command, form, jig, and function against `C:\Dce Pro\AiAgent\DanhBa\*.json` (311 AutoCAD `<CommandMethod>` entries, 73 Excel Ribbon commands, 49 `DceSteel` functions, 29 `MBKC` functions, 69 native C++ exports, and all Revit external commands) in `docs/rea-dce-pro/TRUTH_MATRIX.md`.
- Mark any existing command in `HPDcePro` that currently uses hardcoded sample/demo data (such as `CalculateSampleBeam` or `GenerateSampleColumnBuffer`) as `ĐANG LÀ MOCK`.
- Any function or method whose metadata or body cannot be inspected must be explicitly marked `CHƯA GIẢI MÃ ĐƯỢC` with exact Metadata Token / RVA and technical reason — never guessed or faked.

### R2. Standalone Asset & Template Migration (`HPDcePro/assets/`)
- Copy and normalize all required static engineering data assets from `C:\Dce Pro` into `HPDcePro/assets/` (17 `.dwg` templates, 132 `.xlsm` workbooks, `MayHoc\mauhoc.db` + `model.zip`, 178 `System\*.txt` config files, `Language\**\*.txt` dictionaries, `Icon\**`, and `.cuix` menus).
- Update all file paths inside migrated `.lsp`, `.xlsm`, and config files to resolve relative to `HPDcePro/assets/` and `HPDcePro.Excel` so software operates 100% independently even if `C:\Dce Pro` is renamed or absent.

### R3. 1:1 AutoCAD, Revit & Excel Functional, UI, and Interactive Jig Migration
- Replace all simplified/mock implementations across `HPDcePro.Core`, `HPDcePro.UI`, `HPDcePro.AutoCad`, `HPDcePro.Revit`, and `HPDcePro.Excel` with authentic 1:1 implementations matching `DCE Pro`:
  - **AutoCAD (`HPDcePro.AutoCad`)**: Implement real interactive selection/point prompts (`Editor.GetSelection`, `Editor.GetEntity`, `Editor.GetPoint`, `Editor.GetKeywords`), all 11 interactive `DrawJig`/`EntityJig` classes (`VeMBKC_JIG`, `JigSlabRebar`, `TagLeader`, `TagChiaThep`, `ijgRaithepchamtron`, `ijgVeDaiVong`, `JigVeDaiC`, `VePolylineBatKy`, `ijgVeThepVachCT`, `JigVeVachHam`, `DimNhanhMBKC`), real XData read/write across all 13 RegApps, real DWG block insertion from `HPDcePro/assets/Cad/`, and all 24 `AutoStr` steps (`AutoStrStep01`..`24`).
  - **UI (`HPDcePro.UI`)**: Implement specialized Forms/Dialogs (`DamCong_Frm`, `MongCoc_Frm`, `MongCoc_FrmLopThepThem`, `MongCoc_FrmChonThepTuDong`, `ThangMoi_Frm`, `CocBT_Frm`, `TKT_frmMain`, `AutoStrWindow`, `Bea_ToiUuCatThepV2`, etc.), `PaletteSet` (`DcePalette`, `DcePalette_WallThickness`), and Ribbon (`DceRibbon` + `.cuix`).
  - **Revit (`HPDcePro.Revit`) & Excel (`HPDcePro.Excel`)**: Implement all Revit 2026 commands/dialogs/rebar generators from `DBIM_RVT_26.exe` and all 73 Excel Ribbon/dispatch actions + UDFs from `DCE Excel.dll`.
  - **Exclusions**: Exclude ZWCAD/GstarCAD/IntelliCAD ports (`ZwDCE Pro.dll`, `GcDCE Pro.dll`, `IcDCE Pro.dll`), Vosk voice recognition (`libvosk.dll` + `DceAiAgent`), and legacy cloud account/updater modules (`DbimTaiKhoanApi`, `DceUpdate.exe`).

### R4. Repository & Build Constraints
- **Strictly Read-Only Source**: Never create, modify, rename, or delete any file inside `C:\Dce Pro`.
- **Clean Code Governance**: Every new or modified `.cs` file in `HPDcePro/` must obey `AGENTS.md` and `docs/clean-code/HP_CLEAN_CODE_CORE.md` (<= 300 lines per file, <= 50 lines per method, 0 commented-out code, 0 unannotated empty catch blocks, `TreatWarningsAsErrors=true`).
- **Sequential Build Execution**: Always run `dotnet build` and `dotnet test` with `--disable-build-servers -m:1`.

## Acceptance Criteria
- [ ] `docs/rea-dce-pro/TRUTH_MATRIX.md` inventories 100% of the 311 AutoCAD commands, 73 Excel Ribbon commands, 49 Steel functions, 69 native C++ exports, 11 CAD Jigs, specialized UI forms, and all unpacked `DBIM_RVT_26.exe` Revit commands with exact legacy class/method names, metadata tokens, and current verification status.
- [ ] Zero functions or commands in `HPDcePro` use hardcoded sample/demo data (`CalculateSampleBeam`, `GenerateSampleColumnBuffer`, or fake placeholder outputs) while being marked completed.
- [ ] Any legacy method whose body cannot be inspected is explicitly listed under `CHƯA GIẢI MÃ ĐƯỢC` in `docs/rea-dce-pro/TRUTH_MATRIX.md` with its exact assembly, token/RVA, and technical blocker.
- [ ] `HPDcePro/assets/` contains the complete, normalized set of 17 `.dwg` templates, 132 `.xlsm` workbooks, `mauhoc.db` + `model.zip`, 178 `System\*.txt` files, `Language\**\*.txt`, `Icon\**`, and `.cuix` files.
- [ ] A static and runtime grep across `HPDcePro/src/**` and `HPDcePro/assets/**` confirms zero runtime dependency on `C:\Dce Pro` (all paths resolve relative to `HPDcePro`'s own installation/assets directory).
- [ ] `dotnet build HPDcePro/HPDcePro.slnx -c Release --disable-build-servers -m:1` and `-c Debug` succeed with `0 Warning(s), 0 Error(s)`, and `HPDcePro/deploy/pack-bundle.ps1` deploys cleanly to AutoCAD, Revit 2026, Desktop, and MCP.
- [ ] Every item marked `HOÀN THÀNH` in `docs/rea-dce-pro/TRUTH_MATRIX.md` is backed by: (1) decompiled source/IL parity check (`rea@6.6.0` / `ilspycmd`), (2) automated unit/integration tests in `HPDcePro/tests/` verifying identical calculation/XData/geometry/Excel outputs, and (3) execution verification.


## 2026-10-18T05:30:07Z

You are the Project Orchestrator (orchestrator_25) for HPDcePro 100% Standalone Reverse-Engineering & Truth Matrix.

Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\

Read your dispatch specification and authoritative user request immediately:
- Dispatch Specification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\DISPATCH.md
- Authoritative User Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (header ## 2026-10-18T05:27:06Z)
- Clean Code Rules: AGENTS.md and docs/clean-code/HP_CLEAN_CODE_CORE.md
- Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx
- Legacy Read-Only Baseline: C:\Dce Pro
- Analysis Tools: rea@6.6.0 (C:\Users\STR-HP03\AppData\Roaming\npm\rea.cmd) and ilspycmd (C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe)

Key Requirements:
1. R1: Comprehensive Binary Analysis & Extraction. Extract DBIM_RVT_26.exe to scratch dir outside C:\Dce Pro. Inspect assemblies with rea and ilspycmd. Generate docs/rea-dce-pro/TRUTH_MATRIX.md cross-referencing all 311 AutoCAD commands, 73 Excel Ribbon commands, 49 Steel functions, 29 MBKC functions, 69 native C++ exports, and all Revit commands. Mark mocks as ĐANG LÀ MOCK, uninspectable methods as CHƯA GIẢI MÃ ĐƯỢC with Token/RVA.
2. R2: Standalone Asset & Template Migration. Copy & normalize 17 DWGs, 132 XLSMs, mauhoc.db + model.zip, 178 System txt files, Language txt, Icons, cuix into HPDcePro/assets/ with relative path resolution. Zero runtime dependency on C:\Dce Pro.
3. R3: 1:1 AutoCAD, Revit & Excel Functional, UI, and Interactive Jig Migration. Implement real selection/prompts, 11 DrawJig/EntityJig classes, 13 RegApps XData, 24 AutoStr steps, specialized Forms/Dialogs, Palettes, Ribbons, Revit commands & Excel UDFs. Exclude ZW/Gc/Ic, Vosk, DbimTaiKhoanApi, DceUpdate.
4. R4: Strictly read-only C:\Dce Pro, Clean Code Governance (<=300 lines/file, <=50 lines/method, 0 dead code, 0 unannotated catch, TreatWarningsAsErrors=true), sequential single-node build (--disable-build-servers -m:1).
