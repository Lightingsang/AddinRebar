# BRIEFING — 2026-10-18T07:15:00Z

## Mission
Drive HPDcePro 100% Standalone Reverse-Engineering through Milestones M2 to M5 with zero legacy dependency and strict clean code governance.

## 🔒 My Identity
- Archetype: orchestrator
- Roles: orchestrator, user_liaison, human_reporter, successor
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen2\
- Original parent: parent (a009d788-a2c2-489d-8f18-946b6449e124)
- Original parent conversation ID: a009d788-a2c2-489d-8f18-946b6449e124

## 🔒 My Workflow
- **Pattern**: Project
- **Scope document**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen2\PROJECT.md
1. **Decompose**: Decomposed into 5 Milestones (M1-M5). M1 is DONE. Currently executing M2.
2. **Dispatch & Execute**:
   - Direct iteration loop: Explorer -> Worker -> Reviewer -> Challenger -> Forensic Auditor -> Gate
3. **On failure**:
   - Retry: nudge stuck agent or re-send task
   - Replace: spawn fresh agent with partial progress
   - Skip: proceed without (only if non-critical)
   - Redistribute: split stuck agent's remaining work
   - Redesign: re-partition decomposition
4. **Succession**: Self-succeed at 16 spawns, write handoff.md, spawn successor
- **Work items**:
  1. M1 Master Truth Matrix [done]
  2. M2 Standalone Asset & Template Migration [done]
  3. M3 1:1 AutoCAD Functional, UI & Interactive Jig Migration [done]
  4. M4 1:1 Revit & Excel Functional Migration [ready to start]
  5. M5 Final Build, 3-Layer Parity & Forensic Integrity Audit [pending]
- **Current phase**: 4
- **Current focus**: Succession to Generation 3 for Milestone M4

## 🔒 Key Constraints
- NEVER write, modify, or create source code files directly.
- NEVER run build/test commands yourself — require workers to do so.
- NEVER investigate or explore the problem at the code level — dispatch Explorers for technical investigation.
- File-editing tools ONLY for metadata/state files (.md) in .agents/teamwork/.
- Forensic Auditor reports INTEGRITY VIOLATION => binary veto, milestone FAILS unconditionally.
- Zero hardcoded references to C:\Dce Pro.
- Never reuse a subagent after it has delivered its handoff — always spawn fresh.
- Host Memory Protection Rule: Zero parallel `dotnet` invocations. Single-node sequential execution with `$env:DOTNET_GCHeapHardLimit="10000000"`, `--disable-build-servers -m:1`. Gate agents inspect build/test logs statically.
- MTP Lock Prevention Rule: NEVER run `dotnet test`. Workers must execute compiled test binaries directly:
  `& "...\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"`
  `& "...\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"`

## Current Parent
- Conversation ID: a009d788-a2c2-489d-8f18-946b6449e124
- Updated: 2026-10-18T09:44:00Z

## Key Decisions Made
- Milestone M1 completed and certified by orchestrator_25 (104 HOÀN THÀNH, 110 ĐANG LÀ MOCK, 388 CHƯA GIẢI MÃ ĐƯỢC).
- Milestone M2 completed and certified (all assets migrated to HPDcePro/assets/, 0 occurrences of C:\Dce Pro, 392/392 tests pass, Forensic Auditor CLEAN).
- Milestone M3 completed and certified (11 Jigs, 13 XData codecs, 12 UI dialogs/palettes, 24 AutoStr steps, 422/422 tests pass, Forensic Auditor CLEAN).
- Cumulative spawn threshold 16/16 reached with 100% completed subagents. Executing self-succession to `orchestrator_25_gen3`.

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| explorer_m2_gen2_1 | teamwork_preview_explorer | M2 Asset Mapping Explorer | completed | 30a9bc71-3789-44b6-89b7-48b4516c0af9 |
| explorer_m2_gen2_2 | teamwork_preview_explorer | M2 C# Path Resolver Explorer | completed | 4733f885-b782-464a-a4b9-38fbf22d6f1e |
| explorer_m2_gen2_3 | teamwork_preview_explorer | M2 Verification Strategy Explorer | completed | 60c850ea-392f-47d0-9005-791a3e339e36 |
| worker_m2_gen2 | teamwork_preview_worker | M2 Asset Migration & Decoupling Worker | completed | 543688f1-4d10-43f9-8fa7-afb7e1558082 |
| reviewer_m2_gen2_1 | teamwork_preview_reviewer | Architecture & Decoupling Reviewer | completed | fe2a62ca-d5ea-4115-a97d-622bf314186a |
| reviewer_m2_gen2_2 | teamwork_preview_reviewer | Asset Parity & Clean Code Reviewer | completed | 89875e0f-b534-484a-9896-e77a6aea8689 |
| challenger_m2_gen2_1 | teamwork_preview_challenger | Adversarial Asset Integrity Challenger | completed | 297ee072-06e2-4b9d-ba99-bdfe8f717274 |
| challenger_m2_gen2_2 | teamwork_preview_challenger | Adversarial Path Resolution Challenger | completed | 2256bd9f-1f4d-4cc8-b37e-e6083d1a46c6 |
| auditor_m2_gen2 | teamwork_preview_auditor | M2 Forensic Integrity Auditor | completed | d1eecec2-92d2-4972-9508-a479e0a60c71 |
| explorer_m3_gen2_1 | teamwork_preview_explorer | M3 AutoCAD Jigs & Selection Explorer | completed | 72a96151-50f9-4d3e-acc0-6e90434217ee |
| explorer_m3_gen2_2 | teamwork_preview_explorer | M3 Specialized UI & AutoStr Engine Explorer | completed | 6e02ce29-1011-4831-a5d7-870d0b928a7c |
| worker_m3_gen2 | teamwork_preview_worker | M3 1:1 AutoCAD, UI & Jigs Worker | completed | 2252517b-3b88-4e59-9164-bde9d29e2152 |
| reviewer_m3_gen2_1 | teamwork_preview_reviewer | AutoCAD Jigs & UI Reviewer | completed | b5c09f9d-d360-4880-9277-d7e1bb3bc1c9 |
| reviewer_m3_gen2_2 | teamwork_preview_reviewer | XData & AutoStr Pipeline Reviewer | completed | 8d46385f-d980-46e6-be43-1aecdaf2568c |
| challenger_m3_gen2_1 | teamwork_preview_challenger | Adversarial Jigs & Geometry Challenger | completed | 640e66c8-e797-4088-87e7-451c569122bb |
| auditor_m3_gen2 | teamwork_preview_auditor | M3 Forensic Integrity Auditor | completed | 27e602fb-960e-49ab-b29d-309b6c6ced14 |

## Succession Status
- Succession required: yes (threshold 16/16 reached; all 16 subagents completed)
- Spawn count: 16 / 16
- Pending subagents: none
- Predecessor: orchestrator_25
- Successor: spawning orchestrator_25_gen3


## Active Timers
- Heartbeat cron: 1e55a9de-0d52-405d-83fa-fa58bb05a293/task-216 (every 10m)
- Safety timer: none (covered by heartbeat cron)
- On succession: kill all timers before spawning successor
- On context truncation: run `manage_task(Action="list")` — re-create if missing

## Artifact Index
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md — Master Truth Matrix
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen2\PROJECT.md — Master Project Scope
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen2\GATE_STATUS.md — Gate Status Ledger
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen2\DEAD_ENDS.md — Oscillation Guard Ledger
