# BRIEFING — 2026-09-07T15:41:00Z

## Mission
Refactor and migrate the complete R03_FoundationRebar tool into AddinRebar as 'Foundation Rebar' following Plan A (floor/mat slab foundation, 2-layer 2-way rebar mesh, pure domain logic in HPRebar.Core with xUnit tests, Revit feature layer in HPRebar/Foundation Rebar/, WPF MVVM UI with dynamic theme, and Ribbon integration).

## 🔒 My Identity
- Archetype: orchestrator
- Roles: orchestrator, user_liaison, human_reporter, successor
- Working directory: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_2
- Original parent: Project Sentinel
- Original parent conversation ID: 218114ac-389e-4412-bbe5-1e67466deba0

## 🔒 My Workflow
- **Pattern**: Project
- **Scope document**: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_2\SCOPE.md
1. **Decompose**: Decompose scope into 5 milestones (M1: Core Pure Logic, M2: xUnit Tests, M3: Revit Feature Layer, M4: WPF MVVM UI, M5: Ribbon Integration & Multi-version Build).
2. **Dispatch & Execute**:
   - Direct iteration loop per milestone: Explorer(s) -> Worker -> Reviewer(s) -> Challenger(s) -> Forensic Auditor -> Gate.
3. **On failure** (in this order):
   - Retry: nudge stuck agent or re-send task
   - Replace: spawn fresh agent with partial progress
   - Skip: proceed without (only if non-critical; AUDITOR IS NEVER SKIPPABLE)
   - Redistribute: split stuck agent's remaining work
   - Redesign: re-partition decomposition
   - Escalate: report to parent
4. **Succession**: Threshold 16 spawns, soft handoff to successor.
- **Work items**:
  1. Survey & Scope Definition [in-progress]
  2. M1: Pure Domain Logic & Geometry Engine [pending]
  3. M2: Pure Domain xUnit Test Suite [pending]
  4. M3: Revit Feature Layer & Geometry Readers [pending]
  5. M4: WPF MVVM UI & ViewModels [pending]
  6. M5: Ribbon Integration & Multi-Version Verification [pending]
- **Current phase**: Milestone M1 (Pure Domain Logic)
- **Current focus**: Milestone M1 execution: Pure Domain Logic & Geometry Engine in HPRebar.Core/FoundationRebar/

## 🔒 Key Constraints
- NEVER write, modify, or create source code files directly.
- NEVER run build/test commands yourself — require workers to do so.
- NEVER investigate or explore the problem at the code level — dispatch Explorers for technical investigation.
- You MAY use file-editing tools ONLY for metadata/state files (.md) in your .agents/ folder.
- HPRebar.Core MUST NOT reference Autodesk.Revit.*
- Binary Veto on Forensic Audit violations.
- Never reuse a subagent after it has delivered its handoff — always spawn fresh.

## Current Parent
- Conversation ID: 218114ac-389e-4412-bbe5-1e67466deba0
- Updated: 2026-09-07T15:48:00Z

