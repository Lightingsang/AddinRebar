# Code review — phase 03 Revit readers (KataExport)

Date 2026-09-27. Static review, no Revit run. Scope: `HPRebar/HPRebar/KataExport/Service/*.cs` (8 files) + `Model/*.cs` (2 files), 766 LOC. Contract: [KataRunModels.cs](../../../HPRebar/HPRebar.Core/KataExport/Models/KataRunModels.cs), [KataSegmenter.cs](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataSegmenter.cs). Spec: [phase-03](../phase-03-revit-data-extraction.md), [kata-cell-contract.md](kata-cell-contract.md).

**Score 6.5/10.** Clean, small, compiles on every net48/net8 config, conventions respected. But the probe-along-centre-line design has blind spots that silently change the sheet: joined (trimmed) crossing beams, foundations running along the run, the wrong exception caught, and heights read from joined solids.

## Verification run

| Check | Result |
|---|---|
| `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false --artifacts-path <scratch>` | ✅ pass, 0 CS warnings in KataExport |
| same, `-c Debug.R24` (net48) | ✅ pass |
| same, `-c Debug.R23` (net48, `Category.BuiltInCategory` since 2023) | ✅ pass |
| API contracts checked in `RevitAPI.xml` (2023.1.90 + 2026.4.10) | `Solid.IntersectWithCurve`, `Solid.Volume`, `Parameter.GetUnitTypeId`, `UnitUtils.ConvertFromInternalUnits`, `GeometryInstance.GetInstanceGeometry`, `FamilyInstance.GetOriginalGeometry`, `Level.ProjectElevation/Elevation` |
| Cross-feature refs / plan refs in code | none (grep) |
| Grid station formula `s = (p×d)/(a×d)` | ✅ solves `O + s·a = G0 + t·d` (algebra checked) |
| Transverse `Z × axis` | ✅ = left of run direction; B9 = grid − beam centre, positive left ✅ |

## High

