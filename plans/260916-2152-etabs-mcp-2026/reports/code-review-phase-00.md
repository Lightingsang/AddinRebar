# Code review — phase 0 (McpShared additive ETABS contracts + engine tests)

Date 2026-09-16 · reviewer: code-reviewer · read/grep only (no build/test — gate running in background) · suites reported by implementer: `HPRebar.Mcp.Server.Core.Tests` 157/157 (128 + 29).

## Scope

- Diff: 16 modified + 1 new file under `McpShared/` (`git diff --stat`: +151/−14). All 15 code files map to plan rows #1–#13 (rows #10 and #12 each touch 2 files); + `Fakes/FakeRevitExecutor.cs` (+3, test support for #7) + `EtabsProfileTests.cs` (381 lines, 29 tests).
- `HostScriptContracts.cs` `"HPAutoCad.Aec"` (line 32) is **already in HEAD** (8fcdb05) — the ETABS block (lines 70–93) is the only uncommitted hunk in that file. No merge hazard.
- Nothing outside the table. No `ETABSv1`/`Autodesk.*` in any `McpShared` csproj; `ETABSv1` appears only as a string in `EtabsImports` and in doc comments.

## Verdict

**Score 9/10 — approve.** Every row implemented as specified, all three old messages byte-identical when the knobs are null, no tools/list-visible change by inspection, `HostProfile.Revit` untouched, only one `IHostProfile` implementer in the repo. One pre-existing guard weakness (not introduced here) is worth a follow-up row because it directly undercuts the new ETABS `HPRebar.McpBridge.Core.Host` denial; two small contract nits.

## Findings (ranked)

### H1 — pre-existing: `ScriptGuard` namespace denial and member-position identifiers are bypassed by `global::` (all hosts, all profiles)

`McpShared/HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs:68-74,101-107,126-134`. In expression context Roslyn parses `global::System.IO.File.WriteAllText(a, b)` as MemberAccess chains whose leftmost node is `AliasQualifiedNameSyntax`. The walker's `dotted` string is then `"global::System.IO.File.WriteAllText"`, which neither equals nor starts with `"System.IO."`, so `IsDeniedNamespace` returns false; `File` sits in member position (`access.Name == node`) so `VisitIdentifierName` skips it; `WriteAllText` is not a denied member. Same for `global::System.Diagnostics.Process.Start(...)` and — the reason it matters for this phase — `global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current` (`McpBridgeHost.cs:54` is a public static singleton). Type-context uses (`new global::System.IO.FileStream(...)`) are still caught because the identifier is under a `QualifiedNameSyntax`, not a member access. Confidence ~90 % from the parser shape; not executed (no dotnet allowed in this review).

Not introduced by this diff; ADR-04 says the guard is defence-in-depth, not a sandbox. Still a one-line fix: normalise `dotted` by stripping a leading `global::` in `IsDeniedNamespace` (or override `VisitAliasQualifiedName`), plus one `[Theory]` row per profile. **Outside the phase-0 table — the lead decides whether to add a row (base list "Không đổi") or defer.** Recommended: add now; it is additive and byte-identical for every script that does not write `global::`.

### M1 — `EtabsInfo` non-nullable strings vs. the documented `IsAttached = false` state

`McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs:106-116`. Plan §Architecture says context is returned before attach (`IsAttached=false`). In that state `OapiVersion`, `PresentUnits`, `DatabaseUnits` cannot be known; the bridge must send `""` or `null`, and `null` into a non-nullable positional parameter is silently accepted by STJ (NRT lie for every consumer). `AutocadInfo.CurrentLayout` (`:69`) is the in-repo precedent for `string?`. Field count and names stay exactly the 10 the plan pins; only nullability changes; wire identical. Recommend `string? OapiVersion, string? PresentUnits, string? DatabaseUnits` — now, or at the latest in phase 2 before a bridge serialises the type.

### M2 — `ExecuteResult.Snapshot` bypasses the server's path-stripping last line of defence

`McpShared/HPRebar.Mcp.Contracts/Messages/ExecuteResult.cs:46-51` documents "file name (never a directory)"; `ResultFormatter.FromExecute` (`Services/ResultFormatter.cs:27-43`) strips paths from `Message` and `Diagnostics` only, described at `:75` as the "last line of defence against a machine path leaking into the model's context". A future bridge sending a full path in `Snapshot` reaches the model untouched. Additive fix in the formatter (`if (result.Snapshot is not null) result.Snapshot = Path.GetFileName(result.Snapshot);` or `StripPaths`) is byte-identical for null. Outside the table (`ResultFormatter` is not listed) → lead's call; alternatively pin it bridge-side in phase 2 with a test.

