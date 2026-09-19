---
name: roslyn-script-refuses-precancelled-token
description: Roslyn Script.RunAsync throws OperationCanceledException before running when the token is already cancelled (verified 2026-09-14, Roslyn 5.9.0) — both MCP bridges' "cancel arrived before the run started" path relies on this
metadata:
  type: project
---

`Script<T>.RunAsync(globals, ct)` with an already-cancelled `ct` throws `OperationCanceledException` before executing the submission (globals untouched). Verified 2026-09-14 with a scratch console app on Microsoft.CodeAnalysis.CSharp.Scripting 5.9.0 (the version McpShared pins).

**Why:** neither `ScriptRunner` (Revit) nor `AutocadScriptRunner` checks `cancelSource` before calling `RunAsync`; the AutoCAD `MainThreadQueue` can hold a request for the whole busy grace, so "server cancelled while the item waited" is a real path there. Roslyn's pre-check is what turns it into "Script was cancelled. Nothing was committed." instead of a late commit.

**How to apply:** when reviewing cancel/timeout handling in either bridge, do not flag "cancelled token runs the script" as a data hazard — it does not. Do flag that the guarantee lives in a Roslyn internal, so an explicit `IsCancellationRequested` check before the lock/transactions is still the right hardening (also avoids opening the document lock and two transactions for nothing).
