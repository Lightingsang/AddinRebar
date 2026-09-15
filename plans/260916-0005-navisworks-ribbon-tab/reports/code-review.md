# Code review — Navisworks Ribbon tab "HPNavis" ▸ "MCP" ▸ "MCP Bridge"

**Date:** 2026-09-16 · **Mode:** static, read-only (no build/test/harness run; live numbers taken from `reports/ribbon-live-check.md`). Probes run: `dotnet msbuild -getItem:Page,None` on the plugin csproj, reflection over `Autodesk.Navisworks.Api.dll` (attribute defaults), a PowerShell `continue`-in-`switch` experiment, AGENTS.md regeneration diff.

## Scope

New: `HPNavis/HPNavis.McpBridge/HPNavisRibbonPlugin.cs`, `Ribbon/en-US/HPNavisRibbon.{xaml,name}`, `Ribbon/Images/McpBridge_{16,32}.png`, `HPNavis/tools/icons/render-ribbon-icons.ps1`, `HPNavis/HPNavis.McpBridge.Tests/RibbonPluginTests.cs`, `HPNavis/tools/harness/run-ribbon-check.ps1`, plan folder.
Modified: `HPNavis.McpBridge.csproj`, `HPNavisWindowPlugin.cs`, `HPNavis.McpBridge.Tests.csproj`, `tools/harness/harness-common.ps1` (+123), CLAUDE.md / AGENTS.md / HPNavis README / harness README / codebase-summary / changelog.
Parity: `git diff --stat McpShared/ HPRebar/ HPAutoCad/` empty (confirmed from `git status`).

## Checks run (static)

