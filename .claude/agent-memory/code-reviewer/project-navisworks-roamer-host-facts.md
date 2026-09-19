---
name: navisworks-roamer-host-facts
description: Environment facts about Navisworks Manage 2026 (Roamer.exe, net48) that shape reviews of HPNavis — AssemblyResolve requester always null, NextUndo is name-only, Idle stops under native modals, no loader to wrap plugin exceptions
metadata:
  type: project
---

Roamer.exe (Navisworks Manage 2026, .NET Framework 4.8) host facts observed 2026-09-15 (phase-1 spike, `plans/260915-0824-navisworks-mcp-2026/reports/phase-01-spike.md`):

- `ResolveEventArgs.RequestingAssembly` is **null for every** AssemblyResolve request in Roamer (85/85 spike lines "requested by <none>", including our own static binds). Any requester-based filter in a plugin resolver is decorative there; filter by allow-list + version major instead.
- `Document.NextUndo` returns the transaction **display name only** — two entries with the same label are indistinguishable, so "is the undo top ours?" checks must make the label unique per run.
- `Application.Idle` does **not** fire while a native modal (file dialog) runs → main-thread queues need timer-based expiry (`MainThreadQueue(expireWithoutTicks: true)`).
- Navisworks reflects over every plugin type (`GetTypes()`) **before** the plugin's static ctor runs → dependencies pulled by plugin types must bind by exact version without a resolver (why Contracts multi-targets net48).
- No loader/ALC layer exists for the Navis plugin (unlike AutoCAD): `EventWatcherPlugin.OnLoaded/OnUnloading` and `AddInPlugin.Execute` are the outermost frames — exceptions there reach Roamer directly.

**Why:** these are not derivable from the code and were each learned by a live failure (D1–D3 in the spike report).
**How to apply:** when reviewing anything under `HPNavis/`, check resolver filters, undo-label uniqueness, exception wrapping at the plugin entry points, and that queue/quiescence logic never depends on Idle ticking.
