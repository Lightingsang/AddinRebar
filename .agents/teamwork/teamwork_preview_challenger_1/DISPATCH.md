# Task Assignment: Challenger 1 (MCP Tools & Excel UDF Stress Testing)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_1\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Specification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Clean Code Standards: `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Objectives
Empirically challenge and stress-test the implementation:
1. **Stress-test MCP Tools (`HPDcePro.Mcp`)**:
   - Test extreme/boundary inputs across all 8 tools:
     - Zero/negative geometry ($b \le 0, h \le 0, L \le 0$).
     - Enormous forces ($N = 10^7$ kN, $M = 10^7$ kNm, $V = 10^7$ kN).
     - Missing optional fields vs required fields.
     - Malformed JSON arguments and invalid operation names.
     - Rapid sequential and concurrent tool dispatching.
   - Verify that the executor returns structured error `DceProToolResult` instead of throwing unhandled crashes.
2. **Stress-test Excel UDF Bridges (`HPDcePro.Excel`)**:
   - Test all 12 functions in `CalculationUdfBridge`:
     - Physical singularity points (zero thickness, zero depth, negative soil parameters).
     - Tension/uplift conditions in pile and footing calculations.
     - Large moment combinations in PEB moment connection and base plate bolts.
   - Verify zero unhandled exceptions (UDFs must return fallback numbers or 0.0 safely).
3. **Execution & Evidence**:
   - Write or execute ephemeral test runners/scripts as needed to stress-test the compiled assemblies.
   - Run `dotnet test HPDcePro/HPDcePro.slnx -c Debug`.

Deliver verdict: **APPROVE** (no vulnerabilities/bugs found) or **REQUEST_CHANGES** (with reproducible failure case) in `handoff.md`.
Notify orchestrator via send_message.

## 2026-10-18T01:18:06Z
You are Challenger 1 for HPDcePro Wave 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_1\
Read your task assignment at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_1\DISPATCH.md
MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-18T00:11:55Z).
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md
Read Clean Code rules at: docs/clean-code/HP_CLEAN_CODE_CORE.md and AGENTS.md.

Empirically challenge and stress-test the 8 MCP tools in HPDcePro.Mcp and all 12 Excel UDFs in CalculationUdfBridge:
- Extreme boundary inputs (zero/negative values, massive forces 10^7 kN, invalid geometries).
- Null/empty tool calls, malformed JSON arguments, unknown operation names.
- Singularity checks and soil uplift conditions.
- Verify tests pass: `dotnet test HPDcePro/HPDcePro.slnx -c Debug`.

Deliver verdict: APPROVE (no vulnerabilities) or REQUEST_CHANGES (with repro) in handoff.md and notify orchestrator via send_message.
