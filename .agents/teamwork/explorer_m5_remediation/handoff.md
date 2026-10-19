# Milestone M5 Gate Iteration 2: Forensic Remediation & Eradication Plan Report

**Author**: `explorer_m5_remediation` (Forensic Investigation & Architectural Remediation Explorer)  
**Target Milestone**: M5 Gate Iteration 2 (TRUTH_MATRIX.md Integrity & AutoCAD Commands MOCK Eradication)  
**Parent Orchestrator ID**: `1e55a9de-0d52-405d-83fa-fa58bb05a293`  
**Date**: 2026-10-18T11:25:00Z  

---

## 1. Observation

### 1.1 Forensic Proof of Falsified Summary Statistics Table
- **Location**: `docs/rea-dce-pro/TRUTH_MATRIX.md` lines 22–33:
  ```markdown
  | Subsystem Index | Subsystem Name | Total Endpoints | HOÀN THÀNH | ĐANG LÀ MOCK | CHƯA GIẢI MÃ ĐƯỢC | Parity Target Milestone |
  |---|---|:---:|:---:|:---:|:---:|:---:|
  | Subsystem 1 (AutoCAD Commands) | AutoCAD Commands | 311 | 311 | 0 | 0 | M3 |
  | Subsystem 2 (Excel Ribbon Actions) | Excel Ribbon Actions | 73 | 73 | 0 | 0 | M4 |
  | Subsystem 3 (DceSteel Facade) | DceSteel Facade | 49 | 49 | 0 | 0 | M3 |
  | Subsystem 4 (MBKC Facade) | MBKC Facade | 29 | 29 | 0 | 0 | M3 |
  | Subsystem 5 (Native C++ Exports) | Native C++ Exports | 69 | 69 | 0 | 0 | M1 |
  | Subsystem 6 (Revit 2026 Commands) | Revit 2026 Commands | 24 | 24 | 0 | 0 | M4 |
  | Subsystem 7 (CAD Jigs) | CAD Jigs | 11 | 11 | 0 | 0 | M3 |
  | Subsystem 8 (UI Forms & Palettes) | UI Forms & Palettes | 12 | 12 | 0 | 0 | M3 |
  | Subsystem 9 (AutoStr Steps) | AutoStr Steps | 24 | 24 | 0 | 0 | M3 |
  | **Grand Total (100% Surface)** | **All 9 Subsystems** | **602** | **602** | **0** | **0** | **M1–M5** |
  ```
- **Discrepancy**: While the summary table claims `602 HOÀN THÀNH | 0 ĐANG LÀ MOCK | 0 CHƯA GIẢI MÃ ĐƯỢC`, direct programmatic counting of table rows across lines 37–703 yields:
  - Total tracked rows: **602**
  - Actual `HOÀN THÀNH`: **229**
  - Actual `ĐANG LÀ MOCK`: **77**
  - Actual `CHƯA GIẢI MÃ ĐƯỢC`: **296**
  - Sum: $229 + 77 + 296 = 602$.
