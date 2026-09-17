# HPAutoCad MCP — tool catalog: drawing + data (context, entity query, spatial query, batch create / update, hatches, xrefs, layers, layouts, selection)

Generated from `tools/list` of `HPAutoCad.Mcp.Server.exe` (62 tools in all) on an isolated registry — the surface a fresh install shows. Names are `mcp__hprebar-autocad__<name>` in the host coding agent. `REQ` = required; every length is millimetres, points are `{x, y}` objects in mm; handles are hex strings. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

## Drawing

### `create_entities_batch` — Create entities in a batch

*destructiveHint.* Creates up to 200 entities in one call. Each item has a type — line {start, end}, polyline {points[{x, y, bulge?}], closed}, circle {center, radiusMm}, arc {center, radiusMm, startAngleDeg, endAngleDeg}, point {position}, text {text, position, heightMm, rotationDeg?, style?}, mtext {text, position, heightMm, widthMm?, rotationDeg?, style?}, blockReference {blockName, position, scale?, rotationDeg?, attributes{tag: value}?}, dimension {kind linear|aligned, p1, p2, dimLinePoint, rotationDeg?, dimStyle?, textOverride?} — plus optional layer (must exist and be unlocked; frozen → warning), colorIndex (0 ByBlock, 1–255, 256 ByLayer) or color (#RRGGBB|ByLayer|ByBlock), linetype (loaded), lineweight (hundredths of mm), visible. All coordinates and sizes in millimetres, angles in degrees. Returns the edit envelope {success, createdCount, modifiedCount, deletedCount, affectedHandles, items[{index, ok, handle, type, changed, error}], warnings, errors[{code, message, handle}]}. items[i] answers for request.items[i] with the new handle; unknown attribute tags of a blockReference are warnings. Side effects: modifies the current drawing inside one undo entry 'MCP: <label>' (U reverts it); dryRun on the request rolls everything back and still reports what would change. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `items` REQ | array<object> |  | Entities to create, in the order their handles are returned |
| `space` | string | "current" | Where new entities go: current (default), model, or a layout name |
| `atomic` | boolean | true | true (default): every item is validated first (layers, styles, keys per entity type, attribute tags) and one invalid item refuses the whole batch — success false, zero counts, every refusal in items[i].error, nothing written and nothing opened for write; a failure while writing aborts the transaction. false: invalid items are listed per item and the rest is written; an item that fails midway reports what it had changed |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `draw_circle` — Draw a circle

*destructiveHint.* Draws a circle from a centre point and radius given in millimetres, in the current space, optionally on an existing layer. Returns handle, radius and area.

| arg | type | default | description |
|---|---|---|---|
| `center` REQ | object |  | Centre in millimetres |
| `radiusMm` REQ | number |  | Radius in millimetres (> 0) |
| `layer` | string |  | Existing layer name; default: the current layer |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `draw_polyline` — Draw a polyline

*destructiveHint.* Draws a lightweight polyline through the given points (millimetres, {x, y} each, at least 2 — two points still give an LWPOLYLINE, not a LINE) in the current space, optionally closed, on an existing layer and with an ACI colour. Returns handle, length in mm and vertex count.

| arg | type | default | description |
|---|---|---|---|
| `points` REQ | array<object> |  | Vertices in millimetres, in order |
| `closed` | boolean | false | Close the polyline back to the first vertex |
| `layer` | string |  | Existing layer name; default: the current layer |
| `colorIndex` | integer |  | ACI colour index 0–256; default: ByLayer |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `get_drawing_context` — Drawing context

*readOnlyHint.* Describes the open drawing before any analysis or edit: file name and path, AutoCAD version, drawing units (INSUNITS, measurement, mm per unit), active space and layout, current layer/color/linetype, UCS origin and axes, model and paper extents in mm, annotation scale, text and dimension styles, linetype scale, the current view, and counts of entities, layers, block definitions, xrefs, layouts and styles; optionally the layout list and the layer table. Use this first when the user asks about the drawing, its units, what is open, or before running spatial, classification or editing tools. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `includeLayouts` | boolean | true | List the layouts with their entity counts |
| `includeLayers` | boolean | false | List the layer table (name, on/frozen/locked, color, linetype, lineweight), capped at 200 |

### `manage_hatches` — Manage hatches

*destructiveHint.* Hatches, chosen by op. create (hatch {boundaryHandles [closed curves: first outer, others islands] | points [polygon in mm] | seedPoint {x, y} → the smallest closed entity around it, pattern (default SOLID; ANSI31, AR-CONC… from acad.pat/acadiso.pat), patternType predefined|userDefined|custom, scale, angleDeg, hatchStyle normal|outer|ignore, associative, layer, colorIndex|color}); a boundary that is not closed (for AutoCAD or within the point tolerance) is refused with NOT_CLOSED, a wrong layer/property with LAYER_LOCKED / INVALID_ARGUMENT — nothing is created; a pattern AutoCAD rejects aborts the run. associative: true makes the hatch follow its boundary entities. update (handles[0] + set {pattern, patternType, scale, angleDeg, hatchStyle, layer, colorIndex|color, linetype, lineweight, visible} — every key validated first, an unknown key or bad value refuses the whole update), delete (handles — hatches only), detectBoundary (seedPoint → closed entities containing the point, innermost first: handle, type, layer, areaMm2; found geometrically, so it works outside a command). Read op returns the analysis envelope, write ops the edit envelope with the hatch's area in mm². Side effects: modifies the current drawing inside one undo entry 'MCP: <label>' (U reverts it); dryRun on the request rolls everything back and still reports what would change. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `op` REQ | string create|update|delete|detectBoundary |  |  |
| `hatch` | object |  | create: the hatch to add (one of boundaryHandles, points, seedPoint) |
| `handles` | array<string> |  | Entity handles (hex, as every AEC tool reports them) |
| `set` | object |  | update: the change |
| `seedPoint` | object |  |  |
| `space` | string | "current" | Where new entities go: current (default), model, or a layout name |
| `atomic` | boolean | true | delete only: true (default): every item is validated first (layers, styles, keys per entity type, attribute tags) and one invalid item refuses the whole batch — success false, zero counts, every refusal in items[i].error, nothing written and nothing opened for write; a failure while writing aborts the transaction. false: invalid items are listed per item and the rest is written; an item that fails midway reports what it had changed |
| `limit` | integer | 20 | detectBoundary: containing entities returned |
| `maxCandidates` | integer | 5000 |  |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `update_entities_batch` — Update entities in a batch

*destructiveHint.* Changes existing entities by handle. Give items [{handle, set}] or handles [] + one set applied to all. set keys: layer, colorIndex|color, linetype, lineweight, visible, text (TEXT/MTEXT/dimension override), heightMm, rotationDeg (text/blocks), scale (blocks), style (text style), dimStyle, textOverride, attributes {tag: value} (blocks), geometry per type (line {start, end}; circle/arc {center, radiusMm, startAngleDeg, endAngleDeg}; polyline {points, closed}; text/mtext/block/point {position}), and the transforms move {dx, dy}, rotate {angleDeg, about?}, scaleBy {factor, about?} that work on any entity. Millimetres and degrees. Entities on locked or frozen layers are refused with LAYER_LOCKED / LAYER_FROZEN (an attribute on a locked layer refuses its block too), erased handles with ERASED, entities inside a block definition and keys that do not apply to the entity type with UNSUPPORTED_ENTITY, unknown keys with INVALID_ARGUMENT. set.layer may move an entity onto a locked or frozen layer (warned): it is then not editable through this tool until the layer is unlocked/thawed. Up to 200 items per call. Returns the edit envelope {success, createdCount, modifiedCount, deletedCount, affectedHandles, items[{index, ok, handle, type, changed, error}], warnings, errors[{code, message, handle}]}. items[i].changed lists what was applied. Side effects: modifies the current drawing inside one undo entry 'MCP: <label>' (U reverts it); dryRun on the request rolls everything back and still reports what would change. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `items` | array<object> |  | Per-entity changes |
| `handles` | array<string> |  | Entity handles (hex, as every AEC tool reports them) |
| `set` | object |  | One change applied to every handle in handles[] (ignored when items[] is given) |
| `atomic` | boolean | true | true (default): every item is validated first (layers, styles, keys per entity type, attribute tags) and one invalid item refuses the whole batch — success false, zero counts, every refusal in items[i].error, nothing written and nothing opened for write; a failure while writing aborts the transaction. false: invalid items are listed per item and the rest is written; an item that fails midway reports what it had changed |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

## Data

### `get_drawing_info` — Get drawing info

*readOnlyHint.* Summary of the active drawing: file name, INSUNITS and measurement system, millimetres per drawing unit, model-space extents (null until the drawing has any; as of the last regen/save) in mm, current layout and layer, layer count, and optionally the number of model-space entities per DXF type. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `countByType` | boolean | true | Count model-space entities per DXF type |

### `get_entities` — Find entities

*readOnlyHint.* Selects entities by DXF type (LINE, LWPOLYLINE, CIRCLE, ARC, INSERT, TEXT, MTEXT, DIMENSION, HATCH…) and/or layer, in model space or the current space, and returns handle, type, layer and bounding box in millimetres for up to limit of them. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `type` | string |  | DXF entity name such as LINE, LWPOLYLINE, CIRCLE, INSERT, TEXT, MTEXT, DIMENSION; omit for any type |
| `layer` | string |  | Exact layer name; omit for any layer |
| `space` | string | "model" | model (default), current (the active layout), or all |
| `limit` | integer | 200 | Maximum entities to return (1–2000) |

### `manage_xrefs` — Manage external references

*destructiveHint.* External references (xrefs), chosen by op. list (namePattern?) and resolveStatus (re-resolves paths first) return the analysis envelope with name, path, foundPath, found, status (Resolved|Unloaded|Unreferenced|FileNotFound|Unresolved), loaded, overlay, referenceCount, nestedIn. Write ops return the edit envelope: attach (attach {path — fully qualified .dwg that exists (<host-path>), name? (valid block name), position, overlay?, scale?, rotationDeg?, layer?} — reads the file from disk, saves the path as given and inserts one reference), detach (names — all must be xrefs; erases every reference of each and counts them in deletedCount; a nested xref aborts the run), reload, unload (names), bind (names, insertBind? — only resolved, loaded xrefs; one refused xref refuses the whole bind). modifiedCount counts block-table records for reload/unload/bind; affectedHandles holds entity handles only. Millimetres at the boundary. Side effects: modifies the current drawing inside one undo entry 'MCP: <label>' (U reverts it); dryRun on the request rolls everything back and still reports what would change. attach/detach/bind change the block table as well as the entities. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `op` REQ | string list|attach|detach|reload|unload|bind|resolveStatus |  |  |
| `names` | array<string> |  | detach / reload / unload / bind: xref names as list reports them |
| `namePattern` | string |  | list: wildcard on the xref name |
| `attach` | object |  | attach: what to attach and where |
| `insertBind` | boolean | false | bind: merge names INSERT-style instead of prefixing them with the xref name |
| `space` | string | "current" | Where new entities go: current (default), model, or a layout name |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `query_entities` — Query entities

*readOnlyHint.* Finds entities by type, layer (wildcards), color, linetype, block name, text content, handle list, visibility and space, and returns them page by page (handle, type, layer, space, bounding box in mm, length, text, block name; detail mode adds tessellated geometry, area, attributes, position, color, linetype). Use this when the user asks what is on a layer, how many of something exist, to find blocks or text, or to get handles for other tools. Prefer a layer/type filter on big drawings; page with offset/limit. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to consider. Types, layers and block names accept AutoCAD wildcards (* and ?) and comma lists; handles short-circuit everything else. |
| `limit` | integer | 100 | Entities per page |
| `offset` | integer | 0 | Skip this many matches (paging) |
| `mode` | string summary|detail | "summary" | summary = identity + bounds + text/block/length; detail = everything incl. geometry vertices (capped at 64 per entity and 20 entities per page) |
| `properties` | array<string> |  | Return exactly these properties (plus handle/type/layer) instead of the mode's set |
| `maxCandidates` | integer | 5000 | Stop scanning after this many candidate entities (truncated=true in the result) |

### `query_entities_spatial` — Spatial query

*readOnlyHint.* Answers spatial questions between two sets of entities in plan view: which sources are within / contain / intersect / cross / overlap / touch which targets, the nearest target per source, every target within a distance (distance_to + maxDistance), or sources inside a bounding box or polygon. Each set is a filter (types, layers, block names, handles…); results list source/target handles with the distance and the crossing points in mm. Use this for 'which pipes cross beams', 'what is inside this room', 'nearest column to each beam end', 'walls closer than 100 mm to the grid'. Tolerances are in mm and can be overridden. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `source` | object |  | Which entities to consider. Types, layers and block names accept AutoCAD wildcards (* and ?) and comma lists; handles short-circuit everything else. |
| `target` | object |  | Which entities to consider. Types, layers and block names accept AutoCAD wildcards (* and ?) and comma lists; handles short-circuit everything else. |
| `relation` REQ | string within|contains|intersects|crosses|overlaps|touches|nearest|distance_to|inside_bbox|inside_polygon |  | Relation tested from each source to each target |
| `maxDistance` | number |  | mm — required for distance_to; optional search radius for nearest |
| `tolerance` | object |  | Overrides of the geometry tolerances in millimetres (angles in degrees); omitted members keep the defaults shown. |
| `limit` | integer | 100 | Maximum matches returned (count tells the total) |
| `maxCandidates` | integer | 5000 | Cap on entities read per set |

## Layer

### `create_layer` — Create or update a layer

*destructiveHint.* Creates a layer with an ACI colour index and optional lineweight; when the layer already exists it is skipped or updated according to ifExists. Returns whether it was created or updated.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Layer name (AutoCAD naming rules: no < > / \ " : ; ? * \| , = ` characters) |
| `colorIndex` | integer | 7 | ACI colour index 1–255 (7 = white/black) |
| `lineweight` | integer |  | Lineweight in hundredths of a millimetre: 0, 5, 9, 13, 15, 18, 20, 25, 30, 35, 40, 50, 53, 60, 70, 80, 90, 100, 106, 120, 140, 158, 200, 211; omit for default |
| `ifExists` | string | "skip" | skip (default) or update the colour/lineweight of an existing layer |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `list_layers` — List layers

*readOnlyHint.* Lists every layer of the active drawing: name, ACI colour index, off/frozen/locked/plottable flags and lineweight (hundredths of a mm as create_layer takes it; -3 = default, -1 ByLayer, -2 ByBlock); optionally the number of model-space entities on each layer. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `includeCounts` | boolean | false | Also count the model-space entities on each layer (slower on big drawings) |

## Layout

### `list_layouts` — List layouts

*readOnlyHint.* Lists the layouts (Model and every paper-space tab) with tab order, viewport count and which one is current. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `includeModel` | boolean | true | Include the Model tab in the list |

## Generic

### `get_selected_entities` — Get selected entities

*readOnlyHint.* Returns the entities the user currently has selected in AutoCAD (handle, DXF type, layer), up to limit. Empty when nothing is selected. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `limit` | integer | 200 | Maximum entities to return (1–2000) |
