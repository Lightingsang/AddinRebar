# HPKata End-to-End Test Infrastructure Architecture (TEST_INFRA)

## 1. Test Philosophy & Overview
The HPKata E2E test suite (`HPKata.E2E.Tests`) provides opaque-box, requirement-driven verification of the dual-host structural rebar detailing, scheduling, cutting-stock optimization, and drawing interop system. Tests strictly validate observable functional contracts without probing internal private implementation mechanics.

- **Authoritative Specifications**:
  - `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`
  - `PROJECT.md`
  - `docs/clean-code/HP_CLEAN_CODE_CORE.md`
- **Runner Environment**: .NET 8 (`net8.0-windows`), `xunit.v3` (v3.1.0), `Microsoft.Testing.Platform` runner.
- **Isolation Directives**: In-memory test doubles (`E2ECadContext`, `E2ERevitContext`), zero live `%AppData%` mutation, and zero dependency on running AutoCAD or Revit instances.

---

## 2. Feature Inventory Matrix (12 Major Areas)

| Area ID | Major Feature Area | Requirement Reference | Tier 1 (Coverage) | Tier 2 (Boundary) | Tier 3 (Cross) | Tier 4 (Scenario) |
|:---:|---|---|:---:|:---:|:---:|:---:|
| **F01** | Config & 86 Parameters | Feature 2 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F02** | Rebar Shapes & Geometry | Feature 3 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F03** | Hook, Bend & Lap Calculations | Feature 4 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F04** | XData 2-Way Serialization | Feature 5, 6 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F05** | 1D Cutting Stock Optimization | Feature 8 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F06** | Schedule & Diameter Summaries | Feature 7 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F07** | Coupler Tracking & Deductions | Feature 7 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F08** | QR Tag Generation & Encoding | Feature 9 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F09** | Headless ClosedXML Export | Feature 10 / R2 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F10** | AutoCAD Detailing Commands | Features 15-18 / R3 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F11** | Revit Host Integration | Features 19, 20 / R3 | ≥ 5 | ≥ 5 | ✓ | ✓ |
| **F12** | Cross-Platform & Reactive UI | Features 12-14, Host Neutrality | ≥ 5 | ≥ 5 | ✓ | ✓ |

---

## 3. Four-Tier Testing Methodology

```
+-------------------------------------------------------------------------+
|                  Tier 4: Real-World Scenarios (5 Workloads)             |
|  Continuous Beam | Column Cage | Mat Foundation | 200-Bar Cut | Round-Trip |
+-------------------------------------------------------------------------+
|            Tier 3: Cross-Feature Combinations (≥ 12 Tests)              |
|   Shape -> Schedule -> Cut -> ClosedXML -> QR | CAD XData <-> Revit     |
+-------------------------------------------------------------------------+
|             Tier 2: Boundary & Corner Cases (≥ 60 Tests)                |
|   1mm .. 24m | Zero/Neg Dim | Kerf 0..20mm | Empty/Degenerate Shapes    |
+-------------------------------------------------------------------------+
|              Tier 1: Feature Coverage (≥ 60 Tests)                      |
|   Isolated verification across all 12 major functional areas             |
+-------------------------------------------------------------------------+
```

### Tier 1 — Category-Partition Feature Coverage
Validates the happy path, standard options, and functional contracts for all 12 feature areas in strict isolation. Each area contains at least 5 tests.
- **Target**: ≥ 60 tests (5 × 12 areas).

### Tier 2 — Boundary Value Analysis & Stress Bounds
Evaluates numerical limits, zero/negative inputs, empty lists, degenerate geometry, saw kerfs, long-span splices, and diacritic string encodings.
- **Target**: ≥ 60 tests (5 × 12 areas).

### Tier 3 — Cross-Feature Interaction Matrix
Examines end-to-end multi-module pipelines:
- Pipeline 1: Geometry creation -> Schedule summary -> Cutting stock solver -> Excel export -> QR tag verification.
- Pipeline 2: AutoCAD XData entity parsing -> Core rebar schedule -> Coupler summary -> Excel generation.
- Pipeline 3: Revit Rebar parameters -> Parameter bridge -> Shape adapter -> 1D cutting stock solver.
- Pipeline 4: Config mutations -> Reactive ViewModel updates -> Cut length recalculation.
- **Target**: ≥ 12 tests.

### Tier 4 — Real-World Engineering Scenarios
Five realistic, end-to-end structural detailing workflows:
1. **Scenario 1**: Continuous Multi-Span Beam (main continuous bars, top extra bars at supports L/4, zoned stirrups 100/200mm, schedule & cutting stock).
2. **Scenario 2**: High-Rise Column Cage (vertical rebars with mechanical couplers, master ties with 135° seismic hooks, inner cross ties).
3. **Scenario 3**: Foundation Mat & Footing (orthogonal rebar meshes, 90° anchorage hooks, stand chairs `dang26`, 11.7m stock cutting).
4. **Scenario 4**: Full Building 200+ Item Cutting Stock (multi-diameter inventory, stock optimization, offcut warehouse management, scrap tracking).
5. **Scenario 5**: Dual-Host CAD ↔ Revit Round-Trip Detailing (CAD detailing -> core schedule -> Revit parameter mapping -> zero metric loss).
- **Target**: 5 comprehensive scenarios.

---

## 4. Quality & Clean Code Standards
- **Rule C2**: All test source files ≤ 300 physical lines.
- **Rule M3**: All test method bodies ≤ 50 physical lines.
- **Rule Q-B1**: Zero commented-out or dead code.
- **Rule Q-B2**: Zero unannotated empty catch blocks.
- **Rule R5**: Zero unit conversions outside `RevitUnitConverter.cs`.
- **Clean-Room**: Zero references to `Security.dll` or license verification logic.
