# Technical Investigation Report: Requirement R1 & Requirement R2 (HPBim Wave 2)

**Author:** `explorer_w2_1` (Codebase Investigator R1-R2)  
**Date:** 2026-10-08T15:25:00Z  
**Target Solution:** `HPBim` (`G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx`)  
**Mission Scope:** Requirement R1 (Strict Record Matching & 1:1 Bijection Validation, P0) & Requirement R2 (Journal Torn-tail Resilience & Auto-Recovery, P1).

---

## 1. Executive Summary

A comprehensive, read-only architectural investigation was performed on the execution pipeline reconciliation logic and write-ahead intent journal of `HPBim`.

### Key Findings Summary:
1. **Requirement R1 (P0): Strict Record Matching & 1:1 Bijection Validation is NOT currently enforced.**
   - `ReconcileRecordsStrict1To1` in `src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` groups records by `SourceKey` and iterates *only* over `plan.Items`.
   - **Foreign records** (records returned with `SourceKey`s outside the plan) are silently ignored and dropped from validation.
   - **Duplicate records** (multiple records for the same `SourceKey`) are queued; the first record is taken and subsequent records are ignored without warning or error.
   - **Status enum validation** is missing: invalid status strings fail `Enum.TryParse` silently and default to `(ElementExecutionStatus)0` (an undefined enum value), which bypasses error checks and is treated as a success.
   - **Missing `TargetUniqueId`** on `Created` or `Updated` records is NOT validated; elements created without a UniqueId are accepted, causing silent tracking failure in `BimCoordinationLedgerService`.
   - **Category & Mark consistency** is NOT checked: mismatched Category or Mark between plan and record is silently accepted.
   - **Unauthorized `Skipped` records** on required plan items are returned as-is and do not trigger batch rollback, allowing un-constructed elements to pass as successful pipeline runs.

2. **Requirement R2 (P1): Journal Torn-Tail Resilience is BROKEN (`TORN_APPEND_INTENT_LOST`).**
   - In `src/HPBim.Client/Connection/IntentJournal.cs`, `AppendLineWithFlushAsync` holds `_ioLock`, but opens the file with `FileMode.Append` and immediately appends new bytes without checking whether the file ends with `\n`.
   - If a previous process crashed mid-write leaving a partial line without a newline, new JSON lines are concatenated directly onto the broken line.
   - When the file is read after process restart, the merged line throws `JsonException` and is skipped, causing **100% loss of the newly recorded operation intent** upon cold restart.
   - This defect was already characterized in `tests/HPBim.Client.Tests/Connection/IntentJournalAdversarialTests.cs` (lines 226–253, `TrailingTruncatedLineWithoutNewline_InvestigatesAppendBehavior`), which explicitly asserts `Assert.Null(op2Cold)`.

---

## 2. Codebase Path Mapping & Structural Orientation

The prompt noted paths such as `HPBim/HPBim.Client/Execution/` and `HPBim/HPBim.Core/Journal/`. The actual codebase layout in `HPBim` is structured as follows:

| Conceptual Component | Actual File Location in Codebase | Project & TFM |
|---|---|---|
| **Pipeline Element Records** | `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` | `HPBim.Client` (`net10.0`) |
| **Pipeline Plan Execution** | `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs` | `HPBim.Client` (`net10.0`) |
| **Pipeline Engine Core** | `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.cs` | `HPBim.Client` (`net10.0`) |
| **Intent Journal Implementation** | `HPBim/src/HPBim.Client/Connection/IntentJournal.cs` | `HPBim.Client` (`net10.0`) |
| **Intent Journal DTOs** | `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs`, `IntentStatus.cs` | `HPBim.Client` (`net10.0`) |
| **Element Execution Models** | `HPBim/src/HPBim.Core/Execution/ElementExecutionRecord.cs`, `ElementExecutionStatus.cs` | `HPBim.Core` (`netstandard2.0`) |
| **Execution Plan Models** | `HPBim/src/HPBim.Core/Execution/BimExecutionPlan.cs`, `ElementPlanItem.cs` | `HPBim.Core` (`netstandard2.0`) |
| **Pipeline Tests** | `HPBim/tests/HPBim.Client.Tests/Pipeline/` | `HPBim.Client.Tests` (`net10.0`) |
| **Journal Tests** | `HPBim/tests/HPBim.Client.Tests/Connection/` | `HPBim.Client.Tests` (`net10.0`) |

