# Dispatch: challenger_dynamo_m2_2

**Agent**: challenger_dynamo_m2_2
**Role**: Correctness Challenger 2 (Concurrency & State Stress Verifier) for Milestone 2 (RV-02, RV-03, RV-05)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_dynamo_m2_2\`
**Parent Conversation ID**: `f448d195-c008-4b83-983a-30d0f3ab6338`

## Context & Inputs
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST READ FIRST)
2. `PROJECT.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
3. Worker M2 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2\handoff.md`

## Mission
Stress-test empirical correctness of RV-05 (Queue State Machine & Thread Safety):
1. `WorkItemTracker` State Transitions:
   - Verify atomic state transitions: `Queued` -> `Running` -> `Completed`.
   - Verify that invalid transitions (e.g., transitioning to `Running` when already `Completed` or `Cancelled`) return false.
   - Test concurrent cancellation: if token cancels while `Queued`, task cancels immediately. If token cancels while `Running`, task does NOT cancel prematurely; the worker continues until completion.
2. `DynamoBridgeCoordinator` Queue Drain:
   - Enqueue multiple items and invoke `Dispose()`.
   - Verify that all pending items in `_pending` have their `TaskCompletionSource` transitioned to exception/cancellation so callers do not hang.
   - Verify `_workTrackers` does not leak memory.
3. Test Execution:
   - Run:
     `dotnet test HPDynamo\HPDynamo.McpBridge.Tests`
     `dotnet test HPDynamo\HPDynamo.E2E.Tests`

## Output Requirements
- Deliver stress-test findings and issue verdict: **APPROVE** or **REJECT** in `handoff.md`.
- Send message to parent (`f448d195-c008-4b83-983a-30d0f3ab6338`) when complete.
