# Phase 5 — web-only tools (2026-10-25)

## Done
| Feature | Files (`HPBimToolkit.Frontend/src/kata/elevation/` unless noted) |
|---|---|
| Tool strip over the drawing: Đo · Lớp · Tìm số hiệu · Xuất hình | `ElevationTools.tsx` |
| Layers (Kata mode): concrete, bars, stirrups, tags, dims, flags, grid, level/title — shapes carry their layer (`ShapeList.layer`), hidden ones filtered before render | `shapes.ts`, `kataElevationPainter.ts`, `kataTagPainter.ts`, `drawingTools.ts` |
| Measure: two clicks, snapped within 8 px to Kata's line vertices and dimension feet, second point follows the cursor, reading "Δx · Δz · L mm"; Esc drops the measure, then the tool | `drawingTools.ts`, `useDrawingTools.ts` |
| Find a bar number: tags carrying it, Enter cycles 1/N…, each framed with ±1.5 m and highlighted (ring + leader feet) | `drawingTools.ts` (`findBar`), `useDrawingTools.ts` |
| Smooth framing: focus requests (Trước/Tiếp/Toàn dải), middle double click and find glide 220 ms (geometric scale, ease-out); wheel/drag stay immediate; no glide under prefers-reduced-motion | `useElevationView.ts` (`glideTo`, `frameRange`, `centreOn`) |
| Minimap: whole run (beams + supports) with the view framed, click/drag moves there; hidden while a section panel is open or the area < 480 px | `Minimap.tsx` |
| Export: SVG (vector), PNG ×2 / ×4 (canvas from a data: image — CSP unchanged), print/PDF (only the drawing, landscape); blob downloads go through **a Save dialog shown by the host** (`WebViewHost.OnDownloadStarting`, owner = the window, cancel = no file) — nothing lands silently in Downloads, no 256 KB message cap | `exportDrawing.ts`, `HPWebHost/Hosting/WebViewHost.cs` |
| Taller drawing area (phase-4 note: 267 px left the span framing no room): card row ≥ 500 px, drawing ≥ 360 px | `KataApp.tsx` |
| Live harness for the tools + a Save-dialog check from PowerShell | `HPBimToolkit/tools/harness/live-verify-kata-tools.mjs [--export]` |

## Checks
| Check | Result |
|---|---|
| vitest / typecheck / lint | ✅ 138/138 (tools 6 + app tools 2 new) / ✅ / ✅ |
| HPWebHost.Tests | ✅ 68/68 |
| `dotnet build HPRebar.slnx -c Debug.R26` | ✅ |
| Live, harness Revit on a Dam kata copy: `live-verify-kata-native.mjs` | ✅ 11/11 (area now 877×367) |
| Live: `live-verify-kata-tools.mjs --export` | ✅ 7/7 — minimap click moves the view, concrete layer off 31 → 4 polylines, measure "Δx 17.189 · Δz 17 · L 17.189 mm" (snapped), Esc keeps the window, find answered, SVG export starts |
| Host Save dialog | ✅ "Save As" appeared owned by the harness Revit, closed by WM_CLOSE (cancel → nothing written) |
| Screenshot | ✅ [phase-05-tools.png](phase-05-tools.png) — tool strip, Kata drawing, minimap |
| PNG export, print/PDF live | CHƯA TEST live (unit-free browser paths; SVG path + dialog verified) |
| Find on real tags live | CHƯA TEST — the open workbook (KataB03.xlsm) blocks this run's plan, no tags; covered by unit + app tests |

## Notes
- Harness gotcha: the comparison script's `#wpf-overlay` left in the page made a screenshot show the WPF picture again — always remove it (`querySelectorAll`) before screenshots; the tools harness's DOM checks are unaffected.
- The glide reads `performance.now()` itself: rAF's frame time can run on another clock (jsdom), which made t negative and the scale extrapolate.
