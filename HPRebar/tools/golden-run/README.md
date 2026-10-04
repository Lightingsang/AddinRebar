# Golden runs — Column, Foundation, Beam Rebar in Revit 2026

Proves that a refactor did not change what the rebar features put into the model. One unattended run opens a fresh copy
of `HPRebar.Tests/Fixtures/column-stack-2-storey.rvt` in its own Revit 2026, runs each feature with its default settings
through the real window, and stores read-only snapshots; `compare-golden.py` compares two runs.

```powershell
powershell -ExecutionPolicy Bypass -File HPRebar/tools/golden-run/run-golden.ps1 [-Tag x] [-SkipBuild] [-KeepRevit] [-Features column,foundation,beam]
python HPRebar/tools/golden-run/compare-golden.py <baseline-dir> HPRebar/output/golden/<run>
```

| File | Role |
|---|---|
| `run-golden.ps1` | orchestrator: refuses while any Revit runs; builds `Debug.R26` (deploys HPRebar + HPRebar.McpBridge); copies the fixture to `HPRebar/output/golden/<yyMMdd-HHmm>-<sha>[-dirty][-tag]/work/`; starts Revit on it; answers unsigned add-in prompts *Load Once*; opens the MCP bridge and ticks its opt-in; `s0.json`; per feature: select marks → ribbon → window (a refusal dialog is recorded as the result) → `<f>.readback.json` → run button → result dialog `<f>.dialog.txt` → `<f>.json`; unticks the opt-in, answers *No* to "save changes", deletes the work copy, writes `run-meta.json` |
| `golden-ui.ps1` | UI Automation / Win32 helpers scoped to the run's Revit pid (ribbon, windows, inputs by page, task dialogs — their command buttons are panes without Invoke, so they are clicked) |
| `golden-mcp.py` | `context`, `select`, `snapshot` through the Debug `HPRebar.Mcp.Server` on a registry inside the run folder (deleted afterwards) — never the user's `%AppData%` registry |
| `scripts/select-by-mark.csx`, `scripts/snapshot.csx` | bridge scripts (`transaction: none`); both refuse unless the active document is under the run folder |
| `scripts/build-fixture.csx` | generates the fixture model (see `HPRebar.Tests/Fixtures/README.md`) |
| `specs/<feature>.json` | marks, ribbon button, window title, run button, snapshot scope |
| `compare-golden.py` | normalised comparison (document path and Revit build dropped), unified diff, exit 0 = identical |

Snapshot = every `Rebar` whose host Mark starts with the feature's prefix (`C`, `FND-`, `BR-`): type, shape, quantity,
layout, spacing, bar length, hooks, end points of position 0 (mm, rounded), plus views, dimensions, text notes, tags and
warnings of the whole document. Lists are sorted; no element ids.

Rules: run with Revit closed (the pipe and the deployed DLL are per machine); never point it at a user model — the
scripts check the path; the bridge opt-in is ticked only for the run. Known gaps: Column is refused on the fixture by
B-45 (`CLEAN_CODE_AUDIT.md`), so its result is the refusal text until that is fixed; the readback shows some Beam
ComboBoxes (bar types) as empty because their items expose no UIA name — the snapshots carry the bar types actually
used.
