# DISPATCH — worker_m1_3 (Truth Matrix Remediation Specialist Iteration 3)

## Task Assignment
Remediate all defects identified by Reviewer M1.2 (gen2) and Challenger M1.1 in `docs/rea-dce-pro/TRUTH_MATRIX.md`:
1. Reclassify 23 endpoints in Subsystem 3 (lines 464–487) from `HOÀN THÀNH` to `CHƯA GIẢI MÃ ĐƯỢC` with token `0x02000B90` and target `src/HPDcePro.Core/Rebar/RebarEditingService.cs (Milestone M3 clean-room target)`.
2. Reclassify 3 endpoints in Subsystem 2 (lines 368–370: `Col_ExportDrawingData`, `Col_ExportSize`, `Sla_ExportSize`) from `HOÀN THÀNH` to `CHƯA GIẢI MÃ ĐƯỢC` with target `src/HPDcePro.Excel/ClosedXmlReportExporter.cs (Milestone M4 target)`.
3. Fix the 16 unclosed backticks in Subsystem 2 descriptions (lines 364, 368, 369, 372, 377, 380, 386, 387, 388, 394, 401, 404, 405, 407, 408, 409).
4. Shift Subsystem 5 export ordinals from `(Ord: 0..68)` to `(Ord: 1..69)` (1-based PE Export Address Table `Base = 1`). Also annotate target for 4 excluded anti-tamper endpoints (lines 530, 532, 538, 545) as `N/A (Excluded per Clean-Room Governance)`.
5. Update Section 1 Summary Statistics Table and each subsystem section header:
   - Subsystem 1: 0 HOÀN THÀNH, 64 ĐANG LÀ MOCK, 247 CHƯA GIẢI MÃ ĐƯỢC (Total 311)
   - Subsystem 2: 2 HOÀN THÀNH, 0 ĐANG LÀ MOCK, 71 CHƯA GIẢI MÃ ĐƯỢC (Total 73)
   - Subsystem 3: 14 HOÀN THÀNH, 6 ĐANG LÀ MOCK, 29 CHƯA GIẢI MÃ ĐƯỢC (Total 49)
   - Subsystem 4: 6 HOÀN THÀNH, 7 ĐANG LÀ MOCK, 16 CHƯA GIẢI MÃ ĐƯỢC (Total 29)
   - Subsystem 5: 65 HOÀN THÀNH, 0 ĐANG LÀ MOCK, 4 CHƯA GIẢI MÃ ĐƯỢC (Total 69)
   - Subsystem 6: 1 HOÀN THÀNH, 2 ĐANG LÀ MOCK, 21 CHƯA GIẢI MÃ ĐƯỢC (Total 24)
   - Subsystem 7: 0 HOÀN THÀNH, 11 ĐANG LÀ MOCK, 0 CHƯA GIẢI MÃ ĐƯỢC (Total 11)
   - Subsystem 8: 0 HOÀN THÀNH, 12 ĐANG LÀ MOCK, 0 CHƯA GIẢI MÃ ĐƯỢC (Total 12)
   - Subsystem 9: 16 HOÀN THÀNH, 8 ĐANG LÀ MOCK, 0 CHƯA GIẢI MÃ ĐƯỢC (Total 24)
   - Grand Total: 104 HOÀN THÀNH, 110 ĐANG LÀ MOCK, 388 CHƯA GIẢI MÃ ĐƯỢC (Total 602).
6. Run `python tools/verification/run_all_truth_matrix_verifications.py` and `dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` to verify.

## File Ownership
- Exclusive write ownership: `docs/rea-dce-pro/TRUTH_MATRIX.md`.
- Read-only on `C:\Dce Pro\`.

## 2026-10-18T06:45:29Z
You are Worker M1.3 for Milestone M1 Iteration 3 (Master Truth Matrix Remediation).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_3\
Read your dispatch specification at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_3\DISPATCH.md
Read the review findings:
- Reviewer M1.2 gen2: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m1_2_gen2\handoff.md
- Challenger M1.1: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m1_1\handoff.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (`## 2026-10-18T05:27:06Z`).

