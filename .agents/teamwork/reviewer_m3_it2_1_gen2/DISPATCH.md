# Dispatch Instructions: reviewer_m3_it2_1_gen2

## Identity
- Role: Code Reviewer & Clean Code Inspector
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_it2_1_gen2
- Target Milestone: Milestone 3 Iteration 2 (Remediation Review)

## Scope
Review all files modified or added in Milestone 3 Iteration 2:
- `HPBim/src/HPBim.Client/Connection/PipeMessageReader.cs`
- `HPBim/src/HPBim.Client/Connection/RevitPipeClient.cs`
- `HPBim/src/HPBim.Client/Verification/RevitModelVerifier.cs`
- `HPBim/src/HPBim.Client/ScriptGenerators/Stage5OpeningGenerator.cs`
- `HPBim/src/HPBim.Client/ScriptGenerators/CompositeBimGenerator.cs`
- `HPBim/src/HPBim.Client/ScriptGenerators/CompositeBimGenerator.Stages1To3.cs`
- `HPBim/src/HPBim.Client/ScriptGenerators/CompositeBimGenerator.Stages4To6.cs`
- `HPBim/src/HPBim.Client/ScriptGenerators/CompositeBimGenerator.Stages7To8.cs`
- `HPBim/tests/HPBim.Client.Tests/TypeResolver/TypeResolutionRulesTests.cs`
- `HPBim/tests/HPBim.Client.Tests/ScriptGenerators/StageGeneratorTests.cs`
- `HPBim/tests/HPBim.Client.Tests/Connection/RevitPipeClientAdversarialTests.cs`

## Review Criteria
1. Clean Code Governance:
   - Check every `.cs` file in `HPBim/src/HPBim.Client/`: every file must be <= 220 lines (and <= 300 hard limit).
   - Check method lengths: all methods <= 40 lines (and <= 50 hard limit). Specifically verify `CompareCounts` <= 15 lines.
   - Zero dead code: confirm `GetDoorSymbolName` and `GetWindowSymbolName` are utilized.
   - Zero empty catches, zero commented-out code.
2. Build & Test Verification:
   - Run: `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release /warnaserror` (0 warnings, 0 errors).
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release` (216/216 pass).
3. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T02:54:39Z
You are reviewer_m3_it2_1_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_it2_1_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_it2_1_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read worker handoff at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_fix_gen2\handoff.md

Review all remediated files in HPBim.Client and tests.
Check Clean Code: all files <= 220 lines, all methods <= 40 lines, CompareCounts <= 15 lines, 0 dead code.
Run build: dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release /warnaserror
Run tests: dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release
Write handoff.md with verdict APPROVE or REQUEST_CHANGES and report back via send_message.
