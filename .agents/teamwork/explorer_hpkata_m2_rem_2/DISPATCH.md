# Dispatch Specification — M2 Iteration 2 Explorer (explorer_hpkata_m2_rem_2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## FORENSIC AUDIT FAILURE EVIDENCE REPORT (FULL AND UNFILTERED)
The Forensic Auditor (`auditor_hpkata_m2_1`) reported INTEGRITY VIOLATION with the following evidence:
1. **Rule M3 Violations**:
   - `HPKata.Revit/Commands/ColumnRebarCommands.cs:23`: `Execute` is 56 lines long (lines 23–78).
   - `HPKata.Revit/Commands/FramingRebarCommands.cs:23`: `Execute` is 56 lines long (lines 23–78).
   - `HPKata.Revit/Commands/WallRebarCommands.cs:23`: `Execute` is 56 lines long (lines 23–78).
2. **Challenger Finding**: In `HPKata.Revit.Tests/Commands/RevitExportExcelRunnerTests.cs`, the test teardown (`finally { File.Delete(generatedPath); }`) was found to delete `C:\HPKata\Kata.xlsm` when `ResolveOutputPath` returned it. The test must isolate with `KataInstallEnvironment.UseRootDirectory(tempDir)` or `ResolveOutputPath` must never target master library assets.

## Mission:
1. Inspect `ColumnRebarCommands.cs`, `FramingRebarCommands.cs`, and `WallRebarCommands.cs`. Design helper method extraction (e.g. `ShowDialogAndGenerateRebar`) to bring all `Execute` methods under 40 lines.
2. Inspect `RevitExportExcelRunnerTests.cs` and `RevitExportExcelRunner.cs` to ensure export output paths are safe and isolated, preventing destructive deletion of master files in `C:\HPKata\`.
3. Verify that all `.cs` files are <= 300 lines (Rule C2), all methods are <= 50 lines (Rule M3).
4. Write your report to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\handoff.md`.
Message parent when done.


## 2026-10-18T04:40:43Z
You are explorer_hpkata_m2_rem_2 (M2 Iteration 2 Explorer — Revit Commands & Safe Teardown).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Features 5–8).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

Read ORIGINAL_REQUEST.md and DISPATCH.md first.

FULL UNFILTERED AUDITOR EVIDENCE REPORT:
1. Rule M3 Violations in HPKata.Revit:
   - HPKata.Revit/Commands/ColumnRebarCommands.cs:23: Execute is 56 lines long (> 50 lines).
   - HPKata.Revit/Commands/FramingRebarCommands.cs:23: Execute is 56 lines long (> 50 lines).
   - HPKata.Revit/Commands/WallRebarCommands.cs:23: Execute is 56 lines long (> 50 lines).
2. Challenger Finding: In HPKata.Revit.Tests/Commands/RevitExportExcelRunnerTests.cs, teardown in `finally { File.Delete(generatedPath); }` deleted C:\HPKata\Kata.xlsm because ResolveOutputPath returned it. The test must isolate with KataInstallEnvironment.UseRootDirectory(tempDir) or ResolveOutputPath must not target master library files.

Your mission:
1. Provide exact diffs for ColumnRebarCommands.cs, FramingRebarCommands.cs, and WallRebarCommands.cs extracting helper methods so Execute is <= 35 lines.
2. Provide exact diffs for RevitExportExcelRunnerTests.cs and RevitExportExcelRunner.cs to ensure export output paths are safe and isolated, preventing deletion of C:\HPKata\Kata.xlsm.
3. Verify all .cs files <= 300 lines (Rule C2), all methods <= 50 lines (Rule M3).
4. Write your report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\handoff.md
5. Send a message to parent (3c323530-716e-4b3b-a14c-2a7fcee0c68a) when done.
