## 2026-10-08T16:31:21Z
You are worker_m2_remediation for Milestone M2 (Receipt Completeness & CLI Ledger-First Durable Wiring) in HPBim Wave 2 Remediation.
Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_remediation\
Parent Conversation ID: e0aba3da-bb9f-4e02-be57-11f1cc948914
Original Request: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (READ under header ## 2026-10-08T15:03:54Z)
Explorer Survey Report: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_w2_2\report.md
Target Solution: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Scope & Exclusively Owned Files:
1. `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs` (Requirement R3)
2. `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` (Requirement R3)
3. `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs` (Requirement R3)
4. `HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs` (Requirement R4)
5. `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` (Requirement R4)
6. `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs` (Requirement R4)
7. Associated tests in `HPBim/tests/HPBim.Client.Tests/Recovery/` and `HPBim/tests/HPBim.Client.Tests/Cli/`

Tasks to Implement:
1. Requirement R3: Recovery Receipt Completeness & Coverage Verification (P1):
   - In `ExtensibleStorageReceiptReader.cs`:
     * Update `ParseElementMap(string json)` to return `IReadOnlyDictionary<string, string>?` (nullable).
     * When `json` is null, whitespace, or invalid/truncated JSON (`catch (JsonException)`), return `null` (or throw). DO NOT silently return an empty dictionary `{}`.
     * Update `CoordinationReceiptRecord.ElementMap` to be nullable `IReadOnlyDictionary<string, string>?`.
   - In `ReconcileService.cs`:
     * In `ApplyCommittedReceiptAsync`:
       - Verify `receipt.ElementMap != null`. If null, keep status `OutcomeUnknown`, refuse to mark `Committed`.
       - Verify `ItemCount`: `receipt.ElementMap.Count` must match `intent.ItemCount` (allow when both are 0). If mismatched, keep status `OutcomeUnknown`, refuse to mark `Committed`.
       - Verify expected keys: all keys in mapping must be valid composite source keys matching the intent.
     * In `MatchTargetDocId` and `ExtensibleStorageQueryScriptTemplate.cs`:
       - Update the query script to extract `doc.Title`, `doc.PathName`, and `doc.VersionGUID` (or `doc.GetWorksharingCentralModelPath()`).
       - In `MatchTargetDocId`, verify document identity against `targetDoc`: match `Title`, `PathName`, and `VersionGuid` to prevent accepting receipts from duplicate or detached models.
   - Update existing test assertions expecting `{}` on corrupt JSON in `ExtensibleStorageReceiptReaderTests.cs`, `AdversarialReceiptReaderTests.cs`, and `ReceiptReaderTier5AdversarialTests.cs` to expect `null`.
   - Add new unit tests verifying that corrupt receipts or item count mismatches keep `OutcomeUnknown`.

2. Requirement R4: CLI Ledger Wiring & Durable Ledger-First Commit Order (P1):
   - In `HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs`:
     * Resolve ledger file path from `--ledger` option (or default `state/bim_coordination_ledger.json`).
     * Instantiate/load `BimCoordinationLedgerService` from disk via `BimCoordinationLedgerService.LoadFromFile(ledgerPath)`.
     * Wire `pipeline.LedgerService = ledgerService` and `pipeline.LedgerFilePath = ledgerPath`.
   - In `HPBim.Client/Pipeline/BimPipelineEngine`:
     * Automatic `TargetUniqueId` lookup: before dispatching items, query `LedgerService` for known `TargetUniqueId` based on `SourceKey` to distinguish `Create` vs `Update`/`NoOp`.
     * **Durable Ledger-First Commit Order**: In `BimPipelineEngine.IntentJournal.cs` (or post-dispatch), when execution succeeds:
       1. Update ledger mappings in memory.
       2. Atomically persist `BimCoordinationLedger` to disk (`.tmp` -> atomic replace with lock) via `BimCoordinationLedgerSerializer`.
       3. **ONLY AFTER** the ledger is durably written to disk, update `IntentJournal` status to `Committed`.
   - In tests: add a test for cold process rerun (executing `GenerateCommand` twice) proving that the second run yields 100% `NoOp` elements and 0 duplicate creations.

3. Clean Code Compliance:
   - File length <= 300 lines (Rule C2).
   - Method length <= 50 lines (Rule M3).
   - 0 commented-out code (Rule Q-B1).
   - 0 unannotated catch blocks (Rule Q-B2).
   - `HPBim.Core` pure netstandard2.0 with 0 Revit references.

4. Verification:
   - Run `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
   - Run `dotnet test HPBim/HPBim.slnx` -> 100% tests pass.
   - Document results in `handoff.md` and send message to Parent.