### L1 — un-normalised `transaction` forwarded to `*.analyze`

`ToolLifecycleService.cs:75` (`Transaction = TransactionModes.Normalize(input.Transaction) ?? input.Transaction`) then `:91` forwards `record.Transaction` before `ToolValidator` rejects an invalid mode. Existing bridges ignore the key; the ETABS bridge (phase 2+) must treat any unknown string as null, not throw. Note for phase 2, no engine change.

### L2 — test file size and two long-running tests

`EtabsProfileTests.cs` is 381 lines (rule < 300; sibling `NavisProfileTests.cs` 231). Consider splitting constants/guard/profile (pure) from wire/registry (pipe) tests. `TimeOutAsync` leaves a fake run of 40 × 100 ms (4 s) on the pool after the 300 ms client timeout, twice per test — deterministic (13× margin) but wasteful; `ProgressSteps = 10` keeps a 3× margin. Not blocking.

### L3 — plan success criterion count

Phase file `:74` says "`git diff --stat McpShared -- '*.cs'` = đúng 13 file". Rows #10 and #12 are two files each → 15 code files (+ fake + test = 17). Fix the number in the plan so the gate does not false-fail.

## Explicit checks requested

**(a) Byte-identity.** `RevitBridgeClient.cs:84-87` — old `$"…{timeout.TotalSeconds:0}s. The timeout is cooperative: …until it does."` ≡ new `$"…{timeout.TotalSeconds:0}s. " + (hint ?? $"The timeout is cooperative: …until it does.")` — identical concatenation. `:140-144` not-connected — same pattern, identical. `RequestDispatcher.cs:139-140` — `_executionDisabledMessage ?? <old interpolated string, unchanged>`. Confirmed against `git diff`; pinned by tests `EtabsProfileTests.cs:175,203,213`. `tools/list`: no tool attribute, description, schema, `ScriptContractSummary` or seed touched; the two new `IHostProfile` members are read only in `RevitBridgeClient`. Wire: `BridgeJson.cs:18` `DefaultIgnoreCondition = WhenWritingNull`; `ContextService.Shape` (`:52-58`) returns the object for Revit and `SerializeToNode(context, BridgeJson.Options)` for others; `ResultFormatter.Text/FromExecute` both `BridgeJson.Serialize`; `RegistryJson.cs:15` also `WhenWritingNull`. So `etabs`/`snapshot`/`transaction` are absent when null (test `:284-302`). Executable proof remains plan step 5 (rebuild 3 exes → `tools/list` before/after).

**(b) `AnalyzeRequest(string Code, string? Transaction = null)`.** All constructions: `ToolLifecycleService.cs:54` (2 args), `PipeRoundTripTests.cs:226,235` (1 arg), `EtabsProfileTests.cs:294,296`. No `Deconstruct`, `with`, or positional pattern anywhere in `HPRebar/`, `HPAutoCad/`, `HPNavis/`, `McpShared/`. Host bridges only read `request.Code` (`McpBridgeExternalEventHandler.cs:126`, `MainThreadExecutor.cs:152`, `NavisMainThreadExecutor.cs:193`). Bridge deserialises via `JsonRpcEnvelope.ParamsAs<T>` → `BridgeJson.Options` (default `JsonUnmappedMemberHandling.Skip`), so deployed bridges ignore the new key; the server omits it when null (ad-hoc `AnalyzeAsync` callers `ToolifyPrompts.cs:41`, `RunHistoryTools.cs:34` still send the old wire). net48 asset: `HPRebar.Mcp.Contracts.csproj` targets `netstandard2.0;net48`, `LangVersion latest`, `Nullable enable`, `Polyfill 11.0.1` (`IsExternalInit`, `required`) already required by the existing records; an optional parameter on a positional record adds nothing new. STJ 10.0.12 (net462 asset) fills missing ctor params with their defaults (test `:298-301`). `FakeRevitExecutor` is linked into **four** projects (HPRebar/HPAutoCad/HPNavis server tests and `HPRebar.McpBridge.Core.Net48Tests`); the addition is an auto-property + assignment — compiles on net48.

