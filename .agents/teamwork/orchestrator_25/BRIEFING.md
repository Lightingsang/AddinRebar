# BRIEFING — 2026-10-18T07:15:00Z

## Mission
Analyze the legacy software suite at `C:\Dce Pro`, build `docs/rea-dce-pro/TRUTH_MATRIX.md`, migrate all assets into `HPDcePro/assets/`, and implement a 100% standalone, feature-complete C# replacement in `HPDcePro/HPDcePro.slnx` for AutoCAD 2026, Revit 2026, and Excel with zero runtime dependency on `C:\Dce Pro`.

## 🔒 My Identity
- Archetype: orchestrator
- Roles: orchestrator, user_liaison, human_reporter, successor
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\
- Original parent: parent (a009d788-a2c2-489d-8f18-946b6449e124)
- Original parent conversation ID: a009d788-a2c2-489d-8f18-946b6449e124

## 🔒 My Workflow
- **Pattern**: Project
- **Scope document**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md
1. **Decompose**: Decompose HPDcePro 100% Standalone Reverse-Engineering & Truth Matrix into 5 milestones across binary analysis/truth matrix, asset migration, AutoCAD/UI migration, Revit/Excel migration, and build/forensic audit.
2. **Dispatch & Execute**:
   - Survey: Spawn 3 Explorers in parallel to map legacy baseline at `C:\Dce Pro` and existing solution at `HPDcePro/`.
   - Iteration Loop: Explorer(s) -> Worker -> Reviewer(s) -> Challenger(s) -> Forensic Auditor -> Gate.
3. **On failure**:
   - Retry: nudge stuck agent or re-send task
   - Replace: spawn fresh agent with partial progress
   - Skip: proceed without (only if non-critical)
   - Redistribute: split stuck agent's remaining work
   - Redesign: re-partition decomposition
   - Escalate: report to parent (sub-orchestrators only, last resort)
4. **Succession**: At 16 spawns, write handoff.md, spawn successor.
- **Work items**:
  1. Step 0 Survey: Binary analysis & scope mapping [done]
  2. M1: Comprehensive Binary Analysis, Package Extraction & Truth Matrix (`docs/rea-dce-pro/TRUTH_MATRIX.md`) [done]
  3. M2: Standalone Asset & Template Migration (`HPDcePro/assets/`) [ready]
  4. M3: 1:1 AutoCAD Functional, UI & Interactive Jig Migration (`HPDcePro.AutoCad`, `HPDcePro.UI`) [pending]
  5. M4: 1:1 Revit & Excel Functional Migration (`HPDcePro.Revit`, `HPDcePro.Excel`) [pending]
  6. M5: Final Build, 3-Layer Parity & Forensic Integrity Audit [pending]
- **Current phase**: 2 (Milestone M1 completed -> Transitioning to M2 via Succession)
- **Current focus**: Self-Succession to orchestrator_25_gen2

## 🔒 Key Constraints
- NEVER write, modify, or create source code files directly.
- NEVER run build/test commands yourself — require workers to do so.
- NEVER investigate or explore the problem at the code level — dispatch Explorers for technical investigation.
- You MAY use file-editing tools ONLY for metadata/state files (.md) in your .agents/teamwork/ folder.
- DO NOT CHEAT. All implementations must be genuine. Forensic Auditor has binary veto.
- Strictly read-only C:\Dce Pro (never modify, delete, or create anything inside C:\Dce Pro).
- Clean Code Governance (<=300 lines/file, <=50 lines/method, 0 dead code, 0 unannotated catch, TreatWarningsAsErrors=true).
- Sequential single-node build: dotnet build/test with `--disable-build-servers -m:1`.

## Current Parent
- Conversation ID: a009d788-a2c2-489d-8f18-946b6449e124
- Updated: 2026-10-18T05:30:07Z