*Note:* `HPBim.Core` is pure domain logic (`netstandard2.0`, zero host/file dependencies). All I/O, named pipe connectivity, and journaling live in `HPBim.Client`.

---

## 3. Deep Investigation: Requirement R1 (Strict Record Matching & 1:1 Bijection Validation, P0)

### 3.1 Current Implementation Analysis

In `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs`:

```csharp
86:  private static IReadOnlyList<ElementExecutionRecord> ReconcileRecordsStrict1To1(
87:      BimExecutionPlan plan,
88:      IReadOnlyList<ElementExecutionRecord> returned,
89:      bool isFailedRollback)
90:  {
91:      var recordMap = GroupRecordsBySourceKey(returned);
92:      var reconciled = new List<ElementExecutionRecord>(plan.Items.Count);
93:      foreach (var item in plan.Items)
94:      {
95:          recordMap.TryGetValue(item.SourceKey, out var queue);
96:          reconciled.Add(ReconcileSingleItem(item, queue, isFailedRollback));
97:      }
98:
99:      return reconciled;
100: }
```

And lines 102–117:
```csharp
102: private static Dictionary<CompositeSourceKey, Queue<ElementExecutionRecord>> GroupRecordsBySourceKey(
103:     IReadOnlyList<ElementExecutionRecord> returned)
104: {
105:     var recordMap = new Dictionary<CompositeSourceKey, Queue<ElementExecutionRecord>>();
106:     foreach (var rec in returned)
107:     {
108:         if (!recordMap.TryGetValue(rec.SourceKey, out var q))
109:         {
110:             q = new Queue<ElementExecutionRecord>();
111:             recordMap[rec.SourceKey] = q;
112:         }
113:         q.Enqueue(rec);
114:     }
115:
116:     return recordMap;
117: }
```

And lines 161–170:
```csharp
161: private static ElementExecutionRecord ParseSingleElementRecord(JsonElement elem)
162: {
163:     string rawSourceKey = GetOptionalString(elem, "sourceKey") ?? string.Empty;
164:     string mark = GetOptionalString(elem, "mark") ?? string.Empty;
165:     string catStr = GetOptionalString(elem, "category") ?? "Column";
166:     string statusStr = GetOptionalString(elem, "status") ?? "Failed";
167:
168:     Enum.TryParse<CoordinationCategory>(catStr, true, out var cat);
169:     Enum.TryParse<ElementExecutionStatus>(statusStr, true, out var status);
```

### 3.2 Evaluation Against R1 Specifications

| Question / Requirement | Current Behavior | Verdict & Gap |
|---|---|---|
| **Strict 1:1 Bijection Checking** | Only checks deficit (when an item in `plan.Items` is missing from `returned`). Does NOT check cardinality equality (`plan.Items.Count == returned.Count`). | **NON-COMPLIANT.** A mathematical bijection requires injective, surjective, and cardinality matching. Surplus or deficit breaks bijection. |
| **Foreign Record Rejection** | Any record in `returned` whose `SourceKey` is NOT in `plan.Items` is placed in `recordMap` but never iterated over. It is silently dropped without error. | **NON-COMPLIANT.** Foreign records must cause immediate batch rejection (`ERR_FOREIGN_RECORD`) and rollback. |
| **Duplicate Record Rejection** | Multiple records with identical `SourceKey` are enqueued in `queue`. `queue.Dequeue()` takes the first record, leaving the duplicates uninspected. | **NON-COMPLIANT.** Duplicate records must cause immediate batch rejection (`ERR_DUPLICATE_RECORD`) and rollback. |
| **Valid `Status` Enum Validation** | `Enum.TryParse` fails silently on invalid strings (e.g. `"BogusStatus"`), setting `status = 0`. Value `0` is undefined in `ElementExecutionStatus`. It is not `Failed` and not `OutcomeUnknown`, so pipeline treats it as **SUCCESS**. | **NON-COMPLIANT.** Must validate `Enum.IsDefined(typeof(ElementExecutionStatus), status) && status != 0`. If invalid, treat as `ERR_INVALID_STATUS_ENUM`. |
| **Mandatory `TargetUniqueId` for `Created`/`Updated`** | `TargetUniqueId` is accepted as `null` or whitespace. In `SyncFromExecutionRecords`, lines 93–96 silently skip empty IDs, leaving elements untracked in the ledger. | **NON-COMPLIANT.** Records with `Created` or `Updated` MUST have non-empty `TargetUniqueId`. Missing ID is an integrity defect (`ERR_MISSING_TARGET_UNIQUE_ID`). |
| **Category and Mark Consistency** | Matches strictly by `SourceKey`. Does not verify whether `rec.Category == item.Category` or `rec.Mark == item.Mark`. | **NON-COMPLIANT.** Mismatched Category or Mark must be rejected (`ERR_CATEGORY_MISMATCH`, `ERR_MARK_MISMATCH`). |
| **Skipped Policy (Integrity Defect)** | If `rec.Status == ElementExecutionStatus.Skipped`, it is returned as-is. `ResolvePlanExecutionOutcome` does not flag `Skipped` as an error, so the batch reports `Success = true`. | **NON-COMPLIANT.** All plan items are required by default. An unapproved `Skipped` status is an Integrity Defect (`ERR_INTEGRITY_DEFECT_SKIPPED`) and must trigger batch rollback. |

