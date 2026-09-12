# DISPATCH — spec_miner_m1_3

Role: M1 Test Suite Specification Miner
Working Directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3

## Context & Inputs
- Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
- Project Master Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
- Survey Analysis: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1\survey_source_analysis.md`
- Target Reference: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar.Core.Tests\ColumnRebar\`

## Task
Design the unit test specifications for verifying `HPRebar.Core/BeamRebar/` under xUnit v3:
1. Identify test cases across all 4 tiers:
   - Tier 1: Feature coverage (uniform stirrup, 3-zone L4 stirrup, 3-zone L3 stirrup, continuous top bars, continuous bottom bars, top support bars, bottom midspan bars, side bars, hanging ties, canvas transform).
   - Tier 2: Boundary & Corner cases (zero/negative spans, single span, multi-span (2, 3, 5 spans), cantilevers on left/right/both, step changes in beam depth, link beams with short span, max bar positions > 1002 exception).
   - Tier 3: Combinations (continuous beams with varying heights and widths across spans, deep beams with side bars + 3-zone stirrups, cantilever + interior span).
   - Tier 4: Real-world structural framing cases (standard 3-span continuous frame with exterior columns, secondary beam intersection).
2. Specify test class structure matching `HPRebar.Core.Tests/ColumnRebar/` patterns:
   - `TestBeamData.cs` providing builder methods for typical continuous beams.
   - Exact assert criteria (point coordinates, counts, spacings, total bar lengths).
3. Ensure 100% test pass rate criteria under `dotnet test HPRebar.Core.Tests`.

## Output
Write report to `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3\test_spec_plan.md` and `handoff.md`.
Notify orchestrator via send_message.

## 2026-09-07T07:35:36Z
You are spec_miner_m1_3.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read project master plan at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md

Design the comprehensive unit test specifications for verifying HPRebar.Core/BeamRebar/ under xUnit v3 (fixtures, 4-tier test cases, boundary conditions, edge cases, assertion precision).
Write your report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3\test_spec_plan.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3\handoff.md
When finished, notify orchestrator via send_message.
