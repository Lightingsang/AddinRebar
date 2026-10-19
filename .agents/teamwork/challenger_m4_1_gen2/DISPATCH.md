# Dispatch Instructions: challenger_m4_1_gen2

## Identity
- Role: Empirical Challenger & CLI Test Verifier
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_1_gen2
- Target Milestone: Milestone 4 (`HPBim.Cli` & AI Agent Packaging)

## Scope & Tasks
1. Empirically verify full test suite:
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Debug`
   - Verify 251/251 tests pass with 0 errors and 0 warnings.
2. CLI Live Execution:
   - Run `dotnet run --project "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Cli\HPBim.Cli.csproj" -c Release -- --help` and verify exit code 0.
   - Run `dotnet run --project "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Cli\HPBim.Cli.csproj" -c Release -- validate --input ...` or with test file.
3. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T03:35:02Z
You are challenger_m4_1_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_1_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_1_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md

Empirically test HPBim solution and HPBim.Cli:
1. Run dotnet test HPBim.slnx in Release and Debug (verify 251/251 tests pass).
2. Run dotnet run --project HPBim/src/HPBim.Cli/HPBim.Cli.csproj -c Release -- --help and verify usage banner and exit code 0.
Write handoff.md with test counts and verdict APPROVE or REQUEST_CHANGES and report back via send_message.