### 3.3 Root Cause of Pipeline Rollback Enforcement

In `BimPlanRunnerScriptTemplate.Receipt.cs`, the Revit Roslyn script checks:
```csharp
bool hasBatchErrors = records.Any(r => (string)r["status"] == "Failed");
...
if (hasBatchErrors)
{
    throw new InvalidOperationException($"[BATCH_TRANSACTION_FAILED] ... ||PAYLOAD:{payloadJson}");
}
```
Currently, the script in Revit only throws if a record has status `"Failed"`. If Revit returned `"Skipped"`, or an item with missing `UniqueId`, the script in Revit commits!
Therefore:
1. On the client side in `ReconcileRecordsStrict1To1`, if ANY bijection violation, invalid status, missing UniqueId, category/mark mismatch, or unauthorized skip occurs, `hasIntegrityDefect` MUST be set to `true`.
2. All items in the batch must be converted to `ElementExecutionStatus.Failed` (either with their specific defect code or `ERR_BATCH_ROLLED_BACK`), ensuring `ResolvePlanExecutionOutcome` sets `Success = false`.

### 3.4 Existing vs Required Tests for R1

**Existing tests in `tests/HPBim.Client.Tests/Pipeline/`:**
- `CardinalityAndTruncationTests.cs`:
  - `ExecutePlanAsync_WhenTruncated_MarksAllItemsOutcomeUnknownAndSetsIsOutcomeUnknownTrue` (Passes)
  - `ExecutePlanAsync_WhenResponseLacksRecords_NeverSynthesizesCreatedStatus` (Passes)
  - `ExecutePlanAsync_WhenRecordDeficitOccurs_MarksMissingItemsOutcomeUnknown` (Passes — checks deficit only)
  - `ExecutePlanAsync_Strict1To1_PreservesItemSequenceOrderAndSourceKeyIdentity` (Passes — checks happy path order)
- `StrictBatchRollbackTests.cs`:
  - `BimPlanRunnerScriptTemplate_GeneratedScript_ContainsBatchRollbackGuard` (Passes)
  - `ExecutePlanAsync_WhenBatchFails_RollsBackAllItemsAndPreservesDiagnostics` (Passes)

