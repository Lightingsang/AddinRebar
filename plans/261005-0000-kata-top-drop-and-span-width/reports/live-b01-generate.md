# Live B01 generate (2026-10-05)

Setup: test Revit 2026.4 on a copy of `Dam kata test.rvt` (user's model with crossing beams at E and a stub column on F),
Debug.R26; user's Revit and KataB1.xlsm untouched (Kata Export ▸ Đọc thép Excel only reads the active KataB1).

| Step | Result |
|---|---|
| Đọc thép | ✅ no [Chặn]: 539 bars 2641.5 kg; warnings: stub column (not a support), console provisional, anchorage short 55 at support 2 |
| Tạo thép run 1 | ❌ "Revit không tạo được thanh thép '25' từ 4 đoạn cong" — cause: `KataScopeFilter` still zeroed `TopDrop`, leaving a zero step at I → two collinear segments. Fixed (scope keeps row 19; zero steps dropped); error now names the bar and its points |
| Tạo thép run 2, 3 | ✅ `beam B01 done — main 36, support top 28, span bottom 10, side 6, flat sets 20 (all Fixed Number, 0 single), stirrup sets 14`, 1 Revit warning (stale "outside host", cleared) |
| Row 20 at E (one-sided crossing beams 400x500) | ✅ after `OneSidedStation` in `KataSupportCollector`: `400x500`, row 21 = 0 |

Positions: each Fixed Number set passed the add-in's own check against the planned bar positions (throws above
tolerance). Not measured bar by bar in Revit: MCP bridge is attached to the user's Revit, not the test instance.

Core vs DWG (golden `KataB01DrawingTests`): top crank 17750→18050 / 20600→20900 ✓, end hooked in K 24945 ✓, narrow
top 24200→31550 ✓, console top 30800→33550 at −230 ✓, bottom hooked in K 24925 ✓, narrow bottom 24250 (Kata 24400),
hoop zones H 19950…20550 h600 / J 20650…22750 h650 ✓, console hoop 250×850 ✓.

## Console (R-92) + main bars of rows 19/21 (2026-10-05, later)
DWG sections 11-14 of B01 (MCP AutoCAD, read-only): L top 3Ø20 (+2Ø16 at K), bottom 3Ø20 (+2Ø20 L17), console top
3Ø20 + 2Ø16, side 2Ø12, bottom 3Ø20 → rows 19/21 `3f20` = span main bars, carried on.
Live (copy): `beam B01 done — main bars 30` (6 → 3 in L and console). One 3Ø20 set: z first off 2.5 mm (thinner bar
centre kept at the Ø25 level) → fixed (outer face against the stirrup); then y 104.3 vs 105 (Revit model bar diameter
against the stirrup) → that set falls back to single bars (positioned exactly). No error.

## Re-run after console review fixes (2026-10-05 06:02)
- Build Debug.R26 deployed to test Revit pid 40744 on `live\b01-test.rvt`; user's Revit 58764 + KataB1 untouched (active workbook checked = KataB1, only Đọc thép Excel pressed).
- Đọc thép: 542 bars 2387.6 kg, no [Chặn]; warnings stub column, console provisional, anchorage short 55 at support 2.
- Tạo thép Revit: ✅ "beam B01 done — main bars 30, support top bars 28, span bottom bars 10, side bars 6, flat-bar sets 20, stirrup sets 14, Revit warnings 1"; one 3Ø20 set → single bars (y 104.3 vs 105, unchanged).
- Test Revit stopped.

## Row 24 `*` + row 17 at the joint I (2026-10-05 06:17)
- K24 `*` → note "đúng như bản vẽ" (no joint stirrups; HPRebar draws none). I17 2f20 → bottom extras of joined span H+J, R-51 cut 19200…23500 (Kata DWG 18950…23300, rule at a joint unknown; read from T2-DY7.dwg read-only: polyline 25682…30032 at y −790, dims 2700 from I / 1300 from K face).
- Live test Revit pid 70208 on copy, KataB1 read only: ✅ "beam B01 done — main bars 30, support top bars 28, span bottom bars 12 (+2 I17), side bars 6, flat-bar sets 20, stirrup sets 14, Revit warnings 1". Stopped.
- Review 7/10 (code-review-joint-cells.md): M1 unreadable-width support rows 17/18 now noted; M2 joint bars only to a span with no own bottom extras; M3 joint outside L/6…5L/6 reported; M4 shared blank check (empty/0/-); L1 Skipped doc; L2 B01 numbers moved to comment. L5 braces/L4 literals left (file style). Tests 1343/1343, R26 build ✅. Fixes after the live run do not change B01 (H17/H18 empty, joint at 0.38 L).

## Console stirrups + crossing-beam top line (2026-10-05 06:28)
- DWG (read only): console stirrups drawn first/last only, 31650 and 33500, label Ø10a150 → rule: face + 50 … (tip − a − 50), evenly ≤ G9. HPRebar was 31650…33450 (13 @150). Now 14 @142.3.
- Live test Revit pid 74336 (copy, KataB1 read only): log "stirrups Console 14@142.3 (a150) from x=31650"; "beam B01 done — main bars 30, support top bars 28, span bottom bars 12, side bars 6, flat-bar sets 20, stirrup sets 14, Revit warnings 1". Stopped.
- Elevation outline: a crossing-beam support now draws its top at the spans' tops (step at its centre when they differ) instead of at 0; end crossing beam uses the span top. Test `A_crossing_beam_between_spans_of_different_tops_steps_its_top_at_its_centre`. Tests 1344/1344, R26 ✅.
