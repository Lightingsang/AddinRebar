# DISPATCH — worker_m1

Role: M1/M2 Core Domain & Test Suite Worker
Working Directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Context & Inputs
- Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
- Project Master Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
- Models Specification: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_1\models_plan.md`
- Calculators Specification: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2\calculators_plan.md`
- Unit Test Specifications: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3\test_spec_plan.md`
- Repository rules: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\AGENTS.md`

## Exclusive Write Ownership
You exclusively own and may create/edit files in:
- `HPRebar/HPRebar.Core/BeamRebar/`
- `HPRebar/HPRebar.Core.Tests/BeamRebar/`

You MUST NOT edit any other project files.

## Task
1. Implement all domain models in `HPRebar.Core/BeamRebar/Models/`:
   - `Point3.cs`, `Vector3.cs`, `Polyline3.cs`, `BarPolyline.cs`
   - `BeamSpan.cs`, `BeamSupportNode.cs`, `SecondaryBeamIntersection.cs`, `BeamContinuousStack.cs`
   - `BeamStirrupSpec.cs`, `StirrupZone.cs`, `StirrupRun.cs`, `BeamMainBarSpec.cs`, `BeamAdditionalBarSpec.cs`, `BeamSideBarSpec.cs`, `BeamSpecialBarSpec.cs`
   - `Enums.cs`, `ValidationResult.cs`
   Ensure `netstandard2.0` compatibility, file-scoped namespaces (`namespace HPRebar.Core.BeamRebar.Models;`), immutability (`readonly struct`, `sealed record`, `{ get; init; }`), and zero references to `Autodesk.Revit.*`.

2. Implement all domain calculators in `HPRebar.Core/BeamRebar/Calculators/`:
   - `Tolerance.cs`
   - `BeamStirrupDistributionCalculator.cs` (uniform, 3-zone L4/L3, MaxBarPositions=1002 check, centering slack)
   - `BeamMainBarCalculator.cs` (continuous top/bottom polylines, 90° hooks, 50% staggered splices > 11.7m, <1.0mm segment culling)
   - `BeamAdditionalBarCalculator.cs` (support top bars L/3, L/4; midspan bottom bars L/7; 2 vertical layers)
   - `BeamSideBarCalculator.cs` (skin bars for h >= 700mm, cross-ties with 90°/135° hooks)
   - `BeamSpecialBarCalculator.cs` (hanging stirrup cages @ 50mm, 45° diagonal ties)
   - `BeamCanvasTransformCalculator.cs` (screen coordinates conversion for WPF elevation & cross-section preview canvas)

3. Implement the comprehensive xUnit v3 unit test suite in `HPRebar.Core.Tests/BeamRebar/`:
   - `TestBeamData.cs` fixture builder
   - `BeamStirrupDistributionCalculatorTests.cs`
   - `BeamMainBarCalculatorTests.cs`
   - `BeamAdditionalBarCalculatorTests.cs`
   - `BeamSideBarCalculatorTests.cs`
   - `BeamSpecialBarCalculatorTests.cs`
   - `BeamCanvasTransformCalculatorTests.cs`
   - Cover all 4 tiers (Feature coverage, Boundary & Corner cases, Combinations, Real-world framing).

4. Verification:
   - Run `dotnet test HPRebar/HPRebar.Core.Tests` — verify 100% tests pass with 0 failures and 0 skipped.
   - Run `dotnet build HPRebar/HPRebar.Core/HPRebar.Core.csproj -c Release` — verify 0 warnings/errors.

5. Deliverable:
   - Write your handoff report to `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1\handoff.md`.
   - Notify orchestrator via send_message with test results and command outputs.

## 2026-09-07T07:41:52Z
You are worker_m1.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1
Read your task assignment and mandatory integrity warning at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read the models design specification at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_1\models_plan.md
Read the calculators design specification at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2\calculators_plan.md
Read the test suite specification at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3\test_spec_plan.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Implement:
1. All domain models in HPRebar.Core/BeamRebar/Models/
2. All domain calculators in HPRebar.Core/BeamRebar/Calculators/ and Tolerance.cs
3. All unit tests in HPRebar.Core.Tests/BeamRebar/
Verify by running:
`dotnet test HPRebar/HPRebar.Core.Tests`
`dotnet build HPRebar/HPRebar.Core/HPRebar.Core.csproj -c Release`
Write your handoff report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1\handoff.md
When finished, notify orchestrator via send_message.
