# Milestone Report: Wave 3 Milestone 2 (R2) — Reconcile Persist-Before-Terminal & Exact Keyset Coverage

**Worker**: Worker W3 M2 (`teamwork_preview_worker_w3_m2_1`)  
**Parent ID**: `8b5286ec-5528-473c-b9fe-c3867bdf1bf8`  
**Date**: 2026-10-09  
**Status**: COMPLETE (100% PASS)

---

## 1. Executive Summary

Milestone 2 (R2) resolves the critical vulnerabilities in `ReconcileService` identified in the Wave 3 evaluation:
1. **Persist-Before-Terminal Violation**: Previously, `ReconcileService.ApplyCommittedReceiptAsync` updated in-memory ledger mappings and directly marked the `IntentJournal` as `Committed` on disk *before* persisting the ledger to disk. If a crash occurred immediately thereafter, the on-disk ledger was never updated, and a cold restart would lack the mappings and generate duplicate elements in Revit.
2. **Weak Keyset Check**: Previously, `ReconcileService` only compared scalar `ItemCount`, allowing receipts with missing keys or foreign keys to pass validation.
3. **Failure Insecurity**: Disk persistence failures were not handled, risking false completion states.

We upgraded `ReconcileService` and `IntentJournalRecord` to enforce:
- Exact keyset bijection coverage validation against `intent.SourceKeys` (with backward compatibility fallback to `ItemCount`).
- Atomic ledger disk persistence (`.tmp` -> atomic replace with lock via `_ledgerService.SaveToFileAsync`) strictly **BEFORE** transitioning `IntentJournal` status to `Committed`.
- Strict preservation of `OutcomeUnknown` whenever ledger persistence fails, keyset coverage is incomplete, or mappings are corrupt, allowing subsequent safe reconciliation.

---

## 2. Changes Made

### 2.1 `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs` (60 lines)
- Added `public IReadOnlyList<string> SourceKeys { get; init; } = Array.Empty<string>();` to record target composite source keys for exact keyset verification.
- Added `using System.Collections.Generic;`.
- *Note on path*: Dispatch listed `HPBim/src/HPBim.Core/Storage/IntentJournalRecord.cs (if adding SourceKeys list)`. In the codebase, `IntentJournalRecord.cs` is located at `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs` (and `HPBim.Core` contains no `Storage` folder). The canonical class was updated without breaking any references.

### 2.2 `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` (290 lines)
- Added `public string? LedgerFilePath { get; set; }` and overloaded constructors supporting optional `ledgerFilePath`.
- Upgraded `ApplyCommittedReceiptAsync`:
  - Validates `receipt.ElementMap != null`.
  - Invokes `ValidateKeysetCoverage`: validates exact keyset coverage (`expectedKeys.SetEquals(actualKeys)`) when `intent.SourceKeys` is populated; falls back to `ItemCount` for legacy records. Returns detailed diagnostic reason on failure.
  - Validates `CompositeSourceKey.TryParse` and non-empty `uniqueId`.
  - Updates in-memory mappings via `_ledgerService.UpdateMapping`.
  - Invokes `PersistLedgerBeforeCommitAsync`: when `LedgerFilePath` is configured, calls `await _ledgerService.SaveToFileAsync(LedgerFilePath, ct)`. On exception, preserves `OutcomeUnknown` in journal and returns `ReconcileResult.StillUnknown`.
  - Only after successful ledger save transitions `IntentJournal` status to `Committed`.
- Modularized into helper methods (`ValidateKeysetCoverage`, `PersistLedgerBeforeCommitAsync`), keeping every method <= 46 lines (Rule M3 <= 50 lines) and the whole file at 290 lines (Rule C2 <= 300 lines).

### 2.3 `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcilePersistBeforeTerminalTests.cs` (230 lines)
Created 6 new targeted integration tests:
1. `Reconcile_WhenLedgerDiskSaveFails_PreservesOutcomeUnknown_JournalNotMarkedCommitted`: Proves that when disk persistence fails (unwritable directory/path), `ReconcileResult.StillUnknown` is returned, journal is updated to `OutcomeUnknown` with diagnostic reason, and journal is NOT marked `Committed`.
2. `Reconcile_SuccessfulReconcile_SavesLedgerToDiskBeforeMarkingJournalCommitted`: Uses a spy journal to verify that at the exact moment `UpdateStatusAsync(..., Committed)` is invoked, the ledger file on disk already exists and contains the updated mappings.
3. `Reconcile_ExactKeysetCoverage_MissingSingleSourceKey_PreservesOutcomeUnknown`: Proves that missing even a single expected `SourceKey` results in `StillUnknown` and preserves `OutcomeUnknown` in the journal.
4. `Reconcile_ExactKeysetCoverage_ForeignSourceKeyPresent_PreservesOutcomeUnknown`: Proves that a receipt with matching element count but a foreign key is rejected with `StillUnknown`.
5. `Reconcile_ExactKeysetCoverage_CompleteKeysetMatch_SuccessfullyPersistsAndCommits`: Proves that exact keyset match succeeds, persists ledger to disk, and transitions journal to `Committed`.
6. `Reconcile_SimulateCrashThenRetry_RecoversAndPersistsSuccessfully`: Proves that simulating a disk write failure keeps `OutcomeUnknown`, and a subsequent retry with valid path succeeds, persists ledger, and completes recovery cleanly.

---

## 3. Verification & Test Evidence

### 3.1 Test Execution Results
- `dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`:
  - **484 passed, 0 failed** (duration: 4.7s)
- `dotnet test HPBim.slnx`:
  - **991 passed, 0 failed** across all 3 test projects (`HPBim.Client.Tests`, `HPBim.Core.Tests`, `HPBim.E2E.Tests`)
- `dotnet test HPRebar.Core.Tests`:
  - **1,682 passed, 0 failed**
- `dotnet test McpShared`:
  - **915 passed, 0 failed**

### 3.2 Clean Code Governance
- **Rule C2**: All files $\le 300$ lines (`IntentJournalRecord.cs`: 60 lines, `ReconcileService.cs`: 290 lines, `ReconcilePersistBeforeTerminalTests.cs`: 230 lines).
- **Rule M3**: All methods $\le 50$ lines (longest method is 46 lines).
- **Rule Q-B1**: Zero commented-out code.
- **Rule Q-B2**: Zero unannotated empty catch blocks.
- **Rule FM1**: `dotnet format --verify-no-changes` returned 0 violations.
- **Framework Purity**: `HPBim.Client` and `HPBim.Core` remain pure `netstandard2.0` / host-neutral with zero `Autodesk.Revit.*` references.
