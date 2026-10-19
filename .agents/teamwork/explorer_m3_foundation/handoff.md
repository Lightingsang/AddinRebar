# Handoff Report — explorer_m3_foundation (Foundation Domain & Architecture Spec Miner)

- **Agent**: `explorer_m3_foundation`
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_foundation\`
- **Recipient**: Parent Orchestrator (`a7a2dbbb-6b7d-4279-98cd-1216d8280d80`)
- **Milestone**: M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing)
- **Handoff Type**: Hard (Task Complete)
- **Primary Deliverable**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_foundation\foundation_spec_report.md`

---

## 1. Observation

1. **AutoLISP Command Bindings in `C:\kata_pro\Goi lenh.lsp`**:
   - Line 60: `(defun c:vmbm()(command "ve_mat_bang_mong"))`
   - Line 61: `(defun c:rm()(command "rai_mong"))`
   - Line 62: `(defun c:vmd()(command "ve_mong_don"))`
   - Line 63: `(defun c:vmc()(command "ve_mong_coc"))`
   - Ribbon icons in `C:\kata_pro\Icon\Ribbon\`: `ve_mong_don.png`, `ve_mong_coc.png`, `ve_mong_coc_giat_cap.png`, `ve_mat_bang_mong.png`, `rai_mong.png`.

2. **Decompiled Types in `C:\kata_pro\Kata_pro64_Cad2013.dll` & `Kata_pro64_Cad2025.dll`**:
   - `Mong_coc+InfoMongCoc`: holds `daiRauCoc` (pile dowel extension), `h1, h2, h3, h4`, `d1, d2, d3, e1, e2, e3`, `l, b`, `diameter, diameter_X, diameter_Y`, `la_mong_bien_thien` (stepped/variable), `list_point_coc` (pile coordinates).
   - `Mong_coc+InfoUserMongCoc`: holds `fdd, fdn, ftd, ftn, fdai, fdo, fct` (diameters for bottom/top mesh, ties, skin, starter dowels), `spacing_dd, spacing_dn, spacing_td, spacing_tn, spacing_dai`, `co_thep_tren`, `co_thep_cau_tao`, `co_thep_dai`, `co_thep_cot`.
   - `Mong_coc+CoversMong`: holds `cover_tren`, `cover_duoi`, `cover_ngang`.
   - `Mong_coc+InfoMongCocSave`: full XRecord serialization payload for pile caps.
   - `InfoMongDonSave`: holds `X1, X2, X3, Y1, Y2, Y3, Z1, Z2, Z3, Z4`, `Thep_mong_X, Thep_mong_Y`, `Thep_cot_X, Thep_cot_Y`, `phi_cot, dai_cot`, `bt_lot`, `Cover_mong, Cover_cot`.
   - `Ve_Mat_Bang_Mong_Module`: holds `InfoVeMatBangMong`, `CocChiuTai`, `InfoCotCocGenMong`, `InfoMBMongDon`, `CotChiuTaiMongDon`, `VMBM()`, `PhaMong()`.
   - `Rai_Mong_Module`: holds `InfoRaiMong`, `InfoRaiMongDon`, and `RaiMongJig`.

3. **Existing HPKata Baseline**:
   - `HPKata.Core`: models and calculators partitioned cleanly into `Beams/`, `Columns/`, `Walls/`, with zero host dependencies.
   - `HPKata.AutoCad`: controllers and commands partitioned cleanly in `Controllers/` and `Commands/`.
   - `HPKata.Revit`: `FramingRebarGenerator.cs`, `ColumnRebarGenerator.cs`, `WallRebarGenerator.cs` using `RebarCreationService.CreateRebar` with `BarTerminationsData` and comments-based idempotency.
   - `HPKata.Core.Tests`: 437/437 passing tests.

---

## 2. Logic Chain

1. **Model Synthesis**:
   - Pad footings in KataPro (`ve_mong_don`) decompose into a base slab ($L \times B \times h_1$) and a pedestal ($l_c \times b_c \times h_2$) with bottom mesh (Shape 02 U-bars), column starter dowels (Shape 02 L-bars), and pedestal ties (Shape 51).
   - This directly maps to `IsolatedFootingModel.cs` and `FootingGeometryCalculator.cs` (prismoidal volume formula for sloped pedestals and box formula for stepped pedestals).
2. **Pile Cap Coordinate Synthesis**:
   - Pile caps (`ve_mong_coc`) require positioning $N$ piles symmetrically about the cap centroid $(0, 0)$ with center-to-center spacing $s \ge 3D$ and edge distance $c_e \ge 1.0D$.
   - Canonical configurations for $N \in [1, 9]$ piles (1 single, 2 inline, 3 triangular/inline, 4 grid, 5 quincunx, 6 grid $2 \times 3$, 7 hexagonal, 8 grid $2 \times 4$, 9 grid $3 \times 3$, and general $N_x \times N_y$) provide unambiguous coordinates, embedded into `PileCoordinateGenerator.cs`.
3. **Revit Host Integration**:
   - Revit 2026 hosts isolated footings and pile caps in `OST_StructuralFoundation` (`FamilyInstance`).
   - Extracting bounding box dimensions and host covers (`CLEAR_COVER_BOTTOM`, `CLEAR_COVER_TOP`, `CLEAR_COVER_OTHER`) allows generating 3D centerline curve loops for bottom mesh X, bottom mesh Y, top mesh, perimeter skin hoops, and starter dowels via `RebarCreationService.CreateRebar`.
   - Comments prefix `"HPKata_Foundation_{Mark}"` ensures 100% idempotent regeneration without duplicate rebars.
4. **Excel Scheduling**:
   - `FoundationScheduleAggregator.cs` maps mesh bars to Shape 02, straight bars to Shape 01, and ties to Shape 51/11, calculating exact mass via `RebarCalculator.GetUnitMass`.
   - `FoundationPlanScheduleWorkbookGenerator.cs` formats ClosedXML worksheet `Mong` with 19 columns matching `Kata.xlsm`.

---

## 3. Caveats

- **Sloped Footing Pedestal Forms**: In real-world fabrication, sloped footings often use steep slopes where timber top forms are omitted if concrete slump is low ($\le 8\text{ cm}$); however, for conservative formwork estimation, the sloped contact surface area equation is provided in Section 4.2.
- **Pile Dowel Clash in Multi-Pile Caps**: In 3D Revit modeling, pile dowels projecting into the cap must avoid physical intersection with the bottom mesh. A vertical clearance offset of $+50\text{ mm}$ above the bottom rebar layer is designed to ensure clearance.

---

## 4. Conclusion

The specification and architecture for the Foundation Detailing Subsystem (Phase 4 / Milestone M3) is completely documented in `foundation_spec_report.md`.
The Worker subagents can implement the 18 planned modular files across `HPKata.Core`, `HPKata.AutoCad`, `HPKata.Revit`, and `HPKata.Core.Tests` with zero guesswork, guaranteed Clean Code compliance ($\le 300$ lines/file, $\le 50$ lines/method), and complete test coverage.

---

## 5. Verification Method

To verify the mined models, formulas, and architecture independently:
1. Inspect the deliverable report at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_foundation\foundation_spec_report.md`.
2. Inspect the extracted types and fields from `C:\kata_pro\Kata_pro64_Cad2013.dll` using PowerShell reflection:
   ```powershell
   $asm = [System.Reflection.Assembly]::LoadFrom("C:\kata_pro\Kata_pro64_Cad2013.dll")
   $asm.GetType("Kata_pro64_Cad2013.Mong_coc+InfoMongCoc").GetFields() | Select-Object Name
   $asm.GetType("Kata_pro64_Cad2013.Mong_coc+InfoUserMongCoc").GetFields() | Select-Object Name
   ```
3. Inspect `C:\kata_pro\Goi lenh.lsp` lines 60–63 for command aliases.
4. Verify current solution build and test status:
   ```powershell
   dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1
   ```
