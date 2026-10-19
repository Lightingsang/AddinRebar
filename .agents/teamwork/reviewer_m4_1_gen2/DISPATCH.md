# Dispatch Instructions: reviewer_m4_1_gen2

## Identity
- Role: Code Reviewer & Clean Code Inspector
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_1_gen2
- Target Milestone: Milestone 4 (`HPBim.Cli` & AI Agent Packaging)

## Scope
Review all files implemented for Milestone 4:
- `HPBim/src/HPBim.Cli/`:
  * `HPBim.Cli.csproj`
  * `CommandLine/CliParser.cs`, `CommandLine/CliParseResult.cs`
  * `Formatting/ConsoleFormatter.cs`
  * `Estimation/BoqEstimator.cs`, `Estimation/BoqReport.cs`
  * `Commands/ValidateCommand.cs`, `Commands/GenerateCommand.cs`, `Commands/VerifyCommand.cs`, `Commands/EstimateCommand.cs`
  * `Program.cs`
- `HPBim/HPBim.slnx`
- `HPBim/agents/` (6 markdown files)
- `HPBim/tests/HPBim.Client.Tests/Cli/` (6 test files)

## Review Criteria
1. Clean Code Governance:
   - Check file lengths: every `.cs` file in `HPBim/src/HPBim.Cli/` must be <= 200 lines.
   - Check method lengths: all methods <= 40 lines.
   - Zero empty catches without comments, zero dead code, zero commented-out code.
2. Dependency Boundaries:
   - Zero references to `Autodesk.Revit.*` in `HPBim.Cli.csproj`.
   - Zero cross-referencing to other MCP directories (only references `HPBim.Core`, `HPBim.Client`, and `McpShared/HPRebar.Mcp.Contracts`).
3. Build & Test Verification:
   - Run: `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release /warnaserror` (0 warnings, 0 errors).
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release` (251/251 tests pass).
4. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T03:35:02Z
You are reviewer_m4_1_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_1_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_1_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read worker handoff at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_gen2\handoff.md

Review HPBim.Cli, HPBim.slnx, agents/ and tests.
Verify Clean Code: all files <= 200 lines, all methods <= 40 lines.
Verify 0 references to Autodesk.Revit.* and 0 cross-MCP dependencies.
Run build: dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release /warnaserror
Run tests: dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release
Write handoff.md with verdict APPROVE or REQUEST_CHANGES and report back via send_message.
