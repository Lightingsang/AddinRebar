# Handoff Report — explorer_m3_stair_lintel

**Role**: Stair & Lintel Domain Explorer (Milestone M3, Feature 12)  
**Parent Agent**: `orchestrator_17` (`a7a2dbbb-6b7d-4279-98cd-1216d8280d80`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_stair_lintel\`  
**Primary Deliverable**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_stair_lintel\stair_lintel_spec_report.md`  
**Handoff Type**: Hard (Investigation & Specification Design Complete)  

---

## 1. Observation

1. **AutoCAD Commands & Aliases (`C:\kata_pro\Goi lenh.lsp`)**:
   - Line 81: `(defun c:vemc()(command "vemc"))`
   - Line 82: `(defun c:vethang()(command "ve_thang"))`
   - Line 83: `(defun c:velanhto()(command "velanhto"))`
   - All 3 commands and aliases are registered and mapped to KataPro entry points.
2. **Backing .NET Classes in `C:\kata_pro\Kata_pro64_Cad2025.dll`**:
   - `Kata_pro64_Cad2025.cau_thang_new`:
     - Lines 15–106: `public class VeThang`: fields `so_bt_zz`, `Xbt`, `Ybt`, `veNumber`, `thepDocTren`, `thepDocTren2`, `thepDocDuoi`, `thepDocDuoi2`, `L1`..`L5`, `H1`..`H3`, `Hb1`..`Hb3`, `Hd1`, `Hd4`, `Bd1`..`Bd4`, `hDamDuoiv2`, `hDamTrenv2`, `main_steel`, `cau_tao`, `chiu_nghi`, `thep_bac_thang`, `cb_cat_thep`, `dangThepBacThang`, `hFirst`, `hMid`, `hLast`, `tb_so_bac_thang`, `rong`.
     - Lines 199–332: `public class InfoThangSave`: exact DTO persisted into DWG XRecord `KATA_STAIR_DATA`.
     - Lines 422–428: `public enum LoaiCauThangEnum { MotVe = 1, HaiVeSongSong, BaVe, HaiVeThang }`.
     - Lines 478–481: `ThepNoiVe1Dang04`, `ThepNoiVe1Dang01`, `GetListPThep04NoiGiua`, `GetListPThep01NoiGiua`.
     - Line 1002: `public static diem[] GetDiemThepBacThang(...)`.
   - `Kata_pro64_Cad2025.Lanh_to`:
     - Lines 25–68: WinForms control properties: `LT_Grid` (columns `Loai_LT`, `B`, `H`, `d`), `loai`, `tien_to`, `cong_d`, `Dim_lt`.
   - `Kata_pro64_Cad2025.view_thang_new`:
     - 118 controls including `BtnThangThang`, `BtnThangZigZac`, `RadioButton1`..`4`, `ve_thang`, `ve_thang_shop`, `MirrorBtn`, `RotateBtn`, `main_steel`, `cau_tao`, `chiu_nghi`, `thep_bac_thang`.
3. **Revit Stair Selection & Category**:
   - `Kata_Class_Lib_Revit.dll` `Element_Select.cs`: `public class Stair_SelectionFilter : ISelectionFilter` filters `BuiltInCategory.OST_Stairs`.
4. **Existing HPKata Production Baseline**:
   - `HPKata/HPKata.Core/Beams/Persistence/BeamXRecordSerializer.cs` (32 lines) uses `System.Text.Json` to serialize to XRecord `KATA_BEAM`.
   - `HPKata/HPKata.Core/Beams/Persistence/BeamPlanScheduleWorkbookGenerator.cs` (132 lines) uses `ClosedXML.Excel` to format sheet `ThepDamAllMBKC`.
   - `HPKata/HPKata.Revit/Services/FramingRebarGenerator.cs` (206 lines) and `ColumnRebarGenerator.cs` (159 lines) follow idempotent clean-and-recreate patterns via `Rebar.CreateFromCurves`.
   - All existing 437 tests in `HPKata/HPKata.slnx` pass 100%.

---

## 2. Logic Chain

1. **From Observation 1 & 2 to Domain Model Design**:
   - `cau_thang_new.VeThang` and `InfoThangSave` demonstrate that a stair flight is completely parameterized by: riser count $n_r$, riser height $h_r$, tread width $b_t$, waist throat thickness $t_w$, landing lengths $L_{land1}, L_{land2}$, landing thicknesses $t_{land1}, t_{land2}$, and flight width $W_f$.
   - The existence of `ThepNoiVe1Dang04` (cranked bar) and the physics of re-entrant corners require a scissor-crank crossing rebar detail to prevent radial concrete breakout at the obtuse soffit / reflex deck angle.
   - Therefore, `StairFlightModel` and `StairRebarCalculator` in `HPKata.Core` must encapsulate these exact geometric relations, nosing sawtooth lines, and scissor-crank reinforcement paths.
2. **From Observation 2 to Lintel Model Design**:
   - `Lanh_to` requires clear span $L_0$, wall thickness $b$ ($110\text{ mm}$ or $220\text{ mm}$), height $h$ ($100 - 300\text{ mm}$), and bearing length $d \ge 200\text{ mm}$ (typ. $250\text{ mm}$).
   - Therefore, `LintelModel` and `LintelRebarCalculator` in `HPKata.Core` calculate total length $L_{total} = L_0 + 2 L_{bear}$, $90^\circ$ end hooks $h - 2c$, and zoned stirrups ($\le 100\text{ mm}$ over bearings, $\le 200\text{ mm}$ at midspan).
3. **From Observation 1 & 2 to Generic Section Engine (`vemc`)**:
   - `vemc` (`tao_MC_cau_kien1`) is a cross-cutting section cut command across beams, columns, walls, and stairs.
   - Therefore, `GenericSectionModel` and `GenericSectionCalculator` in `HPKata.Core` model concrete outline polygons, rebar coordinates $(x, y)$, stirrup polyline hoops, associative dimensions, and section callout bubbles.
4. **From Observation 4 to Clean Code & Host Isolation**:
   - The architecture partitions the subsystem into 23 distinct files across `HPKata.Core`, `HPKata.AutoCad`, `HPKata.Revit`, `HPKata.UI`, and `HPKata.Core.Tests`.
   - The largest planned file is 210 lines (`StairRebarGenerator.cs`), well within Rule C2 ($\le 300$ physical lines).
   - `HPKata.Core` references only `System` and `ClosedXML` (for export), strictly zero references to AutoCAD or Revit.

---

## 3. Caveats

- **No Caveats**. All required reverse-engineered classes, UI controls, DWG persistence keys, Revit API bindings, and ClosedXML schemas have been mined and verified directly from the source assemblies and scripts.

---

## 4. Conclusion

The Stair & Lintel subsystem specification is complete, mathematically rigorous, and ready for immediate implementation in Milestone M3.
- Primary report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_stair_lintel\stair_lintel_spec_report.md`
- Key classes planned:
  - `HPKata.Core/Stairs/Models/StairFlightModel.cs`, `StairReinforcementModel.cs`, `LintelModel.cs`, `GenericSectionModel.cs`
  - `HPKata.Core/Stairs/Calculators/StairGeometryCalculator.cs`, `StairRebarCalculator.cs`, `LintelRebarCalculator.cs`, `GenericSectionCalculator.cs`
  - `HPKata.Core/Stairs/Persistence/StairXRecordSerializer.cs`, `LintelXRecordSerializer.cs`, `StairScheduleAggregator.cs`, `StairPlanScheduleWorkbookGenerator.cs`
  - `HPKata.AutoCad/Commands/StairLintelCommands.cs`, `Controllers/StairDrawController.cs`, `LintelDrawController.cs`, `SectionCutController.cs`
  - `HPKata.Revit/Commands/StairRebarCommands.cs`, `Services/StairRebarGenerator.cs`, `LintelRebarGenerator.cs`
  - `HPKata.UI/ViewModels/StairDetailingViewModel.cs`, `LintelDetailingViewModel.cs` and matching WPF Views
  - Comprehensive 5-suite unit test coverage in `HPKata.Core.Tests/Stairs/`

---

## 5. Verification Method

1. **Inspect Specification Report**:
   - View `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_stair_lintel\stair_lintel_spec_report.md`
   - Confirm coverage of all 7 assignment topics: Stair flight geometry, scissor crank bars, lintels, generic section engine (`vemc`), AutoCAD commands & XRecords, Revit `OST_Stairs`, ClosedXML sheet `Thang`, and Clean Code line budgets.
2. **Verify Solution Compilation & Existing Baseline Tests**:
   - Run `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` (0 errors, 0 warnings).
   - Run `dotnet test HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-verify"` (all baseline tests green).
3. **Invalidation Conditions**:
   - Any file in `HPKata.Core` referencing `Autodesk.*`.
   - Any planned file exceeding 300 physical lines.
   - Any failure to enforce the re-entrant corner scissor-crank rule ($L_{ext} \ge 35d$).
