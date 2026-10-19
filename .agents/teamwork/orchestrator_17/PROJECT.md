# Project: HPKata Phases 2, 3, 4, and 5 Clean-Room Reconstruction

## Architecture
- **Ecosystem**: Independent deliverable `HPKata/HPKata.slnx` (.NET 8 SDK 10.0.300).
- **HPKata.Core**: `netstandard2.0;net8.0` host-neutral domain library with zero references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`. Encapsulates:
  - Phase 1 Baseline: 68 rebar shapes, config parser, XData serializer, cutting-stock optimization, ClosedXML export.
  - Phase 2 (Beam & ETABS): Multi-span continuous beam models, curtailment zones, foundation/inclined/curved beams, intersection reinforcement (`dam_giao`, `giao_dam`, `cat_dam`), ETABS/SAP2000 frame geometry & rebar import adapter (`EtabsBeamImportService`), beam rebar persistence and scheduling.
  - Phase 3 (Column & Wall): Multi-story column elevation & cross-section detailing (`ColumnStorySegment`, `ColumnSectionModel`, splice zones, crank/offset bends, confinement ties), shear wall & boundary element detailing (`ShearWallModel`, `WallBoundaryElementModel`, web mesh, edge hooks, `ve_loi_vach`), ETABS column/wall import.
  - Phase 4 (Foundation, Slab, Stair, Lintel): Pad footings (`IsolatedFootingModel`), pile caps & stepped caps (`PileCapModel`), two-way and polyline/4D slabs (`SlabPanelModel`), stair flights & landings (`StairFlightModel`), lintels (`LintelModel`), generic cross-sections.
  - Phase 5 (QS Takeoff & Drawing Utilities): Concrete/formwork quantity takeoff engine (`QsTakeoffEngine`, `QS_mbkc`, `QS_tru`, intersection deduction rules, tile grid layout, Excel export), sheet/drawing layout arrangers (`GridGenerator`, `DrawingSheetArranger`, viewport managers `taomv`, drawing index `danhmuc`).
- **HPKata.UI**: `net8.0-windows` WPF library with `CommunityToolkit.Mvvm` and `MahApps.Metro`, theme-aware (Dark/Light) dialogs for:
  - Beam Detailing & ETABS Import
  - Column & Shear Wall Detailing
  - Foundation, Slab & Stair Detailing
  - QS Takeoff & Drawing Utilities
- **HPKata.AutoCad**: .NET 8.0 AutoCAD 2026 add-in with `GoiLenh_HPKata.lsp`, registering all command methods and Lisp aliases across all 5 phases.
- **HPKata.Revit**: .NET 8.0 Revit 2026 add-in, bridging Revit elements (`OST_StructuralFraming`, `OST_StructuralColumns`, `OST_Walls`, `OST_StructuralFoundation`, `OST_Floors`, `OST_Stairs`) to `HPKata.Core` domain models, rebar generation, and QS export.
- **Dependency Rule**: `HPKata.AutoCad` -> `HPKata.Core` & `HPKata.UI`; `HPKata.Revit` -> `HPKata.Core` & `HPKata.UI`. `HPKata.Core` has ZERO host dependencies.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Continuous & Tie Beam Engine | Multi-span continuous beam models, support/midspan zones, tie/inclined/curved beams | M1 | Phase 2 R1 |
| 2 | Beam Intersections & Detailing | Intersection reinforcement (`dam_giao`, `giao_dam`, `cat_dam`), stirrups $L/4$, $L/2$, skin bars | M1 | Phase 2 R1 |
| 3 | ETABS Beam Import Service | Parsing frame geometry, forces, reinforcement areas $A_s$, rebar auto-selection | M1 | Phase 2 R1 |
| 4 | Beam UI & Dialogs | `BeamDetailingViewModel`, `EtabsImportViewModel` and WPF views | M1 | Phase 2 R1 |
| 5 | Beam CAD & Revit Commands | 25 AutoCAD commands & aliases (`vd`, `vds`, `vdm`, `vdx`, `vdc`, `damgiao`, `Etabs`, etc.) + Revit framing rebar | M1 | Phase 2 R1 |
| 6 | Column Elevation & Splice Engine | Multi-story column models, crank bends, lap splices, confinement ties $l_0$ | M2 | Phase 3 R2 |
| 7 | Shear Wall & Boundary Detailing | Wall web mesh, boundary elements (`ve_loi_vach`), edge hooks, openings | M2 | Phase 3 R2 |
| 8 | Column & Wall UI Dialogs | `ColumnDetailingViewModel`, `WallDetailingViewModel` and WPF views | M2 | Phase 3 R2 |
| 9 | Column & Wall CAD & Revit Commands | 10 AutoCAD commands (`vc`, `vcs`, `vv`, `vvs`, `vlv`, `loadcolumn`, `loadwall`, etc.) + Revit column/wall rebar | M2 | Phase 3 R2 |
| 10 | Footing & Pile Cap Detailing Engine | Pad footings, pile caps, stepped caps, foundation layout | M3 | Phase 4 R3 |
| 11 | Slab Reinforcement Engine | Two-way slabs, polyline slabs, 4D radial rebar, spacers, block takeoff | M3 | Phase 4 R3 |
| 12 | Stair Flight & Lintel Detailing | Stair flight and landing reinforcement, lintels, generic section cuts | M3 | Phase 4 R3 |
| 13 | Foundation/Slab/Stair UI Dialogs | `FoundationDetailingViewModel`, `SlabStairDetailingViewModel` and WPF views | M3 | Phase 4 R3 |
| 14 | Foundation/Slab/Stair CAD & Revit Commands | 15 AutoCAD commands (`vmbm`, `vmd`, `vmc`, `vesan`, `vethang`, `velanhto`, etc.) + Revit foundation/slab/stair | M3 | Phase 4 R3 |
| 15 | QS Quantity Takeoff Engine | Concrete, formwork, finish takeoff, intersection deductions, tile layout | M4 | Phase 5 R4 |
| 16 | Drawing Sheet & Viewport Arrangers | Grid generator, drawing sheet layout (`sap_ban_ve`), viewports (`taomv`), index | M4 | Phase 5 R4 |
| 17 | QS & Drawing Utility UI Dialogs | `QsTakeoffViewModel`, `DrawingUtilityViewModel` and WPF views | M4 | Phase 5 R4 |
| 18 | QS & Utility CAD & Revit Commands | 27 AutoCAD commands (`QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `vg`, `sbv`, `ibv`, etc.) + Revit QS export | M4 | Phase 5 R4 |
| 19 | Solution Integration & Clean Code Compliance | Full build, <=300 lines/file, <=50 lines/method, 0 dead code, 0 unannotated catch | M5 | Phase 5 R5 |
| 20 | E2E Testing Suite & Hardening | Complete xUnit/MTP suite expanding to >=650 tests, 100% green | M5 | Phase 5 AC6 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Phase 2 — Beam Shop Drawing & ETABS Integration | Features 1–5 (Core, UI, AutoCAD 25 cmds, Revit, Unit Tests) | Baseline (Phase 1) | DONE |
| M2 | Phase 3 — Column & Shear Wall Shop Drawing | Features 6–9 (Core, UI, AutoCAD 10 cmds, Revit, Unit Tests) | M1 | DONE |
| M3 | Phase 4 — Foundation, Slab, Stair & Lintel Detailing | Features 10–14 (Core, UI, AutoCAD 15 cmds, Revit, Unit Tests) | M2 | DONE |
| M4 | Phase 5 — Quantity Surveying (QS) & Drawing Utilities | Features 15–18 (Core, UI, AutoCAD 27 cmds, Revit, Unit Tests) | M3 | DONE |
| M5 | Full Solution Integration, Clean Code Audit & E2E Verification | Features 19–20 (Full solution build, clean code audit, >=650 tests) | M1, M2, M3, M4 | IN_PROGRESS |


