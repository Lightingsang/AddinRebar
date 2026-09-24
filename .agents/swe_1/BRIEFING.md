# BRIEFING — 2026-09-24T06:50:00Z

## Mission
Integrate project-local Archify v2.16.0 skill into HPRebar repository, create two verifiable interactive architecture diagrams (HPRebar System Architecture and Column Rebar Workflow), and link them in existing architecture documentation.

## 🔒 My Identity
- Archetype: teamwork_preview_swe
- Roles: orchestrator, user_liaison, human_reporter, successor
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\swe_1
- Original parent: parent
- Original parent conversation ID: baf12e85-f773-4510-8171-9bd7048e4430

## 🔒 My Workflow
- **Pattern**: SWE Light
- **Scope document**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\swe_1\context.md
1. **Decompose**: No decomposition. Single line of work per SWE Light pattern.
2. **Dispatch & Execute**:
   - Dispatch teamwork_preview_implementer alone with verbatim task.
   - Run adversarial review rounds with teamwork_preview_reviewer (minimum 3 review rounds).
   - Verify diff and re-run test commands independently (`archify doctor`, `archify validate`, `archify deliver`).
3. **On failure**:
   - Retry: nudge stuck agent
   - Replace: spawn fresh agent from interruption point
   - Redesign: carry open ledger items to next review round
4. **Succession**: At spawn count >= 16 or context overflow, write soft handoff.md, cancel timers, spawn successor.
- **Work items**:
  1. Archify v2.16.0 skill integration & diagrams [pending]
- **Current phase**: 2 (Dispatch & Execute)
- **Current focus**: Implementation round (teamwork_preview_implementer)

## 🔒 Key Constraints
- NEVER write, modify, or create source code files yourself. Delegate all implementation and all repair to teamwork_preview_implementer and teamwork_preview_reviewer.
- NEVER explore or debug the codebase in order to solve the task yourself.
- Do NOT run global sync-agent-skills.py apply.
- Do NOT touch .csproj, .slnx, installer, Revit runtime, or MCP build.
- HPRebar.Core remains 100% free of Autodesk references and zero C# logic modified.
- Never reuse a subagent after it has delivered its handoff — always spawn fresh.

## Current Parent
- Conversation ID: baf12e85-f773-4510-8171-9bd7048e4430
- Updated: 2026-09-24T06:50:00Z

## Key Decisions Made
- SWE Light loop initiated: implementer -> reviewer -> reviewer -> reviewer -> victory auditor.

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| implementer_r1 | teamwork_preview_implementer | Archify skill installation & diagrams | in-progress | 63b0262c-4cd6-4cdd-8e38-95fd4c25a345 |

## Succession Status
- Succession required: no
- Spawn count: 1 / 16
- Pending subagents: 63b0262c-4cd6-4cdd-8e38-95fd4c25a345
- Predecessor: none
- Successor: not yet spawned

## Active Timers
- Heartbeat cron: d9313c9b-4a0e-49ad-a578-34cc518ec229/task-16
- Safety timer: none

## Artifact Index
- context.md — Task context and requirements summary
- DISPATCH.md — Dispatch log
- progress.md — Live status and iteration tracking
