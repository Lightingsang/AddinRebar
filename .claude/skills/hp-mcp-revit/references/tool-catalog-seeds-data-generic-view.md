# HPRebar Revit MCP — tool catalog: Data / Generic / View seeds (element filter, statistics, material quantities, family types, selection, line-based creation, delete, view info / elements, colour, hide / isolate)

Generated from `tools/list` of `HPRebar.Mcp.Server.exe` (33 tools in all) on an isolated registry — the surface a fresh install shows; the user's own registry may add approved tools (e.g. `set_mark_from_comments`). Names are `mcp__hprebar-revit__<name>` in Claude Code. `REQ` = required. Every seed takes and reports **millimetres** (points as `{x, y, z}` objects in mm) even though the Revit API works in feet; ids are Revit `ElementId` numbers. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

## Data

### `ai_element_filter` — Find elements by criteria

*readOnlyHint.* Query elements by category (BuiltInCategory name or display name), element class name (e.g. Wall, FamilyInstance, Floor), family type id, visibility in the current view and/or a bounding box in millimetres. Returns rich per-element info (type, family, level, location, bounding box, key parameters) so the AI can answer questions such as 'walls taller than 5 m'. At least one of filterCategory, filterElementType or filterFamilySymbolId is required.

| arg | type | default | description |
|---|---|---|---|
| `filterCategory` | string |  | BuiltInCategory (OST_Walls) or category display name (Walls) |
| `filterElementType` | string |  | Revit API class name: Wall, Floor, FamilyInstance, Level, Grid, ... |
| `filterFamilySymbolId` | integer |  | ElementId of a FamilySymbol; only instances of that type |
| `includeTypes` | boolean | false | Include element types |
| `includeInstances` | boolean | true | Include element instances |
| `filterVisibleInCurrentView` | boolean | false | Only elements visible in the active view |
| `boundingBoxMin` | object |  | Min corner in mm (with boundingBoxMax) |
| `boundingBoxMax` | object |  | Max corner in mm (with boundingBoxMin) |
| `maxElements` | integer | 50 | Maximum elements to return |

`boundingBoxMin` items:

| arg | type | default | description |
|---|---|---|---|
| `x` | number |  |  |
| `y` | number |  |  |
| `z` | number |  |  |

`boundingBoxMax` items:

| arg | type | default | description |
|---|---|---|---|
| `x` | number |  |  |
| `y` | number |  |  |
| `z` | number |  |  |

### `analyze_model_statistics` — Analyze model statistics

*readOnlyHint.* Model audit: total elements, types, families, views, sheets; per-category counts (optionally broken down by family/type) and per-level element distribution (elevations in mm).

| arg | type | default | description |
|---|---|---|---|
| `includeDetailedTypes` | boolean | true | Break each category down by family and type |
| `topCategories` | integer | 40 | Maximum categories to list (largest first, 0 = all) |

### `get_material_quantities` — Get material quantities

*readOnlyHint.* Material take-off: per material name/class, total area (m²), volume (m³), element count and element ids. Optionally restricted to categories (BuiltInCategory names) or to the current selection.

| arg | type | default | description |
|---|---|---|---|
| `categoryFilters` | array<string> |  | e.g. ["OST_Walls", "OST_Floors"]; empty = all categories |
| `selectedElementsOnly` | boolean | false |  |
| `maxElementIdsPerMaterial` | integer | 50 | Cap on element ids listed per material |

## Generic

### `create_line_based_element` — Create line-based elements

*destructiveHint.* Create walls, beams (structural framing), ducts, pipes, conduits, cable trays or any line-based family instance from start/end points in mm. baseLevel is an elevation in mm — the nearest level is used and the remainder becomes the base offset. Walls: thickness picks/creates a wall type of that width, height is the unconnected height. Beams/MEP curves use the points' Z as absolute elevation. Give typeId (from get_available_family_types) to pick a specific type; otherwise the first type of the category is used and reported.

| arg | type | default | description |
|---|---|---|---|
| `data` REQ | array<object> |  |  |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

`data[]` items:

| arg | type | default | description |
|---|---|---|---|
| `category` REQ | string |  | OST_Walls, OST_StructuralFraming, OST_DuctCurves, OST_PipeCurves, OST_Conduit, OST_CableTray, or a category with line-based families |
| `typeId` | integer |  | ElementId of the type to use (optional) |
| `locationLine` REQ | object |  |  |
| `thickness` | number |  | Wall width / duct width / pipe diameter in mm (optional) |
| `height` | number |  | Wall height in mm (default 3000) / duct height |
| `baseLevel` REQ | number |  | Base elevation in mm (nearest level is used) |
| `baseOffset` | number | 0 | Offset from the base level in mm |
| `structural` | boolean | false | Walls: structural usage |

