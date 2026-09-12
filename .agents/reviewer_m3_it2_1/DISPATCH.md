## 2026-09-07T09:04:56Z

# Task Assignment: reviewer_m3_it2_1

## Role
M3 Iteration 2 Reviewer 1 (Architecture, Code Quality, Invariants)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_it2_1`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3_it2\handoff.md`
4. Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`

## Task
Conduct an independent code and architecture review of the remediated Milestone M3:
- Verify that all 9 fixes in worker handoff are correctly implemented.
- Verify that `BeamStackReader.cs` correctly calculates relative top elevation (`faces.Top.Origin.Z - originPoint.Z`).
- Verify that `BeamStackValidator.cs` contains `HasUniformWidth` validation rule.
- Verify that `BeamRebarOrchestrator.cs` correctly synchronizes section view counts per span dynamically.
- Verify adherence to feature folder convention, file-scoped namespaces (`namespace HPRebar.BeamRebar;`), and zero deprecated APIs.

## Deliverables
- Review report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_it2_1\review_report.md`
- Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_it2_1\handoff.md`
- Notify orchestrator with binary verdict: `APPROVE` or `REQUEST_CHANGES`.
