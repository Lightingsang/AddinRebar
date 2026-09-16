# ETABS MCP — Plan Only: A Fourth Host That Is Neither an Add-In Nor Transactional

**Date**: 2026-09-16 23:30  
**Severity**: Low (planning; no source code; red team rewrote the safety core of ADR-02 before any line was written)  
**Component**: plans/260916-2152-etabs-mcp-2026 (plan, architecture, 5 ADR, phases 0–4) / McpShared (13 additive edits planned, none made)  
**Status**: Planned. Not implemented, not built, not tested, nothing verified against ETABS. `HPEtabs/` does not exist.

## Tình huống

The brief asked for an ETABS MCP shaped like the Revit/AutoCAD/Navisworks ones, in a new `HPEtabs/` folder, with three fixed
questions to answer first: bridge or no bridge (ETABS's OAPI is out-of-process COM, so nothing has to load into ETABS.exe), what
replaces `dryRun`/rollback in a program that has **no transaction and no undo**, and how to reference `ETABSv1.dll` when CSI ships no
NuGet package. Read-only probes on the dev machine settled more than expected before any research: ETABS **22** (v22.7.0.4095 — CSI
numbers versions, the user had said "2022"), `ETABSv1.dll` is a **managed netstandard2.0 wrapper** with zero `[ComImport]` that attaches
through P/Invoke `oleaut32!GetActiveObject`, ETABS.exe is the COM `LocalServer32`, and ETABS itself runs on **.NET 8** — so a net8/net10
client needs no interop shim at all. The API CHM only opened with 7-Zip (`hh.exe -decompile` wrote nothing); its 1 707 topics became the
citation source for every OAPI claim.

## Quyết định

- **ADR-01 — B′, not A.** A bridge stays, but as a **standalone WPF app** the user launches beside ETABS, hosting the unchanged
  `McpBridgeHost`/`PipeListener`/`RequestDispatcher` and an `EtabsExecutor : IBridgeExecutor`; the server is a copy of the AutoCAD server's
  shape and never references the API. Option A (server hosts the engine in-process, no pipe) is technically open — `IRevitBridgeClient`
  and `IBridgeExecutor` are real seams — and was rejected on process lifetime: every Claude Code session spawns its own server, which on a
  model with no transactions means N COM owners, N opt-in windows and no serialisation of writes. One user-owned process gives one queue
  and one opt-in, and keeps server tests buildable on machines without ETABS.
- **ADR-02 — tiers + snapshot instead of rollback.** R (read-only) / W (write, `.EDB` snapshot before the run) / D (destructive: unlock,
  `RunAnalysis`, file ops, deletes — second checkbox, 600 s, refused when off). `dryRun` on a writing script is a **static preview**, never
  an execution. Attach only to the user's running instance; `CreateObject`, `ApplicationExit`, `Helper` denied outright.
- **ADR-03** — install dir from the CLSID `LocalServer32` registry value (there is no `HKLM\SOFTWARE\Computers and Structures` key),
  `Private=false` + runtime `AssemblyLoadContext.Resolving`, no CSI binary ever in the repo; seed compile-check lives in the **server**
  tests so its skip is observable on a machine without ETABS.
- **ADR-04** — one STA worker owns the COM proxy; `ct` is cooperative and cannot interrupt a single OAPI call; units forced to
  `kN_mm_C` per run and restored; globals `sapModel, etabs, units, ct, log, progress, args`.

## Red team đã đổi gì (36 finding → 14, tất cả có file:line)

The first draft's safety core did not survive the reviewers, and that is the lesson worth keeping:

- The R tier was a **prefix rule** (`Set|Add|Delete|…`). The real API has `ExportFile`, `GetTableForDisplayCSVFile` (a `Get*` that writes a
  file), `EditGeneral.Move`, `StartDesign`, `ModifyUndeformedGeometry` — all would have run as "read-only" with no snapshot. Now R is an
  **allow-list minus any member with a path parameter**, unknown members default to D, and the tier table is generated from the CHM
  index and pinned by a test.
- `isQuiescent` used `IsWindowEnabled(hwnd)`; a closed ETABS leaves a dead handle, so the bridge would have refused every request —
  including Attach — forever. Liveness is now eager (`Process.Exited`) and Attach/Detach bypass the queue.
- `ExecuteRequest.Snapshot` (an AI-controlled opt-out) had **no code path that could set it**, and `ExecuteResult.Snapshot` carried a
  full `%LocalAppData%` path to the model. Snapshot is unconditional; the result carries a file name.
- A static preview returning `isError=false` would have let `test_tool` mark W tools *tested* without executing them; a `DESTRUCTIVE`
  refusal returned as an error result would have self-quarantined `run_analysis` after five "please tick the box" round trips. Preview is
  now an error with diagnostic `PREVIEW`; the refusal is JSON-RPC `-32001`.
- Single-file publish of the bridge would have killed Roslyn (`Assembly.Location == ""`). Bridge publishes as a folder.
- The engine never applies `profile.DefaultVersion`; `BridgeOptions.HostVersion` defaults to 2026 and the three hosts never noticed. One
  additive `.Configure` line in phase 0.

## Bài học

- Reflect over the vendor DLL and read the registry before writing a single architecture sentence; here it removed the "does net10 need
  COM interop" question in ten minutes.
- A deny/allow rule for a host API must be derived from the API's member list, not guessed from naming habits — and pinned by a test that
  fails when the vendor adds a member.
- Red-team the safety design *before* the code exists; every accepted finding above would have been a rewrite after phase 2.
- Tool-side gotchas: the Bash tool's heredoc mangles backslashes (use Write for files with `\\`), `hh.exe -decompile` is unreliable
  (7-Zip extracts CHM), `ck` CLI is absent so plan files are written directly as in every previous plan.

## Bước tiếp theo

Phase 0 (engine additive, no ETABS needed) → phase 1 needs the user to open ETABS 22 with a throwaway model and approve two write
probes (`Save(tmp)`, `SetModelIsLocked` round trip) before the spike. Plan: `plans/260916-2152-etabs-mcp-2026/plan.md`.

## Phase 0 thực thi cùng đêm (Implemented + Built + Tested)

16 engine edits in `McpShared/` (+157/−14), all additive, 34 new tests (128 → 162), the four other suites unchanged
(60 / 109 / 49; AutoCAD moved 136 → 153 because a parallel AEC session added seeds), `tools/list` of the Revit and
Navisworks exes byte-identical before/after and the 37 pre-existing AutoCAD tools identical too, `Server.Core.dll`
hash changed, three Debug host solutions build. Two things the code review (9/10) added beyond the table, both kept
one-liners: `ScriptGuard.IsDeniedNamespace` now strips `global::` — `global::System.IO.File.WriteAllText(...)` had
been walking past the namespace deny-list of every host — and `ResultFormatter` reduces `ExecuteResult.Snapshot` to
a file name. Lesson: the engine had never applied `profile.DefaultVersion`; the three hosts only worked because
2026 is the hard-coded default. Reports: `plans/260916-2152-etabs-mcp-2026/reports/phase-00-*.md`.
