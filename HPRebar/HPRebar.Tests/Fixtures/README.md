# Test fixture model

The TUnit tests in this project and the golden runs use one Revit model, `column-stack-2-storey.rvt`. It is
**generated**, not hand-built: `HPRebar/tools/golden-run/scripts/build-fixture.csx`, run once through the Revit MCP
(`execute_revit_code`, `transaction: manual`) in Revit 2026, builds it from the out-of-the-box template
`Structural Analysis-DefaultMetric.rte` and the English\US family library, checks every Mark, shape and bar type,
and saves it. `golden/fixture-meta.json` records the Revit build and the file's sha256. To change the model, edit
the script and regenerate — never edit the `.rvt` by hand.

`.gitattributes` at the repo root routes `*.rvt` through git-lfs.

The model has three areas: **A** at the origin (the column stack below, used by the TUnit tests and the Column
golden run), **B** at x + 20 m (a beam run, Beam golden run) and **C** at y + 20 m (a foundation slab, Foundation
golden run).

## Levels

| Name | Elevation |
|---|---|
| Foundation | 0 |
| Level 1 | 3000 |
| Level 2 | 6000 |

## Area A — column stack

All columns are the same rectangular concrete family, and every column carries a **Mark** — the tests find them by
Mark, nothing else.

| Mark | Base → Top | Size (b × h) | Notes |
|---|---|---|---|
| `C1-LOWER` | Foundation → Level 1 | 400 × 600 | Joined to the foundation; sits on its top face |
| `C1-UPPER` | Level 1 → Level 2 | 300 × 500 | Offset so its **west face is 50 mm east** of `C1-LOWER`'s west face; not rotated; fully inside `C1-LOWER` in plan |
| `C-SLANTED` | Foundation → Level 1 | 400 × 600 | Slanted (not vertical) — any other geometry is fine |
| `C2-DETACHED` | Level 2 → above | 300 × 500 | Deliberately **not** touching `C1-LOWER`'s head, so the pair reads as discontinuous |
| `C3-LOWER` | Foundation → Level 1 | 300 × 500 | |
| `C3-WIDER` | Level 1 → Level 2 | 400 × 600 | Wider than `C3-LOWER`, which is not allowed going up |

Plus:

- **Foundation** `FTG-1` — one 8000 × 1600 × 600 structural footing under `C1-LOWER`, `C-SLANTED` and `C3-LOWER`,
  on the Foundation level, joined to each of them (the footing cuts the column), with its top face flush with the
  column bases.
- **Beam** `B-C1` — one 300 × 500 structural framing element whose reference level is **Level 1**, framing into the
  head of `C1-LOWER`, joined to it and not cutting it. Its top face is flush with the column head, which puts the
  soffit 500 mm below — that is what makes `Hb` and `Zb` both read 500.
- Nothing frames into the head of `C1-UPPER`.

## Area B — beam run (x + 20 m)

| Mark | What |
|---|---|
| `BR-C1`, `BR-C2`, `BR-C3` | 400 × 400 columns, Foundation → Level 1, at x = 20000 / 26000 / 31000 |
| `BR-B1`, `BR-B2` | 300 × 600 beams on Level 1, `BR-C1` → `BR-C2` → `BR-C3`, each column cutting them |
| `BR-CANT` | 300 × 600 cantilever from `BR-C3` to a free end 1800 further |
| `BR-SEC` | 250 × 450 secondary beam into mid-span of `BR-B1` (cut by it), 3000 long, ending on column `BR-C4` |

## Area C — foundation slab (y + 20 m)

`FND-SLAB`: a structural Floor (the Foundation feature takes the Floors category), 6000 × 4000 × 600, top face on
the Foundation level.

## Rebar shapes and bar types

The shape families `M_T1` (rectangular tie), `M_T3` (circular tie) and `M_10` (the beam feature falls back to it —
the library has no `M_T10`) are loaded. The template's bar types are replaced by `D16, D10, D25, D12, D20, D8`,
created **in that order**, so element-id order differs from diameter order and an id/index mix-up shows in the
golden runs.

## Sanity check

`dotnet run --project HPRebar.Tests -c Debug.R26` should report every test as passed rather than skipped.