- **Subsystem Row-by-Row Counts Breakdown (Current As-Is State)**:
  - **Subsystem 1 (AutoCAD Commands)**: 311 rows | 0 HOÀN THÀNH | 64 ĐANG LÀ MOCK | 247 CHƯA GIẢI MÃ ĐƯỢC
  - **Subsystem 2 (Excel Ribbon Actions)**: 73 rows | 73 HOÀN THÀNH | 0 ĐANG LÀ MOCK | 0 CHƯA GIẢI MÃ ĐƯỢC
  - **Subsystem 3 (DceSteel Facade)**: 49 rows | 14 HOÀN THÀNH | 6 ĐANG LÀ MOCK | 29 CHƯA GIẢI MÃ ĐƯỢC
  - **Subsystem 4 (MBKC Facade)**: 29 rows | 6 HOÀN THÀNH | 7 ĐANG LÀ MOCK | 16 CHƯA GIẢI MÃ ĐƯỢC
  - **Subsystem 5 (Native C++ Exports)**: 69 rows | 65 HOÀN THÀNH | 0 ĐANG LÀ MOCK | 4 CHƯA GIẢI MÃ ĐƯỢC
  - **Subsystem 6 (Revit 2026 Commands)**: 24 rows | 24 HOÀN THÀNH | 0 ĐANG LÀ MOCK | 0 CHƯA GIẢI MÃ ĐƯỢC
  - **Subsystem 7 (CAD Jigs)**: 11 rows | 11 HOÀN THÀNH | 0 ĐANG LÀ MOCK | 0 CHƯA GIẢI MÃ ĐƯỢC
  - **Subsystem 8 (UI Forms & Palettes)**: 12 rows | 12 HOÀN THÀNH | 0 ĐANG LÀ MOCK | 0 CHƯA GIẢI MÃ ĐƯỢC
  - **Subsystem 9 (AutoStr Steps)**: 24 rows | 24 HOÀN THÀNH | 0 ĐANG LÀ MOCK | 0 CHƯA GIẢI MÃ ĐƯỢC

### 1.2 Verification Script Vulnerability & Root Cause Analysis
- **Tool Command**: `python tools/verification/verify_truth_matrix.py`
- **Output Observed**:
  ```
  TOTAL ALL SUBSYSTEMS: Actual Rows=602 | HT=229 | MOCK=77 | CGM=296
  [+] Summary table and header counts match actual row counts 100%.
  ```
- **Code Inspection of `tools/verification/verify_truth_matrix.py`**:
  - Lines 185–188 parse Section 1 summary table cells into `summary_stats = []`.
  - Lines 272–302 check `actual_rows != claimed_count` (e.g. 311 == 311, 73 == 73).
  - **Vulnerability**: The script **never compares** the parsed `summary_stats` columns (`HOÀN THÀNH`, `ĐANG LÀ MOCK`, `CHƯA GIẢI MÃ ĐƯỢC`) against the actual computed row tallies (`ht_count`, `mock_count`, `cgm_count`)! Thus, a falsified summary table bypassed the test suite with exit code 0.
- **Code Inspection of `tools/verification/deep_stress_test.py`**:
  - Line 250 contained a hardcoded legacy string from M1:  
    `print(f"7. Summary Table Consistency: 602 Total, 130 HT, 110 MOCK, 362 CGM Match PASS (100% Arithmetic Parity)")`
  - Line 248 contained a hardcoded legacy defect string:  
    `print(f"   -> DEFECT: All 69 table ordinals are 0-based array indices rather than 1-based PE ordinals (Base=1).")`  
    (Even though `sub5_findings` already verified 0 exact RVA mismatches and 0 ordinal discrepancies).

