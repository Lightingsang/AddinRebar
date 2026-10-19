# Dispatch Instructions: reviewer_m3_1_gen2

## Identity
- Role: Code Reviewer & Clean Code Inspector
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_1_gen2
- Target Milestone: Milestone 3 (Revit Client & Pipe Engine)

## Scope & Assigned Files
Review all files implemented for Milestone 3:
- Project files:
  * `HPBim/src/HPBim.Client/HPBim.Client.csproj`
  * `HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`
  * `HPBim/HPBim.slnx`
- `HPBim/src/HPBim.Client/`:
  * `Connection/`: `IRevitPipeClient.cs`, `RevitPipeClient.cs`, `RevitConnectionOptions.cs`, `RevitPipeException.cs`
  * `TypeResolver/`: `TypeResolutionRules.cs`, `DynamicTypeScriptHelper.cs`
  * `ScriptGenerators/`: `IStageScriptGenerator.cs`, `StageExecutionPayload.cs`, `StageGenerationOptions.cs`, `Stage1LevelGenerator.cs` .. `Stage8RebarGenerator.cs`, `CompositeBimGenerator.cs`
  * `Pipeline/`: `IBimPipelineEngine.cs`, `BimPipelineEngine.cs`, `BimPipelineOptions.cs`, `BimPipelineResult.cs`, `StageResult.cs`
  * `Verification/`: `IRevitModelVerifier.cs`, `RevitModelVerifier.cs`, `ModelVerificationReport.cs`
- `HPBim/tests/HPBim.Client.Tests/`:
  * `Fakes/FakeRevitPipeClient.cs`
  * `Connection/RevitPipeClientTests.cs`
  * `ScriptGenerators/ScriptSyntaxTests.cs`
  * `ScriptGenerators/StageGeneratorTests.cs`
  * `Pipeline/BimPipelineEngineTests.cs`
  * `Verification/RevitModelVerifierTests.cs`

## Review Criteria
1. Clean Code Governance:
   - Methods <= 50 lines
   - Classes <= 300 lines
   - Zero empty catch blocks without clear justification comments
   - Zero dead code / commented-out code
   - Q-B1, Q-B2, Q-B3 rules
2. Dependency Boundaries:
   - Zero references to `Autodesk.Revit.*` in `HPBim.Client`
   - Zero cross-referencing between MCP folders (only `McpShared/` allowed)
   - Targets `net8.0;net10.0`
3. Functional Verification:
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`
   - Verify 190/190 tests pass with 0 errors and 0 warnings.
4. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T02:16:54Z
You are reviewer_m3_1_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_1_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_1_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read worker M3 handoff at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_gen2\handoff.md

Review HPBim.Client and HPBim.Client.Tests.
Run tests: dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release
Verify Clean Code rules (classes <= 300 lines, methods <= 50 lines, 0 empty catches, 0 dead code).
Verify 0 references to Autodesk.Revit.* in HPBim.Client and 0 cross-MCP references.
Write handoff.md with verdict APPROVE or REQUEST_CHANGES and report back via send_message.
