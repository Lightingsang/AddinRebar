# DISPATCH — teamwork_preview_reviewer_m3_2

## Context
You are Reviewer 2 (Adversarial Focus) for Milestone M3 (Revit Element Geometry Reader & Safe Picker Fallback).

## Paths to Context Files
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (entry dated 2026-10-17T14:27:21Z)
- Project Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Worker M3 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_1\handoff.md`

## Review Scope
Review Worker M3's deliverables with adversarial focus on edge cases, unit conversion precision, exception handling, and contract conformance:
1. `HPKata/HPKata.Revit/Services/RevitElementGeometryReader.cs`
2. `HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs`
3. `HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs`
4. `HPKata/HPKata.Revit/Commands/WallRebarCommands.cs`
5. `HPKata/HPKata.Revit.Tests/Services/RevitElementGeometryReaderTests.cs`

## Key Verification Criteria
1. Edge cases in geometry reader: negative or zero dimensions, null symbols, null BoundingBox, multi-segment walls, sloped beams.
2. Rule R5 quarantine: Verify no `304.8` literal exists anywhere in `HPKata.Revit` except `RevitUnitConverter.cs`.
3. Null safety on `targetElement` when dialog accepted vs cancelled.
4. Clean code compliance: line counts <= 300, method counts <= 50, zero compiler warnings.
5. Verification of tests:
```powershell
dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
```

## Handoff
Write `handoff.md` in your working directory with a clear verdict (`APPROVE` or `REQUEST_CHANGES`). When done, notify `orchestrator_19`.

## 2026-10-17T15:52:05Z
You are teamwork_preview_reviewer_m3_2, Reviewer 2 (Adversarial Focus) for Milestone M3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_m3_2\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_m3_2\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).
Read the project plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Read Worker M3's handoff report at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_1\handoff.md.

Independently review Milestone M3 deliverables with adversarial focus on edge cases, Rule R5 quarantine (literal 304.8 only in RevitUnitConverter.cs), null safety, and clean code limits.
Verify build and tests:
  dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
  dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
Write handoff.md with clear verdict (APPROVE or REQUEST_CHANGES) and notify orchestrator_19.

