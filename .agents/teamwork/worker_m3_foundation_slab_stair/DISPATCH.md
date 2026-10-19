# DISPATCH — worker_m3_foundation_slab_stair (Milestone M3: Phase 4 Substructure, Two-Way Slab, Stairs & Lintel)

You are `worker_m3_foundation_slab_stair`, an expert C# domain implementation worker (`teamwork_preview_worker`).
Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation_slab_stair\`
Authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-17T22:07:16Z`).
Master project scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_21\PROJECT.md`.
Specification blueprint: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\handoff.md`.
Codebase survey: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\handoff.md`.
Test architecture survey: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\handoff.md`.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership
You exclusively own:
- `HPDcePro/src/HPDcePro.Core/Calculations/Foundation/*` (`PileCapModels.cs`, `PileCapCalculator.cs`, `PadFootingModels.cs`, `PadFootingCalculator.cs`)
- `HPDcePro/src/HPDcePro.Core/Calculations/Slab/*` (`TwoWaySlabModels.cs`, `TwoWaySlabCalculator.cs`)
- `HPDcePro/src/HPDcePro.Core/Calculations/Secondary/*` (`StaircaseModels.cs`, `StaircaseCalculator.cs`, `LintelModels.cs`, `LintelCalculator.cs`)
- `HPDcePro/src/HPDcePro.Core/Drafting/FoundationDetailingGenerator.cs`
- `HPDcePro/src/HPDcePro.UI/ViewModels/FoundationSlabViewModel.cs`
- `HPDcePro/src/HPDcePro.UI/Views/FoundationSlabWindow.xaml` and `.xaml.cs`
- `HPDcePro/src/HPDcePro.AutoCad/Commands/FootingCommands.cs`
- `HPDcePro/src/HPDcePro.AutoCad/Commands/SlabCommands.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/PileCapTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/PadFootingTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/TwoWaySlabTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/StaircaseAndLintelTests.cs`
- `HPDcePro/tests/HPDcePro.Adapters.Tests/FoundationSlabUiTests.cs`

Do NOT modify `C:\Dce Pro` (strictly read-only!).

## Implementation Requirements for Milestone M3 (Phase 4)
1. **`PileCapCalculator`** in `HPDcePro.Core`:
   - 1 to N pile cap layouts (single pile, 2-pile, 3-pile triangular, 4-pile rectangular, 5-pile dice, etc.).
   - Individual pile reactions under axial force + biaxial moment: $P_i = \frac{N}{n} \pm \frac{M_y x_i}{\sum x_j^2} \pm \frac{M_x y_i}{\sum y_j^2}$.
   - Column 45° pyramid punching shear check ($P_{punch} \le 0.75 R_{bt} b_{pm} h_0$).
   - Corner pile punching shear and one-way shear.
   - Flexural rebar $A_{sx}, A_{sy}$ from cantilever bending at column face.
   - Pile coordinate schedule generation `XuatToaDoCoc` (`CoordinateSchedule`).
2. **`PadFootingCalculator`** in `HPDcePro.Core`:
   - Isolated spread footing on soil: soil pressure distribution ($p_{max}, p_{min}$, $p_{max} \le 1.2 R_{tc}$).
   - Uplift check ($e = M/N \le L/6$).
   - Column punching shear along 45° pyramid.
   - Two-way flexural reinforcement $A_{sx}, A_{sy}$.
3. **`TwoWaySlabCalculator`** in `HPDcePro.Core`:
   - Two-way slab panel moment calculation ($\lambda \le 2.0$) via Marcus / Wood-Armer coefficients.
   - Continuous bottom mesh $A_{sx,bot}, A_{sy,bot}$.
   - Support top negative reinforcement cutoff at $L_0/4$ (or $L_0/3$).
   - Spacer chair bars (`con_ke` at $1\text{/m}^2$).
   - Fallback to one-way beam strip equations if $\lambda > 2.0$.
