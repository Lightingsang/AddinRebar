## 2026-10-18T07:37:16Z
You are reviewer_m2_gen2_2.
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_gen2_2\
Parent Conversation ID: 1e55a9de-0d52-405d-83fa-fa58bb05a293

MANDATORY READING BEFORE WORK:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Master Scope: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen2\PROJECT.md
- Truth Matrix: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md
- Worker M2 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_gen2\handoff.md

MISSION: Milestone M2 Asset Parity & Clean Code Review
1. Inspect HPDcePro/assets/ against requirements:
   - DWG templates: 13 in Cad/, 4 in AutoStr/00.FileDungChung/ (17 total).
   - XLSM workbooks: 132 total in Excel/ (File V.2020/, DceSteel/, Menu DCE_2020.xlsm).
   - Machine Learning: MayHoc/mauhoc.db (8.98 MB SQLite DB) + model.zip.
   - System configs: 178 files in System/ (170 txt + 6 dat + executable + bin).
   - Language: 8 txt files in Language/.
   - Icons: 133 files in Icon/.
   - Menus: 7 CUIX in Cuix/.
   - SystemSteel: 112 txt in SystemSteel/.
   - Verify assets normalization (zero occurrences of "C:\Dce Pro" in text assets).
2. Clean Code Governance Review:
   - Rule C2: <= 300 lines per file for all touched/new files.
   - Rule M3: <= 50 lines per method for all touched/new files.
   - Rule Q-B1: Zero commented-out or dead code.
   - Rule Q-B2: All catch blocks have "// Reason: ..." comments.
3. Run verification:
   - python .agents/teamwork/explorer_m2_gen2_3/verify_m2_integrity.py
   - dotnet test HPDcePro/HPDcePro.slnx -c Debug --no-build
4. Write your review report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_gen2_2\handoff.md with a clear verdict: APPROVE or REQUEST_CHANGES. Then notify parent orchestrator via send_message.
