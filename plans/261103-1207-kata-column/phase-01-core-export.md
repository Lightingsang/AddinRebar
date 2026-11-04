# Phase 1 — Core export: column stack → sheet `Cot` values

## Overview
Priority high. Status ✅ 2026-11-03 (Core 1717/1717, build Debug.R26). Pure `HPRebar.Core/KataExport/ColumnStack/` (folder not `Column`: it shadowed the beam tests' `Column(...)` helper) (netstandard2.0, no Revit) + xUnit. Contract:
[reports/kata-column-cell-contract.md](reports/kata-column-cell-contract.md).

## Input model (`KataColumnStackInput`)
- Name, count; storeys bottom→top: level name, base/top elevation (mm), b, h (mm, column local X / Y), face offsets of
  the section relative to the storey below (left/right along X, bottom/top along Y, mm), beam depth at the head (mm).
- Optional pile cap: bottom elevation (mm), height (mm). Optional crossing-beam width at the head (mm), slab thickness (mm).

## Output (`KataColumnSheet`)
| Cells | Value |
|---|---|
| C1 | widest crossing beam at the first storey head (mm), else keep |
| C2 | slab thickness (mm), else keep |
| C3 / C4 | name / count |
| C5 | pile-cap bottom (m, 3 decimals) — or base of the first storey when no cap |
| A9 / B9 | `PILECAPS` / cap height (m) when a cap is found |
| A10.. / B | level name / storey height (m) |
| C / D | b / h; **sign**: −value when the storey keeps the left (bottom) face of the storey below, +value when centred on it; first storey positive |
| K | beam depth at the storey head (mm) |
| below the stack | rows A..V cleared (Kata reads storeys until A is empty) |

Bars, J, hoops/ties (E–I, J, L–V, F4, F5, H4, H5, flags) are user input — never written.

## Rules + warnings
- Offset fits neither "same left face" nor "centred" within 1 mm → write the nearer sign, warning `"<storey>: lệch tâm
  x mm — Kata chỉ vẽ bóp 1 bên / 2 bên"`.
- Upper storey larger than the lower one → written as is, warning (Kata semantics for growth = same rule, unverified in Revit).
- Circular column → C = diameter, D = 0 (verified by VC variants r01–r04); round ↔ rect mixed stacks allowed.
- Validation: storeys contiguous (top = next base ± 1 mm), b, h > 0, heights > 0.

## Tests (xUnit, `HPRebar.Core.Tests/KataExport/Column/`)
- Golden 1 stack (cap 1.5 m, 6 storeys, 1000×800, K 600/600/400/400/600/400) → exact cell values of Kata.xlsm sample.
- v01/v02/v05 offsets → C13 −800 / +800 / −500; FLOOR 4 back to 1000 centred → +1000.
- Eccentric offset → warning; non-contiguous → error.

## Success criteria
Build `Debug.R26`; Core tests green; no Revit reference in Core.