| Check | Result |
|---|---|
| Ids consistent: `[RibbonTab("ID_HPNAVIS")]` ⇔ XAML `RibbonTab Id` ⇔ `.name` key; `[Command("ID_HPNAVIS_MCP_BRIDGE")]` ⇔ `NWRibbonButton Id` ⇔ `.name` keys | OK (and pinned by 11 tests) |
| XAML root + 6 namespaces byte-equal to the working foreign layout (`NavisworksMCPPlugin\en-US\NavisworksMCPRibbon.xaml`) | OK |
| `Image="..\Images/McpBridge_16.png"` resolves from `en-US\` to `Images\`; same mixed-slash form the foreign plugin uses live | OK |
| `[Command].Icon/LargeIcon` bare names = the PNG file names; sizes 16/32 match API doc ("16x16", "32x32") | OK |
| Three `[Plugin]` names distinct, one developer id | OK |
| `AddInLocation.None` = "Do not display in the menus" only; `ExecuteAddInPlugin` needs "is an AddInPlugin" | OK (live-unverified, already a known gap) |
| csproj: `-getItem` shows Page = only our 2 WPF views; None = 4 Ribbon files with correct `Link` + `PreserveNewest`, no duplicates (NETSDK1022 avoided by `<None Remove>`) | OK |
| Files present in `bin/Debug/net48/{en-US,Images}`, test output (transitive copy) and the deployed plugin folder | OK |
| Deploy target `$(OutDir)**\*` recursive → copies both folders; Release: items unconditional, deploy Debug-only (unchanged) | OK |
| Test csproj `Reference Autodesk.Navisworks.Api Private=false`: compile-time only; run time via `NavisworksApiProbe` (ModuleInitializer, env → registry → default); `RequireNavisworksApi` target still the single readable error | OK |
| PNG parsing: signature 0–7, "IHDR" 12–15, width 16–19, height 20–23, depth 24, colour type 25 — correct | OK |
| `OrderBy` + `Assert.Equal` to a sorted literal — deterministic, not brittle | OK |
| Harness kills only its Roamer (`Assert-NoNavisworksRunning` + `Stop-Navisworks $proc`); `return` inside try → `finally` runs; `$script:results` scoping correct for `-File`; exit 1/2/0 as specified | OK |
| Encodings: new `.ps1` ASCII without BOM; `harness-common.ps1` keeps its BOM (has `—` in comments) | OK |
| AGENTS.md == `_TO_PORTABLE` regeneration of CLAUDE.md | OK (byte-equal) |
| Docs vs code/report: 135 tests, 14 PASS/1 MANUAL, colours #3C3C3C/#0696D7, paths, "greys every tab" (screenshot `ribbon-nodoc.png` shows the whole ribbon greyed) | OK except finding 6 |

## Findings

No Critical, no High.

| # | Sev | File:line | Problem | Fix |
|---|---|---|---|---|
| 1 | Medium | `harness-common.ps1:289-296` (`Select-RibbonTab`) | `continue` inside `switch` inside `foreach` leaves only the **switch** (verified: PS 5.1 and pwsh both fall through to the line after the switch). When a header lacks a pattern, the code proceeds as if it had acted: waits up to 4 s in `Wait-RibbonButtonVisible`, logs the misleading "via X ran but the tab did not display"; with `$verifyButton` empty it returns `$true` having done nothing. Latent today (Invoke worked live). | Resolve the pattern outside the switch: `$id = switch ($name) { 'Invoke' {[...InvokePattern]::Pattern} ... }; if (-not $t.TryGetCurrentPattern($id, [ref]$pattern)) { continue }` (now at foreach level), then a second switch calls `Invoke()/DoDefaultAction()/Select()`. |
| 2 | Medium | `run-ribbon-check.ps1:102-104` | Check 12 "no 'HPNavis MCP' Add-ins entry" is tautological: the report's own fact (line 33) says a tab's buttons enter the UIA tree only while that tab is displayed, and the HPNavis tab is the displayed one — the check passes with `AddInLocation.AddIn` too. The unit test `Window_add_in_is_hidden_from_the_add_ins_menu` is the real proof. | Assert the absence of the tab Navisworks synthesises for `AddInLocation.AddIn` plugins (`Find-RibbonTabHeaders '' 'Tool add-ins 1'` — confirm the title once from a build with `.AddIn`), or drop the check and reword the report row. |
| 3 | Medium | `HPNavisRibbonPlugin.cs:40-44` | When the bridge failed to start, `BridgeEntry.ShowWindow()` throws `InvalidOperationException("The bridge did not start; see <log>")`; the Ribbon catches and logs → the primary entry point becomes a silent no-op click. AutoCAD disables its buttons with a tooltip pointing at the log. (Pre-existing in `HPNavisWindowPlugin`, but the Ribbon is now the visible path.) | In the catch, on the main thread: `System.Windows.MessageBox.Show(exception.Message, "HPNavis MCP")` (one line), or `CanExecuteCommand` → `new CommandState(BridgeEntry.IsStarted)` with a small public flag (`_host is not null`). |
| 4 | Low | `HPNavisRibbonPlugin.cs:21` | Comment "the default would grey the button out on a clear document" is wrong: `CommandAttribute.CallCanExecute` default is `Always` (= 0; reflection on the 2026 DLL, `RibbonTabAttribute` too). The explicit value is fine to keep as documentation of intent. | Reword: "explicit — the button must stay enabled on a clear document (also the API default)". |
| 5 | Low | `RibbonPluginTests.cs:135` (and `:122/:130`) | ASCII check via `File.ReadAllText` — a UTF-8 BOM is stripped before the check, and `ReadAllLines` hides it from the `$utf8`-first-line check too, so the one non-ASCII byte sequence the `.name` parser is most likely to trip on is invisible to the test. | `Assert.All(File.ReadAllBytes(StringsPath), b => Assert.True(b < 128, ...))`. |
| 6 | Low | `phase-01-ribbon-tab-mcp-bridge-button.md:45` | "`McpBridge_16.png` 188 B, `_32.png` 241 B" — files are 203 B / 240 B. | Update or drop the byte counts. |
| 7 | Low | `harness-common.ps1:297` | `if (-not $verifyButton) { ...; return $true }` — dead branch (the only caller passes `'^MCP Bridge$'`) and the branch that turns finding 1 into a false positive. | Make `$verifyButton` mandatory and delete the branch. |
| 8 | Low | `HPNavisRibbon.xaml:16-17` | `x:Uid="RibbonPanel_MCP"` / `"RibbonPanelSource_MCP"` are identical to the foreign plugin's panel Uids on this machine. Both tabs rendered live side by side, so no collision manifested; the XAML comment's claim "only the root x:Uid must be unique" is unverified. | Rename to `RibbonPanel_HPNavisMcp` / `RibbonPanelSource_HPNavisMcp`; costs nothing. |
| 9 | Low | `RibbonPluginTests.cs:131-134` vs `.name:14-18` | `.name` ToolTip/ExtendedToolTip duplicate the `[Command]` text by hand; the tests pin DisplayName equality only, so those two can drift silently. | Add `Assert.Equal(command.ToolTip, keyed[... + ".ToolTip"])` and the ExtendedToolTip twin. |
| 10 | Low | `harness-common.ps1:185-189`, callers `run-ribbon-check.ps1:72,80` | `SetForegroundWindow` may be refused (foreground lock); the `$false` is discarded, so a covered screenshot (run 2's failure mode) has no log line explaining it. | `if (-not (Set-RoamerMainWindowForeground)) { Write-Host 'foreground refused' }` before `Save-RibbonScreenshot`. |
| 11 | Low | `render-ribbon-icons.ps1:13` | Relaunch passes `-OutDir ''`; pwsh < 7.3 drops empty native args → "Missing an argument for parameter 'OutDir'". Harness already requires pwsh ≥ 7.3. | Build the arg list conditionally like `run-ribbon-check.ps1:18-20`. |

## Verdicts

- **Plugin declaration (a):** correct and pinned; the only defect is the comment (4). Lazy `CommandHandlerPlugin` instantiation on first click, `ExecuteCommand` on the main thread — `ShowWindow` legal there.
- **csproj (b):** correct; `<Page Remove>` is required (UseWPF globs `**\*.xaml`), `<None Remove>` is required (duplicate None → NETSDK1022), `Link` puts the files where Roamer reads. Release unaffected. Test project gets the files transitively (test 11 pins it).
- **Tests (c):** no tautologies; the three-way pin (attributes ⇔ XAML ⇔ `.name` ⇔ PNG) is exactly the check Navisworks does not give you. Findings 5 and 9 are gaps, not errors.
- **Harness (d):** safe (own Roamer only, finally closes it, never saves). Findings 1 and 2 weaken it; neither changes today's result (Invoke worked, unit test covers `None`).
- **Docs (e):** accurate, AGENTS.md is a true regeneration; one stale byte count (6).
- **YAGNI/DRY (f):** every new helper in `harness-common.ps1` is called by `run-ribbon-check.ps1`; glyph defined once (the accent hex is repeated from Theme.xaml by design — the icon must not depend on the WPF theme). Dead branch (7). The `.name`/attribute/XAML triplication is Navisworks' scheme, mitigated by the tests. `HPNavisWindowPlugin` kept for `ExecuteAddInPlugin` is cheap and justified.

## Plan follow-ups (report only)

All 10 TODO boxes in `phase-01` are ticked and match the evidence. Suggested next: apply findings 1–3 (harness + one-line user feedback), then 4–9 in one tidy commit; no re-verification in Roamer needed except for finding 2 if the tab-title check is adopted.

**Score: 8 / 10** — the plugin, layout, build plumbing and tests are right and verified live; the deductions are for a harness check that cannot fail (2), a latent control-flow bug in the tab selector (1) and a silent failure path on the primary button (3). Nothing blocks the commit.

**What is good:** the three-way id pin in tests, the `-getItem`-clean csproj item handling, pixel-exact even-coordinate glyph rendering, harness scoped to its own Roamer with meaningful exit codes, and docs regenerated rather than hand-edited.

**Status:** DONE_WITH_CONCERNS
**Summary:** Ribbon button change is correct, consistent across attributes/XAML/.name/PNG, builds and deploys as intended, and is verified live; no Critical/High issues.
**Majors:** none. Mediums: harness `continue`-in-`switch` fall-through (1), tautological Add-ins check (2), silent click when the bridge failed to start (3).

## Outcome (applied 2026-09-16, same session)

| Finding | Action |
|---|---|
| M1 `continue` inside `switch` in `Select-RibbonTab` | **Fixed** — patterns resolved in a candidate list before acting, `continue` at the `foreach` level; the `LegacyIAccessiblePattern` candidate was dropped (the managed UIA client has no such type — surfaced as `Unable to find type` on the first re-run) |
| M2 tautological "no Add-ins entry" check | **Fixed** — asserts no "Tool add-ins"/"Add-ins" tab header exists (ours was its only member); if one exists it is selected and our button must be absent. Live: PASS "(no Add-ins tab at all)" |
| M3 silent click when the bridge never started | **Fixed** — `MessageBox` with the exception message beside the log line |
| L comment on `CallCanExecute` default | **Fixed** — wording no longer claims a different default |
| L ASCII test blind to a BOM | **Fixed** — bytes checked |
| L phase file byte sizes | **Fixed** — 203 / 240 B |
| L dead `$verifyButton` branch | kept — `Select-RibbonTab` is also used for the Add-ins tab with no verify button (M2) |
| L duplicate `x:Uid` with the foreign plugin | **Fixed** — `HPNavis_` prefixed |
| L `.name` ToolTip/ExtendedToolTip not pinned | **Fixed** — test compares them to the attribute |
| L `SetForegroundWindow` refusal not logged | kept — the screenshot MANUAL note carries the selection/visibility flags |
| L icon script relaunch with empty `-OutDir` | **Fixed** |

Re-verified: build 0 warnings, tests 135/135, `run-ribbon-check.ps1 -WithNoDoc` → **14 PASS, 0 FAIL, 1 MANUAL** (icon).
