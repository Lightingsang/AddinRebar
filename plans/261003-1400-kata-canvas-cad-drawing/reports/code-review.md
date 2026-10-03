# Code review — Kata canvas = Kata elevation drawing (ShowRebar on)

Date 2026-10-03. Scope: uncommitted Core `KataElevationDrawing*` + `KataDrawing*` + `KataElevationOutline/Dims` + `KataBarTagBuilder` foot change, WPF `KataElevationCadPainter`, `KataCadDimPainter`, `KataCadText`, `KataRebarDrawing`, canvas Kata mode. Findings only, no edits.

Method: read every file; rules table of plan.md checked line by line; edge cases probed with a scratch console over the built `HPRebar.Core.dll` + `HPRebar.Core.Tests.dll` (DY7, DY14 E350/E500, empty spec, cantilever both ends, interior crossing beam, B7 = 0, shallow end beam, asymmetric row 13/14 cells). No exception in any probe.

## Verdict
Plan rules table implemented faithfully (lineweights, colours, outline, hidden, bars, stirrups, all chains, flags, level, title, dim style). No Critical/High. Three Medium geometry/rule gaps (one hidden by a self-referential test), one Medium perf trap in the failure path, Lows below.

## Medium

### M1 — Outline open where a crossing-beam support is shallower than its span
- [KataElevationOutline.cs:52-57](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataElevationOutline.cs#L52-L57) interior beam: top + soffit across s..e, no vertical at either face. [KataElevationOutline.cs:76-81](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataElevationOutline.cs#L76-L81) end beam: soffit at `-SupportDepth(k)`, span soffit at `-DepthOf(span)`, nothing joins them.
- `SupportDepth = min(span depths, BeamDepth)` ([KataBeamRebarSpec.cs:116-123](../../../HPRebar/HPRebar.Core/KataRebar/Models/KataBeamRebarSpec.cs#L116-L123)) so any depth difference leaves a gap. Probed: DY7 with support 1 = "200x400" → span-0 soffit ends (5950,-500), support soffit (5950..6450,-400), span-1 soffit starts (6450,-600): two open steps. DY7 with support 0 = "200x300" → span soffit -500 from x=450, beam soffit -300 to 450: open step at 450.
- Fix: in `Lines`, for a beam support k emit a face segment at SpanEnd[k-1] / SpanStart[k] from `-SupportDepth(k)` to the adjacent span soffit when they differ by > 1 mm (End(): from `depth` to `soffit` at `inner`). Plan already lists interior beam as GIẢ ĐỊNH; at least close the outline.

### M2 — Stagger dims pick the first layer-1/layer-2 curve only: wrong or missing with asymmetric "left;right" cells
- [KataElevationDims.cs:61-64](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataElevationDims.cs#L61-L64) groups by support, [KataElevationDims.cs:111-112](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataElevationDims.cs#L111-L112) `FirstOrDefault(Layer == 1/2)`.
- An asymmetric interior cell produces several curves per layer ([KataSupportTopBarLayout.cs](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs) strong side anchored, weak side runs through). Probed DY7 with E13 = "2f18;1f18", E14 = "3f18;2f18": layer 1 = #6 (4050→6408 hooked) + #7 (5230→8700 free), layer 2 = #8 (4550→6360 hooked) + #9 (5230→8200 free). Only #6/#8 compared → right-side dim 8200-8700 is lost (symmetric case emits it). If curve order flips (weak side first) the left dim compares a run-through end with a cut → wrong value.
- Fix: per side, take among the layer's bars the one whose end on that side is outermost (min X for left, max X for right) and test that end's hook.

### M3 — Top chain splits a span Kata leaves whole; the golden test mirrors the code
- [KataElevationDims.cs:85-90](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataElevationDims.cs#L85-L90) splits every span with ≥ 2 zones. DY14's 250 mm span has two one-stirrup zones → extra station 16350 (probed: top chain `... 16300 16350 16550 16650`).
- [KataElevationDrawingTests.cs:60-71](../../../HPRebar/HPRebar.Core.Tests/KataRebar/KataElevationDrawingTests.cs#L60-L71) `TopChain` re-implements `ZoneSplits`, so the assertion at [:242-243](../../../HPRebar/HPRebar.Core.Tests/KataRebar/KataElevationDrawingTests.cs#L242-L243) passes while its own comment says "Kata leaves the 250 span whole" — tautological for splits.
- Fix: merge neighbouring zones with same bar number + spacing before splitting (same rule `KataBarTagBuilder.StirrupRuns` already uses), and let the test state Kata's split stations (or assert "no split inside 16300..16550") instead of recomputing them.

### M4 — A throwing layout is rebuilt several times per render
- [KataElevationCanvas.cs:199-217](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationCanvas.cs#L199-L217): on exception `_drawing` stays stale/null and nothing remembers the failure; `KataMode` ([:220](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationCanvas.cs#L220)) is evaluated from `Band()`, `MarginRows()`, `FrameRange()` and `OnRender` → 3–6 full `KataBarTagBuilder + KataSectionCuts + KataElevationDrawingBuilder` runs, each throwing, per render (and per wheel via `MinScale`). Logged once, but the cost repeats on every mouse-move repaint.
- Fix: cache the failure with the plan reference (`_failedPlan = plan` → return null without rebuilding until `RebarPlan` changes).

## Low
- L1 Kata framing ignores tag rows: `Band()` ([KataElevationCanvas.cs:226-229](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationCanvas.cs#L226-L229)) uses `Elevation.Top` = 696.4 ([KataElevationDrawingBuilder.cs:48](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataElevationDrawingBuilder.cs#L48)); with 4 top levels at one cut the stirrup row + circle reach 725 → clipped under a 12 px margin. Take `max(Top, KataBarTagBuilder.Band(...).Above)`. Also `4.357 * 25.0` is `FlagTop` of the WPF painter — name it in `KataDrawingStyle`.
- L2 Fallback framing: when the CAD painter throws, the old painters run inside a viewport framed with Kata bands + 12 px margins ([:163-173](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationCanvas.cs#L163-L173)); their fixed-px label rows (`AbovePx` 142) are clipped. Acceptable degrade; could reset `_viewport` once on first failure.
- L3 Vertical dim ticks drawn "/" like horizontal ones ([KataCadDimPainter.cs:78-79,100-104](../../../HPRebar/HPRebar/KataExport/View/Controls/KataCadDimPainter.cs#L100-L104)); ArchTick rotates with the dim line → "\" on the depth dims.
- L4 Reversed `StationMap`: level mark lands at Length+475 and its block runs +289 further right (to ~Length+765), past `FrameRange`'s mapped MinX (-650 → Length+650) ([KataElevationCadPainter.cs:141-155](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationCadPainter.cs#L141-L155)). Flags, bubbles, run arrows, texts are screen-space and read correctly either way.
- L5 Per paint: 6 pens + DashStyles, 3 dim pens, one pen per flag ([:111](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationCadPainter.cs#L111)) and title, LINQ `ToList` per line, one `FormattedText` per text, no culling of lines/dims. Fine at DY7 size (~60 lines, ~50 dims); cache pens keyed on (Scale, PixelsPerDip, palette) if a 30-span run feels slow.
- L6 `KataLeader` / `KataCircle` palette pens are 1.0 DIP, not `1/PixelsPerDip` — at 150 % DPI they are 1.5 px while the CAD painter's thin lines are 1 px.
- L7 `KataElevationCanvas.cs` 330 lines > 300 rule; `Band/MarginRows/FrameRange/Drawing` would move cleanly to a partial `KataElevationCanvas.Kata.cs`.
- L8 `KataRebarDrawing` builds tags, cuts and elevation in one constructor: an elevation-builder bug also kills tags and the section card (sections need only Cuts).

## Edge cases verified OK (probes)
Empty spec (0 spans): no throw, no lines/dims. Cantilever both ends (width 0): free-end faces only, no stubs/grids, chains merge 0-width supports. B7 = 0: no slab line, run dims at -120 (DefaultSlab). One-stirrup zone: single stroke, no run dim. DY14 E500 and E350 outlines match the golden lines. `KataBarTagBuilder` foot now `KataDrawingLevels.Drawn` — leader arrows land on drawn bars. Overlay/Push-Pop are try/finally, nothing escapes `OnRender`. No plan-artefact references in code comments.

## Plan follow-ups
Phase 1 + 2 appear implemented; phase 3 live screenshot vs CAD still open. Recommend M1–M3 before live compare (M3 visibly differs from DY14 DWG).

**Status:** DONE_WITH_CONCERNS
