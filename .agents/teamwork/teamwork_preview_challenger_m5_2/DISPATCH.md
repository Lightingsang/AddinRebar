# DISPATCH — teamwork_preview_challenger_m5_2

## Task Assignment
**Role**: Bridge Tests & Headless Execution Challenger for Milestone 5 (teamwork_preview_challenger)
**Mission**: Empirically challenge `HPDynamo.McpBridge.Tests`, coordinator routing, transaction rollback guarantees, opt-in rejection, and DesignScript parsing.

## Required Reading
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. Project Specifications: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md`
3. Worker Handoff Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m5_1\handoff.md`

## Challenge Scope
1. **Coordinator Routing & Headless Seams Challenge**:
   - Challenge `CoordinatorTests`: verify live canvas attach/detach, headless `ContextResult.Dynamo` structure, concurrency CAS guard, and cancellation.
2. **Transaction Scope Rollback Challenge**:
   - Challenge `TransactionScopeTests`: verify pre-flight document checks, `dryRun=true` rollback, `dryRun=false` assimilation, and exception rollback guarantees.
3. **Opt-In Security Gate Challenge**:
   - Challenge `OptInSecurityTests`: verify error code `-32001` fast rejection when `ExecutionEnabled=false`, safe tools bypass (`ping`, `context`), and wire protocol behavior.
4. **DesignScript & ViewModel Challenge**:
   - Challenge `DesignScriptRunnerTests` and `ViewModelBindingTests`: verify DesignScript argument parsing, formatting, master toggle sync, and listener commands.
5. **Run Verification Commands**:
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"`
   - `powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier Tier2`
   - `powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier Tier3`
   *(Do NOT pass `--nologo`)*

## Deliverable
Write your comprehensive challenge report with an explicit verdict (**APPROVE** or **REQUEST_CHANGES**) to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_m5_2\handoff.md`
When complete, send a message back to orchestrator_4.
