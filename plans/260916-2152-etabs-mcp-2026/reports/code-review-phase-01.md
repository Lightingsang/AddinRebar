# Code review — phase 1 `HPEtabs/` (bridge app + server exe + tests + harness)

**Date:** 2026-09-17 · **Reviewer:** code-reviewer (read-only) · **Verdict:** **fix-first** (3 contained fixes in `EtabsTierGate` + tests, then approve) · **Score:** 6.5/10

> **Resolution 2026-09-17:** findings 1–12 and 14 applied (13 deferred to phase 2: spike buttons leave the window; labels no longer carry plan codes). Bridge tests 26 → 46, server 17/17, harness `disabled`/`detached` 2/2 + 15/15, `nomodel` 1/1 with the new assertion. Details: `phase-01-spike.md` › "Review round".

## Scope

| Item | Value |
|---|---|
| Files | 46 new under `HPEtabs/` (bridge 21 incl. XAML/theme, bridge tests 3, server 9, server tests 3, harness 4, solution 5) |
| LOC | 3 495 (cs/xaml/py/ps1) |
| Built | `dotnet build HPEtabs/HPEtabs.slnx -c Debug` → 0 warnings, 0 errors; `ETABSv1.dll` absent from `bin/` |
| Tested | `HPEtabs.Mcp.Server.Tests` 17/17; `HPEtabs.McpBridge.Tests` **26**/26 (spike report says 25 — 19 theory rows + 3 facts + 4 serializer) |
| Not run | bridge exe, ETABS, any `*.ps1` harness (a person is driving ETABS) |
| Extra evidence | scratch console in the session scratchpad referencing `HPEtabs.McpBridge.csproj`, calling `EtabsTierGate.Inspect` + `ScriptGuard.Check(GuardProfile.Etabs)` on 18 samples — results quoted verbatim in findings 1–3 |
| Mirrors compared | `HPNavis/HPNavis.McpBridge/{NavisExecutor, View, ViewModel}`, `HPAutoCad/HPAutoCad.McpBridge/BridgeEntry.cs`, `HPNavis.Mcp.Server*` |

## Acceptance criteria (a)–(i)

