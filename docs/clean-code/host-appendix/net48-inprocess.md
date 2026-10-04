# Host appendix — .NET Framework 4.8 in-process plugins (Navisworks, Tekla)

> Read with [HP_CLEAN_CODE_CORE.md](../HP_CLEAN_CODE_CORE.md). Scope: `HPNavis/`, `HPTekla/`, and the `net48` assets of `McpShared/` they consume. Sources as of 2026-10-04 (`plans/261004-1005-hp-clean-code-ai-tools/reports/map-04-net48-inprocess.md`).

## Shared rules

| Id | Rule | Source |
|---|---|---|
| NI1 | No `AssemblyLoadContext` on .NET Framework: plugin dependencies resolve only through `PluginAssemblyResolver` (allow-list + same major version, not newer than the shipped file). A package added to Contracts or Bridge.Core updates that list in the same change | `HPNavis/HPNavis.McpBridge/PluginAssemblyResolver.cs:17-25`; `HPTekla/HPTekla.McpBridge/PluginAssemblyResolver.cs:17-27` |
| NI2 | Code in the `net48` assets compiles for both targets: Polyfill for `init`/records, `IReadOnlyCollection` not `IReadOnlySet`, every net48-only line behind `#if NET48`; contract fields are plain types (string severities, no enums on the pipe) | `McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj`; `AnalyzerProfile.cs:71` |
| NI3 | Host calls run on the host's main thread through `MainThreadQueue(expireWithoutTicks: true)` — the idle event stops under a native modal; busy grace 8 s | `HPNavis/HPNavis.McpBridge/NavisMainThreadExecutor.cs:58-71`; `HPTekla/HPTekla.McpBridge/TeklaThreadDispatcher.cs:36-59` |
| NI4 | The bridge owns the only transaction: a script never commits, rolls back or touches the undo stack (Navis guard: `Transaction`, `BeginTransaction`, `Undo`, `Redo`, `Rollback`; Tekla guard: `CommitChanges`) | `GuardProfile.cs:54-80, 200-224` |
| NI5 | Heavy or destructive operations need a second opt-in, and analysis screens with it OFF — a stored tool never depends on a session checkbox | `HPNavis/HPNavis.McpBridge/NavisMainThreadExecutor.cs:192-198`; `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs:375-381` |
| NI6 | No interactive UI from scripts (`MessageBox`, `System.Windows.Forms`, Tekla `Picker`/`Pick*`/`Tekla.Structures.Dialog`) | `GuardProfile.cs` |
| NI7 | Caller errors → `ArgumentException`; host/API failures → `InvalidOperationException` | seed contracts |

## Navisworks
| Id | Rule | Source |
|---|---|---|
| NI8 | `auto` = one undo entry `MCP: <label>`; `dryRun` = commit then `Document.Rollback()` only when `NextUndo` is the bridge's own entry and the fingerprint changed; a heavy call is never `dryRun` | `HPNavis/HPNavis.McpBridge/Service/NavisScriptRunner.cs`, `HPNavis/HPNavis.McpBridge/Service/NavisUndoDecision.cs:49-65` |
| NI9 | `Search` + `SearchCondition` with `Locations = DescendantsAndSelf` + `PruneBelowMatch`; walks bounded by `Take` | `HPNavis/HPNavis.McpBridge.Tests/SeedLibraryCompileTests.cs:80-86` |
| NI10 | `VariantData` read by type (`IsDisplayString` before `ToDisplayString()`, which throws otherwise); mm via `units`; collections capped at 200 items | CLAUDE.md "HPNavis MCP Bridge" |

## Tekla
| Id | Rule | Source |
|---|---|---|
| NI11 | Native savepoint: `Operation.SetTestSavePoint()` before every run, `RollbackToTestSavePoint` on `dryRun` and on any exception; `CommitChanges(label)` issued by the bridge only | `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs:22, 178-221` |
| NI12 | A `.db1`/`.db2` snapshot before any write/destructive run; a failed snapshot fails the run (today it is only logged — H-07) | `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs:151-161` |

Known gaps: H-05 — the Tekla executor commits by tier and never reads `request.Transaction`, so `none` + a write still commits; H-07 — a failed snapshot does not stop the write.
Tests: `HPRebar.McpBridge.Core.Net48Tests`, `HPNavis.McpBridge.Tests` (net48, needs Navisworks installed), `HPNavis.Mcp.Server.Tests`, `HPTekla.Mcp.Server.Tests` (compile checks skip without Tekla). Build with Navisworks open: `-p:DeployPlugin=false`.
