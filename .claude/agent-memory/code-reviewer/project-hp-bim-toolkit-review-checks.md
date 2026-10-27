---
name: hp-bim-toolkit-review-checks
description: HP BIM Toolkit (Revit 2026 WebView2 pane) review facts — threading of the dispatcher, unreachable CANCELLED, write-pipeline guidance traps, harness prerequisites
metadata:
  type: project
---

Facts verified 2026-10-24 while reviewing the Domain/Application extraction + skill `revit-webview-addin`:

- Page messages reach `CommandDispatcher.DispatchAsync` on Revit's UI thread (`WebViewHost` BeginInvoke → `WebMessageTransport.OnMessageReceived`); a handler's synchronous throw happens inside the dispatcher's try, so `BridgeException` thrown before `queue.RunAsync` still becomes the right code. `SingleFlightRun` relies on this single thread (no lock).
- The transport calls `DispatchAsync(request)` with no caller token and the page has no cancel message, so `CANCELLED` is unreachable in production — any doc/guidance promising "page cancels" or "Huỷ = RollBack" needs a cancel command first.
- Skill guidance once proposed one `TransactionGroup` spanning several `RunAsync` rounds — contradicts R2 and CLAUDE.md (groups open and close inside one `Execute`). Check any write-pipeline text for this.
- `live-verify-events.mjs` assumes the model is saved in a plan view (view-switch check); `run-live-verify.ps1` force-kills its Revit.
- `dotnet test HPRebar/HPBimToolkit.Core.Tests` from the repo root runs 0 tests silently (see [[dotnet-test-zero-tests-from-repo-root]]).
- `DependencyPreloader` (2026-10-24) JIT-warms every non-generic method of toolkit-folder assemblies at OnStartup against a UI-thread deadlock in Revit's `AddInLoadContext.Load` walk. Check: OnStartup rethrows, so any exception type the warm-up does not catch drops the add-in; generic methods/types and `Nice3point.*` (HostPrefixes) are never warmed; the compiler drops unused references, so `HPRebar.Core` is walked only once code uses it (the 816 ms baseline did not include it).
- The toolkit's private `HPRebar.Core.dll` stays isolated only while `HPRebar.dll` ILRepacks Core (`IsRepackable=true`). Revit shares loose DLLs by simple name across add-in contexts (Serilog precedent).

**How to apply:** use these as first checks on any HPBimToolkit diff or doc about it.
