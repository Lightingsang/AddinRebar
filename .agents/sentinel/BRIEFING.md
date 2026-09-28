# BRIEFING — 2026-09-28T00:15:00+07:00

## Mission
Implement the **Kata Rebar** feature in the `HPRebar` ecosystem, enabling automated generation of 3D concrete beam reinforcement in Revit 2026 based on structural calculation and detailing data read from sheet `Dam` of `Kata.xlsm` (via active COM or file fallback).

## 🔒 My Identity
- Archetype: sentinel
- Working directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\sentinel
- Orchestrator: 2ff0ff8b-87f5-4c14-be1c-b37017b7f55d
- Victory Auditor: 8b5245e4-bddf-4842-a8a8-6ea1fbdb24f9
- Cron 1 (Progress): task-32 (*/8 * * * *)
- Cron 2 (Liveness): task-34 (*/10 * * * *)
- Current Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\sentinel
- Current Orchestrator: 050984c1-afaa-4911-859c-331e9279dc4f
- Current Victory Auditor: [to be spawned on victory claim]
- Cron 1 (Progress): task-36 (*/8 * * * *)
- Cron 2 (Liveness): task-38 (*/10 * * * *)
- Active Victory Auditor (M5 Victory Claim): 34d8b3ad-f1ae-43c4-a109-67149a660a3e
- Active Orchestrator (Smart Plot Pro): 5a9f631f-8834-48f7-a8a5-72b226a4b480 (orchestrator_4)
- Cron 1 (Smart Plot Pro Progress): task-22 (*/8 * * * *)
- Cron 2 (Smart Plot Pro Liveness): task-24 (*/10 * * * *)
- Active Victory Auditor (Smart Plot Pro): 5cf21af9-c4da-460c-be05-2b35df56304c (victory_auditor_4)
- Active Orchestrator (HPPowerBi): 4d88b310-8910-4f85-b5a8-50216392bc6b (orchestrator_5)
- Cron 1 (HPPowerBi Progress): task-38 (*/8 * * * *)
- Cron 2 (HPPowerBi Liveness): task-40 (*/10 * * * *)
- Active Victory Auditor (HPPowerBi): [to be spawned on victory claim]
- Active Orchestrator (HPExcel): a6affb02-3586-4014-be6f-de9dfcd816bd (orchestrator_6)
- Cron 1 (HPExcel Progress): task-380 (*/8 * * * *)
- Cron 2 (HPExcel Liveness): task-382 (*/10 * * * *)
- Active Victory Auditor (HPExcel): b9d10e85-16b6-4ae7-9f68-62443376b5d5 (victory_auditor_5)
- Active Orchestrator (HPRobot): b32c5a58-8b71-46dd-ba9a-5c9e4b6709de (orchestrator_7)
- Cron 1 (HPRobot Progress): 4bd10de8-10ae-4602-8fa6-5dbd994a0bd6/task-524 (*/8 * * * *)
- Cron 2 (HPRobot Liveness): 4bd10de8-10ae-4602-8fa6-5dbd994a0bd6/task-526 (*/10 * * * *)
- Active Victory Auditor (HPRobot): [to be spawned on victory claim]
- Active Orchestrator (HPTekla): 5d7560ee-5142-428f-a172-e73cf7738ac1 (orchestrator_8)
- Cron 1 (HPTekla Progress): 9c2201a8-9827-4f5e-9938-46e09b933134/task-20 (*/8 * * * *)
- Cron 2 (HPTekla Liveness): 9c2201a8-9827-4f5e-9938-46e09b933134/task-22 (*/10 * * * *)
- Active Victory Auditor (HPTekla): [to be spawned on victory claim]
- Active Orchestrator (Archify Integration): d9313c9b-4a0e-49ad-a578-34cc518ec229 (swe_1)
- Cron 1 (Archify Progress): baf12e85-f773-4510-8171-9bd7048e4430/task-48 (*/8 * * * *)
- Cron 2 (Archify Liveness): baf12e85-f773-4510-8171-9bd7048e4430/task-50 (*/10 * * * *)
- Active Victory Auditor (Archify): [to be spawned on victory claim]
- Active Orchestrator (Kata Rebar): aa8876fc-b61d-4725-aacd-616632eb9cc0 (orchestrator_9)
- Cron 1 (Kata Rebar Progress): 68d40caa-242b-4b75-9541-008aeac8f556/task-44 (*/8 * * * *)
- Cron 2 (Kata Rebar Liveness): 68d40caa-242b-4b75-9541-008aeac8f556/task-46 (*/10 * * * *)
- Active Victory Auditor (Kata Rebar): 362ec385-332d-422f-ba0a-ae2448a72635 (victory_auditor_6)

## 🔒 Key Constraints
- No technical decisions — relay only
- Victory Audit is MANDATORY before reporting completion
- Must not write code, analyze problems, or make any technical decisions
- Keep context ultra-light
- Route via Routing Decision Table: General -> teamwork_preview_orchestrator
- Mandatory closed-loop live verification in AutoCAD 2026 via MCP before completion
- Victory Auditor spawn mandatory upon victory claim
- Independent post-victory audit is blocking for all completion claims
- Route via Routing Decision Table: SWE Light -> teamwork_preview_swe
- Route via Routing Decision Table: General -> teamwork_preview_orchestrator

## User Context
- **Last user request**: Implement the Kata Rebar feature in the HPRebar ecosystem (Revit 2026 / HPRebar.Core / WPF MVVM / Excel sheet Dam parser / Rebar generation).
- **Pending clarifications**: none
- **Delivered results**:
  * Prior deliverables complete: Beam Rebar, Foundation Rebar, HPGeoLink, Smart Plot Pro, HPPowerBi, HPExcel, HPRobot, HPTekla, Archify.
  * Kata Rebar feature: 100% complete, independently audited, VICTORY CONFIRMED.

## Routing Decision
- **Route**: General (`teamwork_preview_orchestrator`)
- **Rationale**: Multi-part software engineering project (Core parser, Core geometry calculator, Revit 3D rebar generation, WPF MVVM UI, xUnit tests) without explicit lightness signal.

## Project Status
- **Phase**: complete
- **Active Orchestrator**: orchestrator_9 (aa8876fc-b61d-4725-aacd-616632eb9cc0)
- **Active Victory Auditor**: victory_auditor_6 (362ec385-332d-422f-ba0a-ae2448a72635)

## Victory Audit Status
- **Triggered**: yes
- **Verdict**: VICTORY CONFIRMED
- **Retry count**: 0

## Artifact Index
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md — Authoritative user requirements record
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\context.md — Context passed to orchestrator_9
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\DISPATCH.md — Dispatch log for orchestrator_9
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\progress.md — Live progress of orchestrator_9
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\victory_auditor_6\DISPATCH.md — Dispatch log for victory_auditor_6
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\victory_auditor_6\report.md — Independent Victory Audit Report (VICTORY CONFIRMED)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\victory_auditor_6\handoff.md — Victory Auditor handoff report
