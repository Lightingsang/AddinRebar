---
phase: 5
title: "Live verify AutoCAD 2026 + publish + docs"
status: pending
priority: P1
effort: "4h"
dependencies: [4]
type: HITL
---

# Phase 5: live verify, publish, docs

## Overview
Prove both seeds in AutoCAD 2026 on the generated fixture DWG and on the copy of the user's real DWG, then publish the server exe and
update docs.

## Requirements
- Harness step in `HPAutoCad/tools/harness/run-aec-edit-tools-live.ps1` (+ `live-verify` Python checks): build fixture DWG by the phase-1
  recipe (DBText TCVN3, DBText VNI, `.VnTimeH`, MText with two `\f` runs + `\P`, attributed block, block definition text, dimension
  override, MLeader, table, xref-like dependent block skipped, SHX style warned, locked layer refused).
- Checks: detect counts; convert `dryRun` → drawing unchanged; commit → every text equals expected Unicode, styles remapped; second run
  modified 0; `U` restores; change set begin → record → commit → rollback restores texts **and** styles; envelopes < 64 KB.
- Real DWG copy: detect + convert commit on the copy, screenshots before/after, spot-check 10 strings with the user; never saved over the original.
- Regressions: `run-aec-tools-live.ps1` and `run-aec-edit-tools-live.ps1` previous counts unchanged; HPCivil3d mirror tests green.
- Publish `HPAutoCad.Mcp.Server` (stop running `hprebar-autocad` first — exe locked); `tools/list` = previous + 2.
- Docs: CLAUDE.md HPAutoCad section (VNConvert vendored, NOTICE, seeds), AGENTS.md by hand-inserting the same text (never regenerate —
  AGENTS.md carries content CLAUDE.md lacks), skill `hp-mcp-autocad` tool catalog + workflow note (detect → dryRun → convert),
  `docs/project-changelog.md`.

## Success Criteria
- [ ] Harness new step all PASS, old steps unchanged counts.
- [ ] User confirms the real-drawing spot-check.
- [ ] Published exe lists both seeds; docs updated; code review (`code-reviewer`) findings fixed.

## Risk Assessment
- Published exe locked by a running MCP server → restart Claude Code or rename-the-running-exe trick (known).
- SECURELOAD prompts after rebuild → harness answers *Load Once* for its own pid.
