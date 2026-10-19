# DISPATCH — teamwork_preview_worker_m5_1

## Task Assignment
**Role**: Test Suites & Server Verification Implementation Worker (teamwork_preview_worker)
**Mission**: Implement Milestone 5 — full unit and integration test suites for HPDynamo subsystem (`HPDynamo.Mcp.Server.Tests` and `HPDynamo.McpBridge.Tests`), wire them into `HPDynamo.slnx`, and verify build and test execution.

## Required Reading
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. Project Specifications: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md`
3. Explorer Reports for Milestone 5:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_1\handoff.md` (Server Tests Blueprint)
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_2\handoff.md` (Bridge Tests Blueprint)
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_3\handoff.md` (Solution Integration Blueprint)

## File Boundaries & Write Ownership
You exclusively own:
- `HPDynamo/HPDynamo.slnx`
- `HPDynamo/Directory.Build.props`
- `HPDynamo/HPDynamo.McpBridge/HPDynamo.McpBridge.csproj`
- `HPDynamo/HPDynamo.Mcp.Server.Tests/**`
- `HPDynamo/HPDynamo.McpBridge.Tests/**`
- `HPDynamo/run-all-dynamo-tests.ps1`

## Implementation Scope
1. **Solution & Props Configuration**:
   - In `HPDynamo/Directory.Build.props`, add `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`.
   - In `HPDynamo/HPDynamo.McpBridge/HPDynamo.McpBridge.csproj`, add `<InternalsVisibleTo Include="HPDynamo.McpBridge.Tests" />`.
   - In `HPDynamo/HPDynamo.slnx`, add:
     `<Project Path="HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj" />`
     `<Project Path="HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj" />`
2. **HPDynamo.Mcp.Server.Tests**:
   - Create `HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj` (`net10.0`, `OutputType=Exe`, `UseMicrosoftTestingPlatformRunner=true`).
   - Link `FakeRevitExecutor.cs` from `..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs`.
   - Implement test suites:
     - `ToolSurfaceTests.cs`: 24-tool invariant, 4 core tools, 8 meta tools, 12 published seeds, inspect_type suppression.
     - `SeedManifestTests.cs`: 36 manifest resources under `SeedLibrary/`, 0 compiled seed classes, schema and example validation.
     - `ScriptQualityTests.cs`: ScriptQuality.Find validation across all 12 seeds (0 errors, 0 warnings, rules C2, M3, Q-B1, Q-B2).
     - `ScriptGuardTests.cs`: ScriptGuard.Check against GuardProfile.Dynamo (0 security violations).
     - `RegistryLifecycleTests.cs`: DynamicToolRegistrar propose, test, publish, manage, search, and retrieval.
3. **HPDynamo.McpBridge.Tests**:
   - Create `HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj` (`net10.0-windows7.0`, `OutputType=Exe`, `UseMicrosoftTestingPlatformRunner=true`, `<UseWPF>true</UseWPF>`, `<Private>false</Private>` on host refs).
   - Implement fakes in `Fakes/`: `FakeMcpBridgeRunner.cs`, `SimulatedTransactionContext.cs`.
   - Implement `TestInitializer.cs` (`[ModuleInitializer]` calling `DynamoAssemblyResolver.Install()`).
   - Implement test suites:
     - `CoordinatorTests.cs`: Headless fallback, live canvas attach/detach, ContextResult.Dynamo, concurrency CAS guard, cancellation.
     - `TransactionScopeTests.cs`: RevitTransactionScope rollback on dryRun=true, assimilation on dryRun=false, exception rollback.
     - `OptInSecurityTests.cs`: Code -32001 fast rejection when ExecutionEnabled=false, safe tools bypass, named pipe wire verification.
     - `DesignScriptRunnerTests.cs`: DesignScript argument parsing, parameter formatting, ephemeral CodeBlock cleanup.
     - `ViewModelBindingTests.cs`: DynamoStatusViewModel master toggle, theme sync, listener commands, counters.
4. **Clean Code Governance**:
   - All classes/scripts <= 300 lines (Rule C2).
   - All methods/blocks <= 50 lines (Rule M3).
   - 0 empty catch blocks (Rule Q-B1).
   - 0 commented-out code (Rule Q-B2).
   - Zero host DLLs copied into test bin folders.
   - Do NOT pass `--nologo` to MTP runners.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Verification Commands
Execute and verify:
1. `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug`
2. `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"`
3. `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"`
4. `powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier All`
5. `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"`
(Do NOT pass `--nologo`)

Deliver your complete handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m5_1\handoff.md`
When finished, send a message back to orchestrator_4.
