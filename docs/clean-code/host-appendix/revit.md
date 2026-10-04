# Host appendix — Revit (HPRebar add-in + Revit MCP)

> Read with [HP_CLEAN_CODE_CORE.md](../HP_CLEAN_CODE_CORE.md). Scope: `HPRebar/` — the rebar add-in, `HPRebar.Core`, `HPRebar.Mcp.Server`, `HPRebar.McpBridge` and their tests.

## Add-in and Core

The Revit add-in rules are the Revit standard: **[REVITADDINAI_CLEAN_CODE_STANDARD.md](../REVITADDINAI_CLEAN_CODE_STANDARD.md)** — D1, D4, K6, T2, T5, T6, R1–R13, P1–P4. They are not repeated here.

## Revit MCP bridge and seeds

| Id | Rule | Source |
|---|---|---|
| RV1 | A tool is `tool.json + code.cs + examples.json` in the library and runs through `revit.execute`; never a native command class in the bridge | CLAUDE.md "HPRebar MCP Bridge" (ADR-05 of plan 260912-1521) |
| RV2 | Transaction policy is the bridge's: `auto` = one Transaction inside a TransactionGroup `MCP: <label>`; `manual` = the script opens its own; `none` = read-only; `dryRun` always rolls the group back | CLAUDE.md (ADR-03 of plan 260912-1521) |
| RV3 | Seed/tool code: plain script body ending in `return`, mm at the boundary, `args.X("key", default)` for every input, `HPRebar.McpBridge.Core.Scripting.ScriptArgs` spelled in full when a helper takes it as a parameter | CLAUDE.md "Seed/tool code contract" |
| RV4 | `SeedLibraryTests` compile every seed against the Revit API reference assemblies with the bridge's exact imports — the wrapper mirrors the bridge or a script passes the test and fails in Revit | `HPRebar/HPRebar.Mcp.Server.Tests/SeedLibraryTests.cs` |
| RV5 | Caller mistakes throw `ArgumentException` (excluded from the stability window); host failures `InvalidOperationException` | CLAUDE.md registry section |
| RV6 | The bridge is never a sandbox: guard deny-list + opt-in + audit; no reflection, I/O, process, threads in scripts | CLAUDE.md (ADR-04 of plan 260912-1521) |
| RV7 | Live checks run on a **model copy** in a second Revit; every script refuses unless `doc.PathName` is under the run folder | user rule 2026-10-04; `HPRebar/tools/golden-run/scripts/` |

Runtime quality rules Q-B1…Q-W5: core §13. Builds: `Debug.R23…R27`, `-p:DeployAddin=false` while Revit is open.