| # | Criterion | Verdict | Evidence |
|---|---|---|---|
| a | Only `../McpShared/*` referenced; `McpShared/` untouched; no machine paths / secrets / opt-in bypasses; `ETABSv1.dll` never copied; server never references the wrapper | **PASS** | grep `HPRebar/\|HPAutoCad/\|HPNavis/` over csproj/slnx/props = 0; `git diff --stat -- McpShared` empty (only untracked `McpShared/.claude/`, not phase-1 work); grep `HPETABS_SPIKE\|ENABLE_EXECUTION` = 0; absolute paths only under `obj/` (gitignored); `HPEtabs.McpBridge.csproj:27-30` `<Private>false</Private>`, no `<Content>` for a dll (`HPEtabs.Mcp.Server.csproj:29` is `appsettings.json` only); server csproj = one `ProjectReference` to `HPRebar.Mcp.Server.Core` (`HPEtabs.Mcp.Server.csproj:25`); `ETABSv1` appears in server text only as the script namespace in descriptions |
| b | Threading / COM | **PASS with notes** | STA foreground worker `EtabsExecutor.cs:60-62`; control lane drained before `OnTick` `:262-263`; `_running` volatile `:39`, `_busy` CAS `:109`; `IsQuiescent` not-attached ⇒ true `:250`; disconnect → `Detach` + `-32003` `:203-205` / `EtabsAttachment.cs:138-143`; `Dispose` order cancel → stop → join 10 s → `FailAll` → attachment → audit `:289-303`; `_gate` + volatile `_attached` `EtabsAttachment.cs:22-26`; `Exited` hook only for exactly one process `:77-78`; re-attach without restart (VM `AttachAsync`). Notes: findings 5, 6, 9, 10 |
| c | Safety: tier gate bypasses, guard coverage, destructive flag reachability, probes off the pipe | **FAIL on the gate** | Bypasses reachable in phase 1 — finding 1 (Critical), 2, 3 (High). Guard profile `GuardProfile.cs:85-92` covers `Helper`, 7 `cOAPI` lifecycle members, `HPEtabs.McpBridge`, `HPRebar.McpBridge.Core.Host` ✓. `BridgeEntry._executor` private static `BridgeEntry.cs:43` ✓; script references lack PresentationFramework so `Application.Current.MainWindow.DataContext` cannot be spelled ✓; `SpikeProbes` only called from `EtabsBridgeStatusViewModel.cs:97-110` through `RunOnWorkerAsync` (control lane, no dispatcher path) ✓ |
| d | Units / runner / serializer | **PASS** | `GetPresentUnits` → `SetPresentUnits(kN_mm_C)` → restore in `finally` `EtabsScriptRunner.cs:59-61,100-103`; timeout CTS created before the units calls `:49-50` so prep counts against the budget; disconnect rethrown `:90-94`; a script that returned after the timeout still fails `:78`; serializer walks `IDictionary` through `IDictionaryEnumerator` `EtabsResultSerializer.cs:83-90`, proxies summarised `:72,98-99`, 500-item + byte cap `:16,41-46` |
| e | Context reader | **PASS with a phase-2 item** | One unguarded `GetPresentUnits()` first `EtabsContextReader.cs:40`; rooted-path rule `:43`. **Today, after a `Save`, `GetModelFilename(true)` returns `X.$et` and the reader publishes `docTitle "X.$et"` / `docPath "...\X.$et"` verbatim** (`:46-47`) — finding 7 |
| f | Server profile / descriptions / prompts | **PASS with notes** | Profile = ADR-05 §1 table (`EtabsHostProfile.cs:36-67`: HostId, ServerName, EnvPrefix, 22/[22], 9 categories, 600 s, CLI, both hints). `ToolDescription` is the phase-4 target and over-promises W runs + snapshot (`ExecuteEtabsCodeTool.cs:95`), but the bridge answers `"Writing runs (save + .EDB snapshot first) are not available in this bridge build yet."` (`EtabsExecutor.Audit.cs:41`) — honest enough at call time. Prompt examples use the real signatures `GetSection(name, ref prop, ref sauto)` / `GetNameList(ref n, ref names)` and pass the gate (verified by the probe: no `StartsWith`/`Clear`/`NewLine`). Description inaccuracies: finding 8 |
| g | Tests | **PASS with notes** | Deterministic (GUID pipe names, in-memory config), no machine paths (`EtabsHostProfileTests.cs:57`, `EtabsToolsOverPipeTests.cs:254-255`); bridge tests documented as needing ETABS (`README.md:23,35`, csproj comment); description cap 1 800 with the reason in the test (`EtabsHostProfileTests.cs:131-132`) and the deviation recorded in `phase-01-spike.md` §"đổi thiết kế" 5. Gaps: no test for `Results.*` (would have caught finding 2), no test for the alias/`?.` bypasses, count drift 25 vs 26, fictitious path `sapModel.AnalysisResultsSetup` in `EtabsTierGateTests.cs:18` (real: `sapModel.Results.Setup`) |
| h | Harness | **PASS with notes** | Only the bridge exe is started/stopped (`harness-common.ps1:25-38,120-133`), ETABS asserted-running never driven (`:20-22`); UIA scoped by `ProcessIdProperty` + title (`:41-58`); pid file (`spike-step.ps1:36,54`). E12/E15 are `check(..., True, ...)` — can never fail (finding 11); `stop` trusts the pid without a process-name check (finding 12) |
| i | Style | **PASS with notes** | File-scoped namespaces everywhere (`Program.cs` is top-level statements); comments explain why; no `red-team`/`ADR`/`phase N`/`#14` in code. Spike codes `E9/E10/E14` live in `SpikeProbes.cs`, the VM and the XAML button labels (finding 13). `EtabsExecutor.cs` is 304 lines (rule < 300; already a partial). MVVM: code-behind = `InitializeComponent` + `DataContext` + `CloseRequested` (same as Navis), no `DataContext` in XAML (`d:DataContext` design-time only), colours/spacing/fonts all `DynamicResource`; fixed `Width/MinWidth` numbers match the Navis view |

## Findings (ranked)

### Critical

**1. The tier gate is bypassed by ordinary C# idioms — W and D members run as "read-only" in the phase-1 build.** `EtabsTierGate.cs:80-96` classifies a member only when its receiver chain roots at the bare identifier `sapModel`/`etabs` (`RootIdentifier`, `:108-118`), and a non-rooted member is dropped unless its *name* is destructive (`:92`). `?.` produces `MemberBindingExpressionSyntax`, which is not a `MemberAccessExpressionSyntax` at all, so it is never visited. Verified against the built assembly:

