# Code review — AutoCAD MCP bridge Ribbon reduced to the Revit-style surface (uncommitted, 2026-09-16)

**Reviewer:** code-reviewer · **Mode:** read-only (no edits, no AutoCAD/Revit/Navisworks launched, no `*.ps1` run). Build/harness numbers are the controller's: `dotnet build HPAutoCad.slnx -c Debug` 0 warnings; `run-ribbon-check.ps1` 10/10 + 1 MANUAL; bridge 21/21; smoke 22/22. I cross-checked the live run against `%LocalAppData%\HPAutoCad\McpBridge\logs\loader.log` (read-only).

## Scope

| File | Change |
|---|---|
| `HPAutoCad/HPAutoCad.McpBridge.Loader/Ribbon/McpRibbonTab.cs` (144 l) | rewritten: tab `HPAUTOCAD_MCP_TAB` "HPAutoCad" ▸ panel `HPAUTOCAD_MCP_PANEL` "MCP" ▸ button `HPAUTOCAD_MCP_BRIDGE` "MCP Bridge" → `BridgeActions.Run("show")`; event wiring kept |
| `…/Ribbon/RibbonIcons.cs` (45 l) | rewritten: one frozen `DrawingImage`, window + plug |
| `…/Ribbon/RibbonStatusPresenter.cs` | deleted |
| `…/BridgeActions.cs` (42 l) | `Query<T>`, `OpenPath`, `System.IO`/`System.Diagnostics` usings removed |
| `…/HPAutoCad.McpBridge.Loader.csproj`, `Bundle/PackageContents.xml` | 0.3.0; README item + `_Guide` copy removed |
| `HPAutoCad/HPAutoCad.McpBridge/BridgeEntry.Ribbon.cs` | deleted (`status.subscribe`, `copyLastScript`, `autoStart.get/set`, `path`) |
| `HPAutoCad/HPAutoCad.McpBridge/BridgeEntry.cs`, `.csproj` | `AddRibbonEntryPoints` call removed; 0.3.0 |
| `HPAutoCad/tools/harness/run-ribbon-check.ps1` (120 l) | rewritten for the one-button surface, adds a COLORTHEME round trip |
| Docs (`CLAUDE.md`, `AGENTS.md`, `HPAutoCad/README.md`, `docs/*`, journal) | updated in the same working tree; `AGENTS.md` regenerated (line 143 identical to `CLAUDE.md`) |

Net: +112 / −379 lines under `HPAutoCad/`. `git diff HEAD --stat -- McpShared HPRebar HPNavis` → **empty**; no untracked files there.

## Acceptance criteria

| # | Criterion | Verdict | Evidence |
|---|---|---|---|
| a | Exactly tab "HPAutoCad" ▸ panel "MCP" ▸ button "MCP Bridge"; click needs no document, never edits one | ✅ | `McpRibbonTab.Build():103-136` adds one `RibbonPanel` with one `RibbonButton`; handler → `BridgeActions.Run("show")` → `BridgeEntry.ShowWindow():119-144` (`ShowModelessWindow`, no `Document`, no transaction); `Run` reports only when a document exists and the delegate returned text (`ShowWindow` returns `""`) |
| b | Never duplicated; re-created after WSCURRENT and COLORTHEME; `Uninstall` unsubscribes everything | ✅ | Three add paths (`Install`, `ItemInitialized`, `Idle`) all end in `EnsureCreated:55-76` behind `FindTab` + `_building` re-entrancy guard (AdWindows raises `ItemInitialized` for our own items during `Tabs.Add`). `OnSystemVariableChanged:80-89` sets `_rebuild` *before* the `_idlePending` early-return, so a COLORTHEME change that lands while a WSCURRENT idle tick is pending is still honoured. `OnIdle:91-101` unhooks first, removes on `_rebuild`, then `EnsureCreated`. `Uninstall:39-52` removes all three handlers, clears both flags, removes the tab. Live log 11:35:22–11:36:38 shows exactly 5 `created` lines = 1 start + 2 workspace + 2 theme |
| c | `show/start/stop/status/dispose` + `HPMCP*` untouched; McpShared/HPRebar/HPNavis untouched | ✅ | `BridgeEntry.cs:87-94` dictionary unchanged except the removed call; `BridgeLoaderCommands.cs` not in the diff; sibling trees: empty diff stat |
| d | No dead code | 🟡 | Old keys/helpers: grep across `HPAutoCad/` (cs/ps1/py/csproj/md/xml) → none. Leftovers: vestigial `partial` on `BridgeEntry` (L5) and three plural "buttons" comments (L4) |
| e | Icon: frozen, 32×32 backing rect, even coordinates, no per-click allocation | ✅ | `RibbonIcons.cs:31-38` `Frozen(new DrawingImage(group))` — `Freeze()` is deep, so the `DrawingGroup`, both `GeometryDrawing`s and the ink brush freeze with it; `Accent` static frozen once. Transparent `RectangleGeometry(0,0,32,32)` keeps bounds. All 40 coordinates even (checked by hand). Icon built once per `Build()`, click path allocates nothing. Path strings are byte-identical to `HPNavis/tools/icons/render-ribbon-icons.ps1:32-34`. Even-odd fill: frame subpaths nest (hole), plug subpaths only share edges (no accidental holes) |
| f | Harness | 🟡 | see M1–M3, L1–L3 below |
| g | 0.3.0 everywhere | ✅ | loader csproj:12, bridge csproj:11, `PackageContents.xml:16` AppVersion + `:23` ComponentEntry |
| h | Repo C# rules | ✅ | file-scoped namespaces match folders; `<Nullable>enable</Nullable>` in both csproj; `RibbonIcons`, `RibbonCommandHandler` sealed, the rest static; longest file 182 l (`BridgeEntry.cs`, pre-existing) |

