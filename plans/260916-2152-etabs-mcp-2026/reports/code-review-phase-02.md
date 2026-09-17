# Code review — phase 2 `HPEtabs.McpBridge` (tiers from fixture, snapshot, fingerprint, path policy, executor matrix)

**Date:** 2026-09-17 · **Reviewer:** code-reviewer (read-only) · **Verdict:** **fix-first** (2 High in the path policy / script directives, 3 Medium; all contained) · **Score:** 6.5/10

> **Resolution 2026-09-17 (same day):** every finding applied — H1 (`ArgsKey` requires the bridge's `args` field on `EtabsScriptGlobals`, the engine's `ScriptArgs`, exactly one argument; run-time refusal when a declared key is absent or not a string), H2 (engine: `ScriptGuard.Check` refuses `#r`/`#load` for every host — `McpShared` tests 162 → 164 and 60 → 62; bridge test pins it), M1 (static rules re-run on the normalised path; `%LocalAppData%\HPEtabs\McpBridge\**` off limits), M2 (preview before the opt-in check — ADR order kept; theory rows for D + none/dryRun with the opt-in off), M3 (cancel during the save reported as cancelled, `TimedOut=false`), L1–L8 (non-string key, prune by name, `nameof`, `Save("")` for an optional path, unknown receivers null and skipped, drive-root model + ADS refused, `fullPath` flagged on `cHelper.Create*`, harness mirrors `SanitizeLabel`), Info (`DefaultMaxTimeoutSeconds`, `RolledBack:true` on "nothing ran" results, tool description names the path screening). Bridge tests 166 → **184**, server 17/17, live `-Phase bridge` 48/48 after the fixes. Details: `phase-02-bridge-runtime.md` › "Review round".

