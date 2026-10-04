# Code review — console (R-92) + span main bars rows 19/21 — 2026-10-05

Reviewer: code-reviewer agent, read-only + offline probe (scratchpad `consoleprobe/`, 20 sheets). Score **5/10** → all findings fixed same day (below).
Tests before: 1320/1320. After fixes: **1335/1335** (+14 in `KataConsoleAndSpanBarsTests`, 1 updated).

## Findings → fix

| Id | Finding (probe) | Fix | Test |
|---|---|---|---|
| H1 | Console cut + width/bar change at same support → 3 duplicate bottom stubs (N20 200; N21 `0;2f16`) | `KataWidthProfile.Apply`: a main bar not covering both spans was already cut there (console, soffit step) → not cut again; pieces only take span bars/width | `A_narrower_console_is_cut_once_at_its_support`, `A_console_of_its_own_bottom_bars_is_cut_once_and_gets_them` |
| H2 | Top step + row-19 bar change at one support → duplicate top stub (N19 `-200;3f16`) | `KataScopeFilter`: blocks "gối đổi cả cao độ đỉnh và thép chủ trên" (like width+top) | `A_support_changing_both_the_top_and_the_top_bars_is_refused` |
| H3 | Console bottom 10d leg unchecked → leg above beam top (h 280) | `ConsoleRun` clamps leg to `TopRoom(support) − z`, warns "chân neo chỉ còn" | `A_console_too_shallow_for_a_10d_leg_keeps_it_under_the_top_bars` |
| H4 | Laps/legs sized from B11/B12 before swap; B01 L bottom 24200→31950 vs DWG 24400→31800 | Bottom planner per-span diameter (`diameterOf`, `EndSolver.Step` takes d); width cut sizes anchored/lapped piece from its span's bars; bottom main lap = G3·d own past own support face (unrounded, as planner); top lap = G3·max(d lapped, d anchored); console leg 10·max(d, B12 d) | `B01s_narrow_bottom_bars_lap_30_d_of_their_own_past_their_faces_of_K_and_M` (24400/31800 ±1), `Span_bars_larger_than_B12_size_the_laps_and_the_console_leg` (3f28: 24150/32040, leg 280) |
| M1 | Tip leg foot on console bottom bar (5 mm) | Foot room − ((dT+dB)/2 + max(dT,dB)) corrected for swap shifts → 40 mm c/c (DWG −1030 over −1070) | `B01s_console_tip_leg_stops_one_bar_clear_above_the_console_bottom_bar` |
| M2 | Equal-depth console: lapped bars coincide, silent | Warning "nhịp và console gần cùng đáy" when \|step\| − d < LayerGap; cut kept (only B01 evidence) | `Calculate_BothSidesCantilever_HandlesDoubleOverhang` updated |
| M3 | Record equality on bars (RawNotation) → needless cut, refused join, crash in blocked sheet | `KataBarItem.SameBars` (count + Ø); `CanJoin` compares effective bars (B11/B12 fallback); planner skips `Calculate` when blocked | `Row_19_restating_B11_…`, `A_refused_sheet_is_not_laid_out` |
| M4 | `SpacingWarnings` used B11/B12 → 4 false B01 warnings, missed 8f20 | Per-span `TopMainOf/BottomMainOf`; cell row 20 (width) or 19/21 (bars) | `Span_bars_too_close_…_and_B01_has_none` |
| M5 | One-span console: no console bottom bar | `ConsoleRun` for n = 1; side from `SupportWidth[span+1]` | `A_one_span_console_gets_its_own_bottom_bars_…` (left + right) |
| M6 | B11/B12 empty under row 19/21 bars: silent | Scope reports the cell skipped | `Span_bars_with_B11_empty_are_reported` |
| M7 | Consoles both ends over columns blocked "không có gối" | Block only when 1 span | `Consoles_at_both_ends_over_two_columns_are_drawn` |
| L1 | M14 extras start 30800 vs DWG 30960 | Extras at a top cut lap G3·max(d, anchored span main d) → 30950 | `B01s_console_extras_lap_30_d_of_the_span_bars_into_L` |
| L2 | Hook data stale after swap | Moot: anchors sized with the piece's own d (H4) | — |
| L3 | `IsTie` doc comment misplaced | Moved | — |
| L4 | 3Ø20 set fallback (y 104.3 vs 105) | Not changed — live note, single bars correct | — |

Open Q1 (console bar Ø) settled by DWG section 14: 3Ø20 top/bottom → bars carry through a number-only row 19/21 cell (parser rule unchanged). Q2 (equal-depth console in Kata) still no drawing → warning only.

## Gates
- `dotnet test HPRebar.Core.Tests`: ✅ 1335/1335
- `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false`: ✅ 0 errors
- Live B01 on model copy (test Revit, user's Revit untouched, KataB1 read only): ✅ "beam B01 done — main bars 30, support top bars 28, span bottom bars 10, side bars 6, flat-bar sets 20, stirrup sets 14, Revit warnings 1"; same 3Ø20 set fallback (L4).
