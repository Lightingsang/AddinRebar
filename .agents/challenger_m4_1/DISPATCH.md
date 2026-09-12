# Task Assignment: challenger_m4_1

## Role
Milestone M4 Challenger 1 (UI Parameter Validation & Binding Stress Test)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_1`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Architecture Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m4_1\ui_canvas_plan.md`
4. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4\handoff.md`
5. Target Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`

## Task
Stress-test and challenge the ViewModel state and parameter validation engine:
- Challenge `BeamRebarSession.Validate(out string error)` against edge cases:
  1. Bar count < 2 (e.g. 0, 1, negative).
  2. Negative or zero cover / stirrup spacing.
  3. Physical clearance violation: $2 \cdot Cover + 2 \cdot \phi_{stirrup} + \phi_{main} \ge \min(b, h)$.
  4. Stirrup set element limit: > 1002 stirrups per span.
- Verify that validation failures properly prevent runner execution and display clear user error messages.
- Verify two-way binding synchronization between `BeamRebarSession`, tab ViewModels, and `BeamRebarSpec`.

## Deliverables
- Challenge report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_1\challenge_report.md`
- Handoff report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_1\handoff.md`
- Notify orchestrator with binary verdict: `APPROVE` or `CHALLENGE_FAILED`.
