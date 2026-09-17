# Workflows — end-to-end call sequences (Navisworks Manage 2026; ✅ = ran live in the phase-5 harness 2026-09-15, ◻ = same tools, sequence not driven live)

Every sequence starts with `get_navis_context` (opt-ins, `isBusy`, `hasClashModule`, document units). Tool names are `mcp__hprebar-navis__<name>`; every length is mm; item identity is `InstanceGuid`.

## 1. Survey a federated model, keep what you found ✅

1. `get_navis_context {includeSelection: true}` → `navis.models[]` with per-model units, counts of sets / viewpoints / clash tests, what the user has selected.
2. `get_model_info {includeRootItems: true}` → title, file, document units, one root item per appended model (the discipline files of the federation).
3. `summarize_by_category {}` → items per class (`PolyFace Mesh`, `Insert`, `Layer`, `File`…); `{groupBy: "Item.Type"}` or `{groupBy: "Element.Level"}` → counts per type / level; `complete: false` means the walk hit `maxItems` — narrow instead of raising it blindly.
4. `find_items_by_property {category: "Item", property: "Type", op: "contains", value: "Duct", maxResults: 50}` → `total` + items with `guid`, `path`, `bboxMm`; `op: "wildcard"` for `M-*`, `gt`/`lt` for numeric properties (value as text).
5. Keep the result as a live search set: `create_selection_set_from_search {name: "MCP ducts", category: "Item", property: "Type", op: "contains", value: "Duct", dryRun: true}` → `changed.added: 1`, `rolledBack: true` → without `dryRun` → one Undo entry `MCP: create_selection_set_from_search`; `list_selection_sets {includeCounts: true}` shows it with its count.
6. Mark the spot: `create_viewpoint {name: "MCP - L3 ducts", comment: "review with structure", author: "MCP"}` (camera = the user's current view; move the camera first by asking the user — the script API move is not undoable).

## 2. Inspect properties of what the user picked ✅

1. Ask the user to select the items in Navisworks, then `get_selected_item_properties {maxItems: 3}` → every category with values (lengths in mm); `{category: "Element"}` for one category only (each full item is ~5 KB).
2. Learn the exact category / property display names from that output (they are localised), then reuse them in `find_items_by_property` / sets / clash searches.
3. Many items, one property → `execute_navis_code {transaction: "none"}` with a `Search` + `PropertyCategories.FindPropertyByDisplayName(cat, prop)` (pattern in `script-contract.md`); return `{guid, value}` rows, capped.

## 3. Clash: read what exists, then run a new test (heavy) ✅

1. `get_navis_context` → `navis.hasClashModule` must be true (Manage); `heavyOperationsEnabled` tells whether a run can happen now; `isModified: true` → ask the user to save first.
2. `get_clash_results {}` → every test with status, last run, counts by status; `{testName: "Pipes vs Beams", maxResults: 200}` → results with `distanceMm`, `centerMm`, the two items. Read-only, no opt-in beyond execution.
3. New test: explain that the run is **heavy** (not undoable, not interruptible, may take minutes) and ask the user to tick **Allow heavy operations**; without it `create_and_run_clash_test` answers `HEAVY` naming the checkbox (that is not a failed run and never quarantines the seed).
4. `create_and_run_clash_test {name: "MCP pipes vs beams", a: {category: "Item", property: "Type", op: "contains", value: "Pipe"}, b: {category: "Item", property: "Type", op: "contains", value: "Beam"}, toleranceMm: 10, testType: "Hard", timeoutSeconds: 300}` → `status: Complete`, counts by status, elapsed ms (3 015 results in 107 ms on the harness model). `dryRun` is refused for it.
5. `get_clash_results {testName: "MCP pipes vs beams"}` → the results; for each clash worth a review, `create_viewpoint` after the user navigates there, or a script that copies `result.Center` into a comment.
6. Colour the clashing discipline for the review session: `override_color_by_search {category: "Item", property: "Type", op: "contains", value: "Pipe", r: 255, g: 0, b: 0, transparency: 0.3, dryRun: true}` → then real; `{…, reset: true}` removes the overrides afterwards (they are permanent and saved with the file otherwise).

## 4. Schedule check with TimeLiner ✅ (tasks added by script in the harness)

1. `get_timeliner_tasks {maxTasks: 500, includeSelection: true}` → flat list with nesting level, planned/actual dates, task type, attached selection display string; an empty list is a valid "no schedule".
2. Missing task → `execute_navis_code {transaction: "auto", dryRun: true, args: {name: "Pour L3"}, code: "var t = new TimelinerTask { DisplayName = args.Str(\"name\"), PlannedStartDate = DateTime.Today, PlannedEndDate = DateTime.Today.AddDays(5) }; doc.GetTimeliner().TaskAddCopy(t); return (int)doc.GetTimeliner().TaskTotalTasks();"}` → confirm → real run (one Undo entry). Attaching a selection set to a task is a script too (`task.Selection.CopyFrom(set...)`) — dryRun first.

## 5. Toolify a script the user keeps asking for ✅

1. Run it once with `execute_navis_code` (e.g. count items by `Item.Source File`) → `get_run {runId}` → code, literals worth parameterising, `hasLoops`.
2. `propose_tool {name: "count_items_by_source_file", category: "Report", description, inputSchema: {properties: {maxGroups}}, code: "…args.Int(\"maxGroups\", 50)…", examples: [{title, args}, {title, args}], transaction: "none"}` → draft (`host: navis`, `hostVersions: ["2026"]`); a heavy member in the code (`TestsRunAllTests`) is refused — heavy tools stay seed-only.
3. `test_tool {name}` (examples with dryRun) → tested → `publish_tool {name}` → pending_approval + `%AppData%\HPNavis\McpServer\tools-library\_review\<name>.md` naming `HPNavis.Mcp.Server.exe registry approve`; `run_tool` refuses it until then.
4. The user runs `HPNavis.Mcp.Server.exe registry approve <name> --by "<who>"` → `notifications/tools/list_changed` → `mcp__hprebar-navis__<name>` exists within 0.5 s → call it by name.
5. A published tool failing ≥ 5 runs with > 40 % failures is quarantined at once (a `First()` on an empty search did it live); `manage_tool {name, action: "restore"}` → `propose_tool {newVersion: true}` with `args.Require` + `ArgumentException` for caller mistakes → test → publish → approve; argument errors never count afterwards.
