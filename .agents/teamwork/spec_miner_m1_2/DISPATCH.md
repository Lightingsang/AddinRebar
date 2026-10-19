# Dispatch: Spec Miner M1.2 (AutoCAD 311 Commands, 11 Jigs & 12 Forms Specification)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_2\`
- Parent Orchestrator: `orchestrator_25`
- Role: `teamwork_preview_spec_miner`
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (`## 2026-10-18T05:27:06Z`)

## Mission
Extract and structure the exact specifications for all 311 AutoCAD commands (`C:\Dce Pro\AiAgent\DanhBa\danhba_lenh.json`), 11 CAD Jigs, and 12 UI Forms. Cross-reference their prompt requirements, selection flags, drawing outputs, and current mock status in `HPDcePro.AutoCad`.
Report findings to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_2\handoff.md`.
## 2026-10-18T05:50:43Z
You are Spec Miner M1.2 for Milestone M1 (AutoCAD 311 Commands, 11 Jigs & 12 Forms Specification).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_2\
Read your dispatch specification at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_2\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (`## 2026-10-18T05:27:06Z`).
Read the project scope at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md.
Inspect `C:\Dce Pro\AiAgent\DanhBa\danhba_lenh.json` (strictly read-only) and `HPDcePro/src/HPDcePro.AutoCad/`.

Your mission:
1. Extract and inventory all 311 AutoCAD commands from `danhba_lenh.json`. Map each to its command name, Vietnamese description, prompt requirement (`can_chon`), modal dialog requirement (`can_form`), drawing output (`ghi_ban_ve`), and target implementation in `HPDcePro.AutoCad`.
2. Extract and format the 11 CAD Jigs (`VeMBKC_JIG`, `JigSlabRebar`, etc.) and 12 UI Forms (`DamCong_Frm`, `MongCoc_Frm`, etc.) with legacy tokens (`0x02000E52`..`0x02000E5C`).
3. Classify their current status in `HPDcePro` (identifying which commands are currently `ĐANG LÀ MOCK` or `MISSING`).
4. Format these entries into ready-to-insert markdown tables for `docs/rea-dce-pro/TRUTH_MATRIX.md`.
5. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_2\handoff.md`.
Communicate your completion back to parent via send_message.