```
ReadOnly    guard=0  hits=[]  <- var m = sapModel; return m.FrameObj.SetSection("F1", "C40x40");
ReadOnly    guard=0  hits=[]  <- var m = sapModel; m.FrameObj.AddByCoord(...)
ReadOnly    guard=0  hits=[]  <- ((cSapModel)sapModel).FrameObj.SetSection(...)
ReadOnly    guard=0  hits=[]  <- (sapModel).FrameObj.SetSection(...)
ReadOnly    guard=0  hits=[]  <- sapModel?.FrameObj.SetSection(...)
ReadOnly    guard=0  hits=[]  <- sapModel?.SetModelIsLocked(false)          // D: unlock with the checkbox OFF
ReadOnly    guard=0  hits=[]  <- int F(cSapModel s) => s.FrameObj.SetSection(...); return F(sapModel);
ReadOnly    guard=0  hits=[]  <- Func<cSapModel,int> f = s => s.PropFrame.SetRectangle(...); return f(sapModel);
```

`EtabsExecutor.cs:133-152` then runs these as tier R: attached, no snapshot, no preview, audit outcome `ok`, `-32001` never raised for the unlock. `var m = sapModel;` is not adversarial — it is the first thing a model writes when it wants a short name. The phase-1 safety claim ("W/D never run") therefore does not hold for a live model with the execution checkbox ticked.

*Fix (phase 1, ~15 lines, fail-closed):* in `Inspect`, (i) visit `ConditionalAccessExpressionSyntax`/`MemberBindingExpressionSyntax` and treat them like a member access on the same root; (ii) any occurrence of `sapModel`/`etabs` **not** as the root of a member-access chain (initializer, argument, cast, parenthesised, tuple, pattern, lambda body) → `Destructive` with a `PREVIEW`/`DESTRUCTIVE` hit "alias of the model global — call members on `sapModel` directly"; (iii) `SapModel` reached from `etabs` is a root too. Add the eight samples above as theory rows. Phase 2's semantic pass (bind every receiver to `ETABSv1.*`; unbound → D) closes it properly and replaces (ii).

*Engine note for phase 2 (McpShared, not in scope here):* `ScriptGuard.VisitMemberAccessExpression` has the same `?.` blind spot — `etabs?.ApplicationExit(false)` passes `DeniedMembers`; `sapModel?.File` is caught only because `File` lands in `VisitIdentifierName` as a bare identifier. Worth a `VisitMemberBindingExpression` override in the engine (additive, all hosts).

### High

**2. Every analysis-results read is classified W and previewed instead of run.** `cSapModel.Results` is `cAnalysisResults` (38 members: `FrameForce`, `JointDispl`, `BaseReact`, `StoryDrifts`, `ModalPeriod`, … — reflected from the installed wrapper); none starts with a read prefix, so `Classify` returns `Write` (`EtabsTierGate.cs:65-72`). Probe: `Write hits=[Results.FrameForce:Write]`, `Write hits=[Results.BaseReact:Write]`. ADR-02 §1(d) and the tool description/`ScriptContractSummary` promise `AnalysisResults*` as R (`ExecuteEtabsCodeTool.cs:94`, `EtabsHostProfile.cs:55`), so in phase 1 the main read use-case is refused while the contract says it runs. Also `ReadOnlyExact` names `SetOptionMultiValuedCombos` (real member is singular) and misses `SetOptionModeShape/SetOptionMultiStepStatic/SetOptionBaseReactLoc`.
*Fix:* a receiver rule — chain contains `Results` (i.e. `sapModel.Results.X` / `.Results.Setup.X`) ⇒ R unless `X` is in `DestructiveExact`; fix the exact names; add `sapModel.Results.FrameForce(...)` and `sapModel.Results.Setup.SetCaseSelectedForOutput(...)` as theory rows.

**3. BCL members on any receiver trip the destructive prefixes → spurious `-32001`.** `Classify` applies `DestructivePrefixes` to non-rooted receivers (`:69` via `:91-92`). Probe: `Destructive hits=[x.StartsWith:Destructive]`, `[Environment.NewLine:Destructive]`, `[l.Clear:Destructive]`, `[sb.Clear:Destructive]`, `[System.Diagnostics.Stopwatch.StartNew:Destructive]`. A read-only script using `names.Where(n => n.StartsWith("C"))` or `Environment.NewLine` is refused with "Destructive operations are disabled — tick 'Allow destructive operations'", and with the checkbox on it is previewed rather than run. The class summary (`:28-30`) claims non-rooted members are caught "only when its name is on the destructive list", but the code also applies the prefixes.
*Fix:* for non-rooted receivers use `DestructiveExact.Contains(member)` only (the alias rule from finding 1 keeps aliased OAPI receivers closed); theory rows for `StartsWith`, `Clear`, `NewLine`, `StartNew`.

### Medium

