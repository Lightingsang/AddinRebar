# Dispatch: reviewer_m4_2

## Role & Mission
You are `reviewer_m4_2`, reviewing Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities) focusing on Functional Completeness & Build/Test Verification.
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_2\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Authoritative Reference Sources
1. Master user request at: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. Master project scope at: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Worker changes & handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_qs_layout_utils\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_qs_layout_utils\handoff.md`

## Your Review Mission
1. Functional Verification:
   - Verify coverage of all 27 Phase 5 AutoCAD commands and aliases in `GoiLenh_HPKata.lsp`: `QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `timqs`, `tkqs`, `vg`, `ve_luoi_truc`, `sbv`, `sap_xep_ban_ve`, `ibv`, `chen_khung_ten`, `taomv`, `taomv1`, `khoav`, `mokhoav`, `danhmuc`, `catdim`, `cat_chan_dim`, `ngatdim`, `ngat_dim`, `dnoi`, `dim_noi_tiep`, `sl`, `ghi_do_doc`, `dtext`, `can_chinh_text`, `mcat`, `cao_do_mat_cat`, `anlayer`, `hienlayer`, `doilayer`, `clean_kata`.
   - Verify genuine AutoCAD controllers without simulation stubs.
   - Verify Revit services (`RevitQsTakeoffService`, `RevitSheetArrangementService`).
   - Verify ClosedXML workbook generator for sheets `Kata_QS` and `Thong_ke_QS`.
2. Build & Test Verification:
   - Run: `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` (must pass with 0 errors, 0 warnings).
   - Run: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"` (all 862 tests must pass 100% green).
3. Provide explicit verdict: APPROVE or REQUEST_CHANGES.

## Deliverable
Write your review report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_2\review.md`
and a concise `handoff.md` in your working directory.
When complete, notify parent via `send_message`.


## 2026-10-17T12:24:04Z
Review Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities) focusing on Functional Completeness & Build/Test Verification.
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_2\
Parent conversation ID: a7a2dbbb-6b7d-4279-98cd-1216d8280d80
Verify all 27 Phase 5 commands/aliases, genuine controllers, Revit services, ClosedXML generator, build & test (all 862 tests pass 100% green).
