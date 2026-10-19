# BRIEFING — 2026-10-18T03:37:00Z

## Mission
Implement Milestone M2 (Features 5–8): Bi-directional Excel Engine in HPKata.Core, ViewModel & View Excel Integration in HPKata.UI, AutoCAD Detailing Commands Excel Sync, and Revit Detailing Commands Excel Sync with 100% test pass and zero warnings.

## 🔒 My Identity
- Archetype: worker_hpkata_m2
- Roles: implementer, qa, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2
- Original parent: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- Milestone: Milestone M2 (Features 5–8)

## 🔒 Key Constraints
- Pure C# in HPKata.Core (zero references to Autodesk.AutoCAD.* or Autodesk.Revit.*).
- Every .cs file <= 300 lines (Rule C2), every method <= 50 lines (Rule M3).
- 0 warnings (TreatWarningsAsErrors=true), 0 commented out code (Q-B1), no empty catch blocks without reason (Q-B2).
- Single-node build rule: dotnet build and dotnet test with --disable-build-servers -m:1.
- Retain System.Windows.Window + MaterialDesignThemes v5.2.1, Theme.xaml keys intact, no MahApps.Metro.
- All 13 WPF windows must instantiate cleanly on STA thread without throwing under both Dark and Light themes.
- Support parameterless/default constructors on Detailing ViewModels.
- Hermetic tests in HPKata.Core.Tests using KataInstallEnvironment.UseRootDirectory(tempDir).
- Genuine implementations only: no hardcoding, no facades, maintain real state.

## Current Parent
- Conversation ID: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- Updated: 2026-10-18T03:36:08Z

## Task Summary
- **What to build**: Milestone M2 (Features 5-8): Bi-directional Excel Engine in HPKata.Core, ViewModel/View integration in HPKata.UI, AutoCAD commands sync, Revit commands sync, and comprehensive unit/integration test suites.
- **Success criteria**: 100% build pass with 0 errors/0 warnings, 100% test pass (1,019 existing + all new M2 tests), all .cs files <= 300 lines, all methods <= 50 lines.
- **Interface contracts**: HPKata/PROJECT.md
- **Code layout**: HPKata/PROJECT.md § Code Layout

## Key Decisions Made
- Decompose Excel engine into dedicated files (DTOs, abstractions, session providers, readers/writers, mappers, adapter) to stay strictly <= 250 lines per file (Rule C2).
- Use partial classes for Detailing ViewModels (.Excel.cs) to isolate Excel sync logic and keep files <= 200 lines.
- Retain optional IKataExcelAdapter? parameter in ViewModel constructors to support both DI and parameterless callers.
- In KataScheduleSheetWriter, ensure Title 'BẢNG THỐNG KÊ CỐT THÉP' is written at A1 and table headers at row 3 for canonical ClosedXML and Kata compatibility.
- Ensure all host unit tests avoid runtime AutoCAD/Revit API assembly coupling in headless test runners.

## Artifact Index
- DISPATCH.md — Assignment specification
- progress.md — Liveness heartbeat & progress tracker
- handoff.md — Final completion report
- HPKata.Core/Excel/ — Bi-directional Excel engine (Features 5 & 8)
- HPKata.UI/ViewModels/ — Detailing ViewModels companion Excel files & XAML toolbars (Feature 6)
- HPKata.AutoCad/Controllers/ & Commands/ — Detailing AutoCAD command handlers with KataLayerHelper & KataEntityStyler (Feature 7)
- HPKata.Revit/Commands/ & Runners/ — Detailing Revit commands Excel sync (Feature 8)

## Change Tracker
- **Files modified/added**: 32 files across HPKata.Core, HPKata.UI, HPKata.AutoCad, HPKata.Revit and their test suites
- **Build status**: PASS (0 warnings, 0 errors under TreatWarningsAsErrors=true)
- **Pending issues**: None

## Quality Status
- **Build/test result**: PASS (1,054 / 1,054 tests passed across 5 assemblies)
- **Lint status**: Clean (0 empty catch, 0 commented-out code, 100% Rule C2 <= 300 lines, 100% Rule M3 <= 50 lines)
- **Tests added/modified**: +35 tests added for M2 (Core: 26, UI: 9, AutoCad: 10, Revit: 5)

## Loaded Skills
- None

