# HPBim Wave 2 Remediation: Deep Technical Investigation Report (R3 & R4)

**Target Deliverable**: `HPBim` (`HPBim.slnx`)  
**Investigator**: `explorer_w2_2` (Codebase Investigator R3-R4)  
**Date**: 2026-10-08  
**Scope**: 
- **Requirement R3**: Recovery Receipt Completeness & Coverage Verification (P1)
- **Requirement R4**: CLI Ledger Wiring & Durable Ledger-First Commit Order (P1)

---

## Executive Summary

This investigation conducted a deep code-level audit of the recovery receipt pipeline and ledger coordination mechanisms across `HPBim.Client`, `HPBim.Core`, `HPBim.Cli`, and test suites (`HPBim.Client.Tests`, `HPBim.E2E.Tests`).

We confirmed critical architectural defects in both areas:
1. **In Requirement R3**:
   - `ExtensibleStorageReceiptReader.ParseElementMap` silently catches `JsonException` and returns an empty dictionary `new Dictionary<string, string>()`. This masks corrupt or truncated JSON receipts.
   - `ReconcileService.ApplyCommittedReceiptAsync` blindly accepts empty or partial receipts, performs zero validation against the original intent's `ItemCount`, and prematurely marks the intent as `Committed` in the write-ahead journal (`intent_journal.jsonl`).
   - Document identity matching (`MatchTargetDocId`) relies on loose `OR` logic matching solely on `Title`, allowing receipts from cloned RVT files at different filesystem paths or with different `VersionGuid`/`CentralGuid` to be erroneously accepted.
   - Existing tests in `HPBim.Client.Tests/Recovery/` actively enforce the flawed behavior (asserting empty dictionary on corrupted JSON).
2. **In Requirement R4**:
   - `GenerateCommand.cs` does **not** instantiate, load, or wire `BimCoordinationLedgerService` into `BimPipelineEngine`.
   - `BimPipelineEngine` does **not** persist `BimCoordinationLedger` to disk upon successful execution.
   - The commit order in `BimPipelineEngine` updates `IntentJournal` to `Committed` without persisting the coordination ledger to disk, violating the durable **Ledger-First** guarantee.
   - As a consequence, running `hpbim generate` a second time in a cold process fails to achieve 100% `NoOp` and risks generating duplicate elements in Revit.
   - Existing E2E and Client tests either test single-process in-memory idempotency or use mock data structures that bypass CLI and disk operations entirely.

Detailed findings, code locations, defect explanations, and proposed production diffs follow below.

---

## 1. Requirement R3: Recovery Receipt Completeness & Coverage Verification (P1)

### 1.1 `ParseElementMap` Silent Swallowing of JSON Exceptions
- **File**: `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs`
- **Lines**: 150–167
```csharp
150:     public static IReadOnlyDictionary<string, string> ParseElementMap(string json)
151:     {
152:         if (string.IsNullOrWhiteSpace(json) || json == "{}")
153:         {
154:             return new Dictionary<string, string>();
155:         }
156: 
157:         try
158:         {
159:             return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
160:                    ?? new Dictionary<string, string>();
161:         }
162:         catch (JsonException)
163:         {
164:             // Corrupt or truncated JSON in storage handled gracefully
165:             return new Dictionary<string, string>();
166:         }
167:     }
```

#### Why Returning an Empty Map on Corrupt JSON is Fatal
1. If Extensible Storage contains corrupted or truncated JSON (e.g., due to a crash during serialization, network truncation, or Revit data storage quota), `ParseElementMap` catches the `JsonException` and returns `{}`.
2. The caller in `ParseSingleReceipt` (line 146) sets `ElementMap = ParseElementMap(elemMapJson)`, producing a valid `CoordinationReceiptRecord` with `ElementMap.Count == 0`.
3. Upstream reconciliation cannot distinguish between:
   - A valid execution that legitimately created 0 elements (`ItemCount == 0`), and
   - A corrupted payload where 10 elements were created in Revit, but their mapping string was truncated.
4. When `ApplyCommittedReceiptAsync` runs, it maps 0 elements to the ledger and immediately updates `IntentJournal` to `Committed`.
5. **Impact**: The journal records the operation as permanently committed. The system assumes reconciliation succeeded. However, `_ledgerService` has 0 mappings for those elements. Any subsequent run will not find `TargetUniqueId` and will attempt to create them again in Revit, causing collisions or duplicate BIM geometry. This violates Rule 0: "Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."

