# Phase 6 — live verify, review, docs (2026-10-24)

## Automated
| Check | Result |
|---|---|
| `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` | 0 errors (32 pre-existing warnings) |
| `dotnet test HPBimToolkit.Core.Tests` | 67/67 |
| Regression `HPRebar.Core.Tests` / `HPRebar.Mcp.Server.Tests` | 1627/1627 / 118/118 |
| Frontend `typecheck`, `lint`, `vitest`, `build` | pass, 27/27, bundle 209 KB js + 15 KB css |

## Live (second Revit 2026.5 on copies of DynamoSampleModel.rvt)
`pwsh HPBimToolkit/tools/harness/run-live-verify.ps1 -Runs 3` → **3 × 22/22 PASS**, Revit responsive after each run. Checks: page on `https://toolkit.hpbim.local/`, handshake (2026.5, theme dark), document info, model stats total = Σ categories (430 / 34 categories, 23 ms), metrics, document key, bookkeeping categories excluded, active-view stats with view id (260, 8 ms), select category = counted elements (118/118), selection read-back, coalesced `selection.changed` (1 event), zoom to current selection keeps selection, UNKNOWN_COMMAND / BAD_REQUEST ×3, UI: title, real data, top category, selection card.

## Found live and fixed
1. **Bookkeeping categories counted** (Materials 173 on top, `<Sketch>`, Sun Path, Constraints, Analytical Nodes…). Now: `IsVisibleInUI`, no subcategory, Model/Annotation, minus Materials/Project Information/Sheets/Color Fill Schema/Pipe Segments → 1364 → 430 elements, 67 → 34 categories.
2. **UI-thread deadlock (1 of 3 starts)**: dump showed the UI thread JIT-ing the first handler inside the WebView2 `WebMessageReceived` callback → Revit `AddInLoadContext.Load` → blocked in `GetCodeBase` while Revit was still opening the model; CPU flat, Revit "Not Responding". Fix: `DependencyPreloader` resolves the toolkit's references at `OnStartup`; messages handled via `Dispatcher.BeginInvoke` outside the COM callback. 3/3 runs clean after.
3. Harness: `Start-Process -Wait` waits for the whole tree (blocked until Revit exits); WMI start → "Failed to open document"; stopping a harness killed its Revit. Now `cmd /c start` (detached, inherits env) and debug-port probe before any UIA (UIA over an open pane hangs on Chromium's tree).

## Review round (code-review-hp-bim-toolkit.md) — all High/Medium + L1–L9 fixed
H1+M1 single timeout source (timed-out request cancelled before Revit runs it — test), statistics join/BUSY + abandoned-run replacement, page `statisticsSettled`; M2 120 s statistics budget (page 130 s) + cancellation checks in the walk; M3 `documentKey`/`viewId` in statistics, page drops foreign answers, active-view refresh on view change, reducer clears on document switch; M4 guarded `PostJson`, renderer reload / browser-exit message; M5 `currentSelection` zoom (never replaces the selection); M6 replay selection + stale on pane show; M7 npm reinstall on lock change; L1 teardown on start-up failure; L2 host `ArgumentException` → INTERNAL; L3 cached theme; L4 skip document publish while hidden; L5 navigation/new-window guard; L6 debug profile folder; L7 Vietnamese error texts; L8 `WithCulture=false`; L9 handshake retry. Open: L10 (no cap on "select category" size), L12 packaging into the MSI/bundle.

## Event and failure paths (second round, same day)
`live-verify-events.mjs` on `prepared-site-plan.rvt` (DynamoSampleModel copy saved in the Site plan) → **13/13 PASS**:
- `openTool revit.projectUnits` opens the modal Project Units dialog; a request meanwhile answers host TIMEOUT after 30 004 ms; Cancel → requests work again (timed-out request was skipped).
- `openTool revit.default3DView` (new quick tool) → `document.changed` Site → {3D}, header updates.
- Zoom to elements the active view cannot show: answered in 43 ms, `zoomed: 0`, no dialog (see fix below).
- Delete 11 Generic Models with the Delete key → `model.stale {deleted: 88}` (hosted elements too), badge shown; Làm mới clears it, total 430 → 397; Ctrl+Z → `model.stale {added: 88}`, badge back.
Pane hide/show (UIA on the pane's hide button, `revit-uia.ps1` skips the Chromium subtree): selecting 38 walls while hidden pushes nothing; Dashboard button → replay `selection.changed {38}` + `document.changed`.

### Found live and fixed
4. **`UIDocument.ShowElements` blocked Revit**: with elements no open view shows, Revit opens the modal TaskDialog "There is no open view that shows any of the highlighted elements… Continue?" (journal) — the API thread waits for the user, every toolkit request TIMEOUTs (3 in a row). Now `ElementNavigator` zooms only to the elements the active view shows (`FilteredElementCollector(doc, view).WherePasses(ElementIdSetFilter)`), answer carries `zoomed`; the page says when nothing could be zoomed.
5. **Large selections freeze Revit**: selecting "Lines" (19 617 elements) took 18.2 s in `SetElementIds` on the large model. The page now asks (`window.confirm`) above 5 000 elements (test added).

## Large model (copy of THCPHCS2 …_detached.rvt, 885 MB, 72 788 counted elements, 83 categories)
| Measure | Result |
|---|---|
| Open (second Revit, incl. "Copied Central Model" notice closed by harness) | 700 s |
| getStatistics model — first / warm | host 2 589 ms (round trip 6 733 ms) / host 674 ms |
| getStatistics active view (a sheet, 3 elements) | ~240 ms |
| Select 19 617 elements | 18 186 ms (now behind a confirmation) |
| Requests while Revit showed "could not find or read 41 references" | host TIMEOUT 30 s / 120 s, then normal — expected |

## CHƯA TEST (live)
- `theme.changed`: changing Revit's UI theme writes the per-user Revit settings shared with the user's own Revit — not done on purpose. Start-up theme read verified (`hello.theme = dark`).
- Dynamo opened in the same session (ribbon UIA did not find tabs in the large-model Revit; Dynamo ships the same WebView2 1.0.2478.35, so no version conflict is expected — GIẢ ĐỊNH CHƯA XÁC MINH).
- Document close → empty state (only unit-tested on the page side).
- Visual check inside Revit (screenshots only in the browser pane with mock data, 360 px / 760 px, light + dark).

## Side effects on the dev machine
- The user's running Revit (pid 22824) loaded the phase-0 spike `HPBimToolkit.dll` (unknown trigger; it was not running when the add-in was first deployed) and locks it; deploys renamed the locked file to `HPBimToolkit.dll.locked-*` in `%AppData%\Autodesk\Revit\Addins\2026\HPBimToolkit\` — delete those after closing Revit.
- Add-ins shipping Serilog loose share one `Log.Logger` (see phase-00 report); HPRebar is not affected (ILRepack-merged).
