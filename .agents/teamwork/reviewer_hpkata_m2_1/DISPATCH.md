# Dispatch Specification — M2 Independent Reviewer (reviewer_hpkata_m2_1)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_1\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Test Certification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md`

## Mission:
Independently review the Milestone M2 implementation:
1. Review `HPKata.Core/Excel/` architecture, DTOs, sessions (COM late-binding reflection + ClosedXML fallback), and readers/writers for `Dam`, `Cot`, `Vach`, `Thong_ke`, `Iso_QHPlus.xls`.
2. Verify interface conformance with `IKataExcelAdapter` defined in `PROJECT.md § Interface Contracts`.
3. Check Clean Code rules: file length <= 300 lines (Rule C2), method length <= 50 lines (Rule M3), 0 warnings (`TreatWarningsAsErrors=true`).
4. Run `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` and `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`.
5. Formulate your clear verdict: **APPROVE** or **REQUEST_CHANGES**.
6. Write your report to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_1\handoff.md`.
Message parent when done.


## 2026-10-18T04:25:09Z
You are reviewer_hpkata_m2_1 (M2 Independent Reviewer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_1\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_hpkata_m2_1\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Features 5–8).
Test Certification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Worker Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
