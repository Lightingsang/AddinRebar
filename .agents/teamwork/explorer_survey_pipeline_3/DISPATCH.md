## 2026-10-07T13:55:14Z
You are explorer_survey_pipeline_3, a teamwork_preview_spec_miner agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_pipeline_3\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this file first, especially the latest entry ## 2026-10-07T13:49:28Z).

Your mission is read-only specification and test baseline exploration:
1. Existing Test Baseline: Survey all 370 tests across HPBim (268 Core + 102 Client). Document test project files, test runners, frameworks (xUnit, NUnit, MTP), execution patterns, and coverage.
2. Required Acceptance Criteria Tests: Map detailed test case specifications for all required acceptance criteria:
   - Schema Validation test suite: Rejecting raw CAD JSON and invalid schemas, no empty model bypass.
   - Single Execution Plan test suite: Preview and Apply using the exact same plan IR.
   - Dry-Run verification test suite: FAIL on execution error or unconfirmed rollback.
   - Idempotency test suite: Run 1 creates; Run 2 NoOp/Update without duplicating elements.
   - Reconcile & Recovery test suite: Disconnect/timeout simulation, receipt recovery in Extensible Storage.
   - Geometric Verification test suite: Tolerance violations mark Defect/Failed even if counts match.
3. Clean Code Governance: Check docs/clean-code/HP_CLEAN_CODE_CORE.md and host appendix. Document class/method length limits, dependency rules (HPBim -> McpShared only, no circular dependencies), error handling rules.
4. R3 (P2 Architectural Proposal): Outline the requirements and structure for the Design Proposal covering two-way change tracking, background queue, and large-scale batching.

DO NOT write or modify source code files.
Write a detailed report to G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_pipeline_3\report.md and your completion handoff to G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_pipeline_3\handoff.md.
When finished, notify the orchestrator with send_message.
