# Map 01 — HPRebar (Revit MCP) + McpShared

Scout, read-only, 2026-10-04. Paths relative to repo root. "inferred" = not run, reasoned from code.

## 1. Insertion point in `ScriptAnalyzer`

- Parse: `CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script))` — McpShared/HPRebar.McpBridge.Core/Scripting/ScriptAnalyzer.cs:42. Walker `FactsWalker(tree, profile)` visits root L43-44.
- `AnalyzeResult` built in `Analyze(code, profile)` — ScriptAnalyzer.cs:46-53 (`Literals, ArgKeys, LineCount, HasLoops, UsesTransaction`). `Run(...)` L21-35 calls `Analyze` first (L23), then `ScriptGuard.Check` (L24, re-parses its own tree, ScriptGuard.cs:62), then compiler only when guard clean (L26-32).
- **Recommended insertion: inside `Analyze`, after L44, reusing the local `tree`** — `var quality = ScriptQualityWalker.Check(tree)` (name inferred), then set `QualityAnalysed = true, QualityFindings = …` in the initializer L46-53. Reasons:
  - tree already parsed, no third parse; pure syntax, matches the class doc "syntax-only facts" (L8-13);
  - `Analyze` is what the fake executor and validator tests call (FakeRevitExecutor.cs:86, ToolLifecycleTests.cs:32-38). If the walker lived only in `Run`, every test using `Analyze` would get `QualityAnalysed=false` → the new "quality not analysed" warning → `Clean_candidate_passes` (`Assert.Empty(report.Warnings)`, ToolLifecycleTests.cs:46) breaks;
  - quality findings survive the ETABS/SAP early returns `if (!result.Compiles) return result;` (EtabsExecutor.cs:214-215, SapExecutor.cs:179-180) and Tekla's prepend (TeklaBridgeExecutor.cs:~380) because they are computed before guard/compile.
  - Cost: `Analyze` is also called directly by 8 other hosts' seed tests feeding `ToolValidator.Validate` (see §4) → blocking findings there become validator errors. See risk R2.
- Pattern to copy: `ScriptGuard` = static facade `Check(code, profile)` + `private sealed class DenyListWalker(GuardProfile) : CSharpSyntaxWalker` with `Diagnostics` list (ScriptGuard.cs:60-74, 89-93); trivia handled outside the walker (`#r`/`#load` via `root.GetReferenceDirectives()` L67-71). `FactsWalker` = primary-ctor walker taking `tree` for `GetLineSpan` positions (ScriptAnalyzer.cs:56, 77).
- Walker gotchas for the new rules (from code, inferred impact):
  - `CSharpSyntaxWalker` default depth is `Node` — both existing walkers use the default ctor (ScriptAnalyzer.cs:56, ScriptGuard.cs:89), so comments are **not** visited. Comment rules need `base(SyntaxWalkerDepth.Trivia)` or `root.DescendantTrivia()` (as ScriptGuard does for directives).
  - Script mode: top-level declarations are members, not statements — "a top-level `double x = 150;` is a field in script mode" (ScriptAnalyzer.cs:151). So a top-level `int F(bool b){…}` is a `MethodDeclarationSyntax`, nested ones are `LocalFunctionStatementSyntax`. Rules "local function > 50 lines" and "bool parameter on local function" must cover both. Top-level statements sit in `GlobalStatementSyntax` (nesting count starts below it).
  - net48 asset: McpBridge.Core targets `net8.0;net48` (HPRebar.McpBridge.Core.csproj:6) with Polyfill only for net48 (L25). Existing code uses ranges `text[..n]` (ScriptAnalyzer.cs:156) and `Substring` (ScriptGuard.cs:79) — new walker must compile on both; Net48Tests will catch it.