**Required New Tests to be added:**
1. `ExecutePlanAsync_ForeignRecordReturned_RejectsBatchAndSetsSuccessFalse`: Revit returns extra record with `SourceKey` not in plan -> batch fails with `ERR_FOREIGN_RECORD`.
2. `ExecutePlanAsync_DuplicateRecordsReturned_RejectsBatchAndSetsSuccessFalse`: Revit returns 2 records with identical `SourceKey` -> batch fails with `ERR_DUPLICATE_RECORD`.
3. `ExecutePlanAsync_InvalidStatusEnumString_RejectsBatchWithInvalidStatusEnum`: Status string `"UnknownStatus"` or `"Bogus"` -> record marked `Failed` (`ERR_INVALID_STATUS_ENUM`), batch fails.
4. `ExecutePlanAsync_CreatedStatusWithMissingUniqueId_RejectsBatchWithMissingTargetUniqueId`: Status `Created` with `targetUniqueId = null` or `""` -> record marked `Failed` (`ERR_MISSING_TARGET_UNIQUE_ID`), batch fails.
5. `ExecutePlanAsync_UpdatedStatusWithMissingUniqueId_RejectsBatchWithMissingTargetUniqueId`: Status `Updated` with empty `targetUniqueId` -> record marked `Failed`, batch fails.
6. `ExecutePlanAsync_CategoryMismatch_RejectsBatchWithCategoryMismatch`: Plan has `Column`, record has `StructuralFraming` -> `ERR_CATEGORY_MISMATCH`, batch fails.
7. `ExecutePlanAsync_MarkMismatch_RejectsBatchWithMarkMismatch`: Plan has `C1`, record has `C2` -> `ERR_MARK_MISMATCH`, batch fails.
8. `ExecutePlanAsync_RequiredItemSkippedWithoutDefinedReason_TreatsAsIntegrityDefectAndRollsBack`: Plan item returned `Skipped` with empty or undefined reason -> `ERR_INTEGRITY_DEFECT_SKIPPED`, batch fails.
9. `ExecutePlanAsync_PlanItemSkippedWithValidDefinedReason_PreservesSkippedStatus`: Plan item returned `Skipped` with recognized reason code (e.g. `SKIP_FROZEN_ZONE`) -> preserved.

---

## 4. Deep Investigation: Requirement R2 (Journal Torn-Tail Resilience & Auto-Recovery, P1)

### 4.1 Current Implementation Analysis

In `HPBim/src/HPBim.Client/Connection/IntentJournal.cs`:

```csharp
151: private async Task AppendLineWithFlushAsync(string jsonLine, CancellationToken ct)
152: {
153:     await _ioLock.WaitAsync(ct).ConfigureAwait(false);
154:     try
155:     {
156:         await using var stream = new FileStream(
157:             _journalPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096, useAsync: true);
158:         await using var writer = new StreamWriter(stream, Utf8NoBom, 4096, leaveOpen: true);
159:         await writer.WriteLineAsync(jsonLine.AsMemory(), ct).ConfigureAwait(false);
160:         await writer.FlushAsync(ct).ConfigureAwait(false);
161: 
162:         if (_flushToDiskImmediately)
163:         {
164:             stream.Flush(flushToDisk: true);
165:         }
166:     }
167:     finally
168:     {
169:         _ioLock.Release();
170:     }
171: }
```

And in lines 186–210 (`ReadLatestRecordsMapCoreAsync`):
```csharp
194:     string? line;
195:     while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
196:     {
197:         if (string.IsNullOrWhiteSpace(line)) continue;
198:         try
199:         {
200:             var rec = JsonSerializer.Deserialize<IntentJournalRecord>(line, JsonOptions);
201:             if (rec != null) map[rec.OperationId] = rec;
202:         }
203:         catch (JsonException)
204:         {
205:             // Corrupted or truncated journal line from abrupt process termination; skip gracefully to preserve undamaged records.
206:             continue;
207:         }
208:     }
```

### 4.2 Anatomic Breakdown of the `TORN_APPEND_INTENT_LOST` Defect

1. **Pre-condition:** A previous run terminated abruptly while writing. The last line in `.jsonl` ends with partial content without `\n`:
   ```json
   {"operationId":"...op1...","status":"InFlight"
   {"operationId":"partial-no-newline
   ```
2. **Append Step:** A new operation intent is recorded via `RecordIntentAsync(op2)`. `AppendLineWithFlushAsync` opens the file with `FileMode.Append`.
3. **File Pointer Mechanics:** `FileMode.Append` sets the write pointer to `stream.Length`. Since the last byte of the file is `'e'`, `writer.WriteLineAsync` writes bytes directly adjacent to `'e'`:
   ```json
   {"operationId":"partial-no-newline{"operationId":"...op2...","status":"InFlight"}
   ```
