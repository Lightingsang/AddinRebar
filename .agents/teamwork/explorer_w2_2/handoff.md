# Handoff Report — Explorer W2-2 (R3 & R4 Investigation)

## 1. Observation
1. **`ParseElementMap` Swallows Corrupt JSON**:
   In `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs` lines 150–167:
   ```csharp
   public static IReadOnlyDictionary<string, string> ParseElementMap(string json)
   {
       if (string.IsNullOrWhiteSpace(json) || json == "{}")
       {
           return new Dictionary<string, string>();
       }

       try
       {
           return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                  ?? new Dictionary<string, string>();
       }
       catch (JsonException)
       {
           // Corrupt or truncated JSON in storage handled gracefully
           return new Dictionary<string, string>();
       }
   }
   ```
   When `json` is corrupt or truncated, `catch (JsonException)` swallows the exception and returns an empty dictionary `{}` instead of `null`.
2. **`ApplyCommittedReceiptAsync` Lacks Completeness and Coverage Verification**:
   In `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` lines 159–175:
   ```csharp
   private async Task<ReconcileResult> ApplyCommittedReceiptAsync(
       IntentJournalRecord intent,
       CoordinationReceiptRecord receipt,
       CancellationToken ct)
   {
       var mappings = receipt.ElementMap ?? new Dictionary<string, string>();
       foreach (var (keyStr, uniqueId) in mappings)
       {
           if (CompositeSourceKey.TryParse(keyStr, out var sourceKey) && sourceKey != null)
           {
               _ledgerService.UpdateMapping(sourceKey, uniqueId, intent.PlanHash, ElementExecutionStatus.Created);
           }
       }

       await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.Committed, null, ct).ConfigureAwait(false);
       return ReconcileResult.Committed(intent.OperationId, intent.PlanHash, mappings);
   }
   ```
   It accepts `receipt.ElementMap ?? new Dictionary<string, string>()`, performs zero checks on `intent.ItemCount`, performs zero validation that all expected keys exist, and unconditionally marks `intent.OperationId` as `IntentStatus.Committed`.
3. **Document Identity Matching Flaw**:
   In `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` lines 126–154 (`MatchTargetDocId`) and `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs` line 51:
   The query script only returns `targetDocId = doc.Title ?? string.Empty`.
   `MatchTargetDocId` evaluates `if (!string.IsNullOrEmpty(targetDoc.Title) && string.Equals(targetDoc.Title, actualDocId, StringComparison.OrdinalIgnoreCase)) return true;`.
   It returns `true` immediately based on title alone, ignoring `PathName`, `VersionGuid`, and `CentralGuid`.
4. **`GenerateCommand.cs` Missing Ledger Wiring**:
   In `HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs` lines 99–102:
   ```csharp
   var pipeline = new BimPipelineEngine(client)
   {
       IntentJournal = journal
   };
   ```
   `BimCoordinationLedgerService` is never instantiated or loaded. `pipeline.LedgerService` is null.
5. **Durable Ledger-First Commit Order Violation**:
   In `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs` line 51, `ledgerService.SyncFromExecutionRecords` only updates in-memory dictionaries.
   In `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` lines 70–76, `RecordPostDispatchOutcomeAsync` immediately marks `IntentJournal` as `Committed`.
   The coordination ledger is never saved to disk before (or even after) the journal write.
6. **Test Suites Currently Pass but Enforce Flawed Patterns**:
   - `dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj` (376 passed)
   - `dotnet test tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj` (429 passed)
   - `dotnet test tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj` (42 passed)
   Tests in `ExtensibleStorageReceiptReaderTests.cs` (line 110), `AdversarialReceiptReaderTests.cs` (line 146), and `ReceiptReaderTier5AdversarialTests.cs` (line 106) assert that corrupt JSON returns an empty dictionary.
   `IdempotentExecutionTests.cs` and `Tier4_RealWorldWorkloadTests.cs` do not test cold process restarts or CLI ledger persistence.

---

## 2. Logic Chain
1. From Observation 1: When corrupt JSON is read from Revit Extensible Storage, `ParseElementMap` returns an empty dictionary rather than `null`.
2. From Observation 2: `ApplyCommittedReceiptAsync` processes the empty dictionary, adds 0 element mappings to `_ledgerService`, ignores `intent.ItemCount`, and marks the operation as `Committed` in `intent_journal.jsonl`.
3. Therefore: The system falsely declares a corrupt receipt as successfully committed while zero element mappings were restored to the ledger. Subsequent executions cannot find `TargetUniqueId` and will attempt duplicate element creation in Revit.
4. From Observation 3: Because `ExtensibleStorageQueryScriptTemplate` only queries `doc.Title` and `MatchTargetDocId` returns `true` on title match alone, receipts from a cloned RVT document at a different directory or with a different `VersionGuid` will be accepted.
5. From Observation 4: Because `GenerateCommand.cs` does not instantiate or wire `BimCoordinationLedgerService`, `pipeline.LedgerService` is null during `hpbim generate`.
6. From Observation 5: Because `BimPipelineEngine` does not persist the ledger to disk before writing `IntentStatus.Committed`, any process restart loses all mappings, and a cold process rerun cannot achieve `NoOp`.
7. Consequence: The pipeline fails the core idempotency guarantee on cold restart.

---

## 3. Caveats
- No changes to existing production source code files were made during this investigation (strictly read-only exploration).
- Revit 2026 live IPC was not connected; tests were verified through the project's official xUnit/MTP test suites and `FakeRevitPipeClient` harnesses.
- Updating `ParseElementMap` to return `null` on corrupt JSON will require updating 3 existing test assertions that previously expected an empty dictionary.

---

## 4. Conclusion
Requirements R3 and R4 require immediate implementation:
1. **R3**: Update `ParseElementMap` to return `null` on corrupt JSON; update `CoordinationReceiptRecord.ElementMap` to nullable; enforce strict `ItemCount` and element key validation in `ApplyCommittedReceiptAsync` (refusing `Committed` and keeping `OutcomeUnknown` on mismatch); enhance `ExtensibleStorageQueryScriptTemplate` and `MatchTargetDocId` to require matching `PathName`, `VersionGuid`, and `CentralGuid`.
2. **R4**: Wire `BimCoordinationLedgerService` into `GenerateCommand.cs` (resolving `--ledger` path and loading from disk); wire `pipeline.LedgerService` and `pipeline.LedgerFilePath`; implement the **Durable Ledger-First** commit order in `BimPipelineEngine.IntentJournal.cs` by atomically persisting `BimCoordinationLedger` to disk before updating `IntentJournal` to `Committed`; add a cold process rerun test verifying 100% `NoOp` across distinct command process invocations.

---

## 5. Verification Method
1. Inspect files:
   - `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs`
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs`
   - `HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs`
   - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs`
   - Full analysis report at `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_w2_2\report.md`
2. Test commands to run after implementer applies changes:
   ```bash
   dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   dotnet test tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
   dotnet test tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj
   ```
3. Invalidation condition:
   If `ParseElementMap` returning `null` or `ApplyCommittedReceiptAsync` checking `ItemCount` causes legitimate 0-item receipts (where `intent.ItemCount == 0`) to be rejected, ensure `intent.ItemCount == 0 && receipt.ElementMap.Count == 0` is treated as valid.
