# Workflows — end-to-end call sequences (Revit 2026; ✅ = ran live 2026-09-12 on a 3-storey structural model, ◻ = same tools, sequence not driven live)

Every sequence starts with `get_revit_context` (opt-in, document, `isModifiable`, active view). Tool names are `mcp__hprebar-revit__<name>`; every length is mm; ids are `ElementId` numbers taken from an earlier result, never guessed.

## 1. Survey a model before touching it ✅

1. `get_revit_context {includeSelection: true}` → version, `docTitle`, `isFamily`/`isReadOnly`/`isModifiable`, `units.length`, `activeView`, what the user has selected.
2. `analyze_model_statistics {includeDetailedTypes: false, topCategories: 20}` → counts per category and per level (46 k elements answered in one call live); `includeDetailedTypes: true` when family/type breakdown matters.
3. `ai_element_filter {filterCategory: "OST_StructuralColumns", maxElements: 50}` → per element: type, family, level, location, bounding box, key parameters. Narrow with `filterVisibleInCurrentView: true`, `boundingBoxMin/Max` (mm) or `filterFamilySymbolId`; raise `maxElements` only after narrowing.
4. `get_current_view_info {}` then `get_current_view_elements {modelCategoryList: ["OST_Walls", "OST_Doors"], limit: 100}` when the question is "what is in this view".
5. Quantities: `get_material_quantities {categoryFilters: ["OST_Walls", "OST_Floors"]}` (m², m³ per material); rooms: `export_room_data {}`.
6. A question no seed answers → `search_tools {query: "…"}` → still nothing → `execute_revit_code {transaction: "none", code: "…"}` returning counts and `Take(n)` lists in mm.

## 2. Frame a building: levels → grids → walls / columns → floor ◻ (each seed ✅ in dryRun)

1. `create_level {data: [{name: "Level 2", elevation: 3500}, {name: "Level 3", elevation: 7000}], dryRun: true}` → existing names are skipped and reported; then without `dryRun`.
2. `create_grid {xCount: 5, xSpacing: 6000, yCount: 4, ySpacing: 4500, dryRun: true}` → `A…E` × `1…4`; names already used get a suffix; `xStartPosition`/`yStartPosition`/extents in mm.
3. `get_available_family_types {categoryList: ["OST_Walls"]}` → pick a `typeId` (basic wall types, width in mm); `["OST_StructuralColumns"]` → column `typeId`.
4. `create_line_based_element {data: [{category: "OST_Walls", typeId: <id>, locationLine: {p0: {x:0,y:0,z:0}, p1: {x:24000,y:0,z:0}}, height: 3500, baseLevel: 0}], dryRun: true}` → one item per wall; `thickness` instead of `typeId` picks/creates a wall type of that width. Beams: `category: "OST_StructuralFraming"` with the points' `z` as absolute elevation; MEP: `OST_DuctCurves` / `OST_PipeCurves` / `OST_Conduit` / `OST_CableTray`.
5. `create_point_based_element {data: [{category: "OST_StructuralColumns", typeId: <id>, locationPoint: {x:0,y:0,z:0}, baseLevel: 0, rotation: 0}], dryRun: true}` — one item per grid intersection (build the list from the grid spacing).
6. `create_surface_based_element {data: [{category: "OST_Floors", thickness: 150, baseLevel: 3500, boundary: {outerLoop: [{p0, p1}, …]}}], dryRun: true}` → the loop must be closed and planar; `structural: true` for a structural slab.
7. Every step: dryRun → show `value` (ids that would be created, warnings such as "typeId is not a … type — used …") + `changed.added` → user confirms → real run. Each real run is one Ctrl+Z entry `MCP: <tool>`.

## 3. Doors, rooms and annotation in a plan ◻ (`create_dimensions`, `tag_all_walls`, `operate_element` ✅; doors/rooms need families the test model lacked)

