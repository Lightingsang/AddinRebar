# HPCivil3d MCP — tool catalog (24 tools)

Generated from `tools/list` of `HPCivil3d.Mcp.Server.exe` on an isolated registry — the surface a fresh install shows. Names are `mcp__hprebar-civil3d__<name>` in Claude Code. `REQ` = required. **Units:** plan x/y and lengths cross the tool boundary in **millimetres**; stations, elevations and areas stay in the **Civil drawing unit** (Meters or Feet — every envelope says `drawingUnit`); handles are hex strings. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

The engine's registry tools (`inspect_type`, `search_tools`, `propose_tool`, `test_tool`) carry host-neutral descriptions that quote Revit/AutoCAD examples. For Civil 3D read them as: `inspect_type.typeName` = an AutoCAD or Civil type (`Alignment`, `Autodesk.Civil.DatabaseServices.TinSurface`, `CogoPoint`, `Corridor`, `Pipe`, `Parcel`, `Profile`); `category` ∈ Document | Alignment | Profile | Surface | Corridor | Pipe | Parcel | Point | Data | Generic; `transaction` `manual` ≡ `auto`; the CLI is `HPCivil3d.Mcp.Server.exe registry …`.

Result shape of every run (`execute_civil3d_code`, `run_tool`, seeds): `{isError, value, valueType, message, logs[], diagnostics[{id, line, column, message}], changed{added, modified, deleted}, rolledBack, timedOut, durationMs, truncated, runId, hint}` — `changed` is counted from the drawing's HANDSEED and modified objects and is reported even when `dryRun` rolled everything back.

## Core

### `cancel_execution` — Cancel running script

*idempotentHint.* Signals cancellation to the script currently running in the host application. Cancellation is cooperative: the script stops at its next `ct` check, and its transaction group is rolled back. Returns whether anything was running.

_No arguments._

### `execute_civil3d_code` — Execute C# in Civil 3D

*destructiveHint.* Runs a C# script inside the open Civil 3D drawing with the user's full privileges. Globals: doc, db, ed (WriteMessage/SelectImplied/SelectAll only), app, tr (the bridge's Transaction: tr.GetObject(id, OpenMode.ForRead), AppendEntity + tr.AddNewlyCreatedDBObject(obj, true); never Commit/Abort/Dispose it, never StartTransaction or LockDocument), civil (CivilDocument: GetAlignmentIds/GetSurfaceIds/GetPipeNetworkIds/GetSiteIds, CorridorCollection, CogoPoints, Settings, Styles), units (the Civil drawing unit, Meters or Feet: ToDrawing(mm), ToMm(du), Label — plan x/y cross the tool boundary in mm; stations, elevations and areas stay in the Civil unit; a drawing without Civil settings reports Feet whatever INSUNITS says, so read get_civil3d_context and warn on insunitsMismatch), ct, log(string), progress(cur, total, msg), args (Str/Double/Int/Long/Bool(key, fallback), Obj/List(key), Has/Require(key)). Civil and AutoCAD both define Entity/DBObject/Surface: write Autodesk.Civil.DatabaseServices.Surface in full. End with `return <value>;` (Entity → {handle,type,layer,dxfName,name}, CogoPoint adds number/x/y/elevation, Point3d → {x,y,z} — all in drawing units). Usings: the AutoCAD and Autodesk.Civil namespaces. transaction=auto commits on return; none is read-only and fails if anything changed; manual runs like auto. dryRun runs everything, rolls back and still reports `changed`. Blocked: Rebuild*, DataShortcuts, SurveyProjects, file import/export, AeccUiMgd dialogs, COM interop, Editor prompts. U in Civil 3D reverts every AI run since the user's last command. isError=true + diagnostics on compile error, exception, guard rejection or timeout; nothing is kept then. Requires 'Allow AI code execution' ticked in the HPCivil3d MCP Bridge window (command HPC3DMCPBRIDGE).