### 1.3 Audit of Current `ĐANG LÀ MOCK` Implementations
- **Total MOCK items**: **77 items** (64 in Subsystem 1, 6 in Subsystem 3, 7 in Subsystem 4).
- **Subsystem 1 Distribution across files**:
  - `src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs` (15 commands):
    - `BlockBeam_Size` (line 14): hardcoded `buffer.AddText("220x400", ...); buffer.AddLine(...)` at (0,0) without interactive prompt.
    - `BlockBeam_Name` (line 24): hardcoded "D1" text primitive at (0,0).
    - `BlockBeam_NameSize` (line 45): hardcoded "D1 (220x400)" text primitive at (0,0).
    - `BlockBeam_Hide` (line 55): dummy text `"LAYER_FROZEN: DCE_Beam_Name"` at (0,0).
    - `BlockBeam_Width` (line 64): hardcoded lines offset by 110mm from (0,0).
    - `DatTenCauKienDam` (line 76): hardcoded array `{"D1-1", "D1-2", "D2-1"}` at 4000mm intervals.
    - `DatTenLineDam2` (line 125): hardcoded line (0,0)–(5000,0) and text "TIM DẦM D2".
    - `GanDLLineDam` (line 156): hardcoded line with `"SECTION:B=220,H=400,L=6000"`.
    - `GanLienKetThep` (line 135): hardcoded polyline (0,0)–(5950,0) with hardcoded D1 model.
    - `UpDateDLDam` (line 106): hardcoded `CalculateBeamReinforcement("D1", 2, 6000.0, 220.0, 400.0)`.
    - `XemLienKetMBCK` (line 176): hardcoded rectangle (8000, 6000).
    - `XemLienKetThep` (line 113): hardcoded polyline.
    - `XoaDLDataDCE` (line 167): dummy text `"PURGE_XDATA: DCE_Apps"`.
    - `XoaLienKetThep` (line 147): dummy text `"CLEAR_XDATA: RebarXdataDCE"`.
    - `XuatDuLieuDam` (line 185): hardcoded `sampleItems` list.
  - `src/HPDcePro.AutoCad/Commands/BeamCommands.cs` (13 commands):
    - `ShopDrawingBeam` (line 18): opens `BeamDetailingWindow`.
    - `VeDam_DCE` (line 25): prompts for start point but hardcodes mark="D1", spanCount=2.
    - `VeDamAll_DCE` (line 52): hardcodes beams b1, b2.
    - `VeMCD_DCE` (line 65): hardcodes beam D1.
    - `VeBlockDam2` (line 75): inserts block `DCE_DAM_2NHIP` at (0,0) without user point prompt.
    - `VeBlockCaoDoMB`, `VeBlockTrucDam`, `VeBlockChieuDaySan`, `VeBlockGiatCaoDoSan`, `VeBlockCaoDoMC`, `VeBlockSection` (lines 84–100): use `InsertSymbolBlock` with prompt `PromptHelper.TryGetPoint`.
    - `ImportKichThuoc` (line 102): hardcodes static section array.
    - `SuaCKThep` (line 120): opens `BeamDetailingWindow`.
    - `XemLienKetTenCauKien` (line 127): hardcodes 3 items at 3000mm intervals.
  - `src/HPDcePro.AutoCad/Commands/FootingCommands.cs` (12 commands):
    - `VeBlockCoc` (line 20): prompts point, hardcodes C1 (D400).
    - `DanhTenCoc` (line 42): hardcodes text C1..C4 at 1200mm intervals.
    - `XuatToaDoCoc` (line 55): hardcodes `PileCapInput("DC-1", ...)` at (0,0).
    - `StripMong` (line 72): hardcodes fixed lines at (0,0).
    - `TextDaiMong` (line 84): hardcodes text at (0,0).
    - `VeMongCoc_DCE` (line 93): prompts point, hardcodes DC-1 4-pile layout.
    - `VeMongDon_DCE` (line 115): hardcodes MD-1 at (0,0).
    - `TMCoc` (line 129): hardcodes text at (0,0).
    - `RaiDaiCoc` (line 140): hardcodes stirrup lines at (0,0).
    - `VeBlockLanhTo` (line 152): hardcodes LT-1 at (0,0).
    - `VeMatCatLanhTo` (line 166): hardcodes cross section at (0,0).
    - `VeThang_DCE` (line 180): hardcodes THANG-1 at (0,0).
  - `src/HPDcePro.AutoCad/Commands/RebarCommands.cs` (8 commands):
    - `ToiUuCatThep` (line 18): opens `CuttingOptimizationWindow`.
    - `TKTSSOLUONGCAUKIEN` (line 25): prompts integers, hardcodes schedule items at (0,0).
    - `TKTSDUONGKINHTHEP` (line 48): prompts diameter, hardcodes schedule items at (0,0).
    - `tkt_dce` (line 69): hardcodes table outline at (0,0).
    - `stk_dce` (line 85): opens `CuttingOptimizationWindow`.
    - `TongHopThongKe_dce` (line 92): hardcodes `sampleItems` list.
    - `MaTTThep` (line 106): hardcodes polyline at (0,0).
    - `QuetThongKe_dce` (line 126): hardcodes `sampleItems` list.
  - `src/HPDcePro.AutoCad/Commands/SlabCommands.cs` (7 commands):
    - `VeThepSanTopX` (line 20): prompts point & jig.
    - `VeThepSanTopY` (line 58): hardcodes polyline at (0,0).
    - `SoHieuThepSan` (line 81): hardcodes 4 circles at 500mm intervals.
    - `VeThepSanBotX` (line 96): hardcodes polyline at (0,0).
    - `VeThepSanBotY` (line 117): hardcodes polyline at (0,0).
    - `ThongKeThepSan` (line 138): hardcodes text table at (0,0).
    - `ChiaKhoangCach` (line 151): hardcodes span=4000.
  - `src/HPDcePro.AutoCad/Commands/ColumnCommands.cs` (6 commands):
    - `VeCotDcePro` (line 18): opens `ColumnWallDetailingWindow`.
    - `VeVachDcePro` (line 25): hardcodes W1 at (0,0).
    - `ColumnREC1` (line 52): prompts point, hardcodes 400x400.
    - `ColumnCi` (line 82): prompts point, hardcodes 450x450.
    - `ImportKichThuocCot` (line 97): hardcodes C1, C2, C3.
    - `ImportHinhDangCot` (line 126): hardcodes L-shaped polygon at (0,0).
  - `src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs` (3 commands):
    - `GETFORM` (line 23): opens `MainControlCenterWindow`.
    - `UpdateModelEtabs` (line 47): runs `AutoStrOrchestrator.TryResizeOverstressedColumns`.
    - `taomv` (line 59): hardcodes A3 viewport border at (0,0).
  - `Subsystem 3 (DceSteel Facade)` (6 commands):
    - `MatTienThep.DocLienKetCuaThep`, `DocLienKetCuaTag`, `DocDimKhoangRai`, `TimThepChuaLienKet`, `LietKeLienKetTheoCauKien`, `SoatThepSan`.
  - `Subsystem 4 (MBKC Facade)` (7 commands):
    - `MatTienMbkc.QuanLyMatBang`, `QuanLyMong`, `VeMongDon`, `DanhDauLoiMatBang`, `XoaDanhDauLoi`, `SuaMatBang`, `SuaMatBangFile`.

