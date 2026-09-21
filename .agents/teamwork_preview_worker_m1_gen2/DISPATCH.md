# Dispatch for Worker Milestone 1 (Iteration 2 Fix): McpShared Additive Integration

## 2026-09-21T17:44:00Z

- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m1_gen2
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (header ## 2026-09-21T17:20:33Z)
- Challenger 1 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m1_1\handoff.md
- Challenger 2 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m1_2\handoff.md
- Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

File Ownership:
You have exclusive write access to:
- `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`
- `McpShared/HPRebar.McpBridge.Core.Net48Tests/`
- `McpShared/HPRebar.Mcp.Server.Core.Tests/`

Objective:
Remediate the vulnerability identified by Challenger 1 and Challenger 2:
1. In `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`:
   Update `GuardProfile.Tekla`:
   Add `"CommitChanges"` and `"PickFace"` to `deniedMembers`:
   ```csharp
   deniedMembers: new[]
   {
       // Interactive UI picking methods that block waiting for mouse clicks
       "PickObject", "PickObjects", "PickPoint", "PickPoints", "PickLine", "PickPolygon", "PickFace",
       // Direct commit bypass on any receiver (bridge owns transaction commit and dryRun enforcement)
       "CommitChanges",
       // Application shutdown
       "Exit", "Quit",
   },
   ```
2. Build & Test:
   - Run `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`
   - Run `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`
   Verify that all tests in `TeklaMilestone1Challenger2Net48Tests.cs` and all other tests pass 100% with 0 failures and 0 skipped.
3. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m1_gen2\report.md` and handoff to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m1_gen2\handoff.md`.
