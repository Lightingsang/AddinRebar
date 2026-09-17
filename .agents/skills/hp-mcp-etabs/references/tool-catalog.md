# HPEtabs MCP — tool catalog (24 tools)

Generated from `tools/list` of `HPEtabs.Mcp.Server.exe` (2026-09-17) with an isolated registry — the surface a fresh install shows. Names are `mcp__hprebar-etabs__<name>` in Claude Code. `REQ` = required. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

The engine's registry tools (`inspect_type`, `search_tools`, `propose_tool`, `test_tool`) carry host-neutral descriptions that quote Revit/AutoCAD examples. For ETABS read them as: `inspect_type.typeName` = an `ETABSv1` interface (`cSapModel`, `cFrameObj`, `cAnalysisResults`, `cAnalysisResultsSetup`, `cPropFrame`…); `category` ∈ Model | Geometry | Property | Load | Analysis | Results | Table | Data | Generic; `transaction` `manual` ≡ `auto`; `test_tool` dryRun on a W/D tool is a static preview (0 passed) — `realRun=true` on a throw-away model only; the CLI is `HPEtabs.Mcp.Server.exe registry …`. One stale phrase in `execute_etabs_code.dryRun` ("Refused for destructive members"): the bridge (`EtabsExecutor.cs`, preview check **before** the opt-in check) answers a static `PREVIEW` for a D script under `dryRun`/`none` with no opt-in needed — only the real run needs "Allow destructive operations".

