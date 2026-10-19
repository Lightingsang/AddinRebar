# DISPATCH — 2026-10-17T09:27:00Z

## Task Assignment for reviewer_m2_1 (M2 Code Architecture & Clean Code Reviewer)

You are **reviewer_m2_1**, reviewing Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_1\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### MANDATORY REFERENCES:
1. `ORIGINAL_REQUEST.md` at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. Master Project Scope at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`.
3. Worker's Changes & Handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\handoff.md`

### YOUR MISSION:
Review the Milestone M2 implementation focusing on Code Architecture & Clean Code Governance:
1. Layer Isolation:
   - Verify `HPKata.Core` has ZERO references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`.
   - Verify all Column & Wall domain models, calculators, and ETABS mappers reside purely in `HPKata.Core`.
   - Verify `HPKata.UI` cleanly follows MVVM with `CommunityToolkit.Mvvm` and dynamic binding to `Theme.xaml`.
2. Clean Code Rules:
   - Rule C2: Measure every C# file added or touched in Phase 3 — must be <= 300 physical lines.
   - Rule M3: Measure methods in Phase 3 code — must be <= 50 physical lines.
   - Rule Q-B1: Zero dead/commented-out code or unaddressed TODOs/FIXMEs/HACKs.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Rule R5: Constant 304.8 must appear strictly and exclusively in `RevitUnitConverter.cs`.
3. Verdict:
   Provide an explicit verdict: **APPROVE** or **REQUEST_CHANGES**.

DELIVERABLE:
Write your review report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_1\review.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