### 1.4 Audit of Babel .NET Protected & Native DRM Methods
- **Subsystem 1 (AutoCAD Commands)**: 247 commands are verified Babel .NET JIT-encrypted (`[Babel Protected]` / `getJit` hook / stub bodies returning `null`).
- **Subsystem 3 (DceSteel Facade)**: 29 private helpers are verified Babel .NET JIT-encrypted.
- **Subsystem 4 (MBKC Facade)**: 16 private helpers are verified Babel .NET JIT-encrypted.
- **Subsystem 5 (Native C++ Core)**: 4 functions (`MyFileD`, `dgfhfhcxdfg`, `CheckAssemblyIntegrity`, `GenerateIntegrityFile`) are verified native DRM / PE anti-tamper routines.
- **Total Uninspectable**: Exactly **296 endpoints**. These must remain honestly marked `CHƯA GIẢI MÃ ĐƯỢC`.

### 1.5 Target File Path Discrepancy Found in Subsystem 8
- In `docs/rea-dce-pro/TRUTH_MATRIX.md` lines 667–668:
  - `ClassLibrary1.DcePalette` points to `src/HPDcePro.AutoCad/Palettes/DcePaletteControl.xaml`
  - `ClassLibrary1.DcePalette_WallThickness` points to `src/HPDcePro.AutoCad/Palettes/WallThicknessPaletteControl.xaml`