**4. Context reader publishes `(Untitled)` as a document and `isModifiable` ignores "no model".** With no model, `GetModelFilename(true)` = `(Untitled)` (E12) → `docTitle "(Untitled)"`, `openDocs ["(Untitled)"]`, `activeView "(Untitled)"` (`EtabsContextReader.cs:46,50-51`) while the tool description says docTitle/docPath are "absent while no model is open" and that context "fails with 'no model' on the ETABS start screen" (`EtabsContextTool.cs:148,153`) — neither is true today (and per E12 the second should not be). `isModifiable = quiescent` (`:49`) has no "model open" term although the description says it does (`:149`).
*Fix:* `DocTitle = hasFile ? name : null`, `OpenDocs = []` when not rooted; `IsModifiable = quiescent && hasFile`; reword the description to "answers with `docPath` absent on the start screen; only writing runs are refused there" (phase-2 rule).

**5. Stale main-window handle makes the bridge permanently "busy" after ETABS recreates its window.** `MainWindowHandle` is read once at Attach (`EtabsAttachment.cs:149`); `IsQuiescent` calls `IsWindowEnabled(handle)` (`EtabsExecutor.cs:252`), which returns FALSE for a destroyed HWND, so every request expires with `-32002` until the user Detaches/Attaches. E12 (close model) did not trigger it, but nothing guarantees ETABS keeps one HWND across New/Open.
*Fix:* `handle == IntPtr.Zero || !IsWindow(handle) || IsWindowEnabled(handle)` (one more P/Invoke), or refresh `process.MainWindowHandle` when the queue has pending items.

**6. Side effect inside an exception filter.** `catch (Exception e) when (_attachment.DetachIfGone(e) is { } refusal)` (`EtabsExecutor.cs:203`) mutates state and raises `StateChanged` during the first pass; an exception thrown by any `StateChanged` subscriber inside the filter is swallowed, the filter evaluates false, and the raw `COMException` propagates as a generic script error while the attachment is already half-dropped. It works today; the pure form is clearer and cannot mis-route: `catch (Exception e) when (EtabsAttachment.IsDisconnectError(e)) { _attachment.Detach(...); throw EtabsAttachment.NotAttached(); }`.

**7. `.$et` working-file name is published as the model (phase-2 blocker for the snapshot).** After `File.Save`, `GetModelFilename(true)` returns `…\Model.$et` (spike E9); the reader accepts it because it is rooted (`EtabsContextReader.cs:43-47`) → `docTitle "Model.$et"`, `docPath` to the sidecar. Phase 2's snapshot copies `docPath` → it would copy the `.$et`, not the `.EDB`, and `"no file path"` checks would pass on a sidecar.
*Phase 2:* normalise once in a shared helper (`Path.ChangeExtension` when the extension equals `.$et` case-insensitively, then `File.Exists` on the `.EDB`); test both spellings; use the same helper in the snapshot manager and the context reader.

**8. Description/summary claims that phase 1 does not implement.** Besides finding 2: `ScriptContractSummary`/`ToolDescription` state R = `…/Count/…` — `Count` as a prefix matches nothing on the real surface and `cCombo`/`cStory` have no `Count()` (ADR-02 context); `EtabsHostProfile.cs:55` and `ExecuteEtabsCodeTool.cs:94` also say "AnalysisResults*" which today is W. Keep the target wording only where the bridge already honours it, or add "(this bridge build: reads only; writes are previewed)" — a 40-char suffix that removes the ambiguity the probe found without changing the ≤ 1 800 cap (1 704 now).

### Low

**9. `Attach` can end "attached" with null proxies.** `Watch` calls `OnProcessExited` synchronously when `process.HasExited` (`EtabsAttachment.cs:154`) *inside* `Attach`'s lock and before `_attached = true` (`:81`); the re-entrant `Detach` nulls the proxies, then `:81` sets `_attached = true` anyway. `Require()` still refuses (`:115`), so no crash — but the window shows "Attached to ETABS pid ?". Set `_attached = true` before `Watch` (Detach then works) or return early when `HasExited`.

**10. `_destructiveEnabled` is a plain bool read on the pipe thread and written on the UI thread** (`EtabsExecutor.cs:42,74-76,128`). A stale read only delays the flag by a memory barrier's worth; mark it `volatile` like `_running` since it is the second opt-in. ADR-04 §5's `Dispatcher.CheckAccess()` assert on the setter is also absent (Info — the assembly is unreachable to scripts).

