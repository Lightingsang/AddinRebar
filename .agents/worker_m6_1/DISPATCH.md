# Dispatch Assignment — Worker M6 (worker_m6_1)

## Context
You are Worker M6 (worker_m6_1) for Milestone M6 (Final Verification & E2E Track) in the HPExcel MCP Ecosystem project.

## Working Directory
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m6_1\`

## Authoritative Documentation to Read First
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (§ 2026-09-21T09:44:29Z)
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_6\PROJECT.md`
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_6\GATE_STATUS.md`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations and test executions must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Objectives & Scope
1. Full Solution Build Verification:
   - Run `dotnet build HPExcel/HPExcel.slnx -c Debug`
   - Run `dotnet build HPExcel/HPExcel.slnx -c Release`
   - Verify 0 errors and 0 warnings.
2. Full Automated Test Suite Execution:
   - Run `dotnet test HPExcel/HPExcel.Mcp.Server.Tests` (verify 90/90 pass)
   - Run `dotnet test HPExcel/HPExcel.McpBridge.Tests` (verify 110/110 pass)
   - Run `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests` (verify 385/385 pass)
   - Run `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests` (verify 71/71 pass)
   - Confirm total 656 tests pass with 0 failures and 0 skipped.
3. Feature Inventory & Deliverables Verification:
   - Verify all 35 features in `PROJECT.md § Feature Inventory` are satisfied.
   - Verify all 24 tools (4 core + 8 registry + 12 seeds) are discoverable and executable.
   - Verify 3-tier safety engine and snapshot manager.
   - Verify skill documentation at `.agents/skills/hp-mcp-excel/SKILL.md` and `.claude/skills/hp-mcp-excel/SKILL.md`.
   - Verify repository registration in `AGENTS.md`.
4. Artifact Publication:
   - Publish `TEST_READY.md` at `.agents/orchestrator_6/TEST_READY.md` (or project root) summarizing the full test suite and verification commands.
   - Write your complete handoff report to `.agents/worker_m6_1/handoff.md`.
5. Report completion back to parent via `send_message`.
