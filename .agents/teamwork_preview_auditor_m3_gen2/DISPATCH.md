# Dispatch Assignment: Milestone 3 Forensic Auditor (Iteration 2 Remediation)

## Role: Forensic Auditor (teamwork_preview_auditor)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_auditor_m3_gen2`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Worker M3 Gen 2 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3_gen2\handoff.md`

---

## AUDIT MANDATE
Perform forensic integrity verification of the remediated seed tools:
1. Anti-Cheat & Authenticity: Verify that changes made to the 5 seed tools are authentic Tekla Open API logic and not mock facades or dummy outputs.
2. Architecture Isolation: Confirm zero cross-host references.
3. Roslyn Compilation: Confirm all 12 seeds compile cleanly against installed Tekla Structures 2025.0 assemblies.
4. Tool Surface: Confirm exactly 24 tools exposed over stdio.

## Audit Verdict Requirement
Deliver a binary verdict in your `handoff.md`:
- `CLEAN` if no cheating, dummy implementations, or integrity violations exist.
- `INTEGRITY VIOLATION` if any cheating, facade, or integrity breach is discovered.
Send a message to the orchestrator upon completion.