## Key Decisions Made
- Architecture follows Project Pattern with 5 focused milestones matching user request.
- Respect existing codebase conventions established by Column Rebar and Beam Rebar.
- Survey completed: 23 features inventoried and assigned across M1–M5.

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| spec_miner_source_2 | teamwork_preview_spec_miner | Legacy R03_FoundationRebar spec mining | completed | 9215242f-a2c9-4fac-ada2-4188b2918b64 |
| explorer_target_2 | teamwork_preview_explorer | AddinRebar target architecture patterns | completed | fd660f05-2d19-4640-b6ec-e2bdebb3414f |
| explorer_geometry_2 | teamwork_preview_explorer | Foundation pure geometry & math formulas | completed | 81d9f949-8038-4f94-bcef-243c78d33c8b |
| worker_m1_2 | teamwork_preview_worker | Milestone M1: Core pure logic & calculators | completed | d07fc134-bd90-4de5-bd24-a64fbd36588b |
| reviewer_m1_2_1 | teamwork_preview_reviewer | Milestone M1 Code Reviewer 1 | completed | 4bd56709-44fa-43da-97d0-a48a1600f9e2 |
| reviewer_m1_2_2 | teamwork_preview_reviewer | Milestone M1 Code Reviewer 2 | completed | d0f65e1b-6a32-4726-9b3a-33ddd3f70988 |
| challenger_m1_2_1 | teamwork_preview_challenger | Milestone M1 Empirical Challenger 1 | completed | a3b4c20b-2c7c-45aa-9480-1737158d643d |
| challenger_m1_2_2 | teamwork_preview_challenger | Milestone M1 Empirical Challenger 2 | completed | 4d6a1948-57fb-405a-b521-589a13967dde |
| auditor_m1_2_1 | teamwork_preview_auditor | Milestone M1 Forensic Auditor | completed | 20a9eb9a-8afb-4aa9-bafb-c69b75ee644d |
| test_writer_m2_2 | teamwork_preview_test_writer | Milestone M2: Pure Domain xUnit Test Suite | completed | aeab4433-b2bf-4ea7-b95c-e2bfb8cf7322 |
| reviewer_m2_2_1 | teamwork_preview_reviewer | Milestone M2 Test Reviewer 1 | completed | 48911876-8559-47ea-8aca-8dd412163e9c |
| reviewer_m2_2_2 | teamwork_preview_reviewer | Milestone M2 Test Reviewer 2 | completed | 7a4811f3-ad60-47ce-a203-91ba86d648ea |
| challenger_m2_2_1 | teamwork_preview_challenger | Milestone M2 Adversarial Challenger 1 | completed | 307554f1-8351-4ac2-b2be-c2e2f9b0eb7b |
| challenger_m2_2_2 | teamwork_preview_challenger | Milestone M2 Adversarial Challenger 2 | completed | 9e16e422-c696-4bce-906a-1a7a66d622b3 |
| auditor_m2_2_1 | teamwork_preview_auditor | Milestone M2 Forensic Auditor | completed | e52fbba8-16d8-423a-813f-c4a908c1956f |
| worker_m3_4 | teamwork_preview_worker | Milestone M3 & M4: Revit Feature Layer & WPF MVVM | completed | a58ed662-fac7-4856-8502-8ae527e1a236 |
| reviewer_m3_4_1 | teamwork_preview_reviewer | Milestone M3/M4 Feature Reviewer 1 | completed | fec15f4e-4046-48af-9a43-48707505bd3e |
| reviewer_m3_4_2 | teamwork_preview_reviewer | Milestone M3/M4 UI Reviewer 2 | completed | cc91b2e1-643e-4393-a9ab-49405655baec |
| challenger_m3_4_1 | teamwork_preview_challenger | Milestone M3/M4 Geometry Challenger 1 | completed | afcda76d-43cb-42af-aa29-734f7a419783 |
| challenger_m3_4_2 | teamwork_preview_challenger | Milestone M3/M4 UI Challenger 2 | completed | 4b029ffd-77ce-4ef6-8a43-77d31ab09cf0 |
| auditor_m3_4_1 | teamwork_preview_auditor | Milestone M3/M4 Forensic Auditor | completed | ec9ceabb-4f72-42bf-a603-c59fbe6828da |
| worker_m5_2 | teamwork_preview_worker | Milestone M5: Ribbon Integration & Multi-Version Build | completed | 5339ab37-fd4b-40b8-b0bb-3394628b2117 |
| reviewer_m5_2_1 | teamwork_preview_reviewer | Milestone M5 Ribbon Reviewer 1 | completed | f79a4c20-7814-4f89-9b84-66b1a1071364 |
| reviewer_m5_2_2 | teamwork_preview_reviewer | Milestone M5 Architecture Reviewer 2 | completed | 66517e02-0913-4cee-8c2b-aa6c4db8e42e |
| challenger_m5_2_1 | teamwork_preview_challenger | Milestone M5 E2E Flow Challenger 1 | completed | 34195dad-796b-4bd6-a642-bbc56bb2208d |
| challenger_m5_2_2 | teamwork_preview_challenger | Milestone M5 Regression Challenger 2 | completed | 18a2c16c-19db-4a79-b01e-70bc739cd68f |
| auditor_m5_2_1 | teamwork_preview_auditor | Milestone M5 Forensic Auditor | completed | b1a80d05-f926-4b87-a417-8edd198cae7c |

## Succession Status
- Succession required: no
- Spawn count: 27 / 128
- Pending subagents: none
- Predecessor: orchestrator_1
- Successor: none

## Active Timers
- Heartbeat cron: task-189
- Safety timer: none

## Artifact Index
- F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\ORIGINAL_REQUEST.md — Authoritative User Request
- F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_2\DISPATCH.md — Task assignment
- F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_2\BRIEFING.md — Persistent memory
- F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_2\progress.md — Progress & heartbeat
- F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_2\SCOPE.md — Feature Inventory & Milestones
- F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_2\GATE_STATUS.md — Milestone gate verdicts
