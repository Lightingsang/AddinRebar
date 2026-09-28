# Dispatch: Worker M2 (Core Rebar Geometry & Distribution Calculator)

- **Role**: teamwork_preview_worker
- **Milestone**: M2 - Core Rebar Geometry Calculator
- **Scope**:
  1. Implement `KataRebarCalculator.cs` in `HPRebar/HPRebar.Core/KataRebar/Calculators/`:
     - Method `KataRebarLayoutResult Calculate(KataBeamRebarSpec spec)`
     - Top & bottom continuous main bars:
       - Station $X$: spans from start support face to end support face.
       - Exterior anchorage: 90° hook legs bent downwards (top bars) or upwards (bottom bars).
       - Cantilever spans (support width = 0 or console): top bars continue to cantilever end with 90° hook, bottom bars stop at interior face.
       - Transverse $Y$ spacing centered across beam width $b$ with cover $c_{stirrup} + d_{stirrup}$.
     - Support top additional bars (extra layers 1-4):
       - Multi-layer vertical $Z$ offsets (Layer 1 below continuous, Layer 2 below Layer 1, etc.).
       - Cutoff length: $L_{\text{cutoff}} = \max(L_{\text{left}}, L_{\text{right}}) \times \text{ratio}$ (e.g. $L/3$ or $L/4$ or sheet settings).
       - Exterior support: 90° hook down into column.
       - Interior support: straight bar extending into adjacent spans.
     - Span bottom additional bars (extra layers 1-2):
       - Clear span cutoffs relative to clear faces ($L/7$ or sheet settings).
       - Centered across beam width.
     - Side bars / web skin reinforcement:
       - If $h \ge 700\text{ mm}$ (or sheet specifies side bars in row 20): place symmetrical pairs along vertical web with vertical spacing $\le 300\text{ mm}$.
     - 3-zone stirrups distribution:
       - For each span, calculate dense zones at support ends ($L_n/4$, spacing $s_1$, start offset 50 mm) and sparse zone at midspan ($s_2$).
       - Closed hoop (□), cap stirrups (U), and cross-ties (C) per sheet specifications (rows 25-27).
       - Ensure `Polyline3.Simplify(1.0)` is applied to all generated polylines to protect against Revit short-curve crashes.
  2. Implement unit tests in `HPRebar/HPRebar.Core.Tests/KataRebar/KataRebarCalculatorTests.cs` verifying:
     - Single-span beam calculation
     - Multi-span continuous beam calculation (e.g., 3-span or Kata golden sample B01)
     - Cantilever overhang beam calculation
     - Multi-layer top additional bars
     - Deep beam ($h = 1100\text{ mm}$) side bars
     - 3-zone stirrup counts, spacing, and bounds
  3. Verify:
     - `dotnet build HPRebar/HPRebar.Core/HPRebar.Core.csproj`
     - `dotnet test HPRebar/HPRebar.Core.Tests`
     - 100% green tests with 0 failures and 0 skipped.
- **Reference**:
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\report.md`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\PROJECT.md`
  - Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (under ## 2026-09-27T15:57:37Z)
- **Constraint**: `HPRebar.Core` must remain pure `netstandard2.0` with 0 references to `Autodesk.Revit.*`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## 2026-09-27T16:21:57Z
You are worker_m2 (Core Rebar Geometry & Distribution Calculator Implementer).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2
Read your dispatch instructions at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-27T15:57:37Z.
Read the architectural master plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\PROJECT.md
Read survey report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\report.md
Read Milestone 1 worker handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1\handoff.md

Your Task:
Implement Milestone 2 & 3 (Geometry Calculator & Test Suite):
1. Create `KataRebarCalculator.cs` in `HPRebar/HPRebar.Core/KataRebar/Calculators/`:
   - Method `public static KataRebarLayoutResult Calculate(KataBeamRebarSpec spec)`
   - Continuous main bars: top and bottom main bars running across the full beam length, with 90° hooks at exterior support ends ($L_{hook} = \min(h - 2c - 2d_{stirrup}, \max(30d, 200\text{ mm}))$), correctly handling cantilever overhangs.
   - Additional top bars (support extra bars): multi-layer offsets ($Z$), cutoff extensions into spans ($L_{cutoff} = \max(L_{left}, L_{right}) \times \text{ratio}$, with $L/3$, $L/4$, or sheet ratio), 90° hooks at exterior columns.
   - Additional bottom bars (midspan extra bars): clear span cutoffs ($L_n/7$ or sheet settings), centered across width.
   - Side bars: if $h \ge 700\text{ mm}$ or row 20 specified, symmetrical pairs along vertical web with vertical spacing $\le 300\text{ mm}$.
   - 3-zone stirrup distribution: support dense zone ($L_n/4$, spacing $s_1$, 50mm offset) and midspan sparse zone ($s_2$) for closed hoops (□), cap stirrups (U), and cross ties (C).
   - Ensure `Polyline3.Simplify(1.0)` is applied to all generated polylines.
2. Create comprehensive xUnit unit tests in `HPRebar/HPRebar.Core.Tests/KataRebar/KataRebarCalculatorTests.cs`:
   - Single-span beam calculation
   - Multi-span continuous beam (B01 Kata sample)
   - Cantilever overhang beam
   - Multi-layer top additional bars
   - Deep beam side bars
   - 3-zone stirrup distribution bounds and spacing
3. Run verification:
   - `dotnet build HPRebar/HPRebar.Core/HPRebar.Core.csproj`
   - `dotnet test HPRebar/HPRebar.Core.Tests`
   - Ensure 100% green pass with 0 failures and 0 skipped.
