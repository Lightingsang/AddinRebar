# Project: HPKata Dual-Host Structural Rebar Suite

## Architecture
- **Ecosystem**: Independent deliverable `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300).
- **HPKata.Core**: `netstandard2.0;net8.0` host-neutral domain library with zero references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`. Encapsulates 68 rebar shapes (85 catalog entries), hook/bend/lap calculations, 86 configuration parameters, 2-way XData / Block Attribute serialization, schedule aggregation, coupler tracking, QR code payload generation, 1D cutting-stock optimization (`to_hop_thep`), and headless Excel export via `ClosedXML`.
- **HPKata.UI**: `net8.0-windows` WPF library with `CommunityToolkit.Mvvm` and `MahApps.Metro`, providing host-theme-aware (Dark/Light) dialogs for Setup, Drawing & Editing, and Rebar Scheduling / Cutting Stock.
- **HPKata.AutoCad**: .NET 8.0 AutoCAD 2026 add-in with `GoiLenh_HPKata.lsp`, implementing Phase 1 commands (`vt`, `sua`, `link`, `ud`, `tkt`, `tohopthep`, `toexcel`, etc.) interacting with DWG polylines, XData RegApp `"KATA"`, blocks, and dimension leaders.
- **HPKata.Revit**: .NET 8.0 Revit 2026 add-in, bridging Revit `Rebar` elements to `HPKata.Core` domain models, 68 shape codes, schedule aggregation, 1D cutting stock, and export.
- **Dependency Rule**: `HPKata.AutoCad` -> `HPKata.Core` & `HPKata.UI`; `HPKata.Revit` -> `HPKata.Core` & `HPKata.UI`. `HPKata.Core` has ZERO host dependencies.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Reverse-Engineering Blueprint | Comprehensive technical blueprint in `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` | M1 | Survey 1, 2, 3 |
| 2 | 86-Parameter Configuration Engine | Full parser & serializer for KataPro 86 config parameters from `Goi lenh.lsp` lines 152–237 | M2 | Survey 1 |
| 3 | 68 Standard Rebar Shapes Catalog | Mathematical catalog & geometry evaluation for shapes `dang00` to `dang67` + 16 variants | M2 | Survey 2 |
| 4 | Hook, Bend & Lap Calculator | Calculation of 90°, 135°, 180° hooks, bend deductions, lap splices, and anchorage lengths | M2 | Survey 2 |
| 5 | KataPro XData 2-Way Serializer | RegApp `"KATA"`, DxfCodes (1000/1070/1005), handle links, and XRecord serializer | M2 | Survey 2 |
| 6 | Block & Attribute Dictionary Mapper | DWG Block attributes (`Iso_kata.dwg`) bidirectional reader/writer | M2 | Survey 2 |
| 7 | Schedule & Coupler Aggregator | Detailing schedule aggregation (`thong_ke_thep`), diameter summary (`tong_hop_thep`), and coupler schedule (`thong_ke_coupler`) | M2 | Survey 3 |
| 8 | 1D Cutting-Stock Engine | 11,700 mm stock bar optimization (`to_hop_thep`), 5 heuristics (`Auto`, `DuBang0`, `LapDayCanhDai`, `BeamSearchTopK`, `PatternCat`), offcut warehouse management, scrap tracking | M2 | Survey 3 |
| 9 | QR Code Tag Payload Generator | Generates pipe-delimited QR payload with ECC Level Q for shop tags (`in_tem_thep`) | M2 | Survey 3 |
| 10 | Headless Excel Exporter | ClosedXML-based exporter producing `Kata.xlsm` / `Iso_QHPlus.xls` compatible workbooks | M2 | Survey 3 |
| 11 | Unit Test Suite (>=100 tests) | 100% green tests in `HPKata.Core.Tests` covering all 68 shapes, config, XData, cutting stock, and Excel | M3 | R3 / AC3 |
| 12 | Host-Theme-Aware WPF Setup Dialog | Setup UI (`setupthep` / `setupkata`) with dark/light theme tracking | M4 | Survey 1, R2 |
| 13 | Rebar Drawing & Editing UI | Interactive WPF dialogs for rebar parameters, shapes, and hooks (`ve_thep` / `sua_thep`) | M4 | Survey 1, R2 |
| 14 | Scheduling & Cutting Stock UI | Dialogs for rebar schedules, cutting stock solver configuration, and offcut review | M4 | Survey 3, R2 |
| 15 | AutoCAD 2026 Rebar Detailing Commands | Creation/editing commands (`setupthep`, `vt`, `taothep`, `nhan`, `sua`, `smd`, `dai90`, `dai135`, `dai180`, `dai90135`, `neo`, `cut`, `cattuyy`, `dimthep`) | M5 | Survey 1, R3 |
| 16 | Dynamic Rebar ↔ Tag Linking Commands | Dynamic linking commands (`link`, `cl`, `dl`, `tim`, `xoa`, `ud`, `gt`, `stt0`, `stt1`, `timthep`, `check_lap`) | M5 | Survey 1, 2, R3 |
| 17 | AutoCAD Scheduling & Export Commands | Scheduling commands (`tkt`, `suatk`, `sstk`, `ththep`, `tkco`, `tohopthep`, `toexcel`, `toexcelqh`, `itt`) | M5 | Survey 1, 3, R3 |
| 18 | AutoCAD Lisp Loader & Aliases | `GoiLenh_HPKata.lsp` companion file with all command shortcuts | M5 | Survey 1, R3 |
| 19 | Revit 2026 Rebar Parameter Bridge | Mapping between Revit `Rebar` elements and `HPKata.Core` models (`REBAR_PARTITION = "{ten_ck}"`) | M6 | Survey 3, R3 |
| 20 | Revit Rebar Tagging & Scheduling | Inspection, mark assignment, schedule generation, and 1D cutting stock export in Revit 2026 | M6 | Survey 3, R3 |
| 21 | E2E Requirement-Driven Testing Suite | 4-tier opaque-box test suite (`TEST_INFRA.md` -> `TEST_READY.md`) | M7 | Dual Track |
| 22 | Tier 5 Adversarial Coverage Hardening | White-box adversarial testing, edge-case saturation, and victory audit | M7 | Dual Track |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Reverse-Engineering Blueprint | Feature 1 (Full Blueprint in docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md) | Survey done | DONE |
| M2 | HPKata.Core Domain Library | Features 2, 3, 4, 5, 6, 7, 8, 9, 10 (Host-neutral C# library) | M1 | PLANNED |
| M3 | HPKata.Core.Tests & Cutting Stock Verification | Feature 11 (>=100 unit tests, 100% pass) | M2 | PLANNED |
| M4 | HPKata.UI Library | Features 12, 13, 14 (Theme-aware WPF dialogs) | M2 | PLANNED |
| M5 | HPKata.AutoCad 2026 & Lisp Loader | Features 15, 16, 17, 18 (AutoCAD 2026 add-in & Lisp) | M2, M4 | PLANNED |
| M6 | HPKata.Revit 2026 Add-in | Features 19, 20 (Revit 2026 add-in) | M2, M4 | PLANNED |
| M7 | E2E Testing Track & Final Verification | Features 21, 22 (Dual Track E2E + Tier 5 Hardening + Audit) | M3, M5, M6 | PLANNED |

## Code Layout
- `docs/hpkata/`: Master technical blueprints and schemas (`HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`).
- `HPKata/HPKata.slnx`: Solution configuration (.NET 8 SDK 10.0.300).
- `HPKata/HPKata.Core/`: `netstandard2.0;net8.0` host-neutral domain core.
  - `Configuration/`: 86-parameter config parser, serializer, default settings.
  - `Shapes/`: 68 rebar shape definitions (85 catalog entries), geometry equations, parameter models.
  - `Calculators/`: Hook, bend deduction, lap splice, anchorage, unit weight calculators.
  - `Serialization/`: KataPro XData RegApp `"KATA"`, DxfCodes, block attributes.
  - `Scheduling/`: Rebar schedule aggregation, diameter grouping, coupler schedule.
  - `Optimization/`: 1D cutting stock engine (`to_hop_thep`), heuristics, offcut warehouse.
  - `Tagging/`: QR code payload generation with ECC Level Q.
  - `Export/`: Headless Excel export (`ClosedXML`).
- `HPKata/HPKata.Core.Tests/`: `net8.0` xUnit tests for core functionality.
- `HPKata/HPKata.UI/`: `net8.0-windows` WPF UI library (`CommunityToolkit.Mvvm`, `MahApps.Metro`).
  - `Views/`: Setup, Detailing, Scheduling dialogs.
  - `ViewModels/`: MVVM ViewModels with reactive property binding.
- `HPKata/HPKata.AutoCad/`: `net8.0-windows` AutoCAD 2026 plugin.
  - `Commands/`: Setup, Detailing, Linking, Scheduling commands.
  - `Interop/`: AutoCAD DWG XData, Block, and Polyline synchronization.
- `HPKata/HPKata.Revit/`: `net8.0-windows` Revit 2026 plugin.
  - `Commands/`: Tag, Schedule, Cutting-stock commands.
  - `Interop/`: Revit Rebar to HPKata Rebar adapter.
