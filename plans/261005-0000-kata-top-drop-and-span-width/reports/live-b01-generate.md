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
