# DISPATCH — 2026-10-17T16:06:44Z

## Task Assignment for orchestrator_20 (Project Orchestrator)

You are **orchestrator_20**, the Project Orchestrator responsible for:
1. Reverse-engineering the structural engineering ecosystem in `C:\Dce Pro` using REA into `docs/rea-dce-pro/`.
2. Implementing the clean-room C# rewrite in `HPDcePro/` (`HPDcePro.slnx`) with WPF MVVM (`MahApps.Metro`), completing Phase 1 end-to-end (2D geometry core replacing `DceAnalyssiss64.dll`, `.autostr` project I/O, `XData` backward compatibility, TCVN 5574:2018 RC beam calculation & detailing, rebar schedule & cutting optimization, `MayHoc/mauhoc.db` ML rebar suggestion, ETABS DTO bridge, and AutoCAD/Excel/Revit host projects) alongside the multi-phase architecture for Phases 2–6.

Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_20\`
Parent Sentinel conversation ID: `0a049d24-8203-4e76-b372-e203b1b67e80`
Authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-17T16:06:44Z`)

## Verification Resources (Read-Only)
- Shipped target binaries and data:
  - `C:\Dce Pro\DCE Pro.dll` (SHA-256: `8f7b56d1f916351fb91a8464628471499f59006ac3ba5e362b9df8dbc1225780`, REA Evidence ID: `ev_c1bcd4621c3710ffb087bed3d8120354cc071ae032c61a7ad12027fdc929983e`)
  - `C:\Dce Pro\DceAnalyssiss64.dll`
  - `C:\Dce Pro\DCE Pro.lsp`
  - `C:\Dce Pro\AutoStr\DuAnMau.autostr`
  - `C:\Dce Pro\MayHoc\mauhoc.db` (SHA-256: `74eb37741c999c677df271a6c8007d2d09b704fb16c7aff884f4f11ab624e1a8`, REA Evidence ID: `ev_5b5a0aff2c55fc84157b43574b7f79175f0dcda7e6d1ae96721a97d1d17a0241`)
  - `C:\Dce Pro\AddinSteel\DceSteel.dll`
  - `C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll`
  - `C:\Dce Pro\V.2020\DCE Excel.dll`
  - `C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe`
  - `C:\Dce Pro\Excel\File V.2020\*.xlsm` & `C:\Dce Pro\Excel\DceSteel\*.xlsm`
