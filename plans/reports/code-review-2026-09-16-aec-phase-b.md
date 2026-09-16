> **Status 2026-09-16 (later the same day): every finding above (H1–H3, M1–M7, Lows) fixed; pinned by `HPAutoCad.Aec.Tests` 100/100, `HPAutoCad.Mcp.Server.Tests` 97/97 and `run-aec-tools-live.ps1` 55/55 — see `plans/260916-1140-aec-automation-mcp-autocad/reports/phase-B-semantic-live.md` § Review round.**

# Code review — AEC Automation MCP for AutoCAD, phase B semantic (uncommitted)

Date 2026-09-16 · reviewer code-reviewer · read-only (no host launched, no `*.ps1` run) · plan `plans/260916-1140-aec-automation-mcp-autocad/` · phase A findings not re-reviewed except where phase B depends on one (M7)

## Scope

| Item | Value |
|---|---|
| New | `HPAutoCad.Aec/Classification/{AecType,ShapeMetrics,ClassificationRule,ClassificationRuleSet,AecObject,AecClassifier}.cs` (550 LOC), `Rules/aec-classification.default.json` (26 rules), `Relationships/RelationshipDetector.cs` (157), `Cad/ClassificationService.cs` (53), `AecTools.Semantic.cs` (131), `HPAutoCad.Aec.Tests/{ClassificationTests,RelationshipTests}.cs` (16 tests), seeds `Aec/classify_aec_entities`, `Aec/get_entity_relationships` |
| Modified | `HPAutoCad.Aec.csproj` (EmbeddedResource), `AecTools.cs` (partial), `AutocadHostProfile.cs` (+Aec category), `SeedLibraryTests.cs` (19 seeds, shim theory), `tools/harness/aec-tools-live.py` (step B) |
| Evidence (controller) | build 0 warnings; Aec.Tests 69/69; Server.Tests 93/93; live `run-aec-tools-live.ps1` 51/51 in AutoCAD 2026 |
| Evidence (reviewer) | `dotnet build HPAutoCad.Aec` 0 warnings; `dotnet test HPAutoCad.Aec.Tests` 69/69; `dotnet test HPAutoCad.Mcp.Server.Tests` 93/93 (re-run 21:45); scratchpad probe against the built `HPAutoCad.Aec.dll` reproduced H1, H2, H3, M1, M6, M7, L1, L3 below |
| Not covered | live behaviour of nearby-text evidence across spaces, user rule-set files, `test_tool` over the seed examples |

## Checks requested

