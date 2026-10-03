# Kata section drawn as CAD on the canvas

Contract (grill-me 2026-10-03, user confirmed "implement"):
- Click a section flag (upper or lower) on Kata's elevation → panel right of canvas (~35 % width) draws section n-n
  exactly as T2-DY7.dwg: slab + break lines, b dim, h dims (chain + overall), title "n-n / TL: 1/25", hoop with
  bent corners + 135° hooks, C ties, bars as kata_block_THEP, leaders (DotBlank / DotSmall / closed arrows),
  layer-2 marking circles, KHT tags. Lineweights/colours per Kata layers (fixed px like elevation).
- Selected flag pair highlighted. Click on a span → its middle cut (as before). Narrow canvas → panel hidden.
- "Hiện thép" off → old corner card unchanged.
- Bars drawn as Kata draws them (hoop centreline at cover, bars touching it: 38 not real 42).

| Phase | Status |
|---|---|
| 1 Core model + builder (`KataSectionDrawing*`), golden tests DY7 1/2/4/5/7/8, DY14 (E 350) 1/4/5 | done |
| 2 WPF panel painter, flag hit-test + highlight, framing | done |
| 3 Build R26/R25/R24, tests, review | done |
| 4 Live DY7 in Revit 2026 (second Revit, model copy, scratch workbook): flags 4 (over) and 7 (under) show 4-4 / 7-7 highlighted, span click back to 2-2, Thép off = old card, no log errors | done |

Rules read from the DWG: [reports/dwg-section-rules.md](reports/dwg-section-rules.md).
