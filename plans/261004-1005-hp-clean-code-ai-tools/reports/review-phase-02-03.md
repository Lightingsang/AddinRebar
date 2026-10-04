# Review — phases 2 + 3 (quality walker, contract, validator/review, seed tests)

Scope: uncommitted diff — 5 new prod files (walker ×3, `ToolLifecycleService.Review.cs`), 6 changed prod/test files, 11 new test files. Read-only; nothing edited except this report.
Score: **7.5/10** — contract additive and well tested, walker logic matches evaluation §3; one test that depends on checkout line endings and one blocking false positive of Q-B1 must be fixed before commit.

## Checks run
| Check | Result |
|---|---|
| Scratch probe (net8 console → `HPRebar.McpBridge.Core.csproj`), 41 adversarial samples | outputs quoted below; Q-W2/Q-W1/Q-W5 logic as designed; 2 Q-B1 false-positive classes |
| Probe over all 155 seed `code.cs` | Q-B2 2 (Excel `read_table`), Q-W1 1, Q-W2 7, Q-W3 16, Q-W4 2, Q-W5 1, Q-B1 0 — warning noise low (D4a intent met) |
| Contract P9 | new bridge → old server: `BridgeJson` has no `UnmappedMemberHandling` ([BridgeJson.cs:14-23](../../../McpShared/HPRebar.Mcp.Contracts/JsonRpc/BridgeJson.cs#L14-L23)) → unknown fields skipped. Old bridge → new server: defaults `false`/empty → "not analysed" warning (net48 test pins it). Probe JSON: `"qualityAnalysed":true,"qualityFindings":[{"ruleId":"Q-B1","severity":"error","line":1,"column":1,…}]` |
| MCP output leakage | `get_run` projects `analysis` anonymously ([RunHistoryTools.cs:52-60](../../../McpShared/HPRebar.Mcp.Server.Core/Tools/Registry/RunHistoryTools.cs#L52-L60)) → no new field reaches the AI; `tools/list` cannot change |
| `tools/list` gate | `cmp` ×10 `tools-list-before-debug` vs `tools-list-baseline-debug`: all identical; Debug `HPRebar.Mcp.Server.Core.dll` 11:37 ≥ newest source 11:37, snapshot 11:45 |
| Fail-closed points | validator blocks only `Severity == "error"` ([ToolValidator.cs:127](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolValidator.cs#L127)); offline text unchanged ([:85](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolValidator.cs#L85)); findings never in `GuardViolations`; ETABS/SAP get the fields via `Run → Analyze` ([ScriptAnalyzer.cs:54-55](../../../McpShared/HPRebar.McpBridge.Core/Scripting/ScriptAnalyzer.cs#L54-L55)) |
| Lifecycle | `quality_checked` written only after `Save` of an accepted draft ([ToolLifecycleService.cs:98](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLifecycleService.cs#L98)); `ts` is Unix ms + `rowid DESC` tie-break, index `(tool_name, event, ts DESC)` covers it; main file 238 lines (K10 met) |
| CM6 grep (phase/K#/D#/E#/ADR/H-0 in new/changed lines) | 0 hits |
| Tests | not re-run (lead: 784 + 115 + 10 servers + mirror 60 green); probe compiled Bridge.Core net8 only |

Probe excerpt (label → findings): `prose label+call` `// Example: args.Double("spacing", 150);` → **Q-B1** · `prose e.g.` `// e.g. args.Str("name");` → **Q-B1** · `prose usage block` `/* Usage:\n * args.Str("name");\n */` → **Q-B1** · `prose formula` `// area = width * height;` → Q-B1 · `prose note call` / `prose see method` / `prose vn` / `prose limit` → - · `code return` `// return x;` → - · `code incr` `// count++;` → - · `top-level try 60 lines` → Q-W1 · `local fn resets` → - · `true local fn deep` → Q-W2 · `lambda counts` → Q-W2 · `catch return in lambda` → - · `catch decl vague` (`catch (Exception res)`) → - · `crlf 301` → Q-B3.

## High
**H1 — Excel baseline hash depends on the checkout's line endings; regenerating the seed breaks both tests.**
[HPExcel SeedQualityTests.cs:21](../../../HPExcel/HPExcel.Mcp.Server.Tests/SeedQualityTests.cs#L21) pins `cc71f339c081`, computed over the raw embedded text ([:55](../../../HPExcel/HPExcel.Mcp.Server.Tests/SeedQualityTests.cs#L55)). `git ls-files --eol` on `read_table/code.cs` = `i/lf w/crlf` (`core.autocrlf=true`, no `.gitattributes` rule): CRLF sha12 = `cc71f339c081`, LF sha12 = `95f812195e94`. Any LF checkout (`autocrlf=false/input`, CI, a second machine) **and** a plain re-run of the generator — it writes `newline="\n"` ([generate-seed-library.py:21](../../../HPExcel/tools/generate-seed-library.py#L21)) — produces the LF bytes, so `EverySeed_…` and `EveryBaselineEntry_…` both fail on an unchanged seed (the message then wrongly says "the seed changed").
Fix (×10 files, keep them uniform): `Hash(code.Replace("\r\n", "\n"))` and re-pin to `95f812195e94`; add a test row proving CRLF and LF give the same hash. (Same weakness exists in `SeedContent.Checksum`, pre-existing — log, do not fix here.)

**H2 — Q-B1 refuses prose that introduces an example (the case evaluation §3 listed as an expected false positive).**
[ScriptQualityComments.cs:49-59](../../../McpShared/HPRebar.McpBridge.Core/Scripting/ScriptQualityComments.cs#L49-L59): `Word: <statement>;` parses as a *labelled statement* and `e.g. args.Str(…)` as a member access (`e.g.args.Str`), so `// Example: args.Double("spacing", 150);`, `// e.g. args.Str("name");` and a `/* Usage: … */` block are errors that **block the draft** (K1 = H×H). The negative corpus ([ScriptQualityTests.cs:26-34](../../../McpShared/HPRebar.Mcp.Server.Core.Tests/ScriptQualityTests.cs#L26-L34)) has no such row.
Fix: treat as prose when the parsed comment's first statement is a `LabeledStatementSyntax`, or the text starts with `e.g.`/`i.e.`/`eg`/`Example`/`Usage`/`See` (add to `ProseTags`, [:17](../../../McpShared/HPRebar.McpBridge.Core/Scripting/ScriptQualityComments.cs#L17)); add the three probe lines as negative `InlineData`. `// area = w * h;` is valid code — keeping it as Q-B1 is defensible.

## Medium
**M1 — review/auto message can show the quality of a different version of the code.**
`LatestEventDetail` returns the newest `quality_checked` for the *name* ([ToolLifecycleService.Review.cs:49](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLifecycleService.Review.cs#L49), [:84](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLifecycleService.Review.cs#L84)); code also changes without `propose_tool` (hand edit of `code.cs` picked up by the watcher, `registry import`). The reviewer then approves code against a summary of the previous version. Fix: prefix the summary with `v{record.Version} sha:{12 hex of code}` in `ProposeAsync` and have `WriteReview` print "stale — code changed since the check" when it does not match `record`.

**M2 — test gaps against the plan's K8/§3 test list (behaviour is correct today per probe; nothing pins it).**
- `Find_LocalFunctionOver50Lines_Warns` ([ScriptQualityTests.cs:85](../../../McpShared/HPRebar.Mcp.Server.Core.Tests/ScriptQualityTests.cs#L85)) tests a *script-level method* (top-level `int Count()` parses as `MethodDeclarationSyntax`); no test for a real block-nested local function, for the nesting reset inside a function, or for a lambda block counting as a level.
- No Q-W5 row for a bare `catch { stmt; }`; no Q-B1 row for code inside an empty catch; no lifecycle assert that a **rejected** proposal writes no `quality_checked` event.
Fix: add the probe samples `true local fn deep`, `local fn resets`, `lambda counts`, `bare catch stmt`, `code inside catch` as `InlineData`, and an event-count assert in `Invalid_proposal_saves_nothing`.

## Low
- **L1** Walker failure is fail-open twice over: an exception (incl. Roslyn `InsufficientExecutionStackException` — `Nest`'s closures add frames per level, [ScriptQuality.cs:174-195](../../../McpShared/HPRebar.McpBridge.Core/Scripting/ScriptQuality.cs#L174-L195)) fails the whole `analyze`, the server maps it to "bridge offline" and skips guard + compile at propose. Wrap `ScriptQuality.Find` in `Analyze` with a catch → `QualityAnalysed = false` (still a warning, guard/compile unaffected).
- **L2** Severity knowledge in 3 places (K1): `ScriptQuality.Error`, `"error"` in [ToolValidator.cs:127](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolValidator.cs#L127) and [Review.cs:73](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLifecycleService.Review.cs#L73); two different "not analysed" texts ([ToolValidator.cs:27](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolValidator.cs#L27) vs [Review.cs:70](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLifecycleService.Review.cs#L70)). Move `Error`/`Warning` constants onto `QualityFinding` in Contracts (additive) and use them in both processes.
- **L3** File ≠ class name: `ScriptQualityComments.cs` declares `CommentedOutCode`, `ScriptQualityCatches.cs` declares `CatchRules` (repo convention "class names follow file names"). Rename files or classes. `ScriptQuality.cs` 206 lines (plan said ≤ 200; under C2's 300 — fine).
- **L4** Q-W1 fires on a script whose whole top-level body is wrapped in one `try { … }` (probe `top-level try 60 lines`), against the spirit of "top-level body excluded"; 0 seeds hit it. Optional: skip a `BlockSyntax` whose parent is a `TryStatement` that is itself a `GlobalStatement`.
- **L5** Walker misses (warnings only): `return` inside a nested lambda counts as reporting for Q-W5 ([ScriptQualityCatches.cs:45-47](../../../McpShared/HPRebar.McpBridge.Core/Scripting/ScriptQualityCatches.cs#L45-L47)); catch-declaration identifier not checked by Q-W3; anonymous `delegate { }` is not a nesting level. Exclude `ReturnStatement`s under `AnonymousFunctionExpressionSyntax`; add `VisitCatchDeclaration` → `NoteName`.
- **L6** Seed tests: plan said warnings are *printed* — they are not (no `ITestOutputHelper`); the 9 seeded projects have no `Assert.NotEmpty(Seeds())`, so a broken embed makes the test pass vacuously. `Seeds()` is re-loaded per baseline entry inside the LINQ.
- **L7** A tool proposed while the bridge is offline is never quality-checked, and under `auto` publishes with only the "not analysed" note — matches acceptance 3, but `test_tool` always has the bridge online: recording a `quality_checked` there would close it cheaply (lead's call).
- **L8** `ORDER BY ts DESC, rowid DESC` ([ToolRegistryDb.cs:197](../../../McpShared/HPRebar.Mcp.Server.Core/Registry/ToolRegistryDb.cs#L197)): a wall-clock step back picks an older row; `ORDER BY rowid DESC` alone is monotonic for "latest inserted".
- **L9** Naming/docs drift: event is `quality_checked` but phase-02 text says `quality_analysed` (update the plan, not the code); new tests in `ToolLifecycleTests`/`ScriptCompilerNet48Tests` are snake_case vs N10 (matches those files' style — acceptable). Excel adversarial test clears all `QualityFindings` ([ExcelSeedLibraryAdversarialChallengeTests.cs:78](../../../HPExcel/HPExcel.Mcp.Server.Tests/ExcelSeedLibraryAdversarialChallengeTests.cs#L78)) — fine given SeedQualityTests, but filtering only the baselined rule would keep both tests independent.

## Checklist A–F (new/changed lines)
| Section | Verdict |
|---|---|
| 0 Scope | phases 2+3 together in one tree — commit split of phase files (P6: 3 commits) still doable; no csproj/slnx edits (P7) |
| A Names | L3 file/class; N10 L9; no vague names in walker code |
| B Methods | all methods < 30 lines; `Find` orchestrates; no bool params |
| C Classes | `ScriptQuality` + 2 internal rule classes = clear split; `ToolLifecycleService` partial split by concern (C3 ok) |
| D Dependencies | Server.Core still has no Roslyn (severity as string); Bridge.Core changes compile for net48 (C# collection expressions, no `^1`, `Substring`) |
| E Duplication | L2; ten identical seed tests — deliberate (plan: no csproj link), document in the commit body |
| F Comments/tests | CM6 clean; comments explain why (`// In script mode a top-level function is parsed as a method`); M2 gaps |

## Positives
Walker lives in `Analyze`, so fakes, seed tests and every host share one implementation; outermost-only Q-W1/Q-W2 keep reports short; `else if`/`try`/function reset match §3 exactly; `PredatesQualityCheck` on the shared fake tests the old-bridge path under both policies; the stale-baseline test makes the allowlist self-shrinking; seed scan shows the rules are quiet on 155 clean seeds.

## Plan follow-up (lead decides)
Phase 2 todos done except live verification (acceptance 7, CHƯA TEST) and the file names in "Related files"; phase 3 done once H1 is fixed. Recommended order: H1 → H2 (+ M2 rows) → M1 → L1/L2 → commits.

**Status:** DONE_WITH_CONCERNS
**Summary:** Contract is additive and the validator/lifecycle behave as specified; the Excel baseline hash breaks on LF checkouts or a generator re-run, and Q-B1 blocks `Example:`/`e.g.` prose.
**Concerns/Blockers:** H1 and H2 block commit; M1 affects what a human approves.
