# Dispatch Assignment: Milestone 3 Reviewer (Iteration 2 Remediation)

## Role: Reviewer (teamwork_preview_reviewer)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m3_gen2`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Worker M3 Gen 2 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3_gen2\handoff.md`

---

## Review Scope
Verify the remediation of the 5 embedded seed tools in `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/`:
1. Verify `Drawing/list_drawings/code.cs`, `Model/get_model_info/code.cs`, `Model/select_objects/code.cs`, `Rebar/get_reinforcement_info/code.cs`, `Export/export_ifc/code.cs`.
2. Compile all 12 seed tools against Tekla Structures 2025.0 assemblies (`C:\Program Files\Tekla Structures\2025.0\bin`) under .NET Framework 4.8.
   Run:
   ```bash
   python .agents/teamwork_preview_reviewer_m3_2/compile_seeds_check.py
   ```
   Assert 12/12 pass with 0 errors.
3. Build `HPTekla.Mcp.Server`:
   ```bash
   dotnet build HPTekla/HPTekla.Mcp.Server/HPTekla.Mcp.Server.csproj -c Release
   ```
   Assert 0 warnings and 0 errors.
4. Verify stdio handshake reports 24 tools.

## Verdict Requirement
Deliver an unambiguous verdict in your `handoff.md`:
- `APPROVE` if all 12 seeds compile and the server builds cleanly.
- `REQUEST_CHANGES` if defects remain.
Send a message to the orchestrator upon completion.

## 2026-09-21T18:48:11Z
You are teamwork_preview_reviewer_m3_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m3_gen2
Your dispatch assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m3_gen2\DISPATCH.md
The authoritative user request is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-21T17:20:33Z.
Worker M3 Gen 2 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3_gen2\handoff.md
Repo root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Verify that the 5 seed tools were properly remediated per DISPATCH.md.
Compile all 12 seed tools against Tekla 2025 assemblies (`C:\Program Files\Tekla Structures\2025.0\bin`) using `python .agents/teamwork_preview_reviewer_m3_2/compile_seeds_check.py` or equivalent.
Build HPTekla.Mcp.Server and verify stdio surface.
Write report to `.agents/teamwork_preview_reviewer_m3_gen2/report.md` and handoff to `.agents/teamwork_preview_reviewer_m3_gen2/handoff.md`.
Deliver an unambiguous APPROVE or REQUEST_CHANGES verdict.
Send a message to the orchestrator (caller) when done.
