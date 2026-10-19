# BRIEFING — 2026-10-17T10:26:00Z

## Mission
Mine exact specifications and design architecture for Foundation detailing subsystem (Phase 4 / Milestone M3) in HPKata.

## 🔒 My Identity
- Archetype: Specification Miner
- Roles: Foundation Domain & Architecture Spec Miner
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_foundation\
- Original parent: a7a2dbbb-6b7d-4279-98cd-1216d8280d80
- Milestone: M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing)

## 🔒 Key Constraints
- Read-only: discover and document features by probing authoritative specification; do NOT implement anything.
- Max 300 lines/file (Rule C2), max 50 lines/method (Rule M3).
- Zero CAD/Revit dependencies in HPKata.Core.
- No access to Security.dll or licensing code; strictly clean-room reverse engineering.
- Output reports to foundation_spec_report.md and handoff.md, notify parent via send_message.

## Current Parent
- Conversation ID: a7a2dbbb-6b7d-4279-98cd-1216d8280d80
- Updated: 2026-10-17T10:26:00Z

## Task Summary
- **What was built**: Comprehensive specification report and architecture design for Foundation subsystem:
  1. Pad footings (`IsolatedFootingModel`, `ve_mong_don` / `vmd`, stepped/sloped pedestal, prismoidal volume, bottom mesh Shape 02, column starter dowels, pedestal ties).
  2. Pile caps (`PileCapModel`, `ve_mong_coc` / `vmc`, `ve_mong_coc_giat_cap`: canonical 1-9+ pile coordinate solvers, embedment depth $h_{emb} \ge 100\text{ mm}$, bottom/top rebar mesh, perimeter skin rebar, confinement ties, starter dowels).
  3. Foundation plan layout (`ve_mat_bang_mong` / `vmbm`, `rai_mong` / `rm`, `phamong`).
  4. Revit `OST_StructuralFoundation` rebar generator (`FoundationRebarGenerator.cs`, `RebarCreationService.CreateRebar` with `BarTerminationsData`, idempotent purge by comments).
  5. Excel scheduling (`FoundationScheduleAggregator.cs`, ClosedXML sheet `Mong` with 19 columns).
  6. Clean Code class design: 18 modular files partitioned under $\le 300$ lines/file and $\le 50$ lines/method.
- **Success criteria**: 100% met. Authoritative reports generated on disk.

## Key Decisions Made
- Canonical pile coordinate layouts centered at $(0, 0)$ for 1 to 9+ piles with minimum spacing $s \ge 3D$ and edge distance $c_e \ge 1.0D$.
- Pad footing pedestal volume uses exact prismoidal equation for sloped pedestals and box equation for stepped pedestals.
- Revit rebar generation leverages existing `RebarCreationService.CreateRebar` with `BarTerminationsData` and comments-based idempotent cleaning.
- Excel foundation scheduling matches `Kata.xlsm` sheet `Mong` layout with ClosedXML.

## Artifact Index
- `foundation_spec_report.md` — Authoritative Foundation Specification & Architecture Report
- `handoff.md` — 5-Component Hard Handoff Report
