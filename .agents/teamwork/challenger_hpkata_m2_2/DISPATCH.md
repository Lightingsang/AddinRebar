# Dispatch Specification — M2 Host Commands & UI Challenger (challenger_hpkata_m2_2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_2\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 6–8)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md`

## Mission:
Empirically challenge UI integration (Feature 6) and Host Commands (Features 7 & 8):
1. UI Stress & Theme Challenge:
   - On STA thread, verify all 13 WPF windows (`HPKata.UI/Views/`) instantiate cleanly under both Dark and Light themes.
   - Test `BeamDetailingViewModel`, `ColumnDetailingViewModel`, `WallDetailingViewModel` commands (`OpenExcelCommand`, `ReadExcelCommand`, `WriteExcelCommand`, `AutoSyncOnApply`) with mock/fake adapters and missing files to verify zero unhandled exceptions.
2. Detailing Commands & XData Challenge:
   - Verify AutoCAD detailing commands (`loadbeam`, `loadcolumn`, `loadwall`, `ve_dam`, `ve_cot`, `ve_vach`, `toexcel`, `toexcelqh`) populate registries correctly.
   - Verify `KataEntityStyler` attaches valid `KATA` XData records (`Dang`, `Phi`, `STT`, `SoLuong`, `ChieuDaiCacDoanMm`) and canonical layers (`kata_thep chu`, `kata_thep dai`, `kata_net thay`).
   - Verify Revit command parameter calculations and fallback behaviors.
3. Formulate your empirical verdict: **APPROVE** or **REJECT**.
4. Write your report to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_2\handoff.md`.
Message parent when done.

## 2026-10-18T04:25:09Z
You are challenger_hpkata_m2_2 (M2 Host Commands & UI Empirical Challenger).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_2\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_2\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Features 6–8).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Worker Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

Read ORIGINAL_REQUEST.md and DISPATCH.md first.
Your mission:
1. UI Stress & Theme Challenge:
   - On STA thread, verify all 13 WPF windows (HPKata.UI/Views/) instantiate cleanly under both Dark and Light themes.
   - Test BeamDetailingViewModel, ColumnDetailingViewModel, WallDetailingViewModel commands (OpenExcelCommand, ReadExcelCommand, WriteExcelCommand, AutoSyncOnApply) with mock/fake adapters and missing files to verify zero unhandled exceptions.
2. Detailing Commands & XData Challenge:
   - Verify AutoCAD detailing commands (loadbeam, loadcolumn, loadwall, ve_dam, ve_cot, ve_vach, toexcel, toexcelqh) populate registries correctly.
   - Verify KataEntityStyler attaches valid KATA XData records (Dang, Phi, STT, SoLuong, ChieuDaiCacDoanMm) and canonical layers.
   - Verify Revit command parameter calculations and fallback behaviors.
3. Formulate your empirical verdict: APPROVE or REJECT.
4. Write your report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_2\handoff.md
5. Send a message to parent (3c323530-716e-4b3b-a14c-2a7fcee0c68a) when done.