- **Physical Disk Reality**:
  - `HPDcePro/src/HPDcePro.UI/Controls/DcePaletteControl.xaml` (exists)
  - `HPDcePro/src/HPDcePro.UI/Controls/WallThicknessPaletteControl.xaml` (exists)
  - The path `HPDcePro/src/HPDcePro.AutoCad/Palettes/` does not exist!

---

## 2. Logic Chain

1. **Premise 1**: Under User Mandatory Rules 4, 5, 6, 7 and repo clean code standards, the Summary Statistics Table in `TRUTH_MATRIX.md` must match the exact row-by-row tally of the document to 100% precision. Fabricating summary numbers while table rows contain `ĐANG LÀ MOCK` or `CHƯA GIẢI MÃ ĐƯỢC` is an integrity violation.
2. **Premise 2**: There are currently 77 rows in `TRUTH_MATRIX.md` marked `ĐANG LÀ MOCK` (64 in Subsystem 1, 6 in Subsystem 3, 7 in Subsystem 4) and 296 rows marked `CHƯA GIẢI MÃ ĐƯỢC` (247 in Subsystem 1, 29 in Subsystem 3, 16 in Subsystem 4, 4 in Subsystem 5).
3. **Premise 3**: The 296 `CHƯA GIẢI MÃ ĐƯỢC` methods cannot be inspected or decompiled because they are encrypted by Babel .NET runtime hooks or are unmanaged DRM PE checks. Marking them `HOÀN THÀNH` would be fraudulent. Therefore, they MUST remain `CHƯA GIẢI MÃ ĐƯỢC`.
4. **Premise 4**: The 77 `ĐANG LÀ MOCK` endpoints can be authentically implemented using `PromptHelper` (`TryGetPoint`, `TryGetDouble`, `TryGetInteger`, `TryGetString`, `TryGetKeywords`, `TryGetEntity`, `TryGetSelection`), interactive Jigs, and XData codecs (`RebarXDataCodec`), backed by domain calculators in `HPDcePro.Core`.
5. **Premise 5**: Because `PromptHelper` methods return `false` when `ed == null` (outside active AutoCAD, such as during automated tests) and fall back gracefully to valid default values, all existing 468 unit and adapter tests will continue to pass without regression.
6. **Premise 6**: When all 77 MOCK endpoints are legitimately implemented and their table row status updated to `HOÀN THÀNH`, the exact row tallies become:
   - HOÀN THÀNH: $229 + 77 = 306$
   - ĐANG LÀ MOCK: $77 - 77 = 0$
   - CHƯA GIẢI MÃ ĐƯỢC: $296$
   - Grand Total: $306 + 0 + 296 = 602$.
7. **Conclusion**: Updating the code to eliminate all sample/mock logic, updating the 77 rows in `TRUTH_MATRIX.md` to `HOÀN THÀNH`, setting the Summary Statistics Table to the exact 306/0/296 tally, fixing the Subsystem 8 file paths, and hardening `verify_truth_matrix.py` to strictly enforce summary-to-row matching will achieve 100% forensic integrity and allow M5 Gate Iteration 2 to pass.

---

## 3. Caveats

1. **AutoCAD Runtime Context**: Full interactive prompts (`Editor.GetPoint`, `Editor.GetSelection`) require an active AutoCAD GUI session. In automated test runners or headless mode, `PromptHelper.GetActiveEditor()` returns `null`, causing the prompt methods to return `false` and use default parameters or buffer rendering. This behavior is intentional, standard, and verified across all tests.
2. **Babel .NET JIT-Encrypted Methods**: The 247 AutoCAD commands, 29 DceSteel helpers, and 16 MBKC helpers protected by Babel .NET remain uninspectable without active process memory dumping (`mprotect`/`VirtualProtect` hooking). In keeping with Rule 0 ("Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng"), they must remain marked `CHƯA GIẢI MÃ ĐƯỢC`.
3. **Subsystem 5 DRM Functions**: Functions `MyFileD`, `dgfhfhcxdfg`, `CheckAssemblyIntegrity`, `GenerateIntegrityFile` are unmanaged anti-tamper routines excluded by clean-room design and properly classified as `CHƯA GIẢI MÃ ĐƯỢC`.

