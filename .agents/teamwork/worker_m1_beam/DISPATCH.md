# DISPATCH — worker_m1_beam (Milestone M1: Phase 2 Beam Detailing, Drafting Primitives & ETABS Bridge)

You are `worker_m1_beam`, an expert C# domain implementation worker (`teamwork_preview_worker`).
Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_beam\`
Authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-17T22:07:16Z`).
Master project scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_21\PROJECT.md`.
Specification blueprint: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\handoff.md`.
Codebase survey: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\handoff.md`.
Test architecture survey: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\handoff.md`.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership
You exclusively own:
- `HPDcePro/src/HPDcePro.Core/Calculations/Beam/*` (`ContinuousBeamCalculator.cs`, `FoundationBeamCalculator.cs`, `CurvedBeamCalculator.cs`, `BeamTrimmerCalculator.cs`, and supporting data models)
- `HPDcePro/src/HPDcePro.Core/Drafting/*` (`CadEntityType.cs`, `CadColor.cs`, `CadEntityPrimitive.cs`, `CadDraftingBuffer.cs`, `BeamShopDrawingGenerator.cs`, `BeamDetailingOptions.cs`)
- `HPDcePro/src/HPDcePro.Core/Etabs/*` (`EtabsDataModels.cs`, `EtabsModelSynchronizer.cs`)
- `HPDcePro/src/HPDcePro.AutoCad/Drafting/*` (`CadDraftingBufferRenderer.cs`)
- `HPDcePro/src/HPDcePro.AutoCad/Commands/BeamCommands.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/ContinuousBeamTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/BeamShopDrawingTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/EtabsSyncTests.cs`
- `HPDcePro/tests/HPDcePro.Adapters.Tests/BeamCommandsExecutionTests.cs` (or extend existing `AutoCadCommandsTests.cs`)

Do NOT modify `C:\Dce Pro` (strictly read-only!).

## Implementation Requirements for Milestone M1 (Phase 2)
1. **`ContinuousBeamCalculator`** in `HPDcePro.Core`:
   - Multi-span moment redistribution ($\le 20\%$, clamped for unequal spans $L_{long}/L_{short} > 2.0$).
   - Top and bottom continuous main bars.
   - Top negative support extra bars cutoff at $\max(L_{0,left}, L_{0,right})/4 + L_{anchorage}$ (or $L_0/3$ if long/short ratio $> 2.0$).
   - Bottom positive span extra bars cutoff at $0.15 L_0$ from support faces.
   - 11.7m commercial rebar lap splice rules: staggered in low-stress zones (top bars spliced at mid-span $L_0/3..2L_0/3$; bottom bars spliced at supports $\le L_0/4$; lap length $L_{lap} = 30d..40d$; stagger $\ge 1.3 L_{lap}$).
   - Dense stirrup zones ($L_0/4$ near supports at pitch $a100..a150$, mid-span at pitch $a200$).
2. **`FoundationBeamCalculator`** in `HPDcePro.Core`:
   - Inverted moment diagram for foundation/ground tie beams (upward soil pressure, downward column loads).
   - Heavier concrete cover ($35-50\text{ mm}$).
   - Side skin bars ($2\phi 14$ at vertical pitch $\le 300\text{ mm}$ for $h \ge 700\text{ mm}$).
3. **`CurvedBeamCalculator`** in `HPDcePro.Core`:
   - Circular arc and non-prismatic beam chord discretization.
   - Combined flexure, shear, and torsion ($M_u, V_u, T_u$).
   - Closed stirrups $A_{sw1}/s$ and perimeter longitudinal torsion bars $A_{sl}$.
4. **`BeamTrimmerCalculator`** (`BeamTrimmerV2` / `dam_giao`):
   - Concentrated hanger reinforcement $A_{sh} = F_v (1 - h_s/h_m)/R_{sw}$ and localized stirrup pairs at beam-beam intersections.
5. **Host-Independent 2D CAD Drafting Buffer** in `HPDcePro.Core`:
   - `CadDraftingBuffer`, `CadEntityPrimitive`, `CadEntityType` (`Line`, `Polyline`, `Circle`, `Hatch`, `Dimension`, `Text`, `BlockInsert`).
   - Attached `XDataPacket`s support (`RebarXdataDCE`, `DceRebarXDataTKT`, `Links_XDataObject`).
   - Supports 2D affine transforms (`TransformBy(dx, dy, scale, rotationRad)`).
6. **`BeamShopDrawingGenerator`** in `HPDcePro.Core`:
   - Generates complete longitudinal elevation (1:25) and cross-sections (1-1, 2-2) into `CadDraftingBuffer`.
   - Each rebar polyline carries genuine `RebarXdataDCE` metadata (`o(0)..o(11)`).
7. **`EtabsModelSynchronizer`** in `HPDcePro.Core`:
   - Bidirectional exchange of stories, grids, frame sections, and internal forces/reinforcement ($A_s$).
   - Spatial tolerance matching $\delta \le 200\text{ mm}$.
8. **AutoCAD Renderer & Command Upgrade** in `HPDcePro.AutoCad`:
   - `CadDraftingBufferRenderer`: Draws `CadDraftingBuffer` entities and writes XData inside an active AutoCAD transaction.
   - Upgrade `BeamCommands.cs`: `VeDam_DCE`, `VeDamAll_DCE`, `VeMCD_DCE`, `VeBlockDam2`..`VeBlockSection`, `ImportKichThuoc` execute genuine domain logic.
9. **Test Suite Expansion**:
   - Write comprehensive tests in `ContinuousBeamTests.cs`, `BeamShopDrawingTests.cs`, and `EtabsSyncTests.cs` (adding at least 25+ new tests).
   - Verify that all new and baseline tests pass 100% (at least 145+ total tests passing).
   - Ensure `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` compiles with 0 errors and 0 warnings (`TreatWarningsAsErrors=true`).
   - Ensure all files satisfy Clean Code: $\le 300$ lines per file, methods $\le 50$ lines, 0 commented-out code, 0 unannotated catches.

Output:
Write a detailed handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_beam\handoff.md`.
Document exact build and test execution results.
Send a completion message back to orchestrator_21 when finished.

## 2026-10-17T22:22:53Z
You are worker_m1_beam, working in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_beam\.
Read your dispatch file at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_beam\DISPATCH.md and ORIGINAL_REQUEST.md.
Implement Milestone M1 (Phase 2): ContinuousBeamCalculator, FoundationBeamCalculator, CurvedBeamCalculator, BeamTrimmerCalculator, CadDraftingBuffer, BeamShopDrawingGenerator, EtabsModelSynchronizer, CadDraftingBufferRenderer, upgrade BeamCommands in HPDcePro.AutoCad, and create comprehensive tests in ContinuousBeamTests.cs, BeamShopDrawingTests.cs, and EtabsSyncTests.cs.
Build and verify tests via dotnet build and dotnet test (--disable-build-servers -m:1). Verify TreatWarningsAsErrors=true (0 errors, 0 warnings) and all files <= 300 lines, methods <= 50 lines.
Write your detailed report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_beam\handoff.md and send a completion message back to orchestrator_21.
