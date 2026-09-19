---
name: scriptguard-global-alias-bypass
description: ScriptGuard namespace/member-position denials are bypassed by `global::` qualified names (reported 2026-09-16 in the ETABS phase-0 review, not fixed at that time) — verify before re-reporting
metadata:
  type: project
---

Reported 2026-09-16 (ETABS plan `plans/260916-2152-etabs-mcp-2026/reports/code-review-phase-00.md`, finding H1): `ScriptGuard.IsDeniedNamespace` compares the dotted text of the outermost member access, so `global::System.IO.File.WriteAllText(...)`, `global::System.Diagnostics.Process.Start(...)` and `global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current` pass every profile (identifier in member position is skipped by `VisitIdentifierName`; type-context uses like `new global::System.IO.FileStream()` are still caught). Not executed — reasoned from the Roslyn parse shape (~90 %).

**Why:** the ETABS profile's new `HPRebar.McpBridge.Core.Host` denial exists to keep a script from reaching the bridge singleton; the same walk-around defeats it. Plan rule said the base guard list is "Không đổi" in phase 0, so the fix was left to the lead's decision rather than applied.

**How to apply:** before flagging it again, grep `ScriptGuard.cs` for `global::` / `AliasQualifiedName` and the tests for a `global::` theory row; if present, the gap is closed — drop this memory. If still absent when reviewing any bridge/guard change, cite the report above instead of re-deriving.