| # | Check | Verdict | Where |
|---|---|---|---|
| a | Rule matching / confidence / alternatives / Unknown / disciplines / malformed rules | Pass with **M1**, M4, L2–L4, L11 | Hard criteria all in `Reject` (`ClassificationRule.cs:75-89`), nearby text is evidence only (`AecClassifier.cs:53-61`), single boost per rule, cap 0.99, round 3; alternatives skip the winner's type but not each other's (L4); Unknown carries the top 3 matches; disciplines filter rules, not results — consistent with `ParseDisciplines`. `"layers": null` → NRE at classify time; typo'd keys silently accepted (M1) |
| b | Default rule set | **Fail (H2)** | 11 over-broad/contradicting patterns reproduced (table under H2). `closed: true` rules all carry explicit `types` without INSERT/TEXT, so a block's bounding rectangle is not accepted as a footprint — but four rules have neither `types` nor `closed` (`a-window-layer`, `a-stair`, `a-furniture-layer`, and `a-door-arc` has no `closed`) and accept TEXT/MTEXT/DIMENSION/POINT on the layer. Ties resolve by file order (M4) |
| c | ShapeMetrics | Pass with **M7**, L5 | 4-vertex non-approximate rings measured from two adjacent edges — right for any rotated rectangle (`ShapeMetrics.cs:57-65`, test `Rotated_rectangle_…`); a non-rectangular quad reports its first two edges (acceptable proxy); ring with a repeated closing vertex falls to the bounding box (M7); `% 180` correct incl. 180 → 0; circles: `IsCircular` by DXF name, `MainAxis` null via the 1.5 ratio (magic number, L5); `MainAxis` centre line math verified for both edge orders (`:76-84`) |
| d | RelationshipDetector | **Fail (H1)** + M2, M3, M6 | `ConnectionGap` = nearest run end to the other's boundary (0 when inside), symmetric, null for two footprints (`:139-152`, test-pinned); connected/near confidence `1 − d/limit·0.5` consistent; `boxesClose` gates every proximity relation with `reach ≥ max(EndpointConnection, nearRadius)` — correct for touching too (`distance ≤ EndpointConnection`); intersect/touching `locationMm` is (0,0,0) when there is no crossing point (H1); `valueMm` holds degrees for parallel/perpendicular and the two are inconsistent (M2); `index.All()` per source is O(S·T) (M6); early return + `limit + 1` makes `count` a floor, not a total (M3); `string.CompareOrdinal` on handles is a consistent total order (both sides come from `Handle.ToString()`, uppercase, no padding) — fine for de-duplication even though it is not numeric |
| e | ClassificationService | Pass with M5 | `filter.Space` = "all" for every handle-based call → text index spans model + every layout (M5); `MaxTextIndex` truncation warning dropped; `Classify` reads geometry for all `maxCandidates` then pages in memory — inherent to needing `summary`, acceptable at the 5 000 default, 100 000 ceiling is heavy but bounded |
| f | RuleSet.Load | Pass with M1, L7, L9 | Path traversal refused (`ClassificationRuleSet.cs:47`, test-pinned); `default`/`user`/`<name>[.json]`/null fallback as documented; embedded name = csproj `LogicalName` (test `Embedded_rule_set_loads_and_validates` proves the stream resolves); comments + trailing commas allowed (`:23-24`); errors are `ArgumentException` with name + source. Gaps: null lists, unknown keys, min > max, negative radius/boost not validated (M1) |
| g | AecTools.Semantic | Pass with M3, L8 | Clamps `minConfidence`/`limit`/`offset`/`maxCandidates`; `handles` override `filter` with a warning; empty source → `ArgumentException` (ADR-02); unknown relations/disciplines/filter/tolerance keys warned; summaries bounded (19 types, 3 disciplines, 9 relations); `Truncated` honest for classify (`:39`) and for relationships given the cut (`:93`), but `count` is not the total (M3) |
| h | Seeds | Pass with L6, L10 | Schema keys ⇔ `args.X` keys 9/9 both tools; defaults match code (0.5 / 100 / 0 / 5000; near default 100 = `DefaultNearRadiusMm`); `transaction: none`, one `AecTools.` call, ≤ 12 lines (theory-pinned); descriptions accurate except `valueMm`/`locationMm` undocumented (M2) and "count tells the total" (M3); example 3 of classify needs a user rules file (L6) |
| i | Output size | **Fail (H3)** | measured with the bridge's options: 100 column outlines 52 958 B (81 % of cap), 100 door blocks with 12 attributes 103 758 B, relationships ×100 20 593 B, ×500 102 593 B |
| j | Repo rules | Pass with L5, L12, L13 | file-scoped namespaces, `nullable enable`, `sealed`/`record`/static, all files < 300 (max 157), no plan references in comments, XML docs on types; magic numbers (L5), a few undocumented public members (L12), two unused helpers (L13) |

## Findings

### High