### H1 — The wrong exception is caught around `IntersectWithCurve`, so one open solid aborts the whole export
[KataSolidReader.cs:51-58](../../../HPRebar/HPRebar/KataExport/Service/KataSolidReader.cs#L51-L58), [KataSolidReader.cs:28-32](../../../HPRebar/HPRebar/KataExport/Service/KataSolidReader.cs#L28-L32)
- `RevitAPI.xml` (R23 and R26) documents `Autodesk.Revit.Exceptions.ArgumentException` for "the input solid is not a closed volume". It does not document `InvalidOperationException`. The code catches only `InvalidOperationException`. Both derive from `Autodesk.Revit.Exceptions.ApplicationException` and are not related to each other.
- `Solid.Volume` documents `InvalidOperationException` ("volume calculation failed"). It is called at lines 28 and 32 with no guard.
- Failure: a column, footing or beam anywhere in the ±5 ft box has an open shell (a family with imported SAT/DWG geometry, or an IFC or Tekla import where DirectShape is set to a structural category). An unhandled exception is thrown, and the user gets no export for a beam that has nothing to do with that element.
- Fix: catch `Autodesk.Revit.Exceptions.ApplicationException` per solid, both in `ProbeIntervals` and in the Volume filter. Count the skips and return a warning ("N element(s) with invalid geometry skipped").

### H2 — Crossing beams trimmed by joins cannot be seen by probes on the centre line: row 21 at supports is almost never measured, and equal-depth crossings depend on join order
[KataSupportCollector.cs:64-79](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L64-L79), [KataSupportCollector.cs:128-137](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L128-L137)
- The probes run at the run's centre line. `get_Geometry` returns post-join solids.
- **At a column:** a concrete beam framing into a column is cut back to the column face. The probe is inside the column footprint, so it never enters the crossing beam. `CrossingBeamStationMm` stays null and row 21 falls back to the grid offset. The user's decision ("row 21 at a support = the real crossing beam's offset from the column centre") is effectively not implemented.
- **Crossing beam wider along the run than the column** (b_x 400 > c_x 300): only the two slivers beside the column are hit. That gives two Beam intervals, and `Combine` takes the sliver mid nearest the centre, so row 21 = ±175 instead of 0. That also breaks Kata's own limit "Ko nhập > cột/2".
- **Equal-depth crossing away from a column:** if the run wins the join, the crossing solid has a hole where the run passes. Three probes are inside that hole and `soffit−20` is below both beams, so no support is found. If the crossing beam wins, it becomes a support (`carries`). Same model, different sheet, depending on join order.
- Deeper girders are still found through `soffit−20` (OK).
- Fix: for framing, use analytic geometry instead of solids.
  - Station = 2D intersection of the crossing location line with the axis (same closed form as `KataGridReader.Station`).
  - Extent = station ± (b / 2) / sin θ.
  - Soffit/top from `FamilyInstance.GetOriginalGeometry(options)` transformed by `GetTransform()` (pre-join, pre-cutback per the API docs), or from location Z + justification + `h`.
  - Keep solids/probes for columns, walls and foundations.

### H3 — Foundations running along the run become one support covering the whole overlap
[KataSupportCollector.cs:52-54](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L52-L54)
- The parallel filter exists only for walls. The `soffit−20` probe runs inside anything the beam sits on along its length, for example:
  - a strip footing or `WallFoundation` under a grade beam;
  - a foundation slab or raft (`Floor` class, category `OST_StructuralFoundation`);
  - lean concrete ("bê tông lót") modelled as a foundation slab.
- Result: one Foundation support spans the run, which gives no spans or truncated spans, and no warning.
- Fix: skip foundations whose element is a `WallFoundation` or a `Floor`, or whose probe interval is longer than a limit (e.g. > 3 m or > 3·h). Warn the same way as for parallel walls. Keep `FamilyInstance` footings and pile caps.

### H4 — Beam top and soffit come from the joined solid; when the floor wins the join, B7 and the upper column go wrong
[KataRunReader.cs:66-79](../../../HPRebar/HPRebar/KataExport/Service/KataRunReader.cs#L66-L79), used by [KataHeaderReader.cs:36-37](../../../HPRebar/HPRebar/KataExport/Service/KataHeaderReader.cs#L36-L37), [KataSupportCollector.cs:139-141](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L139-L141)
- `TopFt = max vertex Z` of the post-join solid. If the floor cuts the beam (join order floor > beam, common after manual or "join all" tools), `TopFt` = slab soffit. Consequences:
  - B7: `|slabTop − beamTop| = t` (100–200) > 50 mm, so no slab is ever matched and B7 = 0.
  - Upper probe at `TopFt + 100` lands inside the slab and hits the column below that reaches the level. At the roof this reports a column above that does not exist.
  - The height fallback (no `h` parameter) = h − t.
- GIẢ ĐỊNH CHƯA XÁC MINH: how often floor-wins joins occur in the user's models. The consequences follow from the code.
- Fix: read the vertical range (and width) from `GetOriginalGeometry` + `GetTransform()`, or from location line Z + z justification + `h`. Keep the post-join solid only for the fallback width.

## Medium

### M1 — Columns and walls standing on the run are taken as supports
[KataSupportCollector.cs:48-63](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L48-L63)
- A column planted on a transfer beam, or an upturned beam: the column is modelled from the beam soffit or from the level below the beam top. It is hit by `top−20`/mid, so it is written as a support at a point load.
- Fix: a Column or Wall is a support only if its bottom (`VerticalRange`) is below `soffit − inset`. Otherwise it is only an upper candidate. This matches the spec's `KataUpperColumnFinder` idea.

### M2 — The upper-column probe at `top + 100 mm` gives false positives
[KataSupportCollector.cs:20](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L20), [KataSupportCollector.cs:139-141](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L139-L141)
- Beam dropped ≥ 100 mm below the level (wet-area drop, z offset): the column below reaches the level, so the probe hits it. At the roof this is a false "column above".
- Stepped run: the lower piece's probe extends ±1500 mm and hits the support column that rises to the higher piece.
- The spec asked for a window "column bottom ∈ [beam bottom, beam top + 500]".
- Fix: probe at `max(top of pieces within reach) + ~500 mm` (above any slab or drop), or require the column top ≥ beam top + 500.

### M3 — The candidate collection is document-wide and ignores phases and design options
[KataSupportCollector.cs:118-121](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L118-L121)
- The spec had `FilteredElementCollector(doc, view.Id)`. The document-wide choice is defensible (a plan's view range hides footings; supports should not depend on visibility settings), but:
  - demolished or future-phase columns and walls are picked up;
  - elements in secondary design options are picked up;
  - either way they become false supports.
- Fix: keep document-wide. Add `ElementPhaseStatusFilter(view phase, New|Existing)` and keep only main-model + primary/active-option elements (`e.DesignOption is null || e.DesignOption.IsPrimary`). Record the deviation in the phase file.

### M4 — Run validation checks plan only: sloped beams and beams from different storeys pass
[KataRunReader.cs:31-39](../../../HPRebar/HPRebar/KataExport/Service/KataRunReader.cs#L31-L39)
- A sloped piece is accepted:
  - probes run at max/min Z, so the lowest probe runs under the high end and picks crossing beams there as "carrying";
  - stations are plan-projected lengths.
- Beams from two storeys on the same grid (a box selection in an elevation) are accepted. Their stations overlap and supports of both floors are mixed. The core only warns "overlaps previous beam".
- Fix: reject pieces whose location line has |ΔZ| > ~5 mm ("sloped beams not supported", like curved beams). Reject pieces whose vertical range does not overlap or touch the adjacent piece's range, or whose reference levels differ.

### M5 — `ZOffsetMm` reads only `z Offset Value`
[KataRunReader.cs:87](../../../HPRebar/HPRebar/KataExport/Service/KataRunReader.cs#L87)
- Beams dropped through Start/End Level Offset (`STRUCTURAL_BEAM_END0/END1_ELEVATION`, the usual UI) read 0, so rows 19/21 of that span lose the step. The reader already knows each piece's real top.
- This follows the Dynamo contract. Changing it is a data-source decision for the lead, so it is not auto-applied.
- Recommendation: effective top offset = END0 elevation + z offset (with z justification), or measured top − level. Warn when END0 ≠ END1.

### M6 — B10 uses `Level.ProjectElevation`
[KataSessionReader.cs:36](../../../HPRebar/HPRebar/KataExport/Service/KataSessionReader.cs#L36)
- The API docs say "relative to project origin, no matter what values of the Elevation Base parameter is set". The level head the user reads follows the level type's Elevation Base (Project Base Point or Survey Point).
- GIẢ ĐỊNH CHƯA XÁC MINH: whether "project origin" means the internal origin or the PBP on a model whose PBP was moved vertically.
- Fix: add a phase 6 case with the PBP Z ≠ 0 and one with Elevation Base = Survey. If it differs, compute relative to `BasePoint.GetProjectBasePoint(doc).Position.Z` explicitly.

### M7 — B9 is not negated under Reverse (core, cross-phase)
[KataRowBuilder.cs:110](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataRowBuilder.cs#L110)
- The reader defines B9 as "positive = left of the run direction". Reverse flips the direction, and rows 19/21/23 are multiplied by `sign`, but B9 is not.
- Decide in phase 6 together with the Kata sign convention (the plan already lists it).

### M8 — Grids come from the active view only, and an empty result is silent
[KataGridReader.cs:32](../../../HPRebar/HPRebar/KataExport/Service/KataGridReader.cs#L32)
- Some views show no crossing grids: a 3D view, a section parallel to the grids, grids hidden, or grids that live in a linked model. Rows 22/23 are then blank without a warning. B8/B9 is warned by the core.
- Fix: warn when no grid crosses the run extent. Optionally collect grids document-wide and keep those `!grid.IsHidden(view)`.

### M9 — Performance
[KataSupportCollector.cs:43-46](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L43-L46), [KataSupportCollector.cs:102-108](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L102-L108)
- `GetSolids` (Fine) runs before classification. Every non-structural masonry wall and every framing element under 45° extracts geometry that is then thrown away.
- A single axis-aligned outline for the whole run: a 40 m run at 45° searches about 31×31 m (hundreds of candidates), each with 4·P probes × solids.
- `Probes(run)` is rebuilt for every candidate.
- Fix:
  - classify first (category, `IsStructural`, angle), then extract solids;
  - pre-filter each candidate's bbox against the axis band in frame coordinates, or OR one outline per piece;
  - materialise the probe list once.
- Floors: `VerticalRange` tessellates every edge of each whole-storey slab ([KataHeaderReader.cs:36](../../../HPRebar/HPRebar/KataExport/Service/KataHeaderReader.cs#L36)). The bbox `Max.Z` or `HostObjectUtils.GetTopFaces` is enough.

## Low

- **L1** [KataBeamGeometry.cs:33](../../../HPRebar/HPRebar/KataExport/Model/KataBeamGeometry.cs#L33): two classes in one file, and Model depends on Service (`using HPRebar.KataExport.Service` for `KataAxisFrame`, [line 4](../../../HPRebar/HPRebar/KataExport/Model/KataBeamGeometry.cs#L4)). Split `KataRunGeometry.cs`, and consider moving `KataAxisFrame` to `Model/`.
- **L2** [KataSolidReader.cs:31-33](../../../HPRebar/HPRebar/KataExport/Service/KataSolidReader.cs#L31-L33): a nested `GeometryInstance` is not recursed, so column or footing families with nested non-shared families lose those solids.
- **L3** [KataHeaderReader.cs:54-58](../../../HPRebar/HPRebar/KataExport/Service/KataHeaderReader.cs#L54-L58): first-wins on duplicate names over an unordered `ParameterSet` is nondeterministic (e.g. a project parameter and a family shared parameter with the same name). Prefer `GetOrderedParameters()`. Doubles are converted to the project display unit (`GetUnitTypeId`); fine for Name/Count, but worth a comment.
- **L4** [KataRunReader.cs:105-111](../../../HPRebar/HPRebar/KataExport/Service/KataRunReader.cs#L105-L111): `TypeLength` does not check that the spec is Length. A Number parameter named `b`/`h` gets ×304.8. Check `Definition.GetDataType() == SpecTypeId.Length`.
- **L5** [KataSupportCollector.cs:56-59](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L56-L59), [line 90](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L90): `parallelWalls` counts every parallel wall in the ±5 ft box, even ones that never touch a probe. `skippedBeams` counts intervals, not beams. Both make the warnings noisy.
- **L6** [KataSupportCollector.cs:64-66](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L64-L66): braces and vertical framing (plan length 0 → `PlanCos` 0 → "crossing") can become Beam supports. Filter `StructuralType == Beam`.
- **L7** [KataAxisFrame.cs:34-35](../../../HPRebar/HPRebar/KataExport/Service/KataAxisFrame.cs#L34-L35): at exactly 45°, `|X| ≥ |Y|` flips on the last bit, so the Normal direction can differ between two drawings of the same run. Add a tolerance.
- **L8** [KataAxisFrame.cs:49](../../../HPRebar/HPRebar/KataExport/Service/KataAxisFrame.cs#L49): the doc comment says "all mm, elevation in feet space", which contradicts itself.
- **L9** [KataGridReader.cs:32](../../../HPRebar/HPRebar/KataExport/Service/KataGridReader.cs#L32): a view-scoped collector on a template or non-graphical view throws. The command (phase 5) must require a plan/section/elevation view.
- **L10** [KataHeaderReader.cs:28-37](../../../HPRebar/HPRebar/KataExport/Service/KataHeaderReader.cs#L28-L37): the B7 search box reaches 1000 mm past the run ends, so floors of the next bay count. Sloped or shape-edited floors report their highest vertex.
- **L11** [KataSupportCollector.cs:70-71](../../../HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L70-L71): the crossing height fallback can be 0, giving "300x0".
- **L12**: no Serilog trace of the candidates, hits and skips. Phase 6 live debugging will need one `Log.Debug` per candidate (category, id, intervals).

## Design choices — evaluation

| Choice | Verdict |
|---|---|
| Probes + `IntersectWithCurve` instead of boolean ∩ + bbox fallback | ✅ for columns, walls and footings (no boolean failures; full width past the beam end). ❌ for framing (H2) — use analytic geometry there |
| Probe heights top−20 / mid / soffit+20 / soffit−20 | ✅ `soffit−20` gives useful redundancy when the beam cuts the support. ❌ it creates H3 and, on sloped pieces, M4 |
| Document-wide + ±5 ft bbox | 🟡 better than the view-scoped spec, but needs phase/option filters (M3) and a tighter prefilter (M9) |
| Walls = support unless ≤ 30° to the run | ✅ (arc walls are never tested for being parallel; minor) |
| Crossing 45–135°, support if at a hard support or soffit ≤ run soffit + 25 | ✅ rule is sound; undermined by H2 detection |
| Upper = probe at top + 100 | ⚠️ M2 |
| Grids: active view, unbounded 2D | ✅ formula; ⚠️ silent when empty (M8) |
| B8/B9: parallel ≤ 1°, within ±max b, grid − centre, left positive | ✅ matches contract; Reverse sign open (M7) |
| Slab: thickest floor with top within 50 mm of a beam top | ✅ rule; ⚠️ depends on H4 |
| Level = `ReferenceLevel.ProjectElevation` | ⚠️ M6 |

## Conventions
✅ file-scoped namespaces, `sealed`, `required`/`init` (Polyfill covers net48), files ≤ 161 lines, why-comments, no plan refs, no references to other features, `RevitView` alias where `View` is used, no `ElementId.Value/IntegerValue`. The session snapshot keeps only ElementIds and primitive values (safe for the modeless window).

## Plan follow-ups (for lead)
- Phase-03 success criteria: R26 ✅, R24 ✅ (verified in isolated artifacts, `DeployAddin=false`).
- Spec deviations to record in phase-03:
  - probes instead of boolean + bbox fallback;
  - no separate `KataUpperColumnFinder`;
  - document-wide collection instead of view-scoped;
  - B7 = thickest floor in the box.
- Phase 6 must include: a crossing beam at a column (H2), equal-depth crossings with both join orders, a grade beam on a strip footing or lean concrete (H3), a floor-wins join (H4), a dropped roof beam (M2), PBP Z ≠ 0 (M6), and a Reverse B9 check (M7).

## Recommended actions
1. H1: catch `ApplicationException` per solid, guard `Volume`, warn.
2. H2: analytic station/width for framing; pre-join vertical range.
3. H3: skip `WallFoundation` and foundation `Floor`, plus a length cap with a warning.
4. H4: pre-join geometry (`GetOriginalGeometry` + transform) for the run's top/soffit/width.
5. M1/M2: bottom-below-soffit rule for supports; upper probe at max top + 500.
6. M3/M4/M9, then the rest.

Status: DONE_WITH_CONCERNS
