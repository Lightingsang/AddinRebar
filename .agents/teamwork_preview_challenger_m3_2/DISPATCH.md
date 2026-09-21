# Dispatch Assignment: Milestone 3 Challenger 2 — Seed Schema Validation & AST Guard Compliance

## Role: Challenger (teamwork_preview_challenger)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_2`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3\handoff.md`

---

## Challenge Scope
Adversarially challenge the 12 embedded seed tools in `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/`:
1. Schema & Examples Validation:
   - For all 12 tools, parse `tool.json` and validate against JSON Schema draft-07 rules.
   - For all 12 tools, parse `examples.json` and validate that every example payload validates cleanly against the tool's `inputSchema` (no missing required fields, types match).
2. AST Guard & Syntax Compliance:
   - Parse each `code.cs` using Roslyn CSharpSyntaxTree.
   - Verify every script ends with a return statement (`return <expression>;`).
   - Check against `GuardProfile.Tekla`:
     - Assert NO occurrences of `MessageBox`, `System.Windows.Forms`, `Process.Start`, `RunMacro`, or `ShowDialog`.
     - Assert NO direct calls to `CommitChanges` (which is reserved for the bridge transaction manager).
     - Assert NO `#r` or `#load` directives.
3. Cross-Host Contamination Check:
   - Ensure zero mentions of Revit (`Autodesk.Revit`), AutoCAD (`Autodesk.AutoCAD`), Navisworks (`Autodesk.Navisworks`), ETABS (`ETABSv1`), SAP2000, Robot, PowerBI, or Excel in the seed tools.
4. Parameter Extraction Consistency:
   - Check that all `args.X("paramName")` in `code.cs` match property names declared in `tool.json`.

## Verdict Requirement
Deliver an unambiguous verdict in your `handoff.md`:
- `APPROVE` if all seeds pass validation.
- `REQUEST_CHANGES` if any violations, schema mismatches, or guard failures are detected.
Send a message to the orchestrator upon completion.

## 2026-09-21T18:32:52Z
You are teamwork_preview_challenger_m3_2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_2
Your dispatch assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_2\DISPATCH.md
The authoritative user request is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-21T17:20:33Z.
Worker handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3\handoff.md
Repo root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Adversarially challenge the 12 embedded seed tools for schema validity, example consistency, and AST guard compliance per your DISPATCH.md.
Parse tool.json, validate examples.json against schema, parse code.cs with Roslyn syntax tree to check return statements and forbidden guards (MessageBox, Forms, Process, CommitChanges, #r, #load), and check for cross-host contamination.
Write your findings to `.agents/teamwork_preview_challenger_m3_2/report.md` and handoff to `.agents/teamwork_preview_challenger_m3_2/handoff.md`.
Deliver an explicit APPROVE or REQUEST_CHANGES verdict.
Send a message to the orchestrator (caller) with your verdict and summary when done.
