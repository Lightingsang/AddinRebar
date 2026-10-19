# Changes — Milestone M3 (Phase 4 Substructure, Two-Way Slab, Stairs & Lintel)

## Summary of Changes
Implemented Phase 4 Substructure (Pile Cap & Pad Footing), Two-Way Slab, Stairs & Lintel Detailing across HPDcePro.Core, HPDcePro.UI, HPDcePro.AutoCad, HPDcePro.Core.Tests, and HPDcePro.Adapters.Tests.

### 1. Domain Calculations (`HPDcePro.Core/Calculations/`)
- `Foundation/PileCapModels.cs` (140 lines): Data models for 1-N pile cap configurations (`PileCapInput`, `PileCapResult`, `PileReactionResult`, `PileCoordinateScheduleItem`, `PileLayoutPattern`).
- `Foundation/PileCapCalculator.cs` (236 lines):
  - Pile reaction distribution under combined axial load + biaxial bending: $P_i = \frac{N_{total}}{n} \pm \frac{M_y x_i}{\sum x_j^2} \pm \frac{M_x y_i}{\sum y_j^2}$.
  - Column 45° pyramid punching shear check ($0.75 R_{bt} u_{pm} h_0$).
  - Corner pile punching shear and one-way (beam) shear.
  - Flexural rebar $A_{sx}, A_{sy}$ from cantilever bending moments at column face.
  - Symmetrical standard pile coordinate generator for 1, 2, 3, 4, 5, 6 piles.
  - Automatic `XuatToaDoCoc` schedule generator (`CoordinateSchedule`).
- `Foundation/PadFootingModels.cs` (67 lines): Data models for isolated spread footings (`PadFootingInput`, `PadFootingResult`).
- `Foundation/PadFootingCalculator.cs` (131 lines):
  - Soil pressure distribution ($p_{max}, p_{min}, p_{avg}$).
  - Uplift separation detection ($e > L/6$ or $e > B/6$) with triangular contact redistribution.
  - Column 45° pyramid punching shear calculation with net soil pressure deduction.
  - Cantilever flexural reinforcement $A_{sx}, A_{sy}$ at column faces.
- `Slab/TwoWaySlabModels.cs` (94 lines): Data models for slab panels (`TwoWaySlabInput`, `TwoWaySlabResult`, `SlabRebarLayer`, `SlabBoundaryCondition`).
- `Slab/TwoWaySlabCalculator.cs` (137 lines):
  - Aspect ratio $\lambda = L_y / L_x$.
  - Marcus / Wood-Armer load-sharing coefficients for two-way slabs ($\lambda \le 2.0$).
  - Fallback to one-way beam strip equations when $\lambda > 2.0$.
  - Positive span moments and negative support moments based on edge boundary conditions.
  - Continuous bottom mesh and support top negative rebar cutoffs at $L_0/4$.
  - Spacer chair bars (`con_ke` at $1\text{/m}^2$) height calculation.
  - Span-to-depth deflection safety checks.
- `Secondary/StaircaseModels.cs` (65 lines): Data models for staircases (`StaircaseInput`, `StaircaseResult`, `StaircaseType`).
- `Secondary/StaircaseCalculator.cs` (87 lines):
  - Waist slab (`EngineBanThang`) and folded plate zig-zag (`EngineZigZac`) calculations.
  - Ergonomic Blondel check $2h_r + b_t \in [600, 640]\text{ mm}$.
  - Dead load (waist slab, steps, finishes) and live load moment $M = q L_0^2 / 10$ or $/9$.
  - Cranked joint bars at landing junction with lap length $\ge 35d$.
- `Secondary/LintelModels.cs` (54 lines): Data models for lintels over wall openings (`LintelInput`, `LintelResult`).
- `Secondary/LintelCalculator.cs` (98 lines):
  - Openings $800-3200\text{ mm}$ with bearing length $\ge 200\text{ mm}$.
  - Equivalent arching load if wall height above $> L_0 / 3$.
  - Design bending moment and shear capacity check ($V_b + V_{sw}$).
  - Main bars ($2\phi 10 - 2\phi 16$) and stirrups ($\phi 6 a150$).

### 2. Drafting Primitives (`HPDcePro.Core/Drafting/`)
- `CadDraftingBuffer.cs`: Added optional `xdata` parameter to `AddCircle`.
- `FoundationDetailingGenerator.cs` (275 lines):
  - Generates 2D vector CAD primitives with attached XData:
    * Pile caps with `NHAN_XDATA_COC`, `XDATA_MONG_COC`, `RebarXdataDCE`, `DceRebarXDataTKT`, and coordinate schedule table.
    * Pad footings with plan boundary, pedestal, and bottom reinforcement mesh.
    * Slab panels with top rebar ($L_0/4$ cutoff), bottom continuous mesh, and chair markers.
    * Staircase elevations with steps, landings, and cranked joint bars tagged with `SYS_XData_ThangMoi`.
    * Lintel elevations and cross-sections.

### 3. UI Presentation (`HPDcePro.UI/`)
- `ViewModels/FoundationSlabViewModel.cs` (222 lines): Reactive MVVM ViewModel for Pile Cap, Pad Footing, Slab, and Staircase & Lintel.
- `Views/FoundationSlabWindow.xaml` (354 lines) and `.xaml.cs` (28 lines): `MahApps.Metro` `MetroWindow` with 4 animated tabs and Segoe UI theming.

### 4. AutoCAD Commands Upgrade (`HPDcePro.AutoCad/Commands/`)
- `FootingCommands.cs` (204 lines): Upgraded all 12 footing commands (`VeBlockCoc`, `DanhTenCoc`, `XuatToaDoCoc`, `StripMong`, `TextDaiMong`, `VeMongCoc_DCE`, `VeMongDon_DCE`, `TMCoc`, `RaiDaiCoc`, `VeBlockLanhTo`, `VeMatCatLanhTo`, `VeThang_DCE`) to execute real calculations and CAD rendering.
- `SlabCommands.cs` (168 lines): Upgraded all 7 slab commands (`VeThepSanTopX`, `VeThepSanTopY`, `SoHieuThepSan`, `VeThepSanBotX`, `VeThepSanBotY`, `ThongKeThepSan`, `ChiaKhoangCach`) to generate real CAD rebar polylines and schedule tables.

### 5. Test Suite Expansion
- `HPDcePro.Core.Tests/PileCapTests.cs` (136 lines): 8 tests covering symmetrical/eccentric pile reactions, column & corner punching, flexure, layouts, overload states, and CAD XData.
- `HPDcePro.Core.Tests/PadFootingTests.cs` (107 lines): 7 tests covering concentric/eccentric pressure, uplift detection, punching shear, cantilever flexure, overload failure, and CAD drafting.
- `HPDcePro.Core.Tests/TwoWaySlabTests.cs` (109 lines): 7 tests covering Marcus moments, one-way fallback ($\lambda > 2.0$), boundary conditions, rebar layers/cutoffs, spacer chairs, deflection checks, and CAD drafting.
- `HPDcePro.Core.Tests/StaircaseAndLintelTests.cs` (123 lines): 7 tests covering waist slab, zig-zag folded plate, Blondel formula, cranked bar lap $\ge 35d$, lintel moments, inadequate bearing detection, shear checks, and CAD drafting.
- `HPDcePro.Adapters.Tests/FoundationSlabUiTests.cs` (172 lines): 6 tests covering STA window instantiation, ThemeManager switching, ViewModel calculations for all 4 modules, and command execution.
