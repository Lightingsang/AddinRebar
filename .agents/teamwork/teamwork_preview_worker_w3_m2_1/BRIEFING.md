# BRIEFING — 2026-10-09T16:00:00Z

## Mission
Upgrade ReconcileService in HPBim.Client to implement Persist-Before-Terminal & Exact Keyset Coverage (R2) ensuring atomic ledger persistence before journal commit, complete keyset validation, and ReconcileOutcome.Unknown on any failure.

## 🔒 My Identity
- Archetype: Worker W3 M2 (implementer, qa)
- Roles: implementer, qa
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_w3_m2_1
- Original parent: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8
- Milestone: HPBim Wave 3 Remediation Milestone 2: Reconcile Persist-Before-Terminal & Exact Keyset Coverage (R2)

## 🔒 Key Constraints
- File Write Ownership:
  - HPBim/src/HPBim.Client/Recovery/ReconcileService.cs
  - Any companion partial class or helper in HPBim/src/HPBim.Client/Recovery/
  - HPBim/src/HPBim.Core/Storage/IntentJournalRecord.cs (if adding SourceKeys list)
  - HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTests.cs (or new test file ReconcilePersistBeforeTerminalTests.cs)
  - Do NOT modify any other files outside this scope!
- Clean Code Governance:
  - File <= 300 lines (Rule C2)
  - Method <= 50 lines (Rule M3)
  - 0 code comments (Rule Q-B1)
  - 0 empty catch blocks (Rule Q-B2)
  - Pure netstandard2.0 in HPBim.Client and HPBim.Core
- Integrity Mandate: No hardcoding, genuine logic, real test simulations.

## Current Parent
- Conversation ID: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8
- Updated: 2026-10-09T16:00:00Z

## Task Summary
- **What to build**: Atomic ledger persist before journal commit in ReconcileService; exact keyset coverage checking; OutcomeUnknown preservation on failure.
- **Success criteria**: 100% test pass, zero regressions, full adherence to Clean Code and ownership rules.
- **Interface contracts**: PROJECT.md, IntentJournalRecord.cs, ReconcileService.cs, BimCoordinationLedger.cs.

## Key Decisions Made
- Added `SourceKeys` to canonical `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs` (noting dispatch path clarification).
- In `ReconcileService.ApplyCommittedReceiptAsync`, implemented exact keyset bijection checking against `intent.SourceKeys` with fallback to `ItemCount` for legacy records.
- In `ReconcileService.ApplyCommittedReceiptAsync`, added `PersistLedgerBeforeCommitAsync` persisting the ledger to disk via `_ledgerService.SaveToFileAsync` before updating the journal to `Committed`.
- In `ReconcileService`, preserved `OutcomeUnknown` on any disk persistence failure, invalid key, or missing keyset coverage.
- Created `ReconcilePersistBeforeTerminalTests.cs` (230 lines) covering crash simulations, exact keyset checks, disk failure handling, and retry recovery.

## Change Tracker
- **Files modified**:
  - `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs`: Added `SourceKeys` property (60 lines).
  - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs`: Added `LedgerFilePath`, keyset coverage validation, persist-before-terminal logic (290 lines).
  - `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcilePersistBeforeTerminalTests.cs`: Comprehensive test suite for R2 (230 lines).
- **Build status**: PASS (0 warnings, 0 errors).
- **Pending issues**: None.

## Quality Status
- **Build/test result**: 100% PASS (991/991 HPBim, 1,682/1,682 HPRebar.Core, 915/915 McpShared).
- **Lint status**: clean (dotnet format passed with 0 violations).
- **Tests added/modified**: 6 new integration tests in `ReconcilePersistBeforeTerminalTests.cs`.