#### Required Change
`ParseElementMap` must return `IReadOnlyDictionary<string, string>?` (nullable) or throw a dedicated parsing exception:
- Return `new Dictionary<string, string>()` ONLY when `json == "{}"` or `json` legitimately deserializes to an empty dictionary.
- Return `null` when `json` is invalid/corrupt JSON (`catch (JsonException)`).
- Update `CoordinationReceiptRecord.cs` to have `public IReadOnlyDictionary<string, string>? ElementMap { get; init; }`.

---

### 1.2 `ApplyCommittedReceiptAsync` Validation Against Intent & Coverage Verification
- **File**: `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs`
- **Lines**: 159–175
```csharp
159:     private async Task<ReconcileResult> ApplyCommittedReceiptAsync(
160:         IntentJournalRecord intent,
161:         CoordinationReceiptRecord receipt,
162:         CancellationToken ct)
163:     {
164:         var mappings = receipt.ElementMap ?? new Dictionary<string, string>();
165:         foreach (var (keyStr, uniqueId) in mappings)
166:         {
167:             if (CompositeSourceKey.TryParse(keyStr, out var sourceKey) && sourceKey != null)
168:             {
169:                 _ledgerService.UpdateMapping(sourceKey, uniqueId, intent.PlanHash, ElementExecutionStatus.Created);
170:             }
171:         }
172: 
173:         await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.Committed, null, ct).ConfigureAwait(false);
174:         return ReconcileResult.Committed(intent.OperationId, intent.PlanHash, mappings);
175:     }
```

#### Observations & Vulnerabilities
1. **No Null Check**: `receipt.ElementMap ?? new Dictionary<string, string>()` converts a corrupt/null element map into an empty dictionary.
2. **Zero `ItemCount` Verification**: If `intent.ItemCount == 10`, but `mappings.Count == 0` (or `mappings.Count == 2`), the method completely ignores the mismatch.
3. **No Expected Keys Verification**: If the intent had elements with source keys `[Doc]#[Column]:C1` through `[Doc]#[Column]:C10`, but the receipt only has `C1`, the missing 9 keys are silently skipped.
4. **Blind Commit**: Despite missing or incomplete mappings, line 173 marks `intent.OperationId` as `IntentStatus.Committed` in `IntentJournal`!

#### Required Change in `ApplyCommittedReceiptAsync`
1. If `receipt.ElementMap == null`:
   - Keep status `OutcomeUnknown`.
   - Update journal with reason: `"Receipt ElementMap is corrupt or unparseable in Extensible Storage."`.
   - Return `ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, ...)`. Refuse `Committed`.
2. If `intent.ItemCount > 0`:
   - Verify `receipt.ElementMap.Count == intent.ItemCount`.
   - If `receipt.ElementMap.Count != intent.ItemCount`:
     - Keep status `OutcomeUnknown`.
     - Update journal with reason: `$"Receipt item count mismatch: expected ItemCount={intent.ItemCount}, but receipt contained {receipt.ElementMap.Count} entries."`.
     - Return `ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, ...)`. Refuse `Committed`.
3. Validate each key and uniqueId in `receipt.ElementMap`:
   - Must parse to a valid `CompositeSourceKey`.
   - `uniqueId` must not be null or whitespace.
   - If any entry is invalid: refuse `Committed`, keep `OutcomeUnknown`.

---

### 1.3 Document Identity Matching & Cross-Document Protection
- **File**: `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs`
- **Lines**: 106–157 (`MatchTargetDocId`)
```csharp
106:     private static bool MatchTargetDocId(IntentJournalRecord intent, CoordinationReceiptRecord receipt)
107:     {
108:         string expectedDocId = intent.TargetDocId;
109:         string actualDocId = receipt.TargetDocId;
...
126:         var targetDoc = intent.TargetDoc;
127:         if (targetDoc != null && !targetDoc.IsEmpty)
128:         {
129:             if (!string.IsNullOrEmpty(targetDoc.Title) &&
130:                 string.Equals(targetDoc.Title, actualDocId, StringComparison.OrdinalIgnoreCase))
131:             {
132:                 return true;
133:             }
...
```

#### Observations & Vulnerabilities
1. In `ExtensibleStorageQueryScriptTemplate.cs` line 51:
   ```csharp
   targetDocId = doc.Title ?? string.Empty
   ```
   The Roslyn query script executed in Revit only queries and returns `doc.Title`.
