# HPEtabs MCP — troubleshooting

Symptom → cause → what to do. Every item below was seen on the dev machine during phases 1–4 (2026-09-17) unless marked *rule*. Run `pwsh .claude/skills/hp-mcp-etabs/scripts/check-etabs-mcp.ps1` first: it reports the ETABS/bridge processes, the pipe, the exes and the `.mcp.json` entry without touching anything.

## Connection

| Symptom | Cause | Fix |
| Every tool answers `-32003` "… click Attach in the HPEtabs MCP Bridge window" | bridge running but not attached while AutoStart is disabled (or ETABS failed to launch) | tick **AutoStart** or click **Attach** in the bridge window; or call `connect_etabs`. If ETABS crashed, starting it manually and clicking Attach also works |
| Tools fail with "bridge not connected" / pipe error, hint names `HPEtabs.McpBridge.exe` | bridge app not running or listener stopped | start `HPEtabs/output/HPEtabs.McpBridge/HPEtabs.McpBridge.exe` → **Start listener** (settings `AutoStartListener` remembers it) |
| Bridge says "not registered for the API in this session" / "ETABS is running but `GetObject` found nothing" | ETABS started by double-clicking the `.EDB` or another launcher (no running-object-table entry; `Tools › Active Instance for API` greyed), **or** ETABS elevated while the bridge is not | close ETABS; start it from its shortcut; File › Open the model; Attach. Never run ETABS or the bridge as administrator |
| Bridge warns about more than one ETABS | `GetObject` returns the newer instance, never by pid | user closes the other instance or sets the right one active (Tools › Active Instance for API) |
| `-32003` "No model (.EDB)…" on a write | ETABS start screen, model never saved (`(Untitled)`), or model on a UNC share | save the model locally first; reads still work on an unsaved model |
| `-32003` "UNC share" | model path `\\server\…` | copy the model to a local drive |
| `-32001` "Allow AI code execution" | first checkbox off (it resets on every bridge start) | user ticks it in the bridge window |
| `-32001` "Allow destructive operations" | D-tier script/tool with the second checkbox off — **not** a failed run, never counts against a tool | ask the user, they tick it; use dryRun/`none` to preview a D script meanwhile |
| `-32002` busy | a script or `RunAnalysis`/`Save` is still running (a timeout never stops ETABS) | wait; `get_etabs_context` fails fast while busy; `cancel_execution` only stops between OAPI calls |
| Context shows `isModifiable:false` but `isAttached:true` | a modal dialog is open in ETABS | close the dialog (the OAPI itself keeps answering, so reads may still work) |

## Script refusals (diagnostics in `diagnostics[]`)

