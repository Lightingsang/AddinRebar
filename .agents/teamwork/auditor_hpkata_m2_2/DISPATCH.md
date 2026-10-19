# Dispatch Specification — M2 Iteration 2 Forensic Auditor (auditor_hpkata_m2_2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_hpkata_m2_2\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Test Certification Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\handoff.md`

## ⚠️ HARD VETO — NON-NEGOTIABLE:
If you detect INTEGRITY VIOLATION or CHEATING DETECTED, you MUST report INTEGRITY VIOLATION. Do NOT balance against test scores.

## Your Mission:
1. Forensic static analysis of all files modified or added in Milestone M2 remediation:
   - Audit `HPKata/HPKata.Core/Excel/Session/KataComExcelSession.cs:154`: verify whether catch block is annotated with justification comment (Rule Q-B2).
   - Audit `HPKata/HPKata.Core/Excel/Mappers/KataColumnDataMapper.cs`: verify that `ToRebarLayout` and `MapStorySegment` are <= 50 lines (Rule M3).
   - Audit `HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs`, `FramingRebarCommands.cs`, `WallRebarCommands.cs`: verify that `Execute` is <= 50 lines (Rule M3).
   - Audit `HPKata/HPKata.Core/Excel/KataExcelAdapter.cs`: verify that error handling is genuine and does not silently swallow errors or return hardcoded fake results.
   - Audit `HPKata/HPKata.UI/Views/`: verify that `FoundationDetailingWindow.xaml.cs`, `SlabDetailingWindow.xaml.cs`, `StairLintelDetailingWindow.xaml.cs` have genuine parameterless constructors forwarding to ViewModels.
2. Codebase-wide Anti-Cheating & Clean Code Audit:
   - Verify 0 occurrences of `kata_pro` (case-insensitive) across all newly created or touched files.
   - Verify 0 unannotated catch blocks (Rule Q-B2) across the entire solution.
   - Verify 0 commented-out dead code blocks (Rule Q-B1).
   - Verify all `.cs` files <= 300 lines (Rule C2).
   - Verify 0 production methods > 50 lines (Rule M3).
3. Formulate your binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.
4. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_hpkata_m2_2\handoff.md` and send a message to parent when done.

## 2026-10-18T04:59:49Z
[Message] timestamp=2026-10-18T04:59:49Z sender=3c323530-716e-4b3b-a14c-2a7fcee0c68a priority=MESSAGE_PRIORITY_HIGH content=You are auditor_hpkata_m2_2 (Milestone M2 Iteration 2 Forensic Auditor).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_hpkata_m2_2\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_hpkata_m2_2\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Features 5–8).
Test Certification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Worker Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\handoff.md
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
