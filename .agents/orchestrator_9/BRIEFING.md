# BRIEFING — 2026-09-27T15:57:37Z

## Mission
Implement the Kata Rebar feature in HPRebar (reading sheet 'Dam' from Kata.xlsm to generate 3D beam rebar in Revit 2026).

## 🔒 My Identity
- Archetype: orchestrator
- Roles: [orchestrator, user_liaison, human_reporter, successor]
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9
- Original parent: Sentinel / Parent Agent
- Original parent conversation ID: 68d40caa-242b-4b75-9541-008aeac8f556

## 🔒 My Workflow
- **Pattern**: Project
- **Scope document**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\PROJECT.md
1. **Survey**: Spawn 3 Explorers in parallel to map KataExport reference, Excel sheet Dam layout, Rebar creation APIs in HPRebar.
2. **Decompose & Plan**: Create PROJECT.md with architecture, feature inventory, milestones, interface contracts.
3. **Dispatch & Execute**:
   - Milestone 1: Kata Dam Sheet Data Parser & DTOs (HPRebar.Core)
   - Milestone 2: Rebar Geometry & Distribution Calculator (HPRebar.Core)
   - Milestone 3: xUnit Test Suite in HPRebar.Core.Tests
   - Milestone 4: Revit 3D Rebar Generation Service & BarType Resolution (HPRebar)
   - Milestone 5: WPF MVVM UI & Ribbon Integration (HPRebar)
   - Milestone 6: Build, Test & Integration Verification
4. **Gate**: Worker -> Reviewer -> Challenger -> Forensic Auditor -> Gate Verdict
5. **Succession**: At 16 spawns, write handoff.md, spawn successor.
- **Work items**:
  1. Survey & Codebase Exploration [in-progress]
  2. Decomposition & PROJECT.md [pending]
  3. Milestone 1: Core DTOs & Sheet Parser [pending]
  4. Milestone 2: Rebar Geometry Calculator [pending]
  5. Milestone 3: Unit Tests [pending]
  6. Milestone 4: Revit Rebar Generator [pending]
  7. Milestone 5: WPF UI & Ribbon [pending]
  8. Milestone 6: Final Verification & Audit [pending]
- **Current phase**: 0 (Survey)
- **Current focus**: Map codebase patterns and Kata sheet Dam specification

## 🔒 Key Constraints
- NEVER write, modify, or create source code files directly.
- NEVER run build/test commands yourself — require workers to do so.
- NEVER investigate or explore the problem at the code level — dispatch Explorers for technical investigation.
- File-editing tools ONLY for metadata/state files (.md) in our agent folder.
- Subagents must receive the path to ORIGINAL_REQUEST.md.
- Never reuse a subagent after it has delivered its handoff.
- Mandatory integrity warning on all Worker dispatches.
- Forensic Auditor verdict is a BINARY VETO.

## Current Parent
- Conversation ID: 68d40caa-242b-4b75-9541-008aeac8f556
- Updated: not yet

## Key Decisions Made
- Project pattern selected.
- Subagents will have dedicated workspaces under .agents/teamwork/.

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| explorer_survey_1 | teamwork_preview_explorer | Survey Kata & Excel Domain | Completed | 79952425-ba4a-4b8e-afe5-c3129196aff4 |
| explorer_survey_2 | teamwork_preview_explorer | Survey Core Beam Rebar Math | Completed | 02e110cb-38f0-4363-bf9c-00e0f1d14615 |
| explorer_survey_3 | teamwork_preview_explorer | Survey Revit Creation & UI | Completed | 1c5ce194-71dc-4fc6-bd5a-1c02db4a384a |
| worker_m1 | teamwork_preview_worker | Implement Milestone 1 (DTOs & Parsers) | Completed | bb811f44-18ca-436c-a8aa-330682675c0a |
| worker_m2 | teamwork_preview_worker | Implement Milestone 2 (Geometry Calculator) | Completed | 9410ca4e-c8c2-415e-a0f6-9c3cdbb6b8b6 |
| worker_m4_m5 | teamwork_preview_worker | Implement Milestones 4 & 5 (Revit Services & UI) | Completed | 258ea01b-1b86-4a46-a2d5-db9a1fad4974 |
| reviewer_1 | teamwork_preview_reviewer | Review Core Domain & Math | Completed | 77233a72-05aa-48cd-b0b7-97e4fe5a2bc0 |
| reviewer_2 | teamwork_preview_reviewer | Review Revit Services & UI | Completed | 6e412c7e-bedb-4c64-89d0-46e0e3e2df9c |
| challenger_1 | teamwork_preview_challenger | Stress-Test Math & Parser | Completed | 76f94b45-63ad-42c1-99ae-c7a68e6ee3b9 |
| challenger_2 | teamwork_preview_challenger | Stress-Test Revit & Idempotency | Completed | 193aa200-ce9e-4d79-9ef0-2fcc058d1494 |
| auditor_1 | teamwork_preview_auditor | Forensic Integrity Audit | Completed | 9dac723d-a78c-48bf-98e9-ffd3459cae65 |
| worker_fix | teamwork_preview_worker | Fix Challenger 2 Findings | Completed | 19b39463-a560-40c7-8125-5197dc40ba2e |
| challenger_2_retry | teamwork_preview_challenger | Re-verify Remediated Fixes | Completed | e85cf069-f1b0-4fea-a971-455800118526 |

## Succession Status
- Succession required: no
- Spawn count: 13 / 16
- Pending subagents: none
- Predecessor: none
- Successor: not required (project complete)

## Active Timers
- Heartbeat cron: aa8876fc-b61d-4725-aacd-616632eb9cc0/task-16
- Safety timer: none

## Artifact Index
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\DISPATCH.md — Dispatch instructions
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\context.md — Mission context
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\progress.md — Progress log
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\BRIEFING.md — Persistent memory
