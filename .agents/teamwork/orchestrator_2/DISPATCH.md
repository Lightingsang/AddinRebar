# Dispatch: orchestrator_2 (Generation 2 Project Orchestrator)

## Mission
Resume orchestration for the HPBim deliverable (P0 and P1 BIM Pipeline & Coordination Ledger) at `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim`.

## Identity & Hierarchy
- Your identity: `orchestrator_2` (teamwork_preview_orchestrator, Generation 2).
- Your working directory: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_2\`.
- Your parent is: `341a3e3f-2ef0-4505-9dcc-0a822930f000` (Use this conversation ID for all status reporting and communication via `send_message`).

## Predecessor Handoff & State Documents (READ FIRST):
- Handoff from Generation 1: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_1\handoff.md`
- Master Plan: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Authoritative User Request: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
- Gate Status: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_1\GATE_STATUS.md`
- Progress Log: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_1\progress.md`
- Worker Remediation Handoff: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it2_r1\handoff.md`

## Next Immediate Steps:
1. Initialize your `BRIEFING.md` and `progress.md` in your working directory.
2. Start your recurring heartbeat cron via `schedule(CronExpression="*/10 * * * *")`.
3. Execute Milestone M1 Gate Verification (Iteration 2):
   - Dispatch 2 Reviewers, 2 Challengers, and 1 Forensic Auditor (`teamwork_preview_auditor`) to verify `worker_m1_it2_r1`'s deliverables.
   - If Gate passes (APPROVE, CLEAN) -> Mark Milestone M1 as DONE in `PROJECT.md`.
4. Proceed to Milestone M2 (P0 Verification & Measured Geometric Audit):
   - Dry-Run 100% rollback confirmation enforcement.
   - Warning capture in `AutoDismissFailurePreprocessor` -> `ExecuteResult.Warnings`.
   - Remove hardcoded Step 8 literal in `FoundationQcEngine`.
   - Implement `MeasuredGeometricVerifier` querying Revit API solid/curve/parameters against `CoordinationTolerances`.
5. Proceed to Milestones M3, M4, M5, and Final.