2. In `MatchTargetDocId`, line 129 checks:
   `if (string.Equals(targetDoc.Title, actualDocId, StringComparison.OrdinalIgnoreCase)) return true;`
   Because `actualDocId` is just the Title, this check immediately evaluates to `true`!
3. **The Risk**: If an engineer opens a copy, detached version, or separate branch of the model having the same file title (`NhaPho_5x18m.rvt`), but located in another folder or having a different `VersionGuid`/`CentralGuid`, `ReconcileService` accepts the receipts from the wrong document!
4. It uses loose `OR` logic rather than checking all available document identity vectors.

#### Required Changes
1. **Enhance Extensible Storage Query Script** (`ExtensibleStorageQueryScriptTemplate.cs`):
   Query `title`, `pathName`, `versionGuid`, and `centralGuid`:
   ```csharp
   var docVer = Document.GetDocumentVersion(doc);
   receipts.Add(new
   {
       operationId = opId.ToString("D"),
       planHash = planHash,
       targetDocId = doc.Title ?? string.Empty,
       targetDocTitle = doc.Title ?? string.Empty,
       targetDocPath = string.IsNullOrEmpty(doc.PathName) ? string.Empty : doc.PathName,
       targetVersionGuid = docVer != null ? docVer.VersionGUID.ToString("D") : Guid.Empty.ToString("D"),
       targetCentralGuid = (doc.IsWorkshared ? doc.WorksharingCentralGUID : Guid.Empty).ToString("D"),
       committedAtUtc = committedUtc,
       elementMapJson = elemMapJson
   });
   ```
2. **Enhance `CoordinationReceiptRecord.cs`**:
   Add properties `TargetDocTitle`, `TargetDocPath`, `TargetVersionGuid`, `TargetCentralGuid`.
3. **Enforce Strict Multi-Factor Document Matching** in `ReconcileService.cs`:
   - If `intent.TargetDoc.Title` is present, it must match `receipt.TargetDocTitle`.
   - If `intent.TargetDoc.PathName` is present and non-empty, and `receipt.TargetDocPath` is non-empty, they must match.
   - If `intent.TargetDoc.CentralGuid` is present (workshared model), it must match `receipt.TargetCentralGuid`.
   - If `intent.TargetDoc.VersionGuid` is present and matches, or if non-empty, check for discrepancies.
   - Any conflict between document identities must cause `MatchTargetDocId` to return `false` and keep status `OutcomeUnknown`.

---

### 1.4 Test Coverage Analysis for R3
#### Existing Tests
- `HPBim.Client.Tests/Recovery/ExtensibleStorageReceiptReaderTests.cs`:
  - Line 110: `ParseReceipts_WithCorruptedElementMapJson_ReturnsEmptyGracefully` expects empty dict on corrupt JSON. *(Needs update)*
- `HPBim.Client.Tests/Recovery/AdversarialReceiptReaderTests.cs`:
  - Line 146: `ParseElementMap_CorruptedOrTruncatedJson_ReturnsEmptyDictionary` asserts `Assert.Empty(...)` on `"{ corrupt"`. *(Needs update)*
- `HPBim.Client.Tests/Recovery/ReceiptReaderTier5AdversarialTests.cs`:
  - Line 106: `ParseElementMap_EdgeCasesAndInvalidInputs_ReturnsEmptyDictionary` expects empty dictionary on corrupt JSON. *(Needs update)*
- `HPBim.Client.Tests/Recovery/AdversarialReconcileIdempotencyTests.cs`:
  - Line 135: `ScenarioE_CorruptElementMapInReceipt_GracefullyRecoversWithoutCrashing` asserts that when keys are malformed, it ignores them and marks `Committed`. *(Needs update)*

#### Required Test Cases for R3
1. `ParseElementMap_WhenJsonCorrupt_ReturnsNull` (asserts `null` when input is `"{ corrupt"` or `"{ incomplete"`).
2. `ApplyCommittedReceipt_WhenElementMapIsNull_KeepsOutcomeUnknown_RefusesCommitted`.
3. `ApplyCommittedReceipt_WhenItemCountMismatched_KeepsOutcomeUnknown_RefusesCommitted` (e.g., `intent.ItemCount = 5`, `receipt.ElementMap.Count = 3`).
4. `ApplyCommittedReceipt_WhenExpectedKeysMissing_KeepsOutcomeUnknown_RefusesCommitted`.
5. `MatchDocumentIdentity_SameTitleDifferentPath_ReturnsFalse_PreservesOutcomeUnknown`.
6. `MatchDocumentIdentity_SameTitleDifferentCentralGuid_ReturnsFalse_PreservesOutcomeUnknown`.

