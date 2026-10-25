# Phase 3 — Kata rebar elevation drawn by the page (2026-10-25)

## Done
| Area | Files (`HPBimToolkit.Frontend/src/kata/elevation/`) |
|---|---|
| CAD primitives (ports of KataCadPens / KataCadText + KataDrawingStyle / KataTagStyle constants): lineweights in device px (3/2/1), HIDDEN/CENTER dashes scaled, solid under 2 px; kata_text Arial, caps-height sizing, width factor 0.8, nothing under 1.5 px; AutoCAD closed arrow | `kataCad.ts` |
| Kata elevation (KataElevationCadPainter): selection band over the drawing height, lines by pen with bars last, flags (accent when shown), grid bubbles, level mark with hatch, title underlined | `kataElevationPainter.ts` |
| Dimensions (KataCadDimPainter): kata_dim_25 extension lines, heavy arch ticks, value above / reading upwards; kata_rai_thep runs with arrows and cross strokes | `kataDimPainter.ts` |
| Tags (KataElevationBarTagPainter + KataCadTag): arrows on each bar, leaders, text on the leader, touching number circles, stirrup tag T13; `tagBand` = KataBarTagBuilder.Band | `kataTagPainter.ts` |
| Framing in Kata mode (Band / MarginRows / FrameRange): band from drawing top/bottom and tag rows, 12 px rows, range widened to the level mark | `framing.ts` (`kataFrameOptions`) |
| Shapes: polylines/polygons, CAD text (rotated, squeezed), flat caps + round joins for Kata pens | `shapes.ts`, `ShapesSvg.tsx`, `palette.ts` |
| Switch: native now also for "Hiện thép"; only the section card beside the plain elevation (bars off, section on) stays WPF SVG | `NativeElevation.tsx` (`data-mode`), `KataApp.tsx` |
| Harness: Kata mode drawn natively + section card still WPF | `live-verify-kata-native.mjs` (9 checks; frames the run first) |

## Checks
| Check | Result |
|---|---|
| vitest / typecheck / lint | ✅ 120/120 (Kata painters 9 new; app tests now wait for framing — timing fix) ×3 runs stable / ✅ / ✅ |
| `dotnet build HPRebar.slnx -c Debug.R26` | ✅ |
| Dev page, B03 fixture, Hiện thép | ✅ bars, stirrups, tags with circles, dims, flags, bubbles, title drawn (light theme) |
| Live, harness Revit on a Dam kata copy (`live-verify-kata-native.mjs 9334`) | ✅ 9/9 |
| Live side by side, same size (WPF drawing from its `kata.drawing` event laid over the page) | ✅ identical at whole run and at span D zoom (dark) |
| Bars/tags live | CHƯA TEST live — the open workbook (KataB03.xlsm) gives this run a blocked plan, so Revit drew outline/dims/flags only; bars and tags checked on the fixture (unit tests + dev page) |

## Interim gap (closed by phase 4)
In native Kata mode a flag click does not open the section panel, and a bar click / Delete / Ctrl+Z does not select or remove bar groups yet (the WPF canvas did). Phase 4 ports hit testing, selection and sections; phases 3–4 should be committed together.
