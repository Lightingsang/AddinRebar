# B01 — Kata drawing vs HPRebar, rule by rule (2026-10-04)

Coordinates: x from outer face of column C (DWG x − 6732), z from beam top (DWG y + 170). Kata draws bars at the face
∓ 30 (drawing convention, R-13) — z differences of 20–25 mm from that are not counted as deviations.
HPRebar numbers = `KataRebarCalculator.Calculate` on the sheet (the planner blocks B01, see §0). Probe:
scratchpad `b01-probe/` (reads `live/kataB1-dam.txt`).

## 0. What B01 is (sheet KataB1, row 11, 13 columns)

C 400 | D 10400 | E 400 | F 6500 | G 400 | H 2500 | **I 0** | J 4000 | K 400 | L 6200 | M 400 | N 2000 | **O 0** (console).
Row 19 top drops H −50, J 0, L `0;3f20`, N −200. Row 20: C `400x500`, K `400x800`, F `0f12`, N `1f12`, L 300.
Row 21: F 400, L `0;3f20`, N 0. Rows 25–27 inner stirrups; K24 `*`.

**Live (user's Revit, B01 selected):** Đọc thép → 2 × [Chặn]: "Hàng 11 có 13 cột … Revit đo được 10" (model has
spans 10400 · 6500 · **6500** · 6200 · 2000 — no column at I) and R-05 "bề rộng > 0". Tạo thép disabled; model
untouched. Screenshot `scratchpad/live/b01-read.png`.

## 1. Rule by rule

| Rule | B01 evidence (Kata) | HPRebar | Verdict |
|---|---|---|---|
| R-03 span depth = B5 − row 21 | spans 2, 3, 4, 5 all soffit −700 with only F21 = 400; N21 = 0 brings N back to −1100 | spans 3–5 h 1100 | ❌ **empty row-21 cell inherits the previous span's step**; 0 resets |
| R-04/R-05 geometry / blocking | Kata draws B01 whole | blocked (I = 0, O = 0; 13 vs 10 columns) | ❌ cases not supported |
| R-13 bar centre | top bars at −30, bottom −1070 (span 1) | −50 / −1050 (J9 50/25 → a 50) | 🟡 known drawing convention |
| R-21 top mains one bar level | 3 bars: C→K (x 50 … 24945, legs down to −680), K→console (24200 … 31550, leg at 31550 to −480), console (30800 … 33550 at −230, leg to −1030); span 3 cranked down 50 (17750→18050, 20600→20900, slope 1:6) | one bar 50 … 33575 at −50 | ❌ Kata **splits at a support with a crossing beam row 20 `bxh` (K 400x800)** and at a **top drop** (N −200); small top drop (H −50) cranked 1:6 |
| R-22/R-82 bottom mains at steps | span 1: 75 … 11150 legs up (−645 / −670); E→K: 10450 … 24925 (leg up at K to −245); K→M→console: 24400 … 31800 at −670; console 31250 … 33550 at −1070 leg up at 31250 | span 1 155 … 11150 ✓ x right end; 10450 … 18450; 17750 … 31600 at −1050 | ✅ span-1 cut (R-82: far face − a 11150, shallow bar G3·d → 10450 ✓); ❌ rest follows from R-03 inheritance + split at K |
| R-33/R-34 legs | span-1 bottom right leg to −670 | to −705 | 🟡 known (leg ≤ step − Ø − gap) |
| R-35 bottom-leg inset | left bottom leg x 75 | 155 | 🟡 known HPRebar convention |
| R-41 top extras H5·L | C14 → 3000 ✓; E14 7700 … 12850 ✓; G14 16050 … **19750**; K14 left **22950**; M14 29650 … 31490 | 3000 ✓; 7700 … 12850 ✓; 16050 … 18750; 23600; 29650 … 32100 | ✅ C, E. ❌ G/K: Kata uses L = 6500 = H + J (**support of no width is not a support**: 0.25 × 6500 = 1625 → 1650) |
| R-42 G1 stagger | E15 `6f20;0` layer 3: 8200 … 11600 (straight 400 past E) | 8200 … 11048 hooked down | 🟡 left end ✓ (G1 500 stagger), right end ❌ R-46 |
| R-46 left;right at interior support | E15 right side 0 → Kata runs 400 past the far face, straight | hooks at far face (leg −598) | ❌ |
| R-46 K14 `2f20;2f16` | left 2f20 hooks down at 24900 (far face − 100); right 2f16 from 24360 | left hook 24898 ✓; right from 23960 | 🟡 hook ✓, right start ❌ (Kata 240 inside K, HPRebar G2·d through) |
| M14 into console with top drop | split: 29650 … 31490 hooked down; 30960 … 33550 at −275 (dropped level) | one straight bar 29650 … 32100 | ❌ (top drop not supported) |
| R-51 bottom extras | D17 1900 … 9300 (1500 from faces); F17 12150 … 16750 (950); L17 25850 … 30350 (850) | 2150 … 9050 (1750); 12300 … 16600 (1100); 26050 … 30150 (1050) | ❌ **min(H3·L, L/6) does not fit B01** (DY7-only fit). Kata/face: 1500 / 950 / 850 for L 10400 / 6500 / 6200 |
| R-51 row 17 at a support | I17 `2f20`: 18950 … 23300 bar under the support of no width | not read (parser ignores row 17 at supports) | ❌ |
| R-62/R-63/R-64 stirrup zones | spans 1, 2, 5: exact (e.g. 450 … 3000 / 3200 … 8000 / 8200 … 10750) | identical | ✅ (golden test) |
| R-62 with a support of no width | H + J as one span 6500: 18150 … 19750, middle split at I (19950 … 20550, 20650 … 22750), 22950 … 24550 | spans 2500 and 4000 zoned separately | ❌ same cause as R-41 |
| R-66 hoop height | span 2 650 ✓; span 3 under top drop 600 (−245 … −845); console 850 (−395 … −1245) | 650 ✓; 1050; 1050 | ✅ span 2; ❌ R-03 + row 19 |
| Console (R-92) | G9 a150 31650 … 33500, top −200 | 31650 … 33450 (calculator only) | 🟡 last stirrup 50 short; blocked in planner |
| R-71 side bars row 20 | F20 `0f12` → no side bars in spans 2–5; N20 `1f12` → console one layer at −650 | spans 3–5 two layers (G5) at −383/−717; console −550 | ❌ **empty row-20 cell inherits the previous span** (0 carries on); console z from wrong depth |
| R-72/R-73 side bars span 1 | 280 … 10920 at −387 / −713 | 280 … 10920 at −383 / −717 | ✅ (golden test) |
| R-01 row 19 top drop | H −50 cranked 1:6 over 300; N −200 → bars split, lap | skipped (R-06) | ❌ |
| R-68 inner stirrups rows 25–27 | not checked in this pass | drawn in span 1 | — |

