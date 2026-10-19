# E2E TEST WRITER DISPATCH SPECIFICATION — Milestone M7 (test_writer_m7)

## Mission
Design, implement, and verify the comprehensive, requirement-driven 4-tier E2E testing suite for **HPKata** (`HPKata.E2E.Tests`), publish `TEST_INFRA.md` and `TEST_READY.md`, and verify 100% green execution across the entire solution.

## Authoritative Inputs
- **Original User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read first!)
- **Master Blueprint**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`
- **Root Project Definition**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- **Clean Code Governance**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`

---

## Scope of Work

### 1. Test Project Setup (`HPKata/HPKata.E2E.Tests/`)
- Create `HPKata/HPKata.E2E.Tests/HPKata.E2E.Tests.csproj`:
  - `TargetFramework`: `net8.0-windows`
  - `UseWPF`: `true`
  - Project references: `HPKata.Core`, `HPKata.UI`, `HPKata.AutoCad`, `HPKata.Revit`
  - Package references: `xunit.v3` (v3.1.0 or latest matching repository), `Microsoft.Testing.Platform` runner
  - Reference assemblies: `Nice3point.Revit.Api.RevitAPI` 2026.0.4 with `PrivateAssets="all"` & `ExcludeAssets="runtime"` (matching `HPKata.Revit.csproj`)
- Register `HPKata.E2E.Tests.csproj` in `HPKata/HPKata.slnx`.

### 2. Publish `TEST_INFRA.md` at Project Root
- Overwrite `TEST_INFRA.md` with the full test architecture for HPKata:
  - 12 major feature areas from `PROJECT.md § Feature Inventory`
  - 4-Tier test methodology (Category-Partition, Boundary Value Analysis, Pairwise Combinations, Real-World Workloads)
  - Target coverage thresholds ($\ge 5$ per feature for Tier 1 & 2, pairwise for Tier 3, realistic scenarios for Tier 4)

### 3. Implement the 4-Tier Opaque-Box E2E Test Suite
Organize into subfolders within `HPKata/HPKata.E2E.Tests/`:

#### Tier 1 — Feature Coverage (`Tier1_FeatureCoverage/`)
Verify each core requirement in isolation (at least 5 tests per area):
1. **Config & 86 Parameters**: Parsing, serialization, defaults, custom overrides, invalid syntax recovery.
2. **Rebar Shapes & Geometry**: Standard straight (`dang01`), U-bars (`dang02`, `dang04`), closed stirrups (`dang22`), cranked bars (`dang03`), chair bars (`dang26`).
3. **Hook & Lap Calculations**: 90°, 135°, 180° hooks, TCVN 5574 lap splices, anchorage length.
4. **XData 2-Way Serialization**: RegApp `"KATA"`, 12-item stream encoding/decoding, handle linking, severing, clone re-binding.
5. **1D Cutting Stock Optimization**: Standard 11.7m stock bars, heuristics (`Auto`, `DuBang0`, `LapDayCanhDai`, `PatternCat`), offcut tracking, scrap percentage.
6. **Schedule & Diameter Summaries**: Aggregation, total mass, TCVN diameter grouping ($\le 10$, $\le 18$, $> 18\text{ mm}$).
7. **Coupler Tracking**: Coupler schedule, thread deductions, coupler count per diameter.
8. **QR Tag Generation**: Pipe-delimited payload, ECC Level Q, PNG byte generation.
9. **Headless ClosedXML Export**: Valid `.xlsx` workbook generation, sheet `"Thong_ke"`, formula preservation, formatting.
10. **AutoCAD Detailing Commands**: Duplicate mark detection, dynamic tagging, schedule comparison, Lisp loader 238 lines / 118 aliases.
11. **Revit Host Integration**: Parameter bridge, Rule R5 unit quarantine, shape adapters, 3D rebar curves with `BarTerminationsData`.
12. **Cross-Platform Host Neutrality**: Core library runs with 0 AutoCAD/Revit DLL dependencies.

