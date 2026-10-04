# Kata canvas — floating section + delete bar groups

Status: implemented + live-verified 2026-10-04 (Revit 2026.4, DY7 scratch copy), not committed.
Contract agreed by grill-me (user "implement"). Review: [reports/code-review.md](reports/code-review.md).
Live: flag 2 → panel; wheel/middle-drag in panel only; click 3Ø18 bottom extra span 2 → Delete → 196 bars /
351.3 kg; Ctrl+Z → 199 / 379.2; Tạo thép → "5 thanh gia cường nhịp" (8 − 3), log "3 bar group(s) removed";
Esc closes panel; column click does not open it.

## Goal
1. Section n-n: small floating panel top-right (~320×360 px), opens ONLY on a section-flag click, ✕ / Esc closes,
   own zoom (wheel) / pan (middle drag, Shift+left drag) / fit (middle double click). Column pick never opens it.
2. Delete bar groups on the elevation: click a drawn line → its group highlighted → Delete removes it, Ctrl+Z undoes.
   Numbers re-run (`KataBarNumbering`) → tags, sections, Revit Schedule Mark follow. "Tạo thép" skips removed groups.
   Re-reading Excel / another beam / new preview = back to the original. Sheet Dam never written.

## Design
- Core `KataLayoutRemoval`: stable keys (`bar|role|BarId|Ø|len`, `zone|span|zone|count`), `Remove(plan, keys)`
  = filtered layout + numbers reset + `KataBarNumbering.Apply` + weight recomputed; unknown keys reported.
- `KataDrawingLine.Keys`: the groups a drawn line stands for (bars drafted on one line, zones merged in one run).
- VM keeps the unedited plan + removal stack; `RebarPlan` = edited plan; generate passes the keys,
  `KataRebarWorkflow.Generate` re-plans then removes the same keys (model = canvas).
- Canvas: `RemoveBarsCommand` / `UndoRemoveCommand` DPs; hit-test 6 px, repeated click cycles overlapping lines.

## Out of scope
Excel write-back, single-bar delete, delete in the section, persistence across re-read, C ties / inner stirrup
sets (no elevation line), the Kata settings dialog (separate contract).

## Checks
Core tests (removal, renumber, keys stable across re-plan, drawing keys) · build R26/R25/R24 · code review ·
off-Revit render of panel · live Revit 2026 on scratch copies.
