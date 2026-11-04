# Code review — phase 06 (page sections, Revit section plans, elevation) — 2026-11-03

Scope: uncommitted `KataColumnSectionViews`, `KataColumnElevationView`, `KataColumnViewAnnotator`, `KataColumnRoundHoopFactory`, `KataColumnRebarCreator` (views part, `_created`), `KataDrawingDims` diff, Core `KataColumnSectionDrawing/SectionDims/ElevationDims`, page `ColumnSection.tsx`/`ColumnRebarView.tsx`, `KataColumnSectionDrawingTests`. Read-only.

Gates run: `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false -p:SkipFrontendBuild=true` ✅ 0 err; `dotnet test HPRebar.Core.Tests` ✅ 1792/1792. vitest CHƯA TEST (not run here). Revit not run.

Score 7/10 — geometry/unit math sound, beam dims untouched; main gap = view steps can still roll back the bars.

## Acceptance (plan phase 6)
| Item | State | Note |
|---|---|---|
| Page: one section per storey, numbers, none on errors | ✅ | Build returns [] on errors (verified :81) |
| Section = structural plan at cut, 1:20, crop, dims, tags | ✅ | code matches live notes |
| Elevation along b 1:50, levels/laps/zones, tags | ✅ | mirrored frame not handled (M2) |
| View failure never loses bars | ⚠️ | only 3 exception types caught; runner commit errors fatal (H1) |
| Rerun reuses views, clears own drafting | 🟡 | yes for same run key + level; orphans on name failure / key change (M1, L1) |
| KataDrawingDims beam behaviour unchanged | ✅ | `a ? b ?? c : d ?? e` binds as intended; defaults null → old path (verified) |