| Id / text | Cause | Fix |
|---|---|---|
| `GUARD … is not allowed in ETABS scripts` | denied namespace/identifier/member (`Helper`, `File` as a type, `.GetProperty`, `Task`, `await`, `#r`…) | rewrite: `sapModel.File.Save()` is fine (member position); area sections via `DatabaseTables.GetTableForDisplayArray`; no threads |
| `PREVIEW: cFrameObj.SetSection (W)` with `isError:true`, `rolledBack:true` | a writing script ran with `transaction="none"` or `dryRun=true` — by design | show the member list to the user, then `transaction="auto"`, `dryRun=false` |
| `PATH … must be a string literal or args.Str("key")` | path built from a variable, concatenation, `args.Str(key, fallback)`, a method group, a self-built `ScriptArgs` | pass the path as a literal or as `args.Str("key")` with the value in `args` |
| `PATH … outside the model folder / %LocalAppData%\HPEtabs / UNC / HPEtabs\McpBridge` | run-time policy | write next to the model or under `%LocalAppData%\HPEtabs\<yourfolder>\` |
| compile error `CS…` | wrong signature (`ref` missing, wrong arity — `GetStories_2` has 10 refs) | `inspect_type` the interface; see `oapi-cheatsheet.md` |
| `ETABS returned 1 from X` | ETABS refused: unknown name, model locked, wrong units/params | check names with `get_structural_objects`; locked model → assignments refused until unlocked (D, discards results) |
| `ArgumentException: … labels are not names` | a label (`C1`, `B12`) passed where a unique name is required | `get_structural_objects` → use `name`; or `PointObj.GetNameFromLabel(label, story, ref name)` |
| `… has no results (status not run) — run_analysis first` | case defined but never run, or results discarded by an unlock | `run_analysis` (D) or pick a case with status 4 |
| `isError:true`, `rolledBack:false`, "changes … persisted — snapshot …" | exception after the first write; ETABS cannot roll back | tell the user the snapshot name (`%LocalAppData%\HPEtabs\McpBridge\snapshots\<model>\prerun\`), do not re-run blindly |
| `timedOut:true` after 120 s on a D run | ceiling is 600 s only while the destructive opt-in is on | tick the opt-in and set `timeoutSeconds` from the GUI's last analysis time |

## Model state surprises

- **`File.Save(path)` is a save-as**: the model is re-pointed and `GetModelFilename()` then returns the `.$et` working copy; the bridge normalises to `.EDB`, scripts should too (`Path`-free: `name.EndsWith(".$et")` → swap the extension).
- **`RunAnalysis` saves the file itself**, so the next W/D run sees a file "not written by the bridge" and takes a `presave\` copy — expected.
- **`SetModelIsLocked(false)` deletes every result** (`GetCaseStatus` → 1 for all cases). Never unlock to "just assign a section" without saying so.
- **`AddByCoord`** counts +3 (frame + 2 points); **`FrameObj.Delete`** counts −6 for a lone frame (ETABS deletes the orphan points).
- **`SetRunCaseFlag`** changes persist in the model: `run_analysis cases:[…]` leaves the other cases switched off until something turns them back on.
- A user-named frame that already exists is silently renumbered by ETABS — compare name lists before/after instead of trusting `UserName`.
- Present units the user works in are untouched: the bridge forces `kN_mm_C` per run and restores in `finally`. `get_etabs_context.etabs.presentUnits` is the user's own setting (read outside a run); `get_model_info.presentUnits` — read *inside* a run — always says `kN_mm_C`.

## Registry

| Symptom | Cause | Fix |
|---|---|---|
| a seed disappeared from `tools/list` | quarantined after ≥ 5 runs with > 40 % failures (`InvalidOperationException`s count; `ArgumentException` and `-32001` never do) | `manage_tool restore` → `propose_tool newVersion` with the fix → `test_tool` → `publish_tool` → `HPEtabs.Mcp.Server.exe registry approve <name> --by <who>` |
| `run_tool` refuses a proposed tool | status draft/tested/pending | `allowUnpublished:true` while testing; publish + approve for real use |
| `test_tool` reports 0 passed on a W/D tool | dryRun is a static preview for writes — nothing ran | `test_tool realRun:true` **on a throw-away model** |
| `propose_tool` refused: "is destructive … cannot be stored" / "declared transaction: none, but … writes" | D members are seed-only; the declared transaction must match the tier | keep D operations to `execute_etabs_code` + the `run_analysis` seed; declare `auto` for W tools |
| `search_tools` finds nothing | FTS ranks by description/tags in English | search English terms too (`reactions`, `frame section`), or list by `category` |

## Rebuild / republish (developer)

- Rebuilding `HPEtabs.McpBridge` fails with **MSB3027** while any `HPEtabs.McpBridge.exe` runs — close the bridge (harness: `spike-step.ps1 -Action stop`).
- The published server exe is locked while a `hprebar-etabs` MCP server runs (every Claude Code session spawns one): restart Claude Code, then `dotnet publish HPEtabs/HPEtabs.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPEtabs/output/HPEtabs.Mcp.Server`.
- The bridge is published as a **folder** (`dotnet publish HPEtabs/HPEtabs.McpBridge -c Release -r win-x64 -p:SelfContained=false -o HPEtabs/output/HPEtabs.McpBridge`); single-file breaks Roslyn (`Assembly.Location` empty).
- Logs: bridge `%LocalAppData%\HPEtabs\McpBridge\logs\` (look for `MCP scripting self-check OK` and `ETABSv1.dll resolved from …`), audit `%AppData%\HPEtabs\McpBridge\audit\` (tags `[tier:R|W|D] [snapshot:<file>] [destructive]`), settings `%AppData%\HPEtabs\McpBridge\settings.json`, registry `%AppData%\HPEtabs\McpServer\` (`registry.db` + `tools-library\<Category>\<name>\`).
- Full live proof: `pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase full -Publish -Runs 1` on a **throw-away model** (it writes, analyses and deletes objects).
