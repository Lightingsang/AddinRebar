# DISPATCH — teamwork_preview_challenger_m4_2

## Context
You are Challenger 2 for Milestone M4 (Comprehensive STA Unit Tests & StaticResource Parity).

## Paths to Context Files
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (entry dated 2026-10-17T14:27:21Z)
- Project Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Worker M4 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_1\handoff.md`

## Mission
Adversarially challenge Milestone M4 deliverables:
1. Verify that `AllWindowsXamlInstantiationTests` exercises all 13 view types explicitly by type, not just a subset.
2. Verify that `StaticResourceParityScannerTests` checks all `.xaml` files in `HPKata.UI/Views/` without skipping any files.
3. Check for any memory leaks or hanging STA threads.
4. Verify build and tests:
```powershell
dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
```

## Handoff
Write `handoff.md` with clear verdict (`APPROVE` or `REQUEST_CHANGES`) and notify `orchestrator_19`.


## 2026-10-17T16:06:53Z
You are teamwork_preview_challenger_m4_2, Challenger 2 for Milestone M4.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_m4_2\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_m4_2\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).
Read the project plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Read Worker M4's handoff report at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_1\handoff.md.

Adversarially challenge Milestone M4 deliverables:
- Check that all 13 views are verified explicitly by type.
- Check that all 13 views have zero unresolved StaticResource keys.
- Verify clean build and test pass.
Verify build and tests:
  dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
  dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
Write handoff.md with clear verdict (APPROVE or REQUEST_CHANGES) and notify orchestrator_19.
