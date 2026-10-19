# DISPATCH — teamwork_preview_challenger_m3_2

## Context
You are Challenger 2 for Milestone M3 (Revit Element Geometry Reader & Safe Picker Fallback).

## Paths to Context Files
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (entry dated 2026-10-17T14:27:21Z)
- Project Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Worker M3 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_1\handoff.md`

## Mission
Adversarially challenge Milestone M3 deliverables:
1. Challenge fallback cascade order:
   - Does parameter lookup take precedence over bounding box?
   - Does bounding box take precedence over defaults?
   - What happens when named parameters are empty strings or invalid storage types?
2. Challenge unit conversion:
   - Does any file in `HPKata.Revit` bypass `RevitUnitConverter.FeetToMm`?
   - Is there any `304.8` hardcoded literal in `RevitElementGeometryReader.cs` or test files?
3. Verify full solution build and tests:
```powershell
dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
```

## Handoff
Write `handoff.md` with clear verdict (`APPROVE` or `REQUEST_CHANGES`) and notify `orchestrator_19`.


## 2026-10-17T15:52:05Z
You are teamwork_preview_challenger_m3_2, Challenger 2 for Milestone M3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_m3_2\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_m3_2\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).
Read the project plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Read Worker M3's handoff report at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_1\handoff.md.

Adversarially challenge Milestone M3 deliverables:
1. Challenge fallback cascade order (primary parameter -> bounding box -> defaults).
2. Challenge unit conversion: assert zero occurrences of literal 304.8 in RevitElementGeometryReader.cs.
Verify build and tests:
  dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
  dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
Write handoff.md with clear verdict (APPROVE or REQUEST_CHANGES) and notify orchestrator_19.
