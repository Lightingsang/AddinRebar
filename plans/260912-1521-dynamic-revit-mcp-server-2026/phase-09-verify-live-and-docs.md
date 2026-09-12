---
title: "Phase 9 — Live verification in Revit 2026, docs, ADRs"
status: verified (2026-09-12) — see reports/phase-09-live-verify.md
priority: P1
effort: 8h
depends_on: [phase-07, phase-08]
created: 2026-09-12
---

# Phase 9 — Verify live & document

## Context
Phases 6–8 are xUnit-tested with `FakeRevitExecutor`. This phase proves the whole loop inside Revit 2026 and records the decisions.

## Overview
Three end-to-end scenarios on the dev machine, then documentation (CLAUDE.md, AGENTS.md regen, codebase-summary, system-architecture, ADR-05/06), memory note.

## Requirements
- Bridge redeployed once (Revit closed) with phase-6 changes; server exe republished with native SQLite.
- Scenarios executed through `mcp_call.py` against the published exe (same path Claude Code uses).

## Scenarios
1. **Hit** — `search_tools "đếm cột theo tầng"` → `analyze_model_statistics` (seed) → call by name → result + `runs` row + stability.
2. **Miss → memory** — task "tô màu dầm theo loại" not in library → `execute_revit_code` (dryRun, then real) → result has `runId`+hint → `get_run` → `propose_tool color_beams_by_type` → `test_tool` (2 cases, dryRun) → `publish_tool` → `pending_approval` + review file → `HPRebar.Mcp.Server.exe registry approve color_beams_by_type` → tool appears in `tools/list` of the running server without restart → call it on `NhaDanDung-3Tang-KetCau.rvt`.
3. **Degradation** — publish a deliberately fragile tool (needs a Room) on the RC model → 3 failures out of 5 → `quarantined`, gone from `tools/list`, review file explains; `manage_tool restore` after fix + test.

## Implementation steps
1. Close Revit → `dotnet build HPRebar.slnx -c Debug.R26` (deploys bridge) → publish server exe → open Revit, Always Load, tick opt-in.
2. Run scenarios 1–3 with `mcp_call.py`; capture outputs into `plans/.../reports/phase-09-live-verify.md`.
3. Docs: CLAUDE.md § MCP bridge (registry, tool surface, CLI, policy), AGENTS.md regen via engine, `docs/codebase-summary.md`, `docs/system-architecture.md` (registry diagram), `adr/adr-05-typed-tools-are-script-templates.md`, `adr/adr-06-tool-registry-and-publish-gate.md`.
4. Memory file update (`dynamic-revit-mcp-server-plan`).

## Todo
- [x] redeploy · [x] scenario 1 · [x] scenario 2 · [x] scenario 3 · [x] report · [x] docs/ADR-05/06 · [x] memory

## Success criteria
All three scenarios pass on the published exe; docs describe the real state (Planned/Built/Tested/Verified kept distinct).

## Risks
| Risk | Mitigation |
|---|---|
| Unsigned DLL prompt | user clicks Always Load |
| Single-file exe + native sqlite fails to start | test published exe before Revit steps; fallback `PublishSingleFile=false` folder publish |

## Next steps
Optional: approval UI in bridge window; TUnit in-Revit tests for seed tools; embeddings search.
