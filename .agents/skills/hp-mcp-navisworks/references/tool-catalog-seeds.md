# HPNavis MCP — tool catalog: the 12 seeds (model info, property search, selection sets, selected item properties, colour override, viewpoints, clash results and clash runs, TimeLiner tasks, category summary)

Generated from `tools/list` of `HPNavis.Mcp.Server.exe` (24 tools in all) on an isolated registry — the surface a fresh install shows; the user's own registry may add approved tools. Names are `mcp__hprebar-navis__<name>` in Claude Code. `REQ` = required. Every seed takes and reports **millimetres** (the API itself works in the document's units — `units` converts); item ids are instance-guid hashes from earlier results. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

## Model

### `get_model_info` — Get model info

*readOnlyHint.* Overview of the open Navisworks document: title, file, document units, modified flag, the appended models (file, source file, units) and the number of root items; optionally the root item names. Call it first so later scripts use the right units. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `includeRootItems` | boolean | false | Also list the display name and class of each root item (one per appended model, usually) |

## Search

### `find_items_by_property` — Find items by property

*readOnlyHint.* Runs a Navisworks Search over the whole model for items whose property matches (equals / contains / wildcard / gt / lt) and returns up to maxResults of them with name, class, path, guid and bounding box in millimetres, plus the total hit count. Read-only — use create_selection_set_from_search to keep the result.

| arg | type | default | description |
|---|---|---|---|
| `category` REQ | string |  | Property category display name as shown in the Properties window, e.g. Item, Element, Revit Type |
| `property` REQ | string |  | Property display name inside that category, e.g. Name, Type, Level |
| `op` | string equals \| contains \| wildcard \| gt \| lt | "equals" | equals compares the display string; contains / wildcard (* and ?) match text; gt / lt compare numbers |
| `value` REQ | string |  | Value to compare with (numbers as text for gt / lt) |
| `maxResults` | integer | 200 | How many matching items to return (the total is always reported) |

## Selection

### `create_selection_set_from_search` — Create a search set

*destructiveHint.* Saves a search set (a live Navisworks Search: category, property, operator, value) under the given name in Sets; returns its guid and how many items it resolves to right now (counted up to 1000). One Undo entry; dryRun undoes it again.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Display name of the new set |
| `category` REQ | string |  | Property category display name as shown in the Properties window, e.g. Item, Element, Revit Type |
| `property` REQ | string |  | Property display name inside that category, e.g. Name, Type, Level |
| `op` | string equals \| contains \| wildcard \| gt \| lt | "equals" | equals compares the display string; contains / wildcard (* and ?) match text; gt / lt compare numbers |
| `value` REQ | string |  | Value to compare with (numbers as text for gt / lt) |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `get_selected_item_properties` — Get properties of the selected items

*readOnlyHint.* Property categories and values of the items currently selected in Navisworks (max maxItems, default 5), optionally one category only. Length values are converted to millimetres; everything else is the display string. Read-only; select items in Navisworks first.

| arg | type | default | description |
|---|---|---|---|
| `maxItems` | integer | 5 | How many selected items to describe (every category of an item is ~5 KB of output) |
| `category` | string |  | Only this property category (display name, e.g. Item, Element); empty = every category |

### `list_selection_sets` — List selection and search sets

*readOnlyHint.* Flat list of every saved selection set and search set (folders included as path segments): name, kind (search / explicit / folder), guid and folder path; optionally how many items each set currently resolves to. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `includeCounts` | boolean | false | Resolve each set against the model and report its item count (slower on big models) |

### `override_color_by_search` — Colour items found by a search

*destructiveHint.* Finds items by property (like find_items_by_property) and applies a permanent colour override (r, g, b 0–255) and optionally a transparency (0–1) to them; reset=true removes the overrides instead. Permanent overrides are saved with the file and undoable — one Undo entry; dryRun undoes it again.

| arg | type | default | description |
|---|---|---|---|
| `category` REQ | string |  | Property category display name as shown in the Properties window, e.g. Item, Element, Revit Type |
| `property` REQ | string |  | Property display name inside that category, e.g. Name, Type, Level |
| `op` | string equals \| contains \| wildcard \| gt \| lt | "equals" | equals compares the display string; contains / wildcard (* and ?) match text; gt / lt compare numbers |
| `value` REQ | string |  | Value to compare with (numbers as text for gt / lt) |
| `r` | integer | 255 | Red 0–255 |
| `g` | integer | 0 | Green 0–255 |
| `b` | integer | 0 | Blue 0–255 |
| `transparency` | number |  | Optional transparency 0 (opaque) – 1 (invisible) |
| `reset` | boolean | false | Remove the permanent colour/transparency overrides of the matching items instead of colouring them |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