4. **`StaircaseCalculator` & `LintelCalculator`** in `HPDcePro.Core`:
   - Staircase: waist slab (`EngineBanThang`) and folded plate (`EngineZigZac`).
   - Tread/riser formula $2h_r + b_t \in [600, 640]\text{ mm}$.
   - Cranked joint bars at landing junction with lap length $\ge 35d$.
   - Lintel: cast-in-place / precast lintel over openings ($800-3200\text{ mm}$, bearing length $\ge 200\text{ mm}$), main bars $2\phi 10-2\phi 14$, stirrups $\phi 6 a150$.
5. **`FoundationDetailingGenerator`** in `HPDcePro.Core`:
   - Generates plan & cross-section drafting primitives for pile caps, pad footings, slab rebar mats, and staircase elevations into `CadDraftingBuffer` with attached XData packets (`RebarXdataDCE`, `DceRebarXDataTKT`, `NHAN_XDATA_COC`, `XDATA_MONG_COC`).
6. **`FoundationSlabWindow` & ViewModel** in `HPDcePro.UI`:
   - `MahApps.Metro` `MetroWindow` with tabs for Pile Cap, Pad Footing, Slab, and Staircase.
   - Segoe UI font, `ThemeManager` Dark/Light palette switching, reactive ViewModel with calculation and CAD export commands.
7. **AutoCAD Commands Upgrade** in `HPDcePro.AutoCad`:
   - Upgrade all commands in `FootingCommands.cs` (`VeBlockCoc`, `DanhTenCoc`, `XuatToaDoCoc`, `VeMongCoc_DCE`, `VeMongDon_DCE`, `VeThang_DCE`, `VeBlockLanhTo`, `VeMatCatLanhTo`, `RaiDaiCoc`).
   - Upgrade all commands in `SlabCommands.cs` (`VeThepSanTopX`, `VeThepSanTopY`, `SoHieuThepSan`, `VeThepSanBotX`, `VeThepSanBotY`, `ThongKeThepSan`, `ChiaKhoangCach`).
8. **Test Suite Expansion**:
   - Write comprehensive tests across `PileCapTests.cs` ($\ge 7$), `PadFootingTests.cs` ($\ge 6$), `TwoWaySlabTests.cs` ($\ge 6$), `StaircaseAndLintelTests.cs` ($\ge 6$), and `FoundationSlabUiTests.cs` ($\ge 5$).
   - Add at least 30+ new automated tests (bringing total passing tests to $\ge 207$).
   - Verify `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` (0 warnings, 0 errors, `TreatWarningsAsErrors=true`).
   - Clean Code compliance: files $\le 300$ lines, methods $\le 50$ lines, 0 commented code, 0 unannotated catches.

Output:
Write a detailed handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation_slab_stair\handoff.md`.
Document exact build and test execution results.
Send a completion message back to orchestrator_21 when finished.

## 2026-10-17T22:51:24Z
[Message] timestamp=2026-10-17T22:51:24Z sender=9dd160f7-2d30-4389-a7bc-64cd7b359ca0 priority=MESSAGE_PRIORITY_HIGH content=You are worker_m3_foundation_slab_stair, working in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation_slab_stair\.
Read your dispatch file at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation_slab_stair\DISPATCH.md and ORIGINAL_REQUEST.md.
Implement Milestone M3 (Phase 4): PileCapCalculator (1 to N piles, XuatToaDoCoc schedule), PadFootingCalculator, TwoWaySlabCalculator, StaircaseCalculator, LintelCalculator, FoundationDetailingGenerator, FoundationSlabWindow and FoundationSlabViewModel in HPDcePro.UI (MahApps.Metro), upgrade FootingCommands and SlabCommands in HPDcePro.AutoCad, and create comprehensive tests in PileCapTests.cs, PadFootingTests.cs, TwoWaySlabTests.cs, StaircaseAndLintelTests.cs, and FoundationSlabUiTests.cs.
Build and verify via dotnet build and dotnet test (--disable-build-servers -m:1). Verify TreatWarningsAsErrors=true (0 errors, 0 warnings) and all files <= 300 lines, methods <= 50 lines.
Write your detailed report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation_slab_stair\handoff.md and send a completion message back to orchestrator_21.
