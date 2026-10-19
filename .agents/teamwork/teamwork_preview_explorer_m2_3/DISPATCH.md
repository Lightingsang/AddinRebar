# Dispatch: Explorer M2.3 (Registry Meta Tools & 24 Tools Surface)

## Mission
Analyze exact setup for registry tools and the strict 24-tool surface:
- Verify that 8 registry meta tools are wired: `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
- Verify how `inspect_type` is suppressed or filtered from `McpServerOptions.ToolCollection` so that tool count is exactly 24 (4 Core + 8 Registry + 12 Seeds).
- Validate that `tools/list` on the server returns the tools matching the E2E Tier 1 tests (`TC_T1_SRV_01..08` in `Tier1_ContractsAndServerTests.cs`).
- Provide concrete code and validation commands.

## References
- `ORIGINAL_REQUEST.md` (mandatory read)
- `.agents/teamwork/orchestrator_4/PROJECT.md`
- `TEST_READY.md`

## Output
Write your report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_3\handoff.md`
Then send a message back to orchestrator_4.

## 2026-10-08T08:58:41Z
You are teamwork_preview_explorer_m2_3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_3\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_3\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md
Read TEST_READY.md and HPDynamo.E2E.Tests.

Investigate and formulate the exact registry tools integration and 24-tool surface enforcement:
1. Wiring 8 Registry Meta Tools (search_tools, get_tool, run_tool, get_run, propose_tool, test_tool, publish_tool, manage_tool).
2. Suppressing or filtering inspect_type from ToolCollection to enforce exactly 24 tools (4 Core + 8 Registry + 12 Published Seeds).
3. Alignment with Tier 1 E2E tests (TC_T1_SRV_01..08).
Provide line-by-line implementation guidance in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_3\handoff.md and send a message back to orchestrator_4.