---

## 2. Requirement R4: CLI Ledger Wiring & Durable Ledger-First Commit Order (P1)

### 2.1 Examination of `GenerateCommand.cs` and `BimCoordinationLedgerService.cs`
- **File**: `HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs`
- **Lines**: 90–130

```csharp
90:     private async Task<int> RunPipelineAsync(BimModel model, bool dryRun, string pipeName, CancellationToken ct)
91:     {
92:         var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName });
93:         using var journal = dryRun ? null : new IntentJournal();
94:         try
95:         {
96:             var targetDoc = await ResolveTargetDocumentAsync(client, dryRun, ct).ConfigureAwait(false);
97:             if (targetDoc == null) return HandleTargetDocMissing(dryRun);
98: 
99:             var pipeline = new BimPipelineEngine(client)
100:             {
101:                 IntentJournal = journal
102:             };
...
116:             var result = await pipeline.ExecuteAsync(model, options, ct).ConfigureAwait(false);
```

#### Observations
1. `BimCoordinationLedgerService` is completely absent from `GenerateCommand.cs`.
2. `pipeline.LedgerService` is `null`.
3. The command does not read any existing ledger from disk (e.g. `state/bim_coordination_ledger.json`).
4. The command does not write any updated ledger to disk.
5. `BimCoordinationLedgerService` is located at `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.cs`. It already has `LoadFromFileAsync` and `SaveToFileAsync` using atomic file replacement (`.tmp` -> replace with file lock in `BimCoordinationLedgerSerializer.cs`). But it is never invoked by `GenerateCommand`!

---

### 2.2 Automatic `TargetUniqueId` Lookup in Pipeline
- **File**: `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs`
- **Lines**: 38–41 & 74–86
```csharp
38:         if (ledgerService != null)
39:         {
40:             plan = PopulateTargetUniqueIds(plan, ledgerService);
41:         }
...
74:     private static BimExecutionPlan PopulateTargetUniqueIds(BimExecutionPlan plan, BimCoordinationLedgerService ledgerService)
75:     {
76:         var itemsWithTargets = plan.Items.Select(item =>
77:         {
78:             if (!string.IsNullOrEmpty(item.TargetUniqueId)) return item;
79:             if (ledgerService.TryGetTarget(item.SourceKey, out var uid) && !string.IsNullOrEmpty(uid))
80:             {
81:                 return item with { TargetUniqueId = uid };
82:             }
83:             return item;
84:         }).ToList();
85:         return plan with { Items = itemsWithTargets };
86:     }
```

#### Observations
- The mechanism to look up `TargetUniqueId` for plan items already exists in `PopulateTargetUniqueIds`.
- In `BimPlanRunnerScriptTemplate.Columns.cs` (lines 8–45) and other structure scripts:
  - When `targetUniqueId` is populated, the Revit runner fetches `doc.GetElement(targetUniqueId)`.
  - If found and unchanged, the runner returns `status = "NoOp"` without re-creating the element.
  - If `targetUniqueId` is missing, the runner creates a new element (`status = "Created"`).
- **Because `GenerateCommand` never sets `pipeline.LedgerService`, `PopulateTargetUniqueIds` is never executed during CLI runs!** Every single run acts as if the ledger is empty.

---

### 2.3 Commit Order: Durable Ledger-First Commit Order
- **Files**:
  - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs` (lines 49–53)
  - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` (lines 64–77 & 121–148)

#### Current Execution Flow
1. In `ExecutePlanAsync` (`PlanExecution.cs`):
   ```csharp
   if (success && !plan.Options.DryRun && ledgerService != null)
   {
       ledgerService.SyncFromExecutionRecords(elementRecords, plan.PlanHash);
   }
   ```
   This updates the **in-memory** `_mappings` dictionary only. It does NOT save to disk!
2. In `DispatchPlanSafelyAsync` (`IntentJournal.cs`):
   ```csharp
   await RecordPostDispatchOutcomeAsync(
       plan,
       result.Success,
       result.RolledBack,
       outcomeUnknown,
       result.ErrorMessage,
       cancellationToken).ConfigureAwait(false);
   ```
   This immediately writes `IntentStatus.Committed` to `intent_journal.jsonl`.
