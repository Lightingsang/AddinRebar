# BRIEFING — 2026-10-18T02:35:00Z

## Mission
Transform HPKata into a 100% standalone structural rebar detailing & fabrication suite installed centrally at C:\HPKata\, bundled with its complete CAD/Excel/LISP/Icon/Shape library and dedicated standalone installer (HPKata_Setup.exe), operating identically to original KataPro with zero dependency on C:\kata_pro\.

## 🔒 My Identity
- Archetype: orchestrator
- Roles: orchestrator, user_liaison, human_reporter, successor
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_23\
- Original parent: parent
- Original parent conversation ID: 863591cc-183d-404f-9810-d0f191f6ebe6

## 🔒 My Workflow
- **Pattern**: Project
- **Scope document**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md
1. **Decompose**: Survey full scope with 3 Explorers, create PROJECT.md (Feature Inventory, Architecture, Milestones, Interface Contracts, Code Layout).
2. **Dispatch & Execute**:
   - Decompose into milestones, delegate to sub-orchestrator or iteration loop (Explorer -> Worker -> Reviewer -> Challenger -> Auditor -> Gate).
3. **On failure** (in this order):
   - Retry: nudge stuck agent or re-send task
   - Replace: spawn fresh agent with partial progress
   - Skip: proceed without (only if non-critical)
   - Redistribute: split stuck agent's remaining work
   - Redesign: re-partition decomposition
   - Escalate: report to parent (sub-orchestrators only, last resort)
4. **Succession**: Self-succeed at 16 spawns, write handoff.md, spawn successor.
- **Work items**:
  1. Survey & Feature Inventory [done]
  2. M1: Asset Migration & C:\HPKata\ Environment [done]
  3. M2: Bi-Directional Excel Integration & Detailing Parity [done]
  4. M3: Interop Template Importer & Config/Diagram Binding [pending - next]
  5. M4: Centralized Deployment & HPKata_Setup.exe Installer [pending]
  6. M5: Final Verification & E2E Validation [pending]
- **Current phase**: 3 (Milestone M3 Exploration)
- **Current focus**: Milestone M3 Exploration (3 parallel Explorers: Template Importer, 86-Param Config Sync, Visual Assets & Icons)

## 🔒 Key Constraints
- NEVER write, modify, or create source code files directly (DISPATCH-ONLY).
- NEVER run build/test commands yourself — require workers to do so.
- NEVER investigate or explore the problem at the code level — dispatch Explorers for technical investigation.
- You MAY use file-editing tools ONLY for metadata/state files (.md) in your .agents/teamwork/ folder.
- DO NOT switch to MahApps.Metro. Retain MaterialDesignThemes v5.2.1 and Theme.xaml StaticResource keys.
- Every .cs file <= 300 lines, methods <= 50 lines, 0 warnings (TreatWarningsAsErrors=true), 0 errors.
- Single-node build: dotnet build/test with --disable-build-servers -m:1.
- Zero occurrences of kata_pro in HPKata/ source files.
- Binary veto on Forensic Auditor integrity violations.

## Current Parent
- Conversation ID: 863591cc-183d-404f-9810-d0f191f6ebe6
- Updated: 2026-10-18T03:25:00Z

