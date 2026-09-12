# DISPATCH — reviewer_m1_2

Role: Independent Code & Test Reviewer 2
Working Directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m1_2

## Context & Inputs
- Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
- Master Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
- Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1\handoff.md`
- Code under review:
  - `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar.Core\BeamRebar\`
  - `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar.Core.Tests\BeamRebar\`

## Task
1. Perform an independent, adversarial code review of the M1 and M2 deliverables.
2. Verify:
   - Numerical precision and tolerances in `Tolerance.cs` and all calculators.
   - Guardrails against Revit COM exceptions (1002 position limit, short segments).
   - Test suite coverage and assertions rigor in `HPRebar.Core.Tests/BeamRebar/`.
   - Build health across project targets.
3. Build and test verification:
   - Run `dotnet test HPRebar/HPRebar.Core.Tests` and report test summary.
   - Run `dotnet build HPRebar/HPRebar.Core/HPRebar.Core.csproj -c Debug` and report warnings/errors.
4. Issue a clear verdict: `APPROVE` or `REQUEST_CHANGES`.

## Output
Write your review report to `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m1_2\review_report.md` and `handoff.md`.
Notify orchestrator via send_message with verdict.

## 2026-09-07T07:50:54Z
You are reviewer_m1_2.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m1_2
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m1_2\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read worker_m1 handoff at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1\handoff.md

Review HPRebar.Core/BeamRebar/ and HPRebar.Core.Tests/BeamRebar/.
Run:
`dotnet test HPRebar/HPRebar.Core.Tests`
`dotnet build HPRebar/HPRebar.Core/HPRebar.Core.csproj -c Debug`
Write your review report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m1_2\review_report.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m1_2\handoff.md
Notify orchestrator via send_message with your verdict (APPROVE or REQUEST_CHANGES).
