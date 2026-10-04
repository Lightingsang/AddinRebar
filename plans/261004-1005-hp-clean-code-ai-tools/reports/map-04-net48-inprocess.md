# Map 04: net48 in-process hosts (HPNavis, HPTekla)

Read-only scout, 2026-10-04. No source, csproj or test edited, nothing built or run. Every claim cites `path:line`. Lines marked *(inference)* were reasoned, not verified.

## 1. net48 behaviour of the new contract fields (E14)

| Fact | Evidence |
|---|---|
| Contracts multi-targets `netstandard2.0;net48`. The net48 asset exists so that System.Text.Json binds to the net462 package asset (10.0.0.12). | McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj:4-8 |
| System.Text.Json `10.0.12` is the only runtime package. Polyfill `11.0.1` is `PrivateAssets=all` (source-only shims for record/init/required). | HPRebar.Mcp.Contracts.csproj:18, :20 |
| There is no `#if NET48` anywhere in Contracts. Every `#if NET48` lives in Bridge.Core: `MainThreadQueue` clock, `PipeListener` ACL, and the `IReadOnlyCollection`-vs-`IReadOnlySet` choice in `GuardProfile`/`AnalyzerProfile`. | grep; McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs:71-84, GuardProfile.cs:243, Host/MainThreadQueue.cs:56,79, Pipe/PipeListener.cs:78 |
| `AnalyzeResult` is a class with `{ get; set; }` properties. Each list defaults to `System.Array.Empty<T>()`. | McpShared/HPRebar.Mcp.Contracts/Messages/AnalyzeMessages.cs:18-42 |
| Positional records already ship in Contracts and already cross the net48 pipe: `ScriptDiagnostic(Line, Column, Id, Message)`, `CodeLiteral`, `ArgUsage`, and about 25 more. Their `init` accessors compile on net48 through Polyfill's `IsExternalInit`. | Messages/ExecuteResult.cs:63; AnalyzeMessages.cs:49,52; ContextMessages.cs:6-279 |
| One serializer for both ends. It sets camelCase, `PropertyNameCaseInsensitive`, and `WhenWritingNull`. It sets **no** `UnmappedMemberHandling`, so the STJ default `Skip` applies: an old server ignores `qualityAnalysed`/`qualityFindings`. It registers **no** enum converter. | McpShared/HPRebar.Mcp.Contracts/JsonRpc/BridgeJson.cs:14-23 |
| Navis gets STJ transitively through Contracts. Tekla pins STJ `10.0.12` itself, so both bridges carry the same version. | HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj:37 |

**Answers:**
- `public sealed record QualityFinding(string RuleId, string Severity, int Line, int Column, string Message);` compiles on net48 today. It has the same shape as `ScriptDiagnostic` (ExecuteResult.cs:63). STJ builds positional records through their constructor on the net462 asset, and the existing records already prove this live on the Navis pipe.
- Add the field as `public IReadOnlyList<QualityFinding> QualityFindings { get; set; } = System.Array.Empty<QualityFinding>();` beside the existing lists (AnalyzeMessages.cs:23-31). Do **not** use `IReadOnlySet`, which does not exist on net48 (AnalyzerProfile.cs:71-72). Do not use an `init`-only class property; it is not needed.
- `public bool QualityAnalysed { get; set; }`: when the field is missing on the wire (an old bridge), deserialisation leaves `false`. That is exactly the "quality not analysed" signal the contract asks for (§2 criterion 3), so it needs no nullable and no sentinel.
- Severity should be a **string**, not an enum. `BridgeJson` has no `JsonStringEnumConverter` (BridgeJson.cs:14-23), so an enum would travel as a number. A string matches `ScriptDiagnostic.Id`.
- An old bridge with a new server gets `false` plus an empty list (property initialisers). A new bridge with an old server has the extra fields skipped. Both directions are additive-safe *(verified by reading the options; no round-trip test exists, see §2)*.

## 2. `McpShared/HPRebar.McpBridge.Core.Net48Tests`