## Key Decisions Made
- Heartbeat cron task-16 active.
- Survey phase completed, master PROJECT.md published.
- E2E testing track certified (TEST_READY.md, 232 tests, all 13 WPF windows verified on STA thread).
- Milestone M1 100% completed & passed gate cleanly (Features 1-4).
- Milestone M2 dispatched with 3 parallel Explorers (Features 5-8).

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| explorer_survey_1 | teamwork_preview_explorer | Codebase & Asset Explorer | completed | 3de45271-3f55-4504-80bf-8ec280840851 |
| explorer_survey_2 | teamwork_preview_explorer | Parity & Interop Explorer | completed | 6a9cf548-5e50-4e27-9d53-730464c6f1b8 |
| explorer_survey_3 | teamwork_preview_explorer | Deployment & Installer Explorer | completed | 688d2849-5f0c-4c8e-819e-dd97ec20d072 |
| explorer_m1_1 | teamwork_preview_explorer | M1 Core Environment Explorer | completed | 68730449-d5d4-4ed6-8a30-f7ec32f494ed |
| explorer_m1_2 | teamwork_preview_explorer | M1 Asset Staging & Rewriter Explorer | completed | 0c52a55d-8669-4e6c-9367-ea9c1d6d08ce |
| explorer_m1_3 | teamwork_preview_explorer | M1 Codebase Purge Explorer | completed | 20504268-d918-4ab8-a9be-af2c857fd53c |
| test_writer_e2e | teamwork_preview_test_writer | E2E Testing Track Test Writer | completed | f6b472ce-90ff-4e8a-98be-f10f823d3fda |
| worker_m1 | teamwork_preview_worker | M1 Implementation Worker | completed | 5c6ce3d1-2b62-481c-8779-4f46e6d9fa96 |
| reviewer_m1_1 | teamwork_preview_reviewer | M1 Independent Reviewer | completed | 7dc07bb0-046a-4177-a8c6-d942e39e80ba |
| reviewer_m1_2 | teamwork_preview_reviewer | M1 Adversarial Reviewer | completed | 6caad7a9-c513-4237-872c-054a50fd00e6 |
| challenger_m1_1 | teamwork_preview_challenger | M1 Environment Challenger | completed | 01bdfd50-77b4-48b1-909c-71f9fa311c41 |
| challenger_m1_2 | teamwork_preview_challenger | M1 Asset & Purge Challenger | completed | 9ddc29b3-99ed-4127-9c6e-cbefff7c9b55 |
| auditor_m1_1 | teamwork_preview_auditor | M1 Forensic Auditor | completed | bbb7bd01-0b3b-485b-aacd-d5a8c6844a40 |
| explorer_hpkata_m2_1 | teamwork_preview_explorer | M2 Core Excel Engine Explorer | completed | 15ffbee8-1a2a-4c96-aebd-d24c72ff73c3 |
| explorer_hpkata_m2_2 | teamwork_preview_explorer | M2 UI Integration Explorer | completed | 2d840990-8dd3-4c08-bedd-c6438086e4a2 |
| explorer_hpkata_m2_3 | teamwork_preview_explorer | M2 Host Commands Explorer | completed | 0f67bcf0-cff9-487f-865e-d84ff9c649b2 |
| worker_hpkata_m2 | teamwork_preview_worker | M2 Implementation Worker | completed | 1cc5587e-aca9-49e4-b933-0163b26068ab |
| reviewer_hpkata_m2_1 | teamwork_preview_reviewer | M2 Independent Reviewer | completed | 0b799f9b-9c30-44ec-93d3-c3de34378cb1 |
| reviewer_hpkata_m2_2 | teamwork_preview_reviewer | M2 Adversarial Reviewer | completed | ecc550f5-6149-4ed9-ac1c-a859c1dc2b2d |
| challenger_hpkata_m2_1 | teamwork_preview_challenger | M2 Excel Empirical Challenger | completed | 6c4a3a96-11f1-4a59-9be0-10332d0c7a0f |
| challenger_hpkata_m2_2 | teamwork_preview_challenger | M2 Host Commands Challenger | completed | 74c111d9-f4fd-46ea-b8c3-7223d0b422d8 |
| auditor_hpkata_m2_1 | teamwork_preview_auditor | M2 Forensic Auditor | completed | 8d9d43cd-343b-4370-b4b9-8368cc474730 |
| explorer_hpkata_m2_rem_1 | teamwork_preview_explorer | M2 Rem Core & Audit Explorer | completed | 63f144bf-a1b5-47e0-81f2-4531b1f61a94 |
| explorer_hpkata_m2_rem_2 | teamwork_preview_explorer | M2 Rem Revit & Test Explorer | completed | 614151a3-feea-45a8-be5b-0a115421dc15 |
| explorer_hpkata_m2_rem_3 | teamwork_preview_explorer | M2 Rem UI Constructors Explorer | completed | 1807a350-d759-4964-aa7f-cc45d614c993 |
| worker_hpkata_m2_rem | teamwork_preview_worker | M2 Remediation Worker | completed | b1fad4b3-e67e-4a8c-a66e-7dadee394b13 |
| reviewer_hpkata_m2_rem_1 | teamwork_preview_reviewer | M2 Rem Independent Reviewer | completed | b36e88a6-d4a9-48d8-a98c-2514f6abf6a8 |
| reviewer_hpkata_m2_rem_2 | teamwork_preview_reviewer | M2 Rem Adversarial Reviewer | completed | 035a15f6-bd6d-4da8-92ad-a57cd781a3b4 |
| challenger_hpkata_m2_rem_1 | teamwork_preview_challenger | M2 Rem Excel Empirical Challenger | completed | 2ea0ba38-9e73-4823-9be6-25b2f6140df1 |
| challenger_hpkata_m2_rem_2 | teamwork_preview_challenger | M2 Rem UI & Method Challenger | completed | 1bf2e41c-032b-447e-9fce-4edf09a2f7b5 |
| auditor_hpkata_m2_2 | teamwork_preview_auditor | M2 Rem Forensic Auditor | completed | 88633f2a-e8a4-4d7f-abde-b7e181692394 |
| explorer_hpkata_m3_1 | teamwork_preview_explorer | M3 Template Importer Explorer | in-progress | 84060838-ef25-4a1a-b151-c18c8051e323 |
| explorer_hpkata_m3_2 | teamwork_preview_explorer | M3 86-Param Config Sync Explorer | in-progress | 1117f20c-5a07-440e-83d8-906b47bc1d73 |
| explorer_hpkata_m3_3 | teamwork_preview_explorer | M3 Visual Assets & Icons Explorer | in-progress | 2b5caed0-1f9c-4adb-ba70-cdb92f52a476 |

## Succession Status
- Succession required: no (orchestrator continuing directly; runtime archetype policy; spawn quota 34 / 128)
- Spawn count: 34 / 128
- Pending subagents: 84060838-ef25-4a1a-b151-c18c8051e323, 1117f20c-5a07-440e-83d8-906b47bc1d73, 2b5caed0-1f9c-4adb-ba70-cdb92f52a476
- Predecessor: none
- Successor: none

## Active Timers
- Heartbeat cron: task-509
- Safety timer: none
- On succession: kill all timers before spawning successor
- On context truncation: run manage_task(Action="list") — re-create if missing

## Artifact Index
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md — Authoritative User Request
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_23\DISPATCH.md — Dispatch Specification
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_23\BRIEFING.md — Persistent Working Memory
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_23\progress.md — Liveness & Progress