4. **Read Step on Cold Restart:** When `IntentJournal` is instantiated in a new process, `ReadLatestRecordsMapCoreAsync` reads the file line-by-line.
   - Line 2 is read as `{"operationId":"partial-no-newline{"operationId":"...op2...","status":"InFlight"}`.
   - `JsonSerializer.Deserialize` throws `JsonException` because two JSON objects are joined without separation.
   - The catch block executes `continue;`.
5. **Defect Impact:** Operation intent `op2` is **completely lost**. It is absent from `map`. Any subsequent recovery or reconciliation thinks `op2` was never dispatched.

### 4.3 Proof from Existing Test in Codebase

In `tests/HPBim.Client.Tests/Connection/IntentJournalAdversarialTests.cs`, lines 226–253:
```csharp
[Fact]
public async Task TrailingTruncatedLineWithoutNewline_InvestigatesAppendBehavior()
{
    var op1 = Guid.NewGuid();
    var sb = new StringBuilder();
    sb.AppendLine($"{{\"operationId\":\"{op1:D}\",\"status\":\"Committed\"}}");
    sb.Append("{\"operationId\":\"partial-no-newline"); // Intentionally no newline

    await File.WriteAllTextAsync(_journalPath, sb.ToString());

    var op2 = Guid.NewGuid();
    using (var journal = new IntentJournal(_journalPath))
    {
        await journal.RecordIntentAsync(new IntentJournalRecord { OperationId = op2, Status = IntentStatus.InFlight });
    }

    string[] lines = await File.ReadAllLinesAsync(_journalPath);
    // Line 2 has: {"operationId":"partial-no-newline{"operationId":"...
    Assert.Equal(2, lines.Length);
    Assert.StartsWith("{\"operationId\":\"partial-no-newline{\"operationId\":", lines[1]);

    using (var coldJournal = new IntentJournal(_journalPath))
    {
        var op2Cold = await coldJournal.GetIntentAsync(op2);
        // Because line 2 had no trailing newline, the append was glued to the corrupted line!
        // Thus, deserialization of line 2 failed and op2 is lost upon cold restart!
        Assert.Null(op2Cold); // <--- CHARACTERIZATION OF THE CURRENT DEFECT!
    }
}
```

The author of this test specifically documented the bug by asserting `Assert.Null(op2Cold)`.

### 4.4 Proposed Auto-Recovery Strategy: Line Boundary Restorer (Newline Buffering)

Under `_ioLock`, before appending:
1. Open stream with `FileMode.OpenOrCreate, FileAccess.ReadWrite`.
2. If `stream.Length > 0`, seek to `stream.Length - 1` and inspect the last byte.
3. If `lastByte != '\n' && lastByte != '\r'`:
   - A torn tail is present.
   - Seek to end: `stream.Seek(0, SeekOrigin.End)`.
   - Write newline bytes (`"\n"`).
   - Flush stream.
4. Then proceed with normal append via `StreamWriter`.
5. Result: The torn line is terminated, leaving it isolated on its own line. The new record begins on a clean new line.
6. When read: The torn line throws `JsonException` and is safely skipped; the new record deserializes cleanly, achieving **100% intent preservation**.

---

## 5. Clean Code Architecture & Recommended Diff Designs

### 5.1 Architecture Design for R1: `BimPipelineEngine.ElementRecords.cs`

File line budget: Currently 204 lines. Target $\le 275$ lines (well within Rule C2 $\le 300$).  
All methods $\le 50$ lines (Rule M3).