## Verified OK (don't re-check)
- Units: cut mm absolute → ft − `level.ProjectElevation` (internal coords) consistent with `ToModel` absolute Z ([KataColumnSectionViews.cs:127](HPRebar/HPRebar/KataExport/Service/KataColumnSectionViews.cs#L127), annotator :42-47).
- View range order top ≥ cut ≥ bottom = depth always holds (maxSpacing ≥ 0).
- Crop: corners mapped through `CropBox.Transform.Inverse`, axis-aligned envelope → rotated frame gets a larger but correct crop.
- Section dims: vertical chain ticks along b (`drawingRight = AlongB`), dim line `At(w+gap, y)` — correct for turned frames.
- Section Build is also called at plan time (`KataColumnRebarSummaryMapper.Map`), same inputs → a Build throw surfaces before Generate, not after bars.
- `_columns[k]` / `storeys[k]` / `drawings[k]` aligned (reader: one column per storey, bottom→top).

## Findings

### H1 — view steps share the fatal runner → bars lost [verified by code; trigger likelihood inferred]
[KataColumnRebarCreator.cs:84-90](HPRebar/HPRebar/KataExport/Service/KataColumnRebarCreator.cs#L84-L90). `runner.Run` throws when the step's transaction does not commit (any Revit *error* posted by dims/tags/view regen → `ProceedWithRollBack`, [KataTransactionRunner.cs:118-123](HPRebar/HPRebar/KataRebar/Service/KataTransactionRunner.cs#L118-L123)); also any exception outside the 3 caught types (NullReference, KeyNotFound, IndexOutOfRange, Revit-free `InvalidCastException`…) in `views.Create`/`Elevation` escapes. Both → catch at :108 → `group.RollBack()` → all bars, hoops, ties, numbers gone; user sees "failed". Contradicts the phase contract (views never lose bars). Same trap as beam long-section drafting (2026-10-08).
Fix: wrap each view `runner.Run(...)` in `try { } catch (Exception ex) when (ex is not OutOfMemoryException) { warnings.Add(...); Log.Warning(ex, ...); }` — the runner already rolled back only that step's transaction, the group stays assimilable. Keep the per-storey catch too.

### M1 — invalid view name orphans an unstamped view every run [verified order by code; Revit name rule from beam review]
[KataColumnSectionViews.cs:115-120](HPRebar/HPRebar/KataExport/Service/KataColumnSectionViews.cs#L115-L120), [KataSectionViews.cs:46-52](HPRebar/HPRebar/KataRebar/Service/KataSectionViews.cs#L46-L52). `_name` = sheet C3 / `STR_ElementName`, `d.Level` = storey name. A char from `\ : { } [ ] | ; < > ? \` ~` → `view.Name =` throws after `ViewPlan.Create`/`CreateSection`, before `KataDraftingStorage.Write`. Caught (bars kept) but the new view stays, untagged, never found again → N+1 orphan plans per rerun (6 per run on a 6-storey stack) + elevation.
Fix: sanitize the name (replace forbidden chars with `-`) in one helper used by both; `Write` the stamp right after creation, before naming.

### M2 — mirrored lowest column: elevation looks away from the stack [inferred: mirrored `FamilyInstance.GetTransform()` has a reflection]
[KataColumnElevationView.cs:226-237](HPRebar/HPRebar/KataExport/Service/KataColumnElevationView.cs#L226-L237) + [KataSectionViews.cs:66-68](HPRebar/HPRebar/KataRebar/Service/KataSectionViews.cs#L66-L68). Frame = column transform as-is ([KataColumnStackReader.cs:68](HPRebar/HPRebar/KataExport/Service/KataColumnStackReader.cs#L68)). Box look = (−B)×Z = Z×B, which equals +AlongH only for a right-handed frame. Mirrored column → AlongH = −(Z×B); origin at frame y = minY−100 is then *behind* the look, crop depth extends away from the column → empty elevation, dims floating, every tag refused (logged at Information only, M3). Sections unaffected (plan).
Fix: `double look = XYZ.BasisZ.CrossProduct(annotator.AlongB).DotProduct(annotator.AlongH);` → if < 0 put the plane at `maxY + FrontMm` (or pass `Right = -AlongB`). Add a live/golden check with a mirrored column.

### M3 — tag refusals silent; set tags may reference a bar outside the plan range [refusal path verified; set-bar position inferred]
[KataColumnViewAnnotator.cs:88](HPRebar/HPRebar/KataExport/Service/KataColumnViewAnnotator.cs#L88), [KataColumnSectionViews.cs:181-186](HPRebar/HPRebar/KataExport/Service/KataColumnSectionViews.cs#L181-L186). Refused tags → `Log.Information` only, no user warning, no count. Hoop/tie sets are tagged via `GetSubelements().First()`; if that is bar 0 of the set it sits at the zone start, which for a long zone lies far below the view depth (cut − spacing − 50 mm) → subelement refused, fallback "curve" or nothing. Live note says subelement worked; check `TagWays` in the live log for hoop/tie counts.
Fix: count refusals per view → one warning "N số hiệu không gắn được"; for sets prefer the geometry curve visible in the view (or verify subelement ⇔ bar index).

### M4 — cut search can land where storey k's own bars are absent [inferred; untested]
[KataColumnSectionDrawing.cs:130-148](HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnSectionDrawing.cs#L130-L148). `crossing` counts all runs of all storeys and only minimises count. Mid in a lap zone (short storey, long laps): a lower z where only k−1 lap bars cross (n_{k−1} < n_k), or any z with 0 crossings, wins → Revit plan shows k−1's bars (or none) while page + tags are storey k's → tags refused (M3). No test covers `CutElevation`.
Fix: require every non-starter run of storey k to cross z (and all crossing plumb), then minimise; Theory tests for long lap / staggered start.

### L1 — index/run-key identity leaves stale stamped views [verified]
`ColumnSection:k` by index ([:102](HPRebar/HPRebar/KataExport/Service/KataColumnSectionViews.cs#L102)); fewer storeys or a re-pick with another column set (RunKey = sorted UniqueIds) → old views keep stamps + stale dims, never cleared; new ones get "(Kata)" names. Document or sweep views of the run with k ≥ count.

### L2 — duplicate stamped plans picked unordered, no warning [verified]
[:103](HPRebar/HPRebar/KataExport/Service/KataColumnSectionViews.cs#L103) `FirstOrDefault()` without `OrderBy(IdValue)`; beam path orders + warns. Align.

### L3 — ClearDrafting before Cut/Crop [verified]
[:79-83](HPRebar/HPRebar/KataExport/Service/KataColumnSectionViews.cs#L79-L83). A reused view the user put under a template controlling View Range/Crop → `SetViewRange` throws each run after old dims were deleted → committed view without dims. Clear after Cut/Crop succeed.

### L4 — K1/K2 duplicated knowledge [verified]
`ToModel` creator :326-331 = annotator :42-47; `UniqueName` annotator :99-115 = `KataSectionViews.UniqueName`; Core `Plumb` (`.First()`, throws) vs creator `PlumbPosition` (`DefaultIfEmpty`) — same rule, different failure; a run without a plumb segment throws InvalidOperationException at plan time and blanks the page. One frame mapper on the annotator, one plumb rule in Core.

### L5 — stringly typed roles (N9/S1) [verified]
`"bar"/"hoop"/"tie"` literals across creator, section and elevation `switch`/`Where`. Use an enum on `KataColumnCreatedRebar`.

### L6 — beam wording in column run [verified]
`KataSectionViews.SectionType` throws "…để tạo mặt cắt dầm" → column warning "Mặt đứng: … mặt cắt dầm".

### L7 — size/structure triggers [verified]
C2: `KataColumnRebarCreator` 360 lines (views orchestration could move to one `KataColumnViewsStep`). C4: `KataColumnSectionViews.Warnings` exposes `List<string>`. D2: annotator + round-hoop factory constructors run collectors. FM5: `_created`/`_hooklessRoundHoops` fields declared mid-class.

### L8 — refused dims not reported [verified]
`views.RefusedDims` / elevation refused only in the log; surface "N chuỗi kích thước bị Revit từ chối" when > 0.

### L9 — page [verified]
`ColumnSectionGrid` keeps the old `open` drawing after a reload (stale enlarged section); plan text says bar positions "interpolated at the cut", implementation + Core doc use the longest plumb segment (plan drift, not a bug). TS contract matches C# record (camelCase, `kind` 'closed'|'cross').

### L10 — test gaps (T4)
No test for `CutElevation`, round `Build`, 1-storey elevation (starter→storey 0 laps only), or `KataColumnSectionDims` with layer-2 bars on the face row.

## Round hoop factory
Looks sound: shape path in a SubTransaction rolled back on refusal, centre/radius checked ±5 mm after `ScaleToBox`, arc fallbacks flagged. Nit: `outer` uses `BarNominalDiameter`; Revit draws with model diameter — within the 5 mm check, fine.

Status: DONE_WITH_CONCERNS

## Fix round 2026-11-03
| # | Sev | Fix |
|---|---|---|
| 1 | High | `KataColumnRebarCreator.Drawing<T>`: section + elevation steps caught (runner already rolled the step back) → warning, group keeps bars |
| 2 | Med | view stamped (`KataDraftingStorage.Write`) before `Name`; `KataSectionViews.UniqueName` public, replaces `\:{}[]|;<>?`~` by '-'; annotator copy removed (K1) |
| 3 | Med | elevation plane side from sign of (Z×b)·h; Frame.Look = real look |
| 4 | Med | refused tags counted (`RefusedTags`, log Warning) + warning to user; refused section dims → warning (#13) |
| 5 | Med | cut must cross every own bar of storey k; test `Build_Sample_CutPassesThroughEveryOwnBarOfTheStorey`; `Plumb` falls back to first point (no `.First()` throw) |
| 7 | Low | duplicate stamped plans ordered by id + warning |
| 8 | Low | ClearDrafting after Cut/Crop |
| 11 | Low | message "mặt cắt dầm" → "mặt cắt" |
Not fixed (Low, noted): 6 stale views after storey count/column set change, 9 ToModel twice, 10 role strings, 12 size/structure triggers, 14 page enlarged view after reload, 15 more tests (round, 1 storey).

## Low round 2026-11-04
| # | Fix |
|---|---|
| 6 | `KataColumnSectionViews.RemoveExtra`: stamped section views of storeys ≥ count deleted; on a sheet or active → untagged + warning. Column-set change (new run key) still leaves the old views (accepted: other key = other stack) |
| 9 | `KataColumnFrame.ToModel` shared by creator + annotator; `PlumbPosition`/`PlaneNormal` moved there |
| 10 | `KataColumnRebarRole` enum |
| 12 | views orchestration → `KataColumnViewsStep` (creator 360 → 284 lines); elevation failure now rolls its step back (no half view) |
| 14 | `ColumnSectionGrid` keeps the open storey by name: reload shows its new drawing, closes when gone; vitest |
| 15 | tests round stack sections (on circle / one Ø lap inset, no ties), one-storey elevation |
Checks: Core 1818/1818, vitest kataColumn 26/26, build R26. Live 2026-11-04 copy 27: 94 elements, 6 views, 24 + 4 dims 0 refused, 84 tags; rerun E4/F2/G5/H3 148 elements, 21 = 21 numbers, 132 tags, no warnings. RemoveExtra path not exercised live (needs a stack losing storeys).
Checks: Core 1793/1793, vitest kataColumn 25/25, build Debug.R26 ok. Live 2026-11-04 copy kata-col-rebar25 (detached): 94 elements, 6 section views, 24 dims 0 refused, elevation 4 dims 0 refused, 84 tags (subelement 84), 0 refused tags, no warnings beyond cover type; rerun 94 deleted/94 created, same counts, no duplicate-view warning. Visual check: not done (UltraViewer foreground).
