## 2026-10-18T07:08:18Z
You are explorer_m2_gen2_3.
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_gen2_3\
Parent Conversation ID: 1e55a9de-0d52-405d-83fa-fa58bb05a293

MANDATORY READING BEFORE WORK:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Master Scope: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen2\PROJECT.md
- Truth Matrix: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md

MISSION: Milestone M2 Verification & Grep Strategy Exploration
You are designing the verification and testing methodology for Milestone M2:
1. Examine existing tests in HPDcePro/tests/ (HPDcePro.Core.Tests, HPDcePro.Adapters.Tests). How are tests run currently?
2. Design static grep checks to ensure:
   - Zero occurrences of "C:\Dce Pro" or "C:/Dce Pro" in HPDcePro/src/** and HPDcePro/assets/**.
   - Clean code compliance (Rule C2 <= 300 lines/file, Rule M3 <= 50 lines/method, Q-B1, Q-B2).
3. Design runtime unit & integration tests:
   - Tests in HPDcePro.Core.Tests verifying AssetPathResolver correctly locates and resolves every single asset type (17 DWGs, 132 XLSMs, mauhoc.db, model.zip, 178 System files, 8 Lang dicts, 133 Icons).
   - Test that opening mauhoc.db via SQLite succeeds and table schemas can be queried without throwing.
   - Test that Excel templates can be located and opened via ClosedXML.
4. Establish criteria for M2 Gate (Reviewers, Challengers, Forensic Auditor) to verify zero dependency on C:\Dce Pro and 100% preservation of the read-only legacy invariant.
5. Write your comprehensive report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_gen2_3\handoff.md and notify the parent orchestrator via send_message.
