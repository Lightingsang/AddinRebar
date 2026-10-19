# Architectural & Line Budget Analysis: `BimPipelineEngine.ElementRecords.cs`

**Milestone**: M1 Iteration 2 (HPBim Wave 2 Remediation)  
**Agent**: `explorer_m1_it2_3`  
**Target File**: `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs`  
**Date**: 2026-10-08  

---

## 1. Executive Summary

A comprehensive forensic examination of `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` was performed. The current implementation spans **299 lines**, which sits dangerously close to the 300-line repository ceiling (Rule C2). Furthermore, method `ReconcileSingleItem` spans **72 lines**, directly violating Clean Code Rule M3 ($\le 50$ lines), and method `ParseSingleElementRecord` (38 lines) and `ParseElementRecords` (38 lines) exceed the target budget of $\le 35$ lines. In addition, line 167 contains a conditional defect that permits `Created` and `Updated` records with omitted `targetUniqueId` properties to bypass validation.

This report establishes a fully refactored, modular layout that achieves:
1. **File Line Count**: Exactly **274 lines** (comfortable margin under the $\le 280$ line mandate, and 26 lines under the 300-line hard ceiling).
2. **Method Length**: Every single method is $\le 30$ lines (comfortable margin under the $\le 35$ line mandate, and well below the 50-line Rule M3 limit). Maximum method length across the entire file is **30 lines**.
3. **Clean Code & Quality**: Zero commented-out code (Q-B1), zero empty catches (Q-B2), zero heuristic fallbacks.
4. **Defect Elimination**: Fixes the `TargetUniqueId` null-bypass bug in `ValidateReturnedRecord`.

---

## 2. Current Method Inventory & Line Counts

Below is the line-by-line audit of all 12 existing methods in `BimPipelineEngine.ElementRecords.cs` (Total: 299 lines):

| # | Method Name | Line Range | Current Lines | Budget Target ($\le 35$) | Governance Limit ($\le 50$) | Status |
|---|---|:---:|:---:|:---:|:---:|:---:|
| 1 | `ParseElementRecords` | 18–55 | 38 | $\le 35$ | $\le 50$ | **EXCEEDS TARGET** |
| 2 | `TryExtractPayloadRecords` | 57–74 | 18 | $\le 35$ | $\le 50$ | PASS |
| 3 | `CreateBatchUniformRecords` | 76–82 | 7 | $\le 35$ | $\le 50$ | PASS |
| 4 | `ReconcileRecordsStrict1To1` | 84–116 | 33 | $\le 35$ | $\le 50$ | PASS ($\le 35$) |
| 5 | `IsIntegrityDefect` | 118–121 | 4 | $\le 35$ | $\le 50$ | PASS |
| 6 | `GroupRecordsBySourceKey` | 123–138 | 16 | $\le 35$ | $\le 50$ | PASS |
| 7 | `ReconcileSingleItem` | 140–211 | **72** | $\le 35$ | $\le 50$ | **VIOLATION (M3 & TARGET)** |
| 8 | `ApplyStrictBatchRollback` | 213–246 | 34 | $\le 35$ | $\le 50$ | PASS (Near Limit) |
| 9 | `IsSkipAllowed` | 248–249 | 2 | $\le 35$ | $\le 50$ | PASS |
| 10 | `IsValidSkipReason` | 251–255 | 5 | $\le 35$ | $\le 50$ | PASS |
| 11 | `ParseSingleElementRecord` | 257–294 | 38 | $\le 35$ | $\le 50$ | **EXCEEDS TARGET** |
| 12 | `GetOptionalString` | 296–297 | 2 | $\le 35$ | $\le 50$ | PASS |

### Non-Method Line Breakdown:
- Usings and file header: lines 1–12 (12 lines)
- Static field `ValidSkipReasonCodes`: lines 13–16 (4 lines)
- Inter-method spacing: 14 blank lines
- Total: 299 lines.

---

## 3. Structural Decomposition Strategy

To bring all methods under 35 lines while reducing the total file size from 299 to 274 lines, the following cohesive extractions are designed:

