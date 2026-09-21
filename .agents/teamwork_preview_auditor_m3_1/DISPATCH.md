# Dispatch Assignment: Milestone 3 Forensic Integrity Auditor

## Role: Forensic Auditor (teamwork_preview_auditor)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_auditor_m3_1`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3\handoff.md`

---

## AUDIT MANDATE
Perform independent forensic verification of `HPTekla.Mcp.Server` implementation for Milestone 3:
1. Anti-Cheat & Authenticity Audit:
   - Verify that all 12 seed tools have genuine, meaningful C# code implementing Tekla Structures Open API logic.
   - Verify that NO fake, stubbed, or facade implementations exist.
   - Verify that NO test results or responses are hardcoded.
   - Verify that scripts do not bypass AST guards.
2. Architecture & Isolation Audit:
   - Verify `HPTekla.Mcp.Server.csproj` references ONLY `HPRebar.Mcp.Server.Core` and `HPRebar.Mcp.Contracts`.
   - Verify ZERO references to other host projects (`HPAutoCad`, `HPNavis`, `HPEtabs`, etc.).
   - Verify ZERO references to Tekla Open API assemblies in `HPTekla.Mcp.Server.csproj` (the server must remain host-free).
3. Tool Surface Audit:
   - Launch `HPTekla.Mcp.Server.exe` and verify that `tools/list` dynamically registers and reports exactly 24 tools.
   - Verify that all seed tools originate from embedded resources and are validated by the engine.

## Audit Verdict Requirement
Deliver a binary verdict in your `handoff.md`:
- `CLEAN` if no cheating, dummy implementations, or integrity violations exist.
- `INTEGRITY VIOLATION` if any cheating, facade, or integrity breach is discovered.
Send a message to the orchestrator upon completion.

## 2026-09-21T18:32:52Z
Objective:
Perform a forensic integrity audit on Milestone 3 (HPTekla.Mcp.Server) per your DISPATCH.md:
Verify authenticity of implementation (no hardcoded responses, no dummy facades, genuine Tekla Open API C# scripts), verify strict architectural isolation (zero host references to other CAD tools, zero Tekla references in the server project), and verify tool surface integrity (24 tools).
Write your audit findings to `.agents/teamwork_preview_auditor_m3_1/report.md` and handoff to `.agents/teamwork_preview_auditor_m3_1/handoff.md`.
Deliver a binary CLEAN or INTEGRITY VIOLATION verdict.
Send a message to the orchestrator (caller) with your verdict and summary when done.
