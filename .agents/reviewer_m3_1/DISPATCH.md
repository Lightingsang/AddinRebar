# Dispatch: reviewer_m3_1 — Milestone M3 Reviewer 1

## Mission
Conduct an independent code and architecture review of the Continuous Beam Rebar Revit Add-In implementation in `HPRebar/HPRebar/Beam Rebar/` and `HPRebar/HPRebar/Application.cs`.

## Focus Areas
1. Feature folder convention in AGENTS.md (models in `Models/`, views in `View/`, view models in `View Models/`, root files at feature root).
2. Explicit file-scoped namespaces (`namespace HPRebar.BeamRebar;`, `namespace HPRebar.BeamRebar.Models;`, etc.).
3. Revit API deprecation rules: zero `DisplayUnitType`, zero `CreateFreeForm`, correct `#if REVIT2024_OR_GREATER` for element IDs.
4. Transaction safety: `BeamRebarOrchestrator` master `TransactionGroup("Beam Rebar")` with auto-rollback and assimilation.
5. Error handling: `RebarFailureHandling` (`SwallowWarnings : IFailuresPreprocessor`), user cancel handling.
6. Ribbon integration in `HPRebar/HPRebar/Application.cs`.

## Inputs
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3\handoff.md`
4. Target Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`

## Deliverables
- Detailed review report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_1\review_report.md`
- Self-contained handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_1\handoff.md`
- Notify orchestrator via `send_message` with your verdict (APPROVE or REQUEST_CHANGES).

## 2026-09-07T08:42:50Z
You are reviewer_m3_1.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_1
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_1\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read the worker handoff at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3\handoff.md
Read the target codebase at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\
Read ribbon registration in: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Application.cs

Conduct an independent code and architecture review:
- Verify feature folder convention in AGENTS.md.
- Verify explicit file-scoped namespaces (`namespace HPRebar.BeamRebar;`, `namespace HPRebar.BeamRebar.Models;`, etc.).
- Verify zero deprecated APIs (no DisplayUnitType, no CreateFreeForm, #if REVIT2024_OR_GREATER for elementId.Value).
- Verify master TransactionGroup("Beam Rebar") atomicity, auto-rollback on error/cancel, and SwallowWarnings failure preprocessor.
- Verify ribbon button registration.

Write your review report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_1\review_report.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_1\handoff.md
Notify orchestrator via send_message with your verdict (APPROVE or REQUEST_CHANGES).