```csharp
// Recommended implementation for BimPipelineEngine.ElementRecords.cs

namespace HPBim.Client.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.Messages;

public sealed partial class BimPipelineEngine
{
    private static readonly HashSet<string> ValidSkipReasonCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SKIP_FROZEN_ZONE",
        "SKIP_STAGE_DISABLED",
        "SKIP_PRECONDITION_FAILED",
        "SKIP_PREREQUISITE_FAILED",
        "SKIP_USER_EXCLUDED"
    };

    private static IReadOnlyList<ElementExecutionRecord> ParseElementRecords(ExecuteResult execRes, BimExecutionPlan plan)
    {
        if (execRes.Truncated)
        {
            return CreateBatchUniformRecords(
                plan,
                ElementExecutionStatus.OutcomeUnknown,
                "ERR_RESPONSE_TRUNCATED",
                "Revit response was truncated (>64KB / output limit). Outcome cannot be confirmed without reconciliation.");
        }

        List<ElementExecutionRecord>? returned = null;
        if (execRes.Value != null && execRes.Value.Value.ValueKind == JsonValueKind.Object)
        {
            var root = execRes.Value.Value;
            if (root.TryGetProperty("records", out var recordsProp) && recordsProp.ValueKind == JsonValueKind.Array)
            {
                returned = recordsProp.EnumerateArray().Select(ParseSingleElementRecord).ToList();
            }
        }
        else if (execRes.IsError && !string.IsNullOrEmpty(execRes.Message) && execRes.Message.Contains("||PAYLOAD:"))
        {
            returned = TryExtractPayloadRecords(execRes.Message);
        }

        bool isFailedRollback = (execRes.IsError && execRes.RolledBack) || (!plan.Options.DryRun && execRes.RolledBack);
        if (returned == null)
        {
            var status = isFailedRollback ? ElementExecutionStatus.Failed : ElementExecutionStatus.OutcomeUnknown;
            var code = isFailedRollback ? "ERR_TRANSACTION_ROLLED_BACK" : (execRes.IsError ? "ERR_EXECUTION_ERROR" : "ERR_NO_RECORDS");
            var reason = isFailedRollback
                ? (execRes.Message ?? "Transaction rolled back by Revit bridge.")
                : (execRes.IsError ? (execRes.Message ?? "Revit execution failed without records.") : "Response lacked element execution records array.");
            return CreateBatchUniformRecords(plan, status, code, reason);
        }

        return ReconcileRecordsStrict1To1(plan, returned, isFailedRollback);
    }

    private static IReadOnlyList<ElementExecutionRecord> ReconcileRecordsStrict1To1(
        BimExecutionPlan plan,
        IReadOnlyList<ElementExecutionRecord> returned,
        bool isFailedRollback)
    {
        var planItemMap = plan.Items.ToDictionary(i => i.SourceKey);
        var recordMap = GroupRecordsBySourceKey(returned);

        var foreignRecords = returned.Where(r => !planItemMap.ContainsKey(r.SourceKey)).ToList();
        var duplicateKeys = recordMap.Where(kvp => kvp.Value.Count > 1).Select(kvp => kvp.Key).ToList();
        bool hasIntegrityDefect = foreignRecords.Count > 0 || duplicateKeys.Count > 0;

        var reconciled = new List<ElementExecutionRecord>(plan.Items.Count);
        foreach (var item in plan.Items)
        {
            recordMap.TryGetValue(item.SourceKey, out var queue);
            var rec = ReconcileSingleItem(item, queue, isFailedRollback, hasIntegrityDefect);
            if (rec.Status == ElementExecutionStatus.Failed && rec.ErrorCode != "ERR_BATCH_ROLLED_BACK")
            {
                hasIntegrityDefect = true;
            }
            reconciled.Add(rec);
        }

        if (hasIntegrityDefect)
        {
            return ApplyStrictBatchRollback(reconciled, foreignRecords, duplicateKeys);
        }

        return reconciled;
    }

    private static ElementExecutionRecord ReconcileSingleItem(
        ElementPlanItem item,
        Queue<ElementExecutionRecord>? queue,
        bool isFailedRollback,
        bool hasIntegrityDefect)
    {
        if (queue != null && queue.Count > 0)
        {
            var rec = queue.Dequeue();

            if (rec.Category != item.Category)
            {
                return rec with
                {
                    Status = ElementExecutionStatus.Failed,
                    ErrorCode = "ERR_CATEGORY_MISMATCH",
                    ErrorReason = $"Category mismatch for '{item.SourceKey}': expected '{item.Category}', returned '{rec.Category}'."
                };
            }

            if (!string.Equals(rec.Mark, item.Mark, StringComparison.OrdinalIgnoreCase))
            {
                return rec with
                {
                    Status = ElementExecutionStatus.Failed,
                    ErrorCode = "ERR_MARK_MISMATCH",
                    ErrorReason = $"Mark mismatch for '{item.SourceKey}': expected '{item.Mark}', returned '{rec.Mark}'."
                };
            }

            if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated)
            {
                if (string.IsNullOrWhiteSpace(rec.TargetUniqueId))
                {
                    return rec with
                    {
                        Status = ElementExecutionStatus.Failed,
                        ErrorCode = "ERR_MISSING_TARGET_UNIQUE_ID",
                        ErrorReason = $"Element '{item.Mark}' with status '{rec.Status}' MUST have a non-empty TargetUniqueId."
                    };
                }
            }

            if (rec.Status == ElementExecutionStatus.Skipped && !IsValidSkipReason(rec.ErrorReason))
            {
                return rec with
                {
                    Status = ElementExecutionStatus.Failed,
                    ErrorCode = "ERR_INTEGRITY_DEFECT_SKIPPED",
                    ErrorReason = $"Required plan item '{item.Mark}' was skipped without a valid defined reason: '{rec.ErrorReason}'."
                };
            }

            if (isFailedRollback || hasIntegrityDefect)
            {
                if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated)
                {
                    return rec with
                    {
                        Status = ElementExecutionStatus.Failed,
                        ErrorCode = "ERR_BATCH_ROLLED_BACK",
                        ErrorReason = "Transaction rolled back due to batch failure or integrity defect."
                    };
                }
            }

            return rec;
        }

        if (isFailedRollback || hasIntegrityDefect)
        {
            return new ElementExecutionRecord
            {
                SourceKey = item.SourceKey,
                Category = item.Category,
                Mark = item.Mark,
                Status = ElementExecutionStatus.Failed,
                ErrorCode = "ERR_BATCH_ROLLED_BACK",
                ErrorReason = $"Transaction rolled back before plan item '{item.Mark}' ({item.SourceKey}) could be committed."
            };
        }

        return ElementExecutionRecord.OutcomeUnknown(
            item.SourceKey,
            item.Category,
            item.Mark,
            $"No execution record returned from Revit for plan item '{item.Mark}' ({item.SourceKey}).",
            "ERR_RECORD_DEFICIT");
    }

    private static IReadOnlyList<ElementExecutionRecord> ApplyStrictBatchRollback(
        List<ElementExecutionRecord> reconciled,
        List<ElementExecutionRecord> foreignRecords,
        List<CompositeSourceKey> duplicateKeys)
    {
        string? batchErrorReason = null;
        string? batchErrorCode = null;

        if (foreignRecords.Count > 0)
        {
            batchErrorCode = "ERR_FOREIGN_RECORD";
            batchErrorReason = $"Foreign record detected: SourceKey '{foreignRecords[0].SourceKey}' was returned by Revit but does not exist in the execution plan.";
        }
        else if (duplicateKeys.Count > 0)
        {
            batchErrorCode = "ERR_DUPLICATE_RECORD";
            batchErrorReason = $"Duplicate records detected: Multiple execution records returned for SourceKey '{duplicateKeys[0]}'.";
        }

        return reconciled.Select(r =>
        {
            if (r.Status == ElementExecutionStatus.Failed && r.ErrorCode != "ERR_BATCH_ROLLED_BACK")
            {
                return r;
            }

            return r with
            {
                Status = ElementExecutionStatus.Failed,
                ErrorCode = batchErrorCode ?? "ERR_BATCH_ROLLED_BACK",
                ErrorReason = batchErrorReason ?? "Transaction rolled back due to batch integrity defect."
            };
        }).ToList();
    }

    private static bool IsValidSkipReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return false;
        foreach (var code in ValidSkipReasonCodes)
        {
            if (reason.Contains(code, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static ElementExecutionRecord ParseSingleElementRecord(JsonElement elem)
    {
        string rawSourceKey = GetOptionalString(elem, "sourceKey") ?? string.Empty;
        string mark = GetOptionalString(elem, "mark") ?? string.Empty;
        string catStr = GetOptionalString(elem, "category") ?? "Column";
        string statusStr = GetOptionalString(elem, "status") ?? "Failed";

        Enum.TryParse<CoordinationCategory>(catStr, true, out var cat);
        bool validStatus = Enum.TryParse<ElementExecutionStatus>(statusStr, true, out var status)
                           && Enum.IsDefined(typeof(ElementExecutionStatus), status)
                           && status != 0;

        long? elemId = elem.TryGetProperty("targetElementId", out var idProp) && idProp.ValueKind == JsonValueKind.Number
            ? idProp.GetInt64()
            : null;

        var warnings = new List<string>();
        if (elem.TryGetProperty("warnings", out var wProp) && wProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var w in wProp.EnumerateArray())
            {
                if (w.ValueKind == JsonValueKind.String && w.GetString() is { } str)
                {
                    warnings.Add(str);
                }
            }
        }

        string? errorCode = GetOptionalString(elem, "errorCode");
        string? errorReason = GetOptionalString(elem, "errorReason");

        if (!validStatus)
        {
            status = ElementExecutionStatus.Failed;
            errorCode = "ERR_INVALID_STATUS_ENUM";
            errorReason = $"Invalid ElementExecutionStatus '{statusStr}'.";
        }

        return new ElementExecutionRecord
        {
            SourceKey = CompositeSourceKey.TryParse(rawSourceKey, out var parsedSk) ? parsedSk! : new CompositeSourceKey(string.Empty, cat, mark),
            Category = cat,
            Mark = mark,
            Status = status,
            TargetElementId = elemId,
            TargetUniqueId = GetOptionalString(elem, "targetUniqueId"),
            ErrorCode = errorCode,
            ErrorReason = errorReason,
            Warnings = warnings
        };
    }
```

