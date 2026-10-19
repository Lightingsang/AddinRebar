# Dispatch: worker_m2_it2 (Milestone 2 Iteration 2 Remediation Worker)

## 2026-10-09T07:42:00Z
You are `worker_m2_it2`.
Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_it2\`
Parent conversation ID: `f448d195-c008-4b83-983a-30d0f3ab6338`

### Authoritative Reference Files (READ FIRST)
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (specifically section `## 2026-10-09T05:36:26Z`)
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
3. Forensic Auditor Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_dynamo_m2\handoff.md`
4. Verified Remediation Proposal: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m2_it2_1\proposed_CanvasGraphRunner.cs`
5. Unified Diff Patch: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m2_it2_1\CanvasGraphRunner.patch`

### Exclusive Write Ownership
You exclusively own and may edit:
- `HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs`
Do NOT modify other production files.

### Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

### Task Description
1. Replace `HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs` with the verified implementation from `.agents/teamwork/explorer_dynamo_m2_it2_1/proposed_CanvasGraphRunner.cs` (or apply the patch).
2. Verify Clean Code Compliance on `CanvasGraphRunner.cs`:
   - File length: strictly $\le 300$ lines (proposed is 261 lines).
   - Method lengths: all 8 methods strictly $\le 50$ lines (longest is `RunActiveGraph` at 44 lines).
   - Zero commented-out code (Rule Q-B1).
   - Zero empty catch blocks without reason comments (Rule Q-B2).
3. Run and verify all 5 test suites:
   - `dotnet test HPDynamo/HPDynamo.McpBridge.Tests`
   - `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests`
   - `dotnet test HPDynamo/HPDynamo.E2E.Tests`
   - `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`
   - `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`
4. Confirm 100% test pass rate with 0 errors and 0 regressions.
5. Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_it2\handoff.md` and send a completion message to parent (`f448d195-c008-4b83-983a-30d0f3ab6338`).
