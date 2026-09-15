# Phase 3 — server report: `HPNavis.Mcp.Server` (net10, stdio) + first end-to-end run

**Date:** 2026-09-15 · **Exe:** `HPNavis/output/HPNavis.Mcp.Server/HPNavis.Mcp.Server.exe` (publish `-c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false`, 7.4 MB) · **Smoke:** `powershell.exe -File HPNavis/tools/harness/run-server-smoke.ps1` (Roamer + plugin from phase 2, isolated registry root) · **Raw output:** `HPNavis/output/spike/server-smoke.log` (gitignored)

**Gate result:** `dotnet test HPNavis/HPNavis.Mcp.Server.Tests` **12/12** · stdio smoke against the live bridge **8/8** · `serverInfo.name == "HPNavis MCP"` · `tools/list` = exactly the 12 expected names · no-bridge error names Navisworks and carries no machine path · **net10 client (`PipeOptions.CurrentUserOnly`) ↔ net48 listener (`PipeSecurity.SetOwner`) connects; 0 `UnauthorizedAccess` in the bridge log.**

## What was built

| File | Role |
|---|---|
| `HPNavis.Mcp.Server/Program.cs` | one line: `McpServerHost.RunAsync(args, NavisHostProfile.Instance)` |
| `Hosts/NavisHostProfile.cs` | `HostId navis`, `ServerName "HPNavis MCP"`, `ProductFolder HPNavis` (registry root `%AppData%\HPNavis\McpServer\`), `EnvPrefix HPNAVIS_MCP_`, versions `[2026]`, prefix `navis.`, categories Model/Search/Selection/Viewpoint/Clash/Timeliner/Report/Data/Generic, **`MaxTimeoutSeconds = 600`**, `CliExecutable HPNavis.Mcp.Server.exe`, script contract summary (globals, review-tool nature, heavy, no Transaction/Undo/Database) |
| `Tools/ExecuteNavisCodeTool.cs` | `execute_navis_code` (Destructive), description 1 8xx chars: .NET Framework 4.8 limits, globals, mm boundary, Search over Descendants, the writability classes (read-only geometry · undoable review edits · maybe-not-undoable current viewpoint/temporary overrides · heavy with the second opt-in and `HEAVY` diagnostic), `none` = fingerprint check, `rolledBack:false` ⇒ may have persisted, guard list, opt-in checkbox; `timeoutSeconds` 5–120 (600 with heavy) |
| `Tools/NavisContextTool.cs` | `get_navis_context` (ReadOnly): the `navis` block fields, busy-fast behaviour |
| `Prompts/NavisScriptPrompts.cs` | `navis_query_template` (Search + bounding box in mm), `navis_review_template` (dry run → confirm → real run, one Undo entry) |
| `Resources/NavisDocumentResources.cs` | `navis://document/info`, `navis://selection` |
| `appsettings.json` | copy of the AutoCAD one (`Bridge.HostVersion 2026`) |
| `HPNavis.Mcp.Server.Tests/` | `NavisHostProfileTests` (5: profile values, options binding to the HPNavis registry root, 12-tool surface + description contract, resources/prompts, server name) · `NavisToolsOverPipeTests` (7: context with `navis` block and no Revit fields, resource, execute pass-through, **600 s accepted / 900 → 600**, opt-in refusal naming Navisworks, busy + no-model errors, no-bridge error without machine path) |
| `tools/harness/run-server-smoke.ps1` + `server-smoke.py` | Roamer + UIA opt-in (PowerShell), then one stdio session with the published exe (Python, reuses `HPAutoCad/tools/harness/mcp-session.py`) |

Nothing in `HPNavis.Mcp.Server/` references `Autodesk.*` (`grep -rn "Autodesk\." --include=*.cs --include=*.csproj` = 0 hits outside string literals of descriptions/prompts). `McpShared/` untouched in this phase (`git diff 63d9454 -- McpShared/HPRebar.Mcp.Server.Core McpShared/HPRebar.Mcp.Contracts` empty), so the Revit/AutoCAD `tools/list` snapshots from phase 1 still hold.

## Stdio smoke (live bridge)

| # | Check | Result |
|---|---|---|
| 1 | `initialize` → `serverInfo.name` | `HPNavis MCP` 1.0.0 |
| 2 | `tools/list` | 12: `cancel_execution, execute_navis_code, get_navis_context, get_run, get_tool, inspect_type, manage_tool, propose_tool, publish_tool, run_tool, search_tools, test_tool` |
| 3 | `get_navis_context {includeSelection:true}` | `host=navis`, title `gatehouse_pub.nwd`, units Millimeters, 1 model, 1 selection set, `hasClashModule=true`, `heavyOperationsEnabled=false`, no `revitVersion`/`isFamily`, **0.20 s** |
| 4 | `execute_navis_code` `none` (`doc.Title`, set count, `units.ToMm(1.0)`) | ok, `changed 0/0/0`, `rolledBack=false` |
| 5 | `execute_navis_code` `auto`+`dryRun` (add a selection set) | `changed.added=1`, `rolledBack=true`, set count 1 → 1 afterwards |
| 6 | `execute_navis_code` with `AppendFile`, heavy OFF | `isError`, `HEAVY` diagnostic naming the checkbox — through the server's error mapping |
| 7 | `inspect_type SelectionSet` | `Autodesk.Navisworks.Api.SelectionSet : SavedItem`, members listed |
| 8 | `search_tools "selection set"` | answers with 0 hits + the "write it with execute_navis_code" hint (no seeds until phase 4) |

Bridge log for the session: `MCP server connected` (the stdio server's pipe client), no `UnauthorizedAccess`, no `[ERR]`.

## Fix made while smoking

- `NavisScriptRunner` zeroed `Changed` when a run was rolled back; a dry run exists to report what *would* change, and the tool description (like Revit/AutoCAD) promises `changed` — now the delta is reported regardless of the rollback. Bridge redeployed; `run-bridge-unattended.ps1 -Runs 1 -WithModal` re-run after the change (see gate line in `phase-02-bridge-runtime.md` for the full matrix).

## Client wiring

`.mcp.json` (machine-specific; the file is tracked in git with local modifications and must not be committed) gained:

```json
"hprebar-navis": { "command": "<repo>\\HPNavis\\output\\HPNavis.Mcp.Server\\HPNavis.Mcp.Server.exe", "args": [], "env": { "HPNAVIS_MCP_Bridge__HostVersion": "2026" } }
```

The existing `hprebar-revit` / `hprebar-autocad` entries were not touched. Claude Code picks the new server up on its next MCP reload; the exe is locked while that server runs (republish with the server stopped — same known gap as Revit).

## Harness notes

- PowerShell 5.1 cannot pass JSON with embedded quotes to a native process reliably (`& python @args` strips them); the smoke therefore lives in Python (`server-smoke.py`), PowerShell only drives Roamer and the window.
- `mcp-call.py`/`mcp-session.py` print UTF-8 (registry descriptions contain `≥`): set `PYTHONIOENCODING=utf-8` or reconfigure stdout.
- The Windows `Test-Path \\.\pipe\…` probe counts as one "server connected/disconnected" pair in the bridge log — harmless, but do not read the connection count as the number of MCP sessions.

## Deferred / not covered

- Seeds (`Registry/SeedLibrary/`) — phase 4; `search_tools` returns 0 hits until then.
- `McpShared/tools/` canonical harness scripts — phase 5 (the smoke reuses the AutoCAD copies by path).
- `run_tool` with a 600 s record, `test_tool`, propose/approve loop over the real bridge — phase 5 live verify.
- Republishing while `hprebar-navis` is loaded in Claude Code locks the exe (known gap shared with Revit).

## Code review (same day)

`reports/code-review-phase-03.md`: 8/10, no Critical/High; 3 Medium + 8 Low. Fixed before commit: M2 (600 s as one Contracts constant), M3, L1–L8; M1 handled by keeping `.mcp.json` out of the commit. Every description/prompt claim was fact-checked by the reviewer against the bridge code, the installed API (reflection) and a Roslyn-on-net48 probe. Final: 12/12 tests, smoke 8/8 after republish.