#### Tier 2 — Boundary & Corner Cases (`Tier2_BoundaryCorner/`)
Stress test at and outside physical limits:
- Extreme bar lengths (1 mm, 11,700 mm, 24,000 mm multi-splice).
- Zero and negative dimension inputs gracefully handled without crashes/NaNs.
- Saw kerf variations (0 mm, 2 mm, 5 mm, 10 mm, 20 mm) strictly adhering to stock capacity.
- Empty rebar lists, 0-mass schedules, single-item cutting stock.
- Collinear vertices, acute turns (< 15°), degenerate segments in CAD polylines.
- Null partition names, special Vietnamese diacritics in rebar marks.

#### Tier 3 — Cross-Feature Combinations (`Tier3_CrossFeature/`)
Pairwise and end-to-end pipeline integration:
- Shape generation -> Schedule aggregation -> Cutting-Stock solver -> ClosedXML export -> QR tag.
- AutoCAD DWG XData -> Core Schedule -> Coupler summary -> Excel export.
- Revit Rebar parameters -> Parameter bridge -> Shape adapter -> Cutting-Stock optimization.
- Config mutations -> Reactive ViewModel update -> Detailing parameter changes -> Dynamic cut length recalculation.

#### Tier 4 — Real-World Application Scenarios (`Tier4_RealWorldScenarios/`)
Complete structural workflows exercising realistic engineering problems:
1. **Scenario 1 — Continuous Multi-Span Beam**: Bottom bars, top bars, splice offsets at $L/4$, varying stirrup spacing ($100/200\text{ mm}$), schedule generation, cutting stock.
2. **Scenario 2 — High-Rise Column Cage**: Vertical bars with mechanical couplers, master ties with 135° hooks, inner cross ties with 90°/135° hooks.
3. **Scenario 3 — Foundation Mat & Footing**: Top and bottom orthogonal rebar meshes with 90° anchorage hooks, stand chairs (`dang26`), cutting stock on 11.7m bars.
4. **Scenario 4 — Full Building Cutting Stock & Offcut Warehouse**: 200+ rebar items across various diameters, multi-heuristic optimization, zero-waste patterns, offcut re-utilization.
5. **Scenario 5 — Dual-Host CAD ↔ Revit Round-Trip**: Detailing in CAD -> export schedule -> import/map to Revit Rebar parameters -> verify zero metric distortion.

### 4. Publish `TEST_READY.md` at Project Root
Upon achieving 100% test pass rate, write `TEST_READY.md` summarizing:
- Test runner invocation command
- Tier-by-tier coverage breakdown
- Complete feature checklist
- Total test count ($\ge 400$ total solution tests expected: 287 existing + $\ge 120$ E2E tests).

---

## Mandatory Resource Directive
- Every `dotnet build` or `dotnet test` must include `--disable-build-servers -m:1`.
- For test runs, use `--results-directory "$env:TEMP\mtp-testm7-$([guid]::NewGuid())"` to prevent test results locking collisions.
- Do NOT spawn full builds or background build servers in parallel.

---

## Clean Code Governance
- Rule C2: all test files $\le 300$ physical lines.
- Rule M3: all test method bodies $\le 50$ physical lines.
- Rule Q-B1: 0 dead code.
- Rule Q-B2: 0 unannotated catches.
- Rule R5: 0 unit conversion factors outside `RevitUnitConverter.cs`.
- Clean-Room: `C:\kata_pro` is strictly read-only. 0 references to `Security.dll` or licensing routines.

---

## Mandatory Verification Sequence
1. Build solution:
   ```powershell
   dotnet build HPKata/HPKata.slnx -c Release --disable-build-servers -m:1
   ```
   *Required*: 0 errors, 0 warnings under `TreatWarningsAsErrors`.
2. Run test suite:
   ```powershell
   dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-testm7-$([guid]::NewGuid())"
   ```
   *Required*: 100% tests pass green across all 5 test projects (`HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, `HPKata.E2E.Tests`).
3. Deliver `handoff.md` in `.agents/teamwork/test_writer_m7/handoff.md`.


## 2026-10-17T05:33:43Z
You are test_writer_m7, the E2E Test Suite Writer for Milestone M7 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\test_writer_m7\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).