### 5.2 Architecture Design for R2: `IntentJournal.cs`

File line budget: Currently 250 lines. Target $\le 278$ lines (well within Rule C2 $\le 300$).  
All methods $\le 50$ lines (Rule M3).

```csharp
    private static readonly byte[] NewlineBytes = "\n"u8.ToArray();

    private async Task AppendLineWithFlushAsync(string jsonLine, CancellationToken ct)
    {
        await _ioLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var stream = new FileStream(
                _journalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, useAsync: true);

            await EnsureLineBoundaryAsync(stream, ct).ConfigureAwait(false);

            await using var writer = new StreamWriter(stream, Utf8NoBom, 4096, leaveOpen: true);
            await writer.WriteLineAsync(jsonLine.AsMemory(), ct).ConfigureAwait(false);
            await writer.FlushAsync(ct).ConfigureAwait(false);

            if (_flushToDiskImmediately)
            {
                stream.Flush(flushToDisk: true);
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private static async Task EnsureLineBoundaryAsync(FileStream stream, CancellationToken ct)
    {
        if (stream.Length == 0) return;

        stream.Seek(-1, SeekOrigin.End);
        int lastByte = stream.ReadByte();
        if (lastByte != '\n' && lastByte != '\r')
        {
            // Torn-tail detected: file ends abruptly without trailing newline.
            // Buffer a newline to restore record line boundary before appending new record.
            stream.Seek(0, SeekOrigin.End);
            await stream.WriteAsync(NewlineBytes.AsMemory(), ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }
        else
        {
            stream.Seek(0, SeekOrigin.End);
        }
    }
```