## Scope
- Working tree on top of `555f3df` (HEAD moved to `66241ac` = unrelated AutoCAD commit; ETABS work still uncommitted). Files re-read after the 08:13–08:18 edits (`EtabsScriptRunner.cs`, `EtabsExecutor.cs` → 248 lines, new `EtabsExecutor.Worker.cs`, `.Audit.cs`).
- Bridge: `Service/EtabsTierTable|EtabsTierAnalyzer|EtabsPathPolicy|EtabsSnapshotManager|EtabsFingerprint|EtabsUnitsPolicy|EtabsScriptRunner.cs`, `EtabsExecutor*.cs`, `BridgeEntry.cs`, VM/View/App, csproj, `Resources/etabs-oapi-{tiers,index}.txt`, `tools/generate-oapi-tier-fixture.ps1`, harness. ~1 250 LOC bridge + ~900 LOC tests.
- Not reviewed: `McpShared/` (guard/compiler/pipe) — except where the phase's own acceptance depends on it (finding H2).
- Tests **not re-run by the reviewer** (166 + 17 claimed by the lead; the bridge exe was running and locked `bin\` for part of the session). Live: the brief says the attached `bridge` phases were not run; `reports/phase-02-bridge-runtime.md` (08:20) says `-Phase bridge -Runs 2` → 2 × 48 PASS. Not verified here; nothing below depends on it.

## Evidence: scratch probe (per memory rule — gate claims are proven, not argued)
`net8.0-windows` console in the session scratchpad, `ProjectReference` → bridge csproj, same references/imports/globals as `BridgeEntry.CompilerReferences`, `EtabsTierTable.Embedded`, `EtabsScriptRunner.PathRefusals(verdict, args, @"C:\Models\Tower")`. Printed verdicts (verbatim, trimmed):

| # | script | guard | tier / hits | static refusals | run-time refusals |
|---|---|---|---|---|---|
| A1 | `sapModel.File.Save(args.Str("out", @"\\srv\share\evil.EDB"))`, args `{}` | 0 | D `cFile.Save` | [] argKeys=[out] | **[]** |
| A3 | `class ScriptArgs { public string Str(string k) => @"\\srv\…"; } sapModel.File.Save(new ScriptArgs().Str("out"))` | 0 | D | [] argKeys=[out] | **[]** |
| A6 | `sapModel.File.Save(args.Str("out"))`, args `{}` | 0 | D | [] | **[]** (Str → null) |
| A5 | same, args `{"out":"\\\\srv\\x.EDB"}` | 0 | D | [] | `args.out … UNC` ✓ |
| B1 | `#r "System.Diagnostics.Process"` + read | 0 | **compiles**, R | | |
| B2 | `#load "C:\Users\Public\probe-load.csx"` (file: `System.IO.File.WriteAllText(…); sapModel.Analyze.RunAnalysis()`) | **0** | trees=2, D `cAnalyze.RunAnalysis` (analyzer sees the loaded tree) | | |
| B3 | `#r "C:\Users\Public\probe-evil.dll"` + `return Evil.Marker.Go(sapModel)` (DLL: `m.Analyze.RunAnalysis()` + `File.WriteAllText`) | **0** | **R, hits=[]** | | |
| C1 | `nameof(sapModel.FrameObj.Delete)` | 0 | D (false positive) | | |
| C3/C4/C9/C14–C17 | cast via `object`, lambda in `Select`, alias chain, `Action` closure, generic `Id<T>`, local function param, tuple deconstruct | 0 | bound correctly (D/W) | | |
| C5/C6/C12 | `File?.Save(@"\\srv…")`, `Save(FileName: …)`, raw `"""…"""` literal | 0 | D | `PATH … UNC` ✓ | ✓ |
| C11/C20 | interpolated / `const` path | 0 | D | `PATH … literal or args.Str` ✓ | |
| C7/C18 | `Save()` / `Save("")` | 0 | D | none / `empty path` | |
| C10 | `sapModel.GetType().Name + sapModel.ToString()` | 0 | R, hits=[] ✓ | | |

`RuntimeRefusal(value, @"C:\Models\Tower")`: `…\Local\HPEtabs\McpBridge\snapshots\x.EDB` → off limits ✓ · `…\HPEtabs\.\McpBridge\…` → **ALLOWED** · `…\HPEtabs\\McpBridge\…` → **ALLOWED** · `…/HPEtabs/x/../McpBridge/audit/x.EDB` → **ALLOWED** · `..\..\Windows` → refused ✓ · `\\?\C:\…` → refused ✓ · `C:\Models\Tower2\` → refused ✓ · `C:\Models\Tower\out.EDB:stream` → allowed · model dir `C:\` → whole drive allowed.
Reflection over the installed wrapper: **0** interface methods missing from the fixture; only public class is `ETABSv1.Helper`; `cHelper.CreateObject(fullPath)` is not `path=`-flagged (exact-name list). Public statics in the bridge assembly: no executor/analyzer/snapshot-manager instance (`BridgeEntry.Dispose/Start`, pure helpers only) ✓. Directive scan: `GetReferenceDirectives()/GetLoadDirectives()` sees `#r`/`#load` (fix path for H2); `WithMetadataResolver(null)` NREs inside Roslyn — not a viable fix.

## Acceptance criteria
| # | Criterion | Verdict | Evidence |
|---|---|---|---|
| a | Tier from fixture via semantic binding; unknown → D; alias/cast/lambda bound | **PASS with one hole** | `EtabsTierAnalyzer.cs:81-118`; probe C3–C17; unknown → D `:108-111`, test `A_member_the_table_does_not_know_is_destructive`. Hole: `#r` DLL → R (H2) |
| b | `transaction × dryRun` matrix per ADR-02 | **PARTIAL** | R runs `EtabsExecutor.cs:123`; W/D none/dryRun → preview `:123`, `Audit.cs:34-45`; D off → `-32001` `:117-119` (test line 35); D on → path policy + `[destructive]` + 600 s `:128`. Deviation: D + dryRun/none with opt-in **off** → `-32001` instead of the ADR's preview (M2) |
| c | Snapshot unconditional in budget, presave conditional, UNC/no-path → `-32003`, `started` before save, name only, label safe | **PASS** | `EtabsScriptRunner.cs:68-92` (budget clock `:50-51` precedes save), `EtabsSnapshotManager.cs:49-57,74-104,128-143`; `beforeSave` callback `EtabsExecutor.cs:141-142` now fires after the run-time path check and before `Prepare` (busy refusals no longer leave a stray `started`); tests `EtabsSnapshotManagerTests` 8. Nit: cancel during the save is reported as a timeout (M3) |
| d | Fingerprint add/delete only | PASS | `EtabsFingerprint.cs:45-66`, tests 3. Comment vs code mismatch on failed receivers (L5) |
| e | Path policy static + run-time (`args`) | **FAIL** | static ✓ `EtabsPathPolicy.cs:24-33`; run-time bypassed by `Str` fallback / fake `ScriptArgs` / absent key (H1, probe A1/A3/A6) and forbidden folders reachable through `\.\`, `\\`, `/../` (M1) |
| f | Units forced/restored | PASS | `EtabsUnitsPolicy.cs:23-47`, `EtabsRunnerPartsTests` 2 (restore after throw pinned) |
| g | OAPI on STA worker; timeout always fails; `StripPaths`; no public static executor; flag only from VM; guard denies `HPEtabs.McpBridge` | PASS | `EtabsExecutor.Worker.cs:28-40,54-68`; `:109` re-throws after a script that returned; `StripPaths` `EtabsScriptRunner.cs:129`, `EtabsExecutor.cs:170`; probe public-static list; `GuardProfile.cs:92` + `ScriptGuard.IsDeniedNamespace` prefix match (`HPEtabs.McpBridge.Service` covered); `DestructiveOperationsEnabled` only via `EtabsBridgeStatusViewModel.cs:131-143` |

## Findings

### High
**H1 — Run-time path policy bypassed by `args.Str(key, fallback)`, a script-defined `ScriptArgs`, or a key absent from `args`.** `EtabsTierAnalyzer.cs:182-190` accepts any invocation whose containing type is *named* `ScriptArgs` and only records the key; `EtabsScriptRunner.PathRefusals:169-189` screens values that *exist* in `args`. So `sapModel.File.Save(args.Str("out", @"\\srv\share\evil.EDB"))` (A1) or `ExportFile(…)`/`GetTableForDisplayCSVFile(…)` with a fallback reach ETABS with an unscreened path; A3 (fake class) and A6 (`Str` → null) likewise. ADR-02 §4 promises "không có gì khác qua được". Needs opt-in #2 (all path members are D), which is exactly the case the policy exists for; an AI writes `args.Str("out", "C:\\Temp\\x.csv")` innocently. **Fix:** (1) `ArgsKey`: compare `method.ContainingType.ToDisplayString() == "HPRebar.McpBridge.Core.Scripting.ScriptArgs"` and refuse a call with more than one argument (or screen a literal second argument as a `PathLiteral`); (2) `PathRefusals`: every key in `verdict.PathArgKeys` must be present in `args` as a JSON string, else `PATH "args.<key> is required for a file-taking member"`; (3) theory rows for A1/A3/A6 in `EtabsTierAnalyzerTests` + `EtabsPathPolicyTests.Run_time_screening…`.

**H2 — `#r` / `#load` directives are accepted by the script compiler; `#r <dll>` escapes the tier analyzer entirely, `#load` escapes the guard.** `ScriptCompiler.cs:44-48` keeps `ScriptOptions.Default` resolvers, `ScriptGuard.Check` (`ScriptGuard.cs:60-66`) walks nodes only (directives are trivia). Probe B3: a DLL on disk calling `m.Analyze.RunAnalysis()` + `File.WriteAllText` runs as **tier R** — no opt-in #2, no snapshot, `[tier:R]` in the audit. B2: `#load`ed source with `System.IO.File.*` passes the guard (the analyzer does see the loaded tree). Engine code (`McpShared`), all four hosts — out of this diff, but it falsifies criterion (a). Threat model: the AI must first place a file on disk, so it is a deliberate escape, not an accident; still one line defeats the only ETABS safety net. **Fix (engine, 4 lines):** in `ScriptGuard.Check` after `ParseText`, `var root = tree.GetCompilationUnitRoot(); foreach (var d in root.GetReferenceDirectives().Cast<DirectiveTriviaSyntax>().Concat(root.GetLoadDirectives())) Report(d, "#r / #load are not allowed: scripts use the bridge's references only");` + one test per host profile in `HPRebar.Mcp.Server.Core.Tests` (guard) and a pin in `EtabsExecutorRefusalTests.Guard_and_compile_rejections_come_first`. Do **not** null the resolvers (Roslyn NREs). Probe artefacts in `C:\Users\Public` were deleted.

### Medium
**M1 — Forbidden-folder rule checked on the raw string only.** `EtabsPathPolicy.RuntimeRefusal:38-46` runs `StaticRefusal(value)` before `GetFullPath`, then allows anything under `LocalRoot`; `…\HPEtabs\.\McpBridge\snapshots\…`, `HPEtabs\\McpBridge`, `HPEtabs/x/../McpBridge/audit` all print ALLOWED. With opt-in #2 a `Save(path)` (= save-as) onto a prerun copy destroys the safety net and re-points the model into the snapshot folder. **Fix:** after `full = GetFullPath(value)`, `if (StaticRefusal(full) is { } r2) return r2;` and additionally refuse `IsUnder(full, Path.Combine(LocalRoot, "McpBridge"))`; theory rows for the three shapes.

**M2 — D + dryRun / `none` with opt-in off → `-32001`, ADR-02 table says static preview.** `EtabsExecutor.cs:117-123` orders the opt-in check before the preview. ADR §1 matrix: "bất kỳ, dryRun=true → D: static preview (không 'refused up front' riêng — cùng một preview)". A preview runs nothing and needs no user consent, so the AI should get the member list without bothering the user. **Fix:** swap lines 117-119 below line 123 (or amend the ADR row explicitly — sticky-decision rule); add rows `(D, None, off)` / `(D, dryRun, off)` to `EtabsExecutorRefusalTests` theory at line 57 (today only W is pinned; the `bridgedestructive` D5 scenario only covers opt-in **on**).

**M3 — `cancel_execution` during the forced save is reported as "Timed out", `TimedOut=true`, audit outcome `timeout`.** `Prepare` receives the *linked* token (`EtabsScriptRunner.cs:80`) and can only say `TimedOut(…)` (`EtabsSnapshotManager.cs:83,95,99,124-125`). **Fix:** pass `timeoutSource.Token` for the verdict and check `cancelSource.IsCancellationRequested` in the runner to rewrite `Message`/`TimedOut=false` ("cancelled while saving…"); or give `Prepare` both tokens.

### Low
- **L1** Declared path key whose JSON value is not a string (`{"out": 12}`) is never screened (`StringValues` yields strings only) → `Save("12")` relative to ETABS's cwd. Fold into H1 fix (2): require a string.
- **L2** `Prune` (`EtabsSnapshotManager.cs:148`) orders by `LastWriteTimeUtc`; `File.Copy` preserves the source mtime, so a presave taken after the user restored an older file sorts as oldest and is pruned first. Order by name (the `yyyyMMdd-HHmmss` prefix) instead.
- **L3** `nameof(sapModel.FrameObj.Delete)` classifies D (C1). Skip a `MemberAccessExpression` whose ancestor is `nameof(...)` (`InvocationExpression` with `IdentifierName "nameof"`).
- **L4** `Save("")` refused as "empty path" while `Save()` passes; same ETABS semantics (save in place). Either accept the empty literal for optional path parameters or say so in the message.
- **L5** `EtabsFingerprint.Take` comment (`:17`) says a refusing receiver "is not diffed against a full one" but `Diff:51-60` diffs it → a transient `GetNameList` failure logs "+N"/"-N" ("another writer?"). Store `null` for a failed receiver and skip it in `Diff`.
- **L6** Model at a drive root (`GetDirectoryName` = `C:\`) allows the whole drive; ADS suffix `:stream` allowed. Refuse when `modelDirectory` is a root; refuse a `:` after index 1.
- **L7** `cHelper.CreateObject(fullPath)` / `CreateObjectHost(…, fullPath)` not `path=`-flagged (`generate-oapi-tier-fixture.ps1:29,45` exact names). Unreachable today (guard denies `Helper`; no factory returns `cHelper`) — add `fullPath` for defence in depth and regenerate.
- **L8** Harness `live-verify.py:201` builds the snapshot folder from the raw model stem; the manager uses `SanitizeLabel(stem, 60)` — diverges for a model name with spaces/dots.

### Info
- `maxTimeoutSeconds` W/R ceiling `120` hard-coded (`EtabsExecutor.cs:128`); use the engine constant the other hosts use.
- Run-time refusals carry `ScriptDiagnostic(0, 0, …)` — fine, but `RolledBack` stays false on the "nothing ran" early results (`EtabsScriptRunner.cs:76,84`); the preview sets it true. Pick one.
- `A7`: an unrelated `args` string that merely looks like a path (`"note": "see D:\\notes"`) refuses a script that takes paths — intended by ADR §4(c), worth a sentence in the tool description.

## Closed since the phase-1 review
Phase-1 Critical/High (alias `var m = sapModel`, cast, `?.` MemberBinding, lambda, `StartsWith`/`Clear`/`Environment.NewLine` false positives) — all closed by the semantic analyzer: probe C3/C4/C5/C9/C10/C14–C17 and test theory `EtabsTierAnalyzerTests` lines 24, 48-55.

## Positives
- Fixture is complete against the installed wrapper (reflection: 0 missing) and pinned to the CHM index; `Parse` rejects duplicates/bad tiers.
- Semantic binding is the right tool: every receiver shape in the probe binds; `object` members skipped; navigation properties skipped; enum fields skipped.
- Snapshot sequence is honest and testable (`Func<int> save` injection), label/bucket assertions, same-second suffix, file-name-only on the wire, `started` audit now placed after the run-time check and before the save.
- Units restore proven even after a throw; disconnect rethrown, never swallowed in fingerprint/units paths.
- View: every colour/spacing via `DynamicResource`; probes removed; no plan/finding codes in code comments; executor split into three partials under 300 lines.
- Tests are not vacuous: refusal theories assert the diagnostic id **and** the reason; executor tests drive the real pipeline without ETABS.

## Score and verdict
**6.5/10 — fix-first.** H1 + M1 are a few lines each in `EtabsTierAnalyzer.ArgsKey`, `EtabsScriptRunner.PathRefusals`, `EtabsPathPolicy.RuntimeRefusal` + theory rows; M2 is a two-line reorder (or an ADR amendment); H2 is an engine change in `McpShared/ScriptGuard.Check` that benefits all four hosts and should ship before phase 3 seeds are proposed. Re-review after: rerun the analyzer/path theories and `-Phase bridge` once.