1. `get_current_view_info {}` → the active view must be a plan (tags/dimensions/rooms are view-dependent; `tag_all_walls` in a 3D view fails with Revit's own "3D view is not locked" error).
2. `get_available_family_types {categoryList: ["OST_Doors"]}` → door `typeId`; `create_point_based_element {data: [{category: "OST_Doors", typeId, locationPoint: {x, y, z}, baseLevel: 0, hostWallId?: <wall id>}], dryRun: true}` — without `hostWallId` the nearest wall within 1500 mm hosts it; `facingFlipped` flips the swing.
3. `create_room {data: [{name: "Office", number: "101", location: {x, y, z}, department: "Admin"}], dryRun: true}` → `enclosed: true/false` per room (area > 0); an unenclosed room means the walls do not close — check with `ai_element_filter` on the walls around the point.
4. `tag_all_rooms {}` / `tag_all_walls {}` → tags in the active view, already-tagged elements skipped; `tagTypeId` from `get_available_family_types {categoryList: ["OST_RoomTags"]}` / `["OST_WallTags"]`.
5. `create_dimensions {dimensions: [{startPoint: {x, y, z}, endPoint: {x, y, z}, elementIds: [gridA, gridB]}], dryRun: true}` → one dimension per item; without `elementIds` the tool auto-detects the nearest wall/grid/column at the two points (a 3D active view answers "pass elementIds"); `linePoint` moves the dimension line.

## 4. Visual QA: colour by parameter, isolate, then clean up ✅

1. `color_elements {categoryName: "OST_StructuralColumns", parameterName: "Type Name"}` → every distinct value gets a colour in the active view; the result is the legend `value → colour → count`. `parameterName: "Level"`, `"Mark"`, `"Comments"` for other checks; `customColors: [{r,g,b}, …]` for a fixed palette.
2. `ai_element_filter {filterCategory: "OST_StructuralColumns", maxElements: 200}` → the ids that look wrong (e.g. no Mark) → `operate_element {elementIds: [...], action: "Isolate"}` (temporary) or `"Highlight"` (red override) or `"Select"` so the user sees them in Revit.
3. Done → `operate_element {action: "ResetIsolate", elementIds: []}` clears temporary hide/isolate. Colour overrides stay until cleared through Visibility/Graphics or a script `view.SetElementOverrides(id, new OverrideGraphicSettings())` — tell the user.
4. Deleting what the review found: `delete_element {elementIds: [...], dryRun: true}` first — Revit removes dependents too (1 requested → 6 removed live); show the count, then the real run.

## 5. Bulk parameter edit with a script ✅ (this exact case became `set_mark_from_comments`)

1. `ai_element_filter {filterCategory: "OST_StructuralColumns", maxElements: 500}` → ids + current `Mark` / `Comments`.
2. `execute_revit_code {transaction: "auto", dryRun: true, label: "mark from comments", args: {category: "OST_StructuralColumns", maxLength: 100}, code: "…"}` — the script reads `args.Str("category")`, loops with `ct`, `log`s each change, returns `{updated, skipped}`; `rolledBack: true` + `changed.modified` show the scope (20 columns live).
3. Show the user the counts and a few `logs` lines → confirm → same call with `dryRun: false`. One undo entry `MCP: mark from comments`.
4. Verify with a read script (`transaction: "none"`) that reads the marks back — never assume the write from `changed` alone.

## 6. Toolify a script the user keeps asking for ✅

1. `get_run {runId}` (from the successful ad-hoc run) → code, the literals worth parameterising (line, value, suggested name), `args` keys already read, loop hints.
2. `propose_tool {name: "set_mark_from_comments", category: "Data", description, inputSchema: {properties: {category, prefix, maxLength, onlyEmpty}}, code: "…args.Str(\"category\")…", examples: [{title, args}, {title, args}], transaction: "auto", sourceRunId}` → the registry proves every schema key is read as `args.X("literal")` and the guard passes (draft, warnings listed).
3. `test_tool {name}` → runs both examples with dryRun inside Revit (2/2 live) → tested. `publish_tool {name}` → pending_approval + `%AppData%\HPRebar\McpServer\tools-library\_review\<name>.md`; `run_tool` refuses it until approved.
4. The user runs `HPRebar.Mcp.Server.exe registry approve <name> --by "<who>"` (also `list | pending | show | reject | deprecate | quarantine | restore | stats | export | import`) → `notifications/tools/list_changed` → `mcp__hprebar-revit__<name>` exists within 0.5 s; call it by name (dryRun first on a real model).
5. A published tool failing ≥ 5 runs with > 40 % failures is quarantined automatically and disappears from `tools/list` (5 strict runs on a model without rooms did it live); `manage_tool {name, action: "restore"}` → draft → fix with `propose_tool {newVersion: true}` (turn caller mistakes into `ArgumentException`, which never counts) → test → publish → approve again.