**H1 — `intersect` / `touching` report `locationMm = (0,0,0)` whenever there is no boundary crossing point**
`HPAutoCad/HPAutoCad.Aec/Relationships/RelationshipDetector.cs:87` and `:111`: `m.Points.FirstOrDefault()` on an `IReadOnlyList<Pt>` returns `default(Pt)` — the origin — when the list is empty, and `Make(…, Pt? at)` boxes it into a value. `SpatialRelation.Intersects` holds without points when one shape lies inside the other (`SpatialPredicates.cs:39`), `Touches` holds without points when the shapes are within `EndpointConnection` but do not actually meet (`:52`). Probe: room ∋ desk → `{"relation":"intersect","locationMm":{"x":0,"y":0,"z":0}}`; two wall lines 5 mm apart → `touching … locationMm (0,0,0)`. The AI receives a plausible-looking wrong coordinate. Fix: `m.Points.Count > 0 ? m.Points[0] : null` (or fall back to `a.Centroid` for containment, the closest-point pair for touching). Add a test for both cases — the current test only covers a proper crossing.

**H2 — Default rule set: over-broad wildcards and self-contradicting Vietnamese patterns produce high-confidence misclassifications**
`HPAutoCad/HPAutoCad.Aec/Rules/aec-classification.default.json:8-35`. Reproduced with the embedded set (probe, `includeUnknown`):

| Input | Result | Rule / pattern | Why it is wrong |
|---|---|---|---|
| LINE on `STRUCT`, `S-STRUCTURE` | StructuralGrid **0.9** | `*TRUC*` (trục) | "STRUCT" is the most common structural layer stem; grid at 0.9 outranks every beam/wall rule (0.85) |
| LINE on `A-CLNG-GRID` | StructuralGrid 0.9 | `*GRID*` | AIA ceiling grid |
| LINE on `CUA-SO` | Door 0.85 (alt Window 0.8) | `*CUA*` ⊃ `*CUASO*`/`*CUA-SO*` | the window layer pattern is shadowed by the door one — every Vietnamese window becomes a door |
| INSERT `CUASO-1200` | Door 0.9 (alt Window 0.9) | `*CUA*` vs `*CUASO*`, tie → file order | same, on block names |
| LINE on `ONGGIO` | Pipe 0.85 (alt Duct 0.85) | `*ONG*` ⊃ `*ONGGIO*`, tie | ducts become pipes; `*ONG*` also hits `MONG` (foundation), `CONG`, `LONG`, `SONG` |
| LINE on `S-MONG` | Pipe 0.85 | `*ONG*` | foundation lines as pipes |
| LINE on `M-DAMPER` | StructuralBeam 0.85 | `*DAM*` (dầm) | fire dampers are beams |
| closed LWPOLYLINE on `P-SANR-FIXT` | StructuralSlab 0.85 | `*SAN*` (sàn) | AIA sanitary layers (`P-SANR*`, which the pipe rule itself lists) |
| closed LWPOLYLINE 400×400 on `COTAS` | StructuralColumn 0.9 | `*COT*` (cột) | Spanish/Portuguese dimension layer `COTAS`, French `COTE` |
| INSERT `COLD-WATER-TAP` | StructuralColumn 0.85 | block `COL*` | prefix, not word |
| INSERT `W-CLOSET` | Window 0.9 | block `W-*` | water closet |
| INSERT `STEEL-BRACE` | Fitting 0.8 | block `*TEE*` | "STEEL" |

Fixes (data only): anchor the Vietnamese stems to a name boundary (`*-COT*`, `COT-*`, `*_COT*`, or accept `?`-delimited forms), drop `*ONG*`/`*CUA*`/`*DAM*`/`*SAN*`/`*TRUC*` as bare infixes in favour of `ONG-*`, `CUA-DI*`, `DAM-*`, `SAN-*`, `TRUC-*`; give `*CUASO*`/`*ONGGIO*` rules a higher confidence than their parents or remove the parent's overlap; replace `*GRID*` with `S-GRID*|A-GRID*|*-GRID` minus `CLNG`; use `COL-*`/`COLUMN*` instead of `COL*`, `WIN-*`/`WINDOW*` instead of `W-*`, drop `*TEE*` or require `-TEE`/`TEE-`. Add a "does-not-match" test with these layer names — it is cheaper than a live scene.

