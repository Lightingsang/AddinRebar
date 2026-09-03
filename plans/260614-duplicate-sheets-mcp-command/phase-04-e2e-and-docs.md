# Phase 04 — End-to-end in Revit + docs / memory

**Priority:** P1 · **Status:** ☐ · **Depends on:** Phase 03 (green builds + deploy)

## Overview

Manual end-to-end validation of the agent path in real Revit, then sync docs + project memory.

## E2E steps (manual — needs Revit + MCP service running)

1. Restart Claude Code so the MCP server reloads → confirm `mcp__revit__duplicate_sheets` is available.
2. Launch Revit (2025 or 2026), open a model with several sheets (mix: viewports, a legend, a schedule, sheet text).
3. Ribbon → start the in-process MCP socket service (TCP :8080). One Revit instance at a time holds :8080.
4. Drive the tool, e.g.:
   ```json
   { "sourceSheetNumbers": ["A101", "A102"], "prefix": "COPY-", "useSequence": true,
     "sequenceStart": 1, "sequencePadding": 2, "mode": "WithDetailing" }
   ```
5. Verify in Revit:
   - New sheets `COPY-A101…01`, `COPY-A102…02`, names mirrored with `COPY-` prefix.
   - Title block aligned, legends placed, schedules + sheet annotations present, viewports per mode.
   - **One** Ctrl+Z removes the whole batch (atomic).
   - Result JSON lists per-sheet counts + `missingSheetNumbers`.
6. Negative checks: unknown number → `missingSheetNumbers`; all-unknown → `success:false`; no open doc → `success:false`.

## Docs / memory

- `docs/mcp-architecture.md`: **update to the in-process architecture** (the current diagram still shows the old
  `mcp/plugin` + `mcp/commandset` + `command.json` Reflection design — now merged into `MyRevitAIApp/MCP Server/`,
  in-code `McpCommandRegistry`). Add `duplicate_sheets` to the command list.
- `docs/duplicate-sheets-design.md`: add an "MCP-driven path" note — same `NamingRuleEngine` + `SheetDuplicator`,
  now called in-process by `MCP Server/Duplicate Sheets/`.
- Memory `project_mcp_revit_vendored.md`: **rewrite** — MCP is no longer vendored in `mcp/`; it's merged into the
  add-in (`MyRevitAIApp.McpServer`, in-code registry in `McpSocketService.RegisterAllCommands`, no `command.json`).
- `/bs:code-review` on the new command/handler + TS tool before commit.

## Todo

- [ ] Restart session, confirm tool registered
- [ ] E2E happy path + negative checks
- [ ] Update `docs/mcp-architecture.md` (in-process rewrite) + `docs/duplicate-sheets-design.md`
- [ ] Rewrite memory note `project_mcp_revit_vendored.md`
- [ ] Code review

## Success criteria

- Agent creates correct sheets end-to-end, no WPF dialog, atomic undo, structured result. Docs + memory current.

## Consolidated open questions

1. **🟡 R24 / Revit 2024** — add config or keep R23/R25/R26/R27? (plan Q1 / phase-03)
2. The MCP socket service start UX (ribbon button vs auto-start) — confirm where the user triggers it in the new
   in-process layout (out of scope for this feature, but needed for the E2E step).
