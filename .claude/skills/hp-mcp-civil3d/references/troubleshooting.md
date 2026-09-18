# HPCivil3d MCP — troubleshooting

Symptom → cause → what to do. Every item was seen on the dev machine during phases 1–4 (2026-09-17/18) unless marked *rule*. Run `pwsh .claude/skills/hp-mcp-civil3d/scripts/check-civil3d-mcp.ps1` first: it reports the Civil 3D process, the bundle, the pipe, the published exe and the `.mcp.json` entry without touching anything.

## Connection

| Symptom | Cause | Fix |
|---|---|---|
| Tools fail "Civil 3D bridge not connected … pipe `hpcivil3d-mcp-2026`" | no Civil 3D running, bundle not loaded, or the listener stopped | open Civil 3D; ribbon **HPCivil3d ▸ MCP ▸ MCP Bridge** (or command `HPC3DMCPBRIDGE`) → start the listener |
| Bundle silent after a rebuild: no ribbon tab, no `loader.log` line | **SECURELOAD** dialog *Security - Unsigned Executable File* waiting — one prompt **per unsigned DLL hash** (loader, bridge, two engine DLLs = up to 4), loading is blocked until each is answered; the dialog names no file | user clicks **Always Load** for each; the harness answers *Load Once* for its own acad.exe only |
| Ribbon tab present in plain AutoCAD / Advance Steel | *rule*: cannot happen with `Platform="Civil3D"` — if it does, an **older** bundle (`Civil3dMcp.bundle`, `AutoCadMcp.bundle`, `Platform="AutoCAD*"`) is loading | leave the user's other bundles alone; check `%AppData%\Autodesk\ApplicationPlugins\` |
| Second Civil 3D's bridge window: "Pipe hpcivil3d-mcp-2026 is already in use - another Civil 3D 2026 instance is serving MCP." | two Civil 3D instances; the first owns the pipe | work in the first instance or close it; the second logs `could not create pipe` in the shared log |
| `-32001` "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HP MCP Bridge window inside Civil 3D." | opt-in off — it resets on **every** Civil 3D start and is never persisted | user ticks the checkbox; never edit `settings.json` to force it |
| `-32002` "Civil 3D is running a command or showing a dialog. Press ESC…" after ~8 s | user mid-command (LINE, dynamic input), a modal dialog (Panorama, Toolspace prompts), or a script still running — scripts run on the main thread, so even `get_civil3d_context` queues behind one | user presses ESC / closes the dialog; wait for the running script; `cancel_execution` stops a script at its next `ct` check |
| `-32003` "No drawing is open in Civil 3D. Open one first." | start tab / every drawing closed | open a drawing |
| Context `isReadOnly: true`; writes fail "The active drawing is read-only; only transaction=\"none\" scripts can run." | the drawing was opened twice (COM `Documents.Open` of an already-open file makes a read-only copy) or a stale hidden `.dwl`/`.dwl2` lock | close the read-only copy and activate the original; delete stale `.dwl*` (hidden files) with the drawing closed |
| COM automation answers `RPC_E_CALL_REJECTED` | Civil 3D busy for tens of seconds after a workspace switch or a drawing open | retry with a delay; not an MCP fault |

## Units and drawings

| Symptom | Cause | Fix |
|---|---|---|
| `civil3d.insunitsMismatch: true`, `drawingUnit: Feet` on a metric-looking drawing | drawing has no Civil settings (opened from `acad.dwt` inside Civil 3D) → Civil reports Feet whatever INSUNITS says | tell the user before writing coordinates; the scripts follow the **Civil** unit; a real Civil template fixes it |
| Numbers 1 000× or 304.8× off | mm vs drawing unit mixed up | plan x/y and lengths are **mm** at the tool boundary; stations/elevations/areas are drawing units (`drawingUnit` in the envelope); `units.ToDrawing`/`ToMm` in scripts |
| `coordinateSystemCode` absent / `"."` | drawing has no zone assigned — normal | nothing; `GetCoordinateSystemByCode("")` throws, so never pass an empty code |
| `list_*` return 0 objects on a tutorial `*-1` / `*-1A` drawing | those files are empty starting points of the tutorial | use `Profile-5F.dwg` (Feet: 8 alignments, corridor, network, 2 405 COGO), `Corridor-1a.dwg` (Meters: 52 parcels) copies |

