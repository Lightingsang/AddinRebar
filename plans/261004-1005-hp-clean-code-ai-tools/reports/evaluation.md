# Evaluation — brief 261004-1005 (clean code for AI-created tools)

Inputs: [map-01](map-01-revit-mcpshared.md) · [map-02](map-02-autocad-civil.md) · [map-03](map-03-com-standalone.md) · [map-04](map-04-net48-inprocess.md) · [map-05](map-05-powerbi.md) · [baseline](baseline.md). Status: **Planned** — no source changed.
Lead spot-checks by hand: `ScriptAnalyzer.Analyze` builds the result at [ScriptAnalyzer.cs:40-53](../../../McpShared/HPRebar.McpBridge.Core/Scripting/ScriptAnalyzer.cs#L40-L53); `ToolValidator` maps analysis only when non-null and has the offline warning at [ToolValidator.cs:62-82](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolValidator.cs#L62-L82); `PolicyAuto` returns before `WriteReview` at [ToolLifecycleService.cs:164-171](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLifecycleService.cs#L164-L171); `ToolLifecycleService.cs` = 283 lines.

## 1. Assumption check

| Contract / evidence item | Verdict | Evidence |
|---|---|---|
| E1 every bridge goes through `ScriptAnalyzer` | verified | map-01 §8 (10 executors); ETABS/SAP prepend tier diagnostics to `GuardViolations` after `Run` (map-03 §1) |
| E2 no Roslyn in Server.Core | verified | map-01 §8 |
| E3 `AnalyzeResult` fields | verified | `AnalyzeMessages.cs` (map-01 §2) |
| E4 validator once in propose; null → warning | verified | ToolValidator.cs:79-82 |
| E5 publish does not re-validate; auto publishes directly | verified, **nuance** | under `auto` **no review file is written** (ToolLifecycleService.cs:164-171) → criterion 3 cannot be met as worded (D3) |
| E6 seeds bypass `ToolValidator` | verified | `SeedInstaller` (map-01) |
| E7 465 files in 10 servers | **partial** | 465 files / 155 `code.cs` in **9** servers; HPPowerBi has no `Registry/SeedLibrary/` (conditional embed, HPPowerBi.Mcp.Server.csproj:28-29; map-05 §3) |
| E8 Excel `read_table` `catch { }` ×2 | verified | code.cs:13, :25, generated from `HPExcel/tools/generate-seed-library.py:330,342` (map-03 §3) |
| E9 Excel quality test exists | **refuted (in substance)** | `SeedLibraryQualityVerificationTests.cs` checks counts/validator/guard/schema, not code quality (map-03 §3) → nothing to reuse |
| E10 mirror test 24 files | **partial** | `mirror-tokens.json` lists 29 pairs; test never scans `*.Mcp.Server.Tests` nor `SeedLibrary/` (MirrorTokenTable.cs:70-75) → seed tests are mirror-safe (map-02 §3) |
| E11 snapshot scripts reusable | **partial** | cover ≤ 4 hosts, sort tools → canonical not byte compare; published Revit/ETABS exes stale → gate must use same-config Debug bins (baseline) |
| E12 AGENTS.md extra content | verified, **widened** | also `.claude/skills` holds 7 `hp-mcp-*`, `.agents/skills` 10 (powerbi/robot/tekla portable-only); workflow step 15 says "regenerate AGENTS.md" (map-01 §6) |
| E13 only `HPRebar/.editorconfig` | verified, **count** | 10 solutions lack one (McpShared + 9 hosts), not 9; `HPGeo/` is retired (bin/obj only) → excluded (map-02) |
| E14 net48 deserialization risk | verified, **low** | records + `init` already cross net48 (Polyfill); net48 only serialises `AnalyzeResult`; no enum converter → severity as string (map-04 §1) |
| Accepted assumption: non-Revit bridges "not analysed" until redeployed | verified | missing field reads `QualityAnalysed=false` (map-01 §2) |
| Accepted assumption: seeds in `<Host>.Mcp.Server/Registry/SeedLibrary/**/code.cs` | partial | true for 9; Power BI tools are compiled classes |
| Chosen approach A (walker beside `ScriptAnalyzer`) | verified, refined | must live in `Analyze` (not `Run`): fakes/tests call `Analyze`, and ETABS/SAP return early after a failed compile (map-01 §1, map-03 §1) |

## 2. Risks

| # | Risk | L×I | Mitigation |
|---|---|---|---|
| K1 | Commented-out-code false positives: prose with `;`/`(` (4 Navis headers, 2 ETABS notes, `// v2` accepted by ToolLifecycleTests.cs:241, `// mm`, URLs, TODO) → **blocks** a good tool | H×H | parse-based detector: comment text must parse as ≥ 1 complete C# statement/member with no diagnostics **and** contain a code token (`;`, `=`, `(`…`)`); ≥ 2 consecutive comment lines merged first; prose word ratio check; every known prose line is a negative test |
| K2 | Mirror drift (E10) | L×M | change only McpShared + `*.Mcp.Server.Tests`; touch no mirrored bridge file; run `HPCivil3d.McpBridge.Tests` per commit |
| K3 | net48 serialization (E14) | L×M | string severity, `IReadOnlyList<>` defaulting to empty, bool default false; new net48 round-trip test |
| K4 | AGENTS.md / skills regeneration loss (E12) | M×H | never regenerate; insert by hand into CLAUDE.md and AGENTS.md (D5); skill edits on both sides, `sync-agent-skills.py check` must show no new drift |
| K5 | Excel seed baseline (E8/E9) | L×L | allowlist entry pinned to the seed's sha256; edit → entry stale → test fails until clean |
| K6 | Findings leaking into `GuardViolations` → every finding blocking; ETABS tests assert empty guard list | M×H | separate `QualityFindings` list; rule ids never `PREVIEW/PATH/DESTRUCTIVE/HEAVY` |
| K7 | Validator change breaks 19 test files that call `Validate(..., null, ...)` | M×M | `analysis == null` path unchanged (warning only); new branch only for `QualityAnalysed == false` |
| K8 | Comment rules invisible to default walker (trivia) / script top-level functions are `MethodDeclarationSyntax` | H×M | walker uses `DescendantTrivia()`; both shapes handled; unit tests for both |
| K9 | Review file needs data the registry never stores (analysis not persisted) | — | decision §5 |
| K10 | `ToolLifecycleService` 283 lines → > 300 after review section | M×L | move `WriteReview` into its own partial file |
| K11 | Live verify needs R26 bridge redeploy with Revit closed — collides with golden-run Revit / Kata session | M×M | schedule; harness checks no foreign Revit |
| K12 | Civil seed fixes regenerate ~10 seeds → checksum change → upgrades on user machines | L×M | no seed edit in this plan (allowlist only) |

## 3. Rule fit (runtime walker; ids proposed `Q-B1..B3` blocking, `Q-W1..W5` warning)

| Rule | Syntactic detection | Expected false positives | Tests needed |
|---|---|---|---|
| Q-B1 commented-out code | single-line + block comment trivia; merge consecutive lines; strip `//`; try `ParseStatement`/`ParseMemberDeclaration`; finding when it parses clean and has a code token | prose with punctuation; `// v2`; doc-like headers; examples in comments (`// e.g. args.Str("x")`) | positive: `// var x = 1;`, `// DoIt();`, multi-line block; negative: 4 Navis headers, 2 ETABS notes, `// v2`, `// mm`, URL, TODO |
| Q-B2 empty catch without reason | `CatchClauseSyntax` whose block has 0 statements and no comment trivia inside braces | none known (AutoCAD `get_entities:38` has a reason comment → must pass) | empty, empty+comment, filter-only `catch when` |
| Q-B3 > 300 lines | count lines of the **trimmed** text (proposals are trimmed, seeds are not — trim both the same way) | none (largest seed 137) | 300 vs 301 lines; trailing blank lines not counted |
| Q-W1 block / local function > 50 lines | nested `BlockSyntax`, local function, script-level method; **not** the top-level script body | 21 seeds would warn if top-level counted (map-03 R) | 50/51 lines, top-level 120 lines → no warning |
| Q-W2 nesting > 3 | depth of `if/else-body/for/foreach/while/do/switch/using-block/lambda block`; `else if` chain not a level; `try` not a level | Civil `list_pipe_networks`/`list_surfaces` (10 hits if `try` counted) | depth 3 vs 4; else-if chain; try inside loop |
| Q-W3 vague identifier | declared local / parameter / local-function names in the explicit list `data, tmp, temp, obj, res, val, foo, bar, stuff, thing`; coordinate names (`x`, `y`, `z`, `x1`, `x2`) and `result`, `item`, `value` are **not** in it | schema keys used as coordinates | each listed name; coordinates pass |
| Q-W4 bool parameter on local function | `LocalFunctionStatementSyntax` / script-level `MethodDeclarationSyntax` parameter of type `bool`/`bool?` | none | both shapes |
| Q-W5 swallowing `catch (Exception)` | catch of `Exception`/no type whose block has no `throw`, no `return`, and **does not reference the caught variable** | Civil `Fail(..., ex.Message)` records into `errors[]` (seed contract) → passes by "references ex" | swallow, rethrow, return, `Fail(ex)` |

Thresholds are review triggers except Q-B1..B3 (contract §2).

## 4. Contract deviation proposals (not applied; affected steps marked "pending user decision")

| # | Item | Evidence | Consequence | Recommended |
|---|---|---|---|---|
| D1 | Criterion 4 "seed test green in all 10 servers" — Power BI has no seeds | E7 partial, map-05 §3 | test in Power BI would assert nothing | **a.** keep a Power BI test that accepts an empty library and checks any future seed (cheap parity). b. exclude Power BI and say so |
| D2 | `.editorconfig` for "9 solutions" — 10 lack one | E13 | McpShared would stay without one | **a.** 10 (add McpShared), HPGeo excluded |
| D3 | Criterion 3 "review file shows the same line" — `auto` writes no review file | E5 nuance | criterion unmeetable under `auto` | **a.** manual policy: line in review file; auto policy: same line appended to the `publish_tool` outcome message (result text, not `tools/list`) |
| D4 | Rule wording for Q-W1/Q-W3/Q-W5 (scope of "block", vague list, "returns an error") | §3 | literal reading = 21 + 18 + 14 noisy warnings on clean seeds | **a.** interpretations of §3 (top-level body excluded; explicit list without coordinates; referencing `ex` counts as reporting) |
| D5 | (f) "CLAUDE.md → AGENTS.md" implies regeneration | E12 widened | regeneration deletes ~123 AGENTS.md lines and portable-only skills content | **a.** hand-insert the same text into both files, no regeneration; fix workflow step 15 wording. b. first back-port AGENTS.md-only sections into CLAUDE.md (bigger, separate commit), then regenerate |
| D6 | (f) "the 10 `hp-mcp-*` skills" — 3 exist only in `.agents/skills` | §1 E12 | editing `.claude` side alone leaves 3 skills untouched | **a.** edit the 10 where they live (7 `.claude` + their mirrors via `apply`; 3 `.agents`-only by hand) — no new `.claude` skills created |

## 5. Design decisions taken (technical — no user input needed)

- Walker = `ScriptQualityWalker` (one responsibility: find script-quality findings in a parsed script) in `McpShared/HPRebar.McpBridge.Core/Scripting/`, called from `ScriptAnalyzer.Analyze` on the existing tree; public static `ScriptQuality.Find(string code)` for seed tests (C1, K5).
- Contract: `AnalyzeResult.QualityAnalysed` (bool) + `QualityFindings` (`IReadOnlyList<QualityFinding>`, empty default); `QualityFinding(string RuleId, string Severity, int Line, int Column, string Message)`, severity `"error"|"warning"` (string, K3).
- `ToolValidator`: `error` → errors, `warning` → warnings; `analysis != null && !QualityAnalysed` → warning `quality not analysed (bridge predates the quality check — redeploy it)`.
- Review data: quality summary written as registry event `quality_analysed` at propose (existing `InsertEvent`), `WriteReview` prints the latest one or `quality not analysed`. No schema/tool.json change (K5); alternative "field on ToolRecord" rejected (persistence change for display only).
- Seed tests: thin `SeedQualityTests.cs` per server (no csproj edit; all 10 test projects already reference Bridge.Core) + `seed-quality-baseline.txt` (`<relative path>|<ruleId>|<sha256-12>`) only where non-empty (Excel).

## 6. Issues outside scope (log, do not fix)

| Host | Issue | Source |
|---|---|---|
| Power BI | cloud handlers check neither opt-in (`trigger_refresh` writes with both off) | PowerBiDispatcher.cs:338-401 (map-05) |
| Power BI | cloud client created without credentials; cloud tools likely always fail (inference) | BridgeEntry.cs:68 |
| Robot | failed `Project.Save()` swallowed, stale file copied as pre-run snapshot | RobotSnapshotManager.cs:102 (map-03) |
| SAP2000 | worker thread never set to STA (logs "STA=") | SapExecutor.cs:57-58 |
| Tekla | executor ignores `request.Transaction` | map-04 §6 |
| Excel | server tests may write the live `%AppData%\HPExcel\McpServer\registry.db` (inference) | baseline.md |
| Docs | CLAUDE.md "24 mirrored files" (29); Revit 34 tools (33 isolated); Power BI AGENTS.md "20" (22); development-rules.md:88 cites removed HPGeo file; AGENTS.md:310 Tekla savepoint API name | maps 02/04/05, baseline |
