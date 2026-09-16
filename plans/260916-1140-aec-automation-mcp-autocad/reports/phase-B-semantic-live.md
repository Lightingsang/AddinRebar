# Phase B — Semantic: implementation + live verification report (2026-09-16)

## Implemented

| Piece | Files | Notes |
|---|---|---|
| Classification (pure) | `HPAutoCad.Aec/Classification/AecType.cs` (19 AEC types, 3 disciplines), `ShapeMetrics.cs` (footprint sides from the ring's own edges — rotated rectangles measure right —, length, orientation, main axis), `ClassificationRule.cs` (JSON rule: layers/types/blockNames wildcards, closed, size/short-side/aspect ranges, own-text regex, nearby-text regex as evidence), `ClassificationRuleSet.cs` (embedded default / `%AppData%\HPAutoCad\McpServer\rules\<name>.json`, validated on load, path traversal refused), `AecObject.cs`, `AecClassifier.cs` (best rule wins, alternatives, Unknown on request, discipline filter, minConfidence) | rules are data: `Rules/aec-classification.default.json`, 26 rules, AIA/NCS + English + Vietnamese layer conventions |
| Relationships (pure) | `Relationships/RelationshipDetector.cs`: intersect, connected (end-of-run gap → confidence 1 − gap/tol·0.5), near, inside, contains, touching, aligned / parallel / perpendicular on main axes | grid index for proximity relations; axis relations over the whole target set |
| Adapters + facade | `Cad/ClassificationService.cs` (query → text index when a rule needs it → classify; relationships classify both sets, self-set pairs de-duplicated), `AecTools.Semantic.cs` | `AecTools` is now partial |
| Seeds | `SeedLibrary/Aec/classify_aec_entities`, `Aec/get_entity_relationships` (category `Aec` added to the profile) | thin shims, `transaction: none` |
| Tests | `ClassificationTests` (9 → 16 + a 21-row layer theory) + `RelationshipTests` (6 → 10) → `HPAutoCad.Aec.Tests` 100; `HPAutoCad.Mcp.Server.Tests` 97 (19 seeds, page limits pinned to engine caps) | |
| Harness | `aec-tools-live.py` step B (8 → 11 checks) + G repeated-closing-vertex check; scene 24 entities | |

## Build / tests

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings; bundle deployed |
| `HPAutoCad.Aec.Tests` | 100/100 |
| `HPAutoCad.Mcp.Server.Tests` | 97/97 (19 seeds compile + validate + shim rule + limit caps) |

## Live (AutoCAD 2026, `run-aec-tools-live.ps1`)

Run 1: 47/51 — `classify_aec_entities` came back as "value is not serializable: InvalidOperationException": `AecObject.Record` was `required` and `[JsonIgnore]`, a combination System.Text.Json refuses. Fixed (non-required, pinned by `Aec_objects_serialise_with_the_bridge_options_and_without_the_record`). Run 2: **51/51**.

| Check | Result |
|---|---|
| classify S-*/A-*/M-* with unknowns | StructuralColumn 4, StructuralBeam 3, ArchitecturalWall 9, Pipe 1, Door 1; circle/text/hatch `Unknown` |
| column object | confidence 0.9, rule `s-column-outline`, evidence "closed outline on a column layer", "layer S-COL matches S-COL*|…", "closed footprint 400×400 mm"; properties width/depth/area/centroid |
| door block | `Door` 0.9 via `a-door-block`, attributes `MARK = D01` carried |
| disciplines Structural + paging | count 7, page 2 (offset 5, limit 5) = 2 items; summary names the rule set (default, embedded, 26 rules, 1 text indexed) |
| connected beams → columns | 6 connections; b2 → c4 gap 7 mm, confidence 0.65; ends named StructuralBeam / StructuralColumn |
| intersect pipe → beams | 2 (b1 + its duplicate) at (3000, 0) |
| self-set parallel on 4 columns | 6 de-duplicated pairs, `sameSet: true` |
| empty source | ArgumentException naming the requirement |

Regressions: phase-A checks unchanged (42 of the 51), read-only check over all 46 AEC calls.

## Review round (`plans/reports/code-review-2026-09-16-aec-phase-b.md`, 6.5/10 → all findings fixed)

| Finding | Fix | Pinned by |
|---|---|---|
| H1 `locationMm` (0,0,0) when no crossing point | inner centroid for nested intersects, closest vertex for near/touching, axis midpoint for aligned; parallel/perpendicular carry `angleDeg` and no location | `Every_relationship_names_a_real_location_never_the_origin`, live "B every proximity relationship carries a real location" (32 relationships, 0 at origin) |
| H2 default rule set misfiles STRUCT / CUASO / ONGGIO / DAMPER / SANR / COTAS / CLNG-GRID | rule set v2: every stem anchored to a name boundary, disjoint door/window patterns, duct above pipe | `Layer_stems_are_anchored_to_name_boundaries` (21 rows), `Fixture_and_slab_layers_do_not_bleed_into_each_other` |
| H3 classify `limit 100` / relationships `max 500` breach 64 KB | `MaxClassifyLimit` 50, `MaxRelationshipLimit` 250, text/attribute cut 120 chars / 8 attributes, schema `maximum` = engine cap | `Aec_seed_page_limits_match_the_engine_caps…`, `Verbose_text_and_attributes_are_cut…`, live "B classify limit 100 -> warned, capped at 50" |
| M1 `"layers": null` → NRE; misspelt key widens a rule silently | null-coalescing setters, `UnmappedMemberHandling.Disallow`, range checks, empty-pattern check | `Rule_files_refuse_null_criteria_silently_widening_a_rule_and_misspelt_keys` |
| M2 self-set de-dup after the cap loses pairs | de-dup inside the loop (`sameSet`, ordinal handle order), `Total` counted independently of the cap | `Self_set_symmetric_relations_are_reported_once_per_pair_and_the_total_counts_past_the_cap`, live "B relationships limit 2: count stays 6" |
| M3 `count` = `limit + 1` | `count` = `RelationshipOutcome.Total`, `truncated` = total > items | same |
| M4 `List.Sort` unstable on ties | `OrderByDescending` (stable) — equal confidence keeps rule-file order | `Equal_confidence_keeps_rule_file_order` |
| M5 two text indexes for one relationship call, silent truncation | one index shared (all-space when the filters disagree), warning at 5 000 texts | code path (`ClassificationService.Relationships`) |
| M6 axis relations O(n×m) unbounded | `MaxAxisPairs` 2 000 000 → `ArgumentException` naming the sizes | `Axis_relations_over_too_many_pairs_are_refused_before_any_work` |
| M7 repeated closing vertex counted | `ReadPolyline` drops it when first ≈ last | live "G closed polyline with a repeated closing vertex reads as a 4-vertex ring" |
| Lows: stand-in rectangles as footprints, `ruleSet.source` leaks `%AppData%`, `IsLinear` unused, evidence NRE on null length, `RegexOptions.Compiled`, doc "ignored for runs" | all applied | `Block_and_text_stand_in_rectangles_are_sized_but_never_closed_footprints`, `Rule_set_source_never_names_the_profile_folder` |

Live after the round: **55/55** (`run-aec-tools-live.ps1`, AutoCAD 2026, scene 24 entities; perf unchanged: layer query 68 ms, nearest 1000×1000 1.8 s, issues 125 ms).

## Known limitations (phase B)

- Classification is rule-driven: an entity on a layer named outside the conventions is `Unknown` until a project rule file names it (`ruleSet: "user"`). No geometry-only inference yet (e.g. "a 400×400 closed square on layer 0 is probably a column").
- `nearbyTextPattern` reads TEXT/MTEXT only (not attributes of nearby blocks); index capped at 5 000 texts.
- Axis relations (`aligned`/`parallel`/`perpendicular`) test every source against every target (no proximity gate by design); keep the sets focused on big drawings.
- `connected` is end-of-run to boundary; two footprints (column ↔ slab) relate through `touching`/`intersect`, not `connected`.
- Confidence numbers are rule weights, not probabilities.