**H3 — `classify_aec_entities` default `limit 100` and `get_entity_relationships` `maximum 500` breach the bridge's 64 KB result cap; the AI then gets truncated text instead of JSON**
Same failure class as phase A H3, not carried over to the new tools. Measured with `BridgeJson` options (camelCase, nulls dropped): a classified plain outline is 529 B (100 → 52 958 B, 81 % of the cap with no nearby-text evidence, no alternatives, no attributes); a door block with 12 attributes is 1 037 B (100 → 103 758 B); `properties.text` is uncapped (MTEXT notes on any `*FURN*`/`*STAIR*`/`*WINDOW*` layer are returned whole, and every TEXT/MTEXT with `includeUnknown`). A relationship item is ~205 B: ×100 = 20 593 B fine, ×500 = 102 593 B over. Fix: cap `limit` for classification at ~50 per page (`MaxClassifyLimit` beside `MaxDetailLimit`, `AecTools.Semantic.cs:29`), cap `properties.text`/attribute values (e.g. 120 chars) and drop `boundsMm` when `centroidMm` + width/depth are present; clamp relationships to 250 (`:78`) and say so in both `tool.json` `limit.maximum`/description (`classify…/tool.json:129-135`, `get_entity_relationships/tool.json:230-236`).

### Medium

**M1 — Rule-file validation misses the cases that break at classify time or widen a rule silently**
`HPAutoCad/HPAutoCad.Aec/Classification/ClassificationRuleSet.cs:19-26, 81-97`. (1) `"layers": null` (also `types`/`blockNames`) deserialises to a null `List<string>` → `NullReferenceException` in `Reject` (`ClassificationRule.cs:77`) on the first record, after validation passed (probe). (2) Unknown keys are ignored by System.Text.Json, so a typo (`"layer"` for `"layers"`) leaves the rule with no layer criterion — probe: such a Door rule at 0.95 matched a 400×400 outline on `S-COL`. That contradicts the class doc "a typo in a rule file surfaces as a tool error, not as a silently useless rule" (`:10-11`) — it surfaces as a silently over-broad rule, which is worse. (3) `minSizeMm > maxSizeMm`, `minAspectRatio > maxAspectRatio`, negative `nearbyRadiusMm`, `nearbyTextBoost` outside `[0, 1)` pass. Fix: `UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow` on `JsonOptions`; in `Parse` coalesce `rule.Layers ??= []` etc. (or make the setters coalesce); add the range checks. Three more `Rule_files_are_validated_on_load` cases.

**M2 — `valueMm` carries degrees for `parallel`/`perpendicular`, and the two report different things**
`RelationshipDetector.cs:127-128`: parallel → `min(angle, 180 − angle)` = deviation from parallel (probe: 0.43); perpendicular → the same expression = the raw angle (probe: 90), so the deviation from 90° the AI would want is not what it gets. Both land in a field named `valueMm` that `tool.json` never describes. Fix: separate `angleDeg` (actual angle between axes for all three axis relations, `valueMm` = offset for `aligned` only), and document `valueMm`/`angleDeg`/`locationMm` per relation in the description.

**M3 — `count` is a floor, not "the total", for `get_entity_relationships`; the self-set ×2 heuristic depends on iteration order**
`AecTools.Semantic.cs:82, 92-93`, `RelationshipDetector.cs:62`: the detector returns as soon as `limit + 1` results exist, so `count` never exceeds 101 at the default limit while `tool.json:235` promises "count tells the total". Phase A solved the same problem with `IssueOverscan = 4` (`AecTools.cs:34`) — reuse it or change the description to "at least". The self-set path (`ClassificationService.cs:44-48`) collects `2·(limit+1)` raw results then keeps the `Source < Target` half: it only guarantees ≥ `limit + 1` survivors because sources iterate in ascending handle order (query sorts by `Handle.Value`) and the earliest sources have mostly larger targets. Simpler and exact: de-duplicate inside `Detect` (`if (sameSet && relation is symmetric && CompareOrdinal(s.Handle, t.Handle) > 0) continue;`) and drop the ×2.

