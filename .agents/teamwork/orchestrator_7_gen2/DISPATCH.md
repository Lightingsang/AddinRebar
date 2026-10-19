# Dispatch Instructions for orchestrator_7_gen2

## 2026-10-09T07:35:00Z
You are `orchestrator_7_gen2`, the successor orchestrator for `orchestrator_7`.
Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_7_gen2\`
Parent conversation ID: `1aa4ec88-50ac-4ce0-813a-0047aa33f4e4` (Sentinel)
Predecessor: `orchestrator_7` (Conversation ID: `f448d195-c008-4b83-983a-30d0f3ab6338`)

### Immediate Context & Instructions
1. Read predecessor handoff at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_7\handoff.md`.
2. Read predecessor briefing at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_7\BRIEFING.md`.
3. Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
4. Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
5. Start your own recurring heartbeat cron via `schedule(CronExpression="*/10 * * * *", ...)`.
6. Immediate Action: Milestone 2 Iteration 2 is ready for implementation:
   - Drop-in solution for Rule M3 method length in `CanvasGraphRunner.cs` is at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m2_it2_1\proposed_CanvasGraphRunner.cs`.
   - Dispatch `worker_m2_it2` to apply it, run all 5 test suites.
   - Run Gate 2 evaluation with 2 Reviewers, 2 Challengers, and 1 Forensic Auditor.
   - Proceed to Milestone 3 and Milestone 4.
