# Phase 03 — Build-verify + deploy

**Priority:** P0 (HARD-GATE) · **Status:** ☐ · **Depends on:** Phases 01–02

## Overview

Verify the add-in builds across the targeted Revit versions and the TS server compiles. Debug builds auto-deploy
to `%ProgramData%\Autodesk\Revit\Addins\<version>\` via `<DeployAddin>true</DeployAddin>`.

## Version matrix (current project configs: R23, R25, R26, R27 — see Open Question re: R24)

| # | Command | Expect |
|---|---------|--------|
| 1 | `dotnet build "RevitAIApp/MyRevitAIApp/MyRevitAIApp.csproj" -c Debug.R27` | 0 err (newest, .NET 8) |
| 2 | `dotnet build … -c Debug.R26` | 0 err |
| 3 | `dotnet build … -c Debug.R25` | 0 err (Revit 2025) |
| 4 | `dotnet build … -c Debug.R23` | 0 err (oldest, net48 — exercises records + IsExternalInit shim path) |
| 5 | `cd mcp/server && npm run build` | no TS errors; `build/tools/duplicate_sheets.js` exists |
| 6 | *(if R24 added)* `dotnet build … -c Debug.R24` | 0 err (net48) |

> No xUnit changes — the 14 NamingRuleEngine tests are unaffected (SheetDuplicator untouched). Run
> `dotnet test` only as a regression sanity check if convenient.

## Deploy

Debug build of each config deploys the merged add-in DLL (with the new command compiled in) to that version's
Addins folder. The MCP server is in-process → no separate commandset DLL / `command.json` copy step.

## Todo

- [ ] Rows 1–5 green (row 6 only if R24 decision = add)
- [ ] Confirm deployed add-in DLL timestamp updated for the active version
- [ ] On failure: fix in the owning phase, re-run from the failing row (do not skip)

## Success criteria

- All targeted configs build; TS server builds and lists the new tool on start.

## Risks

- **net48 (R23/R24):** the DuplicateSheet records + `IsExternalInitShim` already build on net48 in this project
  (shipped R23). The new command/handler use no init-only records of their own → low risk.
- **`McpSocketService` using:** missing `using MyRevitAIApp.McpServer.DuplicateSheets;` → CS0246. Caught at row 1.

## Open questions

- **R24:** add `Debug.R24/Release.R24` to `MyRevitAIApp.csproj` (`<Configurations>` + a `REVIT2024_OR_GREATER`
  PropertyGroup, net48) to literally support Revit 2024 — or keep R23/R25/R26/R27? Pending user confirm (plan Q1).