| Item | Evidence |
|---|---|
| TFM net48, xUnit v3 MTP exe, `AutoGenerateBindingRedirects` (the redirects a plugin cannot have) | HPRebar.McpBridge.Core.Net48Tests.csproj:6, :14-19 |
| ProjectReferences to Bridge.Core and Contracts: the **net48 assets** compile into the test | csproj:30-31 |
| Linked verbatim from the net10 suite: `ScriptGuardTests.cs`, `MainThreadQueueTests.cs`, `Fakes/FakeRevitExecutor.cs`. The comment explains why the compiler and analyzer tests are *not* linked: partial-name `Assembly.Load` fails on .NET Framework | csproj:35-41 |
| Own files: `ScriptCompilerNet48Tests.cs` (11 methods: compile/run/cache, Navis guard, Navis/Robot/Tekla analyzer, Stopwatch clock), `PipeListenerNet48Tests.cs` (4: ping, opt-in refusal, context, second listener; **no `analyze` round trip**), `TeklaMilestone1Challenger2Net48Tests.cs` (11: Tekla guard/analyzer edge cases) | ScriptCompilerNet48Tests.cs:35-198; PipeListenerNet48Tests.cs:38-102; TeklaMilestone1Challenger2Net48Tests.cs:165,234 |
| Count: 113 tests (CLAUDE.md, run 2026-10-03). By grep: 44 `[Fact]`/`[Theory]` methods + 30 `InlineData` in the linked ScriptGuardTests alone | grep |

**Answers:**
- A new walker file under `McpShared/HPRebar.McpBridge.Core/Scripting/` needs **no link**. Bridge.Core multi-targets `net8.0;net48` (McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj:6), so the walker compiles into the net48 asset automatically. Only *test* files are linked.
- A walker test written in `HPRebar.Mcp.Server.Core.Tests` reaches net48 in one of two ways:
  - (a) a new `<Compile Include Link>` line in the Net48Tests csproj (a csproj edit, Phase 2 scope); the test must avoid partial-name `Assembly.Load` and net10-only APIs; or
  - (b) a handful of net48 cases added to the existing `ScriptCompilerNet48Tests.cs`, which needs no csproj change.

  Recommend (b) plus one new `analyze` round trip in `PipeListenerNet48Tests.cs`. That round trip would prove that `qualityAnalysed`/`qualityFindings` serialise on the net48 asset, which no test covers today.
- `FakeRevitExecutor.Analyze` calls `ScriptAnalyzer.Analyze` (McpShared/HPRebar.Mcp.Server.Core.Tests/Fakes/FakeRevitExecutor.cs:83-86). If the quality walk runs inside `Analyze`, every fake-pipe test in the 10 servers and in Net48Tests gets the fields without changes.

## 3. Discovery-before-resolver risk

- Roamer reflects over every plugin type before any plugin code runs. The net48 Contracts asset exists because of this (CLAUDE.md:174; Contracts.csproj:4-7).
- `PluginAssemblyResolver` uses an allow-list of names that ship in the plugin folder. Every Roamer request arrives with `RequestingAssembly == null`, so the real gate is the version family: same major, not newer than the file (HPNavis/HPNavis.McpBridge/PluginAssemblyResolver.cs:17-25, :74-78).
- Tekla copies the resolver and adds `HPTekla.McpBridge` to the list. For its own requesters it allows a cross-major forward bind (HPTekla/HPTekla.McpBridge/PluginAssemblyResolver.cs:17-27, :77-91).
- **Risk if the change stays a POCO record plus a Roslyn `CSharpSyntaxWalker` inside Bridge.Core: none new.** Neither adds an assembly reference, because Roslyn 5.9 is already allow-listed (PluginAssemblyResolver.cs:19).
- Ways to break plugin load *(inference)*:
  - a new PackageReference in Contracts or Bridge.Core (P7 forbids it anyway);
  - a `[JsonConverter]`/attribute type from a package not on the allow-list;
  - a net10-only BCL call in the walker that Polyfill does not cover. That would be a compile error on net48, not a runtime one, because Bridge.Core builds net48.
- Bridge.Core's Polyfill reference is net48-only (HPRebar.McpBridge.Core.csproj:25). Collection expressions (`[]`) are already used in the analyzer (ScriptAnalyzer.cs:58,60), so the same style is safe.

## 4. How Navis and Tekla build `AnalyzeResult` (E1)

