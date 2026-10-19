# Dispatch: reviewer_m4_1

## Role & Mission
You are `reviewer_m4_1`, reviewing Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities) focusing on Code Architecture & Clean Code Governance.
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_1\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Authoritative Reference Sources
1. Master user request at: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. Master project scope at: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Worker changes & handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_qs_layout_utils\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_qs_layout_utils\handoff.md`

## Your Review Mission
1. Layer & Host Isolation:
   - Verify `HPKata.Core` has ZERO references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`.
   - Verify all QS, Layout, and CAD utility models and calculators reside purely in `HPKata.Core`.
   - Verify `HPKata.UI` cleanly follows MVVM with `CommunityToolkit.Mvvm` and dynamic binding to `Theme.xaml`.
2. Clean Code Rules:
   - Rule C2: Measure every C# file added/touched in Phase 5 — must be <= 300 physical lines.
   - Rule M3: Measure methods in Phase 5 code — must be <= 50 physical lines.
   - Rule Q-B1: Zero dead/commented-out code, zero unaddressed TODOs/FIXMEs/HACKs/NotImplementedException.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Rule R5: Constant `304.8` must appear strictly and exclusively in `RevitUnitConverter.cs`.
3. Provide explicit verdict: APPROVE or REQUEST_CHANGES.

## Deliverable
Write your review report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_1\review.md`
and a concise `handoff.md` in your working directory.
When complete, notify parent via `send_message`.


## 2026-10-17T12:24:04Z
You are reviewer_m4_1, reviewing Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities) focusing on Code Architecture & Clean Code Governance.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_1\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under headers ## 2026-10-16T23:55:10Z and ## 2026-10-17T06:46:41Z)
2. Read the master project scope:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Read the worker's changes and handoff:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_qs_layout_utils\changes.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_qs_layout_utils\handoff.md
4. Read your task dispatch:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_1\DISPATCH.md

YOUR MISSION:
Review Milestone M4 implementation focusing on Code Architecture & Clean Code Governance:
1. Layer & Host Isolation:
   - Verify HPKata.Core has ZERO references to Autodesk.AutoCAD.* or Autodesk.Revit.*.
   - Verify all QS, Layout, and CAD utilities reside purely in HPKata.Core.
   - Verify HPKata.UI cleanly follows MVVM with CommunityToolkit.Mvvm and dynamic binding to Theme.xaml.
2. Clean Code Rules:
   - Rule C2: Measure every C# file added/touched in Phase 5 — must be <= 300 physical lines.
   - Rule M3: Measure methods in Phase 5 code — must be <= 50 physical lines.
   - Rule Q-B1: Zero dead/commented-out code, zero unaddressed TODOs/FIXMEs/HACKs/NotImplementedException.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Rule R5: Constant 304.8 must appear strictly and exclusively in RevitUnitConverter.cs.
3. Verdict:
   Provide an explicit verdict: APPROVE or REQUEST_CHANGES.

DELIVERABLE:
Write your review report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_1\review.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