## Script refusals (diagnostics in `diagnostics[]`)

| Id / text | Cause | Fix |
|---|---|---|
| `GUARD … .Rebuild is not allowed in Civil 3D scripts` / `.RebuildAll` / `.RebuildSnapshot` | corridor/surface/network rebuild denied in the MVP (unmeasured time, prompts) | read `IsOutOfDate`; ask the user to rebuild in Civil 3D |
| `GUARD Autodesk.Civil.DataShortcuts.DataShortcuts.SetWorkingFolder is not allowed` (any data-shortcut/survey/import/export member) | file and project operations are out of scope | tell the user; no workaround |
| `GUARD .GetPoint is not allowed` / `tr.Commit is not allowed` / `.StartTransaction is not allowed` / `System.IO.File.Exists is not allowed` | Editor prompts, own transactions, file system | use `tr` as given; take inputs through `args`; no files |
| `CS0104: 'Entity' is an ambiguous reference` (also `DBObject`, `Surface`) | `Autodesk.AutoCAD.DatabaseServices` and `Autodesk.Civil.DatabaseServices` both imported | write `Autodesk.Civil.DatabaseServices.Entity`/`Surface`/`TinSurface` in full |
| `CS0103` / wrong member name | guessed API | `inspect_type` with `typeName` (`Alignment`, `Autodesk.Civil.DatabaseServices.TinSurface`, `CogoPoint`…) + `memberFilter` |
| `PointNotOnEntityException: Point Outside Surface.` | `FindElevationAtXY` off the surface | catch per point; `get_surface_elevation` reports `OUTSIDE_SURFACE` per item |
| `InvalidOperationException: The script modified the drawing with transaction="none"` | a read script wrote something (also a Civil call with side effects) | `transaction: "auto"` (+ `dryRun` first) |
| `ArgumentException: No alignment named 'X'` / `layer 'X' does not exist` / `points needs 1–500 entries` / duplicate name in the batch | caller mistake — never counts against the tool's stability | fix the argument; names are case-insensitive, handles are hex strings |
| `Alignment.Create` fails on the label set | `labelSetName` empty or unknown — the API requires a non-empty existing label set | pass a name from `civil.Styles.LabelSetStyles.AlignmentLabelSetStyles` or leave `create_alignment_from_polyline.labelSet` empty (it picks the first) |
| `timedOut: true` "Script timed out after Ns (cooperative timeout)" | long loop; timeout 5–120 s, checked at `ct` | page (`limit`/`offset`), raise `timeoutSeconds` ≤ 120, add `ct.ThrowIfCancellationRequested()` |
| `truncated: true` and `value` is one string | result over the 64 KB cap | lower `limit`/`partLimit`/`maxSamples`; page with `offset` |

## Registry

| Symptom | Cause | Fix |
|---|---|---|
| `run_tool` "Tool 'x' is pending approval" / "is quarantined; pass allowUnpublished=true …" | lifecycle | user runs `HPCivil3d.Mcp.Server.exe registry approve <name> --by <who>` (or `restore`); do not pass `allowUnpublished` on a user's drawing without asking |
| Tool quarantined after 5 failures | `InvalidOperationException`/other exceptions > 40 % of the last 50 runs (argument errors never count) | `manage_tool restore` + `propose_tool newVersion` that validates input and throws `ArgumentException` |
| `propose_tool` refused: guard (`RebuildAll`), or a `transaction: none` tool that writes (`test_tool` 0/2) | the analyzer runs the same guard; the bridge refuses writes under `none` | remove the member / switch to `auto` |
| Published exe locked when republishing | a `hprebar-civil3d` server process (one per Claude Code session) holds `HPCivil3d.Mcp.Server.exe` | restart Claude Code (or stop the server) before `dotnet publish` |
