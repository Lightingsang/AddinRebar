## 2026-10-17T23:22:09Z
You are worker_m5_final, working in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_final\.
Read your dispatch file at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_final\DISPATCH.md and ORIGINAL_REQUEST.md.
Implement Milestone M5 (Phase 6): PreGableFrameCalculator, SteelMomentConnectionCalculator (TCVN 5575:2024), AnchorBoltBasePlateCalculator, SteelConnectionWindow and SteelConnectionViewModel in HPDcePro.UI (MahApps.Metro), upgrade all 70 AutoCAD commands across all command classes, implement RevitRebarPlacementService in HPDcePro.Revit, and create comprehensive tests in PreGableFrameTests.cs, SteelMomentConnectionTests.cs, AnchorBoltBasePlateTests.cs, SteelConnectionUiTests.cs, RevitRebarPlacementTests.cs, and AutoCadFullCommandsTests.cs.
Build and verify via dotnet build and dotnet test (--disable-build-servers -m:1). Verify TreatWarningsAsErrors=true (0 errors, 0 warnings) and all files <= 300 lines, methods <= 50 lines.
Write your detailed report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_final\handoff.md and send a completion message back to orchestrator_21.

# DISPATCH — worker_m5_final (Milestone M5: Phase 6 Steel Connections, Full CAD Commands & Revit Rebar Integration)

You are `worker_m5_final`, an expert C# domain implementation worker (`teamwork_preview_worker`).
Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_final\`
Authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-17T22:07:16Z`).
Master project scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_21\PROJECT.md`.
Specification blueprint: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\handoff.md`.
Codebase survey: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\handoff.md`.
Test architecture survey: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\handoff.md`.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership
You exclusively own:
- `HPDcePro/src/HPDcePro.Core/Calculations/Steel/*` (`PreGableFrameModels.cs`, `PreGableFrameCalculator.cs`, `SteelMomentConnectionModels.cs`, `SteelMomentConnectionCalculator.cs`, `AnchorBoltBasePlateModels.cs`, `AnchorBoltBasePlateCalculator.cs`)
- `HPDcePro/src/HPDcePro.UI/ViewModels/SteelConnectionViewModel.cs`
- `HPDcePro/src/HPDcePro.UI/Views/SteelConnectionWindow.xaml` and `.xaml.cs`
- `HPDcePro/src/HPDcePro.AutoCad/Commands/*` (upgrade all remaining commands across all command classes so all 70 commands execute real workflows)
- `HPDcePro/src/HPDcePro.Revit/RevitRebarPlacementService.cs` and supporting descriptors
- `HPDcePro/tests/HPDcePro.Core.Tests/PreGableFrameTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/SteelMomentConnectionTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/AnchorBoltBasePlateTests.cs`
- `HPDcePro/tests/HPDcePro.Adapters.Tests/SteelConnectionUiTests.cs`
- `HPDcePro/tests/HPDcePro.Adapters.Tests/RevitRebarPlacementTests.cs`
- `HPDcePro/tests/HPDcePro.Adapters.Tests/AutoCadFullCommandsTests.cs`

Do NOT modify `C:\Dce Pro` (strictly read-only!).

## Implementation Requirements for Milestone M5 (Phase 6)
1. **`PreGableFrameCalculator`** in `HPDcePro.Core`:
   - Portal frame centerline geometry, rafter pitch, eave and apex coordinates.
   - Tapered built-up I-sections $h(x) = h_{start} + (h_{end} - h_{start}) \cdot (x/L)$.
   - Purlin layout with spacing $1.2-1.5\text{ m}$.
2. **`SteelMomentConnectionCalculator`** in `HPDcePro.Core`:
   - TCVN 5575:2024 bolted extended end-plate moment connection:
     - Yield-line theory for required end plate thickness $t_p$.
     - High-strength bolt tension check with prying force $Q$ (Grade 8.8, 10.9).
     - Bolt shear and tension interaction: $(N_{bt} / B_t)^2 + (V_{bt} / V_b)^2 \le 1.0$.
     - Weld sizing for beam flange and web.
     - Column web panel shear and transverse stiffener requirements.
3. **`AnchorBoltBasePlateCalculator`** in `HPDcePro.Core`:
   - Pedestal concrete bearing stress $\sigma_c \le 1.5 R_b$.
   - Cantilever plate thickness $t_{bp}$.
   - Anchor bolt tension, diameter, and embedment $L_{embed} \ge 25 d_b$.
4. **`SteelConnectionWindow` & ViewModel** in `HPDcePro.UI`:
   - `MahApps.Metro` `MetroWindow` with tabs for Moment Connection, Base Plate, and Gable Frame.
   - Segoe UI font, `ThemeManager` Dark/Light switching, reactive ViewModel with calculation and CAD export commands.
5. **Full AutoCAD Commands Upgrade** in `HPDcePro.AutoCad`:
   - Upgrade every remaining command handler across all command files (`BeamCommands`, `ColumnCommands`, `SlabCommands`, `FootingCommands`, `RebarCommands`, `AutoStrCommands`) so all 70 commands execute real domain calculation, drafting buffer generation, or UI dialogs.
6. **`RevitRebarPlacementService`** in `HPDcePro.Revit`:
   - Maps `HPDcePro.Core` detailing models (beam, column, pile cap) to Revit structural framing and rebar descriptors (`RevitRebarPlacementResult`, `RevitRebarDescriptor`, `RevitHookDescriptor`).
   - Converts coordinates between mm and feet ($1\text{ ft} = 304.8\text{ mm}$).
   - Implements idempotent rebar replacement logic by element mark comments.
7. **Test Suite Expansion**:
   - Write comprehensive tests across `PreGableFrameTests.cs` ($\ge 6$), `SteelMomentConnectionTests.cs` ($\ge 7$), `AnchorBoltBasePlateTests.cs` ($\ge 6$), `SteelConnectionUiTests.cs` ($\ge 5$), `RevitRebarPlacementTests.cs` ($\ge 6$), and `AutoCadFullCommandsTests.cs` ($\ge 4$).
   - Add at least 30+ new automated tests (bringing total solution tests to at least 270+ passing tests).
   - Verify `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` (0 warnings, 0 errors, `TreatWarningsAsErrors=true`).
   - Clean Code compliance: files $\le 300$ lines, methods $\le 50$ lines, 0 commented code, 0 unannotated catches.

Output:
Write a detailed handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_final\handoff.md`.
Document exact build and test execution results.
Send a completion message back to orchestrator_21 when finished.