| arg | type | default | description |
|---|---|---|---|
| `code` REQ | string |  | C# script body, max 32 KB. No `await`, no System.IO / System.Net / System.Diagnostics.Process / reflection, no Editor prompts or commands, no Civil rebuilds or data-shortcut calls (blocked by the guard). |
| `transaction` | string | "auto" | auto (default): the bridge commits `tr` when the script returns. none: read-only; any change fails. manual: accepted for compatibility, behaves like auto. |
| `dryRun` | boolean | false | Run the script, then roll everything back. Use first for destructive changes; `changed` still reports what would have happened. |
| `timeoutSeconds` | integer | 30 | Cooperative timeout in seconds, 5–120. The script sees it through `ct`; a script that ignores `ct` blocks Civil 3D until it returns. |
| `label` | ['string', 'null'] |  | Short name for the audit log and the command line summary. Max 64 characters. |
| `args` |  |  | Optional JSON object handed to the script as `args` (e.g. {"alignment": "Road A", "stepMm": 10000}). Keys are matched case-insensitively. |

### `get_civil3d_context` — Get Civil 3D context

*readOnlyHint, idempotentHint.* Returns the current Civil 3D session: hostVersion, active drawing title and docPath (absent for an unsaved drawing), isReadOnly, isModifiable (true when the drawing is writable and the editor is idle), units.length (the Civil drawing unit: Meters or Feet), activeView, openDocs, executionEnabled, autocad {insunits, measurement, currentLayout, currentLayer, isModelSpace, isQuiescent, isNamedDrawing} and civil3d {product (Civil3D when the bridge runs where it should), isCivilDocument, drawingUnit (Meters or Feet — a drawing without Civil settings reports Feet), coordinateSystemCode (absent without a zone), insunitsMismatch (true when INSUNITS disagrees with the Civil unit: scripts follow the Civil unit, so warn the user before writing coordinates), alignmentCount, surfaceCount, corridorCount, pipeNetworkCount, pressureNetworkCount, cogoPointCount}. With includeSelection the current selection comes back as {id = handle value, category = layer, name = DXF name}. Call this before execute_civil3d_code so the script matches the real drawing and its unit.

| arg | type | default | description |
|---|---|---|---|
| `includeSelection` | boolean | false | Include the entities currently selected in Civil 3D (id = handle, category = layer, name = DXF name). |

### `inspect_type` — Inspect a host API type

*readOnlyHint, idempotentHint.* Reflects over an API type loaded in the running host application (Revit or AutoCAD) and returns its public members as C#-like signatures (kind: property | method | field | event). Use it to confirm exact method names and parameters before writing a script.

| arg | type | default | description |
|---|---|---|---|
| `typeName` REQ | string |  | Type name, e.g. 'Wall' or 'FilteredElementCollector' in Revit, 'Polyline' or 'LayerTableRecord' in AutoCAD, or a fully qualified name such as 'Autodesk.Revit.DB.Structure.Rebar'. |
| `memberFilter` | ['string', 'null'] |  | Optional case-insensitive substring to keep only matching member names, e.g. 'Create'. |
| `maxMembers` | integer | 100 | Maximum number of members to return (default 100, max 500). |

## Registry

### `get_run` — Get a past run

*readOnlyHint, idempotentHint.* Details of one run from the history (runId comes back from the execute tool, run_tool and test_tool): outcome, args, the code when it was a successful ad-hoc script, and — when the host application is reachable — an analysis listing hard-coded literals (line, value, variable) and the args keys already read. Use it before propose_tool to decide which literals become parameters.

| arg | type | default | description |
|---|---|---|---|
| `runId` REQ | integer |  | Run id |
| `analyze` | boolean | true | Analyse the code (guard, compile, literals) through the host application |

### `get_tool` — Get a registry tool

*readOnlyHint, idempotentHint.* Full record of one stored tool: metadata, input schema, examples, C# code (optional), run statistics and recent runs.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Tool name from search_tools |
| `includeCode` | boolean | true | Include the C# source |
| `recentRuns` | integer | 10 | How many recent runs to include (0–50) |

### `manage_tool` — Deprecate, quarantine or restore a tool

*idempotentHint.* deprecate: retire a tool (never runs again until restored). quarantine: pull it from tools/list. restore: back to draft so it can be tested and published again.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Tool name |
| `action` REQ | string |  | deprecate \| quarantine \| restore |
| `reason` | ['string', 'null'] |  | Why |

### `propose_tool` — Propose a new registry tool