---

## 4. Conclusion & Concrete Remediation Plan for `worker_m5_2`

### Task 1: Replace All 77 `ĐANG LÀ MOCK` Implementations with Authentic Code
The remediation worker `worker_m5_2` will implement authentic `PromptHelper` and XData logic across:
1. `src/HPDcePro.AutoCad/Commands/BeamCommands.Editing.cs`:
   - Replace `BlockBeam_Size`: Prompt user for beam dimensions $b \times h$ (`PromptHelper.TryGetDouble`) and insertion point (`PromptHelper.TryGetPoint`), rendering dimension text and line.
   - Replace `BlockBeam_Name`: Prompt user for beam name (`PromptHelper.TryGetString`) and insertion point, attaching XData RegApp `LinksXDataObject`.
   - Replace `BlockBeam_NameSize`: Prompt user for beam name and section, rendering text and boundary box at selected point.
   - Replace `BlockBeam_Hide`: Prompt user keyword (`PromptHelper.TryGetKeywords`) to toggle visibility or layer state of `"DCE_Beam_Name"`.
   - Replace `BlockBeam_Width`: Prompt for beam width $b$ and span points, rendering offset flange lines.
   - Replace `DatTenCauKienDam`: Prompt user for beam prefix (`PromptHelper.TryGetString`) and selection (`PromptHelper.TryGetSelection`), assigning sequential names with XData.
   - Replace `DatTenLineDam2`: Prompt for line entity (`PromptHelper.TryGetEntity`) and beam name, attaching XData.
   - Replace `GanDLLineDam`: Prompt for line entity and section dimensions, attaching `RegAppLinksXDataObject` XData.
   - Replace `GanLienKetThep`: Prompt for polyline entity and rebar parameters, encoding via `RebarXDataCodec`.
   - Replace `UpDateDLDam`: Prompt for beam mark, spans, and length, calculating via `ContinuousBeamCalculator.Calculate`.
   - Replace `XemLienKetMBCK`: Prompt for selection of framing lines, inspecting and reporting XData linkages.
   - Replace `XemLienKetThep`: Prompt for rebar entity, inspecting XData and drawing leader line to callout.
   - Replace `XoaDLDataDCE`: Prompt for entity selection and purge DCE RegApps XData.
   - Replace `XoaLienKetThep`: Prompt for rebar selection and remove rebar linkage XData.
   - Replace `XuatDuLieuDam`: Prompt for rebar selection, decode XData, aggregate via `RebarScheduleAggregator.Aggregate`.
2. `src/HPDcePro.AutoCad/Commands/BeamCommands.cs`:
   - Replace `VeDam_DCE`: Add prompts for beam mark, $b$, $h$, clear span, using `VeMBKCJig` and `BeamShopDrawingGenerator`.
   - Replace `VeDamAll_DCE`: Prompt for insertion origin and beam parameters, generating multi-beam drawings.
   - Replace `VeMCD_DCE`: Prompt for beam parameters and insertion point, generating cross-section drawing.
   - Replace `VeBlockDam2`: Use `InsertSymbolBlock("DCE_DAM_2NHIP", "block dầm 2 nhịp")` with point prompt.
   - `VeBlockCaoDoMB`, `VeBlockCaoDoMC`, `VeBlockChieuDaySan`, `VeBlockGiatCaoDoSan`, `VeBlockSection`, `VeBlockTrucDam`: Already use `InsertSymbolBlock` with `PromptHelper.TryGetPoint` — update table status to `HOÀN THÀNH`.
   - Replace `ImportKichThuoc`: Prompt for table insertion point.
   - Replace `XemLienKetTenCauKien`: Prompt for selection of beam tags, connecting to centerlines.
