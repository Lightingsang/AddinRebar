# Dispatch Specification — Milestone M2 Forensic Auditor (auditor_hpkata_m2_1)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_hpkata_m2_1\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Test Certification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md`

## Mission:
Perform rigorous forensic integrity audit on Milestone M2 (Features 5–8):
1. **Static Analysis & Genuine Logic Verification**:
   - Verify that `HPKata.Core/Excel/` implementations (`KataExcelAdapter`, `KataBeamSheetReader/Writer`, `KataColumnSheetReader/Writer`, `KataWallSheetReader/Writer`, `KataScheduleSheetReader/Writer`, `DesktopExcelSession`, `ClosedXmlExcelSession`) contain genuine parsing, writing, and session logic.
   - Verify there are NO dummy implementations, NO hardcoded test return values, NO fake stubs, NO facade classes.
2. **Anti-Cheating & Clean Code Audit**:
   - Verify 0 dead/commented-out code blocks (Q-B1).
   - Verify 0 empty catch blocks without reason comments (Q-B2).
   - Verify all `.cs` files <= 300 lines (Rule C2) and methods <= 50 lines (Rule M3).
   - Verify 0 occurrences of `kata_pro` (case-insensitive) across all newly created or touched files.
3. **UI & Host Commands Integrity**:
   - Verify `HPKata.UI` strictly retains `System.Windows.Window` + `MaterialDesignThemes` v5.2.1, zero `MahApps.Metro`, and all `Theme.xaml` keys.
   - Verify AutoCAD and Revit detailing commands genuinely call `IKataExcelAdapter` and create authentic geometry with `KATA` XData.
4. Formulate your binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.
5. Write your report to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_hpkata_m2_1\handoff.md`.

## 2026-10-18T04:25:09Z
You are auditor_hpkata_m2_1 (Milestone M2 Forensic Auditor).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_hpkata_m2_1\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_hpkata_m2_1\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Features 5–8).
Test Certification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Worker Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2\handoff.md
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
