# Dispatch Specification — M2 Iteration 2 Remediation Worker (worker_hpkata_m2_rem)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Remediation Explorer Reports & Patches (READ CAREFULLY):
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_1\handoff.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\handoff.md` and `m2_revit_remediation.patch`
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_3\handoff.md` and `ui_window_constructors.patch`

## Exclusively Owned Files for Remediation:
1. `HPKata/HPKata.Core/Excel/Session/KataComExcelSession.cs`
2. `HPKata/HPKata.Core/Excel/Mappers/KataColumnDataMapper.cs`
3. `HPKata/HPKata.Core/Excel/KataExcelAdapter.cs`
4. `HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs`
5. `HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs`
6. `HPKata/HPKata.Revit/Commands/WallRebarCommands.cs`
7. `HPKata/HPKata.Revit/Commands/RevitExportExcelRunner.cs`
8. `HPKata/HPKata.Revit.Tests/Commands/RevitExportExcelRunnerTests.cs`
9. `HPKata/HPKata.UI/Views/FoundationDetailingWindow.xaml.cs`
10. `HPKata/HPKata.UI/Views/SlabDetailingWindow.xaml.cs`
11. `HPKata/HPKata.UI/Views/StairLintelDetailingWindow.xaml.cs`
12. `C:\HPKata\Kata.xlsm`

## Concrete Tasks:
1. **Rule Q-B2**: In `KataComExcelSession.cs:154`, add reason comment to catch block:
   `catch (Exception) { /* Best-effort quit on dispose: ignore errors if Excel is busy or closed */ }`
2. **Rule M3 in Core**: In `KataColumnDataMapper.cs`, extract `MapStorySegment` helper so `ToRebarLayout` is <= 25 lines.
3. **Corrupt file safety in Adapter**: In `KataExcelAdapter.cs`, wrap session opening/saving in try/catch returning `false` gracefully on invalid files.
4. **Rule M3 in Revit**: In `ColumnRebarCommands.cs`, `FramingRebarCommands.cs`, and `WallRebarCommands.cs`, extract helpers so `Execute` is <= 31 lines.
5. **Safe test teardown & asset protection**: In `RevitExportExcelRunner.cs` and `RevitExportExcelRunnerTests.cs`, isolate test with `UseRootDirectory(tempDir)` and guard teardown deletion. Ensure `C:\HPKata\Kata.xlsm` is intact from `HPKata/Distribution/Assets/Kata.xlsm`.
6. **Window constructors**: Add parameterless constructors to `FoundationDetailingWindow.xaml.cs`, `SlabDetailingWindow.xaml.cs`, `StairLintelDetailingWindow.xaml.cs`.
7. **Build & Test Validation**:
   - Run `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` (0 warnings, 0 errors).
   - Run `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` (100% pass across all 5 test assemblies).
   - Verify every `.cs` file <= 300 lines (Rule C2), all methods <= 50 lines (Rule M3), 0 unannotated catch blocks (Rule Q-B2).

Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\handoff.md` and message parent when done.

## 2026-10-18T04:50:43Z
Message from parent: Start Milestone M2 Remediation Implementation.