### 3.1 Decomposition of `ReconcileSingleItem` (72 lines $\rightarrow$ 5 lines)
`ReconcileSingleItem` previously combined queue retrieval, category validation, mark validation, identifier checking, skip allowance checking, failed rollback cascading, and deficit outcome-unknown record creation into one bloated block.

We decompose this into three focused, single-responsibility methods:
1. **`ReconcileSingleItem` (5 lines)**: Dispatches queue state. If an item is in the queue, calls `ValidateReturnedRecord`; otherwise calls `CreateMissingItemRecord`.
2. **`ValidateReturnedRecord` (30 lines)**: Performs 5-point semantic validation:
   - Category matching (`ERR_CATEGORY_MISMATCH`)
   - Mark matching (`ERR_MARK_MISMATCH`)
   - TargetUniqueId validation (`ERR_MISSING_TARGET_UNIQUE_ID`) with bug fix
   - Skip rule verification (`ERR_INTEGRITY_DEFECT_SKIPPED`)
   - Cascade rollback handling (`ERR_BATCH_ROLLED_BACK`)
3. **`CreateMissingItemRecord` (12 lines)**: Builds uniform failure record or `OutcomeUnknown` with `ERR_RECORD_DEFICIT` when queue is empty.

### 3.2 Decomposition of `ParseElementRecords` (38 lines $\rightarrow$ 22 lines)
Extract the wire payload parsing branch into:
- **`ExtractReturnedRecords` (16 lines)**: Handles extraction from JSON `Value.records` array or fallback `TryExtractPayloadRecords`.
- `ParseElementRecords` (22 lines) retains truncation checking, uniform error record synthesis, and delegation to `ReconcileRecordsStrict1To1`.

### 3.3 Decomposition of `ParseSingleElementRecord` (38 lines $\rightarrow$ 18 lines)
Extract nested parsing and validation concerns:
- **`ParseStatus` (11 lines)**: Validates enum string value against defined `ElementExecutionStatus` values. Returns tuple `(Status, Code, Reason)`.
- **`ExtractWarnings` (10 lines)**: Safely extracts string array from `warnings` JSON property.
- `ParseSingleElementRecord` (18 lines) constructs the `ElementExecutionRecord` cleanly.

### 3.4 Decomposition of `ApplyStrictBatchRollback` (34 lines $\rightarrow$ 19 lines)
Extract batch error identification:
- **`DetectBatchDefect` (15 lines)**: Evaluates foreign records and duplicate keys, returning `(batchCode, batchReason)`.
- `ApplyStrictBatchRollback` (19 lines) applies uniform rollback projection while preserving original defect error codes.

---

## 4. Defect Analysis: TargetUniqueId Null Bypass

### Root Cause
In `BimPipelineEngine.ElementRecords.cs:167`:
```csharp
if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated && rec.TargetUniqueId != null && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
```
In `ParseSingleElementRecord`:
```csharp
string? targetUid = elem.TryGetProperty("targetUniqueId", out var p)
    ? (p.ValueKind == JsonValueKind.String ? p.GetString() : string.Empty) : null;
```
When a record has status `Created` or `Updated`, but the JSON payload omits `"targetUniqueId"` altogether:
1. `elem.TryGetProperty` returns `false`.
2. `targetUid` is assigned `null`.
3. In line 167, `rec.TargetUniqueId != null` evaluates to `false`.
4. The condition short-circuits, completely bypassing validation.
5. The element is returned with `Status = Created` and `TargetUniqueId = null`.
6. Downstream ledger synchronization drops the element (`BimCoordinationLedgerService.Mappings.cs:93`), breaking idempotency.

### Remediation
Change the condition in `ValidateReturnedRecord` to:
```csharp
if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
```
Since `string.IsNullOrWhiteSpace(null)` evaluates to `true`, omitted properties, `null`, `""`, and whitespace-only identifiers are all intercepted as `ERR_MISSING_TARGET_UNIQUE_ID`. Non-creation statuses (`NoOp`, `Skipped`) remain unaffected.

---

## 5. Proposed Method Inventory & Line Count Comparison

