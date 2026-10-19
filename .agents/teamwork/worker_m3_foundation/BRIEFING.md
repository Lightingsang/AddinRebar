# BRIEFING — 2026-10-07T03:54:00Z

## Mission
Execute Milestone M3 (Phần Móng: Cọc, Đài móng, Giằng móng) by cleaning Revit model, adjusting 196 pile embedments, and modeling 72 footings & 22 grade beams with 2-phase verification.

## 🔒 My Identity
- Archetype: worker
- Roles: implementer, qa, specialist
- Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\
- Original parent: 22ab785c-b656-477d-a044-b5c03eddba16
- Milestone: M3 (Foundation: Piles, Footings, Grade Beams)

## 🔒 Key Constraints
- Defensively pin all 27 Grids and 10 Levels (`grid.Pinned = true`, `level.Pinned = true`).
- Delete 100% of the 211 ghost framing elements (`OST_StructuralFraming`) currently in Revit.
- Purge legacy 57 clustered floor slabs generated without authentic CAD boundaries.
- Adjust 196 piles D400 to embed 100mm into footing bottoms per verified footings schedule. Center error <= 2.0mm, top elevation exact.
- Model 72 footings (top COTE -1.600m, thicknesses 1000, 1200, 1300mm) using verified vertices.
- Model 22 grade beams GM-1..22 (top COTE -1.600m, cross-sections 300x400, 300x600, 200x300mm) using verified CAD spans.
- 2-Phase Self-Verification: query back all created elements, assert delta b = 0, delta h = 0, delta Z <= 1.0mm.
- Record in `state/bim_coordination_ledger.json`.
- Strict integrity mandate: genuine implementation, no dummy data, no hardcoded cheating.

## Current Parent
- Conversation ID: 22ab785c-b656-477d-a044-b5c03eddba16
- Updated: 2026-10-07T03:54:00Z

## Task Summary
- **What to build**: Revit foundation model (purged ghost framing & slabs, pinned grids/levels, adjusted 196 piles, 72 footings, 22 grade beams).
- **Success criteria**: Verification queries match 0 ghost framing, 27 pinned grids, 10 pinned levels, 196 piles correctly positioned and elevated, 72 footings created and dimensionally accurate, 22 grade beams created and accurate.
- **Interface contracts**: Named Pipe `\\.\pipe\hprebar-mcp-r2026`, verified datasets in `explorer_survey_3` and `scratch`.
- **Code layout**: Scripts in `scratch/` or `.agents/teamwork/worker_m3_foundation/`, ledger in `state/bim_coordination_ledger.json`.

## Key Decisions Made
- [TBD]

## Artifact Index
- `progress.md` — Real-time progress tracker and liveness heartbeat
- `handoff.md` — 5-component completion handoff report

## Change Tracker
- **Files modified**: None yet
- **Build status**: N/A
- **Pending issues**: None

## Quality Status
- **Build/test result**: N/A
- **Lint status**: N/A
- **Tests added/modified**: Self-verification scripts against live Revit IPC

## Loaded Skills
- Source: hp-mcp-revit (`.agents/skills/hp-mcp-revit/SKILL.md`)
- Core methodology: Connect and execute C# Revit API scripts / IPC requests via Named Pipe `\\.\pipe\hprebar-mcp-r2026`