`locationLine` items:

| arg | type | default | description |
|---|---|---|---|
| `p0` REQ | object |  |  |
| `p1` REQ | object |  |  |

`p0` items:

| arg | type | default | description |
|---|---|---|---|
| `x` REQ | number |  |  |
| `y` REQ | number |  |  |
| `z` REQ | number |  |  |

`p1` items:

| arg | type | default | description |
|---|---|---|---|
| `x` REQ | number |  |  |
| `y` REQ | number |  |  |
| `z` REQ | number |  |  |

### `delete_element` — Delete elements

*destructiveHint.* Delete elements by id. Revit also removes dependents (tags, dimensions, hosted elements); the response counts both. Use dryRun=true first to see what would go.

| arg | type | default | description |
|---|---|---|---|
| `elementIds` REQ | array<integer> |  | Element ids (numbers; numeric strings accepted) |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `get_available_family_types` — Get available family types

*readOnlyHint.* Loaded family types (FamilySymbol) plus system types (wall, floor, roof, ceiling, curtain system) in the project. Filter by BuiltInCategory names and/or a partial family or type name. Returns the type ids other create_* tools need.

| arg | type | default | description |
|---|---|---|---|
| `categoryList` | array<string> |  | BuiltInCategory names, e.g. ["OST_Walls", "OST_Doors"] |
| `familyNameFilter` | string |  | Case-insensitive substring matched against family name and type name |
| `limit` | integer | 200 | Maximum types to return (0 = no limit) |

### `get_selected_elements` — Get selected elements

*readOnlyHint.* Elements currently selected in Revit (id, uniqueId, name, category, type, family, level). Empty list when nothing is selected.

| arg | type | default | description |
|---|---|---|---|
| `limit` | integer | 100 | Maximum elements to return (0 = no limit) |

## View

### `color_elements` — Colour elements by parameter value

*destructiveHint.* In the active view, colour every element of a category (BuiltInCategory or display name) by the value of a parameter: each distinct value gets its own colour (random palette, a gradient, or your customColors in order). Returns the legend (value → colour → count). Undo with operate_element ResetIsolate? No — clear overrides via the view's Visibility/Graphics or run again with another parameter.

| arg | type | default | description |
|---|---|---|---|
| `categoryName` REQ | string |  | e.g. OST_StructuralColumns or 'Structural Columns' |
| `parameterName` REQ | string |  | Instance or type parameter name, e.g. 'Type Name', 'Level', 'Comments', 'Mark' |
| `useGradient` | boolean | false |  |
| `customColors` | array<object> |  |  |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

`customColors[]` items:

| arg | type | default | description |
|---|---|---|---|
| `r` | integer |  |  |
| `g` | integer |  |  |
| `b` | integer |  |  |

### `get_current_view_elements` — Get elements in the current view

*readOnlyHint.* Elements visible in the active view, filtered by model and/or annotation categories (BuiltInCategory names such as OST_Walls, OST_Doors, OST_Dimensions). Without filters a default set of common model + annotation categories is used. Locations and lengths are reported in millimetres.

| arg | type | default | description |
|---|---|---|---|
| `modelCategoryList` | array<string> |  | Model categories, e.g. ["OST_Walls", "OST_StructuralColumns"] |
| `annotationCategoryList` | array<string> |  | Annotation categories, e.g. ["OST_Dimensions", "OST_TextNotes"] |
| `includeHidden` | boolean | false | Include elements hidden in the view |
| `limit` | integer | 100 | Maximum elements to return (0 = no limit) |

### `get_current_view_info` — Get current view info

*readOnlyHint.* Details of the active Revit view: id, name, view type, scale, detail level, discipline, level, template state. Use before view-dependent tools (tags, dimensions, colouring).

### `operate_element` — Operate on elements

*destructiveHint.* Act on elements by id: Select, SetColor (RGB, view override with solid fill), SetTransparency (0–100), Highlight (red), Hide, TempHide, Isolate, Unhide, ResetIsolate (clears temporary hide/isolate), Delete. View operations target the active view. SelectionBox is interactive and not available through MCP. Use dryRun for Delete.

| arg | type | default | description |
|---|---|---|---|
| `elementIds` | array<integer> |  | Element ids (may be empty for ResetIsolate) |
| `action` REQ | string Select \| SetColor \| SetTransparency \| Highlight \| Hide \| TempHide \| Isolate \| Unhide \| ResetIsolate \| Delete |  |  |
| `colorValue` | array<integer> |  | [r, g, b] for SetColor (default red) |
| `transparencyValue` | integer | 50 |  |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