## Key Decisions Made
- Step 0 Survey completed by 3 parallel Explorers.
- Milestone M1 successfully passed through 3 iterations, reaching 100% verified status for all 602 endpoints (104 HOÀN THÀNH, 110 ĐANG LÀ MOCK, 388 CHƯA GIẢI MÃ ĐƯỢC).
- Forensic Auditor verified 100% clean, zero tampering, 369/369 tests green.
- Reached 16 spawns and 16 completed subagents. Self-succession triggered.

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| explorer_survey_1 | teamwork_preview_explorer | Legacy Baseline Binary Analyst | completed | bc15b506-524c-41f9-a17b-0e8aa10de305 |
| explorer_survey_2 | teamwork_preview_explorer | Target Solution & Mocks Auditor | completed | 91958797-2e2a-43c4-9bbc-a24ea1cde4f9 |
| explorer_survey_3 | teamwork_preview_explorer | Catalogs & Asset Mapping Specialist | completed | 4d979c4d-db6c-4ccb-b64f-963100532df9 |
| explorer_m1_1 | teamwork_preview_explorer | Truth Matrix Architect | completed | 967832e7-a9b8-4d2a-8a40-77b796694106 |
| spec_miner_m1_2 | teamwork_preview_spec_miner | AutoCAD & UI Spec Miner | completed | 7b6dcb8c-338d-4612-a85a-b9f4cb9a5b29 |
| spec_miner_m1_3 | teamwork_preview_spec_miner | Excel & Revit Spec Miner | completed | ae55d755-4b4e-4c96-8db8-b5a67d33c598 |
| worker_m1 | teamwork_preview_worker | Truth Matrix Publisher | completed | b331246f-4bf7-40b7-aa4c-7b608f366b9c |
| reviewer_m1_1 | teamwork_preview_reviewer | AutoCAD & UI Reviewer | completed | 0355f53c-1ab9-484e-bae1-2071c71bec0b |
| reviewer_m1_2 | teamwork_preview_reviewer | Core & Domain Reviewer | completed | 0326222a-037f-4e34-9b57-d180d5e42b00 |
| worker_m1_2 | teamwork_preview_worker | Truth Matrix Remediation Specialist | completed | 205af206-4a89-4c35-a2ce-e3b915d83845 |
| reviewer_m1_1_gen2 | teamwork_preview_reviewer | AutoCAD & UI Reviewer (gen2) | completed | 035a9a77-717e-47b2-ace7-16279c7f17f1 |
| reviewer_m1_2_gen2 | teamwork_preview_reviewer | Core & Domain Reviewer (gen2) | completed | 7c12445e-59a6-451d-a0b5-dc219832615b |
| challenger_m1_1 | teamwork_preview_challenger | Adversarial Data Challenger | completed | 1a482cad-9a2f-4f77-88d4-caee0879cfe0 |
| challenger_m1_2 | teamwork_preview_challenger | Adversarial Build Challenger | completed | 801879ff-d7d8-4ea5-9a64-591a850c19a2 |
| auditor_m1 | teamwork_preview_auditor | Forensic Integrity Auditor | completed | 6048e545-65b9-4ea3-acec-f27778a85723 |
| worker_m1_3 | teamwork_preview_worker | Truth Matrix Remediation Specialist (gen3) | completed | 39c30b77-cc91-4bd3-ae79-74d5410a5069 |

## Succession Status
- Succession required: yes
- Spawn count: 16 / 16
- Pending subagents: none
- Predecessor: none
- Successor: spawning now

## Active Timers
- Heartbeat cron: 1b2bf217-fcc4-4416-87ff-86b0fe0ef59a/task-20 (canceling before succession)
- Safety timer: none
- On succession: kill all timers before spawning successor

## Artifact Index
- `docs/rea-dce-pro/TRUTH_MATRIX.md` — Authoritative Master Truth Matrix (602 endpoints)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md` — Project specification & milestone roadmap
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\GATE_STATUS.md` — Gate verdicts
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\handoff.md` — Soft handoff to Successor Generation 2