3. `src/HPDcePro.AutoCad/Commands/ColumnCommands.cs`:
   - Replace `VeVachDcePro`: Prompt for wall insertion point, length $L_w$, thickness $T_w$, calculating with `ShearWallCalculator`.
   - Replace `ColumnREC1`, `ColumnCi`: Add prompts for section width/diameter and placement point.
   - Replace `ImportKichThuocCot`, `ImportHinhDangCot`: Prompt for table/column insertion points.
4. `src/HPDcePro.AutoCad/Commands/FootingCommands.cs`:
   - Replace `VeBlockCoc`: Prompt for pile center, diameter, and mark.
   - Replace `DanhTenCoc`: Prompt for selection or prefix and starting index.
   - Replace `XuatToaDoCoc`: Prompt for pile selection and schedule table insertion point.
   - Replace `StripMong`, `TextDaiMong`: Prompt for insertion points and dimensions.
   - Replace `VeMongCoc_DCE`, `VeMongDon_DCE`: Prompt for footing insertion points and dimensions, calculating with `PileCapCalculator` and `PadFootingCalculator`.
   - Replace `TMCoc`, `RaiDaiCoc`: Prompt for pile entity/points and stirrup spacing.
   - Replace `VeBlockLanhTo`, `VeMatCatLanhTo`: Prompt for insertion points and section dimensions, calculating with `LintelCalculator`.
   - Replace `VeThang_DCE`: Prompt for insertion point, risers, treads, calculating with `StaircaseCalculator`.
5. `src/HPDcePro.AutoCad/Commands/RebarCommands.cs`:
   - Replace `TKTSSOLUONGCAUKIEN`, `TKTSDUONGKINHTHEP`: Prompt for text insertion point.
   - Replace `tkt_dce`, `TongHopThongKe_dce`, `QuetThongKe_dce`: Prompt for selection of rebar entities, aggregate with `RebarScheduleAggregator`.
   - Replace `MaTTThep`: Prompt for source and destination rebar entities, copying XData.
6. `src/HPDcePro.AutoCad/Commands/SlabCommands.cs`:
   - Replace `VeThepSanTopY`, `VeThepSanBotX`, `VeThepSanBotY`: Prompt for span start and end points and bar diameters.
   - Replace `SoHieuThepSan`: Prompt for rebar selection or start number and insertion point.
   - Replace `ThongKeThepSan`: Prompt for table insertion point and rebar selection.
   - Replace `ChiaKhoangCach`: Prompt for clear span and spacing.
7. `src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs`:
   - Replace `UpdateModelEtabs`: Prompt for ETABS model path and confirm resize.
   - Replace `taomv`: Prompt for viewport center and scale string.
8. `Subsystem 3` (DceSteel Facade 6 commands) & `Subsystem 4` (MBKC Facade 7 commands):
   - Wire each endpoint to real `PromptHelper` selection, entity inspection, XData read/write, and `FramingTopology` validation.

### Task 2: Update Table Rows in `docs/rea-dce-pro/TRUTH_MATRIX.md`
- Change Parity Status from `ĐANG LÀ MOCK` to `HOÀN THÀNH` for all 77 remediated rows (64 in Sub 1, 6 in Sub 3, 7 in Sub 4).
- Fix lines 667–668 target paths to `src/HPDcePro.UI/Controls/DcePaletteControl.xaml` and `src/HPDcePro.UI/Controls/WallThicknessPaletteControl.xaml`.
- Keep all 296 Babel/native uninspectable methods marked `CHƯA GIẢI MÃ ĐƯỢC`.

