# Plan — `duplicate_sheets` MCP Command (in-process, no WPF form)

> Status: DRAFT for review (v2 — rewritten after MCP restructure) · 2026-06-14
> Goal: expose the existing **Duplicate Sheets** feature as an MCP tool so an AI agent can batch-duplicate
> sheets via JSON params — **no WPF dialog**. Reuse the ribbon feature's services directly.

## ⚠️ Architecture changed since v1 — plan rewritten

The vendored `mcp/plugin` + `mcp/commandset` (separate DLLs, Reflection, `command.json`) were **merged into the
main add-in** `RevitAIApp/MyRevitAIApp/` as an in-process **`MCP Server/`** feature folder. Consequences:

- **Single assembly.** MCP code + DuplicateSheet feature now share project `MyRevitAIApp` (namespace root
  `MyRevitAIApp.McpServer`). → The v1 problems **vanish**: no cross-project cherry-pick, no `<Compile Include>`,
  no `<Using>` injection, no IsExternalInit cross-link.
- **Serilog already referenced** by the project → `SheetDuplicator` works as-is. **The v1 logging-agnostic refactor
  is NOT needed** — shipped code stays untouched.
- **Registration is in code**, not `command.json`: [`MCP Server/McpSocketService.cs`](../../RevitAIApp/MyRevitAIApp/MCP%20Server/McpSocketService.cs) `RegisterAllCommands()` (lines ~62–104) news up each `XxxCommand`. `command.json` no longer exists.
- **TS relay unchanged** at `mcp/server/` — `register.ts` auto-discovers `tools/*.ts`.
- **SDK is in-repo source:** `MCP Server/Sdk/` (`ExternalEventCommandBase`, `IRevitCommand`, `IWaitableExternalEventHandler`, `ICommandRegistry`).
- **Model already scaffolded:** `MCP Server/Models/Prediction3D/DuplicateSheetsParameters.cs` (namespace `MyRevitAIApp.McpServer.Models`). Reuse as-is.

## Confirmed decisions (user, 2026-06-14)

1. **Naming = mirror number+name.** `prefix/suffix/find/replace` → both number & name; sequence → number only.
2. **Mode = accept BOTH** `JustSheet` + `Duplicate` (→ `DuplicateMode.Duplicate`).
3. **No cap.** Process the whole `sourceSheetNumbers` batch in one transaction.
4. **Versions = Revit 2024 + 2025 + 2026.** ⚠️ See Open Question — project currently targets **R23, R25, R26, R27 (no R24)**.

## Reference triad (new structure)

`MCP Server/Get Document Info/` → `GetDocumentInfoCommand` (`MyRevitAIApp.McpServer.GetDocumentInfo`) + `…EventHandler`
(`IExternalEventHandler, IWaitableExternalEventHandler`), registered at `McpSocketService.cs:74`.

## Phases

| # | Phase | File | Status |
|---|-------|------|--------|
| 1 | C# command + handler in `MCP Server/Duplicate Sheets/` + register | [phase-01](phase-01-command-handler-register.md) | ☐ |
| 2 | TS tool `mcp/server/src/tools/duplicate_sheets.ts` | [phase-02](phase-02-ts-tool.md) | ☐ |
| 3 | Build-verify (R23/R25/R26/R27 [+R24?]) + npm build + deploy | [phase-03](phase-03-build-verify-deploy.md) | ☐ |
| 4 | End-to-end in Revit + docs/memory | [phase-04](phase-04-e2e-and-docs.md) | ☐ |

## Files touched (summary)

**Create:** `MCP Server/Duplicate Sheets/DuplicateSheetsCommand.cs`, `MCP Server/Duplicate Sheets/DuplicateSheetsEventHandler.cs`, `mcp/server/src/tools/duplicate_sheets.ts`.
**Edit:** `MCP Server/McpSocketService.cs` (+1 `RegisterCommand` line + `using`), `MyRevitAIApp.csproj` only **if** R24 added.
**Reuse as-is (same assembly, no edit):** `DuplicateSheet/Services/{NamingRuleEngine,SheetDuplicator}.cs`, `DuplicateSheet/Models/*`, `MCP Server/Models/Prediction3D/DuplicateSheetsParameters.cs`.
**Untouched (v1 refactor cancelled):** `SheetDuplicator.cs`, `DuplicateSheetsViewModel.cs`.

## Open questions

1. **🟡 R24 / Revit 2024:** project targets R23, R25, R26, R27 — **no R24**. You asked for 2024+2025+2026. 2025/2026 ✓.
   Add a `Debug.R24/Release.R24` config (net48, like R23) to literally support Revit 2024, or keep the current matrix?
2. **🟢 Recovered:** the v1 "missing mcp/ files" alarm was the restructure (code moved into the add-in), not data loss.
3. Structured-log loss — N/A now (SheetDuplicator untouched, keeps Serilog).