- `LineCount` exists: `tree.GetText().Lines.Count` (ScriptAnalyzer.cs:50) = **physical lines incl. blank and comment lines**; a trailing newline adds one empty line. Proposals are `Trim()`med before analysis (ToolLifecycleService.cs:76), seeds read raw from resources are not (SeedLibraryTests.cs:31-49). "> 300 lines" rule can reuse `LineCount` (consistent with the 32 KB cap, ToolValidator.cs:56); decide whether seeds are trimmed first (1-line difference).
- Severity wire type: `BridgeJson.Options` has **no `JsonStringEnumConverter`** (BridgeJson.cs:14-23) → an enum `Severity` would cross as an integer. Use `string` (`"error"`/`"warning"`) like `ScriptDiagnostic.Id` (ExecuteResult.cs:63).

## 2. How `AnalyzeResult` crosses the pipe

- Bridge: `RequestDispatcher` `case AnalyzeSuffix` → `JsonRpcEnvelope.Success(id, _executor.Analyze(parameters))` (RequestDispatcher.cs:122-128) → `BridgeJson.ToElement(object)` = `JsonSerializer.SerializeToElement(value, Options)` (BridgeJson.cs:25-26, JsonRpcEnvelope.cs:55) — runtime type, reflection. Line written by `NdjsonPipeWriter` (NdjsonPipeWriter.cs:34).
- Server: `RevitBridgeClient` → `response.ResultAs<T>()` (RevitBridgeClient.cs:66) = `Result.Value.Deserialize<T>(BridgeJson.Options)` (JsonRpcEnvelope.cs:67). Called from `ToolLifecycleService.AnalyzeAsync` (ToolLifecycleService.cs:54).
- Options (BridgeJson.cs:14-23): camelCase, `PropertyNameCaseInsensitive`, `WhenWritingNull`, relaxed encoder. **No `JsonSerializerContext`, no source-gen, no `UnmappedMemberHandling`** (grep for `JsonSerializerContext|UnmappedMemberHandling|JsonSerializable` in McpShared: 0 hits) → STJ default `Skip`.
- New server + old bridge: field absent → `bool QualityAnalysed` = `false`, list keeps its initializer (follow the pattern `= System.Array.Empty<…>()`, AnalyzeMessages.cs:23-31). Distinguishable from "analysed, clean" only through `QualityAnalysed` — that is why the flag is needed.
- Old server + new bridge: unknown `qualityAnalysed`/`qualityFindings` skipped by default. Precedent: `AnalyzeRequest.Transaction` (AnalyzeMessages.cs:5-11) and `ExecuteResult.Snapshot` (ExecuteResult.cs:51) were added additively and old peers kept working (CLAUDE.md McpShared row).
- `WhenWritingNull`: a null list would be omitted; keep non-null defaults.
- New record `QualityFinding(RuleId, Severity, Line, Column, Message)` positional — same shape as `ScriptDiagnostic`/`CodeLiteral` records already deserialized by the server (ExecuteResult.cs:63, AnalyzeMessages.cs:49).
- net48 asset: Contracts `netstandard2.0;net48`, STJ 10.0.12 package (HPRebar.Mcp.Contracts.csproj:9, 21). net48 bridges (Navis, Tekla) only **serialize** `AnalyzeResult`; deserialization happens on the net10 server only → no new binding surface (no new package/assembly). Gap: no Net48 test round-trips `AnalyzeResult` JSON today (Net48Tests files: PipeListenerNet48Tests, ScriptCompilerNet48Tests, TeklaMilestone1Challenger2Net48Tests; analyze appears only as direct `ScriptAnalyzer.Run` calls, ScriptCompilerNet48Tests.cs:135-202). Recommend one net48 test: serialize a result with findings, deserialize with fields missing.

## 3. Who calls analyze; validator/review change surface