**M4 — Equal-confidence rules resolve by file order only because `List<T>.Sort` happens to be an insertion sort below 16 elements**
`AecClassifier.cs:66`. Introsort is unstable; the tie outcome (Door vs Window at 0.9, Pipe vs Duct at 0.85 in H2) is an implementation detail, not a contract. Use `OrderByDescending(m => m.Confidence).ThenBy(rule index)` and state "earlier rule wins ties" in the rule-file header so authors can order deliberately.

**M5 — The nearby-text index mixes model and paper space, is built twice for relationships, and its truncation is silent**
`ClassificationService.cs:25, 33-35, 42-43`. Every handle-based call has `Space = "all"` (`AecTools.Semantic.cs:115`), so the TEXT/MTEXT selection spans every layout; paper-space coordinates overlap model-space ones, so a title-block "C1" at (450, 100) is "nearby text" for a model-space column at (450, 100). `AecEntityRecord.Space` is available — key the index by space and match `t.Space == record.Space` in `Near`. `Relationships` calls `Classify` for source and target, each rebuilding the index (two full text scans per call with the default set, which uses nearby text). The `Stopped after 5000 candidates` warning of the text query is dropped, so a large drawing's classification quietly loses evidence — forward `texts.Warnings`.

**M6 — Axis relations are O(|S|·|T|) with `MainAxis` recomputed twice per pair per relation**
`RelationshipDetector.cs:59, 117-118`. Probe: 2 000 × 2 000 parallel walls, `perpendicular` (no hits) = 2.5 s; 5 000 × 5 000 (the default `maxCandidates` on both sets) extrapolates to ~16 s, and `maxCandidates` may be raised to 100 000 — the 60 s timeout then cancels the run. Precompute `MainAxis` once per object (a `Dictionary<string, Seg?>` or a field on `AecObject`), check `ct` inside the target loop, and warn + cap when `sources × targets` exceeds a budget (e.g. 4 M pairs) instead of scanning silently.

**M7 — A closed polyline with a repeated closing vertex is measured from its bounding box (phase A reader defect phase B depends on)**
`HPAutoCad/HPAutoCad.Aec/Cad/EntityShapeReader.cs:228-229` strips the duplicate last vertex only when `!pl.Closed`; a polyline with `Closed = true` **and** a repeated first vertex (common in DXF exports from Revit/Tekla/SketchUp) keeps 5 vertices and a zero-length closing segment. `ShapeMetrics.FootprintSides`/`MainAxis` (`ShapeMetrics.cs:57, 76`) require exactly 4, so the rotated 400×800 column of the test measures 746×893 at 0° through the bounds (probe); a rotated 200×6 000 beam outline fails `s-beam-outline`'s `minAspectRatio 3`. Fix in the reader (`if (closed && points.Count > 3 && points[0].AlmostEqualsXY(points[^1], tol)) points.RemoveAt(…)`) and make `ShapeMetrics` tolerant (ignore segments shorter than `TinySegment` when counting edges).

### Low