**11. Harness checks that cannot fail.** `live-verify.py:155` (E12) and `:164` (E15) pass `True` as the condition, so `nomodel 1/1` and `modal 1/1` in the report are observations, not verifications. Either assert what E12/E15 concluded (`ok(ctx) and etabs.isAttached and not ctx.get("docPath")` + execute `ok(r)` or `isError`; E15 `dt < 12` and (`ok(r)` or "busy" in message)) or record them with `skip(...)`/`save(...)` so the PASS count stays honest.

**12. `spike-step.ps1 stop` and `Use-StartedBridge` trust the pid file.** `Get-Process -Id` (`spike-step.ps1:43,101`) does not check `ProcessName -eq 'HPEtabs.McpBridge'`; after a pid reuse `Stop-Bridge` would `CloseMainWindow`/`Stop-Process -Force` a foreign process. One `if ($proc.ProcessName -ne 'HPEtabs.McpBridge') { throw }` closes it.

**13. Spike artefacts in product code.** `SpikeProbes.cs` (the only writes in the bridge, leaves `*.hpetabs-e9-probe.EDB` in the user's model folder, never re-locks after E10), the three VM commands and the XAML row (`EtabsBridgeStatusView.xaml:162-172`) carry plan codes `E9/E10/E14` in UI text and log lines. They served their purpose (E9/E10/E14 concluded); schedule removal in phase 2 (or gate behind `#if DEBUG`) so the shipped window has no "throw-away model only" buttons.

**14. Smaller items.** `EtabsTierGateTests.cs:18` uses the non-existent path `sapModel.AnalysisResultsSetup` (`Results.Setup`); bridge-test count 25 in the spike report vs 26 in the run; `.gitignore` `!**/bin/**` is a no-op negation under an excluded `*bin/` (harmless, confusing); `EtabsBridgeStatusViewModel.Refresh` never clears `AttachWarning` after a Detach (`:165`), so the "2 instances" text survives until the next Attach; `EtabsExecutor.cs` 304 lines vs the < 300 rule.

## Positives

- The two-process/three-process split is clean: server csproj references only `HPRebar.Mcp.Server.Core`; `Program.cs` is one line; every ETABS-specific string lives in `EtabsHostProfile`/the two tool classes (`HPEtabs.Mcp.Server/**`).
- Resolver-before-JIT discipline (`BridgeEntry.Start` → `Install` → non-inlined `Wire`, `BridgeEntry.cs:56-62`) and the `Assembly.Location` self-check (`ScriptingSelfCheck.cs:35-41`) turn two red-team findings into startup assertions.
- Control lane ahead of the busy queue + "not attached ⇒ quiescent" (`EtabsExecutor.cs:94-105,246-253`) is exactly ADR-04 §1 and is what made E19 answer in 0.1 s instead of hanging on a dead HWND.
- Disconnect detection on every call with the six RPC HRESULTs (`EtabsAttachment.cs:129-135`) covers the multi-instance case the `Exited` hook cannot.
- Units in/out with restore in `finally`, the timeout clock started before the prep calls, and "returned after the timeout still fails" (`EtabsScriptRunner.cs:49-61,78,100-103`) match ADR-04 §3-4 to the letter.
- Serializer never walks an OAPI proxy (`EtabsResultSerializer.cs:72,98`) — the one bug that would have made a script returning `sapModel` call into ETABS property by property is designed out.
- Preview is `isError:true` + `rolledBack:true` + `PREVIEW` diagnostics (`EtabsExecutor.Audit.cs:33-44`) so `test_tool` can never mark an unrun tool as tested (ADR-02 §1); D-off is a JSON-RPC `-32001`, never a run (`EtabsExecutor.cs:128-130`).
- Tests are behaviour-first and readable; the pipe tests drive the real `PipeListener` with the shared fake and pin the hint texts, the 600 s ceiling and the absence of Revit-named fields.
- Harness never touches ETABS, scopes UIA to its own pid, wipes its registry root, and captures the bridge log tail per run.

## Score and verdict

**6.5 / 10 — fix-first.** Architecture, wiring, runner, serializer, tests and harness are solid and match the ADRs; the one component that carries the phase-1 safety claim (`EtabsTierGate`) both under-blocks (finding 1: aliases, casts, `?.`, lambdas run W/D as R) and over-blocks (findings 2–3: results reads previewed, BCL names refused as destructive). All three are contained in one 134-line file plus theory rows and should land before anyone points the bridge at a real model or before phase 2 builds the semantic pass on top of the same walker. Findings 4–8 are phase-2 inputs (7 is a snapshot blocker); 9–14 are hygiene.

**Fix order:** 1 → 3 → 2 (same file, one PR, add the probe samples as tests) → 8 (description suffix) → 11/12 (harness honesty) → the rest with phase 2.
