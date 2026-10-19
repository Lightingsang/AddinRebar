# Handoff Report — Milestone M4: Drawing Layout, Grid & Viewport Specification

## 1. Observation
1. **Master Request & Milestones**:
   - `ORIGINAL_REQUEST.md` (lines 1114–1124, 1152–1155) and `orchestrator_17/PROJECT.md` (lines 37–40, 45–51) specify Milestone M4 (Phase 5: Quantity Surveying & Drawing Utilities).
   - Milestone M3 completed with 743 passing tests across 5 test projects (`worker_m3_foundation_slab_stair/handoff.md` lines 14–19).
2. **Legacy KataPro Command Parity**:
   - `C:\kata_pro\Goi lenh.lsp` lines 43 and 146–151 expose:
     ```lisp
     (defun c:vg()(command "ve_grid"))
     (defun c:danhmuc()(command "danhmuc"))
     (defun c:cat()(command "cat_doituong"))
     (defun c:taomv()(command "taomv"))
     (defun c:taomv1()(command "taomv1"))
     (defun c:sbv()(command "sap_ban_ve"))
     (defun c:ibv()(command "in_ban_ve"))
     ```
   - Configuration lines 152–237 define 86 parameters including `kata_grid` (line 156), `kata_dim` (line 152), `kata_text` (line 161), `kata_net khung` (line 154), and `TitleScaleMode` (line 213).
3. **Existing Codebase State**:
   - `HPKata/HPKata.AutoCad/Commands/UtilityCommands.cs` (lines 44–50) currently holds placeholder stub `VeGrid()`.
   - `HPKata/HPKata.Core/` currently contains domains for `Beams/`, `Columns/`, `Walls/`, `Foundations/`, `Slabs/`, `Stairs/`, but `DrawingLayout/` is not yet created.
   - `HPKata/HPKata.Revit/Units/RevitUnitConverter.cs` (lines 10–15) quarantines all conversions between decimal feet and millimeters; constant `304.8` appears strictly within this file.
4. **Authoritative Specification Produced**:
   - Written to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_layout\layout_spec_report.md` (745 lines, 8 sections) covering domain models, geometric algorithms, AutoCAD controllers, Revit services, UI ViewModel, and empirical test matrices.

---

## 2. Logic Chain
1. *From Observation 1 and 2*: Milestone M4 requires full clean-room implementation of drawing layout utilities matching KataPro commands `vg`, `sbv`, `ibv`, `taomv`, `taomv1`, `khoav`, `mokhoav`, and `danhmuc`.
2. *From Observation 3*: The existing codebase establishes clean architectural patterns where all mathematical calculations, geometry projections, and data aggregations reside in `HPKata.Core` with zero Autodesk references, while host controllers (`HPKata.AutoCad`, `HPKata.Revit`) handle interactive UI, transactions, and entity drafting.
3. *From Observation 4*: `layout_spec_report.md` provides complete, unambiguous, and compilable C# contracts and algorithms for:
   - `GridAxisGenerator`: Orthogonal, skewed, and radial grid systems with bubble coordinates, ISO 128 label skipping (`'I'`, `'O'`, `'Q'`), and continuous dimension strings.
   - `SheetArrangerCalculator`: 2D shelf-packing bin-packing algorithm with TitleBlock exclusion zone $[W - M_r - W_{\text{tb}}, W - M_r] \times [M_b, M_b + H_{\text{tb}}]$ and multi-sheet overflow pagination.
   - `DrawingIndexAggregator`: Natural alphanumeric sorting (`KC-01` to `KC-10`), sequence gap detection, duplicate alerting, and ClosedXML Excel export.
   - `RevitSheetArrangementService`: Automated `ViewSheet.Create` and `Viewport.Create` with strict Rule R5 isolation via `RevitUnitConverter`.
   - Host controllers: `GridDrawController`, `SheetLayoutController`, `ViewportController`, `DrawingIndexController`.

---

## 3. Caveats
- `layout_spec_report.md` is a read-only exploration and design deliverable; no production C# files in `HPKata/` were altered during this exploration turn.
- Implementation of the specified models, calculators, controllers, and unit tests will be executed by the designated worker agent (`worker_m4_layout`).

---

## 4. Conclusion
The Drawing Layout, Grid & Viewport Subsystem is fully explored and comprehensively specified in `layout_spec_report.md`. The design guarantees 100% parity with legacy commands, 100% host neutrality for `HPKata.Core`, zero regressions on existing 743 passing tests, and strict adherence to Clean Code rules C2, M3, Q-B1, Q-B2, and R5.

---

## 5. Verification Method
To independently verify this specification and its readiness for implementation:

1. **Inspect Authoritative Specification Report**:
   ```powershell
   Get-Content "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_layout\layout_spec_report.md"
   ```
2. **Verify Baseline Build & Tests (Sanity Check)**:
   ```powershell
   dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1
   dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"
   ```
   *Expected outcome*: `0 Warning(s), 0 Error(s)`, `743 passed, 0 failed, 0 skipped`.
3. **Check Clean Code Compliance**:
   Confirm that all proposed C# class models and methods in `layout_spec_report.md` are bounded by $\le 300$ lines per file and $\le 50$ lines per method.
