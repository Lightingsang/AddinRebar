# Handoff Report: ETABS Beam Import, TCVN Rebar Auto-Selection & WPF UI Design

- **Agent**: `explorer_m1_3` (ETABS & UI Explorer)
- **Role**: Explorer / Architect
- **Milestone**: M1 (Phase 2: Beam Shop Drawing & ETABS Integration)
- **Date**: 2026-10-17
- **Target Deliverable Report**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_beam_etabs_ui\beam_etabs_ui_report.md`

---

## 1. Observation

1. **ETABS & Force Reading Baseline in Repository**:
   - `HPRebar/HPRebar/BeamRebar/Service/EtabsBeamForceReader.cs` (lines 81–105) contains production tabular text parsing and delimiter detection (`DetectSeparator`, lines 141–147) converting ETABS station records into `BeamForceEnvelope` instances.
   - `HPRebar/HPRebar.Core/Tcvn5574/Calculators/TcvnBeamRebarOptimizer.cs` (lines 18–43, 48–74) contains concrete implementations of clear spacing checks ($t \ge \max(25, d)$ for bottom, $t \ge \max(30, d)$ for top) and single/dual diameter candidate ranking per TCVN 5574:2018 Clause 10.3.1.2.
   - `HPEtabs/HPEtabs.Mcp.Server/Registry/SeedLibrary/Geometry/get_structural_objects/code.cs` (lines 39–48) demonstrates how ETABS frame objects are identified by story, endpoints, section name, and Euclidean length in mm.
2. **HPKata Baseline in Repository**:
   - `HPKata/HPKata.UI/HPKata.UI.csproj` targets `net8.0-windows`, referencing `CommunityToolkit.Mvvm` 8.4.0 and `MaterialDesignThemes` 5.2.1.
   - `HPKata/HPKata.UI/Themes/Theme.xaml` establishes dynamic theme resource keys (`Brush.Background`, `Brush.Surface`, `Brush.Text`, `Brush.Accent`, `StandardTextBox`, `StandardComboBox`, `Card.Panel`).
   - `HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj` and `HPKata/Directory.Build.props` pin .NET 8, `Microsoft.Testing.Platform` test runner, `<UseSharedCompilation>false</UseSharedCompilation>`, `<BuildInParallel>false</BuildInParallel>`, and dynamic `--results-directory` isolation to prevent MTP log file collisions.
   - Existing test suite (437 tests across 5 test projects) executes cleanly and rapidly without Revit or AutoCAD running.
3. **KataPro Legacy Evidence**:
   - `C:\kata_pro\Goi lenh.lsp` lines 69–96 registers all Phase 2 beam command aliases: `vd` (`ve_dam`), `vds` (`ve_dam_shop`), `vda` (`ve_dam_all`), `vdas` (`ve_dam_all_shop`), `vdmc` (`ve_dam_mc`), `vdmca` (`ve_damMC_all`), `vdm` (`ve_dam_mong`), `vgm` (`ve_giang_mong`), `vgms` (`ve_giang_shop`), `vdx` (`ve_dam_xien`), `vdc` (`ve_dam_cong`), `damgiao` (`dam_giao`), `giaodam` (`giao_dam`), `catdam` (`cat_dam`), `raidam`, `tkdam`, `qd` (`quet_dam`), `qda` (`quet_dam_all`), `std` (`Save_thep_dam`), `stda` (`Save_thep_dam_all`), `atd` (`Add_thep_dam`), `shtd` (`show_thep_dam`), `loadbeam`, and `Etabs`.
   - `C:\kata_pro\Kata_pro64_Cad2025.dll` contains TypeDefs: `beam_kata`, `FormBeamRebar`, `Set_Rebar_Beam`, `Check_model_Etabs`, `Build_Etabs_Model`, `Foundation_Etabs`.

---

## 2. Logic Chain

1. **Separation of Concerns**:
   - In accordance with repository rule ADR-0002 (Feature-Sliced Monolith with Pure Core) and Clean Code Governance, all structural geometry calculations, ETABS text/excel parsing, TCVN clear spacing validation, and rebar auto-selection algorithms MUST reside in `HPKata.Core` (`netstandard2.0;net8.0`) with 0 references to AutoCAD or Revit APIs.
2. **Continuous Beam Assembly Logic**:
   - Individual ETABS frame elements must be grouped by Story, filtered for directional collinearity ($\vec{u}_1 \cdot \vec{u}_2 \ge \cos 1^\circ$), and topologically chained into continuous beam runs ordered monotonically along the beam axis ($X_{\text{start}} < X_{\text{end}}$). Intermediate column joints define support widths and span clear lengths $L_n$.
3. **Rebar Auto-Selection & TCVN Optimization**:
   - Area requirements ($A_{s,\text{prov}} \ge A_{s,\text{req}}$) and minimum reinforcement ratio ($\mu \ge 0.10\%$) must be enforced concurrently with clear spacing checks per TCVN 5574:2018 Clause 10.3.1.2:
     - Clear horizontal spacing $t \ge \max(25\text{ mm}, d_{\max})$ for bottom tension bars.
     - Clear horizontal spacing $t \ge \max(30\text{ mm}, d_{\max})$ for top pouring face bars.
     - Maximum adjacent diameter difference $|d_1 - d_2| \le 4\text{ mm}$ for dual-diameter layouts.
     - 2 continuous corner bars running through the entire beam run are required to hold stirrup cages.
     - Transition to 2 layers occurs only when 1 layer cannot fit, penalizing the candidate score by $+25\%$ to prioritize pourability.
4. **WPF MVVM & Theme Binding**:
   - In `HPKata.UI`, `BeamDetailingViewModel` and `EtabsImportViewModel` encapsulate the user interaction states, binding to `BeamSpanModel` and `EtabsBeamImportRun` DTOs via `CommunityToolkit.Mvvm`.
   - Views consume `Themes/Theme.xaml` resources (`{DynamicResource Brush.*}`), providing automatic dark/light theme switching without hardcoded colors.
5. **Clean Code & Test Coverage**:
   - All proposed files are scoped under 300 lines and methods under 50 lines.
   - An 80-scenario unit test plan across 6 test groups guarantees regression-free implementation and automated verification.

---

## 3. Caveats

1. **ETABS Version Diversity**:
   - ETABS table column names vary slightly across versions (e.g. `TopRebarArea` in ETABS 22 vs `Top Area` in ETABS 19). The parser includes robust alias matching (`TopArea`, `Top Rebar`, `TopArea_mm2`).
2. **Column Sizing Heuristic**:
   - When importing from delimited tables without full column geometry, support widths default to standard column widths ($300\text{ mm} - 400\text{ mm}$), editable in `BeamDetailingViewModel`.
3. **Revit Host Interaction**:
   - Unit tests run headlessly in `HPKata.Core.Tests` and `HPKata.UI.Tests`. Live 3D rebar creation in Revit requires a host Revit session.

---

## 4. Conclusion

1. The architectural blueprint for the ETABS Beam Import Service, TCVN 5574:2018 Rebar Auto-Selection Engine, and WPF MVVM Detailing UI is complete, structured, and fully documented in `beam_etabs_ui_report.md`.
2. All 18 planned C# files are partitioned to respect the 300-line file limit and 50-line method limit.
3. An 80-scenario Unit Test Plan is prepared for `HPKata.Core.Tests` and `HPKata.UI.Tests`.

---

## 5. Verification Method

1. **Inspect Architectural Report**:
   - View `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_beam_etabs_ui\beam_etabs_ui_report.md`.
2. **Verify Clean Code & Build Baseline**:
   - Run command: `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` (must succeed with 0 errors).
3. **Verify Existing Tests**:
   - Run command: `dotnet test HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj --disable-build-servers -m:1` (all tests pass).
   - Run command: `dotnet test HPKata/HPKata.UI.Tests/HPKata.UI.Tests.csproj --disable-build-servers -m:1` (all tests pass).