## Interface Contracts
### HPKata.Core ↔ HPKata.AutoCad
- `BeamSpanModel`, `BeamRebarLayout`, `BeamSectionModel` provide geometry and rebar curves in metric mm.
- AutoCAD commands instantiate models, interact with DWG Editor/Transaction, and generate Polyline/XData/Block entities.
### HPKata.Core ↔ HPKata.Revit
- Revit commands map `Structural Framing` element geometry into `BeamSpanModel`.
- `RebarCreationService` uses curves and properties to generate Revit `Rebar` with `RebarBarType` mapping.
### HPKata.Core ↔ HPKata.UI
- ViewModels expose observable properties wrapping Core models using `CommunityToolkit.Mvvm`.

## Code Layout
- `HPKata/HPKata.Core/`:
  - `Beams/`: Beam models, detailing calculators, ETABS beam importer, intersection solvers.
  - `Columns/`: Column models, story transitions, crank bends, confinement ties.
  - `Walls/`: Shear wall models, boundary elements, web mesh.
  - `Foundations/`: Pad footings, pile caps, foundation layout.
  - `Slabs/`: Slab panel models, top/bottom mesh, polyline/radial layout.
  - `Stairs/`: Stair flight, landing, and lintel models.
  - `QuantitySurveying/`: QS takeoff engine, element records, deduction rules, Excel workbook generation.
  - `DrawingUtilities/`: Sheet arranger, grid generator, drawing index.
- `HPKata/HPKata.UI/`:
  - `ViewModels/`: `BeamDetailingViewModel`, `EtabsImportViewModel`, etc.
  - `Views/`: Matching WPF UserControls and Windows with ThemeDark/ThemeLight support.
- `HPKata/HPKata.AutoCad/`:
  - `Commands/`: Grouped by phase (`BeamCommands.cs`, `ColumnCommands.cs`, etc.).
  - `Lisp/`: `GoiLenh_HPKata.lsp`.
- `HPKata/HPKata.Revit/`:
  - `Commands/`: Framing, Column, Wall, Foundation, Slab, Stair, QS commands.
- `HPKata/HPKata.Core.Tests/`:
  - Unit tests for all domain models, calculators, and services.

## Clean Code Governance & Concurrency Rules
- Every C# file $\le 300$ physical lines (Rule C2).
- Every method $\le 50$ physical lines (Rule M3).
- 0 dead/commented-out code (Rule Q-B1).
- 0 unannotated empty catch blocks (Rule Q-B2).
- Host neutrality: `HPKata.Core` has ZERO AutoCAD or Revit references.
- Concurrency/MTP: NEVER weaken `Directory.Build.props`. Build and test strictly with `--disable-build-servers -m:1`.
