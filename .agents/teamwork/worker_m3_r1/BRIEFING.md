# BRIEFING — 2026-10-08T03:52:00Z

## Mission
Implement Milestone M3: Document Identity Snapshot & Pre-Flight Gate (F7), Composite Source Key & Provenance Tracking (F8 Part 1), and Coordination Ledger & Idempotent Element Resolution (F8 Part 2) with zero regressions and strict Clean Code governance.

## 🔒 My Identity
- Archetype: teamwork_preview_worker
- Roles: implementer, tester
- Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_r1\
- Original parent: 44938268-52fa-4afa-af1c-06dc2c4f2512
- Milestone: M3 (Features F7 & F8)

## 🔒 Key Constraints
- Rule C2: EVERY file touched or created MUST be strictly <= 300 lines.
- Rule M3: EVERY method in touched or created files MUST be strictly <= 50 lines (target <= 35 lines).
- No empty catch blocks without explanatory comments.
- No commented-out code.
- Zero regressions across existing tests (521 HPBim tests + McpShared + HPRebar tests).
- Clean Code limits verified via automated AST/regex method measurement script.
- Maintain genuine implementations — no dummy/facade implementations or hardcoded results.

## Current Parent
- Conversation ID: 44938268-52fa-4afa-af1c-06dc2c4f2512
- Updated: 2026-10-08T03:52:00Z

## Task Summary
- **What to build**:
  - Feature F7: DocumentIdentitySnapshot, on-thread Pre-Flight Gate in BimPlanRunnerScriptTemplate, DocumentIdentityQuery in HPBim.Client, PreFlightGateTests.
  - Feature F8 Part 1: CompositeSourceKey with OrdinalIgnoreCase, Parse/TryParse, canonical formatting, ElementPlanItem TargetUniqueId & ProvenanceHandles, CompositeSourceKeyTests (19 cases).
  - Feature F8 Part 2: LedgerElementMapping, BimCoordinationLedger dictionary & lookups, BimCoordinationLedgerService.Mappings.cs partial class, IdempotentElementResolver, BimPlanRunnerScriptTemplate.*.cs element resolution, BimPipelineEngine synchronization into ledger, BimCoordinationLedgerIdempotencyTests, IdempotentExecutionTests.
- **Success criteria**: 100% build pass, all existing and new tests pass (0 regressions), all files <= 300 lines, all methods <= 50 lines.
- **Interface contracts**: PROJECT.md § Interface Contracts
- **Code layout**: PROJECT.md § Code Layout

## Key Decisions Made
- Use partial class `BimCoordinationLedgerService.Mappings.cs` to ensure `BimCoordinationLedgerService.cs` remains well below 300 lines (currently at 290 lines).
- In `CompositeSourceKey`, compare `DocIdentifier` and `PrimaryHandle` with `StringComparer.OrdinalIgnoreCase`, exclude `ProvenanceHandles` from equality, support canonical and plain formats, strip brackets from handles, and allow empty doc when `allowEmptyDoc: true` for legacy test compatibility.
- Ensure Pre-Flight Gate asserts both `Title` (OrdinalIgnoreCase) and `VersionGUID` when expected values are non-empty.
- Refactor test helper methods in `BimCoordinationLedgerIdempotencyTests` and `IdempotentExecutionTests` to strictly ensure all methods are <= 39 lines (target <= 35 lines, limit <= 50 lines).

## Artifact Index
- `.agents/teamwork/worker_m3_r1/DISPATCH.md` — Assignment instructions
- `.agents/teamwork/worker_m3_r1/BRIEFING.md` — Situational awareness and working memory
- `.agents/teamwork/worker_m3_r1/progress.md` — Liveness heartbeat and progress tracking
- `.agents/teamwork/worker_m3_r1/report.md` — Final implementation report
- `.agents/teamwork/worker_m3_r1/handoff.md` — Completion handoff report

## Change Tracker
- **Files modified/created**: 19 files across HPBim.Core, HPBim.Client, HPBim.Core.Tests, HPBim.Client.Tests
- **Build status**: PASS (0 warnings, 0 errors)
- **Pending issues**: none

## Quality Status
- **Build/test result**: 568 passed in HPBim; 793 in McpShared; 1648 in HPRebar. Total: 3,009 passed, 0 failures, 0 regressions.
- **Lint status**: 0 violations, all files <= 300 lines, all methods <= 50 lines (max: 47 lines, new methods max: 39 lines).
- **Tests added/modified**: +37 new tests across 4 test suites.

## Loaded Skills
- None explicitly loaded.