3. The ledger is **never** saved to disk.

#### The Consequence of Wrong Commit Order
- If the journal is updated to `Committed` before the ledger is persisted to disk:
  - If the process is terminated (or crashes) between these steps:
    - The journal says `Committed` (so reconciliation is bypassed).
    - The ledger on disk does NOT have the mappings.
    - Upon next CLI run, the ledger is empty, so elements are re-created as duplicates!
- **Durable Ledger-First Order Requirement**:
  1. Pipeline applies successfully in Revit.
  2. Pipeline syncs execution records into `ledgerService`.
  3. `ledgerService.SaveToFileAsync(ledgerFilePath)` persists the ledger to disk atomically (`.tmp` -> replace).
  4. **ONLY AFTER** disk persistence succeeds: `IntentJournal.UpdateStatusAsync` records `IntentStatus.Committed`.
  5. If `SaveToFileAsync` fails: the journal MUST NOT be marked `Committed`. It must be marked `OutcomeUnknown` with the ledger persistence error reason.

---

### 2.4 Cold Process Rerun Testing (Proving 100% `NoOp`)
#### Analysis of Existing Tests
- `HPBim.Client.Tests/Cli/GenerateCommandTests.cs`:
  Has zero tests for ledger loading, disk persistence, or rerun idempotency.
- `HPBim.Client.Tests/Pipeline/IdempotentExecutionTests.cs`:
  Tests rerun within the same memory instance using a shared in-memory `ledgerService`. Never tests disk serialization or cold process restarts.
- `HPBim.E2E.Tests/Tiers/Tier4_RealWorldWorkloadTests.cs` (`TC_T4_WL_03`):
  Tests a loop with `ledger.TryGetTarget`, completely bypassing CLI and pipeline execution.

#### Cold Process Test Architecture
To definitively prove 100% `NoOp` on rerun, a test must simulate two independent process lifetimes:
```csharp
[Fact]
public async Task GenerateCommand_ColdProcessRerun_PersistsLedgerAndProduces100PercentNoOp()
{
    string tempDir = Path.Combine(Path.GetTempPath(), $"cold_test_{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempDir);
    string modelPath = Path.Combine(tempDir, "model.json");
    string ledgerPath = Path.Combine(tempDir, "state", "bim_coordination_ledger.json");
    try
    {
        var model = NhaPho5x18mFixture.CreateModel();
        await BimModelSerializer.SerializeToFileAsync(model, modelPath);

        // --- PROCESS 1 SIMULATION ---
        var fakePipe1 = new FakeRevitPipeClient(); // returns Created with unique IDs
        var cmd1 = new GenerateCommand(new ConsoleFormatter(new StringWriter()), fakePipe1);
        int exit1 = await cmd1.ExecuteAsync(CliParser.Parse(["generate", "--input", modelPath, "--ledger", ledgerPath]));
        
        Assert.Equal(0, exit1);
        Assert.True(File.Exists(ledgerPath));
        
        // Assert ledger file on disk contains mappings for all items
        var diskLedger1 = BimCoordinationLedgerSerializer.DeserializeFromFile(ledgerPath);
        Assert.NotEmpty(diskLedger1.Mappings);

        // Terminate Process 1: cmd1, fakePipe1, and all in-memory state are discarded.
        cmd1 = null; fakePipe1 = null;

        // --- PROCESS 2 SIMULATION (COLD RESTART) ---
        var fakePipe2 = new FakeRevitPipeClient(); // handles incoming targetUniqueIds and returns NoOp
        ConfigureSimulatedRevitNoOpHandler(fakePipe2, diskLedger1.Mappings);
        
        var cmd2 = new GenerateCommand(new ConsoleFormatter(new StringWriter()), fakePipe2);
        int exit2 = await cmd2.ExecuteAsync(CliParser.Parse(["generate", "--input", modelPath, "--ledger", ledgerPath]));

        Assert.Equal(0, exit2);
        
        // Verify all elements in batch request contained TargetUniqueId
        Assert.All(fakePipe2.ExecutedRequests, req => {
            // verify req items have targetUniqueId populated
        });
        
        // Verify 100% NoOp in result
    }
    finally
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }
}
```

---

## 3. Recommended Code Changes & Architecture Diffs

