# HPNavis MCP — tool catalog: core + registry (execute / context / inspect / cancel, search / get / run / get_run / propose / test / publish / manage)

Generated from `tools/list` of `HPNavis.Mcp.Server.exe` (32 tools in all) on an isolated registry — the surface a fresh install shows; the user's own registry may add approved tools. Names are `mcp__hprebar-navis__<name>` in Claude Code. `REQ` = required. Every seed takes and reports **millimetres** (the API itself works in the document's units — `units` converts); item ids are instance-guid hashes from earlier results. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

## Core

### `cancel_execution` — Cancel running script

*idempotentHint.* Signals cancellation to the script currently running in the host application. Cancellation is cooperative: the script stops at its next `ct` check, and its transaction group is rolled back. Returns whether anything was running.

### `execute_navis_code` — Execute C# in Navisworks

*destructiveHint.* Runs a C# script inside the open Navisworks Manage model with the user's full privileges. Runtime is .NET Framework 4.8: no Span, Random.Shared, async or positional records. Globals: doc (Document), app (Year, HasClashModule, IsModified), units (document units: units.ToMm(du), units.ToDrawing(mm), units.Label), ct (check it in long loops), log(string), progress(cur, total, msg), args (args.Str/Int/Double/Bool(key, fallback), args.List/Obj(key)). End with `return <value>;`. Navisworks objects are summarised (boxes in mm); collections stop at 200 items. Find items with a Search (var s = new Search(); s.Selection.SelectAll(); s.SearchConditions.Add(SearchCondition.HasPropertyByDisplayName("Item", "Name").DisplayStringContains(text)); s.FindAll(doc, false)) rather than walking Descendants. Navisworks is a review tool: geometry is read-only. Undoable edits (one Undo entry `MCP: <label>`): selection sets, saved viewpoints, comments, permanent appearance overrides, hidden/required, current selection, clash tests and result status, TimeLiner tasks; the current viewpoint and temporary overrides may not be undoable. Heavy (doc.AppendFile/MergeFile/RemoveFile/OpenFile/Clear/UpdateFiles, SaveFile/Export*/PublishFile/GenerateImage, TestsRun*/TestsCompact*) needs the user to tick 'Allow heavy operations' (a HEAVY diagnostic says when it is off); never undone, not interruptible, up to 600 s, no UNC paths — ask the user to save first. transaction=auto commits on return; none is read-only and fails when the model changed (fingerprint check); manual runs like auto. dryRun undoes only the bridge's own Undo entry — `rolledBack:false` means the change may have persisted. isError=true with diagnostics on compile error, exception, guard/HEAVY refusal or timeout. Never open a Transaction, call Undo/Redo, show a dialog or use Document.Database — the guard rejects them. Requires the user to tick 'Allow AI code execution' in the HPNavis MCP Bridge window (Add-ins ▸ HPNavis MCP).

| arg | type | default | description |
|---|---|---|---|
| `code` REQ | string |  | C# script body, max 32 KB. No `await`, no System.IO / System.Net / System.Data / reflection / expression trees, no Transaction or Undo calls, no dialogs (blocked by the guard). |
| `transaction` | string | "auto" | auto (default): the bridge commits its transaction when the script returns. none: read-only; any change fails (and is undone when it left an undo entry). manual: accepted for compatibility, behaves like auto. |
| `dryRun` | boolean | false | Run the script, then undo the bridge's own Undo entry. Refused for heavy calls (nothing to undo). `changed` still reports what happened. |
| `timeoutSeconds` | integer | 30 | Cooperative timeout in seconds, 5–120 (up to 600 while heavy operations are allowed). The script sees it through `ct`; a script that ignores `ct` blocks Navisworks until it returns. |
| `label` | ['string', 'null'] |  | Short name for the audit log and the Undo entry `MCP: <label>`. Max 64 characters. |
| `args` |  |  | Optional JSON object handed to the script as `args` (e.g. {"text": "Wall", "name": "MCP walls"}). Keys are matched case-insensitively. |

### `get_navis_context` — Get Navisworks context

*readOnlyHint, idempotentHint.* Returns the current Navisworks session: hostVersion, model title and docPath (absent when no model is open or it was never saved), isReadOnly, isModifiable (true when a model is open and Navisworks is idle — no load, clash run or dialog in progress), units.length (the document's units), executionEnabled, and navis {documentUnits, modelCount, models [{fileName, units, sourceFileName}], selectionSetCount, savedViewpointCount, clashTestCount, hasClashModule (Manage only), heavyOperationsEnabled (the second opt-in for append/save/export/clash runs), isClear (no model open), isBusy, isModified}. With includeSelection the current selection comes back (max 50) as {id = instance-guid hash, category = class display name, name = display name}. Fails fast with a busy error while a script is running. Call this before execute_navis_code so the script matches the real model and its units.

| arg | type | default | description |
|---|---|---|---|
| `includeSelection` | boolean | false | Include the model items currently selected in Navisworks (max 50: id = instance-guid hash, category = class display name, name = display name). |

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

