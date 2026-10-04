# Phase 3 — Snapshot script + golden-run harness

## Context / prior art
- UIA scoped to the Revit pid: `plans/260928-1259-kata-rebar-mvp/reports/revit-kata-ui.ps1` (ribbon button, window, Invoke/Select/Toggle, read, screenshot).
- Revit launch + prompts + bridge: `plans/260919-1910-materialdesign-xaml-adoption/reports/revit-answer-unsigned-addin.ps1`, `revit-open-bridge-window.ps1`, `revit-toggle-optin.ps1` (used live in `plans/260912-1521-.../reports/phase-09-live-verify.md`).
- Second Revit on a copy + `doc.PathName` guard: `plans/261002-0930-kata-dy7-match/reports/live-verify-dy7-dy14.md`.
- MCP over stdio: `McpShared/tools/mcp-session.py`; server env prefix `HPREBAR_MCP_` (`McpShared/HPRebar.Mcp.Server.Core/Hosts/HostProfile.cs:20`).
- Result dialog = modal `TaskDialog` from handler (`ColumnRebarExternalEventHandler.cs:66`, `BeamRebarExternalEventHandler.cs:64`, `FoundationRebarExternalEventHandler.cs:62`) → blocks Revit/MCP until closed.
- Ribbon: tab `HPRebar`, panel `Rebar`, buttons `Column Rebar` / `Beam Rebar` / `Foundation Rebar` (`HPRebar/HPRebar/Application.cs:63-66`; absent in a `KataOnly` build → harness checks).

## Files (all new, `HPRebar/tools/golden-run/`, not in the solution)
| File | Role |
|---|---|
| `scripts/guard.csx` (prefix) | every script starts: refuse unless `doc.PathName` starts with `args.Str("runDir")` |
| `scripts/select-by-mark.csx` | `transaction: none`; marks → `uidoc.Selection.SetElementIds`; returns ids found / missing (missing → `ArgumentException`) |
| `scripts/snapshot.csx` | `transaction: none`; read-only dump (below) |
| `golden-mcp.py` | starts Debug `HPRebar.Mcp.Server.exe` with isolated registry (`HPREBAR_MCP_Registry__LibraryPath/DbPath` → run dir) + `HPREBAR_MCP_Bridge__RevitVersion=2026`; subcommands `context`, `select`, `snapshot`, `build-fixture`; writes JSON |
| `golden-ui.ps1` | PS 5.1 UIA (copy/trim of `revit-kata-ui.ps1`): `-Ribbon`, `-DumpInputs` (walk every TabItem, record Edit/ComboBox/CheckBox values by label+index), `-Set` overrides, `-Click`, `-TaskDialog read+close`, `-Shot` on failure |
| `run-golden.ps1` | orchestrator (steps below) |
| `compare-golden.py` | normalized JSON equality, unified diff, exit 0/1 |
| `specs/{column,foundation,beam}.json` | fixed spec: marks, ribbon name, window title prefix, run-button name, overrides, expected read-back file |

## Snapshot (`snapshot.csx`, args `scope` = column|foundation|beam|all, `runDir`)
Rebar = `Rebar` (+ counts of `RebarInSystem`, `AreaReinforcement`, `PathReinforcement`, `FabricSheet` as safety). Scope = host Mark prefix (`C*`, `FND-*`, `BR-*`).
```json
{ "schema":1, "scope":"beam", "revitBuild":"…", "docPath":"…(ignored by compare)",
  "rebar": { "count":n, "byPartition":{"(none)":n},
    "byTypeShape":[{"type":"D20","shape":"M_00","elements":n,"bars":q,"totalLengthMm":12345.6}],
    "elements":[{"host":"BR-B1","partition":"","type":"D20","shape":"M_00","style":"Standard","qty":3,
      "layout":"FixedNumber","spacingMm":0.0,"barLengthMm":5432.1,"hookStart":"","hookEnd":"",
      "p0":[x,y,z],"p1":[x,y,z]}] },
  "views": { "count":n, "names":[{"name":"…","type":"Section"}] },
  "dimensions": { "count":n, "byView":{"…":n} },
  "textNotes": { "count":n, "byView":{"…":n}, "texts":["…"] },
  "tags": { "count":n }, "warnings": { "count":n, "byText":{"…":n} }, "truncated":false }
```
Rules: mm, lengths rounded 0.1, points 1 mm (`p0/p1` = ends of bar position 0 centreline, catches cover/position changes lengths miss); every list sorted; no element ids; views/dims/notes over the whole doc (driver diffs against the pre-run S0). Output > 64 KB (`BridgeSettings.cs:22`) → `truncated:true` → harness fails loudly (scope smaller, never silently cut).

## `run-golden.ps1` steps (one Revit session, Column → Foundation → Beam)
1. Precondition: no `Revit.exe` running (else abort: pipe + DLL lock + user models).
2. `dotnet build HPRebar/HPRebar.slnx -c Debug.R26` (deploys HPRebar + McpBridge to `%AppData%\Autodesk\Revit\Addins\2026\`); `dotnet build HPRebar/HPRebar.Mcp.Server`; record git sha + `HPRebar.dll` sha256.
3. Copy fixture → `HPRebar/output/golden/<yyMMdd-HHmm>-<sha>/work/fixture.rvt`; start `Revit.exe "<copy>"`; answer unsigned prompts *Load Once* (2 DLLs, this pid only); wait for main window with the file title.
4. Open bridge window by ribbon, start listener if not listening, tick opt-in (never persisted).
5. `context` → assert docPath = copy. `snapshot all` → S0.
6. Per feature: `select` marks → ribbon button → window (no pick prompt = seam works) → `DumpInputs` → compare with `specs/<f>.readback.json` (first run writes it) → apply overrides → click run → wait TaskDialog → store its text → close → close window if open → `snapshot <scope>` → `<f>.json`.
7. Untick opt-in; close Revit discarding changes (graceful, kill own pid after 60 s); delete work copy.

## Spec overrides (closing the default-hides-tie-bug gap)
Column: stirrup/tie type `D12` (default thinnest `D8`); Beam: stirrup type `D10`; Foundation: one bar type ≠ default. Set by UIA (ComboBox ExpandCollapse + SelectionItem). If a combo inside a row template cannot be reached by label → add `AutomationProperties.AutomationId` in XAML (one more small seam commit, approval) or accept defaults and log residual.

## Todo
- [ ] scripts (guard, select, snapshot) compile in bridge (test via `execute_revit_code` dryRun)
- [ ] golden-mcp.py + isolated registry
- [ ] golden-ui.ps1 incl. TaskDialog close
- [ ] run-golden.ps1 end to end on fixture
- [ ] compare-golden.py

## Success criteria
One unattended `run-golden.ps1` produces 3 snapshots + 3 dialog texts + 3 read-backs, `truncated:false`, user `%AppData%\HPRebar\McpServer\` untouched (hash before/after), no foreign process touched.

## Risks
Modal dialogs other than the result (warnings "outside host", Revit failures) → harness screenshots, records text, presses default button, marks run "dialog" (still deterministic or a finding). UIA name drift (localization English default, `ColumnRebar/Service/LocalizationService.cs:14`). Rollback: delete `tools/golden-run/`.

## Security
Opt-in ticked only for the run; scripts read-only except selection; isolated registry; path guard on every script.
