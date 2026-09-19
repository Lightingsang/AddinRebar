---
name: mirror-fence-review-pitfalls
description: Blind spots of the HPCivil3d ↔ HPAutoCad mirror test (tools/mirror-tokens.json + MirrorTests) found in the phase-2 review; check these first when a copied-host bridge is reviewed again
metadata:
  type: project
---

The Civil 3D bridge is a token-rewritten copy of the AutoCAD bridge (ADR-01 option A); `HPCivil3d/tools/mirror-tokens.json` is the contract and `HPCivil3d.McpBridge.Tests/MirrorTests.cs` the fence. Phase-2 review (2026-09-18, `reports/code-review-phase-02.md`, 8/10) found the fence itself has three blind spots:

- **Per-file skip:** the theory skips when the *AutoCAD file* is missing, so a rename/delete on the AutoCAD side turns the fence off for that file silently (should skip only when `HPAutoCad/` root is absent).
- **One-directional coverage:** only the Civil tree is enumerated; a new AutoCAD file or a change to an owned counterpart (`BridgeEntry`, `ScriptingSelfCheck`, csproj, `PackageContents.xml`, `launchSettings.json`) is invisible. Suggested: reverse enumeration + pinned `autocadSha256` per owned counterpart.
- **Add-only blocks keep the AutoCAD line:** doc comments on a member (`Civil3dScriptGlobals.units` said INSUNITS) contradict the Civil rule — one-line differences belong in tokens, not blocks.

Also: the server tree (`HPCivil3d.Mcp.Server`) is a copy too but outside the contract (phase-3 item).

**Why:** every HP MCP host after AutoCAD starts as a copy; the fence is the only thing that catches an AutoCAD fix not being copied.
**How to apply:** when reviewing any `HP<Host>/` that mirrors `HPAutoCad/`, verify these three points before reading the diff; re-run the one-character mutation yourself (line 56 of the runner, `var logs  = new`) rather than trusting the report — the first attempt here hit the wrong line and passed trivially.