### 3.1 `ExtensibleStorageReceiptReader.cs` Diff Design
```diff
--- a/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs
+++ b/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs
@@ -140,7 +140,7 @@ public sealed class ExtensibleStorageReceiptReader : IExtensibleStorageReceiptRe
         return new CoordinationReceiptRecord
         {
             OperationId = Guid.TryParse(opIdStr, out var parsed) ? parsed : Guid.Empty,
             PlanHash = planHash,
             TargetDocId = targetDocId,
             CommittedAtUtc = committedAt,
             ElementMap = ParseElementMap(elemMapJson)
         };
     }
 
-    public static IReadOnlyDictionary<string, string> ParseElementMap(string json)
+    public static IReadOnlyDictionary<string, string>? ParseElementMap(string json)
     {
-        if (string.IsNullOrWhiteSpace(json) || json == "{}")
+        if (string.IsNullOrWhiteSpace(json))
         {
-            return new Dictionary<string, string>();
+            return null;
         }
+        if (json.Trim() == "{}")
+        {
+            return new Dictionary<string, string>();
+        }
 
         try
         {
             return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
-                   ?? new Dictionary<string, string>();
+                   ?? null;
         }
         catch (JsonException)
         {
-            // Corrupt or truncated JSON in storage handled gracefully
-            return new Dictionary<string, string>();
+            // Corrupt or truncated JSON in storage must not be masked as valid empty map
+            return null;
         }
     }
 }
```

### 3.2 `ReconcileService.cs` Diff Design
```diff
--- a/src/HPBim.Client/Recovery/ReconcileService.cs
+++ b/src/HPBim.Client/Recovery/ReconcileService.cs
@@ -159,16 +159,38 @@ public sealed class ReconcileService : IReconcileService
     private async Task<ReconcileResult> ApplyCommittedReceiptAsync(
         IntentJournalRecord intent,
         CoordinationReceiptRecord receipt,
         CancellationToken ct)
     {
-        var mappings = receipt.ElementMap ?? new Dictionary<string, string>();
+        if (receipt.ElementMap == null)
+        {
+            string reason = "Receipt ElementMap is null or corrupt in Revit Extensible Storage. Preserving OutcomeUnknown.";
+            await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
+            return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
+        }
+
+        if (intent.ItemCount > 0 && receipt.ElementMap.Count != intent.ItemCount)
+        {
+            string reason = $"Receipt completeness mismatch: expected ItemCount={intent.ItemCount}, but receipt contained {receipt.ElementMap.Count} entries. Preserving OutcomeUnknown.";
+            await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
+            return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
+        }
+
+        var mappings = receipt.ElementMap;
         foreach (var (keyStr, uniqueId) in mappings)
         {
-            if (CompositeSourceKey.TryParse(keyStr, out var sourceKey) && sourceKey != null)
+            if (!CompositeSourceKey.TryParse(keyStr, out var sourceKey) || sourceKey == null || string.IsNullOrWhiteSpace(uniqueId))
             {
-                _ledgerService.UpdateMapping(sourceKey, uniqueId, intent.PlanHash, ElementExecutionStatus.Created);
+                string reason = $"Receipt contains invalid or unparseable source key '{keyStr}' or empty uniqueId. Preserving OutcomeUnknown.";
+                await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
+                return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
             }
+            _ledgerService.UpdateMapping(sourceKey, uniqueId, intent.PlanHash, ElementExecutionStatus.Created);
         }
 
         await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.Committed, null, ct).ConfigureAwait(false);
         return ReconcileResult.Committed(intent.OperationId, intent.PlanHash, mappings);
     }
```

