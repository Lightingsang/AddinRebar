# Code review — Kata bar numbers + elevation tags (Đợt 1, uncommitted)

Date 2026-10-02. Scope: KataBarNumbering.cs, KataBarTagBuilder.cs (new), KataRebarCalculator/KataSideBarLayout/models (BarNumber), KataRebarStamp.Mark + 4 callers, KataElevationBarTagPainter.cs (new), KataLabelLane.PlaceNear, KataElevationRebarPainter/Canvas/Scene, KataExportView.xaml + VM, KataBarNumberingTests.cs. Source not edited.

Checks run: `dotnet test HPRebar/HPRebar.Core.Tests` → 867/867 pass. Scratch probe (scratchpad console app over HPRebar.Core, `--artifacts-path`) for signature behaviour — results quoted below. Add-in (R26) not built, Revit not run.

Verdict: no High. Numbering logic matches the golden DY7/DY14 contract; three Medium issues (two in the signature, one visual) worth fixing before more beams are compared with Kata DWGs.

## Medium

### M1 — Mirrored bent bars never share a number: hook suffix not canonicalised with the chosen form
[KataBarNumbering.cs:95-101](HPRebar/HPRebar.Core/KataRebar/Calculators/KataBarNumbering.cs#L95-L101)
The polyline part takes the min of 4 forms (incl. reversed + X-mirrored), but the hook suffix is always `Start|End` in the original order. For a bent bar the X-mirror swaps start/end hooks, so the min polyline form matches while the suffix differs.
Probe — symmetric 2-span beam (450/5500/500/5500/450, C13=E13=G13=1f18): row-13 bar at support 0 `(42,-367)(42,-42)(1850,-42)` hooks 90/None → **#3**; at support 2 `(10550,-42)(12358,-42)(12358,-367)` hooks None/90 → **#5**. Same Ø18, same 1808 + 325 leg — exact mirror images. Same for left/right bent bottom runs.
Inconsistency: the z-flip form keeps hooks in place, so top-L vs bottom-L share; X-mirror and 180° rotation don't. Either drop the mirror forms (if Kata numbers mirrored bars apart — golden DY7 cannot tell, its end supports differ in length) or emit the hook pair in the same orientation as the chosen form (swap Start/End for the reversed forms). Needs one golden from a symmetric Kata beam to decide; until then the docstring ("a bar and its mirror image match") is false for every bent bar.

### M2 — Signature quantisation is noise-sensitive (0.5 mm bucket edge + negative zero)
[KataBarNumbering.cs:92, 115](HPRebar/HPRebar.Core/KataRebar/Calculators/KataBarNumbering.cs#L115)
`Math.Round(v).ToString("0")` buckets, it does not compare within ±1 mm (plan contract "kích thước ±1 mm"):
- Probe: lengths 4650.4999 and 4650.5001 → numbers 1, 2; 4649.51 and 4650.4999 → share. With Revit-measured geometry (`KataRebarPlanner.WithMeasuredGeometry` feeds non-integer `LengthMm`) span 2 = 5500.4 keeps row-18 bars shared (#6/#6), 5500.6 splits them (#6/#7) and **shifts every later number** (hoops 9 → 10). Schedule Marks in Revit then stop matching the Kata DWG for a 0.2 mm modelling difference.
- On .NET Core (R25+, tests) `Math.Round(-1e-12)` and `-1 * 0.0` format as `"-0"` (verified: `'-0'`). Probe: same 5-point bar, one copy with ±1e-9 noise on its coordinates → split in 1101/2000 trials. Current generators build legs from the same variable so zeros are exact today, but any independently computed coincident coordinate (crank path, measured stations) splits identical bars — and net48 (R23/R24) formats `"0"`, so numbering can differ by Revit version.
Fix: quantise to `long` (`(long)Math.Round(v)` — no negative zero) and, if the ±1 mm contract matters, cluster lengths/legs with a tolerance instead of string buckets (or quantise the *sheet* values the bar derives from rather than final coordinates). Add a test with a 0.6 mm measured offset.

### M3 — A slid tag's leader is drawn through the tag that displaced it (common case)
[KataElevationBarTagPainter.cs:69-83](HPRebar/HPRebar/KataExport/View/Controls/KataElevationBarTagPainter.cs#L69-L83)
A tag slides only because another tag already sits at its foot (left = foot − Radius, circle centred on foot). The slid tag's leader then runs vertically to `rowY` exactly through that circle and horizontally along `rowY` through its circles and text; it is drawn afterwards, so it strikes through them.
Happens on the golden beam: DY7 span 0 has row 18 (`2+10 2Ø18+1Ø18`) and row 17 (`11 3Ø18`) at the same span middle, both in the single below lane → `11` slides right and its dashed leader crosses `2+10 …`. Above the beam, the inner row (14-16) is *above* the outer row, so at an interior support (E13/E14) the inner leader passes through the outer tag's first circle.
Fix options: route the horizontal jog at an offset (e.g. `rowY ± (Radius + 3)` on the side away from the beam) and stop the vertical there; or draw all leaders first and tags (filled circles + an opaque text backing) last; for the above case put the inner row below the outer one or offset the inner foot.

## Low

| # | Where | Issue / failure scenario |
|---|---|---|
| L1 | [KataBarTagBuilder.cs:65-72](HPRebar/HPRebar.Core/KataRebar/Calculators/KataBarTagBuilder.cs#L65-L72) | Side-bar tags group by `BarNumber` across the whole beam. Two separate side-bar runs of equal length (span 0 and span 2, span 1 without side bars) share a number → one tag at the first run only; if the runs have 2 and 1 layers, the single tag says `2x2Ø12` and the 1-layer run is untagged. Group by (number, first span) instead. |
| L2 | [KataBarTagBuilder.cs:35-41](HPRebar/HPRebar.Core/KataRebar/Calculators/KataBarTagBuilder.cs#L35-L41), `Level` L95-97 | Asymmetric support (`…T` / `…P` bars, same support + layer) → one tag, x taken from the first (T) bar's level run only, so the leader lands off the support; text `1+4+5 …` unverified against Kata. |
| L3 | [KataBarNumbering.cs:49-51, 78-79](HPRebar/HPRebar.Core/KataRebar/Calculators/KataBarNumbering.cs#L49-L51) | All ties of one Ø share one number whatever their width / wrapped-bar Ø (Kata does this — verified), but in Revit one Schedule Mark then covers ties of different cut lengths; a schedule grouped by Schedule Mark shows one length. Tie/inner split rests on display strings (`"Cốt giá"`, `"Thanh C kê"`) — a rename silently moves ties into the inner-stirrup numbering; prefer a flag/role on `KataBarSet`. |
| L4 | [KataBarNumbering.cs:104-110](HPRebar/HPRebar.Core/KataRebar/Calculators/KataBarNumbering.cs#L104-L110) | Set signature ignores `WrappedBarDiameter` / `WrapOffset`: two inner sets with the same centreline but wrapping different bars get one number though Revit lays them out with different bends. Also not reversal-invariant (unlike bars) — fine while generators are consistent. |
| L5 | [KataBarNumbering.cs:81-83](HPRebar/HPRebar.Core/KataRebar/Calculators/KataBarNumbering.cs#L81-L83) | Docstring "read either way round": the form set is not invariant to point order (a bar listed right-to-left gets a different key); it works only because every generator lists points X-ascending. Worth a comment / test pinning that invariant. |
| L6 | [KataBarNumbering.cs:42](HPRebar/HPRebar.Core/KataRebar/Calculators/KataBarNumbering.cs#L42) | Main bottom ordering ignores `Layer` (`_ => 0`): a layer-2 main bottom run with a smaller min X is numbered before layer-1 runs. Kata order for 2-layer B12 unverified. |
| L7 | [KataElevationBarTagPainter.cs:53-68](HPRebar/HPRebar/KataExport/View/Controls/KataElevationBarTagPainter.cs#L53-L68) | Per `OnRender`: `KataBarTagBuilder.Build` + `KataBeamStations.From` + 2 frozen pens + ~3 `FormattedText` per tag (`plus` re-created per tag). Cheap at DY7 size; on long runs during wheel-zoom it is avoidable work — cache tags per `RebarPlan`. Off-screen tags are culled before placement, so lane occupancy changes while panning and visible tags jump between positions. |
| L8 | [KataElevationScene.cs:19-22](HPRebar/HPRebar/KataExport/View/Controls/KataElevationScene.cs#L19-L22) | `AbovePx` 96→142 / `BelowPx` 145→150 apply with tags off too: `Frame` → `ShrinkToHeight` zooms out earlier on short canvases, 46 px blank above the beam. Geometry checked: outer row top ≈ BandTop−110, inner ≈ −128 (+ text) — fits 142; below tags end ≈ BandBottom+140 — fits 150. |
| L9 | [KataLabelLane.cs:27-36](HPRebar/HPRebar/KataExport/View/Controls/KataLabelLane.cs#L27-L36) | `PlaceNear` with `step <= 0` loops forever (only caller passes default 6). Guard `step > 0`. |
| L10 | [KataRebarStamp.cs:15](HPRebar/HPRebar/KataRebar/Service/KataRebarStamp.cs#L15) | Fallback to internal `BarMark` (`3.1.1`, `d…`) when `BarNumber == 0` would mix mark schemes in one partition; unreachable today (every list numbered, stirrup `HostSpanIndex` is always the span), so informational. |

## Answers to the focus questions

- **Top bar leg-down vs bottom bar leg-up sharing:** geometrically correct — for a planar bar the Z-mirror equals a 180° rotation about the bar axis, so the bars are interchangeable on site and in a bending schedule; Kata's "identical bars share a number" rule implies sharing. Not risky for fabrication; only unverified against a Kata DWG (DY7 golden exercises straight bars only). The real inconsistency is M1 (X-mirror not shared for bent bars).
- **Order independence:** deterministic for a given layout (sorted by group/layer/minX, index as last tie-break); numbers depend on the bar *values*, so M2 is the stability risk, not list order. Inner sets are numbered in `BarSets` list order (span → entry → zone), which is stable.
- **Ties vs inner stirrups by ZoneName:** correct today (inner sets carry hoop zone names), fragile (L3).
- **Single stirrups with HostSpanIndex −1:** none are produced (`KataStirrupZoneLayout` passes the span index); a −1 would keep 0 and fall back (L10).
- **Tag builder edge cases:** no main bars → row tags alone, fine; one-point bars → `Level`/`LevelAt` return the point, fine; empty layout → no tags; polylines are never empty (`Simplify` keeps ≥ 1 point). A `-` chain is tagged once at its origin support and suppresses the main-bar mid-span tags it covers (matches DY7 golden).
- **Painter:** pens frozen; per-render cost acceptable (L7); leader geometry is M3.

## Positive

- Numbering isolated in pure Core, applied once in `Calculate` → preview and Revit share numbers; internal `BarMark` kept for set grouping (Revit sets group by a key that implies identical signature, so one set = one number).
- Golden tests from the Kata DWG for both beams, incl. cross-family sharing (E13 = F17) and hoop sharing across spans.
- `KataLabelLane` reuse keeps tags from overlapping each other.

**Status:** DONE_WITH_CONCERNS
**Summary:** 0 High, 3 Medium (mirror hooks, noise-sensitive signature, leader through neighbour tag), 10 Low; tests 867/867.
