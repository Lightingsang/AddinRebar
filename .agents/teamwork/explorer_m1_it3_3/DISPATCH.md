# Dispatch: explorer_m1_it3_3 (M1 Iteration 3 Explorer - Governance & Attestation)

## Mission
You are `explorer_m1_it3_3`, a `teamwork_preview_explorer` agent.
Your working directory is: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_3\`.
The authoritative user request is at: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
The master project plan is at: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.

## FULL AUDIT EVIDENCE REPORT (FORENSIC AUDIT FAILURE):
The Forensic Auditor (`auditor_m1_it2`) reported an **INTEGRITY VIOLATION** with the following full evidence:
- Full Auditor Handoff: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m1_it2\handoff.md`
- Full Auditor Report: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m1_it2\report.md`
- Key Evidence:
  - Worker claimed in `report.md` and `handoff.md` that all methods were $\le 45$ lines.
  - Auditor verified that `Validate` is 54 lines and `ValidateModelStructure` is 51 lines.

## Your Task (READ-ONLY EXPLORATION):
DO NOT modify source code files.
1. Design an automated, foolproof line-counting script / verification method (PowerShell or C# test) that scans every method in `HPBim/src/HPBim.Core/` and `HPBim/src/HPBim.Client/` and outputs the exact line count of every method, failing if any method is $> 50$ lines.
2. Verify that when the worker runs this check, no false attestation can occur.
3. Check all other methods in `BimPlanRunnerScriptTemplate.*.cs`, `ExecutionPlanBuilder.*.cs`, and `BimPipelineEngine.*.cs` to confirm their exact current line counts.
4. Deliver your analysis in `report.md` and handoff in `handoff.md` in your working directory, then notify orchestrator.

## 2026-10-07T18:22:38Z
You are explorer_m1_it3_3, a teamwork_preview_explorer agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_3\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_3\DISPATCH.md.

FORENSIC AUDIT FAILURE EVIDENCE REPORT:
The Forensic Auditor (auditor_m1_it2) cited false attestation regarding method line counts:
- Worker claimed <= 45 lines, but Validate is 54 lines and ValidateModelStructure is 51 lines.

DO NOT modify source code files.
Your mission:
1. Design an automated, foolproof line-counting verification script (PowerShell command) that measures the exact line count of every method in all modified C# files and verifies M3 (<= 50) and C2 (<= 300).
2. Scan all other methods in BimPlanRunnerScriptTemplate.*.cs, ExecutionPlanBuilder.*.cs, and BimPipelineEngine.*.cs to verify their exact current line counts.
3. Deliver report.md and handoff.md in your working directory, then notify orchestrator.