| Caller | Where | Uses |
|---|---|---|
| `ProposeAsync` | ToolLifecycleService.cs:91-94 | `ToolValidator.Validate` → errors block, warnings returned |
| `get_run analyze=true` | RunHistoryTools.cs:34, projection L50-58 | explicit field list → new fields **not** exposed unless added |
| `toolify_run` prompt | ToolifyPrompts.cs:41-45 | literals only |
| `test_tool` | ToolLifecycleService.cs:103-145 | **no re-analysis** (only `_manager.RunAsync`) |
| `publish_tool` | ToolLifecycleService.cs:147-183 | **no re-validation**; `PolicyAuto` L164-171 publishes, returns `ReviewFile=null`; else `WriteReview` L173 |
| Revit bridge | McpBridgeExternalEventHandler.cs:126 | `ScriptAnalyzer.Run(_compiler, code)` (Revit profiles) |

- `ToolValidator.Validate` (ToolValidator.cs:36-106): analysis block L62-78; offline branch L79-82 (warning "code was not analysed (bridge offline)…"). Distinction points:
  - `analysis == null` (offline/timeout/bridge error, ToolLifecycleService.cs:56-60) → existing else-branch L79-82; add the "quality not analysed" line there.
  - `analysis != null && !QualityAnalysed` (old bridge) → new branch inside L62-78: one warning "quality not analysed".
  - `QualityAnalysed` → error-severity findings → `errors`, warning-severity → `warnings` (format like guard lines L68: `quality {line}:{col} {ruleId} {message}`).
  - Both "not analysed" cases are warnings → draft saved → publish allowed under every policy (Publish never re-validates, L147-183). Acceptance 3 holds by construction for the propose/publish gate.
- `WriteReview` (ToolLifecycleService.cs:239-282) receives only `ToolRecord`; **analysis is not persisted anywhere**: `ToolRecord` has no analysis field (ToolRecord.cs:25-88), `tools` table = name/category/…/record_json (ToolRegistryDb.cs:37-40), `tool_versions` = code + schema (L41-43), `runs` = no analysis (L44-47). So the review's quality section needs one of:
  - (a) persist a compact quality summary on `ToolRecord` at propose (nullable property → tool.json + record_json automatically; `WhenWritingNull` RegistryJson.cs:15 keeps seed tool.json unchanged; seed checksum is over raw embedded text, SeedInstaller.cs:22, so unaffected);
  - (b) re-analyse in `Publish` → `Publish` becomes async + bridge round trip (callers: ToolLifecycleTools.cs:121, ToolLifecycleTests.cs:177/206/209/212/251/276). Bridge state may differ from propose time (offline at propose, online at publish → blocking findings after the draft exists: contract silent);
  - (c) put the summary in the `proposed` registry event detail (`_manager.Save(..., detail)`, ToolLifecycleService.cs:96) — WriteReview would have to read events (it reads runs today, L241).
  - Recommendation: (a) — deterministic, "same line" as at propose, no async change. Pending planner decision.
- **Acceptance-3 nuance:** under `PolicyAuto` no review file is written at all (L164-171) — "review file shows the same line" can only apply to manual policy; for auto, the line can go in the `publish_tool` message (L170) or nowhere. Contract wording should say so (deviation candidate).
- Review section placement: after "## Test runs" (L264-267), before "## Code" (L269). `ToolLifecycleService.cs` is 283 lines → crossing 300 is a C2 trigger; extracting the review writer is a judgement call, not required.

## 4. Tests that build or consume `AnalyzeResult`