### Task 3: Synchronize Summary Statistics Table with Exact 100% Row Counts
Update lines 22–33 of `docs/rea-dce-pro/TRUTH_MATRIX.md`:
```markdown
| Subsystem Index | Subsystem Name | Total Endpoints | HOÀN THÀNH | ĐANG LÀ MOCK | CHƯA GIẢI MÃ ĐƯỢC | Parity Target Milestone |
|---|---|:---:|:---:|:---:|:---:|:---:|
| Subsystem 1 (AutoCAD Commands) | AutoCAD Commands | 311 | 64 | 0 | 247 | M3 |
| Subsystem 2 (Excel Ribbon Actions) | Excel Ribbon Actions | 73 | 73 | 0 | 0 | M4 |
| Subsystem 3 (DceSteel Facade) | DceSteel Facade | 49 | 20 | 0 | 29 | M3 |
| Subsystem 4 (MBKC Facade) | MBKC Facade | 29 | 13 | 0 | 16 | M3 |
| Subsystem 5 (Native C++ Exports) | Native C++ Exports | 69 | 65 | 0 | 4 | M1 |
| Subsystem 6 (Revit 2026 Commands) | Revit 2026 Commands | 24 | 24 | 0 | 0 | M4 |
| Subsystem 7 (CAD Jigs) | CAD Jigs | 11 | 11 | 0 | 0 | M3 |
| Subsystem 8 (UI Forms & Palettes) | UI Forms & Palettes | 12 | 12 | 0 | 0 | M3 |
| Subsystem 9 (AutoStr Steps) | AutoStr Steps | 24 | 24 | 0 | 0 | M3 |
| **Grand Total (100% Surface)** | **All 9 Subsystems** | **602** | **306** | **0** | **296** | **M1–M5** |
```

### Task 4: Harden Verification Scripts
1. In `tools/verification/verify_truth_matrix.py`:
   - Add strict assertions comparing each subsystem row in `summary_stats` against the actual computed counts:
     ```python
     # Cross-check summary table cells vs actual counted rows
     for row in summary_stats:
         # parse sub_id, claimed, ht, mock, cgm
         # assert summary_ht == actual_ht
         # assert summary_mock == actual_mock
         # assert summary_cgm == actual_cgm
     ```
   - Update Subsystem 5 regex to match `Ord: X` and backticked RVA `` `0xXXXX` ``:
     ```python
     ord_match = re.search(r'Ord(?:inal)?[:\s]+#?(\d+)', token_field)
     rva_match = re.search(r'RVA:\s*`?(0x[0-9a-fA-F]+)`?', token_field)
     ```
2. In `tools/verification/deep_stress_test.py`:
   - Remove legacy M1 hardcoded strings in lines 248 and 250, replacing with dynamic checks.

---

## 5. Verification Method

To independently verify the remediation:

1. **Verify Unit & Adapter Tests**:
   Execute the direct test executables (avoid `dotnet test` due to NamedPipeServer log file lock per `DEAD_ENDS.md`):
   ```powershell
   & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"
   & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"
   ```
   *Expected*: Total 468/468 tests PASS with 0 failed, 0 skipped.

2. **Verify Master Truth Matrix Verification Suite**:
   ```powershell
   python tools/verification/run_all_truth_matrix_verifications.py
   ```
   *Expected*:
   - Step 1 (verify_truth_matrix.py): Total Actual Rows = 602 | HT = 306 | MOCK = 0 | CGM = 296.
   - Summary table cells match actual row counts 100% with 0 discrepancies.
   - Step 2 (deep_stress_test.py): 0 missing target files on disk, 0 token formatting defects.
   - Step 3 (test_markdown_syntax.py): 0 unclosed backtick defects.

3. **Invalidation Conditions**:
   - Any remaining command in `HPDcePro/src/HPDcePro.AutoCad/Commands/` that relies on static `sampleItems` or dummy hardcoded drawing without `PromptHelper`.
   - Any row in `TRUTH_MATRIX.md` marked `ĐANG LÀ MOCK` ($> 0$).
   - Any Babel .NET JIT-encrypted method fraudulently marked `HOÀN THÀNH`.
   - Any discrepancy between the Summary Statistics Table and row tallies.
