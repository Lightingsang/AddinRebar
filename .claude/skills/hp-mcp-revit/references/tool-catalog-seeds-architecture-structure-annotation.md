# HPRebar Revit MCP — tool catalog: Architecture / Structure / Annotation seeds (levels, grids, doors / windows / columns, floors / roofs, rooms, room export, beam systems, tags, dimensions)

Generated from `tools/list` of `HPRebar.Mcp.Server.exe` (33 tools in all) on an isolated registry — the surface a fresh install shows; the user's own registry may add approved tools (e.g. `set_mark_from_comments`). Names are `mcp__hprebar-revit__<name>` in Claude Code. `REQ` = required. Every seed takes and reports **millimetres** (points as `{x, y, z}` objects in mm) even though the Revit API works in feet; ids are Revit `ElementId` numbers. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

## Architecture

### `create_grid` — Create grid system

*destructiveHint.* Create a rectangular grid: X-axis grids are vertical lines at xStartPosition + i·xSpacing running from yExtentMin to yExtentMax; Y-axis grids are horizontal lines at yStartPosition + j·ySpacing running from xExtentMin to xExtentMax. Labels alphabetic (A, B, … Z, AA) or numeric (1, 2, …); names already used get a suffix. All lengths in mm.

| arg | type | default | description |
|---|---|---|---|
| `xCount` REQ | integer |  | Number of X-axis (vertical) grids |
| `xSpacing` REQ | number |  | Spacing between X-axis grids in mm |
| `xStartLabel` | string | "A" |  |
| `xNamingStyle` | string alphabetic \| numeric | "alphabetic" |  |
| `yCount` REQ | integer |  | Number of Y-axis (horizontal) grids |
| `ySpacing` REQ | number |  | Spacing between Y-axis grids in mm |
| `yStartLabel` | string | "1" |  |
| `yNamingStyle` | string alphabetic \| numeric | "numeric" |  |
| `xStartPosition` | number | 0 | X of the first vertical grid (mm) |
| `yStartPosition` | number | 0 | Y of the first horizontal grid (mm) |
| `xExtentMin` | number |  | Where horizontal grids start (mm); default = xStartPosition − 1000 |
| `xExtentMax` | number |  | Where horizontal grids end (mm); default = last X grid + 1000 |
| `yExtentMin` | number |  | Where vertical grids start (mm); default = yStartPosition − 1000 |
| `yExtentMax` | number |  | Where vertical grids end (mm); default = last Y grid + 1000 |
| `elevation` | number | 0 | Z of the grid lines in mm |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `create_level` — Create levels

*destructiveHint.* Create one or more levels at elevations in millimetres (from project origin), optionally with a floor plan and/or ceiling plan view each. Existing level names are skipped and reported. Use dryRun first on a shared model.

| arg | type | default | description |
|---|---|---|---|
| `data` REQ | array<object> |  |  |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

`data[]` items:

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Level name, e.g. 'Level 2', 'Roof' |
| `elevation` REQ | number |  | Elevation in mm |
| `isBuildingStory` | boolean | true |  |
| `createFloorPlan` | boolean | true |  |
| `createCeilingPlan` | boolean | false |  |
| `createStructuralPlan` | boolean | false |  |

### `create_point_based_element` — Create point-based elements

*destructiveHint.* Place family instances at points in mm: doors and windows (hosted in a wall — hostWallId or the nearest wall within 1500 mm), furniture, columns, generic models, etc. Category is taken from typeId when given; otherwise pass category and the first loaded type is used. rotation in degrees about Z; facingFlipped flips doors/windows. width/height/depth are applied only when the instance exposes writable parameters of those names.

| arg | type | default | description |
|---|---|---|---|
| `data` REQ | array<object> |  |  |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

`data[]` items:

| arg | type | default | description |
|---|---|---|---|
| `name` | string |  | Free text label for the report |
| `category` | string |  | BuiltInCategory when typeId is omitted, e.g. OST_Doors, OST_Windows, OST_Furniture, OST_StructuralColumns, OST_GenericModel |
| `typeId` | integer |  | ElementId of a FamilySymbol (optional) |
| `locationPoint` REQ | object |  |  |
| `width` | number |  |  |
| `depth` | number |  |  |
| `height` | number |  |  |
| `baseLevel` REQ | number |  | Base elevation in mm (nearest level is used) |
| `baseOffset` | number | 0 |  |
| `rotation` | number | 0 | Degrees about the vertical axis |
| `hostWallId` | integer |  | Wall to host a door/window (optional) |
| `facingFlipped` | boolean | false |  |

`locationPoint` items:

| arg | type | default | description |
|---|---|---|---|
| `x` REQ | number |  |  |
| `y` REQ | number |  |  |
| `z` REQ | number |  |  |

### `create_room` — Create rooms

*destructiveHint.* Place rooms at points in mm inside enclosed wall boundaries and set name, number, department, comments and limits. Level from levelId or the nearest level to the point's Z. Reports whether each room ended up enclosed (area > 0).

| arg | type | default | description |
|---|---|---|---|
| `data` REQ | array<object> |  |  |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

`data[]` items:

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  |  |
| `number` | string |  |  |
| `location` REQ | object |  |  |
| `levelId` | integer |  |  |
| `upperLimitId` | integer |  | Level ElementId for the upper limit |
| `limitOffset` | number |  | mm |
| `baseOffset` | number |  | mm |
| `department` | string |  |  |
| `comments` | string |  |  |

