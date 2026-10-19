# HPKata Project Plan

## Architecture & Overview
Clean-room reverse-engineering of KataPro structural detailing suite at `C:\kata_pro` into a comprehensive blueprint (`docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`) and building `HPKata` (`HPKata/HPKata.slnx`), a production-ready dual-host AutoCAD 2026 (.NET 8) and Revit 2026 (.NET 8) rebar detailing system with 1D cutting stock optimization and full two-way compatibility with KataPro DWG XData and Excel templates.

## Phases & Milestones

### Phase 0: Survey & Scope Mapping (Current)
- Survey 1: Command inventory, Lisp aliases (>110 commands), Ribbon structure, 86-line config schema, subsystem decomposition.
- Survey 2: DWG schemas (`Iso_kata.dwg`), XData RegApp / DxfCodes, 68 rebar shapes (`dang00`..`dang67`), bend/hook/lap rules.
- Survey 3: Scheduling, 1D cutting-stock (`to_hop_thep`) logic, Excel templates (`Kata.xlsm`, `Iso_QHPlus.xls`), coupler schedules, Revit integration model (`Kata_Class_Lib_Revit.dll`).

### Milestone M1: Comprehensive Reverse-Engineering Blueprint
- Target: `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`
- Synthesize all findings from Survey into a comprehensive, authoritative technical blueprint covering all 6 subsystems, DWG schemas, 68 shapes, 86 config items, cutting stock algorithms, and interop schemas.

### Milestone M2: HPKata.Core — Domain Library & Shapes Engine
- Target: `HPKata/HPKata.Core/` (`netstandard2.0` / `net8.0`, zero CAD/Revit references)
- 68 standard rebar shapes, hook/bend/lap calculations.
- 86-parameter configuration parser & serializer.
- KataPro XData RegApp & Block Attribute two-way serializer.
- Schedule aggregators, coupler schedule, 1D cutting stock engine (`to_hop_thep`), QR tag generator.
- Headless Excel export (`ClosedXML`) + COM interop.

### Milestone M3: HPKata.Core.Tests & Test Suite Verification
- Target: `HPKata/HPKata.Core.Tests/`
- Minimum 100 unit tests covering all 68 shapes, cut lengths, bend deductions, 86 config parameters, XData round-trip, 1D cutting stock optimization on 11.7m bars, Excel reports.
- 100% green test pass.

### Milestone M4: HPKata.UI — Theme-Aware WPF Library
- Target: `HPKata/HPKata.UI/` (`net8.0-windows`, WPF, `CommunityToolkit.Mvvm`, `MahApps.Metro`)
- Host-theme-aware (Dark/Light) dialogs for Setup (`setupthep`), Rebar Drawing & Editing (`ve_thep`), Schedule & Cutting Optimization (`thong_ke_thep` / `to_hop_thep`).

### Milestone M5: HPKata.AutoCad 2026 & Companion Lisp
- Target: `HPKata/HPKata.AutoCad/` (.NET 8 AutoCAD 2026 plugin) + `GoiLenh_HPKata.lsp`
- Commands for Rebar Creation, Editing, Dynamic Tag Linking (`link`, `ud`, `tim`, etc.), Scheduling (`tkt`, `tohopthep`, `toexcel`).
- DWG XData and Block Attribute synchronization.

### Milestone M6: HPKata.Revit 2026 Add-in
- Target: `HPKata/HPKata.Revit/` (.NET 8 Revit 2026 plugin)
- ExtensibleStorage / Parameter bridge mapping Revit Rebar elements to HPKata rebar models.
- Rebar inspection, tagging, scheduling, 1D cutting-stock optimization, and export.

### Parallel Track: E2E Testing Track
- Test infrastructure and 4-tier requirement-driven opaque-box testing suite (`TEST_INFRA.md` -> `TEST_READY.md`).
- Final Milestone: 100% E2E test pass + Tier 5 Adversarial Coverage Hardening.
