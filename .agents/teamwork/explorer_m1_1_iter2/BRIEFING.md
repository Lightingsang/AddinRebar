# BRIEFING — 2026-10-13T05:56:30Z

## Mission
Propose exact production-ready C# fix in `generate_rebar_curves/code.cs` to resolve ArgumentsInconsistentException on zero-length/sub-tolerance curve segments while keeping clean code compliance (C2, M3, ADR-0007).

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_1_iter2\
- Original parent: b578e01d-ac14-4013-9369-951a857a2c11
- Milestone: M1 Iteration 2

## 🔒 Key Constraints
- Read-only investigation — do NOT implement in production code
- Propose exact, production-ready C# fix in `handoff.md`
- Ensure distance between start/end is evaluated before calling `Line.CreateBound`
- Return graceful error message: `Rebar curve set {setIndex} contains a zero-length segment at index {segIdx}.`
- Clean Code: file <= 300 lines (Rule C2), methods <= 50 lines (Rule M3), ADR-0007 compliant
- All handoff and reporting via `handoff.md` and `send_message`

## Current Parent
- Conversation ID: b578e01d-ac14-4013-9369-951a857a2c11
- Updated: 2026-10-13T05:55:38Z

## Investigation State
- **Explored paths**:
  - `auditor_m1_1_gen2/handoff.md` (Forensic Audit report)
  - `reviewer_m1_1_gen2/handoff.md` (Reviewer 1 report)
  - `ORIGINAL_REQUEST.md` (Milestone M1 requirements N1-N5)
  - `HPGrasshopper/.../generate_rebar_curves/code.cs` (Production seed script)
  - `HPGrasshopper.McpBridge.Tests/.../ChallengerM1N1N2AdversarialTests.cs` (Adversarial test suite)
  - `HPGrasshopper.Mcp.Server.Tests/.../ScriptQualityTests.cs` & `AdversarialSeedStressTests.cs`
  - `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptQuality.cs` (ADR-0007 Walker)
- **Key findings**:
  - `ParseSegments` calls `Line.CreateBound` without pre-checking distance, causing Revit `ArgumentsInconsistentException` for distance < 0.001 ft.
  - Pre-validation loop at lines 129-135 is unreachable for zero-length segments.
  - In `ChallengerM1N1N2AdversarialTests.cs`, `SimulateRebarExecution` replicated this defect and masked it with `Assert.Throws`.
  - Fix design: `ParseSegments` returns `(List<Autodesk.Revit.DB.Curve>? Segments, int ZeroLengthIndex)`, validating `startPoint.DistanceTo(endPoint) < 0.001` before `Line.CreateBound`.
  - Compliance: 0 warnings/errors in ADR-0007 ScriptQuality, file length ~205 lines (<= 300), method length 24 lines (<= 50), 0 McpShared modifications.
- **Unexplored areas**: None for M1 N1 scope.

## Key Decisions Made
- Design `ParseSegments` to return tuple `(List<Autodesk.Revit.DB.Curve>? Segments, int ZeroLengthIndex)`: clean, non-vague parameter names, no bool parameters, separation of concerns.
- Mirror the fix in `ChallengerM1N1N2AdversarialTests.cs:SimulateRebarExecution` and update `N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated` to assert graceful rejection.

## Artifact Index
- DISPATCH.md — Dispatch log
- BRIEFING.md — Working memory index
- progress.md — Liveness heartbeat
- handoff.md — 5-component handoff report
