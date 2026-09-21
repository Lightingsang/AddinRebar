# Dispatch for Challenger 1 - Milestone 1

## 2026-09-21T17:38:00Z

- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m1_1
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (header ## 2026-09-21T17:20:33Z)
- Worker Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m1\report.md
- Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Empirically stress-test and challenge Milestone 1 changes in `McpShared`:
1. Challenge GuardProfile.Tekla: Write test or script cases attempting to call `model.CommitChanges()`, `MessageBox.Show()`, `Picker.PickObject()`, `System.Diagnostics.Process`, `#r`, or `#load`. Verify that `ScriptGuard.Check` firmly denies them.
2. Challenge PipeNaming: Verify `PipeNaming.For("tekla", 2025)` produces exactly `hptekla-mcp-2025`. Check invalid versions or casing behavior.
3. Challenge JsonRpcMethods: Verify `JsonRpcMethods.Suffix("tekla.execute")` produces `"execute"` and `ProgressMethodFor` produces `"tekla.progress"`.
4. Challenge Serialization: Verify that serializing `ContextResult` for other hosts does not include `"tekla"` property.
5. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m1_1\report.md` and handoff with explicit verdict (APPROVE or REQUEST_CHANGES) to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m1_1\handoff.md`.
