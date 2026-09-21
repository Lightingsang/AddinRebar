# Dispatch Assignment: Milestone 4 Forensic Integrity Auditor

## Role: Forensic Auditor (teamwork_preview_auditor)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_auditor_m4_1`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Worker M4 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4\handoff.md`

---

## AUDIT MANDATE
Perform an independent forensic integrity audit on Milestone 4 (Automated Test Suites & Live Verification Harness):
1. Anti-Cheat & Authenticity:
   - Verify that test assertions in `HPTekla.Mcp.Server.Tests` are genuine and strict (no tautologies like `Assert.True(true)`, no empty test methods, no skipped tests without reason).
   - Check `SeedCompilationTests`: verify that it genuinely invokes the Roslyn compiler against official Trimble Tekla Assemblies in `C:\Program Files\Tekla Structures\2025.0\bin`, and assert that none of the 12 seeds were skipped or failed.
2. Architecture Isolation:
   - Check `HPTekla.Mcp.Server.Tests.csproj`: verify project references point strictly to `HPTekla.Mcp.Server` and `McpShared`. Zero cross-references to other CAD test projects.
3. Regression Verification:
   - Run tests across `McpShared` (`HPRebar.Mcp.Server.Core.Tests` and `HPRebar.McpBridge.Core.Net48Tests`) and `HPTekla.McpBridge.Tests` to verify 100% pass rate with zero regressions.

## Audit Verdict Requirement
Deliver a binary verdict in your `handoff.md`:
- `CLEAN` if no cheating, dummy implementations, or integrity violations exist.
- `INTEGRITY VIOLATION` if any cheating, facade, or integrity breach is discovered.
Send a message to the orchestrator upon completion.