Package a working C# script as a reusable tool (status draft). Replace every literal that may change between calls with args.<Kind>("key") and declare each key in inputSchema (JSON Schema object: string/number/integer/boolean/array/object, required, default, description). The code is guard-checked and compiled inside the host application but not run. Give ≥ 2 examples with different args; test_tool runs them. Returns a validation report; fix errors and call again. Use newVersion=true to replace the code of an existing tool.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | snake_case name, e.g. color_beams_by_type |
| `description` REQ | string |  | What the tool does, for search and for the reviewer (≥ 20 chars) |
| `category` REQ | string |  | One of the host's categories (search_tools lists them): Architecture \| Structure \| MEP \| Annotation \| View \| Data \| Generic for Revit, Drawing \| Layer \| Block \| Annotation \| Layout \| Data \| Generic for AutoCAD |
| `inputSchema` REQ |  |  | JSON Schema of the args object |
| `code` REQ | string |  | Script body: same globals as the execute tool plus args; must end with return |
| `examples` REQ |  |  | Usage examples: [{"title": "...", "args": {...}}] |
| `title` | ['string', 'null'] |  | Human title, e.g. 'Colour beams by type' |
| `tags` | ['array', 'null'] |  | Search tags |
| `transaction` | string | "auto" | auto (the bridge wraps the run in a transaction) \| manual (Revit: the code opens its own Transaction; AutoCAD: same as auto) \| none (read-only) |
| `timeoutSeconds` | integer | 60 | 5–120 seconds |
| `sourceRunId` | ['integer', 'null'] |  | runId of the execute run this comes from (see get_run) |
| `newVersion` | boolean | false | Propose a new version of an existing tool name |

### `publish_tool` — Publish a registry tool

*idempotentHint.* Request publication of a tested tool. Policy manual (default): the tool becomes pending_approval, a review file is written and a human must run the server's `registry approve <name>` command (`<server>.exe registry --help`; or set status=published in tool.json). Policy auto: tested tools publish immediately. Published tools appear in tools/list of every running server.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Tool name |

### `run_tool` — Run a registry tool

*destructiveHint.* Run a stored tool from the registry with the given args (must match its inputSchema from search_tools / get_tool). Same result shape as the execute tool. Draft / tested / pending tools need allowUnpublished=true. Use dryRun=true first for tools that modify the document.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Tool name |
| `args` |  |  | Arguments object for the tool |
| `dryRun` | boolean | false | Run then roll back |
| `allowUnpublished` | boolean | false | Allow running a tool that is not published yet |

### `search_tools` — Search the tool registry

*readOnlyHint, idempotentHint.* Search the registry of stored tools for this host (Revit or AutoCAD) BEFORE writing code with the execute tool (execute_revit_code / execute_autocad_code). Full-text over name, description, tags and examples, ranked by relevance × stability × status. Returns each tool's inputSchema so it can be called directly by name (published tools are real MCP tools) or through run_tool. Empty query lists tools (optionally by category).

| arg | type | default | description |
|---|---|---|---|
| `query` | ['string', 'null'] |  | What you want to do, in any language, e.g. 'tạo lưới trục', 'color beams by type', 'room schedule' |
| `category` | ['string', 'null'] |  | One of the host's categories (search_tools lists them): Architecture \| Structure \| MEP \| Annotation \| View \| Data \| Generic for Revit, Drawing \| Layer \| Block \| Annotation \| Layout \| Data \| Generic for AutoCAD |
| `limit` | integer | 5 | Maximum results, 1–50 (default 5) |
| `includeUnpublished` | boolean | false | Also return draft / tested / pending tools (they need allowUnpublished=true in run_tool) |

### `test_tool` — Test a registry tool

*destructiveHint.* Run a tool's examples (or the given cases) inside the host application with dryRun — every change is rolled back — and record the outcomes. All cases passing moves a draft to tested. realRun=true commits the changes (only on a scratch model).

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Tool name |
| `cases` |  |  | Optional cases [{"title": "...", "args": {...}}]; default = the tool's examples |
| `realRun` | boolean | false | Commit instead of rolling back |

## Document

### `get_civil_document_info` — Get Civil document info

*readOnlyHint.* Overview of the active Civil 3D drawing: product, whether it is a Civil document, the Civil drawing unit (Meters or Feet — every station, elevation and area the other tools return is in this unit; plan geometry crosses the tool boundary in mm), the coordinate system (code, description, datum, unit; absent without a zone), INSUNITS and whether it disagrees with the Civil unit (insunitsMismatch — a drawing without Civil settings reports Feet), one count per object family (alignments, siteless alignments, sites, parcels, surfaces, corridors, pipe networks, pressure networks, COGO points, point groups) and up to 20 style names per family. Read-only. Call it first.

