# Handoff Report: Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing)

- **Agent**: `explorer_m2_column_wall` (Domain & Architecture Explorer)
- **Role**: Specification Mining, Architecture Design & Test Planning for Phase 3
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`
- **Deliverable**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_column_wall\column_wall_spec_report.md`
- **Handoff Type**: Hard (Task Complete)

---

## 1. Observation
1. **Master Request & Blueprint Requirements**:
   - `ORIGINAL_REQUEST.md` (lines 1090–1101, 1145–1147): Mandates Phase 3 Column & Shear Wall detailing across `HPKata.Core`, `HPKata.UI`, `HPKata.AutoCad`, and `HPKata.Revit`.
   - `orchestrator_17/PROJECT.md` (lines 8, 28–31, 48): Defines Milestone M2 covering features 6–9: Column Elevation & Splice Engine, Shear Wall & Boundary Detailing (`ve_loi_vach`), Column & Wall UI Dialogs, and 10 CAD/Revit commands.
2. **Reverse-Engineering Evidence**:
   - `GoiLenh_HPKata.lsp` (lines 64–68, 85–86, 44–45, 54):
     - `(defun c:vc()(command "ve_cot"))`, `(defun c:vcs()(command "ve_cot_shop"))`
     - `(defun c:vv()(command "ve_vach"))`, `(defun c:vvs()(command "ve_vach_shop"))`
     - `(defun c:vlv()(command "ve_loi_vach"))`
     - `(defun c:loadcolumn()(command "loadcolumn"))`, `(defun c:loadwall()(command "loadwall"))`
     - `(defun c:raicot()(command "raicot"))`, `(defun c:dimcot()(command "dimcot"))`, `(defun c:tkcot()(command "tkcot"))`
   - `kata_xlsm_data.txt`:
     - Sheet `Cot` (dims A1:BA216): Row 7-15 defines story heights, column dimensions ($b \times h$), longitudinal bars (face $b$, face $h$, corner), stirrups (dense $a_1$, sparse $a_2$, beam-column joint $a_{dam}$), and supplementary ties (rectangular inner hoops, diamond hoops, C-ties).
     - Sheet `Vach` (dims A1:AV216): Row 7-15 defines boundary element width ($b_{bz}$), boundary longitudinal bars, boundary confinement ties, and web distributed double-curtain mesh ($d10a100/200$, C-ties $d10a400$).
   - Decompiled `Kata_Class_Lib.dll`:
     - `struct info_cot`: `double y`, `int b`, `int h`, `string ten`, `bool bop_deu`, `bool cat_thep`.
     - `struct Info_dai_cot`: `cd`, `a1`, `a2`, `a_dam`, `dai_dau_cot` ($l_0$), `cd_v`, `a_v1`, `a_v2`, `sl_v`, `cd_h`, `cdc_v`, `ac_v1`, `slc_v`.
3. **Completed Phase 2 Patterns in HPKata**:
   - `HPKata/HPKata.Core/Beams/`: Modular directory structure (`Calculators/`, `Etabs/`, `Models/`, `Persistence/`).
   - `HPKata/HPKata.UI/`: Observable MVVM ViewModels with CommunityToolkit.Mvvm + dynamic `Theme.xaml` brushes.
   - `HPKata/HPKata.AutoCad/`: Clean CommandMethod bindings delegating to controllers (`ContinuousBeamCommands.cs` -> `BeamDrawController.cs`).
   - `HPKata/HPKata.Revit/`: External commands on Revit elements delegating to generator services (`FramingRebarCommands.cs` -> `FramingRebarGenerator.cs`).
4. **Clean Code Governance & Concurrency**:
   - `docs/clean-code/HP_CLEAN_CODE_CORE.md`: Max 300 lines/file, max 50 lines/method, 0 dead code, 0 unannotated catch, Core zero host dependencies.
   - Concurrency directive: `--disable-build-servers -m:1` must be passed to all `dotnet build` and `dotnet test` commands.

---

## 2. Logic Chain
1. **From Observation 1 & 2 to Domain Models**:
   - Column detailing requires multi-story continuity with step-downs. Following TCVN 5574:2018 and KataPro behavior:
     - When step-down $\Delta \le 50\text{ mm}$, bars must be cranked with a maximum slope of $1:6$ within the beam-column joint depth.
     - When step-down $\Delta > 50\text{ mm}$, bars cannot be cranked safely; lower bars must terminate below the beam top with $90^\circ$ hooks, and separate starter dowels must be introduced for the upper column.
     - Splices must be staggered ($50\%$ bars at section 1, $50\%$ at section 2, with $\Delta Z \ge 1.3 L_{lap}$ or $600\text{ mm}$).
     - Confinement zones $l_0 = \max(b, h, H_n/6, 500\text{ mm})$ dictate dense tie spacing $a_1 \le 100\text{ mm}$.
   - Shear wall detailing requires planar/core shapes, boundary element cages (`ve_loi_vach`) with concentrated vertical bars and confinement ties ($s \le 100\text{ mm}$), double-curtain web distributed bars ($t_w \ge 150\text{ mm}$), U-bar edge hooks (`dang05` / `dang25`), and diagonal corner trimmers ($45^\circ$, $2\phi 14$ - $2\phi 18$) at openings.
2. **From Observation 2 & 3 to Clean Architecture Partitioning**:
   - Sizing and calculation engines (`ColumnCrankBendCalculator`, `ColumnSpliceCalculator`, `ColumnConfinementCalculator`, `WallBoundaryCalculator`, `WallWebMeshCalculator`, `WallOpeningTrimmerCalculator`) are kept under 150 lines each.
   - Models (`ColumnStorySegment`, `ColumnSectionModel`, `ColumnRebarLayout`, `ShearWallModel`, `WallBoundaryElementModel`, `WallOpeningModel`) are strictly typed DTOs with pure geometric calculations.
   - Importers (`EtabsColumnImportService`, `EtabsWallImportService`) parse delimited tables and map design forces to optimized rebar layouts.
3. **From Observation 3 to UI & Host Integration**:
   - UI follows `BeamDetailingWindow` paradigm: WPF Window using `Theme.xaml` resources (`Brush.Background`, `Brush.Text`, `Card.Panel`, Segoe UI), data grid for stories, section preview, and splice options.
   - AutoCAD implements 10 command methods mapped to dedicated controllers.
   - Revit external commands target `OST_StructuralColumns` and `OST_Walls`, delegating to `ColumnRebarGenerator` and `WallRebarGenerator` with idempotent regeneration.

---

## 3. Caveats
- No caveats. The exact schemas of `Cot` and `Vach` sheets, decompiled structures, and command aliases were verified directly from project ground truth artifacts.

---

## 4. Conclusion
The specification and architecture for Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing) is completely established in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_column_wall\column_wall_spec_report.md`
The report provides complete mathematical formulas, data contracts, file manifests ($\le 300$ lines/file, $\le 50$ lines/method), UI layouts, AutoCAD/Revit commands, and an 18-class test suite plan ($\approx 71$ tests) ready for immediate implementation.

---

## 5. Verification Method
1. Inspect the full specification report:
   - `view_file` on `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_column_wall\column_wall_spec_report.md`.
2. Verify that all 10 AutoCAD commands (`vc`, `vcs`, `raicot`, `dimcot`, `tkcot`, `loadcolumn`, `vv`, `vvs`, `vlv`, `loadwall`) and 2 Revit categories (`OST_StructuralColumns`, `OST_Walls`) are accounted for.
3. Verify that the planned file structure partitions code into units under 300 lines each with 0 host dependencies in `HPKata.Core`.