### 3.3 `GenerateCommand.cs` Diff Design
```diff
--- a/src/HPBim.Cli/Commands/GenerateCommand.cs
+++ b/src/HPBim.Cli/Commands/GenerateCommand.cs
@@ -40,6 +40,7 @@ public sealed class GenerateCommand
 
+        string ledgerPath = ResolveLedgerPath(args);
         _formatter.WriteBanner("Generate BIM Model", $"Dispatch 8-Stage Pipeline sang Revit 2026 (Pipe: {pipeName})");
...
-        return await RunPipelineAsync(model, dryRun, pipeName, ct).ConfigureAwait(false);
+        return await RunPipelineAsync(model, dryRun, pipeName, ledgerPath, ct).ConfigureAwait(false);
     }
...
-    private async Task<int> RunPipelineAsync(BimModel model, bool dryRun, string pipeName, CancellationToken ct)
+    private async Task<int> RunPipelineAsync(BimModel model, bool dryRun, string pipeName, string ledgerPath, CancellationToken ct)
     {
         var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName });
         using var journal = dryRun ? null : new IntentJournal();
+        var ledgerService = new BimCoordinationLedgerService();
+        if (File.Exists(ledgerPath))
+        {
+            await ledgerService.LoadFromFileAsync(ledgerPath, ct).ConfigureAwait(false);
+            _formatter.WriteInfo($"[Ledger] Đã nạp sổ cái điều phối từ: {ledgerPath} ({ledgerService.Mappings.Count} mappings)");
+        }
+        else
+        {
+            ledgerService.Initialize("HPBim Coordination Project");
+            _formatter.WriteInfo($"[Ledger] Khởi tạo sổ cái điều phối mới tại: {ledgerPath}");
+        }
+
         try
         {
             var targetDoc = await ResolveTargetDocumentAsync(client, dryRun, ct).ConfigureAwait(false);
             if (targetDoc == null) return HandleTargetDocMissing(dryRun);
 
             var pipeline = new BimPipelineEngine(client)
             {
-                IntentJournal = journal
+                IntentJournal = journal,
+                LedgerService = ledgerService,
+                LedgerFilePath = dryRun ? null : ledgerPath
             };
```

### 3.4 `BimPipelineEngine.IntentJournal.cs` Diff Design (Durable Ledger-First)
```diff
--- a/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs
+++ b/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs
@@ -16,6 +16,7 @@ public sealed partial class BimPipelineEngine
     public IIntentJournal? IntentJournal { get; set; }
     public TimeoutPolicy? TimeoutPolicy { get; set; }
+    public string? LedgerFilePath { get; set; }
 
     private async Task<BimPipelineResult> DispatchPlanSafelyAsync(
         BimExecutionPlan plan,
         BimCoordinationLedgerService? ledgerService,
         Stopwatch sw,
         CancellationToken cancellationToken)
     {
         try
         {
             var result = await ExecutePlanAsync(plan, ledgerService, cancellationToken).ConfigureAwait(false);
 
+            // DURABLE LEDGER-FIRST: Persist ledger to disk BEFORE updating journal to Committed
+            if (result.Success && !plan.Options.DryRun && ledgerService != null && !string.IsNullOrEmpty(LedgerFilePath))
+            {
+                await ledgerService.SaveToFileAsync(LedgerFilePath, cancellationToken).ConfigureAwait(false);
+            }
+
             bool outcomeUnknown = result.IsOutcomeUnknown || (!result.Success && !result.RolledBack && !plan.Options.DryRun);
 
             await RecordPostDispatchOutcomeAsync(
                 plan,
                 result.Success,
                 result.RolledBack,
                 outcomeUnknown,
                 result.ErrorMessage,
                 cancellationToken).ConfigureAwait(false);
```

---

## 4. Synthesis & Recommendations for Wave 2 Implementer

1. **Rule 0 Adherence**:
   - Refuse silent fallbacks in `ParseElementMap`.
   - Refuse premature commits in `ApplyCommittedReceiptAsync`.
   - Refuse single-process-only assertions in test suites.
2. **Clean Code Metrics**:
   - `ExtensibleStorageReceiptReader.cs` is currently 169 lines (<= 300 lines limit).
   - `ReconcileService.cs` is currently 177 lines (<= 300 lines limit).
   - `GenerateCommand.cs` is currently 178 lines (<= 300 lines limit).
   - `BimPipelineEngine.IntentJournal.cs` is currently 162 lines (<= 300 lines limit).
   - All proposed methods are <= 35 lines (well below the 50-line M3 limit).
3. **Execution Plan**:
   - Implement R3 changes in `ExtensibleStorageReceiptReader.cs`, `CoordinationReceiptRecord.cs`, and `ReconcileService.cs`.
   - Update tests in `ExtensibleStorageReceiptReaderTests.cs`, `AdversarialReceiptReaderTests.cs`, `ReceiptReaderTier5AdversarialTests.cs`, and add new test cases.
   - Implement R4 wiring in `GenerateCommand.cs` and `BimPipelineEngine.IntentJournal.cs`.
   - Add cold process rerun test in `GenerateCommandTests.cs` and `Tier4_RealWorldWorkloadTests.cs`.
   - Execute `dotnet test` across all projects to ensure zero regressions across all 847 tests.
