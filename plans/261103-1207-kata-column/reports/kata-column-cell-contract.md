# Sheet `Cot` cell contract — draft 1 (2026-11-03)

Evidence pair (golden 1):
- Sheet values: copy of `C:\kata_pro\Kata.xlsm` → [kata-xlsm-cot-cells.txt](kata-xlsm-cot-cells.txt)
- Kata Pro VC output: user's `C:\Users\STR-HP03.HOANGPHUC\Documents\Cot.dwg` (open in AutoCAD, read-only via MCP runs 839–843 +
  COM dump) → [cot-dwg-raw.json](cot-dwg-raw.json) (498 entities). Drawing units = mm (INSUNITS says Inches — ignore; MCP
  `boundsMm` multiplies by 25.4).
- Match check: storey heights 1500/3000/3500/3900/4600/3300, b 1000, h 800, beam depths, title `C1 (SL=1)`, levels `-5.500 … +14.300`
  all equal the sheet → the DWG was drawn from these cells.

## Header

| Cell | Meaning | Evidence |
|---|---|---|
| C1 | width of the crossing beam drawn at each level (dashed, `kata_net khuat`) | beam 2124.6→2924.6 = 800 |
| C2 | slab thickness | dashed slab line 200 below each level |
| C3 / C4 | name / member count | title block `kata_block_TD` `C1 (SL=1)` |
| C5 | pile-cap bottom level (m) | level tag `-5.500` at bottom; next tags −4.000 (C5 + B9) … |
| F2 / F3 | elevation / section scale | title `TL: 1/25` (F2 1:30 in copy?) — **check**: drawing says 1/25 |
| F4 `30/100` | cover 30 (hoop line 30 inside face); `/100` = bottom cover in pile cap (starter bar bottom 100 above cap bottom) | dim `100` at cap bottom, hoop lines 30 in |
| F5 | starter-bar foot length in d? foot drawn 375 = 15 × 25 | dims `375` both feet |
| H4 / H5 | outer hoop / extra tie spec actually drawn (Ø10a100/200, **Ø8a400**) | marks 7 (`Ø10a100`, `Ø10a200`) and 8–12 (`Ø8a400`) |
| G3..K3 | unknown flags | — |

## Storey rows

| Col | Meaning | Evidence |
|---|---|---|
| A | storey name; A9 `PILECAPS` = pile cap row | texts at each level |
| B | storey height (m), B9 = cap height | grid lines / level tags |
| C / D | b / h (mm) | section dims 1000 × 800 |
| E / F | bars along face b, layer 1 / layer 2 (incl. corners) | section 1-1: outer ring 7 per b face |
| G / H | bars along face h, layer 1 / layer 2 (incl. corners) | outer ring 7 per h face (5 intermediate) |
| I | Ø: single = all; `a/b` = layer 1 Ø a, layer 2 Ø b | FLOOR 2 `25/20` → 24 Ø25 outer + 16 Ø20 inner (marks 4/5 `8Ø20+12Ø25`) |
| J | splice position — empty in golden 1 (default rule below) | — |
| K | depth of the beam at the **top** of this storey | soffit 600 / 400 below level |
| L | outer hoop for the storey | — (golden prints H4 value; same text) |
| M–V | □ / C ties per storey | golden draws Ø8a400 (= H5) although rows say d10a400 → **row ties ignored or flag-driven; check** |

Last row (FLOOR 5) is drawn as a stub with break line 2100 above its level: bars only lap up (no top anchorage). Roof /
top anchorage needs a variant.

## Main bars (golden 1)

- 50 % stagger: two groups (marks 1/2 at cap, 3, 6…), each 20 bars; group 1 lap starts at `s1`, group 2 at `s1 + lap + 250`.
- Lap = **30 d** (dim text `30d=<>`: 750 for Ø25, 600 for Ø20) — not 40 d of the spec.
- Lap start above level `s1`: BASEMENT 400 (above cap top), FLOOR 1 550, FLOOR 2 200, FLOOR 3 1550, FLOOR 4 250, FLOOR 5 850.
  Rule unknown (J empty) → derive (likely end of dense hoop zone / beam) in variants.
- Crank: upper bar sits one bar Ø outside the lower bar along the lap, then cranks in at slope 1:6 (Ø25: 25 over 150; Ø20: 20 over 120).
- Starter bars: foot 375 (15 d) at 100 above cap bottom, rise to lap.
- Layer 2 (inner) Ø changes at FLOOR 2 (`25/20`) — own marks 4/5.

## Hoops

Outer hoop Ø10 a100 dense / a200 mid (H4/L); ties Ø8a400 marks 8–12; zones dimensioned on `Defpoints` aligned dims
(e.g. BASEMENT 2100 / 500 / 300 …). Zone rule to derive in phase 3 from these dims.

## Draft 2 — VC variants (2026-11-03, dedicated acad, `VE_COT` + insertion point `0,0`)

Driver [../tools/vc-batch2.ps1](../tools/vc-batch2.ps1) (+ `dlg-watch.ps1`, `analyze.py`), cells per variant in
[../tools/variants.json](../tools/variants.json), dumps [variants/](variants/). Baseline v00 = 469 entities (user's
`Cot.dwg` = 498: user-edited). `VE_COT` reads the ACTIVE workbook, then jigs an insertion point (needs `0,0`); a run
that draws 0 entities in 2 s is a start race (v03/v07/v18 drew on retry) — not data.

