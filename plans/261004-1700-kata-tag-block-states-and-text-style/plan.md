# Kata tags as kata_block_KHT states + kata_text font on the canvas

Status: implemented + live-verified 2026-10-04 (Revit 2026.4, DY7 5-5 = user picture), not committed. Review: [reports/code-review.md](reports/code-review.md). Contract by grill-me (user: "implement, clean code").

## Evidence (T2-DY7.dwg, read-only via MCP AutoCAD, runs 739–743)
- Text style `kata_text` = Arial, width factor 0.8, h 62.5 (2.5 × 25). Canvas drew Segoe UI → ~25 % wider.
- `kata_block_KHT` visibility states = side (P circles before insertion / T after) + circles (1/2) + layout:
  1 one line on leader (DK, baseline +21.14, x +12.5 / −21.25), 2 bars on leader + spacing under it (KC baseline −74.31),
  3 one line centred on row (DKKC baseline −31.25). Circles r 62.5 at ±62.5 / ±187.5, numbers middle-centred.
- Every stirrup / tie tag of every section (DY7, both DY14 runs) is P12; bar tags P11 / T11; elevation stirrups T13.
- The user's reference picture = DY7 section 5-5.

## Changes
- Core: `KataTagLayout` enum, `KataTagStyle.BlockState`, `TextLift` 21.14, `SpacingDrop`, `CentredDrop`;
  `KataSectionTag.Spacing` (+ `Layout`, `BlockState`); `KataSectionTags` builds stirrup/tie tags as bars + spacing.
- Golden: K lines carry the state (L/R → P12/P11/T11, 68 lines), mutation-checked.
- Canvas: `KataCadText` = Arial ×0.8, caps height from the glyphs; `KataCadTag` draws DK / KC / DKKC through it.

## Checks
Core 1269/1269 · build R26 · off-Revit render 5-5 vs user picture · live Revit · code review.
