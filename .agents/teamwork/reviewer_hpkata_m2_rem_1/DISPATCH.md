# Dispatch Specification — M2 Iteration 2 Independent Reviewer (reviewer_hpkata_m2_rem_1)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_rem_1\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Test Certification Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\handoff.md`

## Your Mission:
1. Review the remediation changes implemented by worker_hpkata_m2_rem:
   - `KataComExcelSession.cs:154` catch block annotation (Rule Q-B2).
   - `KataColumnDataMapper.cs` helper method `MapStorySegment` and method length of `ToRebarLayout` (Rule M3 <= 50 lines).
   - `KataExcelAdapter.cs` exception safety and fallback data generation on corrupted/inaccessible files.
   - Clean Code rules across all production files in HPKata.Core: file length <= 300 lines (Rule C2), method length <= 50 lines (Rule M3), 0 warnings (`TreatWarningsAsErrors=true`).
2. Run `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` and `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`.
3. Verify that 100% of tests pass across all test projects.
4. Formulate your clear verdict: **APPROVE** or **REQUEST_CHANGES**.
5. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_rem_1\handoff.md` and send a message to parent when done.

## 2026-10-18T04:59:49Z
[Message] timestamp=2026-10-18T04:59:49Z sender=3c323530-716e-4b3b-a14c-2a7fcee0c68a priority=MESSAGE_PRIORITY_HIGH content=You are reviewer_hpkata_m2_rem_1 (M2 Iteration 2 Independent Reviewer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_rem_1\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_rem_1\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Features 5–8).
Test Certification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Worker Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\handoff.md
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
