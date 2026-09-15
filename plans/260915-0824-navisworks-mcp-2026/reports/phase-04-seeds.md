# Phase 4 — seed library report: 12 Navisworks seeds, compile-check on net48, registry live

**Date:** 2026-09-15 · **Model:** `Samples\gatehouse\gatehouse_pub.nwd` (2 006 items, 1 pre-existing clash test, no schedule) · **Runner:** `powershell.exe -File HPNavis/tools/harness/run-seeds-live.ps1` (Roamer + plugin, published exe, isolated registry root; `seeds-live.py` phases `normal` → tick heavy → `heavy`) · **Raw output:** `HPNavis/output/spike/seeds-live.log` (gitignored)

**Gate result:** `HPNavis.McpBridge.Tests` (net48) **124/124** — every seed compiles through `BridgeEntry.CreateScriptCompiler` (the bridge's exact imports/references/globals), passes `GuardProfile.Navis`, trips the heavy gate iff tagged heavy · `HPNavis.Mcp.Server.Tests` (net10) **49/49** — record shape, registry validator under the Navis profile, guard + declared-args parity, heavy tag ⇔ heavy member · live **18/18 + 2/2** (final run after the review fixes) — `tools/list` = **24**, every seed ran on the open model through `run_tool`/`test_tool`, a bad `gt` value surfaces as `ArgumentException`.

## The 12 seeds (`HPNavis.Mcp.Server/Registry/SeedLibrary/<Category>/<name>/`)

| # | Seed | Cat. | tx | Live result on gatehouse (runId from the isolated registry) |
|---|---|---|---|---|
| R1 | `get_model_info` | Model | none | title/units Millimeters/1 model/1 root, `includeRootItems` · runId 1 |
| R2 | `get_selected_item_properties` | Selection | none | after selecting one item: 1 item, 3 categories · **fix found live:** `VariantData.ToDisplayString()` throws `NotSupportedException` for anything that is not a display string → type-dispatching `Describe` (display/identifier string, length → mm, any double, int, bool, date, else `ToString()`) |
| R3 | `find_items_by_property` | Search | none | `Item.Type equals PolyFace Mesh` → total 718, 5 shown with name/class/path/guid/bboxMm · `Locations = DescendantsAndSelf`, `PruneBelowMatch = true` |
| R4 | `list_selection_sets` | Selection | none | 1 explicit set "glass", `includeCounts` → 11 items |
| R5 | `list_viewpoints` | Viewpoint | none | 7 viewpoints, positions in mm, comments |
| R6 | `get_clash_results` | Clash | none | shape `{testCount, tests[{counts…}], results[{test,…}]}` (summaries first so a truncated output keeps every count); pre-existing "Test 1": status New, 0 results — after H1: "MCP seed clash" Complete, 3 015 results, distances in mm |
| R7 | `get_timeliner_tasks` | Timeliner | none | empty schedule is valid; after the harness added a task via `execute_navis_code`: total 1, `PlannedOnly`, dates |
| R8 | `summarize_by_category` | Report | none | by class: 2 006 items visited, 5 groups (PolyFace Mesh 718, Block 614, Insert 614, Layer, File); `groupBy Item.Type` same keys · walk bounded by `Take(maxItems)` |
| W1 | `create_selection_set_from_search` | Selection | auto | `test_tool` (dry run) passed, `changed.added=1`, set count 1 → 1 afterwards |
| W2 | `create_viewpoint` | Viewpoint | auto | `test_tool` passed; real `run_tool` → guid, viewpoints 7 → 8 · runId 17 |
| W3 | `override_color_by_search` | Selection | auto | `test_tool` passed (`changed.modified=1`), overrides undone |
| H1 | `create_and_run_clash_test` | Clash | auto, **heavy**, 600 s | `test_tool` → refused (heavy cannot be dry-run: "The script calls heavy operations the user has not allowed…") · `run_tool` heavy OFF → `HEAVY` diagnostic · heavy ON → Hard test PolyFace Mesh (718) vs Block (614), **3 015 results in 96 ms**, `status Complete`, `rolledBack=false`, no "clamped" log (600 s honoured) · runId 21 |

Design points common to all: body ends in `return`, every input via `args.X("key", default)` (the analyzer proves schema ⇔ code), mm at the boundary through `units`, one shared `Condition(category, property, op, value)` local function for the search seeds (`equals` → `EqualValue(VariantData.FromDisplayString)`, `contains`/`wildcard` → `DisplayString…`, `gt`/`lt` → `CompareWith(NumericGreaterThan|NumericLessThan, VariantData.FromDouble)`), `ct.ThrowIfCancellationRequested()` inside walks, no `Transaction`, no `Descendants` without `PruneBelowMatch`/`Take`. Longest code 67 lines (H1).

## API facts pinned by reflection before writing (E8-ter)

`SearchConditionComparison` = `None, HasCategory, NotHasCategory, HasProperty, NotHasProperty, SameType, Equal, NotEqual, NumericLessThan, NumericLessThanOrEqual, NumericGreaterThanOrEqual, NumericGreaterThan, DisplayStringContains, DisplayStringWildcard, DateTimeWithinDay, DateTimeWithinWeek` · `Search.Locations : SearchLocations {None, Self, Descendants, DescendantsAndSelf}`, `PruneBelowMatch`, `FindAll(Document, bool)` · `ClashSelection.Selection : Selection` with `CopyFrom(IEnumerable<ModelItem>)` · `Comment(string body, CommentStatus status, string author)`, `CommentStatus {New, Active, Approved, Resolved}` · `DocumentSavedViewpoints.AddComment(SavedItem, Comment)` · `SavedViewpoint(Viewpoint)`, `Viewpoint.Position : Point3D` · `DocumentModels.OverridePermanentColor/OverridePermanentTransparency/ResetPermanentMaterials(IEnumerable<ModelItem>, …)` · `PropertyCategoryCollection.FindPropertyByDisplayName(string, string) : DataProperty` · `VariantData.Is*/To*/From*` (see R2) · `ClashResult.Status/Distance/Item1/Item2/Center`, `ClashResultStatus {New, Active, Reviewed, Approved, Resolved}`, `ClashTestStatus {New, Old, Partial, Complete}`, `ClashTest.LastRun : DateTime?` · `DocumentTimeliner.Tasks : SavedItemCollection`, `TaskAddCopy(TimelinerTask)`, `TaskTotalTasks() : uint`, `TimelinerTask.PlannedStartDate/PlannedEndDate/ActualStartDate/ActualEndDate : DateTime?`, `TaskStatus`, `SimulationTaskTypeName`, `Selection : TimelinerSelection {IsClear, DisplayString}`.

## Tests

- `HPNavis.McpBridge.Tests/SeedLibraryCompileTests.cs` (net48): seeds read from the source tree (found upward from the test bin), compiled with `BridgeEntry.CreateScriptCompiler(64)` — new public factory so the test and Roamer share one configuration; guard; heavy gate OFF/ON per seed; exactly one heavy seed; Descendants bound; real `SearchCondition` API. 12 × 5 theories + 2 facts.
- `HPNavis.Mcp.Server.Tests/SeedLibraryStructureTests.cs` (net10): 12/8/4/1 counts; record well-formed (host `navis`, versions `["2026"]`, categories ∈ profile, timeout 30–600 with heavy = 600 and non-heavy ≤ 120, `destructive ⇔ transaction ≠ none`, "Read-only" in read-only descriptions, "HEAVY" + checkbox in the heavy one, ≤ 80 lines, ≥ 2 differing examples whose keys exist in the schema, no double-escaped strings); `ToolValidator.Validate` under `NavisHostProfile`; guard + `ScriptAnalyzer` (declared args ⇔ read args, no transaction).
- Fix found by the structure test: a local `Channel(key, fallback)` hid `args.Int("r")` from the analyzer → the colour seed reads `args.Int("r"|"g"|"b")` directly.

## Harness

`run-seeds-live.ps1` = `run-server-smoke.ps1` skeleton + two Python phases on one isolated registry root (`seeds-live.py`): the normal phase picks the two most common `Item.Type` values from `summarize_by_category` and uses them for the search seeds and the clash pair, so the script does not depend on gatehouse's naming; the heavy phase runs after UIA ticks "Allow heavy operations" and reads the pair back from `output/spike/seeds-types.json`.

## Not covered / deferred

- `_seeds.json` upgrade (edit one seed → reinstall) — engine behaviour shared with Revit/AutoCAD (`SeedInstaller`, covered by `HPRebar.Mcp.Server.Core.Tests`), not re-driven for Navis.
- Live data for `R2` categories with non-string values other than lengths (Int32/Boolean/DateTime paths of `Describe`) — exercised by type only through the compile-check.
- `propose_tool` of a heavy tool — out of MVP by design (analyze runs with heavy OFF → validator refuses).
- Revit/AutoCAD `tools/list`: `McpShared/` untouched in phase 4 (`git diff 3ef5c36 -- McpShared` empty).

## Code review (same day)

`reports/code-review-phase-04.md`: 8/10, 1 High + 4 Medium + 9 Low, no Critical; ~95 API members re-verified by the reviewer through reflection. Fixed before commit: Hi1 (number parsing → `ArgumentException`), M1–M4, L1, L7, L9; kept with reasons L2–L4, L6, L8; deferred L5 (area/volume units). Final: 124 + 49 tests, live 18/18 + 2/2.