## Findings (ranked)

No Critical or High findings.

### Medium

| # | File:line | Problem | Fix |
|---|---|---|---|
| M1 | `run-ribbon-check.ps1:76-89` | **The COLORTHEME check cannot fail when the switch silently fails.** `Com` returns its error text instead of throwing after 4 attempts, and the result of both `SetVariable` calls is discarded (`$null = Com …`). If the set is refused, `$afterTheme.Count -eq 1 -and $afterThemeBack.Count -eq 1` still holds because nothing was rebuilt, so "exactly one tab after switching COLORTHEME and back" PASSes without exercising the rebuild path the comment says it proves. (The workspace check at :70 at least asserts `$now -eq $original`.) | Read the variable back after each set and fold it into the check: `$nowTheme = (Com "`$a.ActiveDocument.GetVariable('COLORTHEME')").Trim()` → assert `$nowTheme -eq "$otherTheme"` after the flip and `-eq $theme` after the restore. Cheaper and stronger: count `ribbon tab … created` lines in `loader.log` since `$loaderLines` and assert **5** (1 start + 2 workspace + 2 theme) — the live run already produced exactly that (11:35:22, 11:36:02, 11:36:11, 11:36:26, 11:36:38) |
| M2 | `run-ribbon-check.ps1:79-85, 109-112` | **User's AutoCAD profile can be left on the other theme.** COLORTHEME (like WSCURRENT) is a profile-persisted sysvar. Any throw between :79 and :85 (`Find-RibbonTabs`, `Select-RibbonTab`, `Save-RibbonScreenshot` all can) lands in `catch` → `finally` force-kills acad → the restore at :85 never runs and the user's next AutoCAD opens in the flipped theme. The workspace switch has the same pre-existing gap. | Track `$themeFlipped = $true` after :79 (and `$wsSwitched` after :62); in `finally`, before `Stop-Process`, restore whatever is flagged (`try { Com … } catch { }`), then kill |
| M3 | `run-ribbon-check.ps1:95, 98, 101` | `Count-BridgeWindows` is evaluated **twice per Check** — once for the verdict, once inside the detail string. Each call walks every UIA descendant of the AutoCAD frame (slow), and the two reads can disagree (window appearing between them) so the detail can contradict the PASS/FAIL it explains. | `$n = Count-BridgeWindows; Check '…' ($n -eq 0) "windows=$n"` — same for the other two |

### Low