- Repository governance & clean-code standards:
  - `AGENTS.md`
  - `docs/clean-code/HP_CLEAN_CODE_CORE.md`
  - `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Requirements Summary

### R1. Comprehensive REA Reverse-Engineering Dossier (`docs/rea-dce-pro/`)
Produce a complete reverse-engineering technical specification in `docs/rea-dce-pro/` covering every subsystem in `C:\Dce Pro`:
- Managed & native binary inventory with REA Evidence IDs and SHA-256 digests (`DCE Pro.dll`, `DceAnalyssiss64.dll`, `DceSteel.dll`, `DbimSteelAutocad.dll`, `DCE Excel.dll`, `DBIM_RVT_26.exe`, `MayHoc/mauhoc.db`).
- Decompiled domain architecture and algorithm specifications for all modules: RC Beam (`DceDamMatCatMoi`, `DceShopBeam`, `DuAnVeDam`), Column/Shear Wall (`DceColumn`, `DceWall`), Slab (`DceSlab`), Pile & Foundation (`DceMongCocMoi`, `DceVeCocBeTong`, `VeMongDon`), Stairs & Lintel (`DceThangMoi`), Rebar Schedule & Cutting Optimization (`Bea_ToiUuCatThepV2`, `RebarXdataDCE`, `DceRebarXDataTKT`), `AutoStr` 24-step automated structural workflow (`AutoStrStep01`..`AutoStrStep24`), `DceAiAgent` & `MayHoc` (`mauhoc.db` schema and ML pipeline), Steel Design (`DceSteel` & `DbimSteelAutocad`), and `DCE Excel` UDFs.
- Explicit mapping table from legacy classes/commands/LISP aliases (`DCE Pro.lsp`) to the new `HPDcePro` architecture, plus the staged implementation roadmap for Phases 1–6.

### R2. Independent Solution & Pure C# Domain Core (`HPDcePro/HPDcePro.Core`)
Create an isolated deliverable folder `HPDcePro/` with its own `HPDcePro.slnx`, `global.json` (pinned to `Microsoft.Testing.Platform`), and `Directory.Build.props` that never cross-references sibling product folders (except `../McpShared/` if needed):
- Implement pure C# 2D geometry (`Polygon2D`, polyline/segment chains, vector normalization, plane/origin projection, spatial grid) replacing the native P/Invoke calls to `DceAnalyssiss64.dll` (`ClassLibrary1.DAnalysisss`).
- Implement full serialization/deserialization for `.autostr` JSON project files (compatible with `C:\Dce Pro\AutoStr\DuAnMau.autostr`) and CAD `XData` schemas (`RebarXdataDCE`, `DceRebarXDataTKT`, `Links_XDataObject`).
- Implement pure C# engineering calculation engines (no background Excel COM dependency) for TCVN 5574:2018 RC Beam flexure/shear/crack/rebar layout, 1D Rebar Cutting Stock Optimization (`ToiUuCatThep` with commercial bar length 11700 mm and lap/anchorage rules), Rebar Schedule aggregation (`ThongKeThep`), SQLite reader/predictor for `MayHoc/mauhoc.db` (`MauHoc`, `LichSuSua`, `CauHinhModel`), and core calculation models for TCVN 10304:2025 pile capacity, TCVN 5575:2024 steel design, and TCVN 2737:2023 wind load.
- Completely exclude all legacy licensing, account, telemetry, and updater mechanisms (`LicenseMoi`, `LicenseCheckAPI`, `DceSerinumber`, `DbimTaiKhoanApi`, `DceUpdate`).

### R3. Multi-Host Adapters & MahApps.Metro WPF UI (`HPDcePro.AutoCad`, `HPDcePro.Excel`, `HPDcePro.Revit`, `HPDcePro.UI`)
Build the host projects in `HPDcePro/HPDcePro.slnx`:
- `HPDcePro.AutoCad` + `HPDcePro.UI`: WPF MVVM user interface built with `MahApps.Metro` (`MetroWindow`, dark/light theme support, Beam Detailing & Rebar Cutting Optimization & AutoStr Project configuration views/viewmodels) and AutoCAD command adapters preserving legacy command names and LISP aliases from `DCE Pro.lsp` (`ShopDrawingBeam`, `VeDam_DCE`, `VeDamAll_DCE`, `VeMCD_DCE`, `VeBlockDam2`..`VeBlockSection`, `TKTSSOLUONGCAUKIEN`, `TKTSDUONGKINHTHEP`, `ToiUuCatThep`, `tkt_dce`, `stk_dce`, `TongHopThongKe_dce`, `XuatDuLieuModelEtabs`, `UpdateModelEtabs`, etc.) plus a clean `HPDcePro.lsp` loader.
- `HPDcePro.Excel`: Excel UDF / report export adapter providing backward-compatible calculation functions for `DCE Excel` / `DceSteel` workbooks and ClosedXML/CSV schedule export without Excel COM hangs.
- `HPDcePro.Revit`: Structural data exchange & rebar/framing adapter skeleton compatible with the repository's Revit architecture.

### R4. Comprehensive Automated Test Suite (`HPDcePro.Core.Tests`)
Create an xUnit v3 test project `HPDcePro.Core.Tests` running on `Microsoft.Testing.Platform` that thoroughly tests:
- 2D geometry operations and polygon/rebar spatial queries.
- Round-trip parsing of the real `C:\Dce Pro\AutoStr\DuAnMau.autostr` file and reading the real `C:\Dce Pro\MayHoc\mauhoc.db` SQLite database.
- `XData` packet encoding/decoding (`RebarXdataDCE`, `DceRebarXDataTKT`, `Links_XDataObject`) including malformed/corrupted payload recovery.
- TCVN 5574:2018 RC beam calculation (happy path, minimum/maximum reinforcement ratio edge cases, double reinforcement, stirrup spacing) and 1D rebar cutting optimization (`ToiUuCatThep` including bars exceeding 11.7 m requiring lap splices).
- TCVN 10304:2025 pile bearing capacity, TCVN 5575:2024 steel section checks, and TCVN 2737:2023 wind load calculation formulas.

### R5. Controlled Environment & Clean Code Governance
- Treat `C:\Dce Pro` strictly as read-only input; never modify or delete any file in `C:\Dce Pro`.
- All new C# code must pass the rules in `docs/clean-code/HP_CLEAN_CODE_CORE.md` and `docs/clean-code/CODE_REVIEW_CHECKLIST.md` (no empty catch blocks without reason comments, no commented-out code, no methods or types violating clean-code modularity).
- Always run `dotnet build` and `dotnet test` with `--disable-build-servers -m:1` to avoid MSBuild node contention on Windows.
- Every C# file <= 300 lines (Rule C2), methods <= 50 lines (Rule M3).
- Zero compiler warnings (`TreatWarningsAsErrors=true`).

## Acceptance Criteria
- [ ] `docs/rea-dce-pro/README.md` and companion module documents exist and cite verified REA Evidence IDs and SHA-256 digests for `DCE Pro.dll`, `DceAnalyssiss64.dll`, `DceSteel.dll`, `DbimSteelAutocad.dll`, `DCE Excel.dll`, `DBIM_RVT_26.exe`, and `MayHoc/mauhoc.db`.
- [ ] Every command in `C:\Dce Pro\DCE Pro.lsp` and all 24 `AutoStrStep01`..`AutoStrStep24` classes are documented with their inputs, outputs, and `HPDcePro` target mapping.
- [ ] `dotnet build HPDcePro/HPDcePro.slnx` succeeds with `0 errors` and `0 warnings`.
- [ ] `HPDcePro.UI` / `HPDcePro.AutoCad` references `MahApps.Metro` and provides MVVM Views/ViewModels for Beam Detailing, Rebar Schedule/Cutting Optimization, and `.autostr` Project Configuration.
- [ ] Zero references to legacy licensing/telemetry/updater endpoints exist anywhere in `HPDcePro/`.
- [ ] `dotnet test HPDcePro/HPDcePro.Core.Tests` succeeds with 100% passing tests (at least 80 unit/integration tests covering geometry, `.autostr` real-file parsing, `mauhoc.db` real SQLite inspection, `XData` serialization, TCVN 5574:2018 beam design, TCVN 10304:2025 pile SPT capacity, TCVN 5575:2024 steel checks, and 1D cutting stock optimization).


## 2026-10-17T16:09:17Z
You are orchestrator_20, the Project Orchestrator.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_20\
Your parent Sentinel conversation ID is: 0a049d24-8203-4e76-b372-e203b1b67e80
Detailed assignment: Reverse-engineer C:\Dce Pro using REA into docs/rea-dce-pro/, and implement the clean-room C# rewrite in HPDcePro/ (HPDcePro.slnx) with WPF MVVM (MahApps.Metro), completing Phase 1 end-to-end, with tests in HPDcePro.Core.Tests (>= 80 passing tests), clean code rules, maintain plan.md and progress.md, and send completion report to Parent Sentinel.
