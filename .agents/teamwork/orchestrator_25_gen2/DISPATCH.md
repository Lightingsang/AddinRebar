# Dispatch Specification — Project Orchestrator (orchestrator_25_gen2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen2\`
- Predecessor Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\`
- Predecessor Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\handoff.md`
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md`
- Master Truth Matrix: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (header `## 2026-10-18T05:27:06Z`)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Read-Only Legacy Baseline: `C:\Dce Pro`

## Mission
Take over from `orchestrator_25` via the Succession Protocol.
Milestone M1 is **DONE** (Master Truth Matrix certified with 104 HOÀN THÀNH, 110 ĐANG LÀ MOCK, 388 CHƯA GIẢI MÃ ĐƯỢC; gate PASS; Forensic Auditor CLEAN).
Drive execution through Milestone M2 (Standalone Asset & Template Migration), Milestone M3 (1:1 AutoCAD, 11 Jigs & UI Forms), Milestone M4 (1:1 Revit & Excel), and Milestone M5 (Final Build & Forensic Integrity Audit).

## Concrete Immediate Task
1. Initialize your state in `orchestrator_25_gen2`: create `BRIEFING.md`, `progress.md`, and start heartbeat cron.
2. Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\handoff.md`.
3. Launch Milestone M2 (Standalone Asset & Template Migration):
   - Dispatch Worker M2 to copy all 17 DWGs, 132 XLSMs, `MayHoc/mauhoc.db` + `model.zip`, 178 System files, 8 Language dictionaries, 133 Icons, and 7 CUIX menus from `C:\Dce Pro` into `HPDcePro/assets/`.
   - Update file path resolvers in `HPDcePro/src/` to resolve relative to `HPDcePro/assets/` and `AppContext.BaseDirectory` so that `HPDcePro` operates 100% independently with 0 hardcoded references to `C:\Dce Pro`.
   - Verify with static and runtime grep.
   - Run M2 Gate verification (Reviewer, Challenger, Forensic Auditor).
4. Advance to Milestone M3, M4, and M5 as outlined in `PROJECT.md`.


## 2026-10-18T07:12:00Z
You are the Successor Project Orchestrator (orchestrator_25_gen2) for HPDcePro 100% Standalone Reverse-Engineering.
Current State: Milestone M1 is DONE.
Immediate Task:
1. Initialize working directory: create BRIEFING.md, progress.md, and start recurring heartbeat cron.
2. Read orchestrator_25's handoff.md.
3. Launch Milestone M2 (Standalone Asset & Template Migration):
   - Dispatch Worker M2 to copy all 17 DWGs, 132 XLSMs, MayHoc/mauhoc.db + model.zip, 178 System files, 8 Language dictionaries, 133 Icons, and 7 CUIX menus from C:\Dce Pro into HPDcePro/assets/.
   - Update file path resolvers in HPDcePro/src/ to resolve relative to HPDcePro/assets/ and AppContext.BaseDirectory so that HPDcePro operates 100% independently with 0 hardcoded references to C:\Dce Pro.
   - Run static and runtime grep across HPDcePro/src/** and HPDcePro/assets/** to confirm zero runtime dependency on C:\Dce Pro.
   - Execute M2 Gate verification (Reviewers, Challengers, Forensic Auditor).
4. Advance through Milestone M3 (1:1 AutoCAD, 11 Jigs, 12 UI Forms/Palettes, 24 AutoStr steps), Milestone M4 (1:1 Revit & Excel), and Milestone M5 (Final Build & Forensic Integrity Audit) as specified in PROJECT.md.
