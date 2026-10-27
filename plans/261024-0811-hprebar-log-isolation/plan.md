# HPRebar / McpBridge log isolation

Status: closed 2026-10-24 — user chose option A (docs only, no code change). Defect track, not a refactoring wave.

## Goal
Each HP Revit add-in writes its own log file. Reported symptom: `%LocalAppData%\HPRebar\logs\hprebar-*.log` not written since 2026-10-08.

## Result of phase 0
[reports/phase-00-cause.md](reports/phase-00-cause.md): HPRebar is **not** affected — Serilog is ILRepack-merged into HPRebar.dll, so its `Log.Logger` is private. The silence = no HPRebar command run since 10-08. Sharing exists only between add-ins shipping Serilog loose (McpBridge ↔ HPBimToolkit, toolkit already fixed).

## Phases
| # | Phase | Status |
|---|---|---|
| 0 | Confirm cause (metadata, live module list, log grep) | ✅ |
| 1 | Option A: correct CLAUDE.md / AGENTS.md / toolkit spike report | ✅ |
| 2 | Option B (optional): bridge-owned Logger, D4/ADR-0005 note | ❌ not chosen — only if a foreign add-in ever hijacks the bridge log |

## Constraints
Debug.R26 only; never deploy while Revit is open (rename locked DLL); no commit unless asked.
