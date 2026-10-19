# Dispatch Specification — M2 Iteration 2 Adversarial Reviewer (reviewer_hpkata_m2_rem_2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_rem_2\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Test Certification Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\handoff.md`

## Your Mission:
1. Review the UI constructors and Revit command remediations:
   - Check `FoundationDetailingWindow.xaml.cs`, `SlabDetailingWindow.xaml.cs`, and `StairLintelDetailingWindow.xaml.cs` for default parameterless constructors.
   - Verify that all 13 WPF windows in `HPKata.UI/Views/` support parameterless construction and instantiate on STA thread without throwing under both Dark and Light themes.
   - Check `ColumnRebarCommands.cs`, `FramingRebarCommands.cs`, and `WallRebarCommands.cs` in `HPKata.Revit/Commands/`: verify `Execute` method length <= 50 lines (Rule M3).
   - Review `RevitExportExcelRunner.cs` and `RevitExportExcelRunnerTests.cs`: verify that master installation files (`C:\HPKata\Kata.xlsm`) are never used as volatile test outputs and cannot be deleted during test teardown.
2. Run `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`.
3. Verify that `C:\HPKata\Kata.xlsm` is intact and present after the test suite completes.
4. Formulate your clear verdict: **APPROVE** or **REQUEST_CHANGES**.
5. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_rem_2\handoff.md` and send a message to parent when done.

## 2026-10-18T04:59:49Z
[Message] timestamp=2026-10-18T04:59:49Z sender=3c323530-716e-4b3b-a14c-2a7fcee0c68a priority=MESSAGE_PRIORITY_HIGH content=You are reviewer_hpkata_m2_rem_2 (M2 Iteration 2 Adversarial Reviewer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_rem_2\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_rem_2\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Features 5–8).
Test Certification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Worker Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\handoff.md
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

Read ORIGINAL_REQUEST.md and DISPATCH.md first.
Your mission:
1. Review the UI constructors and Revit command remediations:
   - Check FoundationDetailingWindow.xaml.cs, SlabDetailingWindow.xaml.cs, and StairLintelDetailingWindow.xaml.cs for default parameterless constructors.
   - Verify that all 13 WPF windows in HPKata.UI/Views/ instantiate on STA thread without throwing under both Dark and Light themes.
   - Check ColumnRebarCommands.cs, FramingRebarCommands.cs, and WallRebarCommands.cs: verify Execute method length <= 50 lines (Rule M3).
   - Review RevitExportExcelRunner.cs and RevitExportExcelRunnerTests.cs: verify that master installation files (C:\HPKata\Kata.xlsm) are protected and not deleted during test teardown.
2. Run `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`.
3. Verify that C:\HPKata\Kata.xlsm is intact after the test run.
4. Formulate your clear verdict: APPROVE or REQUEST_CHANGES.
5. Write your report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_rem_2\handoff.md
6. Send a message to parent (3c323530-716e-4b3b-a14c-2a7fcee0c68a) when done.
