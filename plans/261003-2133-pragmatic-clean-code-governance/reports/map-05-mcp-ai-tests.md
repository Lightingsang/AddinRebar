# Map 05 — AI integration (Revit MCP) + test projects

Read-only map, 2026-10-03. No build/test run. Counts = grep of attributes / `[InlineData]` rows, not runner output.
Abbrev: `B/` = HPRebar/HPRebar.McpBridge/, `S/` = HPRebar/HPRebar.Mcp.Server/, `MS/` = McpShared/.

## 1. AI/LLM integration architecture

```
Claude Code ──stdio JSON-RPC (MCP SDK 2.2.0)──▶ HPRebar.Mcp.Server.exe (net10, child of the AI)
   ──named pipe "hprebar-mcp-r2026" (NDJSON JSON-RPC 2.0, methods "revit.*")──▶ Revit.exe
        HPRebar.McpBridge (net8, R25/R26 only, own ALC "HPRebar.McpBridge", Roslyn loose DLLs)
          pipe thread : PipeListener → RequestDispatcher (MS) → handler.ExecuteAsync → ScriptGuard → ScriptCompiler
          Revit thread: ExternalEvent.Raise → handler.Execute → ScriptRunner → TransactionGroup "MCP: <label>"
```
- Pipe name: MS/HPRebar.Mcp.Contracts/PipeNaming.cs:13 (`hprebar-mcp-r` + version), :75. Method prefix `revit.` (JsonRpcMethods.cs:35).
- No LLM call anywhere in the product: the AI is the *client*; the server only exposes tools/prompts/resources. No HTTP/network code in either project (stdio + local pipe only).
- Tool surface: 4 core (2 in S/Hosts/Revit + `inspect_type`, `cancel_execution` in engine) + 8 registry meta tools (engine) + **21 embedded seeds** (S/Registry/SeedLibrary/{Annotation 3, Architecture 6, Data 3, Generic 4, Structure 1, View 4}; 13 `auto`, 8 `none`) = 33; live server also lists user-approved `set_mark_from_comments` → 34 (matches CLAUDE.md).
- Where LLM-facing text lives:
  - Tool descriptions: `[Description]` attributes — S/Hosts/Revit/ExecuteRevitCodeTool.cs:26-37 (~1.4 KB string), RevitContextTool.cs:22-25; engine meta tools in MS/HPRebar.Mcp.Server.Core.
  - Prompts: S/Hosts/Revit/RevitScriptPrompts.cs (`revit_query_template`, `revit_modify_template`, few-shot ChatMessage[]), engine `toolify_run` (MS/…/Prompts/ToolifyPrompts.cs:29).
  - Resources: `revit://document/info`, `revit://selection` (S/Hosts/Revit/RevitDocumentResources.cs:14,18); `registry://tools[/{name}]` (MS/…/Resources/ToolRegistryResources.cs:14,28).
  - Script contract summary for registry: MS/…/Hosts/HostProfile.cs:30-33. Seed descriptions + JSON schemas: 21 × `tool.json`.
- Configuration sources:
  | Source | Where | Content |
  |---|---|---|
  | appsettings.json (beside exe; ContentRoot = exe dir, MS/…/Bootstrap/McpServerHost.cs:34-37) | S/appsettings.json | `Bridge` (RevitVersion 2026, ConnectTimeoutMs 2000, ExtraTimeoutSeconds 5, MaxSourceBytes 32 KB, MaxMessageBytes 4 MB, Ping 10 s, reconnect 5), `Registry` (PublishPolicy manual, SearchTopK 5, RunWindow 50, quarantine 5 runs / 0.4, InstallSeeds, WatchLibrary, KeepAdhocRuns 500) |
  | Env vars `HPREBAR_MCP_*` | McpServerHost.cs:43 | e.g. `Bridge__RevitVersion`, `Registry__LibraryPath/DbPath` |
  | CLI args | Program.cs:8 → `registry <cmd>` | human approval side of the registry |
  | Bridge settings.json | `%AppData%\HPRebar\McpBridge\settings.json` (MS/…/Model/BridgeSettingsStore.cs:25,30) | AutoStartListener etc.; ExecutionEnabled forced false on load (:43, :67) |
