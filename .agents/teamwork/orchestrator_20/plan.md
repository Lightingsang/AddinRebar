# Plan: HPDcePro Structural Ecosystem Reverse-Engineering & Clean-Room Rewrite

## Objective
Reverse-engineer the legacy structural engineering ecosystem in `C:\Dce Pro` using REA into `docs/rea-dce-pro/` and implement the clean-room C# rewrite in `HPDcePro/` (`HPDcePro.slnx`) with WPF MVVM (`MahApps.Metro`), completing Phase 1 end-to-end with >= 80 passing automated tests.

## Phase 0: Full-Scope Survey (Current)
- Dispatch 3 parallel Explorers:
  - Explorer 1 (`explorer_survey_1`): Binary & Assembly reverse-engineering analysis (`DCE Pro.dll`, `DceAnalyssiss64.dll`, `DceSteel.dll`, `DbimSteelAutocad.dll`, `DCE Excel.dll`, `DBIM_RVT_26.exe`).
  - Explorer 2 (`explorer_survey_2`): Data formats & scripts inspection (`DCE Pro.lsp`, `.autostr`, `mauhoc.db`, Excel workbooks, CAD XData).
  - Explorer 3 (`explorer_survey_3`): Repo architecture, clean code standards, build tools, target project structure.
- Synthesize findings into `PROJECT.md` at repo root (Architecture, Feature Inventory, Milestones, Interface Contracts, Code Layout).

## Phase 1: Milestone M1 — Comprehensive REA Reverse-Engineering Dossier (`docs/rea-dce-pro/`)
- M1.1: Document binary inventory with SHA-256 and REA evidence IDs.
- M1.2: Decompile and document domain architecture and algorithms (RC Beam, Column/Wall, Slab, Foundation/Pile, Stairs, Rebar schedule & cutting optimization, AutoStr 24 steps, MayHoc ML, Steel design, Excel UDFs).
- M1.3: Map legacy commands / LISP aliases to HPDcePro and define 6-phase roadmap.

## Phase 2: Milestone M2 — Solution Setup & Pure C# Domain Core (`HPDcePro.Core`)
- M2.1: Scaffold `HPDcePro/HPDcePro.slnx`, `global.json`, `Directory.Build.props`, `HPDcePro.Core.csproj`.
- M2.2: Pure C# 2D geometry engine (`Polygon2D`, polyline/segment chains, spatial index) replacing native `DceAnalyssiss64.dll`.
- M2.3: Pure C# structural calculations: TCVN 5574:2018 RC Beam flexure/shear/crack/detailing; TCVN 10304:2025 pile capacity; TCVN 5575:2024 steel design; TCVN 2737:2023 wind load.
- M2.4: 1D Rebar Cutting Stock Optimization (11.7m commercial bar length, laps, cutting patterns) & Schedule aggregation.
- M2.5: Zero legacy licensing/telemetry/updater code.

## Phase 3: Milestone M3 — Project Serialization, CAD XData & SQLite ML
- M3.1: `.autostr` project JSON serialization/deserialization (verified compatible with `DuAnMau.autostr`).
- M3.2: CAD `XData` packet encoding/decoding (`RebarXdataDCE`, `DceRebarXDataTKT`, `Links_XDataObject`).
- M3.3: SQLite reader/predictor for `MayHoc/mauhoc.db` (`MauHoc`, `LichSuSua`, `CauHinhModel`).

## Phase 4: Milestone M4 — Multi-Host Adapters & MahApps.Metro WPF UI
- M4.1: `HPDcePro.UI`: MVVM Views/ViewModels (`MahApps.Metro` MetroWindow) for Beam Detailing, Cutting Optimization, AutoStr Project config.
- M4.2: `HPDcePro.AutoCad`: AutoCAD commands & aliases matching `DCE Pro.lsp`, clean `HPDcePro.lsp` loader.
- M4.3: `HPDcePro.Excel`: Excel UDF / export adapter (ClosedXML schedule export, legacy UDF compatibility).
- M4.4: `HPDcePro.Revit`: Structural data exchange skeleton.

## Phase 5: Milestone M5 — Automated Test Suite & Verification (`HPDcePro.Core.Tests`)
- M5.1: Scaffold `HPDcePro.Core.Tests` (net10, xUnit v3 / MTP).
- M5.2: Implement comprehensive test suite (>= 80 tests covering geometry, `.autostr` roundtrip, `mauhoc.db` reading, `XData`, TCVN 5574:2018, TCVN 10304:2025, TCVN 5575:2024, cutting optimization).
- M5.3: Ensure 100% green tests under `dotnet test --disable-build-servers -m:1`.

## Phase 6: Final Verification, Victory Audit & Handoff
- M6.1: Challenger & Reviewer verification of build, tests, and clean code compliance (<= 300 lines/file, <= 50 lines/method, 0 warnings with TreatWarningsAsErrors=true).
- M6.2: Forensic Auditor verification for genuine implementation (no stubs, no cheating, no legacy licensing).
- M6.3: Hand off completed project to Parent Sentinel with full evidence summary.
