# HPAutoCad — AutoCAD MCP bridge

Turns AutoCAD 2026 into a runtime for an AI agent, the way `HPRebar/` does for Revit: an MCP server exe
(stdio, launched by the host AI) talks over a named pipe to a plugin loaded inside `acad.exe` that
compiles and runs the C# the agent sends, inside one transaction, with opt-in, guard, timeout and audit.

Plan of record: [`../plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md)
(ADR-06 explains why this is a separate top-level folder and why the engine lives in `../McpShared/`).

## Status

Phase 5 done (2026-09-14) — the plan is complete: `HPAutoCad.Mcp.Server.exe` serves MCP over stdio — 24 tools (4 core + 8 registry + 12 seed
tools installed into `%AppData%\HPAutoCad\McpServer\tools-library\` on first start), `autocad://` resources, 2 prompts —
and the whole loop is verified live in AutoCAD 2026: execute matrix, every seed, MISS → propose → test → publish →
CLI approve → `tools/list_changed` → call by name, quarantine → restore, the Revit exe beside it, a second AutoCAD
failing fast on the pipe and Civil 3D not loading the bundle (`tools/harness/run-live-verify.ps1`, 65/65 + 4/4;
`reports/phase-05-live-verify.md`). Phase 2 gave the bridge itself: pipe listener, main-thread
executor (`Application.Idle` + `IsQuiescent`), two bridge-owned transactions (`tr` is the script's), change counting,
audit, status window (`tools/harness/run-bridge-unattended.ps1`, 21/21).
AutoCAD commands: `HPMCPBRIDGE` (status window with the per-session "Allow AI code execution" opt-in), `HPMCPSTART`,
`HPMCPSTOP`, `HPMCPSTATUS` — and the Ribbon tab **HPAutoCad** ▸ **MCP** ▸ **MCP Bridge** (see below).

| Project | Phase | Purpose |
|---|---|---|
| `HPAutoCad.McpBridge.Loader` | 1 ✅ | The DLL AutoCAD loads: `IExtensionApplication`, the `HPMCP*` commands, the Ribbon tab (`Ribbon/`, Autodesk.Windows), an isolated `AssemblyLoadContext` for the real bridge |
| `HPAutoCad.McpBridge` | 1–2 ✅ | The bridge: Roslyn + self-check, `MainThreadExecutor`, `AutocadScriptRunner` (lock + outer/inner transaction, dryRun, timeout), context reader, serializer, XAML status window; references `HPAutoCad.Aec` and hands it to Roslyn |
| `HPAutoCad.Aec` | AEC A–C ✅ | The AEC engine (net8.0-windows): pure geometry/spatial/issue/classification/relationship code + `Cad/` adapters; `AecTools` facade the AEC seeds call (see "AEC tools" below) |
| `HPAutoCad.Aec.Tests` | AEC A–C ✅ | xUnit v3 (122): geometry maths, spatial predicates + index, issue detector, filters/tolerance/envelopes, review regressions (phases A + B), classification rules + rule-set loading, relationship predicates — no AutoCAD |
| `tools/harness/` | 2–5 ✅ | Unattended harnesses (Python + PowerShell): `run-bridge-unattended.ps1` (pipe, 21 scenarios), `run-server-smoke.ps1` (published exe over stdio, 22 steps incl. every seed), `run-live-verify.ps1` (phase-5 proof on one stdio session: matrix, seeds, registry loops, Revit beside, isolation), shared SECURELOAD/UIA/COM helpers |
| `HPAutoCad.Mcp.Server` | 3–4 ✅ | The MCP server exe (net10, stdio): `AutocadHostProfile`, `execute_autocad_code`, `get_autocad_context`, `autocad://` resources, prompts, 19 embedded seed tools (`Registry/SeedLibrary/`: 12 drawing seeds + 7 AEC seeds) |
| `HPAutoCad.Mcp.Server.Tests` | 3–4 ✅ | xUnit v3 (136): profile, tool surface, tools over a real pipe, every seed compile-checked against `AutoCAD.NET` 25.1.0 from the NuGet cache + `HPAutoCad.Aec` (no AutoCAD needed) |

Shared engine (referenced, never copied): `../McpShared/HPRebar.Mcp.Contracts`, `../McpShared/HPRebar.McpBridge.Core`,
`../McpShared/HPRebar.Mcp.Server.Core`. This folder never references `../HPRebar/`.

## Ribbon tab "HPAutoCad" ▸ "MCP" ▸ "MCP Bridge"

The same one-button surface as the Revit and Navisworks bridges (bundle 0.3.0; 0.2.0 carried three panels and ten
buttons — every one of them is a control of the status window, so the tab only opens that window now). Built by the
loader with `Autodesk.Windows` (AdWindows.dll from the `AutoCAD.NET` package — compile-time only, never copied). Tab
id `HPAUTOCAD_MCP_TAB`, title `HPAutoCad`; panel `MCP`; button `MCP Bridge` → `BridgeActions.Run("show")`, the same
delegate `HPMCPBRIDGE` calls, so a click needs no drawing and never edits one. The icon is a vector `DrawingImage`
drawn in code (window + plug, the glyph shared with the Navisworks bridge): every coordinate even, so the 16-px
small image is an exact half of the 32-px one; the frame ink follows `COLORTHEME` (light ink on the dark theme,
`#3C3C3C` on the light one), the plug is the HP MCP blue `#0696D7`. The tab is created once the Ribbon exists,
re-created after a workspace switch (`WSCURRENT`) and after a theme change (`COLORTHEME` → rebuilt with the other
ink), guarded by `FindTab` so it never duplicates, removed on `Terminate`. When the bridge failed to start, the
button is disabled and its tooltip names the loader log. No CUIx, no change to the user's `acad.cuix` or workspaces.

Install / update / remove: `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` with AutoCAD closed deploys the bundle
(`%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\`, autoloaded by `PackageContents.xml`,
`Platform="AutoCAD"`, R25.1); removing that folder uninstalls. Trusted location = the bundle folder (answer *Always
Load* once; SECURELOAD stays on). To try a build without the bundle: `NETLOAD` `Contents\HPAutoCad.McpBridge.Loader.dll`
from a copy of the bundle. Live check: `pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1` (12 checks + the icon as a
MANUAL item with a screenshot per theme).

## AEC tools (plan `plans/260916-1140-aec-automation-mcp-autocad/`, phases A–C done 2026-09-16)

Beyond drawing commands the server exposes an AEC layer: tools that read and understand the drawing. A tool is still a seed
(`tool.json` + `code.cs` + `examples.json`); the script is a shim that reads `args` and calls one method of
`HPAutoCad.Aec.AecTools`, so the logic is compiled, unit-tested C# in `HPAutoCad.Aec` (the bridge references it and adds it to
Roslyn's references; `HostScriptContracts.AutocadImports` imports the namespace, so ad-hoc scripts can use it too). The seven
analysis tools are read-only (`transaction: none` — the runner refuses any modification), take lengths in **mm** whatever INSUNITS is,
answer with `{ success, summary, items, count, offset, truncated, warnings, errors[{code, message, handle}] }`, and page
(`limit` ≤ 500, `offset`) so a result stays under the bridge's 64 KB cap. The six write tools run under the bridge's `auto`
transaction (one undo entry, `dryRun` rolls back) and answer with the edit envelope `{ success, createdCount, modifiedCount, deletedCount,
affectedHandles, items[{index, ok, handle, type, changed, error}], warnings, errors }`; `atomic` (default) validates every item first (read-open, no mutation) and
refuses the whole batch on one invalid item — nothing written, change counter 0 — while `atomic: false` writes the valid items and lists the rest per item; single-item
ops refuse the whole op on any bad key; batches take up to 200 items.

| Tool | Category | What it answers |
|---|---|---|
| `get_drawing_context` | Drawing | file, AutoCAD version, units/INSUNITS/measurement, active space + layout, current layer/color/linetype, UCS, extents, annotation scale, text/dim styles, current view, counts (entities, layers, blocks, xrefs, layouts, styles), optional layouts + layers |
| `query_entities` | Data | filter {types, layers (wildcards), colors, linetypes, blockNames, textContains, handles, visibleOnly, space} → records (handle, type, layer, space, boundsMm, length, text, block; `mode: detail` adds geometry vertices, area, attributes, position, color, linetype); `properties[]` selector |
| `query_entities_spatial` | Data | source × target under within / contains / intersects / crosses / overlaps / touches / nearest / distance_to / inside_bbox / inside_polygon, with distance + crossing points; `tolerance` overrides |
| `measure_geometry` | Geometry | length, totalLength, area, perimeter, centroid, boundingBox, angle, distance, closestPoint, intersections over handles and/or points (exact AutoCAD curve maths where available) |
| `detect_geometry_issues` | Audit | duplicate, near_duplicate, overlapping_segments, tiny_segment, zero_length, open_polyline, endpoint_gap, self_intersection, invalid_geometry → issues `GEO-nnnn` with severity, handles, location, value, tolerance, suggested action |
| `classify_aec_entities` | Aec | entities → AEC objects (StructuralColumn/Beam/Wall/Slab/Opening/Grid, ArchitecturalWall/Door/Window/Room/Stair/Furniture, Pipe/Duct/CableTray/Equipment/Fixture/Terminal/Fitting) with confidence, evidence, dimensions, alternatives; rules from `HPAutoCad.Aec/Rules/aec-classification.default.json` (26, AIA/NCS + English + Vietnamese layer names) or a project file `%AppData%\HPAutoCad\McpServer\rules\aec-classification.json` (`ruleSet: user`) |
| `get_entity_relationships` | Aec | intersect / connected (end-of-run gap + confidence) / near / inside / contains / touching / aligned / parallel / perpendicular between a source set and a target set (or the source set with itself), ends named by AEC type |
| `create_entities_batch` | Drawing | items[] of line / polyline (bulges) / circle / arc / point / text / mtext / blockReference (+ attributes) / dimension (linear, aligned) with layer, colour, linetype, lineweight; handles back in input order; layer must exist and be unlocked (frozen → warning) |
| `update_entities_batch` | Drawing | items [{handle, set}] or handles + set: layer/colour/linetype/lineweight/visible, text, heightMm, rotationDeg, scale, style, dimStyle, textOverride, attributes, geometry per type, move / rotate / scaleBy on any entity; LAYER_LOCKED / LAYER_FROZEN / ERASED / UNSUPPORTED_ENTITY per item |
| `manage_blocks_attributes` | Block | op listDefinitions / findReferences / insert / readAttributes / writeAttributes / batchUpdateAttributes (items, handles or filter) / inspectDynamic / setDynamic (mm, degrees) |
| `manage_annotations` | Annotation | op create (text, mtext, dimension linear/aligned/angular/radial/diameter with measurement, mleader) / update / batchUpdate / delete (annotation entities only — geometry is refused) |
| `manage_hatches` | Drawing | op create (boundaryHandles of closed curves, a polygon, or a seedPoint → smallest closed entity around it; pattern, scale, angle, style; NOT_CLOSED refuses) / update / delete / detectBoundary (closed entities containing a point, innermost first) |
| `manage_xrefs` | Data | op list / resolveStatus / attach (absolute existing .dwg, overlay) / detach (references erased) / reload / unload / bind (only Resolved + loaded) |

Tolerances (mm / degrees, `GeometryTolerance`): pointEquality 0.5 · endpointConnection 10 · collinearity 1 · parallelAngle 0.5° ·
duplicate 1 · tinySegment 5 · roomGap 25 — any member can be overridden per call. Error codes (`ToolErrorCode`): INVALID_ARGUMENT,
INVALID_HANDLE, ERASED, NOT_AN_ENTITY, UNSUPPORTED_ENTITY, NO_GEOMETRY, LAYER_LOCKED, LAYER_FROZEN, LIMIT_EXCEEDED, NOT_CLOSED, INTERNAL.
Live check: `pwsh HPAutoCad/tools/harness/run-aec-tools-live.ps1` (55 checks on a scene the harness draws; the write tools: `run-aec-edit-tools-live.ps1`, 62 checks — every op with dryRun + commit, locked/frozen/erased/block-definition paths, an associative hatch following its boundary, an xref the run writes itself, `U` reverting a batch — incl. a mirrored arc, a bulge polyline, a hatch, a block with attributes, classification + relationships, and a 3 000-line performance grid; Debug exe, isolated registry).
Next phases: C editing, D QA/QC, E structural, F architecture, G MEP, H coordination, I change sets — see the plan.

## Target

AutoCAD 2026 base release (R25.1, .NET 8). `AutoCAD.NET` NuGet pinned to `[25.1.0]` — `25.1.1` and `26.0.0` are
the .NET 10 builds (2026 Update 1.2 / 2027) and do not load on the base release.

## Commands

```bash
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug                     # deploys the bundle to %AppData%\Autodesk\ApplicationPlugins\
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false   # AutoCAD open (DLL locked)
cd HPAutoCad && dotnet test HPAutoCad.Mcp.Server.Tests            # 93 tests, no AutoCAD needed
cd HPAutoCad && dotnet test HPAutoCad.Aec.Tests                    # 122 tests, the AEC engine's pure parts
pwsh HPAutoCad/tools/harness/run-bridge-unattended.ps1            # live: bridge over the pipe (AutoCAD must be closed)
pwsh HPAutoCad/tools/harness/run-server-smoke.ps1                 # live: published exe over stdio (publish first)
pwsh HPAutoCad/tools/harness/run-live-verify.ps1 -IncludeIsolation   # live: the phase-5 proof (registry loops, Revit beside, isolation)
pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1                 # live: the Ribbon tab (UIA: one tab, workspace + theme round trips, button → window)
pwsh HPAutoCad/tools/harness/run-aec-tools-live.ps1               # live: the 7 AEC analysis tools on a drawn scene (55 checks; Debug exe, isolated registry)
pwsh HPAutoCad/tools/harness/run-aec-edit-tools-live.ps1          # live: the 6 AEC write tools (62 checks; dryRun + commit + undo, own output folder)
dotnet publish HPAutoCad/HPAutoCad.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:IncludeNativeLibrariesForSelfExtract=true -o HPAutoCad/output/HPAutoCad.Mcp.Server
HPAutoCad/output/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.exe registry approve <tool> --by <who>
```

`.mcp.json` entry (machine-specific, untracked):

```json
"hprebar-autocad": {
  "command": "<repo>\\HPAutoCad\\output\\HPAutoCad.Mcp.Server\\HPAutoCad.Mcp.Server.exe",
  "args": [],
  "env": { "HPAUTOCAD_MCP_Bridge__HostVersion": "2026" }
}
```

Runtime folders: registry `%AppData%\HPAutoCad\McpServer\` (tools-library, registry.db), bridge settings and
audit `%AppData%\HPAutoCad\McpBridge\`, bridge logs `%LocalAppData%\HPAutoCad\McpBridge\logs\`, pipe `hpautocad-mcp-2026`.
