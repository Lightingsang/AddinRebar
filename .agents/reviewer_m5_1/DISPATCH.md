# DISPATCH — reviewer_m5_1

## Mission
You are Reviewer 1 for Milestone M5 (Ribbon Integration, Multi-Version Compliance, and Architectural Standards).

## Mandatory Reading
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Repository Rules: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\AGENTS.md`
4. Worker M5 Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m5\handoff.md`
5. Codebase under review:
   - `HPRebar/HPRebar/Application.cs`
   - `HPRebar/HPRebar/Beam Rebar/BeamRebarCommand.cs`
   - `HPRebar/HPRebar/Beam Rebar/ThemeSwitcher.cs`
   - `HPRebar/HPRebar/Beam Rebar/RevitUnits.cs`
   - `HPRebar/HPRebar/Beam Rebar/`

## Review Objectives
- Verify that `Application.cs` creates/uses the "Rebar" panel under the "HPRebar" tab and registers the "Beam Rebar" push button with correct icon resource URIs.
- Verify that `BeamRebarCommand` derives from `ExternalCommand` with `[Transaction(TransactionMode.Manual)]`.
- Verify multi-version compilation compliance for both Revit 2025 (`Debug.R25`) and Revit 2026 (`Debug.R26`).
- Verify zero deprecated APIs across `HPRebar/HPRebar/Beam Rebar/` (no `DisplayUnitType`, no `IntegerValue`, modern ForgeTypeId `UnitTypeId.Millimeters`).
- Verify strict adherence to repository rules: file-scoped namespaces, PascalCase, no collateral file changes.

## Output
Write your review report to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m5_1\review_report.md`
Write your handoff to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m5_1\handoff.md`
Notify orchestrator via `send_message` with your verdict: **APPROVE** or **REQUEST_CHANGES**.

## 2026-09-07T10:22:35Z
You are reviewer_m5_1.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m5_1
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m5_1\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read the worker handoff at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m5\handoff.md
Read the target codebase at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\

Conduct an independent review of Ribbon Integration and Multi-Version Compliance:
- Verify Application.cs ribbon button registration ("Beam Rebar" on panel "Rebar", icon URIs).
- Verify BeamRebarCommand.cs derives from ExternalCommand with [Transaction(TransactionMode.Manual)].
- Verify multi-version compilation compliance for Revit 2025 and 2026.
- Verify zero deprecated APIs (no DisplayUnitType, no IntegerValue, UnitTypeId.Millimeters used).
- Verify file-scoped namespaces and feature folder conventions.

Write your review report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m5_1\review_report.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m5_1\handoff.md
Notify orchestrator via send_message with your verdict (APPROVE or REQUEST_CHANGES).

