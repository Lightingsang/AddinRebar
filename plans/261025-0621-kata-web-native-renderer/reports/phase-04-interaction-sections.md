# Phase 4 — picking, bar removal, sections (2026-10-25)

## Done
| Area | Files (`HPBimToolkit.Frontend/src/kata/elevation/`) |
|---|---|
| Hit test (port of KataElevationCanvas.Edit / FlagHit): bar lines within 6 px nearest first, second click on the same spot (4 px) cycles to the next line under it; flag stem + flag + number with 4 px spare | `kataHit.ts` |
| Hover (new, web only): group under the cursor in light accent + tooltip reading the keys ("Thép chủ trên Ø20 · L 12.340 mm · lớp 1 · 2 thanh", "Vùng đai nhịp 2 · 18 đai") | `kataHit.ts` (`describeKeys`), `selectionPainter.ts`, `NativeElevation.tsx` |
| Selection (per plan: a new plan drops it, as WPF), accent highlight + hint "Delete: xóa · Ctrl+Z: hoàn tác · Esc: bỏ chọn"; Delete → `kata.removeBars`, Ctrl+Z → `undoRemoveBars`, Esc: section → selection → close window | `useKataPicking.ts`, `selectionPainter.ts`, `NativeElevation.tsx` |
| Kata section panel (port of KataElevationCanvas.Section + KataSectionCadPainter + KataSectionView): flag click opens it at the right (350 px, full height), found again by station after removals, own wheel zoom / pan / middle double-click fit, ✕ and Esc close; section fetched once per plan + number (`kata.section`) | `SectionPanel.tsx`, `sectionPainter.ts`, `useKataPicking.ts` (`useSection`) |
| Section drawing: bulges as **exact SVG arcs** (WPF flattened them), THEP bars, leaders with DotBlank / DotSmall / Closed, marks, dims, tags (shared `tagOnLeader`), title | `sectionPainter.ts`, `kataTagPainter.ts` |
| Plain elevation with sections: cut markers + card 220×270 top right for the picked column's cut (span middle / support nearest / first span), run framed left of it (`reserve`), card only when the area is ≥ 420×290 (WPF rule) | `plainSections.ts`, `framing.ts` |
| Page now draws **every mode** natively (`?renderer=svg` still forces the WPF picture) | `KataApp.tsx`, `useKata.ts` (`loadSection`, `removeBars`) |
| Harness: real mouse/keyboard (CDP `Input.*`), flag → panel → Esc, bar pick → Delete → Ctrl+Z (skips without bars), card or its absence by size; `data-id` on bar lines and flags | `live-verify-kata-native.mjs`, `cdp-session.mjs` (`keyboard.press`), `shapes.ts` |
| Dev mock: sections by number, bar removal + undo re-emitting the scene | `kataMockBridge.ts`, `main.tsx` |

## Deviation (recorded)
The card beside the plain elevation now shows **Kata's own section drawing** of the cut (same as the Kata panel) instead of the WPF schematic card (concrete box, coloured dots, circled numbers, "Trên/Dưới/Giá" counts). Reason: one section drawing for both places, no new DTO for the schematic; the Kata drawing is the one the office reads. Revisit if the schematic counts are missed.

## Checks
| Check | Result |
|---|---|
| vitest / typecheck / lint | ✅ 130/130 (picking + sections 8, app Kata-mode 2 new) / ✅ / ✅ |
| `dotnet build HPRebar.slnx -c Debug.R26` | ✅ |
| Live, harness Revit on a Dam kata copy (`live-verify-kata-native.mjs 9334`) | ✅ 11/11: plain 7 + Kata mode drawn + flag opens section 3 (page-drawn) + Esc closes + no card in an 877×267 area |
| Live side by side (DOM checked clean of overlays) | ✅ section panel 3-3 identical to WPF (only the ✕ glyph differs); Kata elevation at span zoom identical |
| Bar pick / Delete / Ctrl+Z live | CHƯA TEST live — the open workbook (KataB03.xlsm) blocks this run's plan, no bar lines; covered by unit + app tests (mock removal/undo) |
| Plain card live | ✅ (re-run 2026-10-25 08:15, Kata window maximized → area 1548×318): F picked → card "MẶT CẮT 2-2" top right, page-drawn ([screenshot](phase-04-plain-section-card.png)); harness 11/11 |

## Notes
- Harness gotcha fixed: Esc with nothing open closes the Kata window (as WPF) — the harness presses it only over an open panel.
- In a shallow area (318 px, label rows 142 + 150) Tiếp frames the span at the whole run's scale: ShrinkToHeight, the WPF rule — the harness now checks the selection moves to the next span instead of a zoom-in.
- Comparison scripts must remove their WPF overlay with `querySelectorAll('#wpf-overlay')`: one left behind made a "native" screenshot show the WPF picture; re-shot clean.
