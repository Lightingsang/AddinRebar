# Dispatch Specification — M2 Iteration 2 Explorer (explorer_hpkata_m2_rem_1)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_1\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## FORENSIC AUDIT FAILURE EVIDENCE REPORT (FULL AND UNFILTERED)
The Forensic Auditor (`auditor_hpkata_m2_1`) reported INTEGRITY VIOLATION with the following evidence:
1. **Rule Q-B2 Violation**: `HPKata/HPKata.Core/Excel/Session/KataComExcelSession.cs:154`:
   `try { ComLateBindingHelper.InvokeMethod(_excelApp, "Quit"); } catch { }`
   Contains an unannotated empty catch block with no comment and no statement.
2. **Rule M3 Violation**: `HPKata.Core/Excel/Mappers/KataColumnDataMapper.cs:12`:
   `ToRebarLayout` is 51 lines long (lines 12–62), exceeding the 50-line limit.
3. **Challenger Finding**: `KataExcelAdapter.cs` lacks `try/catch` around `OpenSession`, causing unhandled exceptions on corrupt/0-byte files. Ensure `C:\HPKata\Kata.xlsm` is intact from `HPKata/Distribution/Assets/Kata.xlsm`.

## Mission:
1. Inspect `KataComExcelSession.cs` and design the annotated catch block to satisfy Rule Q-B2.
2. Inspect `KataColumnDataMapper.cs` and design the extraction of a helper method (e.g. `MapStorySegment`) to bring `ToRebarLayout` well under 40 lines.
3. Inspect `KataExcelAdapter.cs` and design exception handling around session opening to return `false` gracefully on corrupt/inaccessible files.
4. Verify that all `.cs` files are <= 300 lines (Rule C2), all methods are <= 50 lines (Rule M3).
5. Write your report to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_1\handoff.md`.
Message parent when done.


## 2026-10-18T04:40:43Z
Message received from parent 3c323530-716e-4b3b-a14c-2a7fcee0c68a:
Full unfiltered auditor evidence report:
1. Rule Q-B2: HPKata/HPKata.Core/Excel/Session/KataComExcelSession.cs:154 contains `try { ComLateBindingHelper.InvokeMethod(_excelApp, "Quit"); } catch { }` without comment or statement.
2. Rule M3: HPKata.Core/Excel/Mappers/KataColumnDataMapper.cs:12 `ToRebarLayout` is 51 lines long (> 50 lines limit).
3. Challenger Finding: KataExcelAdapter.cs lacks try/catch around OpenSession, throwing unhandled exceptions on corrupt files. Ensure C:\HPKata\Kata.xlsm is restored from HPKata/Distribution/Assets/Kata.xlsm.