| Host | Path | Evidence |
|---|---|---|
| Dispatcher (shared) | `*.analyze` suffix → `_executor.Analyze(parameters)` → `JsonRpcEnvelope.Success` | McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs:122-127; IBridgeExecutor.cs:34 |
| Navis | `ScriptAnalyzer.Run(_compiler, code, GuardProfile.Navis, AnalyzerProfile.Navis)`, then `NavisHeavyGate().Check` with heavy **off**. Its diagnostics are **prepended to `GuardViolations` only**; every other field is untouched | HPNavis/HPNavis.McpBridge/NavisMainThreadExecutor.cs:192-198; Service/NavisHeavyGate.cs:54-62 |
| Tekla | `ScriptAnalyzer.Run(…, GuardProfile.Tekla, AnalyzerProfile.Tekla)`, then `TeklaTierAnalyzer.Analyze`. A Destructive tier prepends a `HEAVY` diagnostic at (1,1) to `GuardViolations` only | HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs:372-383 |
| Shared core | `Run` = `Analyze` (syntax facts) + guard + compile only when the guard passes | McpShared/HPRebar.McpBridge.Core/Scripting/ScriptAnalyzer.cs:21-35, :40-54 |

**Answer:**
- If the quality walk runs inside `ScriptAnalyzer.Analyze` (ScriptAnalyzer.cs:40-54, which already parses the tree), both executors pass the new fields through unchanged after a **rebuild and redeploy** of the plugin. No host code changes.
- Until then, the deployed Navis and Tekla plugins answer with `qualityAnalysed` missing, which the server reads as `false` (the contract's accepted assumption).
- Deploy conditions:
  - Navis: `dotnet build HPNavis/HPNavis.slnx -c Debug` with Roamer closed (CLAUDE.md, HPNavis install line).
  - Tekla: `-p:DeployPlugin=true` (HPTekla.McpBridge.csproj, `DeployPlugin` default false) with TeklaStructures.exe closed *(inference: the DLL is locked while loaded)*.
- Design note: quality findings exist even when the guard or heavy gate fails, because `Analyze` runs before `Check` (ScriptAnalyzer.cs:23-24). That is harmless, since the guard error already blocks.

## 5. Seeds and seed tests

| | HPNavis | HPTekla |
|---|---|---|
| Seeds (`code.cs`) | 12 (36 files) | 12 (36 files) |
| Largest `code.cs` | 70 lines: Clash/create_and_run_clash_test | 59 lines: Model/select_objects |
| Total seed lines | 443 | 525 |
| Existing line cap in tests | ≤ 80 lines (HPNavis/HPNavis.Mcp.Server.Tests/SeedLibraryStructureTests.cs:97) | ≤ 120 (HPTekla/HPTekla.Mcp.Server.Tests/SeedCatalogTests.cs:25, :213) |
| Structure test (net10, embedded resources) | `SeedLibraryStructureTests` loads `SeedLibrary/` manifest resources (:30-55); `ToolValidator.Validate(record, null, …)` (:142); `ScriptAnalyzer.Analyze(code, AnalyzerProfile.Navis)` (:156) | `SeedCatalogTests` (:45-72); guard + `ScriptAnalyzer.Analyze(code, AnalyzerProfile.Tekla)` (:217-220); `ToolValidator.Validate(record, null, …)` (:238) |
| Compile test | `HPNavis.McpBridge.Tests/SeedLibraryCompileTests.cs` (**net48**, in the bridge test project, not the server's): `BridgeEntry.CreateScriptCompiler(64)` (:17), heavy gate (:56-66), bounded walks (:80-86); reads seeds from the **source tree** (:139-147); no skip, so it needs Navisworks installed | `SeedCompilationTests.cs` (net10): compiles against the installed Tekla DLLs, `Assert.SkipWhen` without Tekla (:68, :105) |
| Test totals (docs) | Server 49 (net10); bridge 135 (net48) | Server 96; bridge 24 (AGENTS.md:306-307) |

**Roslyn for a net10 quality test:** both server test projects already reference `HPRebar.McpBridge.Core` (HPNavis/HPNavis.Mcp.Server.Tests/HPNavis.Mcp.Server.Tests.csproj:29; HPTekla/HPTekla.Mcp.Server.Tests/HPTekla.Mcp.Server.Tests.csproj:26). They therefore get Roslyn 5.9 transitively and can call the walker with no new package or csproj line.

**Places to add a quality assertion (no new file):** the `ScriptAnalyzer.Analyze` calls at Navis :156 and Tekla :220.

**Validator interplay:** both seed tests call `ToolValidator.Validate(record, analysis: null, …)` and assert only `IsValid`. A new "quality not analysed" warning keeps them green, as long as the null-analysis path stays a warning and does not become an error.

## 6. Appendix `net48-inprocess`: candidate rules (with sources)

**Shared (Navis + Tekla):**
1. **Runtime:** .NET Framework 4.8, no `AssemblyLoadContext`. Plugin dependencies resolve only through `PluginAssemblyResolver` (allow-list + version family). Never add a package to Contracts or Bridge.Core without updating that list (Navis PluginAssemblyResolver.cs:17-25; Tekla :17-27).
2. **Main thread:** `MainThreadQueue(expireWithoutTicks: true)`, because the host's idle event stops under a native modal.
   - Navis: `Application.Idle` plus `PostMessage(WM_NULL)` (NavisMainThreadExecutor.cs:58-71, :251-255).
   - Tekla: `ComponentDispatcher.ThreadIdle` plus `Dispatcher.BeginInvoke(ApplicationIdle)` (TeklaThreadDispatcher.cs:36-59, :109).
   - Busy grace is 8 s for both (TeklaThreadDispatcher.cs:39; CLAUDE.md:176).
3. **The bridge owns the only transaction. A script never commits or rolls back.**
   - Navis guard denies `Transaction`, `BeginTransaction`, `Undo`, `Redo`, `Rollback`, `Try*`, `StartDisableUndo` (GuardProfile.cs:54-80).
   - Tekla guard denies `CommitChanges` on any receiver and on `model` (GuardProfile.cs:200-224).
   - The analyzer profiles flag the same names (AnalyzerProfile.cs:23, :61).
4. **Heavy and destructive operations are a second opt-in, and analysis always screens with it OFF.** A stored tool must never depend on a session checkbox.
   - Navis: NavisMainThreadExecutor.cs:192-198.
   - Tekla: TeklaBridgeExecutor.cs:375-381, plus the run-time gate at :127-137.
5. **No interactive UI from scripts.**
   - Navis denies `MessageBox` and `System.Windows.Forms` (GuardProfile.cs:57, :77).
   - Tekla denies `MessageBox`, `Picker`, `Pick*` and `Tekla.Structures.Dialog` (GuardProfile.cs:202-206, :217-219).
6. **Caller errors throw `ArgumentException`**, which is excluded from the stability window. Host or API failures throw `InvalidOperationException` (CLAUDE.md:178).
   - Navis: 5/12 seeds use `ArgumentException`.
   - Tekla: 8/12 use `ArgumentException`, 7/12 use `InvalidOperationException` (grep).

**Navis only (CLAUDE.md:176, :178):**
- `auto` = one undo entry `MCP: <label>`.
- `dryRun` = commit, then `Document.Rollback()` only when `NextUndo` is the bridge's own entry and the fingerprint changed (Service/NavisScriptRunner.cs:13-21, :60-71, :111-131, :183-192; Service/NavisUndoDecision.cs:49-65).
- `none` plus a modification = error and rollback; `manual` ≡ `auto` with a log line (NavisScriptRunner.cs:51).
- `dryRun` with a heavy call is refused up front (NavisScriptRunner.cs:45-46).
- `Search` + `SearchCondition`, `Locations = DescendantsAndSelf` + `PruneBelowMatch`.
- Walks bounded by `Take`, as enforced by SeedLibraryCompileTests.cs:80-86.
- Read `VariantData` by type (`IsDisplayString` before `ToDisplayString()`, which throws otherwise).
- mm via `units`; collections serialised at a 200-item cap.

**Tekla only:**
- Native savepoint: `Operation.SetTestSavePoint()` before every run, `RollbackToTestSavePoint(resetSelection: true)` on `dryRun` and on any exception (TeklaBridgeExecutor.cs:22, :178, :194-198, :208-221).
- `model.CommitChanges(label)` is issued **by the bridge** only when tier ≥ Write and the run is not a dry run (:200-204).
- A `.db1`/`.db2` snapshot is taken before any W/D run. A snapshot failure is only logged (:151-161).
- The tier is decided syntactically by `TeklaTierAnalyzer`: keywords `Delete*`/export → D, `Insert`/`Modify`/`Set*Property` → W (TeklaTierAnalyzer.cs:32-41, :102-116).
- *Observation:* the executor never reads `request.Transaction` (grep). The commit follows the tier, not the declared mode, so `none` plus a write still commits.
- *Doc drift:* AGENTS.md:310 says `model.SetTestSavePoint()`, but the code uses `Tekla.Structures.ModelInternal.Operation` (TeklaBridgeExecutor.cs:22).

## 7. Rough seed violation counts against the §2 rules *(inference: grep plus a brace-depth script, not the Roslyn walker)*

| Rule | Navis | Tekla | Notes |
|---|---|---|---|
| Commented-out code (blocking) | 0 real, **4 detector traps** | 0 (no comments at all) | The same prose header on line 1 of 4 seeds: `// one property condition: equals (display string), contains, wildcard, gt / lt (numeric); bad input is the caller's error`. It contains `;` and `(`, so a naive "looks like code" heuristic fires on it (Clash/create_and_run_clash_test, Search/find_items_by_property, Selection/create_selection_set_from_search, Selection/override_color_by_search, each at code.cs:1) |
| Empty catch without a reason (blocking) | 0 | 0 | No `catch` in any of the 24 seeds |
| Script > 300 lines (blocking) | 0 (max 70) | 0 (max 59) | |
| Block or local function > 50 lines | 0 (longest brace block 24) | 0 (longest 38: get_reinforcement_info `while`) | |
| Nesting > 3 | 0 or 1 | 0 or 1 | Rebar/get_reinforcement_info:8-31 is `while → if → else if → if`: 4 when `else if` nests, 3 when the chain counts as one level. Navis Selection/get_selected_item_properties and Viewpoint/list_viewpoints reach brace depth 4 only through anonymous-object initialisers. The walker must count statements, not braces |
| Vague identifiers | strict list `data/tmp/obj/res`: 1 (Report/summarize_by_category:23 `var data`); wide list (`item`, `value`, `list`, `info`): about 13 | strict: 0; wide: 5 (`list` ×3, `info`, `x`) | `item`/`value` appear as loop variables and as an `args` key name (`string value = args.Require("value")`, Search/find_items_by_property:20). The final word list decides whether this rule is noise |
| `bool` parameter on a local function | 0 | 0 | Navis `bool includeComments` (Viewpoint/list_viewpoints:1) is a top-level local, not a parameter |
| Swallowing `catch (Exception)` | 0 | 0 | |

**Expected baseline allowlist for these two hosts:** empty, provided the commented-out-code detector does not fire on prose. Otherwise there are 4 Navis entries that would be false positives.

## 8. Risks

1. **Detector false positive on prose comments** (§7). This one is blocking: a real Navis seed header would be rejected. Mitigation:
   - require the comment to parse as a statement or member (`SyntaxFactory.ParseStatement` with no diagnostics), not a "has `;`" heuristic;
   - add those 4 lines as walker test cases.
2. **No net48 serialisation test for `AnalyzeResult`.** E14 is plausible-safe but unproven. Add an `analyze` round trip to PipeListenerNet48Tests.cs (no csproj edit).
3. **Enum severity.** It would serialise as an int (BridgeJson.cs:14-23 has no converter). Use string constants.
4. **The Net48Tests link list is curated** (csproj:35-41). A walker test that uses partial-name `Assembly.Load` or net10 APIs cannot be linked. Keep net48 cases in ScriptCompilerNet48Tests.cs.
5. **Redeploy lag.** The Navis and Tekla plugins stay "not analysed" until they are rebuilt with the host closed. Neither has a live harness run planned, so report them as CHƯA TEST (§2 criterion 7).
6. **Navis seed compile test sits in the net48 bridge project and has no skip.** A seed-quality test added there would fail without Navisworks. Put the quality test in the net10 `HPNavis.Mcp.Server.Tests` (embedded resources, no host needed).
7. **Existing seed tests call `ToolValidator.Validate(…, analysis: null, …)`** (Navis :142, Tekla :238). If the null-analysis path ever becomes an error, or if a warnings-empty assertion is added, both break. Keep it a warning.
8. **Tekla's ignored `transaction` field** and the AGENTS.md savepoint doc drift (§6) are unrelated to this work. Log them; do not fix them here (governance rule: log, don't silently fix).
9. **Test naming `TeklaMilestone1Challenger2Net48Tests`** carries plan taxonomy, contrary to review-audit rule §5. This is a Boy Scout item only if a later diff touches the file.

**Status:** DONE
**Summary:**
- On net48, `QualityAnalysed` (bool, default false) plus `IReadOnlyList<QualityFinding>` (a positional record with string severity) are additive-safe: Polyfill already supplies `init` on net48, `BridgeJson` skips unknown members, and a missing field reads as `false`.
- Navis and Tekla both reach `ScriptAnalyzer.Run` and change only `GuardViolations`, so the fields arrive after a plugin redeploy with no host code change. The walker needs no link into Net48Tests, though its tests do.
- The 24 seeds have no real violations. The one hazard is 4 Navis prose headers that a naive commented-out-code detector would block.
