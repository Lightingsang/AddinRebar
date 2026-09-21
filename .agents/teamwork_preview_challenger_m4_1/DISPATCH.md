# Dispatch Assignment: Milestone 4 Challenger 1 — Adversarial Test Suite Stress Testing

## Role: Challenger (teamwork_preview_challenger)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m4_1`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Worker M4 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4\handoff.md`

---

## Challenge Scope
Adversarially challenge `HPTekla.Mcp.Server.Tests`:
1. Execute `HPTekla.Mcp.Server.Tests.exe` directly under multiple runs to verify zero test flakiness.
2. Verify that `TeklaHostProfileTests` strictly checks for Tekla naming invariants and rejects cross-host contamination.
3. Verify that `SeedCatalogTests` strictly rejects undeclared arguments and AST violations.
4. Verify that `SeedCompilationTests` actually resolves assemblies from `C:\Program Files\Tekla Structures\2025.0\bin` and does NOT silently skip on this machine.
5. Check if `SeedExecutionTests` properly handles simulated cancellations and timeouts up to 600s.

## Verdict Requirement
Deliver an unambiguous verdict in your `handoff.md`:
- `APPROVE` if all test suites are robust, deterministic, and non-flaky.
- `REQUEST_CHANGES` if flakiness, false passes, or loopholes exist.
Send a message to the orchestrator upon completion.

## 2026-09-21T19:05:00Z
User Request:
Objective:
Adversarially challenge HPTekla.Mcp.Server.Tests per DISPATCH.md:
Run tests multiple times to verify non-flakiness, check edge cases in profile tests, catalog tests, seed compilation tests, and execution tests.
Write report to `.agents/teamwork_preview_challenger_m4_1/report.md` and handoff to `.agents/teamwork_preview_challenger_m4_1/handoff.md`.
Deliver an unambiguous APPROVE or REQUEST_CHANGES verdict.
Send a message to the orchestrator (caller) when done.
