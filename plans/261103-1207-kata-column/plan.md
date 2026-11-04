# Kata Column: Revit → sheet `Cot` → CAD (Kata Pro VC) + Revit 3D rebar

Request (2026-11-03): tool like Kata Export for columns — column geometry from Revit into the Kata workbook; from the
Kata workbook the CAD drawing (Kata Pro) and the Revit rebar. Requirement contract agreed by interview (`/grill-me`).

## Decisions (user)

| Item | Choice |
|---|---|
| CAD | Kata Pro's own `VC` (`ve_cot`) draws from sheet `Cot`; no HP CAD code |
| Revit rebar | 3D + numbering, all VC draws: main bars per storey (lap / crank / cut+anchor), starter bars in pile cap, top anchorage, outer hoop 3 zones, □ + C ties. Coupler later |
| Bar geometry | real Revit faces + cover; Kata gives counts, Ø, hoops, splice position |
| Acceptance | parity with VC DWG of sample columns: bar count/Ø, splice/crank/anchor, stirrups per zone ±1, positions ±10 mm |
| Scope / UI | columns only (`Vach` later); one button `Kata Column Rebar` (merged 2026-11-03 by user: like Kata Export — geometry → Excel, bars ← Excel, real rebar in Revit; page step 1 / step 2); web page, Revit 2026 only; ADR-0014 |
| Golden DWG | Claude drives VC by AutoCAD COM on copies; fallback user runs VC + saves DWG |
| Column step (Kata draws none) | Revit: crank 1:6 inside the beam depth when the offset is small (default ≤ 75 mm), else lower bars stop under the slab with an F5×d bend and new starter bars lap 30d (2026-11-03) |
| Top of the stack | topmost selected column = roof → bars stop one cover below the top and bend F5×d inward; a column above (not selected) → lap 30d up like Kata (2026-11-03) |
| Source of truth | real `C:\kata_pro\Kata.xlsm` sheet `Cot` + measured VC. `SPEC_VeCot_Kata.md` = hints only (its sheet `Thep cot` A–N with ex/ey does not exist) |

Initial sheet read: [reports/kata-column-sheet-initial-read.md](reports/kata-column-sheet-initial-read.md).

## Placement

- Kata = one feature (ADR-0006): `HPRebar/HPRebar/KataExport/` with `KataColumn*` names; commands beside
  `KataSettingsCommand.cs`; windows `KataExport/View/Web/Column/`.
- No reference to `ColumnRebar/` (ADR-0004, B-45). Pure rules: `HPRebar.Core/KataExport/Column/` (export),
  `HPRebar.Core/KataRebar/Column/` (rebar plan).
- Reuse: `ExcelComAttach`, `ComLateBinding`, `KataHostCoverService`, `KataTransactionRunner`, `KataRebarCleanupService`,
  `KataRebarNumberAssigner`, `KataRebarTypeResolver`, `KataRebarHookResolver`, `KataRebarStamp/Storage`,
  `KataStirrupCoverFit`, `KataRebarCurveFactory`; Core `KataBarNotationParser`, `KataCellTable`, `KataExcelCell`,
  `KataFormat`, `KataPageFixtures`; `HPWebHost*` unchanged.
- Generalise, beams byte-identical: sheet/range parameters on `KataDamComReader` and `KataExcelWriter.Target`.
- Frontend `column.html` + `src/kataColumn/`; `vite.config.ts`, `embed-frontend.targets`, `tools/build-frontend-once.mjs`.

## Phases

| # | Phase | Type | Depends | Status |
|---|---|---|---|---|
| 0 | [VC spike + cell contract](phase-00-vc-spike-cell-contract.md) — golden 1 = user's `Cot.dwg` + 19 VC variants, [contract draft 2](reports/kata-column-cell-contract.md); step + top rules decided; Kata default lap start (J empty) → phase 3 | HITL-light | — | ✅ |
| 1 | [Core export](phase-01-core-export.md): stack input → `KataColumnSheet` (+ shrink sign rule, round, warnings) — `HPRebar.Core/KataExport/ColumnStack/`, 13 tests | AFK | 0 | ✅ |
| 2 | Revit export: stack reader, `Cot` writer, command + web page, ribbon R26, [ADR-0014](../../docs/architecture/adr/0014-kata-column-export-web-page.md) — built R26 + R23, Core 1719, vitest 212; review 7/10 fixed; [live](reports/live-phase-02.md) Revit → sheet → VC ✅ | AFK + live | 1 | ✅ |
| 3 | [Core rebar plan](phase-03-core-rebar-plan.md): sheet reader + planner, parity tests on P0 fixtures — reader, section (layer 1/2, round), hoop zones, auto ties ✅; longitudinal chains (J laps, A/B stagger, starters, crank/cut, dowels, top anchor) ✅ — 56 tests; [review](reports/code-review-phase-03.md) 5.5/10 fixed | AFK | 0 | ✅ |
| 4 | Revit rebar: creator, bar types, one button `Kata Column Rebar` (export + rebar merged by user), page step 1/2 — [live](reports/live-phase-04.md) 94 elements, rerun 94/94; [review](reports/code-review-phase-04.md) 6.5/10 fixed (L3 deferred); Revit numbers = Kata after corner bars kept in one plane | AFK + live | 2, 3 | 🟡 |
| 5 | Live parity on sample columns (visual vs VC: CHƯA TEST) + docs (CLAUDE.md, AGENTS.md, codebase-summary, changelog ✅) | HITL-light | 4 | 🟡 |

Phase files 1–5 are written when phase 0 closes (contract decides their content).

## Checks

- `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` (R26-only gate).
- `dotnet test HPRebar/HPRebar.Core.Tests`; frontend typecheck/lint/vitest/build.
- Live on copies only: scratch Kata workbook active before every Export/Read; Revit/AutoCAD started by harness, killed by pid.

## Safety

- `C:\kata_pro\Kata.xlsm` is open in the user's Excel — never written; COM tools write the ACTIVE workbook (incident
  260929). Every live step checks `ActiveWorkbook.FullName` = scratch copy first.

## Out of scope

Coupler, walls (`Vach`), `VCSHOP`, `TKCOT` / `RAICOT` / `DIMCOT`, HP-drawn CAD.
