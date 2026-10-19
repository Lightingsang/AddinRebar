# BRIEFING — 2026-10-17T11:32:00Z

## Mission
Adversarial stress-testing and empirical verification on Milestone M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing) covering Stair, Lintel, Cross-Section, and ClosedXML Persistence subsystems.

## 🔒 My Identity
- Archetype: challenger
- Roles: critic, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_2\
- Original parent: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Milestone: M3
- Instance: 2 of 2
- Current parent: a7a2dbbb-6b7d-4279-98cd-1216d8280d80
- Active Task: Empirical stress-testing and verification of Phase 4 (Stair, Lintel, Cross-Section, Persistence)

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code
- Run verification code yourself. Empirical evidence required
- .agents/teamwork/ contains only metadata — no source code or tests
- Strict adherence to Clean Code Governance (HP_CLEAN_CODE_CORE.md)
- Verdict must be APPROVE or REQUEST_CHANGES
- Concurrency/MTP: MUST ALWAYS use --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"
- Empirical verification verdict: CONFIRMED_CORRECT or DEFECT_FOUND

## Current Parent
- Conversation ID: a7a2dbbb-6b7d-4279-98cd-1216d8280d80
- Updated: 2026-10-17T11:32:00Z

## Review Scope
- **Files reviewed**:
  - `HPKata/HPKata.Core/Stairs/Calculators/StairGeometryCalculator.cs`
  - `HPKata/HPKata.Core/Stairs/Calculators/StairRebarCalculator.cs`
  - `HPKata/HPKata.Core/Stairs/Models/StairFlightModel.cs`
  - `HPKata/HPKata.Core/Stairs/Calculators/LintelRebarCalculator.cs`
  - `HPKata/HPKata.Core/Stairs/Models/LintelModel.cs`
  - `HPKata/HPKata.Core/Stairs/Calculators/GenericSectionCalculator.cs`
  - `HPKata/HPKata.Core/Stairs/Models/GenericSectionModel.cs`
  - `HPKata/HPKata.Core/Foundations/Persistence/FoundationPlanScheduleWorkbookGenerator.cs`
  - `HPKata/HPKata.Core/Slabs/Persistence/SlabPlanScheduleWorkbookGenerator.cs`
  - `HPKata/HPKata.Core/Stairs/Persistence/StairPlanScheduleWorkbookGenerator.cs`
  - `HPKata/HPKata.Core.Tests/Stairs/`
- **Interface contracts**: `PROJECT.md`, `worker_m3_remediation/handoff.md`, `worker_m3_remediation/changes.md`
- **Review criteria**: mathematical correctness, edge case survival, ergonomic formula accuracy, re-entrant corner tension scissor-crank extension rule $L_{ext} \ge \max(35d, 500\text{mm})$, lintel bearing length requirements, ClosedXML OpenXML format and formula validity.

## Key Decisions Made
- Initiated empirical verification session for Phase 4 Stair, Lintel, Cross-Section, and ClosedXML persistence subsystems.
- Set up test strategy to challenge Blondel formulas, scissor-crank crossing, lintel bearing limits, generic section geometries, and OpenXML sheet generation.

## Artifact Index
- `challenge_report.md` — Empirical test report for Phase 4 subsystems
- `handoff.md` — Final handoff report to parent
- `progress.md` — Heartbeat tracking
- `DISPATCH.md` — Dispatch records

## Attack Surface
- **Hypotheses tested**:
  - H1: Blondel formula $2 \cdot h_r + b_t \in [580, 660]\text{ mm}$ correctly flags steep and flat treads.
  - H2: Scissor-crank extension $L_{ext} = \max(500, 35d)$ is strictly maintained across bar diameters 10mm..32mm.
  - H3: Lintel bearing length checks require $L_{bearing} \ge \max(200\text{mm}, h_{lintel}/2)$ or model validation constraints.
  - H4: GenericSectionCalculator correctly cuts and callouts section points and stirrup hoops.
  - H5: ClosedXML sheets 'Mong', 'San', 'Thang' generate genuine OpenXML spreadsheets with valid non-zero data and columns.
- **Vulnerabilities found**: TBD during stress testing
- **Untested angles**: Active testing underway

## Loaded Skills
- None