Header flags = sheet checkboxes/radio (seen in Excel): D1 "Thép lớp 2 ghép sát thép lớp 1", G1 "Bố trí đai xuyên
dầm", G2 "Bố trí đai dày tại vị trí nối thép", G3 "Bố trí đai tự động với thông số" (H4/H5), J1 radio "Cắt thép ở 1
tầng: 50 % tại 1 vị trí / 50 % tại 2 vị trí / 100 % tại 1 vị trí".

| Variant | Change | VC effect → contract |
|---|---|---|
| v01 / v05 | C13 −800 / −500 | storey keeps the **left** face of the storey below, shrinks on the right; next storey with positive b is **centred on the storey below it** (1000 after 800 → −100..900) |
| v02 | C13 +800 | centred shrink both sides (100..900) |
| v04 | D13 −600 | h one-sided in the section (elevation shows the b face only — Kata elevation is one direction) |
| v01/v05 | steps 200 / 500 | **no crank, no cut+anchor**: lower bars run straight to their lap in the storey above, outside the narrower concrete; only the upper bars move in. Not buildable → Revit needs its own step rule (user decision) |
| v06 | J11 1000, J13 0 | **J = lap start above storey bottom (mm)**; 0 = at the level; empty = Kata default (BASEMENT 400, F1 550, F2 200, F3 1550, F4 250, F5 850 in golden — rule not derived) |
| v12 | J3 = 1 | bars run over two storeys (different cut scheme) — radio index; default 2 = 50 % at 2 positions |
| v13 | K3 false | hoops inside the beam depth dropped → **K3 = "đai xuyên dầm"** |
| v09 | G3 true | layer 2 bundled to layer 1 (26 bars per group, section changes; Kata warns "thép lớp 2 không cùng hàng với lớp 1") → **G3 = "lớp 2 ghép sát"** |
| v14 | L12 d12a150, H4 d8a120/250 | drawn hoops = **H4** (Ø8 a120/a250), row L ignored while auto ✓; zone lengths change with spacing; bar x shifts 2 mm (hoop Ø) |
| v08 / v07 | H5 d6a500 / row ties | tie Ø+spacing = **H5**; row counts (T/V…) change the number of ties (section bars 160) |
| v10 / v11 | H3 / I3 toggled | no visible change |
| v15 | roof row added | extra storey; **last row is always drawn as a stub** (bars lap up, no top anchorage) |
| v16 | E15 empty | Kata refuses: "Lỗi nhập dữ liệu excel … ô E15" → every storey needs bar counts |
| v17 | F4 40/150, F5 20 | cover 40 (bars 10 mm in), starter foot 150 above cap bottom, foot length F5 × d |
| v18 | C1 300, C2 120 | only dashed beam / slab lines |

## Round columns (2026-11-03, variants r01–r04, [../tools/variants-round.json](../tools/variants-round.json))

| Variant | Cells | VC effect → contract |
|---|---|---|
| r01 | all storeys C 800, D 0 | **D = 0 → circular, Ø = C**; section = circle + 1 circular hoop; elevation width = Ø |
| r02 | only FLOOR 3 C 800 D 0 | mixed stack works (rect → round → rect), round storey gets its own section |
| r03 | round, E 16, F/G/H empty | 16 bars |
| r04 | round, E 10, F 4, G 3, H 3 | 14 bars → **bar count = E + F, G/H ignored**; all bars on one circle at radius Ø/2 − cover − hoop Ø − bar Ø/2 (347.5 for Ø800 / cover 30 / Ø10 / Ø25) |
| r01–r04 | — | no inner ties drawn (H5 / M–V ignored for round); hoops from H4 |

## Last row = stub (verified 2026-11-03)
Kata draws every row but the last as a storey; the last row is only the stub the bars lap into (v00: 6 rows → 5 storey
outlines; v15: 7 rows → 6; live ES-0001: 6 rows → 5, TẦNG KỸ THUẬT missing). User decision: the export adds one row
above the top storey, named after the level at the column head (FAMILY_TOP_LEVEL_PARAM), repeating its height, b×h
(centred) and K. Its bars are user input like every row (the page lists it under "rows without bars").
C3 = STR_ElementName (else Mark, else type name), C4 = STR_ElementCount (else 1) — user 2026-11-03.

## Still open (need VC variants or user)

- Negative D: which face (min Y or max Y) Kata keeps — v04 drew the section downward from the base edge; tool assumes min Y.
- B9 = 0 (no pile cap): never drawn by VC.
- A wider storey with negative size (kept on its left face): never drawn by VC.

Resolved by draft 2: b/h sign, J, tie source, G3/K3/J3, F4/F5, C1/C2. Still open:
1. Kata default lap start when J is empty (per-storey numbers known, formula not).
2. Step rule for Revit (Kata draws none) and top anchorage (Kata never draws one) — **user decision**.
3. Ratio columns M/P (0.5) and H3/I3 meaning (no visible effect).
