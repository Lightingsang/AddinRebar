# Phase 0 — cause of "HPRebar log silent since 2026-10-08"

Date 2026-10-24. Read-only investigation, no code changed, nothing deployed, user's Revit (pid 58516) only inspected through its OS module list.

## Verdict
Claim "HPRebar and HPRebar.McpBridge share one Serilog, McpBridge wins, HPRebar lines land in mcpbridge log" is **false for HPRebar**. HPRebar.dll carries its own private copy of Serilog (ILRepack), so the bridge's `Log.Logger = …` cannot touch it. The sharing is real only between add-ins that ship Serilog **loose** — McpBridge ↔ HPBimToolkit (toolkit already fixed with its own `Logger`).

## Evidence
| # | Check | Result |
|---|---|---|
| 1 | Metadata of deployed `Addins\2026\HPRebar\HPRebar.dll` (PEReader) | no AssemblyRef to `Serilog`; TypeDef `Serilog.Log` defined inside HPRebar.dll (`IsRepackable=true`, HPRebar.csproj:7) |
| 2 | `Get-Process 58516).Modules` (user's Revit, started 08:09) | one Serilog 4.4.0 module, from `HPRebar.McpBridge\Serilog.dll`; none from `HPRebar\`; root `Addins\2026\Serilog.dll` **not loaded**; BimSpeed loads its own 2.12 from `%AppData%\BimSpeedApp\Libs` |
| 3 | grep every `mcpbridge-*.log` (10-17 … 10-24) for Kata/Column/Beam/Foundation/Rebar | 0 lines. Message kinds = bridge only + 3 WebView2 lines (HPBimToolkit before `ToolkitLogger`) |
| 4 | `hprebar-*.log` content | only feature lines (Kata Rebar…); HPRebar writes nothing at startup (`Application.CreateLogger` has no startup event) → a day without using an HPRebar command leaves no file |
| 5 | Last HPRebar log 2026-10-08 20:03, deployed HPRebar.dll 2026-10-08 19:57, bridge add-in installed since 09-12 | HPRebar wrote its own file in a Revit that also loaded the bridge |
| 6 | Root `Addins\2026\Serilog.dll` | AssemblyVersion 4.3.0.0, Feb 2026; sits beside `DMStrApp.deps.json` (Serilog 4.3.1) = leftover of an old root-level DMStrApp deploy; not ours, not loaded |

Silence since 10-08 = no HPRebar command run since then, not a logging defect.

## Real residual risk (bridge only)
Bridge ships loose Serilog 4.4.0 and uses static `Log` in 11 files (bridge + `McpShared/HPRebar.McpBridge.Core`). Revit 2026 resolved HPBimToolkit's own folder copy to the already-loaded bridge copy (observed in the toolkit spike, mechanism inside Revit's `AddInLoadContext` GIẢ ĐỊNH CHƯA XÁC MINH). Any future add-in that ships Serilog loose and assigns `Log.Logger` after the bridge loads would redirect the bridge's lines. No add-in installed today does that (only HP ones + BimSpeed's 2.12 carry Serilog).

## Options
| | Option | Cost | Note |
|---|---|---|---|
| A (recommended) | No code change. Correct the wrong statements: CLAUDE.md ▸ HP BIM Toolkit bullet "HPRebar's own log has been silent…" (+ AGENTS.md by hand), toolkit `reports/phase-00-spike.md` finding | docs only | nothing to fix in HPRebar |
| B | Bridge owns a `Serilog.Core.Logger` passed through constructors (toolkit pattern) | 11 files across bridge + McpShared engine (AutoCAD/Civil/Navis/ETABS bridges share the Core) → D4 allowlist / ADR-0005 note | hardening against a not-yet-seen add-in; engine-wide |
| C | Merge Serilog into HPRebar.McpBridge.dll via the existing `RepackMaterialDesign` target | needs McpBridge.Core + Contracts merged too, else Core keeps a loose Serilog and loses its sink | rejected: two Serilogs, silent Core logs |
| D (optional, with A) | One startup `Information` line in HPRebar (`HPRebar loaded, Serilog from <assembly>`) so a missing file means "not loaded" | 1 file | behaviour change, small; only if wanted |

Recommendation: A now; B only if a foreign add-in is ever seen hijacking the bridge log.
