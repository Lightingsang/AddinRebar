# Phase D — QA/QC: implementation + live verification report (2026-09-16)

## Implemented

| Piece | Files | Notes |
|---|---|---|
| Standards rules (data) | `HPAutoCad.Aec/Standards/CadStandardsRuleSet.cs` (JSON model: layerNaming regex + exempt wildcards, entityLayer[] {types, layers, severity, message}, layerZero, overrides {color, linetype, lineweight, exemptTypes, exemptLayers}, textStyles / dimStyles allowed lists, textHeights {allowedMm, toleranceMm, space}, blockNaming, unusedLayers; validated on load — regexes, severities, misspelt keys refused), `Rules/cad-standards.default.json` (embedded, AIA/NCS-shaped and permissive: `^[A-Z]{1,2}-[A-Z0-9]{2,6}(-[A-Z0-9]{1,6}){0,4}$`, text/dimension/hatch layer expectations incl. Vietnamese stems, hatches/blocks and `HP-MCP-*` exempt from override checks), `Model/RuleFileLocator.cs` (shared with classification: `default` / `user` / plain file name in the server's `rules` folder, traversal refused, profile path never echoed) | every section optional |
| Checker (pure) | `Standards/CadStandardsChecker.cs` over `AecEntityRecord`s + `Standards/DrawingTables` (layers, text/dim styles, block definitions) → `AuditIssue`s `STD-nnnn` in the stable order; `unused_layer` only when the whole drawing was examined | `Cad/DrawingTablesReader.cs` reads the tables |
| Shared issue model | `Issues/AuditIssue.cs` (issueId, category, type, severity, handles, locationMm, valueMm, description, suggestedAction, layer, rule; `Ordered` = critical → warning → info, category, type, first handle, layer, description; `AtLeast`) — geometry issues map through `From` | |
| Audit aggregate | `Cad/AuditService.cs` (one query; sections geometry + standards; unused_layer skipped with a warning on a subset) | |
| Markup | `Cad/IssueMarkupService.cs` (issue objects as returned by the audit tools → circle / rectangle / revcloud around locationMm or the handles' extents (radius grows to cover them) + MLeader `<id>: <description>` on a markup layer created on demand, coloured by severity 1/2/4; two-phase: every issue resolved before the first write; locked/frozen markup layer refused) | `AnnotationService.BuildMLeader` shared |
| Facade + seeds | `AecTools.Audit.cs` (`MaxIssueLimit` 100 since the review round); seeds `Audit/cad_standards_check`, `Audit/audit_aec_drawing` (`none`), `Annotation/create_issue_markup` (`auto`) | server 40 tools |
| Record | `AecEntityRecord.Style` (text/dim style) + `TextHeightMm`, read by `EntityShapeReader` | |
| Tests | `CadStandardsTests` (19: default set, 11-row layer-naming theory, entity/override/style/height/table checks, stable ids, rule validation, audit ordering) → `HPAutoCad.Aec.Tests` 141; `SeedLibraryTests` 28 seeds → `HPAutoCad.Mcp.Server.Tests` 153 | |
| Harness | `aec-tools-live.py` step T (8 checks; scene +3 standards defects + a badly named unused layer → 27 entities) and `aec-edit-tools-live.py` step K (5 checks) | |

## Build / tests

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings; bundle deployed |
| `HPAutoCad.Aec.Tests` | 141/141 |
| `HPAutoCad.Mcp.Server.Tests` | 153/153 (28 seeds) |

## Live (AutoCAD 2026)

`run-aec-tools-live.ps1` **63/63** (run 1: 62/63 — a harness expectation asked for the `unused_layer skipped` warning on a call that had not requested `unused_layer`; fixed the check, not the code) · `run-aec-edit-tools-live.ps1` **67/67** (run 1: 66/67 — summary mode carries no `color`; the check now asks for it).

| Step | Verified |
|---|---|
| T standards | whole drawing: `layer_naming` walls_old, `entity_layer` (text on A-WALL), `layer_zero`, `color_override`, `unused_layer`; ids `STD-0001…` identical on a second run; `checks` subset + unknown check warned; `unused_layer` on a filtered subset skipped with a warning; unknown rule set → ArgumentException without the profile path |
| T audit | sections geometry + standards, GEO and STD ids, severity-ordered, `bySeverity` sums to `count`; `minSeverity: warning` drops the 3 info issues and counts them in `belowMinSeverity`; `offset 3 limit 3` returns items 4–6 of the same order; `sections: [geometry]` → no STD issues, no ruleSet |
| K markup | 3 audit issues → 3 revclouds + 3 leaders on the created layer `HP-MCP-ISSUES`, colours 1/2 by severity; rectangle around two handles without a leader (bounds cover both, cyan); dryRun rolled back; atomic refusal for an issue with neither location nor handles (nothing drawn); locked markup layer → ArgumentException |


## Review round (2026-09-16 → 17, `plans/reports/code-review-2026-09-16-aec-phase-d.md`, 5.5/10 → fixed)

| # | Finding | Fix | Pinned by |
|---|---|---|---|
| H1 | empty exemption lists exempted everything (`EntityFilter.Matches` empty = match all) | `CadStandardsChecker.Exempts(list, value) => list.Count > 0 && Matches` at the five sites | `CadStandardsReviewTests.A_rule_without_exemption_lists_still_checks_everything` |
| H2 | 150 issues × entity_layer message = 72 KB > 64 KB cap | `MaxIssueLimit` 100 (schema maximum follows), pattern list named once in the summary, not per item | `A_full_page_of_the_longest_issues_stays_under_the_result_cap` (< 60 000 B) |
| H3 | whole-drawing audit merged every space into one geometry pass (title block on two layouts = `duplicate`) | `GeometryIssueDetector.Detect` groups records by `Space` (ids renumbered across the run) | `Geometry_issues_never_cross_spaces` |
| H4 | markup drawn in the requested space whatever space the issue's entities live in | `space: "auto"` default = the entities' space via the first resolvable handle; an explicit space warns when it disagrees | live K: a Layout1 `layer_zero` finding drawn on Layout1, `summary.spaces == ["Layout1"]` |
| M1 | a filter with only `space` was replaced by the whole drawing | `space` alone is a filter (`wholeDrawing: false`, `unused_layer` skipped with the warning) | live T/K `filter:{space:"Layout1"}` |
| M2 | location + handles → marker grew to the handles' union | radius stays `radiusMm` at `locationMm`; grows to the handles only without a location (`sizedByHandles`) | live K `endpoint_gap` → `radiusMm 500` |
| M3 / M4 | xref-dependent blocks reported by `block_naming`; hidden system layers and block-content layers reported | `IsDependent` (blocks + layers), `LayerTableRecord.IsHidden`, `DrawingTables.LayersUsedInBlocks` (non-xref, non-layout BTRs scanned once) | `Dependent_and_hidden_symbols_and_block_content_layers_are_never_reported` |
| M5 | default names too strict (`S-COLUMNS`, `A-ANNO-TEXT-0.25`), `*KT*` matched the KT- discipline prefix, `*CHU*` matched `A-CHUNG`, TEXT on 0 double-counted | pattern `^[A-Z]{1,2}-[A-Z0-9]{2,12}(-[A-Z0-9.]{1,12}){0,5}$`; stems anchored (`*-KT`, `*-KT-*`, `CHU-*`, `*-CHU`…); `layer_zero` suppresses `entity_layer` on the same entity | naming + entity-layer theories in `CadStandardsReviewTests` |
| M6 | `RegexMatchTimeoutException` = engine failure → quarantine after 5 calls | caught → `ArgumentException` naming `layerNaming/blockNaming.pattern` | `A_catastrophic_pattern_is_the_rule_files_fault_not_an_engine_failure` |
| M7 | 2 000 geometry findings cut in detection order, `truncated: false` | `AuditService.Outcome.GeometryCapped` → `truncated: true` + warning "narrow the filter" | `AecTools.Audit` |
| M8 / M9 | target space opened for write before phase 1; layer name never validated; layer created when nothing is drawn | spaces opened `ForWrite` in phase 2 only; `EditContext.RefuseSymbolName` on the layer; `EnsureLayer` after the "nothing drawable" exit | live K atomic refusal → `run.changed` 0 |
| L2, L3, L6, L7, L8, L11, L12, L13 | `textHeights.space` unvalidated; a disabled requested check silent; bulge comment; locked layer as exception, off layer silent; malformed `locationMm` swallowed; `style`/`textHeight` not queryable, `ARC_DIMENSION` skipped; paper heights through INSUNITS; table findings refused the whole call | `space` validated (`all|paper|model`); "not enabled by rule set" warning; 106° comment; `Refused(LAYER_LOCKED/LAYER_FROZEN)` + off warning; `locationMm` errors surfaced; `query_entities` projects `style` + `textHeight`, every dimension flavour in the checker + default rules; noted in the tool description; table findings skipped with a warning (`skipped` in the summary), unresolvable handles are the error | `CadStandardsReviewTests`, seed `tool.json`, live K |

Decisions (recommended by the review, taken): M2 — a location is the finding, so the marker stays at `radiusMm` around it; H4 — each issue is drawn in the space of its own entities by default (`space: "auto"`).

Left as documented: M10 (a standards-only run still reads every shape; `maxCandidates` bounds it), L1 (ByBlock reported with a note), L4 (regex compiled per load; rule set re-read per call), L5 (order key is ordinal handle text; GEO ids are detection order), L9 (`radiusMm` / `textHeightMm` are drawing units of the target space), L10 (unused table columns kept for later checks).

After the round: `HPAutoCad.Aec.Tests` 170/170 (`CadStandardsReviewTests` 8), `HPAutoCad.Mcp.Server.Tests` 192/192; live `run-aec-tools-live.ps1` 68/68, `run-aec-edit-tools-live.ps1` 75/75 (2026-09-17, scene 42 entities incl. Layout1 text + grid/slab/openings added for phase E).

## Known limitations (phase D)

- The default layer-naming pattern is a starting point: names with a lower-case or a dot segment (`A-ANNO-TEXT-0.25`) are flagged until the project file relaxes the regex.
- `text_height` is only meaningful with an allowed list (empty by default) and defaults to paper space.
- `unused_layer` counts entities in model + paper space and the layers block definitions draw on (`LayersUsedInBlocks`); xref-dependent and hidden system layers are never reported.
- Revision clouds are polylines with 106° arcs (bulge −0.5), not AutoCAD's REVCLOUD object style.
- A standards-only run still reads every entity's shape (M10): bound it with `maxCandidates` / a filter on very large drawings.
