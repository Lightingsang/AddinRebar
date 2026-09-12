# Task Assignment: auditor_m3_it2_1

## Role
M3 Iteration 2 Forensic Auditor (`teamwork_preview_auditor`)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m3_it2_1`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3_it2\handoff.md`
4. Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`
5. Core project: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar.Core\`

## Task
Conduct an exhaustive Forensic Integrity Re-Audit on Milestone M3:
- Inspect all files modified in Iteration 2 for cheating, hardcoding, dummy/facade implementations, or shortcuts.
- Verify that all 9 fixes represent genuine engineering logic.
- Confirm that `HPRebar.Core` continues to have ZERO references to `Autodesk.Revit.*`.
- Confirm zero deprecated Revit APIs across all newly authored/modified files.
- Verify `TransactionGroup("Beam Rebar")` atomicity.

## Deliverables
- Audit report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m3_it2_1\audit_report.md`
- Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m3_it2_1\handoff.md`
- Notify orchestrator with binary verdict: `CLEAN` or `INTEGRITY_VIOLATION`.