| # | Method Name | Responsibility | Proposed Line Range | Proposed Lines | Target ($\le 35$) | Margin Under 35 |
|---|---|---|:---:|:---:|:---:|:---:|
| 1 | `ParseElementRecords` | Entry point & error dispatch | 18–39 | 22 | $\le 35$ | -13 lines |
| 2 | `ExtractReturnedRecords` | Json/payload records extraction | 41–56 | 16 | $\le 35$ | -19 lines |
| 3 | `TryExtractPayloadRecords` | Fallback payload parser | 58–75 | 18 | $\le 35$ | -17 lines |
| 4 | `CreateBatchUniformRecords` | Uniform status record builder | 77–83 | 7 | $\le 35$ | -28 lines |
| 5 | `ReconcileRecordsStrict1To1` | 1:1 Bijection coordinator | 85–112 | 28 | $\le 35$ | -7 lines |
| 6 | `IsIntegrityDefect` | Defect predicate | 114–117 | 4 | $\le 35$ | -31 lines |
| 7 | `GroupRecordsBySourceKey` | FIFO queue grouping | 119–133 | 15 | $\le 35$ | -20 lines |
| 8 | `ReconcileSingleItem` | Item dequeue & dispatch | 135–139 | 5 | $\le 35$ | -30 lines |
| 9 | `ValidateReturnedRecord` | 5-point semantic validation | 141–170 | 30 | $\le 35$ | -5 lines |
| 10 | `CreateMissingItemRecord` | Deficit/rollback record builder | 172–183 | 12 | $\le 35$ | -23 lines |
| 11 | `ApplyStrictBatchRollback` | Batch rollback projector | 185–203 | 19 | $\le 35$ | -16 lines |
| 12 | `DetectBatchDefect` | Batch defect code detector | 205–219 | 15 | $\le 35$ | -20 lines |
| 13 | `IsSkipAllowed` | Skip policy checker | 221–222 | 2 | $\le 35$ | -33 lines |
| 14 | `IsValidSkipReason` | Approved skip reason matcher | 224–228 | 5 | $\le 35$ | -30 lines |
| 15 | `ParseSingleElementRecord` | DTO constructor | 230–247 | 18 | $\le 35$ | -17 lines |
| 16 | `ParseStatus` | Status enum validator | 249–259 | 11 | $\le 35$ | -24 lines |
| 17 | `ExtractWarnings` | Warnings array extractor | 261–270 | 10 | $\le 35$ | -25 lines |
| 18 | `GetOptionalString` | Property accessor helper | 272–273 | 2 | $\le 35$ | -33 lines |

**Summary Metrics**:
- **Total file lines**: **274 lines** (Mandate: $\le 280$ lines).
- **Longest method**: **30 lines** (`ValidateReturnedRecord`, Mandate: $\le 35$ lines).
- **Average method length**: **13.3 lines**.
- **Methods exceeding 35 lines**: **0** (100% compliant).

---

## 6. Complete Proposed Source Code

The complete proposed code for `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` (274 lines) is formatted below:

