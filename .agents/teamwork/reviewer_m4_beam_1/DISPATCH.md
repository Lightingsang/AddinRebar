# Dispatch: reviewer_m4_beam_1 (teamwork_preview_reviewer)

## Identity & Parent
- Identity: `reviewer_m4_beam_1` (teamwork_preview_reviewer)
- Working Directory: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_beam_1\`
- Parent: `orchestrator_3` (Conversation ID: `4db7c650-146a-4a05-a1e1-c0fb57e2733b`)

## Inputs to Review
- Authoritative Request: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (section `## 2026-10-07T22:29:51Z`)
- Project Scope: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Worker Handoff: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_beam\handoff.md`
- Target Code: `HPRebar/HPRebar.Core/Tcvn5574/` and `HPRebar/HPRebar.Core.Tests/Tcvn5574/`

## Mission
Review Milestone M4 (TCVN 5574:2018 Engine & Tests):
1. Verify TCVN 5574:2018 calculation logic:
   - Flexure: Singly reinforced ($A_s$), doubly reinforced ($A_s, A_s'$), boundary depth $\xi_R, \alpha_R$, minimum ratio $\mu_{min}=0.1\%$.
   - Shear: Web crush limit $Q \le 0.3 R_b b h_0$, concrete capacity $Q_b$, stirrup spacing $s \le s_{max}$.
   - Clear spacing: $t \ge \max(25\text{mm}, d)$ bottom, $t \ge \max(30\text{mm}, d)$ top per Clause 10.3.1.2.
2. Architecture verification: strictly ZERO references to `Autodesk.Revit.*` and `ClosedXML` in `HPRebar.Core` (Rule D1).
3. Clean Code verification: all files $\le 300$ lines, all methods $\le 50$ lines.
4. Test execution: `dotnet test HPRebar.Core.Tests` 100% PASS.
5. Render verdict: `APPROVE` or `REQUEST_CHANGES`.

## Output
Write report to: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_beam_1\handoff.md`.
Notify orchestrator_3 via `send_message`.

## 2026-10-07T23:40:44Z
You are reviewer_m4_beam_1 (teamwork_preview_reviewer).
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_beam_1\
Your parent is orchestrator_3 (conversation ID: 4db7c650-146a-4a05-a1e1-c0fb57e2733b).

Read your dispatch file first:
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_beam_1\DISPATCH.md
Read worker handoff: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_beam\handoff.md.

Review Milestone M4 (TCVN 5574:2018 Engine & Tests):
- Verify TCVN 5574:2018 calculation logic (flexure, shear, clear spacing).
- Verify Rule D1 (strictly 0 references to Autodesk.Revit.* and ClosedXML in HPRebar.Core).
- Verify Clean Code (<=300 lines/file, <=50 lines/method).
- Run dotnet test HPRebar.Core.Tests and confirm 100% PASS.
- Render verdict: APPROVE or REQUEST_CHANGES.
Write report to: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_beam_1\handoff.md.
When done, report to orchestrator_3 via send_message.
