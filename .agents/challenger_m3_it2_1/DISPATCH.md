# Task Assignment: challenger_m3_it2_1

## Role
M3 Iteration 2 Challenger 1 (Geometry, Bounds & Support Stress Test)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m3_it2_1`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3_it2\handoff.md`
4. Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`

## Task
Stress-test and challenge the remediated geometry readers, support finding, and validation:
- Challenge cantilever beam configurations: confirm physical supports are NOT discarded and NO phantom columns are created at free cantilever ends.
- Challenge stepped-width beam stacks: confirm `BeamStackValidator.Validate` rejects stepped-width beams ($|b_i - b_0| > 1.0$ mm).
- Challenge flush secondary framing vs supporting girders: confirm secondary beams are NOT misclassified as girders.
- Challenge circular column width measurement: confirm non-zero width is returned via quadrant sampling or bounding box.

## Deliverables
- Challenge report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m3_it2_1\challenge_report.md`
- Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m3_it2_1\handoff.md`
- Notify orchestrator with binary verdict: `APPROVE` or `CHALLENGE_FAILED`.
