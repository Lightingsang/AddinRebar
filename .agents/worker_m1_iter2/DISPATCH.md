# Task Assignment for Worker M1 (Iteration 2: Challenger 2 Fixes)

## Objective
Apply the 3 concrete fixes identified by Challenger 2 in `HPAutoCad.Core/SmartPlot/`:
1. `PresetService` Null-Handling Fix:
   - In `LoadPresets()` and `LoadPresetsAsync()`, after deserialization:
     - Filter out any null preset items: `.Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Name))`.
     - Ensure the collection is valid. If empty after filtering, load default presets.
     - Protect `GetDefaultPreset()`, `GetPreset()`, `SavePreset()`, `DeletePreset()` against null references.
2. `PresetService` Concurrency & Locking:
   - Add a private static `object _fileLock = new()` (and/or `SemaphoreSlim`) to serialize concurrent file writes/reads within the process.
   - Use a unique temporary file name for atomic swap (e.g., `Path.Combine(dir, $"{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp")`) instead of a hardcoded `${FilePath}.tmp` to prevent concurrent write collisions.
   - Wrap atomic rename in retry or proper error handling.
3. `FileNameService` Reserved Names with Extensions:
   - In `SanitizeFileName(string name)`:
     - Check not only `name`, but also `Path.GetFileNameWithoutExtension(name)`. If the stem without extension matches Windows reserved device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1`..`COM9`, `LPT1`..`LPT9`), prefix the stem with `_` (e.g. `aux.pdf` -> `_aux.pdf`, `CON.txt` -> `_CON.txt`).

## Verification
- Run `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug`
- Run `dotnet test HPAutoCad.Tests`
- Ensure all 77 tests in `HPAutoCad.Tests/SmartPlot/Challenger2StressTests.cs` pass 100%!

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task.

## Authoritative Reference
- Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_2_m1\handoff.md` for exact test scenarios and stack traces.

## Output
Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_iter2\handoff.md`.
Send a message when complete.

## 2026-09-20T22:42:00Z
You are Worker M1 Iteration 2. Your mission is to fix the 3 issues identified by Challenger 2 in HPAutoCad.Core/SmartPlot/Services/PresetService.cs and FileNameService.cs:
1. Filter out null presets and null names in PresetService.LoadPresets() / LoadPresetsAsync().
2. Add synchronization lock and unique temporary file naming (GUID) in PresetService to prevent concurrent write collisions.
3. Handle Windows reserved names with extensions (e.g. aux.pdf, CON.txt) by checking Path.GetFileNameWithoutExtension() in FileNameService.SanitizeFileName().

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_iter2
Read your task: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_iter2\DISPATCH.md
Read Challenger 2's detailed failure report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_2_m1\handoff.md

Verify with:
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
dotnet test HPAutoCad.Tests

Write your report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_iter2\handoff.md.
Send a message when complete.
