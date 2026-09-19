---
name: net48-pipe-currentuseronly-and-polyfill-facts
description: What PipeOptions.CurrentUserOnly really does on .NET (server SetOwner+FullControl on WindowsIdentity.Owner, client compares owner SID), how Polyfill's ReadLineAsync(ct) behaves on net48, and which McpBridge.Core net48 branches no test executes
metadata:
  type: project
---

Facts verified 2026-09-15 while reviewing phase 0 of `plans/260915-0824-navisworks-mcp-2026/` (McpBridge.Core multi-target `net8.0;net48`).

- **.NET `PipeOptions.CurrentUserOnly` (Windows):** server side = `PipeSecurity` with `SetOwner(WindowsIdentity.GetCurrent().Owner)` + one `PipeAccessRule(Owner, FullControl, Allow)`; client side (`NamedPipeClientStream.ValidateRemotePipeUser`) = `GetAccessControl().GetOwner(...)` must equal `WindowsIdentity.GetCurrent().Owner`. `Owner` (not `User`) — elevated token → BUILTIN\Administrators SID, so elevated host ↔ non-elevated server is refused on every host. The net48 branch in `PipeListener.cs` reproduces exactly this; a regression to `.User` only shows when the host runs elevated.
- **Polyfill 11 `TextReader.ReadLineAsync(CancellationToken)` on net48** = `ReadLineAsync().WaitAsync(ct)`: the await is released on cancel but the underlying pipe read stays pending until the stream is disposed → an unobserved faulted task per stop; harmless unless the host's app.config sets `ThrowUnobservedTaskExceptions`.
- **`MainThreadQueueTests` all inject `clockMs`** → the default clock branches (`Environment.TickCount64` / net48 `Stopwatch.GetTimestamp()*1000/Frequency`) are never executed by tests. The net48 form overflows `long` when QPC frequency is in the GHz range (some VMs); 10 MHz (normal Win10/11) is safe for ~29 years.
- The net10 suite's second-listener test (`HostNeutralityTests`) waits for `\\.\pipe\<name>` to exist before starting the second listener — any copy of that test must do the same or it races.

**Why:** phase 3 of the Navis plan is the first real net10 server ↔ net48 bridge connection; these are the facts to check against when it fails.
**How to apply:** when reviewing net48 pipe/ACL code or new `#if NET48` branches in `McpShared/HPRebar.McpBridge.Core`, check the owner SID choice, cancellation semantics of Polyfill shims, and whether a test actually executes the branch.
