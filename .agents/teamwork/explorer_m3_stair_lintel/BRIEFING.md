# BRIEFING — 2026-10-17T10:23:00Z

## Mission
Mine exact specifications and design the architecture for the Stair & Lintel detailing subsystem (Feature 12 of Phase 4, Milestone M3) across HPKata.Core, HPKata.AutoCad, HPKata.Revit, HPKata.UI, and Excel scheduling.

## 🔒 My Identity
- Archetype: explorer
- Roles: Stair & Lintel Domain Explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_stair_lintel
- Original parent: a7a2dbbb-6b7d-4279-98cd-1216d8280d80
- Milestone: M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Clean Code rules: <= 300 lines/file, <= 50 lines/method, zero CAD/Revit dependencies in HPKata.Core
- Concurrency/MTP: NEVER run concurrent dotnet test/build storms without isolation
- Reference sources: C:\kata_pro\Kata_pro64_Cad2025.dll, C:\kata_pro\Goi lenh.lsp, docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md

## Current Parent
- Conversation ID: a7a2dbbb-6b7d-4279-98cd-1216d8280d80
- Updated: 2026-10-17T10:23:00Z

## Investigation State
- **Explored paths**:
  - `C:\kata_pro\Kata_pro64_Cad2025.dll` (`Lanh_to`, `cau_thang_new`, `view_thang_new`, `view_thang_zz`, `Lenh_thep`).
  - `C:\kata_pro\Goi lenh.lsp` (commands `ve_thang`/`vethang`, `velanhto`, `vemc`).
  - `C:\kata_pro\Kata.xlsm` & `Kata ENG.xlsm` (sheet inventory, schedule structure).
  - Existing patterns in `HPKata.Core` (`Beams/`, `Columns/`, `Walls/`, serializers, aggregators).
  - Existing patterns in `HPKata.AutoCad` (`Commands/`, `Controllers/`, `GoiLenh_HPKata.lsp`).
  - Existing patterns in `HPKata.Revit` (`Commands/`, `Services/FramingRebarGenerator.cs`, `ColumnRebarGenerator.cs`).
- **Key findings**:
  - Stair flight model requires 2-tier reinforcement: continuous soffit tension bars + scissor-cranked bars crossing re-entrant corner ($L_{ext} \ge 35d$) to prevent radial blowout.
  - Lintel model requires clear span $L_0$, bearing $L_{bear} \ge 200\text{ mm}$ (typ. $250\text{ mm}$), $2\phi$ top/bottom bars with $90^\circ$ hooks, dense stirrups $\le 100\text{ mm}$ at bearings.
  - Generic section cut (`vemc`) cuts across beams, columns, walls, stairs with callout tags, dimension chains, and hatch.
  - DWG XRecord serialization to keys `KATA_STAIR_DATA` and `KATA_LINTEL_DATA`.
  - Revit 2026 generator hosts native 3D `Rebar` in `OST_Stairs` elements with idempotent cleanup.
  - ClosedXML exports formatted schedule to sheet `Thang` with TCVN diameter grouping bins.
- **Unexplored areas**: None. Domain exploration is complete.

## Key Decisions Made
- Partitioned subsystem into 23 distinct files across `HPKata.Core`, `HPKata.AutoCad`, `HPKata.Revit`, `HPKata.UI`, and `HPKata.Core.Tests`.
- All planned files stay strictly $\le 210$ lines ($\le 300$ budget).
- Formulated exact mathematical definitions for flight boundary polygons, step nosings, and scissor crank crossings.

## Artifact Index
- DISPATCH.md — Task assignment
- BRIEFING.md — Persistent context
- progress.md — Liveness heartbeat
- stair_lintel_spec_report.md — Primary specification deliverable
- handoff.md — Final handoff report
