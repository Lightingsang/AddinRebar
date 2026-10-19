## 2026-10-08T11:34:41Z
You are explorer_m1_rem_1.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_rem_1\

Read the authoritative documents first:
- ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (see ## 2026-10-08T11:29:50Z)
- SCOPE.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_5\SCOPE.md
- DISPATCH.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_5\DISPATCH.md

Your mission:
Investigate Canonical String SourceKey implementation in HPBim (R1.1):
1. Normalize SourceKey to canonical string format: `[DocIdentifier]#[Category]:[PrimaryHandle]` across all ElementPlanItem, wire DTO, ScriptArgs, Roslyn runner, and HPBimCoordinationReceipt.
2. Ensure 100% roundtrip equality through JSON serialization/deserialization.
3. Inspect HPBim/HPBim.Core/ (specifically Coordination/CompositeSourceKey.cs, ElementPlanItem.cs, etc.) and HPBim/HPBim.Client/ (BimPlanRunner.cs, wire contracts, ScriptArgs building, etc.).
4. Identify all files, classes, methods, and tests affected.
5. Provide a detailed, step-by-step fix strategy and code snippets for Worker.

Write your findings to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_rem_1\report.md
and write a standard handoff report to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_rem_1\handoff.md

When done, send a message back to the orchestrator notifying completion.
