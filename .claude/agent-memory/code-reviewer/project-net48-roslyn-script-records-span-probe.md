---
name: net48-roslyn-script-records-span-probe
description: What C# the Navis bridge's Roslyn 5.9 scripts can and cannot use on .NET Framework 4.8 with BridgeEntry.CompilerReferences (records, Span, Random.Shared) — probed 2026-09-15
metadata:
  type: project
---

Probed on net48 with the exact `HPNavis` `BridgeEntry.CompilerReferences` set (mscorlib, System.Core, System, HPRebar.McpBridge.Core net48, System.Text.Json 10.0.12), Roslyn 5.9.0, imports incl. `HPRebar.McpBridge.Core.Scripting`:

- positional `record P(int X)` → **CS0518 IsExternalInit** (Polyfill's copy inside Core is `internal`; Roslyn's well-known-type lookup ignores non-public types from references)
- `record Q { public int X { get; set; } }` and `record struct S(int X)` → **compile and run**
- top-level `Span<int>` → CS8345 (script top-level locals are fields); `Random.Shared` → CS0117
- prompt idioms `$"{args.Str("k")}"` and `e ? null : new[] {1.0}` → fine

**Why:** the `execute_navis_code` description says ".NET Framework 4.8: no Span, Random.Shared, async or records" — true for the common (positional) form, a simplification otherwise. Cheap to re-verify: a 40-line net48 console with `EnableDefaultCompileItems=false` (the scratchpad folder may hold stale Core source copies that the default glob would compile).

**How to apply:** when a host description or prompt claims a runtime limitation on net48, cite this probe instead of reasoning from memory; if the claim needs to be exact, say "no positional records (IsExternalInit)".
