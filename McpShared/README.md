# McpShared — host-neutral libraries for the HP MCP bridges

Not an MCP server and not an add-in. This folder holds the code that every HP MCP shares:

| Project | TFM | Contents |
|---|---|---|
| `HPRebar.Mcp.Contracts` | netstandard2.0 | JSON-RPC envelope, execute/context/analyze DTOs, pipe naming, error codes. The only assembly both a server exe and a bridge add-in reference. |
| `HPRebar.McpBridge.Core` | net8.0 · net48 | Bridge half that never touches a host API: named-pipe listener + dispatcher, Roslyn guard/compiler/cache, `ScriptArgs`, `ScriptUnits`, analyzer, settings, audit, bridge host state machine, status view model. `net48` is for hosts still on .NET Framework (Navisworks 2026); everything net48-specific sits behind `#if NET48` (pipe ACL via `PipeSecurity`, `Stopwatch` clock, `IReadOnlyCollection` instead of `IReadOnlySet`) so the net8.0 asset is unchanged. |
| `HPRebar.Mcp.Server.Core` | net10.0 | Server half that never touches a host API: server bootstrap, pipe client, result formatter, execute/context services, tool registry engine, registry meta tools, `toolify_run` prompt, registry CLI. |
| `HPRebar.Mcp.Server.Core.Tests` | net10.0 | xUnit v3 tests of the engine over a real named pipe with a fake executor and a test host profile. |
| `HPRebar.McpBridge.Core.Net48Tests` | net48 | The bridge engine as a .NET Framework host sees it: `ScriptGuardTests`/`MainThreadQueueTests` linked verbatim, plus net48-specific compiler and raw-NDJSON pipe tests (the `PipeSecurity` shim). |
| `tools/` | Python 3 | Host-neutral harness scripts, not a project: `mcp-call.py` (one JSON-RPC request per process — `python McpShared/tools/mcp-call.py <exe> tools/list`), `mcp-session.py` (`Server` class: one long-lived session with `send`/`wait`, `tool`, `prompt`, `list_changed_since`) and `harness_common.py` (`Checklist` bookkeeping + summary/exit code, `utf8_console`, `ok`/`short`; `python McpShared/tools/harness_common.py` self-tests it). Every HP MCP harness imports them by relative path (`HPNavis/tools/harness/`, `HPAutoCad/tools/harness/`); nothing else in the repo carries a copy. |

Consumers: `../HPRebar/` (Revit MCP), `../HPAutoCad/` (AutoCAD MCP) and `../HPNavis/` (Navisworks MCP — the `net48` consumer, plan complete 2026-09-15). The dependency direction is always
MCP folder → `McpShared/`; an MCP folder never references another MCP folder.

Rules (the assembly rule is enforced by `HostNeutralityTests.Shared_assemblies_reference_no_host_api`):

- No `PackageReference` to Autodesk packages, no `using Autodesk.*` — no host *assembly* ever.
- Host names reach user-facing text only through `IHostProfile` (server) and the `hostName` /
  `GuardProfile` / `AnalyzerProfile` parameters (bridge core). Per-host *data* (deny-list names, default
  imports, the Revit defaults every constructor falls back to) may live here because it is plain strings
  the tests of every host need without referencing that host's API.
- Contracts changes must stay wire-compatible with every deployed bridge: add fields, never rename or
  remove them.

Assembly names keep the historical `HPRebar.*` prefix; renaming to a neutral prefix is a separate,
atomic cleanup once both MCPs are green.

```bash
dotnet build McpShared/McpShared.slnx
cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests
cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests   # runs the net48 exe under the same MTP runner
```

`dotnet test` must run with the current directory inside this folder: `global.json` here pins the
Microsoft.Testing.Platform runner, and from the repository root (no `global.json`) the command finds
no tests and still exits 0.
