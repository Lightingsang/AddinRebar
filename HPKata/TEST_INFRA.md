# HPKata End-to-End Test Infrastructure Architecture (TEST_INFRA)

## 1. Test Philosophy & Overview
The HPKata E2E test suite (`HPKata.E2E.Tests`) provides opaque-box, requirement-driven verification of the standalone structural rebar detailing, scheduling, cutting-stock optimization, and drawing interop suite installed centrally at `C:\HPKata\`.

The test methodology enforces a strict separation between test specification and implementation design:
- **Opaque-Box Requirement-Driven**: Tests assert externally observable behavior, canonical contracts, file formats, and UI rendering without probing private implementation mechanics or internal helper classes.
- **Zero Live Host Mutation**: Tests execute safely in headless CI/CD environments without modifying host application registries, production AutoCAD installations, or live Revit documents.
- **Dual-Host & Dual-Theme Parity**: AutoCAD 2026 and Revit 2026 detailing parity, along with WPF dark and light theme fidelity, are mathematically and visually verified.
- **Reference Oracle Grounding**: Test inputs and expected structures are derived directly from `ORIGINAL_REQUEST.md` (lines 1450–1512), `PROJECT.md`, and authentic assets in `C:\kata_pro\`.

---

## 2. Complete 21-Feature Inventory & Testing Matrix

The 21 features defined in `PROJECT.md § Feature Inventory` are mapped across all 4 testing tiers:

| # | Feature Area | Requirement Reference | Tier 1 (Coverage) | Tier 2 (Boundary) | Tier 3 (Cross) | Tier 4 (Scenario) |
|:---:|---|---|:---:|:---:|:---:|:---:|
| **F01** | `KataInstallEnvironment` Service | Milestone 1 / R1 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F02** | Asset Migration & Staging | Milestone 1 / R1 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F03** | LISP & Menu Path Rewriting | Milestone 1 / R1 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F04** | Codebase `kata_pro` Purge | Milestone 1 / R1 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F05** | Bi-Directional Excel Engine | Milestone 2 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F06** | ViewModel Excel Integration | Milestone 2 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F07** | AutoCAD Detailing Commands Sync | Milestone 2 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F08** | Revit Detailing Commands Sync | Milestone 2 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F09** | `KataDwgTemplateImporter` | Milestone 3 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F10** | 86-Parameter Config Sync | Milestone 3 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F11** | Ribbon Icon Loading | Milestone 3 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F12** | Visual Diagram Binding | Milestone 3 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F13** | `HPKata.Installer` Project | Milestone 4 / R3 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F14** | WPF Installer GUI | Milestone 4 / R3 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F15** | Headless Installer CLI | Milestone 4 / R3 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F16** | Centralized DLL & Asset Copy | Milestone 4 / R3 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F17** | Revit Add-in Manifest | Milestone 4 / R3 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F18** | AutoCAD Bundle Manifest | Milestone 4 / R3 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F19** | AutoCAD `TRUSTEDPATHS` Reg | Milestone 4 / R3 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F20** | E2E Regression Pass | Milestone 5 / M5 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F21** | Adversarial Hardening (Tier 5) | Milestone 5 / M5 | ≥ 5 | ≥ 5 | ✓ | ✓ |

---

## 3. Four-Tier Testing Methodology

```
+-------------------------------------------------------------------------+
|                  Tier 4: Real-World Production Scenarios                |
|  Continuous Beam | Column Cage | Mat Foundation | 200-Bar Cut | Round-Trip |
|  Standalone Deployment & Asset Verification Lifecycle Scenario         |
+-------------------------------------------------------------------------+
|            Tier 3: Cross-Feature Multi-Module Pipelines                 |
|   Setup -> Environment -> Excel Sync -> Detailing Window -> STA UI      |
|   Template DWG -> Blocks -> Commands -> XData -> Rebar Export           |
+-------------------------------------------------------------------------+
|             Tier 2: Boundary Value Analysis & Stress Bounds             |
|   Corrupted LISP | Missing Assets | Read-Only Dirs | Excel Lock Fails   |
|   Empty Workbooks | Diacritics | Malformed CLI Flags | Extreme Bounds   |
+-------------------------------------------------------------------------+
|              Tier 1: Category-Partition Feature Coverage                |
|   Isolated verification for all 21 individual feature contracts         |
+-------------------------------------------------------------------------+
```

### Tier 1 — Category-Partition Feature Coverage
Validates functional contracts, default values, canonical paths, and core logic in strict isolation.
- **Coverage Areas**:
  1. Config & 86 Parameters parsing and serialization (`Tier1_F01`).
  2. Rebar geometry formulas and segment calculators (`Tier1_F02`).
  3. Hook, bend, and lap calculations (`Tier1_F03`).
  4. XData two-way serialization (`Tier1_F04`).
  5. 1D cutting stock optimization algorithms (`Tier1_F05`).
  6. Schedule summaries and diameter bins (`Tier1_F06`).
  7. Coupler tracking and deductions (`Tier1_F07`).
  8. QR tag generation and payload format (`Tier1_F08`).
  9. Headless ClosedXML schedule generation (`Tier1_F09`).
  10. AutoCAD detailing command wrappers (`Tier1_F10`).
  11. Revit parameter bridging and rebar commands (`Tier1_F11`).
  12. Host neutrality & MVVM bindings (`Tier1_F12`).
  13. Standalone `C:\HPKata\` environment & canonical paths (`Tier1_F13`).
  14. Zero `kata_pro` occurrences audit (`Tier1_F14`).
  15. Bi-directional Excel roundtrip (`Dam`, `Cot`, `Vach`, `Iso_QHPlus.xls`) (`Tier1_F15`).
  16. AutoCAD template importing and 119 block catalog (`Tier1_F16`).
  17. Headless CLI installer and manifest authoring (`Tier1_F17`).
  18. STA Thread instantiation for all 13 WPF windows (Dark & Light) (`Tier1_F18`).
  19. Ribbon icons and visual diagram binding (`Tier1_F19`).

### Tier 2 — Boundary Value Analysis & Stress Bounds
Examines defensive behavior under invalid, corrupted, or extreme conditions:
- Corrupted LISP config lines, missing semicolons, negative diameters.
- Invalid CLI installer parameters (`--invalid`, null target directories, permission denies).
- Missing template DWG fallback to in-memory standard layers.
- Empty Excel sheets, missing sheets, malformed headers in `Dam`/`Cot`/`Vach`.
- Unicode/Vietnamese diacritics in rebar marks, member names, and folder paths.
- Extreme spans (1mm to 50m) and cutting stock kerf limits.

### Tier 3 — Cross-Feature Integration Pipelines
Validates end-to-end workflows across modular subsystems:
- **Pipeline 1**: `KataInstallEnvironment` -> LISP config load -> `SetupViewModel` -> GUI update.
- **Pipeline 2**: `Kata.xlsm` (`Dam`/`Cot`) -> Excel reader -> Detailing ViewModels -> Shop drawings.
- **Pipeline 3**: Template importer (`Iso_kata.dwg`) -> 119 Blocks -> Detailing command -> XData.
- **Pipeline 4**: Installer CLI (`--install --target-dir`) -> Directory staging -> Manifest generation -> AutoCAD registry.
- **Pipeline 5**: Detailing model -> ClosedXML export -> QR payload -> Verification.

### Tier 4 — Real-World Engineering Scenarios
Simulates realistic, multi-step structural engineering operations:
1. **Continuous Multi-Span Beam**: L/4 curtailment, zoned stirrups, Excel synchronization, schedule generation.
2. **High-Rise Column Cage**: Multi-story column splices, mechanical couplers, tie sets.
3. **Core Shear Wall Detailing**: Boundary elements, web rebar meshes, elevation step-downs.
4. **Building Cutting Stock Solver**: 200+ rebar cuts optimized into 11.7m stock bars with remnant tracking.
5. **Dual-Host CAD ↔ Revit Roundtrip**: Geometric rebar transfer with zero metric distortion.
6. **Standalone Deployment Lifecycle**: Complete installer unpack, environment verification, asset resolution, and window launch.

---

## 4. Quality & Clean Code Standards

All test code in `HPKata.E2E.Tests` strictly adheres to Clean Code Governance:
- **Rule C2**: File length ≤ 300 physical lines.
- **Rule M3**: Method length ≤ 50 physical lines.
- **Rule Q-B1**: Zero commented-out or dead code.
- **Rule Q-B2**: Zero unannotated empty catch blocks.
- **Zero Warnings**: Solution builds with `TreatWarningsAsErrors=true`.
- **STA Thread Isolation**: WPF window tests run on dedicated STA threads with proper Dispatcher shutdown to prevent UI thread deadlocks.

---

## 5. Verification Commands

Run the full test suite using the single-node build command:
```powershell
dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
```

Run only E2E tests:
```powershell
dotnet test HPKata/HPKata.E2E.Tests/HPKata.E2E.Tests.csproj -c Debug --disable-build-servers -m:1
```
