# Dispatch Instructions: reviewer_m3_2_gen2

## Identity
- Role: Adversarial Code Reviewer & Edge Case Inspector
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_2_gen2
- Target Milestone: Milestone 3 (Revit Client & Pipe Engine)

## Scope & Assigned Files
Adversarially review all files implemented for Milestone 3:
- Project files:
  * `HPBim/src/HPBim.Client/HPBim.Client.csproj`
  * `HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`
  * `HPBim/HPBim.slnx`
- Codebase in `HPBim/src/HPBim.Client/` and tests in `HPBim/tests/HPBim.Client.Tests/`.

## Review Criteria & Stress Inspection
1. Concurrency & Pipe Safety:
   - Does `RevitPipeClient` properly guard against concurrent writes using `SemaphoreSlim`?
   - Does disconnection / cancellation cleanly cancel pending tasks and close streams?
   - Are timeouts correctly handled?
2. Script Generation & Roslyn Safety:
   - Does `CompositeBimGenerator` correctly wrap all stages in an outer `TransactionGroup`?
   - Are dynamic family/type resolution methods safe against nulls or missing types?
   - Are element IDs, UV coordinates, and level elevations accurately passed?
3. Clean Code Governance:
   - Methods <= 50 lines
   - Classes <= 300 lines
   - Zero empty catch blocks without comments
   - Zero dead code
4. Dependency Boundaries:
   - Zero references to `Autodesk.Revit.*` in `HPBim.Client`
   - Zero cross-MCP references
5. Functional Verification:
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`
   - Verify 190/190 tests pass with 0 errors and 0 warnings.
6. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T02:16:54Z
You are reviewer_m3_2_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_2_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_2_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read worker M3 handoff at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_gen2\handoff.md

Adversarially review HPBim.Client and HPBim.Client.Tests, focusing on pipe concurrency, Roslyn script safety, and dryRun rollback.
Run tests: dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release
Verify Clean Code rules (classes <= 300 lines, methods <= 50 lines, 0 empty catches, 0 dead code).
Verify 0 references to Autodesk.Revit.* and 0 cross-MCP references.
Write handoff.md with verdict APPROVE or REQUEST_CHANGES and report back via send_message.
