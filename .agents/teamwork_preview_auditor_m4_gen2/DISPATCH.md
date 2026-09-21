# DISPATCH: Forensic Auditor M4 Gen 2 (Integrity Audit)

## Assignment
- **Agent**: `teamwork_preview_auditor_m4_gen2`
- **Role**: Forensic Auditor M4 Gen 2
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_auditor_m4_gen2`
- **Parent / Orchestrator**: `5d7560ee-5142-428f-a172-e73cf7738ac1`
- **Original Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)

## Objective
Perform forensic integrity audit on the Milestone 4 remediation and full test suites:
1. Static analysis of `HPTekla/tools/harness/live-verify.py` and `HPTekla/tools/harness/run-live-verify.ps1`.
2. Verify zero tautologies, zero mock/fake bypasses in the test runners and validation scripts.
3. Verify that `live-verify.py` genuinely connects to `HPTekla.Mcp.Server.exe` over stdio and executes MCP protocol calls.
4. Verify that `HPTekla.Mcp.Server.Tests` (96 tests) and `HPTekla.McpBridge.Tests` (24 tests) are genuine and pass.
5. Verify zero cross-host references to other CAD products.
6. Determine binary verdict (`CLEAN` or `INTEGRITY VIOLATION`).
7. Write full report to `.agents/teamwork_preview_auditor_m4_gen2/report.md` and handoff to `.agents/teamwork_preview_auditor_m4_gen2/handoff.md`.
## 2026-09-21T19:16:21Z

Objective:
Perform forensic integrity audit on the Milestone 4 remediation and full test suites:
1. Static analysis of `HPTekla/tools/harness/live-verify.py` and `HPTekla/tools/harness/run-live-verify.ps1`.
2. Verify zero tautologies, zero mock/fake bypasses in the test runners and validation scripts.
3. Verify that `live-verify.py` genuinely connects to `HPTekla.Mcp.Server.exe` over stdio and executes MCP protocol calls.
4. Verify that `HPTekla.Mcp.Server.Tests` (96 tests) and `HPTekla.McpBridge.Tests` (24 tests) are genuine and pass.
5. Verify zero cross-host references to other CAD products.
6. Determine binary verdict (`CLEAN` or `INTEGRITY VIOLATION`).
7. Write full report to `.agents/teamwork_preview_auditor_m4_gen2/report.md` and handoff to `.agents/teamwork_preview_auditor_m4_gen2/handoff.md`.
8. Send a message to the orchestrator (caller) with a concise summary and path to your handoff when done.