Your exclusive file write ownership is: `docs/rea-dce-pro/TRUTH_MATRIX.md`.
Strictly read-only on `C:\Dce Pro`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Mission:
1. In `docs/rea-dce-pro/TRUTH_MATRIX.md`:
   - Subsystem 3 (lines 464–487): Reclassify all 23 endpoints (`MatTienThep.DoiTenCauKien` to `MatTienThep.CopyKhoangRaiThep`) from `HOÀN THÀNH` to `CHƯA GIẢI MÃ ĐƯỢC`. Set token to `0x02000B90` and target to `src/HPDcePro.Core/Rebar/RebarEditingService.cs (Milestone M3 clean-room target)`.
   - Subsystem 2 (lines 368–370): Reclassify `Col_ExportDrawingData`, `Col_ExportSize`, `Sla_ExportSize` from `HOÀN THÀNH` to `CHƯA GIẢI MÃ ĐƯỢC`. Target: `src/HPDcePro.Excel/ClosedXmlReportExporter.cs (Milestone M4 target)`.
   - Subsystem 2 descriptions: Fix the 16 unclosed backticks (lines 364, 368, 369, 372, 377, 380, 386, 387, 388, 394, 401, 404, 405, 407, 408, 409).
   - Subsystem 5: Shift export ordinals from `(Ord: 0)` through `(Ord: 68)` to `(Ord: 1)` through `(Ord: 69)` to reflect 1-based PE Export Address Table (`Base = 1`). For the 4 clean-room excluded anti-tamper endpoints (lines 530, 532, 538, 545), annotate target as `N/A (Excluded per Clean-Room Governance)`.
   - Section 1 Summary Table & Subsystem section headers:
     * Subsystem 1: 0 HOÀN THÀNH, 64 ĐANG LÀ MOCK, 247 CHƯA GIẢI MÃ ĐƯỢC (Total 311)
     * Subsystem 2: 2 HOÀN THÀNH, 0 ĐANG LÀ MOCK, 71 CHƯA GIẢI MÃ ĐƯỢC (Total 73)
     * Subsystem 3: 14 HOÀN THÀNH, 6 ĐANG LÀ MOCK, 29 CHƯA GIẢI MÃ ĐƯỢC (Total 49)
     * Subsystem 4: 6 HOÀN THÀNH, 7 ĐANG LÀ MOCK, 16 CHƯA GIẢI MÃ ĐƯỢC (Total 29)
     * Subsystem 5: 65 HOÀN THÀNH, 0 ĐANG LÀ MOCK, 4 CHƯA GIẢI MÃ ĐƯỢC (Total 69)
     * Subsystem 6: 1 HOÀN THÀNH, 2 ĐANG LÀ MOCK, 21 CHƯA GIẢI MÃ ĐƯỢC (Total 24)
     * Subsystem 7: 0 HOÀN THÀNH, 11 ĐANG LÀ MOCK, 0 CHƯA GIẢI MÃ ĐƯỢC (Total 11)
     * Subsystem 8: 0 HOÀN THÀNH, 12 ĐANG LÀ MOCK, 0 CHƯA GIẢI MÃ ĐƯỢC (Total 12)
     * Subsystem 9: 16 HOÀN THÀNH, 8 ĐANG LÀ MOCK, 0 CHƯA GIẢI MÃ ĐƯỢC (Total 24)
     * Grand Total: 104 HOÀN THÀNH, 110 ĐANG LÀ MOCK, 388 CHƯA GIẢI MÃ ĐƯỢC (Total 602).
2. Run `python tools/verification/run_all_truth_matrix_verifications.py` to confirm all verification checks pass.
3. Run `dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` to confirm all 369 tests pass.
4. Document all changes and verification output in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_3\handoff.md`.
Communicate your completion back to parent via send_message.
