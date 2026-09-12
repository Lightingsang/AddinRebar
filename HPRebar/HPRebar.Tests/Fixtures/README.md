# Test fixture model

The TUnit tests in this project run against one Revit model, `column-stack-2-storey.rvt`, which is **not
generated** — it has to be built by hand once and committed. Until it is present every test in
`ColumnStackReaderTests` and `ColumnStackValidatorTests` skips with a message naming this file.

Build it in **Revit 2026**, metric project template, and save it here as `column-stack-2-storey.rvt`.

`.gitattributes` at the repo root already routes `*.rvt` through git-lfs, so commit it normally.

## Levels

| Name | Elevation |
|---|---|
| Foundation | 0 |
| Level 1 | 3000 |
| Level 2 | 6000 |

## Elements

All columns are the same rectangular concrete family type, varied by instance dimensions, and every column
carries a **Mark** — the tests find them by Mark, nothing else.

| Mark | Base → Top | Size (b × h) | Notes |
|---|---|---|---|
| `C1-LOWER` | Foundation → Level 1 | 400 × 600 | Joined to the foundation; sits on its top face |
| `C1-UPPER` | Level 1 → Level 2 | 300 × 500 | Offset so its **west face is 50 mm east** of `C1-LOWER`'s west face; not rotated; fully inside `C1-LOWER` in plan |
| `C-SLANTED` | Foundation → Level 1 | 400 × 600 | Slanted (not vertical) — any other geometry is fine |
| `C2-DETACHED` | Level 2 → above | 300 × 500 | Deliberately **not** touching `C1-LOWER`'s head, so the pair reads as discontinuous |
| `C3-LOWER` | Foundation → Level 1 | 300 × 500 | |
| `C3-WIDER` | Level 1 → Level 2 | 400 × 600 | Wider than `C3-LOWER`, which is not allowed going up |

Plus:

- **Foundation** — a structural foundation under `C1-LOWER`, `C-SLANTED` and `C3-LOWER`, on the Foundation
  level, joined to each of them, with its top face flush with the column bases.
- **Beam** — one 300 × 500 structural framing element whose reference level is **Level 1**, framing into the
  head of `C1-LOWER`, joined to it and not cutting it. Its top face must be flush with the column head, which
  puts the soffit 500 mm below — that is what makes `Hb` and `Zb` both read 500.
- Nothing frames into the head of `C1-UPPER`.

## Rebar shapes

Load the rebar shape families `M_T1` (rectangular tie) and `M_T3` (circular tie) into the model. Phase 4
looks them up by name; the readers tested here do not need them, but keeping them in one fixture avoids a
second model later.

## Sanity check

Once saved, `dotnet run --project HPRebar.Tests -c Debug.R26` should report every test as passed rather than
skipped.
