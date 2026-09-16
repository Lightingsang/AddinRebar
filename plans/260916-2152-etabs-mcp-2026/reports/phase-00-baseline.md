# Phase 0 baseline — measured 2026-09-16 (before any McpShared edit)

Runner: `pwsh reports/run-phase-00-gate.ps1 -Tag before` (Release builds of the 3 server exes, `tools/list` on an isolated registry root, 5 xUnit suites). Raw log: `phase-00-before-tests.txt`.

| Suite | Total | Fail |
|---|---|---|
| `McpShared/HPRebar.Mcp.Server.Core.Tests` | 128 | 0 |
| `McpShared/HPRebar.McpBridge.Core.Net48Tests` | 60 | 0 |
| `HPRebar/HPRebar.Mcp.Server.Tests` | 109 | 0 |
| `HPAutoCad/HPAutoCad.Mcp.Server.Tests` | 136 | 0 (AEC plan phases keep moving this) |
| `HPNavis/HPNavis.Mcp.Server.Tests` | 49 | 0 |

| Host exe (bin/Release/net10.0) | `tools/list` count | `HPRebar.Mcp.Server.Core.dll` sha256 |
|---|---|---|
| Revit | 33 | A546DECE0F73B438… |
| AutoCAD | 37 | A546DECE0F73B438… |
| Navis | 24 | A546DECE0F73B438… |

Snapshots: `phase-00-tools-list-before-{revit,autocad,navis}.json` (sorted by name), `phase-00-core-dll-before-*.sha256`.
Gate after phase 0: same suite totals (+ the new `EtabsProfileTests`), `tools/list` after == before per host, Core.dll sha changed.
