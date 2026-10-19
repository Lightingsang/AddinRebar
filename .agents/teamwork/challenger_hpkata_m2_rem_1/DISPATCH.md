# Dispatch Specification — M2 Iteration 2 Empirical Challenger 1 (challenger_hpkata_m2_rem_1)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_rem_1\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Feature 5)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\handoff.md`

## Your Mission:
1. Empirically verify the core Excel engine and asset preservation:
   - Check `C:\HPKata\Kata.xlsm`: verify it exists, has size ~474,860 bytes, and is valid.
   - Run tests or empirical script testing `KataExcelAdapter` resilience on 0-byte files, non-existent files, and invalid formats to confirm it returns `false` gracefully without throwing unhandled exceptions.
   - Verify that running `dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj` does NOT delete or mutate `C:\HPKata\Kata.xlsm`.
   - Run `dotnet test HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj` to verify all 574 Core tests pass.
2. Formulate your empirical verdict: **APPROVE** or **REJECT**.
3. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_rem_1\handoff.md` and send a message to parent when done.


## 2026-10-18T04:59:49Z
You are challenger_hpkata_m2_rem_1 (M2 Iteration 2 Core Excel Engine Empirical Challenger).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_rem_1\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_rem_1\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Feature 5).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Worker Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\handoff.md
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

Read ORIGINAL_REQUEST.md and DISPATCH.md first.
Your mission:
1. Empirically verify the core Excel engine and asset preservation:
   - Check C:\HPKata\Kata.xlsm: verify it exists, has size ~474,860 bytes, and is valid.
   - Run tests or empirical script testing KataExcelAdapter resilience on 0-byte files, non-existent files, and invalid formats to confirm it returns false gracefully without throwing unhandled exceptions.
   - Verify that running `dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj` does NOT delete or mutate C:\HPKata\Kata.xlsm.
   - Run `dotnet test HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj` to verify all 574 Core tests pass.
2. Formulate your empirical verdict: APPROVE or REJECT.
3. Write your report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_rem_1\handoff.md
4. Send a message to parent (3c323530-716e-4b3b-a14c-2a7fcee0c68a) when done.