- File / DB access:
  - SQLite `registry.db` (WAL+FTS5) + `tools-library\<Category>\<name>\` under `%AppData%\HPRebar\McpServer\` (MS/…/Registry/Model/RegistryOptions.cs:21-44; ToolRegistryDb.cs:2,24; FileSystemWatcher ToolLibraryStore.cs:162).
  - Audit JSON-lines incl. **full script source** per run: `%AppData%\HPRebar\McpBridge\audit\audit-*.log` (AuditLogger.cs:36; written from B/McpBridgeExternalEventHandler.cs:199).
  - Serilog file `%LocalAppData%\HPRebar\McpBridge\logs\mcpbridge-.log`, 7 days, `shared: true` (B/Application.cs:191-203).
- Dependency direction (verified by csproj): B → MS/HPRebar.Mcp.Contracts + MS/HPRebar.McpBridge.Core; S → MS/HPRebar.Mcp.Server.Core (→ Contracts). MS never references HPRebar/ or Autodesk.* (csproj comments + no path refs). S.Tests → S + Server.Core + McpBridge.Core + Contracts + linked MS/HPRebar.Mcp.Server.Core.Tests/Fakes/FakeRevitExecutor.cs.

### McpShared boundary used by HPRebar (grep counts of type names in B + S)
`ExecuteResult` 16, `ScriptArgs` 11, `TransactionModes` 7, `ScriptCompiler` 5, `McpBridgeHost` 4, `ContextResult` 4, `CancelResult` 4, `BridgeSettings` 4, `TypeInspector` 3, `AuditLogger` 3, `ScriptProgress` 3, `ExecuteRequest` 3, `IBridgeExecutor` 2, `BridgeSettingsStore` 2, `McpBridgeStatusViewModel` 2, `LastRunInfo` 2, `SafeText` 2, `HostProfile` 2, `ExecuteCodeService` 2, `ContextService` 2, `ElementInfo`/`ChangedCounts` 2, `ScriptGuard`, `ScriptAnalyzer`, `ScriptDiagnostic`, `PipeNaming`, `BridgeJson`, `McpServerHost`, `AuditEntry`, `Inspect*/Analyze*` 1 each.
- **Not used by Revit:** `MainThreadQueue` (AutoCAD/Navis/ETABS path) — Revit bridge has its own `ConcurrentQueue` + ExternalEvent (B/McpBridgeExternalEventHandler.cs:24,167). `HostScriptContracts.RevitImports/RevitGlobals` — not read by B or S (see smells 1-3).
- MS size (.cs LOC, excl. bin/obj): Contracts 13 files / 1 008; McpBridge.Core 23 / 2 558; Mcp.Server.Core 34 / 3 836; Server.Core.Tests 29 / 6 198; McpBridge.Core.Net48Tests 3 / 624. Engine also carries Excel/Navis/ETABS/Civil host constants (e.g. Server.Core.Tests/ExcelMilestone1ChallengerTests.cs:282).

## 2. Class inventory

| Class | File | LOC | Layer | Responsibility | Revit API |
|---|---|---|---|---|---|
| Application | B/Application.cs | 212 | entry + composition root | Serilog, ribbon button + icon/theme, builds compiler/runner/inspector/audit/handler/host, wires ViewActivated/DocumentClosing/ApplicationInitialized, self-check, reorders "HPRebar" tab panels | Y (+AdWindows) |
| McpBridgeExternalEventHandler | B/McpBridgeExternalEventHandler.cs | 221 | Revit-thread adapter + execute pipeline | `IExternalEventHandler` + `IBridgeExecutor`: busy gate, guard, compile, queue/raise, cancel, audit, run events, active-doc title | Y |
| McpBridgeRequest | B/McpBridgeRequest.cs | 29 | model | queued `Func<UIApplication,CT,object>` + TCS | Y (signature) |
| McpBridgeCommand | B/McpBridgeCommand.cs | 59 | UI entry | opens modeless status window, static `_window` | Y |
| ScriptRunner | B/Service/ScriptRunner.cs | 192 | service | doc checks, globals, timeout, TransactionGroup/Transaction policy, dryRun rollback, result build | Y |
| ResultSerializer | B/Service/ResultSerializer.cs | 148 | service | JSON of script return, Revit-object converter, 64 KB truncation | Y |
| RevitContextReader | B/Service/RevitContextReader.cs | 73 | service (static) | `ContextResult` snapshot + `Describe(Element)` | Y |
| DocumentChangeCounter | B/Service/DocumentChangeCounter.cs | 36 | service | add/mod/del counts via `DocumentChanged` | Y |
| AutoDismissFailurePreprocessor | B/Service/AutoDismissFailurePreprocessor.cs | 37 | service | delete warnings, roll back on errors | Y |
| ScriptingSelfCheck | B/Service/ScriptingSelfCheck.cs | 44 | service (static) | startup compile+run probe, logs "self-check OK" | Y |
| ScriptGlobals | B/Model/ScriptGlobals.cs | 46 | model (script API) | lowercase public fields `doc/uidoc/app/uiapp/ct/log/progress/args` | Y |
| McpBridgeStatusView | B/View/McpBridgeStatusView.xaml(.cs) | 20 + 163 xaml | view | MaterialThemeBridge attach, CloseRequested; VM lives in MS (`McpBridgeStatusViewModel`) | N |
| (linked) RibbonIcons, MaterialThemeBridge, RevitHostTheme, IHostTheme, ThemeInfo | B/csproj:44-50 → HPRebar/HPRebar/Resources/** | — | shared UI | icon + theme, compiled into both add-ins | Y (theme) |
| Program | S/Program.cs | 8 | entry | `McpServerHost.RunAsync(args, RevitHostProfile.Instance)` | N |
| RevitHostProfile | S/Hosts/Revit/RevitHostProfile.cs | 12 | profile | `HostProfile.Revit.WithHostAssembly(this asm)` | N |
| ExecuteRevitCodeTool | S/Hosts/Revit/ExecuteRevitCodeTool.cs | 56 | MCP tool | name + description, delegates to `ExecuteCodeService` | N |
| RevitContextTool | S/Hosts/Revit/RevitContextTool.cs | 30 | MCP tool | delegates to `ContextService` | N |
| RevitDocumentResources | S/Hosts/Revit/RevitDocumentResources.cs | 21 | MCP resource | 2 resources over `ContextService` | N |
| RevitScriptPrompts | S/Hosts/Revit/RevitScriptPrompts.cs | 65 | MCP prompt (static) | persona + 2 few-shot prompts | N |
| 21 seeds | S/Registry/SeedLibrary/** | 1 248 (code.cs) | data (embedded) | script bodies compiled in Revit at run time; `<Compile Remove>` (S/csproj:30-31) | API in text only |

Totals: B 1 117 .cs LOC + 181 xaml; S 192 .cs LOC + 1 248 seed LOC (63 seed files).

## 3. Revit boundary in the bridge
- `ExternalEvent.Create(this)` in handler ctor (B/McpBridgeExternalEventHandler.cs:42), legal only because ctor runs inside `OnStartup` (B/Application.cs:110-130).
- Pipe thread: guard (:77) + Roslyn compile (:80) run before queuing; only the run is marshalled. `RunOnRevitThreadAsync` enqueues + `Raise()` (:167-178); refused only when status ∉ {Accepted, Pending}.
- Revit thread: `Execute` drains the queue (:145-163), updates active doc title (:147), completes TCS (`RunContinuationsAsynchronously`, B/McpBridgeRequest.cs:27).
- Busy: `Interlocked` `_busy` flag (:31, :66) — one script at a time; context reads are not gated.
- Transactions (B/Service/ScriptRunner.cs): `none` → no group; `auto` → TransactionGroup "MCP: <label>" (:76) + Transaction "MCP script" with failure preprocessor (:82-84); `manual` → group only, open transaction left = error (:94-95); dryRun → `group.RollBack()` (:102-105) else `Assimilate()` (:109); timeout/cancel/exception → `RollBack` helper (:168-191). Timeout 5–120 s cooperative (:50); script run blocking `.GetAwaiter().GetResult()` on Revit thread (:88).
- Static state: `McpBridgeHost.Current` (MS/HPRebar.McpBridge.Core/Host/McpBridgeHost.cs:63-70); `McpBridgeCommand._window` (B/McpBridgeCommand.cs:24); `Log.Logger`; `RevitContext.UiApplication` (B/Application.cs:146); `RevitHostTheme.Instance`; static classes `RevitContextReader`, `ScriptingSelfCheck`, `ScriptGuard`, `ScriptAnalyzer`, `SafeText`.

## 4. Dependencies / composition
- Server: `Microsoft.Extensions.Hosting` + DI built entirely in engine (MS/…/Bootstrap/McpServerHost.cs:49-61 singletons: `IRevitBridgeClient`, `ResultFormatter`, `ExecuteCodeService`, `ContextService`, `ToolLibraryStore`, `ToolRegistryDb`, `ToolManager`, `ToolLifecycleService`, `DynamicToolRegistrar`; `IHostProfile` :96). Tool/prompt/resource classes discovered by reflection `WithToolsFromAssembly(engine|HostAssembly)` (:77-82); Revit tools take services via primary constructors. S adds no registrations of its own.
- Bridge: no DI container; hand-wired `new` in `Application.CreateBridge` (B/Application.cs:112-151). Settings object shared by reference across UI/pipe/Revit threads.
- Hidden deps: `ResultSerializer` → static `RevitContextReader.Describe` (B/Service/ResultSerializer.cs:115); handler → static `ScriptGuard.Check` (:77), `ScriptAnalyzer.Run` (:126), `ScriptCompiler.Hash` (:200); `McpBridgeCommand` → `McpBridgeHost.Current` (:34).
- Revit-specific defaults in "host-neutral" engine: `HostProfile.Revit` (MS/…/Hosts/HostProfile.cs:14) is the default of every engine ctor.

## 5. Size metrics
- Production files > 300 lines: **none** in B or S (max: handler 221, Application 212, ScriptRunner 192). MS largest: ToolLifecycleService 283, ContextMessages 279, GuardProfile 264, RevitBridgeClient 263.
- Methods > 50 lines: `ScriptRunner.Run` B/Service/ScriptRunner.cs:31-161 (**131**); `McpBridgeExternalEventHandler.ExecuteAsync` :64-113 (50, borderline); `RevitObjectConverter.Write` B/Service/ResultSerializer.cs:98-146 (49).
- Seed script bodies > 50 lines: 12/21 — create_line_based_element 137, ai_element_filter 102, create_point_based_element 101, create_dimensions 99, create_surface_based_element 93, operate_element 88, create_structural_framing_system 70, create_grid 69, color_elements 63, get_current_view_elements 58, analyze_model_statistics 55, get_material_quantities 51.
- Test files > 300 lines: S.Tests/Registry/ToolRegistryTests.cs 481, ToolLifecycleTests.cs 318; Core.Tests KataRebarCalculatorTests 863, KataStressAdversarialTests 810, FoundationMeshCalculatorTests 460, KataSectionDrawingGolden 435, BeamAdditionalBarCalculatorTests 398, BarPolylineBuilderTests 392, BeamMainBarCalculatorTests 337, KataElevationTests 335, KataSupportTopBarTests 317.

## 6. Test inventory

| Project | Framework / runner | Files / LOC | Count (grep) | Covers |
|---|---|---|---|---|
| HPRebar/HPRebar.Core.Tests | xUnit v3 3.1.0, net8.0, MTP | 73 / 12 881 | 559 `[Fact]` + 70 `[Theory]` (351 InlineData + 2 MemberData) ≈ 910+ cases | pure `HPRebar.Core` maths + Themes text check |
| HPRebar/HPRebar.Mcp.Server.Tests | xUnit v3 3.1.0, net10, MTP | 3 / 1 005 | 34 Fact + 7 Theory → **109** cases (3 theories × 21 seeds = 63) | seed records, guard/args⇔schema, seed compile vs Revit 2026 ref DLLs (`Assert.SkipWhen` w/o NuGet cache), registry store/db/lifecycle/CLI, pipe round trip with fake |
| HPRebar/HPRebar.Tests | TUnit 1.61.38 + Nice3point.TUnit.Revit (`RevitThreadExecutor`), R25/R26 | 6 / 574 | 20 `[Test]` | ColumnRebar only (reader 6, validator 5, orchestrator 5, rebar creation 4) |
| MS/HPRebar.Mcp.Server.Core.Tests | xUnit v3, net10 | 29 / 6 198 | 198 Fact + 50 Theory (527 InlineData) | engine (boundary only, not audited) |
| MS/HPRebar.McpBridge.Core.Net48Tests | xUnit v3, net48 | 3 / 624 | 23 Fact + 3 Theory (32 InlineData) | engine net48 asset |

Core.Tests by folder: BeamRebar 101F/4T (21 rows), ColumnRebar 93F/6T (26), FoundationRebar 36F/15T (57), KataExport 76F/9T (38), KataRebar 250F/36T (209 + MemberData), Themes 3F.
- CLAUDE.md drift: says Core.Tests 448, TUnit "16 skip", Server.Core.Tests 164 — grep gives ≈ 910+, 20, 248 methods / 700+ cases. Not verified by a run.
- TUnit fixture missing: HPRebar/HPRebar.Tests/Fixtures/ holds only README.md; all 20 tests `Skip.Unless(ColumnStackFixture.Exists)` (e.g. ColumnStackReaderTests.cs:42) → 20/20 skip. HPRebar.Tests references only HPRebar (not McpBridge).
- Mocks: **no mocking library** in any test csproj (no Moq/NSubstitute/FakeItEasy/FluentAssertions). Fakes are hand-written: `FakeRevitExecutor : IBridgeExecutor` (99 LOC, linked file); builders/golden data in Core.Tests (TestSections, TestBeamData, FoundationTestData, TestKataData, KataRebarTestSheets, KataSectionDrawingGolden). Registry tests use real SQLite + temp dirs (ToolRegistryTests.cs:24, :59).
- Gaps: zero tests for B (ScriptRunner transaction matrix, ResultSerializer converters/truncation, RevitContextReader, handler busy/cancel/queue, DocumentChangeCounter, AutoDismissFailurePreprocessor) — live-verified only (CLAUDE.md 2026-09-12); no test pins Revit tool descriptions/prompts to `HostScriptContracts`; TUnit covers ColumnRebar only.
- Naming: 3 styles. Core.Tests ≈ 581 methods: 277 PascalCase sentence (all BeamRebar/ColumnRebar/KataExport), 68 Method_Scenario_Expected (FoundationRebar, part of KataRebar), 231 sentence_snake (most KataRebar, all S.Tests). Samples:
  - `A3By4GridProducesTenBars` — Core.Tests/ColumnRebar/BarLayoutCalculatorTests.cs:13
  - `ParseCover_VariousStrings_ReturnsGivenNumbersAndZeroForMissing` — Core.Tests/KataRebar/KataBarNotationParserTests.cs:138
  - `A_zero_step_in_the_settings_turns_the_stagger_off` — Core.Tests/KataRebar/KataTopBarStaggerTests.cs:113
  - `Argument_errors_are_the_callers_and_never_quarantine_a_tool` — S.Tests/Registry/ToolRegistryTests.cs:348
  - `RollingBackTheRunLeavesNoViewsBehind` — HPRebar.Tests/ColumnRebarOrchestratorTests.cs:96

## 7. Smells (facts only)

| # | Location | Category | Fact |
|---|---|---|---|
| 1 | B/Application.cs:28-34 | duplication | Revit import list hard-coded; MS/HPRebar.Mcp.Contracts/HostScriptContracts.cs:3-12 exists "so the bridge … read one list" but B never reads it. Re-typed also in S/Hosts/Revit/ExecuteRevitCodeTool.cs:34, RevitScriptPrompts.cs:18, S.Tests/SeedLibraryTests.cs:146-151 (5 copies). |
| 2 | HPRebar/HPRebar.Mcp.Server.Tests/SeedLibraryTests.cs:152 | comment noise (stale) | Says wrapper omits `HPRebar.McpBridge.Core.Scripting` to mirror `Application.ScriptImports`; B/Application.cs:33 includes it → test wrapper ≠ bridge imports. |
| 3 | S/Hosts/Revit/RevitScriptPrompts.cs:17 | duplication / drift | Persona globals omit `args`; `HostScriptContracts.RevitGlobals` (:51) and tool description (ExecuteRevitCodeTool.cs:28-31) include it. |
| 4 | B/Service/ScriptRunner.cs:31-161 | long method / SRP | `Run` = 131 lines: preconditions, globals + log cap, timeout linking, transaction policy, commit/rollback, serialization. |
| 5 | B/Service/ScriptRunner.cs:50, :117 | duplication / bug | 5–120 clamp hard-coded (engine uses `IHostProfile.MaxTimeoutSeconds`, MS/…/Services/ExecuteCodeService.cs:61); timeout message prints unclamped `request.TimeoutSeconds`. |
| 6 | B/McpBridgeExternalEventHandler.cs:22 | SRP | One 221-line class = `IExternalEventHandler` + `IBridgeExecutor` + guard/compile + busy gate + cancel + audit + run events + doc-title tracking. |
| 7 | B/McpBridgeExternalEventHandler.cs:42 | DIP / Revit-boundary | `ExternalEvent.Create` in ctor → handler cannot be constructed outside Revit; its queue/busy/cancel logic has no test. |
| 8 | B/McpBridgeExternalEventHandler.cs:167-178 | hidden behavior | Request's `CancellationToken` never registered on `Completion`; a request queued while Revit cannot run it stays awaited and `_busy` stays 1 until Revit idles (code trace; GIẢ ĐỊNH CHƯA XÁC MINH live). |
| 9 | B/McpBridgeRequest.cs:23; handler :91, :121 | type safety | Queue typed `Func<UIApplication,CT,object>`; results recovered by `as ExecuteResult` / `(ContextResult)` casts. |
| 10 | B/McpBridgeExternalEventHandler.cs:33, :147, :195 | shared mutable state | `_activeDocumentTitle` written on Revit thread, read on pipe thread (`Finish`) without volatile/lock; `StateChanged` raised from both threads. |
| 11 | MS/HPRebar.McpBridge.Core/Host/McpBridgeHost.cs:63-70; B/McpBridgeCommand.cs:24,34; B/Application.cs:76 | static state / service locator | `McpBridgeHost.Current` static singleton read by command and disposed via static in `OnShutdown`; window kept in `static _window`. |
| 12 | B/Service/ResultSerializer.cs:115 | hidden dependency | JSON converter calls static `RevitContextReader.Describe`; serializer ↔ context reader coupled. |
| 13 | B/McpBridgeExternalEventHandler.cs:199-210; B/Model/ScriptGlobals.cs:34 | long param list | `AuditEntry` 14 positional args, `LastRunInfo` 10, `ScriptGlobals` ctor 8. |
| 14 | B/Service/RevitContextReader.cs:12 | boolean flag | `Read(uiapp, includeSelection, executionEnabled)` — 2 bools; bridge opt-in state threaded through a Revit-thread reader. |
| 15 | B/Service/DocumentChangeCounter.cs:21-32 | hidden behavior | App-wide `DocumentChanged`, no document or `e.Operation` filter; disposed after `group.RollBack()` (ScriptRunner.cs:62, :104) — effect on dryRun counts GIẢ ĐỊNH CHƯA XÁC MINH. |
| 16 | B/Service/ScriptingSelfCheck.cs:33 | nullability | `uidoc?.Document!` / `uidoc!` push null into non-nullable `ScriptGlobals` fields at startup. |
| 17 | B/Application.cs:83, :160-184 | coupling / magic strings | Bridge reorders panels of tab "HPRebar" owned by main add-in, matched by literal Id/Title "HPRebar"/"MCP". |
| 18 | MS/HPRebar.Mcp.Server.Core/Services/IRevitBridgeClient.cs:10; MS/…/Models/BridgeOptions.cs:20; McpBridgeHost.cs:27 | naming | Host-neutral engine keeps Revit names: `IRevitBridgeClient`/`RevitBridgeClient`, `BridgeOptions.RevitVersion`, `McpBridgeHost.RevitVersion` ctor, `FakeRevitExecutor` shared by all hosts. |
| 19 | S/Registry/SeedLibrary/View/color_elements/tool.json:20 | comment noise (LLM-facing) | Description contains self-dialogue "Undo with operate_element ResetIsolate? No — …". |
| 20 | MS/HPRebar.McpBridge.Core/Model/BridgeSettings.cs:10-29 | shared mutable state | One `BridgeSettings` mutated on UI thread (`McpBridgeHost.ExecutionEnabled` setter) and read on pipe (RequestDispatcher.cs:156) and Revit thread (ScriptRunner.cs:42, :56); plain auto-props. |

**Status:** DONE