| arg | type | default | description |
|---|---|---|---|
| `includeStyles` | boolean | true | List up to 20 style names per family (alignment, profile, surface, point, parcel, pipe, structure, corridor) |

## Alignment

### `create_alignment_from_polyline` — Create alignment from polyline

*destructiveHint.* Creates a Civil 3D alignment from an existing lightweight polyline (handle) with a unique name: site (empty = siteless), layer (default: the current layer), alignment style and label set (defaults: the drawing's first ones — the label set is mandatory in the API), optional curves between tangents, optional erase of the polyline. Side effects: adds an Alignment (and its labels) to the drawing; with erasePolyline the source polyline is deleted. dryRun creates and rolls back. Returns the alignment's handle, name, length in mm, start/end station in drawing units, entity count, site and style.

| arg | type | default | description |
|---|---|---|---|
| `polyline` REQ | string |  | Handle of an existing LWPOLYLINE (required) |
| `name` REQ | string |  | Alignment name, unique in the drawing (required) |
| `site` | string |  | Site name; "" = siteless |
| `layer` | string |  | Existing layer name; "" = the current layer |
| `style` | string |  | Alignment style name; "" = the drawing's first alignment style (whatever the template lists first — pass a name to choose) |
| `labelSet` | string |  | Alignment label set style name; "" = the drawing's first label set (the API refuses an empty one) |
| `addCurvesBetweenTangents` | boolean | false | Insert curves between tangents |
| `erasePolyline` | boolean | false | Erase the source polyline |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `get_alignment_geometry` — Get alignment geometry

*readOnlyHint.* Horizontal geometry of one alignment (by name or handle): its entities in order (paged by entityLimit/entityOffset) with type, start/end station (drawing units), length in mm, start/end point in mm, and for arcs radius (mm), centre (mm), direction and delta (radians), PI station; optional samples along the alignment every sampleStepMm (station in drawing units, x/y in mm, at most maxSamples). Read-only.

| arg | type | default | description |
|---|---|---|---|
| `alignment` REQ | string |  | Alignment name or handle (required) |
| `entityLimit` | integer (1–150) | 100 | Entities listed per call (1–150); page with entityOffset |
| `entityOffset` | integer (0–…) | 0 | Skip this many entities first (in alignment order) |
| `sampleStepMm` | number (0–…) | 0 | Sample points along the alignment every this many mm (0 = no samples; at least 1000 mm) |
| `maxSamples` | integer (1–200) | 100 | Cap on the number of samples (1–200) |

### `list_alignments` — List alignments

*readOnlyHint.* Lists the alignments of the active Civil 3D drawing in handle order: handle, name, type, length in mm, start/end station (drawing units, raw and as formatted labels with equations), site (empty = siteless), style, profile count, entity count, description. Filter by name wildcard and site; page with limit/offset. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `namePattern` | string | "*" | Name filter with * and ? wildcards, case-insensitive; * = all |
| `site` | string |  | Site name to restrict to; "" = siteless alignments only; omit = every alignment |
| `limit` | integer (1–180) | 100 | Maximum number of items to return (1–180) |
| `offset` | integer (0–…) | 0 | Skip this many items first (paging, in the listed order) |

## Profile

### `list_profiles` — List profiles

*readOnlyHint.* Lists the vertical profiles of one alignment (name or handle) or of every alignment: handle, parent alignment, name, profile type (surface / layout), start/end station, min/max elevation (drawing units), length in mm, PVI count, entity count, style. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `alignment` | string |  | Alignment name or handle; omit for every alignment |
| `limit` | integer (1–180) | 100 | Maximum number of items to return (1–180) |

## Surface

### `get_surface_elevation` — Get surface elevation at points

*readOnlyHint.* Elevation of one surface (name or handle) at up to 500 plan points given in mm ({x, y}); each item reports ok with the elevation in drawing units, or ok=false with the reason (OUTSIDE_SURFACE when the point is off the surface — the other points still answer). Read-only.

| arg | type | default | description |
|---|---|---|---|
| `surface` REQ | string |  | Surface name or handle (required) |
| `points` REQ | array<{x, y}> |  | Plan points in millimetres |

### `list_surfaces` — List surfaces

*readOnlyHint.* Lists the surfaces of the active Civil 3D drawing: handle, name, kind (TinSurface, GridSurface, TinVolumeSurface, …), description, out-of-date and auto-rebuild flags, style, point count, min/mean/max elevation (drawing units), bounds in mm, and for TIN surfaces 2D/3D area (drawing units squared) and max slope. Read-only; never rebuilds.

| arg | type | default | description |
|---|---|---|---|
| `namePattern` | string | "*" | Name filter with * and ? wildcards, case-insensitive; * = all |
| `limit` | integer (1–500) | 100 | Maximum number of items to return (1–500) |

## Corridor

### `list_corridors` — List corridors

*readOnlyHint.* Lists the corridors of the active Civil 3D drawing: handle, name, out-of-date and rebuild-automatic flags, code set style, baselines (name, alignment, profile, region count) and corridor surfaces (name, linked surface). Read-only; a corridor is never rebuilt by this tool.

| arg | type | default | description |
|---|---|---|---|
| `namePattern` | string | "*" | Name filter with * and ? wildcards, case-insensitive; * = all |
| `limit` | integer (1–500) | 50 | Maximum number of items to return (1–500) |

## Pipe

### `list_pipe_networks` — List pipe networks

*readOnlyHint.* Lists the gravity pipe networks (handle, name, reference alignment and surface, parts list, pipe and structure counts) and, with includeParts, the pipes (family, size, start/end in mm with elevation in drawing units, slope, 2D/3D length in mm, inner diameter in mm, shape, flow direction, start/end structure, minimum cover in mm) and structures (family, size, position, rim/sump elevation in drawing units, sump depth, height and inner diameter in mm, connected pipes) — partLimit is the total number of parts listed across the answer (default 60, max 100, so a page stays under 64 KB); a network cut short reports partsTruncated, and networks past the budget list none; pressure networks are summarised by counts. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `namePattern` | string | "*" | Name filter with * and ? wildcards, case-insensitive; * = all |
| `includeParts` | boolean | false | Also list the pipes and structures of each network (partLimit is the total budget across the answer) |
| `limit` | integer (1–500) | 50 | Maximum number of items to return (1–500) |
| `partLimit` | integer (1–100) | 60 | Total budget of parts (pipes + structures) listed across the whole answer (1–100); networks past the budget report partsTruncated |

## Parcel

### `list_parcels` — List parcels

*readOnlyHint.* Lists the parcels of every site (or of one site) in the active Civil 3D drawing: handle, site, name, number, tax id, address, style, description, centroid in mm and area in drawing units squared (m² for Meters, ft² for Feet). Filter by name wildcard; page with limit. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `site` | string |  | Site name to restrict to; omit for every site |
| `namePattern` | string | "*" | Name filter with * and ? wildcards, case-insensitive; * = all |
| `limit` | integer (1–250) | 100 | Maximum number of items to return (1–250) |

## Point

### `create_cogo_points` — Create COGO points

*destructiveHint.* Creates COGO points from a list of {x, y, elevation?, description?, name?} — x/y in mm, elevation in drawing units (0 when omitted) — numbered by the drawing's next-point-number setting; each item may carry its own description (default: the tool's description argument) and point name. Side effects: adds one CogoPoint per item to the drawing's point collection. dryRun creates them and rolls back, reporting the same envelope: createdCount, items[{index, handle, number}], affectedHandles.

| arg | type | default | description |
|---|---|---|---|
| `points` REQ | array<{x, y, elevation?, description?, name?}> |  | Points to create: x/y in millimetres, elevation in drawing units |
| `description` | string | "MCP" | Raw description for points without their own |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `list_cogo_points` — List COGO points

*readOnlyHint.* Lists the COGO points of the active Civil 3D drawing: handle, point number, name, x/y (easting/northing) in mm, elevation in drawing units, raw and full description, primary point group. Filter by primary point group (must exist), description wildcard and number range (a range of at most 5 000 numbers is looked up directly instead of scanning every point); page with limit/offset in point-number order. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `pointGroup` | string |  | Primary point group name to restrict to (must exist in the drawing) |
| `descriptionPattern` | string | "*" | Wildcard on the full description (* and ?), case-insensitive |
| `numberFrom` | integer (0–…) |  | Lowest point number to include |
| `numberTo` | integer (0–…) |  | Highest point number to include |
| `limit` | integer (1–300) | 200 | Maximum number of items to return (1–300) |
| `offset` | integer (0–…) | 0 | Skip this many items first (paging, in the listed order) |