| # | File:line | Problem | Fix |
|---|---|---|---|
| L1 | `run-ribbon-check.ps1:93-94` | Check name overclaims: 'panel "MCP" holds **exactly** the button "MCP Bridge"' only proves *a* Button named "MCP Bridge" exists somewhere in the frame — not the panel title, not that nothing else is on the tab, not that it is unique. | Either rename to 'button "MCP Bridge" found on the Ribbon', or count matches (`@($frame.FindAll(Descendants, Button) | ? Name -match '^MCP Bridge$').Count -eq 1`) which also catches a double-added button |
| L2 | `run-ribbon-check.ps1:99-101` | 'second click **activates** the same window' is not verified — only "still 1 window" is. | Rename to 'second click opens no second window', or additionally assert the bridge window's `Current.HasKeyboardFocus`/`IsTopmost`-style state |
| L3 | `run-ribbon-check.ps1:79, 85` | `[int16]` is **correct** but uncommented; the obvious "cleanup" to `[int]` breaks it. AutoCAD's COM `AcadDocument.SetVariable` requires the VARIANT to match the sysvar's storage type: integer sysvars are 16-bit (`VT_I2`, VBA `Integer`), and a PowerShell `[int]` marshals as `VT_I4` → "Invalid argument type". Same reason the managed `Application.SetSystemVariable("COLORTHEME", …)` takes a `short`. `GetVariable` returns the `Int16` → `"0"`/`"1"` after `Trim()`, so `[int16]$theme` on the restore is right too. | One comment line: `# COM SetVariable needs VT_I2 for integer sysvars — [int] (VT_I4) is rejected` |
| L4 | `BridgeLoaderApplication.cs:65` ("its **buttons** then say why"), `BridgeLoaderCommands.cs:8` ("the same path the Ribbon **buttons** take"), `RibbonCommandHandler.cs:8` ("**Buttons** that need the bridge are created disabled") | Comments still describe the ten-button tab. The first is also slightly wrong now: the button is disabled and the *tooltip* names `loader.log`. | "its button is then disabled and its tooltip says why" / "the path the Ribbon button takes" / "The button that needs the bridge is created disabled" |
| L5 | `BridgeEntry.cs:30` | `public static partial class BridgeEntry` — `partial` was there for `BridgeEntry.Ribbon.cs`; with that file gone there is exactly one part. Harmless, but it invites the reader to look for a second file. | Drop `partial` |
| L6 | `McpRibbonTab.cs:98` (pre-existing) | `Tabs.Remove(tab)` in `OnIdle` runs outside the try/catch that protects `EnsureCreated`; a throw there would escape into AutoCAD's `Idle` dispatch. Very unlikely for `RibbonTabCollection.Remove`. | Wrap the `_rebuild` block in the same "Ribbon is a convenience" try/catch, or move it into `EnsureCreated(bool rebuild)` |
| L7 | `run-ribbon-check.ps1:62, 65, 72` (pre-existing) | `$switched`, `$back`, `$selected` are assigned and never read. | `$null = …` like the neighbouring lines |

### Info

- `McpRibbonTab.OnIdle` rebuild on COLORTHEME drops the active-tab selection if the user was on "HPAutoCad" (the tab is removed and re-added). Cosmetic; `ribbon.ActiveTab = tab` after re-adding would restore it if it was active.
- `run-ribbon-check.ps1:50-51` — when exactly one new `loader.log` line exists, `-match` returns a scalar `$bool`, so the detail prints "1 line(s)" even on FAIL. Cosmetic.
- Exit code is always 2 (the icon is always MANUAL) — by design, and documented.
- Line-ending warnings in `git diff` ("LF will be replaced by CRLF") are the repo's autocrlf setting, not a change in these files.

## Positive observations

- The surface shrank by 279 lines and five cross-ALC entry points without touching the pipe, the server, the seeds or McpShared; the bridge dictionary is back to the five entries the commands need, all BCL-typed.
- Every previous-review finding on the Ribbon that still applies is addressed: theme-aware ink picked at build time, even-odd holes instead of white cutouts, `ItemInitialized` kept subscribed, `_idlePending`/`_rebuild` cleared in `Uninstall`.
- The glyph is the same path text the Navisworks PNGs are rendered from, so the three bridges now share one icon by construction, and the even-coordinate rule makes the 16-px derivative exact at 100 % and 200 % DPI.
- The harness got stricter where it matters: the log check now requires `(bridge available)`, the tab title is asserted, the window count is checked before, after and after a second click, and the icon stays MANUAL with a screenshot per theme instead of a fake PASS.
- Docs (`CLAUDE.md`, regenerated `AGENTS.md`, `HPAutoCad/README.md`, harness README, changelog, codebase summary, system architecture, journal) are in step with the code in the same working tree.

## Score and verdict

**Score: 8.5 / 10.** Loader and bridge code: clean, correct, minimal (9/10). Harness: honest but two of its new checks are weaker than their names and the theme flip has no read-back or restore-on-abort (7.5/10).

**Verdict: APPROVE — mergeable as is.** Nothing blocks the commit. M1–M3 and L1–L5 are worth a follow-up commit to the harness and comments (≈ 20 lines); none changes runtime behaviour.