**(c) `ConfigureOptions` seed.** `McpServerHost.cs:100-107` sets `HostId` + `HostVersion = profile.DefaultVersion` in `Configure` **before** `Bind`, so configuration still wins: `Bridge:HostVersion` (test `:240-241`) and the `RevitVersion` alias (`BridgeOptions.cs:20-24`, still bound by `Bind` after the seed; existing `ProfileOptionsBindingTests.cs:67` `Bridge:RevitVersion=2025` covers it and passes). `PipeName` setter (`BridgeOptions.cs:34-38`) drops a value equal to `DefaultPipeName`; with the seed applied first the binder's write-back equals `PipeNaming.For(HostId, DefaultVersion)` and stays null, so a later version change still re-derives the pipe. All three shipped `appsettings.json` carry `2026` (`HPRebar…:13 RevitVersion`, `HPAutoCad…:13`, `HPNavis…:13 HostVersion`) = the three profiles' `DefaultVersion` = the old hard default → no behaviour change. `PostConfigure` must **not** set it: it runs after `Bind` and would erase the user's version knob (needed to pick Revit 2025's pipe); `HostId` is forced there precisely because config must never redirect an exe to another host's pipe, and `Validate(IsValid(profile.ValidVersions))` already rejects an out-of-range version.

**(d) `IHostProfile` implementers.** Only `HostProfile : IHostProfile` (`HostProfile.cs:12`). `RevitHostProfile.cs:11`, `AutocadHostProfile.cs:17`, `NavisHostProfile.cs:21` are `HostProfile` object initialisers → the two new `init` props default to null. No mock/fake/`Substitute` of `IHostProfile` in any test project. `WithHostAssembly` copies both (`HostProfile.cs:107`, test `:127-137`).

**(e) `GuardProfile.Etabs`** (`GuardProfile.cs:85-92`) equals plan row #8 token for token (identifiers `Helper`, `MessageBox`; 7 members; namespaces `System.Windows.Forms`, `HPEtabs.McpBridge`, `HPRebar.McpBridge.Core.Host`). `ScriptGuard.cs` base lists untouched (not in diff). Namespace match is exact-or-prefix-with-dot (`ScriptGuard.cs:72-73`), so `HPRebar.McpBridge.Core.Host` does not catch `…Core.Scripting` (which every host imports) nor `…Core.Hosting`-style siblings. Only the ETABS profile carries it; Revit/AutoCAD/Navis profiles unchanged. See H1 for the `global::` gap.

**(f) Tests.** Pipe names `hpetabs-mcp-test-<guid>` / `hpetabs-mcp-nobody-<guid>`; temp roots `%TEMP%\hpetabs-registry-<guid>` removed best-effort after `SqliteConnection.ClearAllPools()` (same pattern as `NavisProfileTests.cs:167-189`; leaks only on assertion failure). Timeout test: client `WaitAsync(300 ms)` vs fake 4 s → deterministic on any machine. Not-connected test: single connect attempt at 200 ms (`RevitBridgeClient.cs:131-135`, no retry loop) × 2 clients. No machine paths, no ETABS install, no `ETABSv1` reference — `HostAssembly = typeof(EtabsProfileTests).Assembly`. Row #14 pinned at `:335-380`: `ToolManager.RunAsync` (`ToolManager.cs:178-186`) catches only `BridgeTimeoutException`; `BridgeErrorException(ExecutionDisabled)` propagates before `Record` → 6 refusals, `Runs == 0`, still `Published`; contrast run recorded.

**(g) Style.** All touched files keep file-scoped namespaces; comments explain why (COM boundary, version-numbering, add-in vs separate program). Grep of the diff + test file for `red-team|ADR|phase 0|#14|row #|finding|audit` → 0 hits. Array initialisers match the surrounding `{ … }` / `new[] { … }` style; `AnalyzerProfile.Etabs` uses `Array.Empty<string>()` (net48-safe, unlike a collection expression targeting `IReadOnlyCollection`).

**(h) Scope creep.** None. 15 code files ↔ rows #1–#13, + fake + test.

## Positive

- Every knob is opt-in with a null default and the old text is kept verbatim in the `??` right-hand side rather than re-typed — the three assertions at `:175/:203/:213` would catch a retype.
- Row #14 test proves the "refusal never counts" invariant with a contrast case in the same test.
- Doc comments on `EtabsInfo`, `Snapshot`, `AnalyzeRequest.Transaction` say what a bridge must guarantee, not what phase produced them.

## Recommended actions

1. Lead: decide on H1 (add a table row for the `global::` normalisation + tests) — additive, byte-identical for existing scripts.
2. Apply M1 (`string?` on the three ETABS strings) before any bridge serialises `EtabsInfo`.
3. Decide M2 (strip `Snapshot` in `ResultFormatter`) or pin it bridge-side in phase 2.
4. Fix the "13 file" count in the phase file (L3); optionally split the test file (L2).