## 2. New rules B01 suggests (need the user's decision before coding)

| # | Proposed rule | Evidence |
|---|---|---|
| N1 | Empty row 21 / row 20 cell of a span = same as the previous span; `0` resets | spans 3–5 soffit, side bars spans 3–5 |
| N2 | A support of width 0 (row 11 = 0) is not a support: the spans on both sides are one span for cut lengths and stirrup zones; stirrups split there only when the hoop changes (top drop) | G14/K14 reaches, stirrup runs H+J, Revit model has no column at I |
| N3 | Row 11 `0` at the last column = console end (O) | console drawn 2000 |
| N4 | Top drop (row 19): ≤ 50 → crank 1:6; larger → split bars with lap | H −50 cranked, N −200 split |
| N5 | Bars split (lapped) at a support with a crossing beam in row 20 (`400x800`) | top and bottom mains split at K |
| N6 | Bottom extra cut ≠ min(H3·L, L/6): B01 1500 / 950 / 850 from the faces for L 10400 / 6500 / 6200 — rule unknown (not L/6, not L/7, not 0.15 L) | D17, F17, L17 |

## 3. Locked by test

`KataB01DrawingTests` (6): planner blocks for supports of no width; stirrup zones of spans 1, 2, 5; side bars of
span 1; row-14 bars over C and E. Everything marked ❌ above stays out of the tests until the rule is decided.

## 4. Follow-up 2026-10-04: N1–N3 coded
N1 (row 20/21 carry-over), N2 (support of no width merges spans), N3 (console end) done — plan
`plans/261004-2330-kata-b01-zero-width-supports/`. Live: row 11 now matches Revit; still blocked by console depth
(Revit 900 = B5 − row 21 + row 19). Next: N4 top drop + row 20 width change.