`location` items:

| arg | type | default | description |
|---|---|---|---|
| `x` REQ | number |  |  |
| `y` REQ | number |  |  |
| `z` REQ | number |  |  |

### `create_surface_based_element` — Create floors, ceilings and roofs

*destructiveHint.* Create floors (default), ceilings or footprint roofs from a closed outer loop of line segments in mm. baseLevel is an elevation in mm (nearest level + offset). thickness picks or creates a floor/ceiling type of that thickness when typeId is omitted.

| arg | type | default | description |
|---|---|---|---|
| `data` REQ | array<object> |  |  |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

`data[]` items:

| arg | type | default | description |
|---|---|---|---|
| `name` | string |  |  |
| `category` | string OST_Floors \| OST_Ceilings \| OST_Roofs | "OST_Floors" |  |
| `typeId` | integer |  |  |
| `boundary` REQ | object |  |  |
| `thickness` | number |  | Slab thickness in mm (floors/ceilings, when typeId omitted) |
| `baseLevel` REQ | number |  | Elevation in mm (nearest level is used) |
| `baseOffset` | number | 0 |  |
| `structural` | boolean | true | Floors: structural floor |

`boundary` items:

| arg | type | default | description |
|---|---|---|---|
| `outerLoop` REQ | array<object> |  |  |

`outerLoop[]` items:

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

### `export_room_data` — Export room data

*readOnlyHint.* All rooms with name, number, level, area (m²), volume (m³), perimeter (mm), unbounded height (mm), department, comments, occupancy and phase. Unplaced and not-enclosed rooms are excluded unless requested.

| arg | type | default | description |
|---|---|---|---|
| `includeUnplacedRooms` | boolean | false |  |
| `includeNotEnclosedRooms` | boolean | false |  |

## Structure

### `create_structural_framing_system` — Create beam system

*destructiveHint.* Create a Revit BeamSystem inside a rectangle (mm) on a named level: beams at fixed spacing, running perpendicular to the chosen edge, with beginning/center/end justification. beamTypeName matches a loaded structural framing family or type name (first one otherwise). A missing level named 'Level N' is created at N × 4000 mm, as in the reference implementation.

| arg | type | default | description |
|---|---|---|---|
| `levelName` REQ | string |  |  |
| `xMin` REQ | number |  |  |
| `xMax` REQ | number |  |  |
| `yMin` REQ | number |  |  |
| `yMax` REQ | number |  |  |
| `spacing` REQ | number |  | Beam spacing in mm |
| `directionEdge` | string bottom \| right \| top \| left | "bottom" | Beams run perpendicular to this edge |
| `justify` | string beginning \| center \| end \| directionline | "center" |  |
| `beamTypeName` | string |  |  |
| `is3D` | boolean | false |  |
| `elevation` | number |  | Optional absolute elevation in mm; default = level elevation |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

## Annotation

### `create_dimensions` — Create dimensions

*destructiveHint.* Create linear dimensions in a plan/section view (active view by default). Either give elementIds (walls, columns, grids, family instances — one reference per element, the planar face or datum best aligned with the start→end direction) or leave them empty to auto-detect the nearest wall/grid/column at startPoint and endPoint. linePoint moves the dimension line; coordinates in mm.

| arg | type | default | description |
|---|---|---|---|
| `dimensions` REQ | array<object> |  |  |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

`dimensions[]` items:

| arg | type | default | description |
|---|---|---|---|
| `startPoint` REQ | object |  |  |
| `endPoint` REQ | object |  |  |
| `linePoint` | object |  | Optional point the dimension line passes through; default = 1000 mm beside the segment |
| `elementIds` | array<integer> |  | Elements to dimension between; empty = auto-detect at the two points |
| `dimensionStyleId` | integer |  | DimensionType ElementId; -1 = view default |
| `viewId` | integer |  | View ElementId; -1 = active view |

`startPoint` items:

| arg | type | default | description |
|---|---|---|---|
| `x` REQ | number |  |  |
| `y` REQ | number |  |  |
| `z` REQ | number |  |  |

`endPoint` items:

| arg | type | default | description |
|---|---|---|---|
| `x` REQ | number |  |  |
| `y` REQ | number |  |  |
| `z` REQ | number |  |  |

`linePoint` items:

| arg | type | default | description |
|---|---|---|---|
| `x` REQ | number |  |  |
| `y` REQ | number |  |  |
| `z` REQ | number |  |  |

### `tag_all_rooms` — Tag rooms in the current view

*destructiveHint.* Place a room tag at the centre of every placed room visible in the active plan view (or only roomIds). Rooms already tagged in the view are skipped. tagTypeId picks a RoomTagType; otherwise the first loaded room tag type is used.

| arg | type | default | description |
|---|---|---|---|
| `useLeader` | boolean | false |  |
| `tagTypeId` | integer |  | RoomTagType ElementId (optional) |
| `roomIds` | array<integer> |  | Only these rooms (optional) |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `tag_all_walls` — Tag walls in the current view

*destructiveHint.* Place a wall tag at the midpoint of every wall visible in the active view. Walls already tagged in the view are skipped. tagTypeId picks a wall tag family type (OST_WallTags); otherwise the first loaded one is used.

| arg | type | default | description |
|---|---|---|---|
| `useLeader` | boolean | false |  |
| `tagTypeId` | integer |  | Wall tag FamilySymbol ElementId (optional) |
| `wallIds` | array<integer> |  | Only these walls (optional) |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

