# Kata B01: top drop (row 19) and span width (row 20) — so B01 can be generated

Status: IMPLEMENTED + live-generated 2026-10-05 (phases 1–5; Core 1312/1312, R26 build; B01 generated on a model copy, review 5/10 → fixed [reports/code-review.md](reports/code-review.md), re-generated; Core 1319/1319; not committed). Report: [reports/live-b01-generate.md](reports/live-b01-generate.md). User "có" on "plan N4 + width per span, then generate B01 and measure".
Evidence: B01 DWG (`scratchpad/live/b01-dwg-bars.txt`), Revit model `Dam kata test.rvt` (read-only MCP), KataB1 sheet.

## Facts (verified)
| Fact | Source |
|---|---|
| Revit H: 17850–20550, h 650, top −50; J: 20550–24750, h 700, top 0; break at I, no column | Revit beams 9790835/37 |
| L 24750–31350 b 300 h 700; console 31350–33550 b 300 h 900, top −200 | Revit 9790839/41 |
| Depth = B5 − row 21 + row 19 (H 650, console 900) | Revit vs sheet |
| Top main bar: cranked 1:6 down 50 inside G (x 17750→18050 = centred in G), back up at I (20600→20900, from I into J) | DWG |
| Top bar split at M before the console drop −200: K→M bar ends with leg down at 31550; console bar 30800→33550 at −230, leg at tip | DWG |
| Top extra G14 cranked with the main bars (16050…19750 at layer-2 level, −50 in H) | DWG |
| Main bars (top + bottom) split at K where width 500 → 300 | DWG |
| Kata row 19 carries over (J19 = 0 written to reset H's −50) | sheet |
| Row 20 number = width, `b;2f12` allowed; doc says empty = B6 but Revit console is 300 with N20 `1f12` | doc l.1181 vs model |

## Phases
1. **Model** — span `TopDrop` from row 19 number (carry-over like 20/21), span `Width` from row 20 number (carry-over); a merged span keeps its internal top steps (`TopSteps`); depth/top/width accessors on the spec; Revit pieces give per-segment width + top offset (`KataMeasuredSegment`), geometry check compares them.
2. **Top bars** — main + extras follow the top level: crank 1:6 when `Cranks(step, support, Ø)` (centred in the support; at a zero-width station from it into the next span), else split: higher bar anchored down in the support, lower bar straight G3·d back past the far face.
3. **Width** — main/extra/side bars, hoops, ties, inner stirrups at each span's width; main bars split at a width change (same anchorage scheme as a cut step).
4. **Stirrups & sections** — hoop box top/width per zone; zone split at an internal top step; section/elevation canvas.
5. **Revit + live** — matcher passes width/top per segment; generate B01 on a model copy; measure bars vs DWG.

## Also found
- Kata Export writes H+J as one span (Revit has no column at I) and loses H's row 19 −50; Kata itself writes I = 0. Writing a zero-width joint at a framing break with a top/depth change would round-trip.

## Risks
- Guessed: crank position at a zero-width station, lap of the narrower bar at a width change (DWG 24200 / 24400 not explained by G3·d alone).
- Many calculators assume top = 0 and one width (≈ 30 call sites) — DY7/DY14 goldens guard regressions.