```csharp
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
        "SKIP_FROZEN_ZONE", "SKIP_STAGE_DISABLED", "SKIP_PRECONDITION_FAILED", "SKIP_PREREQUISITE_FAILED", "SKIP_USER_EXCLUDED"
    };

    private static IReadOnlyList<ElementExecutionRecord> ParseElementRecords(ExecuteResult execRes, BimExecutionPlan plan)
    {
        if (execRes.Truncated)
        {
            return CreateBatchUniformRecords(plan, ElementExecutionStatus.OutcomeUnknown, "ERR_RESPONSE_TRUNCATED",
                "Revit response was truncated (>64KB / output limit). Outcome cannot be confirmed without reconciliation.");
        }

        bool isFailedRollback = (execRes.IsError && execRes.RolledBack) || (!plan.Options.DryRun && execRes.RolledBack);
        var returned = ExtractReturnedRecords(execRes);
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

    private static List<ElementExecutionRecord>? ExtractReturnedRecords(ExecuteResult execRes)
    {
        if (execRes.Value != null && execRes.Value.Value.ValueKind == JsonValueKind.Object)
        {
            var root = execRes.Value.Value;
            if (root.TryGetProperty("records", out var recordsProp) && recordsProp.ValueKind == JsonValueKind.Array)
            {
                return recordsProp.EnumerateArray().Select(ParseSingleElementRecord).ToList();
            }
        }
        else if (execRes.IsError && !string.IsNullOrEmpty(execRes.Message) && execRes.Message.Contains("||PAYLOAD:"))
        {
            return TryExtractPayloadRecords(execRes.Message);
        }
        return null;
    }

    private static List<ElementExecutionRecord>? TryExtractPayloadRecords(string message)
    {
        try
        {
            int payloadIdx = message.IndexOf("||PAYLOAD:", StringComparison.Ordinal) + 10;
            string payloadJson = message.Substring(payloadIdx).Trim();
            using var payloadDoc = JsonDocument.Parse(payloadJson);
            if (payloadDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return payloadDoc.RootElement.EnumerateArray().Select(ParseSingleElementRecord).ToList();
            }
        }
        catch (JsonException)
        {
            // Expected when error message payload is truncated or malformed; fall back to uniform records
        }
        return null;
    }

    private static IReadOnlyList<ElementExecutionRecord> CreateBatchUniformRecords(
        BimExecutionPlan plan, ElementExecutionStatus status, string errorCode, string errorReason) =>
        plan.Items.Select(item => new ElementExecutionRecord
        {
            SourceKey = item.SourceKey, Category = item.Category, Mark = item.Mark,
            Status = status, ErrorCode = errorCode, ErrorReason = errorReason
        }).ToList();

    private static IReadOnlyList<ElementExecutionRecord> ReconcileRecordsStrict1To1(
        BimExecutionPlan plan, IReadOnlyList<ElementExecutionRecord> returned, bool isFailedRollback)
    {
        var planKeyCounts = plan.Items.GroupBy(i => i.SourceKey).ToDictionary(g => g.Key, g => g.Count());
        var recordMap = GroupRecordsBySourceKey(returned);

        var foreignRecords = returned.Where(r => !planKeyCounts.ContainsKey(r.SourceKey)).ToList();
        var duplicateKeys = recordMap
            .Where(kvp => kvp.Value.Count > (planKeyCounts.TryGetValue(kvp.Key, out int exp) ? exp : 0))
            .Select(kvp => kvp.Key).ToList();
        bool hasIntegrityDefect = foreignRecords.Count > 0 || duplicateKeys.Count > 0;

        var reconciled = new List<ElementExecutionRecord>(plan.Items.Count);
        foreach (var item in plan.Items)
        {
            recordMap.TryGetValue(item.SourceKey, out var queue);
            var rec = ReconcileSingleItem(item, queue, isFailedRollback);
            if (IsIntegrityDefect(rec)) hasIntegrityDefect = true;
            reconciled.Add(rec);
        }

        if (hasIntegrityDefect || isFailedRollback)
        {
            return ApplyStrictBatchRollback(reconciled, foreignRecords, duplicateKeys);
        }

        return reconciled;
    }

    private static bool IsIntegrityDefect(ElementExecutionRecord rec) =>
        rec.Status == ElementExecutionStatus.Failed && rec.ErrorCode is "ERR_FOREIGN_RECORD"
            or "ERR_DUPLICATE_RECORD" or "ERR_INVALID_STATUS_ENUM" or "ERR_MISSING_TARGET_UNIQUE_ID"
            or "ERR_CATEGORY_MISMATCH" or "ERR_MARK_MISMATCH" or "ERR_INTEGRITY_DEFECT_SKIPPED";

    private static Dictionary<CompositeSourceKey, Queue<ElementExecutionRecord>> GroupRecordsBySourceKey(
        IReadOnlyList<ElementExecutionRecord> returned)
    {
        var recordMap = new Dictionary<CompositeSourceKey, Queue<ElementExecutionRecord>>();
        foreach (var rec in returned)
        {
            if (!recordMap.TryGetValue(rec.SourceKey, out var q))
            {
                q = new Queue<ElementExecutionRecord>();
                recordMap[rec.SourceKey] = q;
            }
            q.Enqueue(rec);
        }
        return recordMap;
    }

    private static ElementExecutionRecord ReconcileSingleItem(
        ElementPlanItem item, Queue<ElementExecutionRecord>? queue, bool isFailedRollback) =>
        queue != null && queue.Count > 0
            ? ValidateReturnedRecord(item, queue.Dequeue(), isFailedRollback)
            : CreateMissingItemRecord(item, isFailedRollback);

    private static ElementExecutionRecord ValidateReturnedRecord(
        ElementPlanItem item, ElementExecutionRecord rec, bool isFailedRollback)
    {
        if (rec.Category != item.Category)
        {
            return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_CATEGORY_MISMATCH",
                ErrorReason = $"Category mismatch for '{item.SourceKey}': expected '{item.Category}', returned '{rec.Category}'." };
        }
        if (!string.Equals(rec.Mark, item.Mark, StringComparison.OrdinalIgnoreCase))
        {
            return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_MARK_MISMATCH",
                ErrorReason = $"Mark mismatch for '{item.SourceKey}': expected '{item.Mark}', returned '{rec.Mark}'." };
        }
        if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
        {
            return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_MISSING_TARGET_UNIQUE_ID",
                ErrorReason = $"Element '{item.Mark}' with status '{rec.Status}' MUST have a non-empty TargetUniqueId." };
        }
        if (rec.Status == ElementExecutionStatus.Skipped && !IsSkipAllowed(item, rec))
        {
            return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_INTEGRITY_DEFECT_SKIPPED",
                ErrorReason = $"Required plan item '{item.Mark}' was skipped without a valid defined reason: '{rec.ErrorReason ?? rec.ErrorCode}'." };
        }
        if (isFailedRollback && rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated)
        {
            return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_BATCH_ROLLED_BACK",
                ErrorReason = "Transaction rolled back due to failure in batch." };
        }
        return rec;
    }

    private static ElementExecutionRecord CreateMissingItemRecord(ElementPlanItem item, bool isFailedRollback) =>
        isFailedRollback
            ? new ElementExecutionRecord
            {
                SourceKey = item.SourceKey, Category = item.Category, Mark = item.Mark,
                Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_BATCH_ROLLED_BACK",
                ErrorReason = $"Transaction rolled back before plan item '{item.Mark}' ({item.SourceKey}) could be committed."
            }
            : ElementExecutionRecord.OutcomeUnknown(
                item.SourceKey, item.Category, item.Mark,
                $"No execution record returned from Revit for plan item '{item.Mark}' ({item.SourceKey}).",
                "ERR_RECORD_DEFICIT");

    private static IReadOnlyList<ElementExecutionRecord> ApplyStrictBatchRollback(
        List<ElementExecutionRecord> reconciled, List<ElementExecutionRecord> foreignRecords, List<CompositeSourceKey> duplicateKeys)
    {
        var (batchCode, batchReason) = DetectBatchDefect(foreignRecords, duplicateKeys);
        var firstDefect = reconciled.FirstOrDefault(r => r.Status == ElementExecutionStatus.Failed && r.ErrorCode != "ERR_BATCH_ROLLED_BACK");

        return reconciled.Select(r =>
        {
            if (r.Status == ElementExecutionStatus.Failed && r.ErrorCode != "ERR_BATCH_ROLLED_BACK" && batchCode == null) return r;
            return r with
            {
                Status = ElementExecutionStatus.Failed,
                ErrorCode = batchCode ?? "ERR_BATCH_ROLLED_BACK",
                ErrorReason = batchReason ?? (firstDefect != null
                    ? $"Transaction rolled back due to batch integrity defect: [{firstDefect.ErrorCode}] {firstDefect.ErrorReason}"
                    : "Transaction rolled back due to batch failure or integrity defect.")
            };
        }).ToList();
    }

    private static (string? Code, string? Reason) DetectBatchDefect(
        List<ElementExecutionRecord> foreign, List<CompositeSourceKey> duplicates)
    {
        if (foreign.Count > 0)
        {
            return ("ERR_FOREIGN_RECORD",
                $"Foreign record detected: SourceKey '{foreign[0].SourceKey}' was returned by Revit but does not exist in the execution plan.");
        }
        if (duplicates.Count > 0)
        {
            return ("ERR_DUPLICATE_RECORD",
                $"Duplicate records detected: Multiple execution records returned for SourceKey '{duplicates[0]}'.");
        }
        return (null, null);
    }

    private static bool IsSkipAllowed(ElementPlanItem item, ElementExecutionRecord rec) =>
        (item.Parameters.TryGetValue("SkipAllowed", out var sa) && sa is bool b && b) || IsValidSkipReason(rec.ErrorCode, rec.ErrorReason);

    private static bool IsValidSkipReason(string? code, string? reason)
    {
        bool Match(string? s) => !string.IsNullOrWhiteSpace(s) && ValidSkipReasonCodes.Any(c => s.Contains(c, StringComparison.OrdinalIgnoreCase));
        return Match(code) || Match(reason);
    }

    private static ElementExecutionRecord ParseSingleElementRecord(JsonElement elem)
    {
        string rawSourceKey = GetOptionalString(elem, "sourceKey") ?? string.Empty;
        string mark = GetOptionalString(elem, "mark") ?? string.Empty;
        Enum.TryParse<CoordinationCategory>(GetOptionalString(elem, "category") ?? "Column", true, out var cat);

        var (status, errCode, errReason) = ParseStatus(elem);
        long? elemId = elem.TryGetProperty("targetElementId", out var idProp) && idProp.ValueKind == JsonValueKind.Number ? idProp.GetInt64() : null;
        string? targetUid = elem.TryGetProperty("targetUniqueId", out var p)
            ? (p.ValueKind == JsonValueKind.String ? p.GetString() : string.Empty) : null;

        return new ElementExecutionRecord
        {
            SourceKey = CompositeSourceKey.TryParse(rawSourceKey, out var parsedSk) ? parsedSk! : new CompositeSourceKey(string.Empty, cat, mark),
            Category = cat, Mark = mark, Status = status, TargetElementId = elemId, TargetUniqueId = targetUid,
            ErrorCode = errCode, ErrorReason = errReason, Warnings = ExtractWarnings(elem)
        };
    }

    private static (ElementExecutionStatus Status, string? Code, string? Reason) ParseStatus(JsonElement elem)
    {
        string statusStr = GetOptionalString(elem, "status") ?? "Failed";
        bool valid = Enum.TryParse<ElementExecutionStatus>(statusStr, true, out var status)
                     && Enum.IsDefined(typeof(ElementExecutionStatus), status) && status != 0;
        if (!valid)
        {
            return (ElementExecutionStatus.Failed, "ERR_INVALID_STATUS_ENUM", $"Invalid ElementExecutionStatus '{statusStr}'.");
        }
        return (status, GetOptionalString(elem, "errorCode"), GetOptionalString(elem, "errorReason"));
    }

    private static List<string> ExtractWarnings(JsonElement elem)
    {
        var list = new List<string>();
        if (elem.TryGetProperty("warnings", out var w) && w.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in w.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s) list.Add(s);
        }
        return list;
    }

    private static string? GetOptionalString(JsonElement elem, string prop) =>
        elem.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}
```

---

## 7. Recommended Unit Test for Regression Prevention

To permanently prevent regression of Check 3.4 (omitted `targetUniqueId` property), the worker should add the following unit test to `HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.cs`:

```csharp
[Fact]
public async Task ExecutePlanAsync_CreatedStatusWithTargetUniqueIdPropertyOmitted_RejectsBatchWithMissingTargetUniqueId()
{
    var plan = CreatePlan((CoordinationCategory.Column, "C1"));
    var payload = new Dictionary<string, object>[]
    {
        new()
        {
            ["sourceKey"] = "[Doc]#[Column]:C1",
            ["category"] = "Column",
            ["mark"] = "C1",
            ["status"] = "Created"
            // targetUniqueId property completely omitted
        }
    };

    var engine = new BimPipelineEngine(CreatePipeClient(payload));
    var result = await engine.ExecutePlanAsync(plan);

    Assert.False(result.Success);
    var c1 = result.ElementRecords[0];
    Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
    Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c1.ErrorCode);
}
```
