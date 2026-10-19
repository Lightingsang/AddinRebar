# Dispatch Specification — M2 Core Excel Engine Challenger (challenger_hpkata_m2_1)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_1\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Feature 5)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md`

## Mission:
Empirically challenge the Feature 5 Bi-directional Excel Engine:
1. Write and execute stress / edge-case tests against `KataExcelAdapter`:
   - Roundtrip read & write on actual `C:\HPKata\Kata.xlsm` (and isolated copies in temp dirs).
   - Test sheets `Dam`, `Cot`, `Vach`, `Thong_ke`, and `Iso_QHPlus.xls`.
   - Test invalid/corrupt paths, missing sheets, empty cells, extreme rebar dimensions, multi-threaded concurrent calls.
2. Verify ClosedXML headless fallback operates cleanly when desktop Excel is not running.
3. Formulate your empirical verdict: **APPROVE** or **REJECT**.
4. Write your report to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_1\handoff.md`.
Message parent when done.

## 2026-10-18T04:25:09Z
[Message] timestamp=2026-10-18T04:25:09Z sender=3c323530-716e-4b3b-a14c-2a7fcee0c68a priority=MESSAGE_PRIORITY_HIGH content=You are challenger_hpkata_m2_1 (M2 Core Excel Engine Empirical Challenger).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_1\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_1\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Feature 5).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Worker Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

Read ORIGINAL_REQUEST.md and DISPATCH.md first.
Your mission:
1. Empirically challenge KataExcelAdapter under stress, edge cases, and boundary conditions:
   - Roundtrip read & write on actual C:\HPKata\Kata.xlsm and isolated temp copies.
   - Sheets Dam, Cot, Vach, Thong_ke, and Iso_QHPlus.xls.
   - Test invalid/corrupt paths, missing sheets, empty cells, extreme dimensions, multi-threaded concurrency.
2. Verify ClosedXML headless fallback operates cleanly when desktop Excel is not running.
3. Formulate your empirical verdict: APPROVE or REJECT.
4. Write your report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_1\handoff.md
5. Send a message to parent (3c323530-716e-4b3b-a14c-2a7fcee0c68a) when done.