Result shape of every run (`execute_etabs_code`, `run_tool`, seeds): `{isError, value, valueType, message, logs[], diagnostics[{id,message}], changed{added,modified,deleted}, rolledBack, timedOut, durationMs, truncated, runId, hint, snapshot}` — `snapshot` = file name of the `.EDB` copy taken before a W/D run (folder `%LocalAppData%\HPEtabs\McpBridge\snapshots\<model>\prerun\`).

## Core

### `get_etabs_context` — Get ETABS context

*read-only, idempotent.* Returns the ETABS session the bridge app is attached to: hostVersion (22), docTitle and docPath of the model file (absent while no model is open or it was never saved), isModifiable (attached, a model is open, ETABS is idle — no dialog open), units.length (mm: every run forces kN_mm_C), executionEnabled, and etabs {isAttached, attachedPid, oapiVersion, isLocked (definitions cannot change until unlocked — unlocking discards results), presentUnits (the user's own API units), databaseUnits, destructiveOperationsEnabled (the second opt-in: unlock, RunAnalysis, file operations), pointCount, frameCount, areaCount}. With includeSelection the selected objects come back (max 50) as {id, category = object type, name}. Fails fast with a busy error while a script is running, with 'not attached' until the user clicks Attach in the HPEtabs MCP Bridge window, and with 'no model' on the ETABS start screen. Call this before execute_etabs_code so the script matches the real model and its lock state.

| arg | type | default | description |
|---|---|---|---|
| `includeSelection` | boolean | false | Include the objects currently selected in ETABS (max 50: id = running index, category = object type (Point/Frame/Area/…), name = object unique name). |

### `execute_etabs_code` — Execute C# in ETABS

*destructiveHint.* Runs a C# script against the ETABS 22 model the HPEtabs MCP Bridge app is attached to (ETABSv1 API). Globals: sapModel (cSapModel), etabs (cOAPI), units (forced to kN_mm_C for the run: mm, kN, kN·mm, kN/mm²; restored after), ct, log(string), progress(cur,total,msg), args (args.Str/Int/Double/Bool(key, fallback)). OAPI calls return int: check ret, throw InvalidOperationException($"ETABS returned {ret} from X"); bad inputs → ArgumentException. End with `return <value>;`. No transaction or undo. Tiers, decided statically from `sapModel.X.Member(...)` chains (an alias, cast, ?., lambda or argument of a global = D): R read-only (Get*/Is*/Has*/Count/RefreshView/all of sapModel.Results/GetTableForDisplayArray; transaction=none). W write (other members; transaction=auto): the bridge saves your model and copies a .EDB snapshot first (`snapshot` names it); unsaved or UNC models are refused; rolledBack:false after an exception means the changes persisted. D destructive (SetModelIsLocked, RunAnalysis, DeleteResults, OpenFile/New*/Save, ApplyEditedTables, Start*/Modify*/Merge*/Reset*/Clear*/Rename*/Show*/Export*/Import*/Delete*, any path-taking member): needs 'Allow destructive operations' in the bridge window, else error -32001; up to 600 s. dryRun or transaction=none on a writing script = static preview (nothing runs; PREVIEW lists the members); manual = auto; changed counts additions/deletions only. cancel/timeout cannot interrupt a running ETABS call; the save counts against the timeout. Paths: a literal or args.Str("key") under the model folder or %LocalAppData%\HPEtabs, never UNC; every path-shaped args string is screened. No Helper/ApplicationExit/dialogs/#r/#load. Needs 'Allow AI code execution' in the HPEtabs MCP Bridge window (a separate app, not inside ETABS).

| arg | type | default | description |
|---|---|---|---|
| `code` REQ | string |  | C# script body, max 32 KB. No `await`, no System.IO / System.Net / reflection / expression trees / interop, no Helper or application lifecycle calls, no dialogs (blocked by the guard). |
| `transaction` | string | "auto" | auto (default): writing script — the bridge saves the model and takes a .EDB snapshot before running. none: read-only; a script that writes is refused with a PREVIEW diagnostic. manual: accepted for compatibility, behaves like auto. |
| `dryRun` | boolean | false | Static preview for a writing script (nothing runs; the PREVIEW diagnostic lists what it would call); a read-only script runs normally. Refused for destructive members. |
| `timeoutSeconds` | integer | 30 | Cooperative timeout in seconds, 5–120 (up to 600 while destructive operations are allowed). Checked between OAPI calls only — a running RunAnalysis/Save/OpenFile cannot be interrupted. |
| `label` | string? |  | Short name for the audit log and the snapshot file name. Max 64 characters; letters, digits, _ and - survive, the rest becomes _. |
| `args` | object |  | Optional JSON object handed to the script as `args` (e.g. {"frame": "12", "section": "C40x40"}). Keys are matched case-insensitively. |

### `inspect_type` — Inspect a host API type

*read-only, idempotent.* Reflects over an API type loaded in the running host application (Revit or AutoCAD) and returns its public members as C#-like signatures (kind: property | method | field | event). Use it to confirm exact method names and parameters before writing a script.

| arg | type | default | description |
|---|---|---|---|
| `typeName` REQ | string |  | Type name, e.g. 'Wall' or 'FilteredElementCollector' in Revit, 'Polyline' or 'LayerTableRecord' in AutoCAD, or a fully qualified name such as 'Autodesk.Revit.DB.Structure.Rebar'. |
| `memberFilter` | string? |  | Optional case-insensitive substring to keep only matching member names, e.g. 'Create'. |
| `maxMembers` | integer | 100 | Maximum number of members to return (default 100, max 500). |

### `cancel_execution` — Cancel running script

*idempotent.* Signals cancellation to the script currently running in the host application. Cancellation is cooperative: the script stops at its next `ct` check, and its transaction group is rolled back. Returns whether anything was running.

_No arguments._

## Seeds — read-only (R, transaction=none)

### `get_model_info` — Get model info

*read-only.* Overview of the ETABS model the bridge is attached to: file name and folder, lock state, present and database units, ETABS version, object counts (points, frames, areas) and optionally the stories. Call it first: every script works in kN and mm whatever the model's units. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `includeStories` | boolean | false | Also list the stories with elevation and height in mm |

### `get_stories_and_grids` — Get stories and grid systems

*read-only.* Lists the stories (name, elevation and height in mm, master/similar flags, splice) from the base up and the names of the grid systems. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `limit` | integer [1..500] | 200 | Maximum number of items to return (1–500) |

### `get_structural_objects` — Get structural objects

*read-only.* Pages through the frame, area or point objects: unique name, label and story, the frame section (areas: none — the API's section member is blocked), end points or vertices in mm and the frame length in mm. Filter by story or by a substring of the name. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `kind` | string ∈ frame\|area\|point | "frame" | Which objects to list |
| `story` | string |  | Only objects on this story (exact story name) |
| `nameLike` | string |  | Only objects whose unique name or label contains this text (case-insensitive) |
| `limit` | integer [1..500] | 200 | Maximum number of items to return (1–500) |
| `offset` | integer [0..] | 0 | Skip this many items first (paging) |

### `get_materials_and_sections` — Get materials and frame sections

*read-only.* Lists the material properties (type, E in MPa, Poisson ratio, thermal coefficient, G in MPa) and the frame section properties (shape type, area in mm², I22/I33 in mm⁴, S/Z in mm³, radii of gyration in mm) defined in the model. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `limit` | integer [1..500] | 200 | Maximum number of items to return (1–500) |

### `get_load_definitions` — Get load definitions

*read-only.* Lists the load patterns (type, self-weight multiplier), the load cases (type, sub-type) and the load combinations (type and, on request, the cases with their scale factors). Read-only.

| arg | type | default | description |
|---|---|---|---|
| `includeComboCases` | boolean | false | Also list each combination's cases and scale factors |
| `limit` | integer [1..500] | 200 | Maximum number of items to return (1–500) |

### `get_joint_reactions` — Get joint reactions

*read-only.* Reactions at the restrained joints for one load case or combination: forces in kN and moments in kN·m, per joint and step. Needs analysis results (run_analysis first, or run it in ETABS). Filter by point names or by story. Read-only — selecting the output case is a results setting, not a model change.

| arg | type | default | description |
|---|---|---|---|
| `caseOrCombo` REQ | string |  | Load case or combination name whose results to read |
| `pointNames` | array |  | Only these joints — unique names from get_structural_objects.name, not labels; default every joint with a reaction |
| `story` | string |  | Only joints on this story |
| `limit` | integer [1..500] | 200 | Maximum number of items to return (1–500) |

### `get_frame_forces` — Get frame forces

*read-only.* Internal forces along frame objects for one load case or combination: axial P, shears V2/V3 in kN, torsion T and moments M2/M3 in kN·m at each output station (mm from the I end). Needs analysis results (run_analysis first). Filter by frame names. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `caseOrCombo` REQ | string |  | Load case or combination name whose results to read |
| `frameNames` | array |  | Only these frames — unique names from get_structural_objects.name, not labels; default every frame |
| `limit` | integer [1..500] | 200 | Maximum number of items to return (1–500) |

### `get_modal_results` — Get modal results

*read-only.* Periods, frequencies and participating mass ratios (UX, UY, UZ, RX, RY, RZ with their sums) of the modal load case. Needs analysis results with a modal case (run_analysis first). Read-only.

| arg | type | default | description |
|---|---|---|---|
| `caseName` | string | "Modal" | The modal load case |
| `limit` | integer [1..500] | 200 | Maximum number of items to return (1–500) |

## Seeds — write (W, transaction=auto, snapshot first)

### `draw_frame_by_coords` — Draw a frame by coordinates

*destructiveHint.* Adds one frame object between two points given in mm (global coordinates), optionally with a frame section and a user name. Writes the model: the bridge saves it and copies a .EDB snapshot first (see the result's snapshot); ETABS has no undo. Returns the new frame's unique name.

| arg | type | default | description |
|---|---|---|---|
| `x1` REQ | number |  | Start X in mm |
| `y1` REQ | number |  | Start Y in mm |
| `z1` REQ | number |  | Start Z in mm |
| `x2` REQ | number |  | End X in mm |
| `y2` REQ | number |  | End Y in mm |
| `z2` REQ | number |  | End Z in mm |
| `section` | string |  | Frame section property name; default the model's default section |
| `name` | string |  | User name for the new frame; default ETABS numbers it |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `assign_frame_section` — Assign a frame section

*destructiveHint.* Assigns one frame section property to the given frame objects (unique names). Writes the model: the bridge saves it and copies a .EDB snapshot first; ETABS has no undo. Frames ETABS refuses are listed in errors.

| arg | type | default | description |
|---|---|---|---|
| `frameNames` REQ | array |  | Unique names of the frames |
| `section` REQ | string |  | Frame section property name (must exist) |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `assign_frame_load` — Assign a frame load

*destructiveHint.* Assigns a uniformly distributed load (kN/m over the whole length) or a point load (kN at a relative position 0–1) to frame objects under a load pattern. direction 10 = gravity by default (a positive value acts downward); 1–3 are the frame's local axes, 4–6 global X/Y/Z, 7–9 projected. Writes the model: the bridge saves it and copies a .EDB snapshot first; ETABS has no undo.

| arg | type | default | description |
|---|---|---|---|
| `frameNames` REQ | array |  | Unique names of the frames |
| `pattern` REQ | string |  | Load pattern name (must exist) |
| `loadType` | string ∈ distributed\|point | "distributed" | Uniform distributed load or a single point load |
| `valueKNperM` | number |  | Distributed load in kN/m (positive = along the direction; downward for gravity directions 10/11) |
| `valueKN` | number |  | Point load in kN |
| `relativePosition` | number [0..1] | 0.5 | Point load position as a fraction of the length from the I end |
| `direction` | integer [1..11] | 10 | 1–3 frame local axes, 4–6 global X/Y/Z, 7–9 projected global, 10 gravity, 11 projected gravity |
| `replace` | boolean | true | Replace existing loads of this pattern on the frame (false = add) |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

## Seeds — destructive (D, second opt-in)

### `run_analysis` — Run the analysis

*destructiveHint.* DESTRUCTIVE: runs the ETABS analysis (all load cases, or only the ones named) after optionally deleting existing results, and reports each case's status. Needs the user's 'Allow destructive operations' checkbox in the bridge window on every call. Choose timeoutSeconds from the last analysis time in the ETABS GUI (up to 600): a timeout does not abort the analysis — ETABS keeps running it and later calls answer busy until it finishes. The model is saved and a .EDB snapshot copied first; results of a run cannot be undone.

| arg | type | default | description |
|---|---|---|---|
| `cases` | array |  | Only run these load cases — the run flags of the others are switched off and stay off in the model until changed; default every case |
| `deleteResultsFirst` | boolean | false | Delete all existing results before running |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

## Registry (engine, host-neutral)

### `search_tools` — Search the tool registry

*read-only, idempotent.* Search the registry of stored tools for this host (Revit or AutoCAD) BEFORE writing code with the execute tool (execute_revit_code / execute_autocad_code). Full-text over name, description, tags and examples, ranked by relevance × stability × status. Returns each tool's inputSchema so it can be called directly by name (published tools are real MCP tools) or through run_tool. Empty query lists tools (optionally by category).

| arg | type | default | description |
|---|---|---|---|
| `query` | string? |  | What you want to do, in any language, e.g. 'tạo lưới trục', 'color beams by type', 'room schedule' |
| `category` | string? |  | One of the host's categories (search_tools lists them): Architecture \| Structure \| MEP \| Annotation \| View \| Data \| Generic for Revit, Drawing \| Layer \| Block \| Annotation \| Layout \| Data \| Generic for AutoCAD |
| `limit` | integer | 5 | Maximum results, 1–50 (default 5) |
| `includeUnpublished` | boolean | false | Also return draft / tested / pending tools (they need allowUnpublished=true in run_tool) |

### `get_tool` — Get a registry tool

*read-only, idempotent.* Full record of one stored tool: metadata, input schema, examples, C# code (optional), run statistics and recent runs.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Tool name from search_tools |
| `includeCode` | boolean | true | Include the C# source |
| `recentRuns` | integer | 10 | How many recent runs to include (0–50) |

### `run_tool` — Run a registry tool

*destructiveHint.* Run a stored tool from the registry with the given args (must match its inputSchema from search_tools / get_tool). Same result shape as the execute tool. Draft / tested / pending tools need allowUnpublished=true. Use dryRun=true first for tools that modify the document.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Tool name |
| `args` | object |  | Arguments object for the tool |
| `dryRun` | boolean | false | Run then roll back |
| `allowUnpublished` | boolean | false | Allow running a tool that is not published yet |

### `get_run` — Get a past run

*read-only, idempotent.* Details of one run from the history (runId comes back from the execute tool, run_tool and test_tool): outcome, args, the code when it was a successful ad-hoc script, and — when the host application is reachable — an analysis listing hard-coded literals (line, value, variable) and the args keys already read. Use it before propose_tool to decide which literals become parameters.

| arg | type | default | description |
|---|---|---|---|
| `runId` REQ | integer |  | Run id |
| `analyze` | boolean | true | Analyse the code (guard, compile, literals) through the host application |

### `propose_tool` — Propose a new registry tool

*writes.* Package a working C# script as a reusable tool (status draft). Replace every literal that may change between calls with args.<Kind>("key") and declare each key in inputSchema (JSON Schema object: string/number/integer/boolean/array/object, required, default, description). The code is guard-checked and compiled inside the host application but not run. Give ≥ 2 examples with different args; test_tool runs them. Returns a validation report; fix errors and call again. Use newVersion=true to replace the code of an existing tool.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | snake_case name, e.g. color_beams_by_type |
| `description` REQ | string |  | What the tool does, for search and for the reviewer (≥ 20 chars) |
| `category` REQ | string |  | One of the host's categories (search_tools lists them): Architecture \| Structure \| MEP \| Annotation \| View \| Data \| Generic for Revit, Drawing \| Layer \| Block \| Annotation \| Layout \| Data \| Generic for AutoCAD |
| `inputSchema` REQ | object |  | JSON Schema of the args object |
| `code` REQ | string |  | Script body: same globals as the execute tool plus args; must end with return |
| `examples` REQ | object |  | Usage examples: [{"title": "...", "args": {...}}] |
| `title` | string? |  | Human title, e.g. 'Colour beams by type' |
| `tags` | array? |  | Search tags |
| `transaction` | string | "auto" | auto (the bridge wraps the run in a transaction) \| manual (Revit: the code opens its own Transaction; AutoCAD: same as auto) \| none (read-only) |
| `timeoutSeconds` | integer | 60 | 5–120 seconds |
| `sourceRunId` | integer? |  | runId of the execute run this comes from (see get_run) |
| `newVersion` | boolean | false | Propose a new version of an existing tool name |

### `test_tool` — Test a registry tool

*destructiveHint.* Run a tool's examples (or the given cases) inside the host application with dryRun — every change is rolled back — and record the outcomes. All cases passing moves a draft to tested. realRun=true commits the changes (only on a scratch model).

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Tool name |
| `cases` | object |  | Optional cases [{"title": "...", "args": {...}}]; default = the tool's examples |
| `realRun` | boolean | false | Commit instead of rolling back |

### `publish_tool` — Publish a registry tool

*idempotent.* Request publication of a tested tool. Policy manual (default): the tool becomes pending_approval, a review file is written and a human must run the server's `registry approve <name>` command (`<server>.exe registry --help`; or set status=published in tool.json). Policy auto: tested tools publish immediately. Published tools appear in tools/list of every running server.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Tool name |

### `manage_tool` — Deprecate, quarantine or restore a tool

*idempotent.* deprecate: retire a tool (never runs again until restored). quarantine: pull it from tools/list. restore: back to draft so it can be tested and published again.

| arg | type | default | description |
|---|---|---|---|
| `name` REQ | string |  | Tool name |
| `action` REQ | string |  | deprecate \| quarantine \| restore |
| `reason` | string? |  | Why |