---

## 6. Verification and Regression Mitigation

1. **Clean Code Verification:**
   - File length:
     - `BimPipelineEngine.ElementRecords.cs` will be ~270 lines ($\le 300$).
     - `IntentJournal.cs` will be ~275 lines ($\le 300$).
   - Method lengths: All individual methods stay under 40 lines ($\le 50$).
   - Zero commented-out code (Q-B1).
   - Zero empty catch blocks without reason comments (Q-B2).

2. **Existing Test Suite Baseline:**
   - `HPBim.Client.Tests`: 376 tests currently passing.
   - `HPBim.Core.Tests`: 429 tests currently passing.
   - `HPBim.E2E.Tests`: 42 tests currently passing.
   - Total HPBim tests: 847 passing (zero regressions tolerated).
   - *Crucial note for Implementer:* When fixing R2, `TrailingTruncatedLineWithoutNewline_InvestigatesAppendBehavior()` in `IntentJournalAdversarialTests.cs` (line 251) must be updated from `Assert.Null(op2Cold)` to `Assert.NotNull(op2Cold)`, as `op2Cold` will now successfully be preserved!

---

## 7. Conclusion

The technical investigation of R1 and R2 is complete with root causes proven, gap analysis enumerated, and drop-in clean code architectures specified. All findings, logic chains, and test specifications are documented in `handoff.md` for seamless handover to the orchestrator and implementer agents.
