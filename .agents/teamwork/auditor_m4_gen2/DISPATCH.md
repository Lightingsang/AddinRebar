# Dispatch Instructions: auditor_m4_gen2

## Identity
- Role: Forensic Integrity Auditor
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_gen2
- Target Milestone: Milestone 4 (`HPBim.Cli` & AI Agent Packaging)

## Forensic Integrity Tasks
1. Authenticity of Implementations:
   - Verify that `HPBim.Cli` is a genuine, runnable executable project targeting `net10.0`.
   - Verify that CLI command handlers genuinely invoke `QcEngine`, `BimPipelineEngine`, `RevitModelVerifier`, and `BoqEstimator`.
   - Verify that `BoqEstimator` genuinely calculates concrete volume, formwork area, and rebar weight using mathematical geometry rather than hardcoded constant mocks.
   - Verify that all 6 AI agent roles in `HPBim/agents/` are genuine, detailed prompts adhering to Vietnamese construction standards.
2. Clean Code Rules (HP_CLEAN_CODE_CORE.md):
   - Measure lines of every `.cs` file in `HPBim/src/HPBim.Cli/`: verify all files <= 200 lines (and <= 300 hard limit).
   - Measure method lengths: all methods <= 40 lines (and <= 50 hard limit).
   - Verify 0 dead code, 0 unannotated catches, 0 commented-out code (Q-B1, Q-B2, Q-B3).
3. Dependency Boundaries:
   - Check `HPBim/src/HPBim.Cli/HPBim.Cli.csproj`: Zero references to `Autodesk.Revit.*`.
   - Verify zero cross-MCP dependencies.
4. Test Verification:
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release` (verify 251/251 tests pass).
5. Deliverable:
   - Write `handoff.md` with explicit binary verdict: `CLEAN` or `INTEGRITY VIOLATION`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T03:35:02Z
Perform full forensic integrity audit on Milestone 4:
1. Verify genuine HPBim.Cli executable project and command handlers, zero facades, zero mocks posing as production code.
2. Verify all files in HPBim.Cli <= 200 lines, all methods <= 40 lines.
3. Verify zero references to Autodesk.Revit.* and zero cross-MCP dependencies.
4. Verify genuine 6 AI Agent Roles in HPBim/agents/.
5. Verify dotnet test passes 251/251 tests in Release mode.
Write handoff.md with binary verdict CLEAN or INTEGRITY VIOLATION and report back via send_message.
