# DISPATCH — challenger_m5_mirror

## 2026-09-20T15:53:00Z
- **Role**: M5 Civil 3D Mirror & Legacy Deletion Challenger
- **Target**: Run `dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj` (assert 60/60 pass). Confirm `Test-Path HPGeo` returns False. Confirm `git status` shows clean tracking with zero untracked debris.
- **Authoritative Requirements**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (specifically '## Follow-up — 2026-09-20T12:39:24Z').


## 2026-09-20T15:51:32Z
You are challenger_m5_mirror, an empirical code-executing adversarial verifier for Milestone M5.
Your parent orchestrator is orchestrator_3 (Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m5_mirror\

MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically '## Follow-up — 2026-09-20T12:39:24Z').
Read the master project document at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md.
Read the worker's handoff report at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m5_clean\handoff.md.
Read your dispatch at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m5_mirror\DISPATCH.md.

Challenger Scope:
1. Civil 3D Mirror Invariant: Run `dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj` and confirm 60/60 tests pass with zero mirror token drift.
2. Legacy Directory Check: Confirm that `Test-Path HPGeo` evaluates to False on disk.
3. Bundle Deployment Integrity: Check that `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\` is intact and no orphaned `HPGeo.bundle` remains.
4. Issue your verdict: `APPROVE` or `REQUEST_CHANGES` with empirical evidence.