- **L1** `EntityFilter.WildcardMatch` builds a `Regex` per call and the default set has ~80 distinct patterns > `Regex.CacheSize` 15 → classifying 5 000 non-matching records costs 1.0–1.8 s, nearly all regex construction (probe). Precompile per rule the way `TextRegex` is (`ClassificationRule.cs:69-72`), or raise `Regex.CacheSize` once. Matters at the 100 000 `maxCandidates` ceiling.
- **L2** `MinShortSideMm`/`MinAspectRatio` docs say "(ignored for runs)" (`ClassificationRule.cs:39, 44`) but they reject runs — `!(null >= x)` is true (`:83-86`). Either the doc or the behaviour; the behaviour is the safer one.
- **L3** Evidence line "open run  mm" for shape-less records (XLINE on a grid layer, probe): `ClassificationRule.cs:98` should skip the size when `LengthMm` is null.
- **L4** `Alternatives` may list one aecType twice (two Furniture rules behind a Door winner) — `DistinctBy(m => m.Rule.AecType)` at `AecClassifier.cs:84`.
- **L5** Magic numbers outside named constants: `1.5` dominant-direction ratio (`ShapeMetrics.cs:87`), `0.5` confidence decay factor (`RelationshipDetector.cs:94, 101`), `200` ms regex timeout twice (`ClassificationRule.cs:69, 72`).
- **L6** `classify_aec_entities/examples.json:39` uses `ruleSet: "user"` → `ArgumentException` on any machine without `rules\aec-classification.json`; `test_tool` runs a tool's examples (`ToolLifecycleService.cs:105`). Use a filter-only example or note the precondition in the title.
- **L7** `summary.ruleSet.source` returns the absolute `%AppData%` path — the Windows user name — when a user set is loaded (`AecTools.Semantic.cs:47`, `ClassificationRuleSet.cs:54`). Return the file name.
- **L8** `ParseDisciplines` returns null (= every discipline) when every name was unknown after only a warning (`AecTools.Semantic.cs:129`); a negative `maxDistance` falls back to the default without a warning (`:76-77`).
- **L9** `Load("default.json")` / `Load("user.json")` resolve to `rules\default.json` / `rules\user.json`, unlike `default` / `user` (`ClassificationRuleSet.cs:50-52`); strip the suffix before the reserved-name check.
- **L10** Both seed schemas carry the phase A boilerplate contradiction: `filter` says handles "short-circuit everything else", `filter.handles` says "the other criteria only narrow this list" (`tool.json:33` vs `:79`; the latter is what `EntityQueryService.PostFilter` does).
- **L11** A pathological regex in a user rule throws `RegexMatchTimeoutException` at classify time (`ClassificationRule.cs:87`, `AecClassifier.cs:55`) — uncaught, so it is a generic failure that counts against the tool's stability although it is the user's file. Catch and rethrow as `ArgumentException("rule 'x': regex timed out")`.
- **L12** Public members without XML docs: `Discipline.Normalize`, `AecType.IsKnown`, `AecClassifier.Classify`, `RelationshipDetector.Detect`, `ClassificationRuleSet.Parse/Load` parameters.
- **L13** Unused: `AecType.IsLinear` (`AecType.cs:69`), `RelationType.IsProximity` (`RelationshipDetector.cs:24`) — YAGNI.

## Positive observations

- Clean split: `Classification/` and `Relationships/` never touch AutoCAD; `ClassificationService` is 53 lines of adapter; the seeds are two-line shims pinned by a theory test.
- Rules as data with load-time validation, path-traversal refusal, comment/trailing-comma tolerance, and the embedded/user/named fallback documented in one place.
- `ShapeMetrics` measures rotated rectangles from their edges, not the bounding box, and the centre-line construction in `MainAxis` is right for both edge orders.
- `ConnectionGap` semantics (run ends vs the other's boundary, 0 when inside, null for two footprints) are precise, documented and test-pinned with the 7 mm case the live harness also checks.
- `boxesClose` gating and a single `reach` derived from the requested relations keep the proximity relations near O(n).
- The `AecObject.Record` `required` + `[JsonIgnore]` live defect was fixed with a serialisation test that uses the bridge's exact options.

## Score and verdict

**6.5 / 10.** The engine design is sound and the pure code is well tested, but three things must not ship: a silent wrong coordinate (H1), a default rule set that misfiles common layers at 0.85–0.9 (H2), and page sizes that break the 64 KB contract the ADR itself states (H3). M1 (NRE from `null` lists, silent typo'd keys) and M3 (`count` contract) are cheap and should go in the same fix pass; M2/M4–M7 can follow.

Fix before commit: **H1, H2, H3, M1, M3.** Recommended in the same pass: M2, M4. Follow-up: M5, M6, M7, L1–L13.
