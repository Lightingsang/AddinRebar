# Task Dispatch: explorer_m4_rem_cad

## Identity
- **Agent Name**: explorer_m4_rem_cad
- **Role**: CAD Commands & Aliases Spec Explorer
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_rem_cad\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Authoritative Context Documents (MANDATORY TO READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Master Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Dead Ends Log: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\DEAD_ENDS.md`
4. Auditor M4 Full Evidence Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\audit_report.md`
5. Functional Reviewer Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_2\review.md`

## Your Exploration Scope
Analyze the AutoCAD commands and aliases defects identified by the Forensic Auditor and Functional Reviewer:
1. **Facade Elimination for `dnoi` / `dim_noi_tiep` (Continuous Dimensioning)**:
   - File: `HPKata/HPKata.AutoCad/Commands/DimensionUtilityCommands.cs:26-31`
   - Currently, `ContinueDimension()` delegates directly to `DimensionUtilityController.TrimDimensionWitnessLinesInteractive()`. This is an integrity violation (cloning `catdim` witness trimming).
   - Formulate a precise, complete design and implementation plan for genuine continuous dimensioning (`ContinueDimensionInteractive()` in `DimensionUtilityController.cs`):
     - Prompt user to pick/select an existing `Dimension` (or `AlignedDimension`) to continue from, or pick two points if starting anew.
     - Extract the dimension style, dimension line orientation/elevation, and the second extension point (`XLine2Point`).
     - Loop with `Editor.GetPoint()` allowing the user to click consecutive points, creating chained `AlignedDimension` or `RotatedDimension` entities placed into ModelSpace with active transaction.
     - Ensure cancellation (ESC / Enter) gracefully completes the transaction without throwing exceptions.
2. **Missing Command Registration for Lisp Alias `vg`**:
   - File: `HPKata/HPKata.AutoCad/Commands/LayoutCommands.cs:12-17`
   - `GoiLenh_HPKata.lsp:43` defines `(defun c:vg()(command "ve_grid"))`.
   - Add `[CommandMethod("ve_grid")]` to `DrawGrid()` in addition to `[CommandMethod("vg")]` and `[CommandMethod("ve_luoi_truc")]`.
3. **Clean Code & Boundary Constraints**:
   - Ensure proposed code keeps all files $\le 300$ physical lines (Rule C2) and methods $\le 50$ physical lines (Rule M3).
   - Zero dead code, zero unannotated catch blocks.

## Deliverable
Write your detailed remediation specification report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_rem_cad\cad_remediation_plan.md`
and a concise `handoff.md` in your working directory.
When done, notify parent via `send_message`.


## 2026-10-17T12:40:35Z
You are explorer_m4_rem_cad, the CAD Commands & Aliases Remediation Spec Miner for Milestone M4 (Iteration 2 Remediation).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_rem_cad\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under headers ## 2026-10-16T23:55:10Z and ## 2026-10-17T06:46:41Z)
2. Read the master project scope:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Read DEAD_ENDS.md:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\DEAD_ENDS.md
4. Read your task dispatch:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_rem_cad\DISPATCH.md
5. Read the auditor's full evidence report:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\audit_report.md
6. Read the reviewer report:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_2\review.md

YOUR MISSION:
Analyze the CAD commands and command aliases defects and formulate a precise, non-facade, genuine implementation plan:
1. Continuous Dimensioning (`dnoi` / `dim_noi_tiep`):
   - Replace the facade call in `DimensionUtilityCommands.cs` that currently routes to `TrimDimensionWitnessLinesInteractive()`.
   - Design `ContinueDimensionInteractive()` in `DimensionUtilityController.cs` to prompt user to pick an existing dimension or points, and iteratively chain aligned dimensions.
2. Register `[CommandMethod("ve_grid")]`:
   - In `LayoutCommands.cs`, add `[CommandMethod("ve_grid")]` to `DrawGrid()` to satisfy `GoiLenh_HPKata.lsp:43`.
3. Clean Code Rules Mandate:
   - Ensure proposed code keeps all files <= 300 physical lines (Rule C2) and methods <= 50 physical lines (Rule M3).

DELIVERABLE:
Write your specification report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_rem_cad\cad_remediation_plan.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message.
