# Dispatch Assignment: Milestone 3 Reviewer 2 — 12 Embedded Seed Tools In-Depth Audit

## Role: Reviewer (teamwork_preview_reviewer)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m3_2`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3\handoff.md`
## Survey Reference: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_3\report.md`

---

## Review Scope
Review all 12 Embedded Seed Tools in `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/`:
1. Check that all 12 tools exist across the 6 categories:
   - `Model/get_model_info`
   - `Model/select_objects`
   - `Property/get_part_properties`
   - `Geometry/create_beam`
   - `Geometry/create_column`
   - `Geometry/create_contour_plate`
   - `Rebar/create_rebar_group`
   - `Rebar/create_single_rebar`
   - `Property/modify_user_properties`
   - `Rebar/get_reinforcement_info`
   - `Drawing/list_drawings`
   - `Export/export_ifc`
2. For each tool:
   - Check `tool.json`: `host: "tekla"`, `hostVersions: ["2025"]`, valid JSON schema, appropriate category, transaction mode, and timeout.
   - Check `examples.json`: contains at least 1-2 realistic, schema-valid examples.
   - Check `code.cs`:
     - Genuine Tekla Open API C# code.
     - Accurate `args` access (`args.Str`, `args.Int`, `args.Double`, `args.Bool`, `args.List`).
     - Safe error handling and clear return object (`return new { ... };`).
     - No prohibited namespaces (`System.Windows.Forms`, etc.) or blocked members (`model.CommitChanges()`, `MessageBox`, etc.).
3. Build verification: run `dotnet build HPTekla/HPTekla.Mcp.Server/HPTekla.Mcp.Server.csproj -c Release`.

## Verdict Requirement
Deliver an unambiguous verdict in your `handoff.md`:
- `APPROVE` if all 12 seeds meet quality, schema, and API standards.
- `REQUEST_CHANGES` if defects or discrepancies are found.
Send a message to the orchestrator upon completion.

## 2026-09-21T18:32:52Z
You are teamwork_preview_reviewer_m3_2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m3_2
Your dispatch assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m3_2\DISPATCH.md
The authoritative user request is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-21T17:20:33Z.
Worker handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3\handoff.md
Survey reference: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_3\report.md
Repo root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Perform an in-depth audit of all 12 Embedded Seed Tools in HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/ per your DISPATCH.md.
Check tool.json, examples.json, and code.cs across all categories.
Write your complete review to `.agents/teamwork_preview_reviewer_m3_2/report.md` and handoff to `.agents/teamwork_preview_reviewer_m3_2/handoff.md`.
Deliver an explicit APPROVE or REQUEST_CHANGES verdict.
Send a message to the orchestrator (caller) with your verdict and summary when done.
