# DISPATCH — worker_m2_column_wall (Milestone M2: Phase 3 Column & Shear Wall Detailing + Biaxial P-M-M Engine)

You are `worker_m2_column_wall`, an expert C# domain implementation worker (`teamwork_preview_worker`).
Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\`
Authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-17T22:07:16Z`).
Master project scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_21\PROJECT.md`.
Specification blueprint: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\handoff.md`.
Codebase survey: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\handoff.md`.
Test architecture survey: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\handoff.md`.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership
You exclusively own:
- `HPDcePro/src/HPDcePro.Core/Calculations/Column/*` (`ColumnPmmModels.cs`, `ColumnPmmInteractionCalculator.cs`)
- `HPDcePro/src/HPDcePro.Core/Calculations/Wall/*` (`ShearWallModels.cs`, `ShearWallCalculator.cs`)
- `HPDcePro/src/HPDcePro.Core/Drafting/ColumnDetailingGenerator.cs` (and supporting detailing options)
- `HPDcePro/src/HPDcePro.UI/ViewModels/ColumnWallDetailingViewModel.cs`
- `HPDcePro/src/HPDcePro.UI/Views/ColumnWallDetailingWindow.xaml` and `.xaml.cs`
- `HPDcePro/src/HPDcePro.AutoCad/Commands/ColumnCommands.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/ColumnPmmTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/ShearWallTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/ColumnDetailingTests.cs`
- `HPDcePro/tests/HPDcePro.Adapters.Tests/ColumnWallUiTests.cs`

Do NOT modify `C:\Dce Pro` (strictly read-only!).

## Implementation Requirements for Milestone M2 (Phase 3)
1. **`ColumnPmmInteractionCalculator`** in `HPDcePro.Core`:
   - TCVN 5574:2018 non-linear fiber method / discrete slice method.
   - Computes $N$-$M_x$-$M_y$ interaction curve, slenderness buckling magnification $\eta = \frac{1}{1 - N/N_{cr}} \ge 1.0$.
   - Computes design moments $M_{xd} = \eta_x M_x$, $M_{yd} = \eta_y M_y$.
   - Evaluates utilization ratio $UR$ (Bresler contour or fiber capacity) and pass/fail safety.
2. **`ColumnDetailingGenerator`** in `HPDcePro.Core`:
   - Multi-story column schedule generation into `CadDraftingBuffer`.
   - Splice zone arrangement in low-moment middle third ($H_n/3..2H_n/3$).
   - 1:6 cranked bend transitions between stories (slope $\le 1:6$; if offset requires slope $> 1:6$, terminate lower bars and create starter dowels).
   - Dense confinement zones $H_c = \max(H_n/6, b, h, 500\text{ mm})$ with tie pitch $s \le \min(100, 10d)$.
   - Rectangular single/multi-tie cross-sections (`ColumnREC1`, `ColumnREC2`) and circular cross-sections (`ColumnCi`).
3. **`ShearWallCalculator`** in `HPDcePro.Core`:
   - Core shear wall boundary elements (cột đầu vách) sizing: $l_c \ge 0.15 L_w$, rebar ratio $\mu_v \ge 0.5\%$.
   - Double-curtain web reinforcement ($\mu \ge 0.25\%$, horizontal/vertical bars, cross-tie density $\ge 4\text{/m}^2$).
   - Shear check $V_u \le 0.3 R_{bt} b h_0 + 0.8 R_{sw} \frac{A_{sw}}{s} h_0$.
4. **`ColumnWallDetailingWindow` & ViewModel** in `HPDcePro.UI`:
   - Built with `MahApps.Metro` `MetroWindow`, Segoe UI font, and `ThemeManager` Dark/Light palette switching.
   - Reactive ViewModel with `CalculatePmmCommand`, `CalculateWallCommand`, and `ExportCadCommand`.
   - Visual preview for P-M-M interaction surface and cross-section rebar layout.
5. **AutoCAD Commands Upgrade** in `HPDcePro.AutoCad`:
   - Upgrade `ColumnCommands.cs`: `VeCotDcePro`, `VeVachDcePro`, `ColumnREC1`, `ColumnREC2`, `ColumnCi`, `ImportKichThuocCot`, `ImportHinhDangCot`, `UpdateThepTinhToan` to execute real domain workflows.
6. **Test Suite Expansion**:
   - Create `ColumnPmmTests.cs` ($\ge 7$ tests), `ShearWallTests.cs` ($\ge 6$ tests), `ColumnDetailingTests.cs` ($\ge 5$ tests) in `HPDcePro.Core.Tests`.
   - Create `ColumnWallUiTests.cs` ($\ge 4$ tests) in `HPDcePro.Adapters.Tests` with STA thread isolation (`[StaFact]` or `RunOnStaThread`).
   - Add at least 22+ new automated tests (bringing solution total to at least 172+ passing tests).
   - Verify `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` compiles with 0 errors and 0 warnings (`TreatWarningsAsErrors=true`).
   - Clean Code compliance: files $\le 300$ lines, methods $\le 50$ lines, 0 commented code, 0 unannotated catches.

Output:
Write a detailed handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\handoff.md`.

## 2026-10-17T22:37:22Z
You are worker_m2_column_wall, working in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\.
Read your dispatch file at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\DISPATCH.md and ORIGINAL_REQUEST.md.
Implement Milestone M2 (Phase 3): ColumnPmmInteractionCalculator (TCVN 5574:2018 N-Mx-My biaxial, slenderness eta), ColumnDetailingGenerator (splices in Hn/3..2Hn/3, 1:6 cranked, REC1/REC2/Ci), ShearWallCalculator (boundary elements, web rebar), ColumnWallDetailingWindow and ViewModel in HPDcePro.UI (MahApps.Metro), upgrade ColumnCommands in HPDcePro.AutoCad, and create comprehensive tests in ColumnPmmTests.cs, ShearWallTests.cs, ColumnDetailingTests.cs, and ColumnWallUiTests.cs.
Build and verify via dotnet build and dotnet test (--disable-build-servers -m:1). Verify TreatWarningsAsErrors=true (0 errors, 0 warnings) and all files <= 300 lines, methods <= 50 lines.
Write your detailed report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\handoff.md and send a completion message back to orchestrator_21.