## Viewpoint

### `create_viewpoint` — Save the current view as a viewpoint

*destructiveHint.* Saves the current camera as a named viewpoint (optionally with a first comment and its author) and returns its guid and position in millimetres. One Undo entry; dryRun undoes it again.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Display name of the viewpoint |
| `comment` | string |  | Optional comment text attached to the viewpoint |
| `author` | string | "MCP" | Author recorded on the comment |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `list_viewpoints` — List saved viewpoints

*readOnlyHint.* Flat list of the saved viewpoints (folders as path segments): name, guid, camera position in millimetres, number of comments and optionally the comment texts. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `includeComments` | boolean | false | Include each comment's author, status and text |

## Clash

### `create_and_run_clash_test` — Create and run a clash test

*destructiveHint.* HEAVY: creates a Clash Detective test between two searches (a and b, each category/property/op/value), with a tolerance in millimetres, and runs it. Needs 'Allow heavy operations' ticked in the bridge window; the run cannot be interrupted or undone (the test definition itself is undoable), may take minutes on big models — save the file first. Returns result counts by status and the elapsed time; read results with get_clash_results.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Display name of the new clash test |
| `toleranceMm` | number | 0 | Clash tolerance in millimetres (0 = any intersection) |
| `testType` | string Hard \| HardConservative \| Clearance \| Duplicate | "Hard" | Clash test type |
| `a` REQ | object |  | Selection A as a search: {category, property, op, value} |
| `b` REQ | object |  | Selection B as a search: {category, property, op, value} |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

`a` items:

| arg | type | default | description |
|---|---|---|---|
| `category` REQ | string |  | Property category display name as shown in the Properties window, e.g. Item, Element, Revit Type |
| `property` REQ | string |  | Property display name inside that category, e.g. Name, Type, Level |
| `op` | string equals \| contains \| wildcard \| gt \| lt | "equals" | equals compares the display string; contains / wildcard (* and ?) match text; gt / lt compare numbers |
| `value` REQ | string |  | Value to compare with (numbers as text for gt / lt) |

`b` items:

| arg | type | default | description |
|---|---|---|---|
| `category` REQ | string |  | Property category display name as shown in the Properties window, e.g. Item, Element, Revit Type |
| `property` REQ | string |  | Property display name inside that category, e.g. Name, Type, Level |
| `op` | string equals \| contains \| wildcard \| gt \| lt | "equals" | equals compares the display string; contains / wildcard (* and ?) match text; gt / lt compare numbers |
| `value` REQ | string |  | Value to compare with (numbers as text for gt / lt) |

### `get_clash_results` — Get clash test results

*readOnlyHint.* Clash Detective tests — status, last run, result counts by status for every test (tests[]) — followed by up to maxResults results per test (results[]: test, name, status, distance and center in millimetres, the two clashing items). Optionally one test by name. Needs Navisworks Manage (hasClashModule). Read-only — run tests with create_and_run_clash_test.

| arg | type | default | description |
|---|---|---|---|
| `testName` | string |  | Only the test with this display name; empty = every test |
| `maxResults` | integer | 100 | Results listed per test (counts are always complete) |

## Timeliner

### `get_timeliner_tasks` — Get TimeLiner tasks

*readOnlyHint.* Flat list of the TimeLiner tasks with their nesting level, planned and actual dates, status, task type and optionally the attached selection. An empty list is a valid answer (no schedule). Read-only.

| arg | type | default | description |
|---|---|---|---|
| `maxTasks` | integer | 500 | Stop after this many tasks |
| `includeSelection` | boolean | false | Include the display string of each task's attached selection |

## Report

### `summarize_by_category` — Summarize items by category or property

*readOnlyHint.* Counts model items grouped by class (default) or by the value of one property given as 'Category.Property' (e.g. Item.Type, Element.Level). Walks at most maxItems items (default 200000) and returns up to maxGroups groups sorted by count. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `groupBy` | string |  | 'Category.Property' display names to group by, e.g. Item.Type; empty groups by item class |
| `maxGroups` | integer | 200 | Groups returned (largest first) |
| `maxItems` | integer | 200000 | Items visited before the walk stops (the answer says whether it was complete) |