- Only production constructor: ScriptAnalyzer.cs:46 (`grep "new AnalyzeResult"` → 1 file).
- Fake executor `FakeRevitExecutor.Analyze` uses real `ScriptAnalyzer.Analyze` + `ScriptGuard.Check` (FakeRevitExecutor.cs:83-92); linked into HPRebar.Mcp.Server.Tests (csproj:35) and Net48Tests (csproj:40). No edit needed if the walker sits in `Analyze`.
- `ToolValidator.Validate` callers in tests (counts): HPRebar ToolLifecycleTests 13; McpShared RegistryProfileTests 10, Navis/Etabs/Excel/Robot/RobotChallenger/Sap2000/Tekla ProfileTests 3-4 each; host seed tests 1 each in AutoCAD/Civil3d SeedLibraryTests, Etabs/Navis/Sap2000 SeedLibraryStructureTests, Excel SeedLibraryQualityVerificationTests + ExcelSeedLibraryAdversarialChallengeTests, Robot/Tekla SeedCatalogTests. Those seed tests will see quality errors for violating seeds (Excel read_table `catch { }`) → must change or get the baseline (other groups' maps).
- Tests asserting exact warnings/review text that the change touches:
  - ToolLifecycleTests.cs:46 `Assert.Empty(report.Warnings)` — stays green only if the walker is in `Analyze` and the sample `return args.Int("count") * 2;` (L17) has no finding.
  - ToolLifecycleTests.cs:127 expects "bridge offline" text — keep wording, append a sentence.
  - ToolLifecycleTests.cs:179-182, RegistryProfileTests.cs:116-119 review `Contains` checks — additive section is safe.
  - ToolLifecycleTests.cs:241 proposes `Code + "\n// v2"` — must stay accepted → false-positive test case for the commented-out-code rule (a lone identifier is not code).
- No test proposes code containing `catch` (grep of string literals in the three engine/Revit test projects: 0 hits).

## 5. Revit seeds

- 21 seeds = 21 `code.cs`, 63 files under HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary (find). Largest: create_line_based_element 137, ai_element_filter 102, create_point_based_element 101 lines.
- Embedded via `<Compile Remove>` + `EmbeddedResource LogicalName="SeedLibrary/…"` (HPRebar.Mcp.Server.csproj:33-34).
- Quick grep (inferred, not the walker): 0 `catch`, 0 lines matching commented-out-statement shape → likely **0 blocking**; vague-identifier warnings at Annotation/create_dimensions/code.cs:13 (`obj`) and Data/get_material_quantities/code.cs:16 (`data`).
- `SeedLibraryTests.cs` (206 lines): own resource loader L29-54 (duplicates the public `SeedInstaller.LoadSeeds(assembly)`, SeedInstaller.cs:28); theories over `Seeds()` L23: `Seed_record_is_well_formed` L66-95, `Seed_code_passes_guard_and_reads_only_declared_args` L97-115 (guard + `ScriptAnalyzer.Analyze`, no `ToolValidator`), `Seed_code_compiles_against_revit_api` L117-125 (RevitAPI ref from NuGet cache, `Assert.SkipWhen` when absent).
- Fit: one new `[Theory][MemberData(nameof(Seeds))] Seed_code_meets_quality_rules` beside L97-115, asserting no error-severity finding unless the seed is in the baseline allowlist. No csproj change: every `*.Mcp.Server.Tests` already references `HPRebar.McpBridge.Core` (grep: HPRebar csproj:29 + the 9 others). A shared helper via `<Compile Include … Link>` (precedent FakeRevitExecutor, csproj:35) would be a csproj edit (P7); per-server ~15-line test using `SeedInstaller.LoadSeeds` + the walker needs none.

## 6. Standard docs — host-neutral vs Revit-specific (ids unchanged)

REVITADDINAI_CLEAN_CODE_STANDARD.md (170 lines):

| Section | Ids | Verdict |
|---|---|---|
| §0 How to use (L7-13) | — | neutral (drop "§11 Revit" pointer) |
| §1 Priorities (L15-23) | — | neutral |
| §2 Naming (L25-38) | N1-N10 | neutral; N7 "feature's UiStrings catalog" HPRebar-specific wording |
| §3 Methods (L40-53) | M1-M10 | neutral; M6/M7 say "Core"/"Revit/Excel" — reword to "host-free code"/"host API" |
| §4 Formatting (L55-63) | FM1-FM5 | neutral (FM1 needs the .editorconfig of phase 4) |
| §5 Classes (L65-75) | C1-C7 | neutral; C7 examples Revit (`IExternalEventHandler`) |
| §6 SOLID (L77-85) | S1-S5 | neutral (rebar examples) |
| §7 Dependencies (L87-96) | D1-D6 | D2, D3, D5, D6 neutral; **D1** (`<Feature>Command.Execute`, ADR-0003) and **D4** (allowlist `_window`, `RevitHostTheme`, `McpBridgeHost.Current`) HPRebar-specific |
| §8 Coupling (L98-107) | K1-K6 | K1-K5 neutral; **K6** (features/`Shared/`, ADR-0004) HPRebar |
| §9 Comments (L109-118) | CM1-CM6 | neutral (CM3 = runtime blocking rule "commented-out code") |
| §10 Tests (L120-130) | T1-T7 | T1, T3, T4, T7 neutral; T2 (`HPRebar.Core.Tests` path), T5 (`Document`), T6 (TUnit `.rvt`) Revit |
| §11 Revit (L132-148) | R1-R13 | Revit appendix; R12 (COM release in `finally`) generalises to com-standalone; R8/R9/R13 (dialogs, ExternalEvent, modeless) Revit+WPF |
| §12 Project (L150-161) | P1-P8 | P6, P7, P8 repo-wide neutral; P5 WPF/MaterialDesign repo-wide (development-rules.md); P1-P4 HPRebar |
| §13 Superseded (L163-170) | — | HPRebar docs only |

CODE_REVIEW_CHECKLIST.md (78 lines): §0 scope L7-10, A names L12-19, B methods L21-29, C classes L31-40, E duplication L50-53, F comments/tests L55-60, H report L76-78 → neutral (A–C,E,F reference neutral ids). §D L42-48 mixed (L43 composition root = D1, L46 `HPRebar.<OtherFeature>`/F1-F2, L47 `Model/`/L5 = HPRebar layer rules; L44, L45, L48 neutral). §G Revit safety L62-74 → Revit appendix (L72 COM line reusable for com-standalone). Header L3 scopes to `HPRebar/`.

TOOL_DEVELOPMENT_WORKFLOW.md (84 lines): overview + steps 1-3, 5-9, 12, 14, 16 neutral; step 4 "Revit boundary analysis" (L23), 10 build `Debug.R26` (L29), 11 tests (L30), 13 Revit safety (L32) host-specific → generalise to "host boundary / host build / host safety (appendix)". "Before code" Q1-Q7, Q10 neutral; Q8 (R1-R13) and Q9 (UiStrings) host-specific. DoD L65-84: L67, L68, L78, L79, L82 Revit-worded. **Step 15 (L34) says "Regenerate AGENTS.md when CLAUDE.md changes" — conflicts with E12 (regen deletes ~123 lines); must change in phase 1.**

## 7. `tools/list` byte-identical risk

- Planned changes touch no `[Description]`/`[McpServerTool]` attribute: meta tool texts live in ToolLifecycleTools.cs:18-23, 76-79, 112-116, 128-129 and `RegistryToolText` — leave untouched.
- `propose_tool` output (`payload` errors/warnings/next, ToolLifecycleTools.cs:61-72) is a `CallToolResult`, not tools/list. Verified from snapshot plans/260916-2152-etabs-mcp-2026/reports/phase-00-tools-list-after-revit.json: `propose_tool` properties = 12 user params, no `cancellationToken`, no `outputSchema`; `publish_tool` = `[name]`.
- If option (b) makes `publish_tool` async with a `CancellationToken` parameter: SDK omits it from the schema (as for propose_tool) → inferred identical; the snapshot gate must prove it.
- Seeds are tools in tools/list (description/schema from tool.json) — the seed-quality test must not edit seed tool.json; a seed `code.cs` fix changes no tools/list byte.

## 8. Evidence check

| # | Verdict | Evidence |
|---|---|---|
| E1 | verified | all 10 executors call `ScriptAnalyzer.Run`: HPRebar McpBridgeExternalEventHandler.cs:126, HPAutoCad MainThreadExecutor.cs:152, HPCivil3d MainThreadExecutor.cs:152, EtabsExecutor.cs:214, ExcelBridgeExecutor.cs:343, NavisMainThreadExecutor.cs:195, PowerBiBridgeExecutor.cs:295, RobotBridgeExecutor.cs:346, SapExecutor.cs:179, TeklaBridgeExecutor.cs:374; `return new AnalyzeResult` ScriptAnalyzer.cs:46 |
| E2 | verified | Server.Core packages ModelContextProtocol/Hosting/Sqlite (HPRebar.Mcp.Server.Core.csproj:19-21), only ProjectReference = Contracts (L28); Roslyn in McpBridge.Core csproj:18 |
| E3 | verified | AnalyzeMessages.cs:18-42 — exactly the 9 listed properties |
| E4 | verified | only production call ToolLifecycleService.cs:92; errors block L93-94; offline warning ToolValidator.cs:79-82. Note: also called by 19 test files |
| E5 | verified | Publish L147-183 no validation; PolicyAuto L164-171; WriteReview call L173, def L239. Nuance: auto policy writes **no** review file |
| E6 | verified | SeedInstaller.cs has no Validate/Analyze call; writes via `WriteSeed` L78/83/99 |
| E7 | partial | 465 files confirmed, but in **9** servers; HPPowerBi has no `Registry/SeedLibrary` (0 `code.cs`; embed is `Condition="Exists('Registry\SeedLibrary')"`, HPPowerBi.Mcp.Server.csproj:29). Largest 137 lines confirmed |
| E11 | partial | scripts exist: navis (revit+autocad), etabs (+navis), civil3d (+etabs) — max 4 of 10 hosts; they target `bin\Release\net10.0` exes and canonicalise (sort by name + `ConvertTo-Json`), so "byte-identical" = canonical JSON, not raw stdout; must be extended to 10 exes |
| E14 | verified, risk lower than implied | Navis/Tekla bridges net48 (their csproj TargetFramework); net48 side only serialises `AnalyzeResult`; new fields need no new package |

## 9. Risks

- R1 Roslyn absent from Server.Core (E2): quality must be computed on the bridge (or in tests via McpBridge.Core). Seed tests fine — all 10 server test projects already reference McpBridge.Core (no csproj edit). Server.Core must never reference the walker.
- R2 Walker inside `Analyze` turns blocking findings into validator errors in 8 hosts' seed tests that call `ToolValidator.Validate(…, ScriptAnalyzer.Analyze(code), …)` (§4) — baseline allowlist must be applied there too, or those tests filter quality findings. Coordinate with map-02..05.
- R3 Trivia depth + script-mode member shapes (§1) — easy to miss; tests must include top-level methods vs local functions and comments.
- R4 Commented-out-code false positives: `// v2` (ToolLifecycleTests.cs:241), prose with `;`, units `// mm`, URLs, `// TODO(x): …` (CM4 allows). Need parse-based detection (statement parses with no diagnostics) rather than regex.
- R5 Review-file data source undecided (§3 a/b/c); acceptance 3 wording vs auto policy.
- R6 `ToolLifecycleService.cs` 283 lines, `ToolValidator.cs` 172 — C2 trigger if review code grows there.
- R7 Severity as enum would serialise as int (no string-enum converter) — use string.
- R8 Old non-Revit bridges report "not analysed" until redeployed (accepted assumption); Revit bridge redeploy needs Revit closed (CLAUDE.md) — live acceptance 7 depends on it.
- R9 Doc split: workflow step 15 "Regenerate AGENTS.md" contradicts E12.

**Status:** DONE_WITH_CONCERNS
**Summary:** Insert the quality walker in `ScriptAnalyzer.Analyze` reusing its tree (L42-53); additive fields cross the pipe safely (reflection STJ, default Skip, string severity); `ToolValidator` L62-82 is the only mapping point; analysis is not persisted, so the review section needs a persistence decision.
**Concerns:** E7/E11 partial (PowerBi has 0 seeds; snapshot scripts cover ≤ 4 hosts); auto policy writes no review file (acceptance 3 wording); walker-in-`Analyze` ripples into 8 other hosts' seed tests via `ToolValidator`.
