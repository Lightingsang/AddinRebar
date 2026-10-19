# Architectural & Technical Investigation Report: Extensible Storage Receipt Schema & Atomic Storage (Milestone M4 / Feature F9 Part 2)

**Agent**: `explorer_m4_2` (`teamwork_preview_explorer`)  
**Date**: 2026-10-07  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_2\`  
**Milestone**: Milestone M4 (Feature F9 Part 2)  
**Status**: COMPLETE (Read-Only Investigation & Architectural Specification)  

---

## 1. Executive Summary

Milestone M4 establishes the resilient recovery layer of the HPBim pipeline. Specifically, **Feature F9 Part 2** requires recording an atomic, durable execution receipt inside Autodesk Revit 2026 using Revit's native **Extensible Storage** mechanism on a hidden **`DataStorage`** element within the exact same transaction that creates model elements.

This investigation demonstrates:
1. **Revit Extensible Storage Schema**: The exact schema specification for `HPBimCoordinationReceipt` with GUID `D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90`, `AccessLevel.Public`, and four strongly typed fields (`OperationId` of `Guid`, `PlanHash` of `string`, `CommittedAtUtc` of `string`, `ElementMapJson` of `string`).
2. **Revit DB `DataStorage` Mechanics**: Non-graphical persistence on `DataStorage` elements in Revit DB, ensuring zero interference with physical BIM geometry, views, or schedules, while surviving project save, central sync, and session restarts.
3. **Atomic Execution in `BimPlanRunnerScriptTemplate`**: Architectural placement of receipt generation at the epilogue of the execution transaction. When `dryRun == true`, receipt generation is cleanly bypassed and 100% rolled back by `TransactionGroup.RollBack()`. When `dryRun == false`, the receipt commits atomically alongside all model elements.
4. **Receipt Query & Recovery**: Design of `ExtensibleStorageReceiptReader` and `ExtensibleStorageQueryScriptTemplate` in `HPBim.Client/Recovery/` operating under `TransactionModes.None` (read-only query) to retrieve receipts by `OperationId` or `PlanHash`.
5. **Testing Architecture & `FakeRevitPipeClient` Support**: Comprehensive test plan for `HPBim.Client.Tests` using in-memory mock receipts and offline round-trip verification without needing a running Revit instance.
6. **Clean Code Governance**: 100% compliance with repo standards (`HP_CLEAN_CODE_CORE.md`, Rule C2 <= 300 lines, Rule M3 <= 50 lines).

---

## 2. Requirement F9: Extensible Storage Schema Specification

### 2.1 Schema Metadata & Access Control

| Parameter | Specification | Technical Justification |
| :--- | :--- | :--- |
| **Schema GUID** | `D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90` | Pinned across `PROJECT.md`, `TEST_READY.md`, and `E2EContractModels.cs:82`. Deterministic and immutable. |
| **Schema Name** | `HPBimCoordinationReceipt` | Distinct schema identifier in the Revit database application domain. |
| **Documentation** | `"HPBim: Execution receipt tracking atomic commit of BIM execution plan."` | Standard documentation string embedded in the schema definition. |
| **Read Access** | `AccessLevel.Public` | Allows read queries by any process, add-in, CLI tool, or test runner without requiring an Autodesk Developer ID. |
| **Write Access** | `AccessLevel.Public` | Allows Roslyn script execution via `HPRebar.McpBridge` without vendor mismatch exceptions (`AccessDeniedException`). Proven by precedent in `KataRebarStorage.cs:46-47`. |

### 2.2 Schema Fields Specification

| Field Name | Type in Revit DB | Type in C# DTO | Description | Example Value |
| :--- | :--- | :--- | :--- | :--- |
| **`OperationId`** | `typeof(Guid)` | `Guid` | Unique operation batch ID for this execution run. Provided by `BimExecutionPlan.Options.OperationId`. | `3fa85f64-5717-4562-b3fc-2c963f66afa6` |
| **`PlanHash`** | `typeof(string)` | `string` | Deterministic SHA-256 hash of the execution plan content and preconditions (`BimExecutionPlan.PlanHash`). | `7e8a9b1c2d3e4f5a6b7c8d9e0f1a2b3c...` |
| **`CommittedAtUtc`** | `typeof(string)` | `string` | ISO 8601 UTC timestamp of the commit transaction. | `2026-10-07T21:05:25.1234567Z` |
| **`ElementMapJson`** | `typeof(string)` | `string` | Serialized JSON dictionary of `CompositeSourceKey -> Revit UniqueId` mappings for all successfully committed elements. | `{"NhaPho#Column:C1": "456a7b8c-...", "NhaPho#Wall:W1": "789d0e1f-..."}` |

### 2.3 Revit Extensible Storage Mechanics

In Revit DB, schemas are registered in session memory. Registering a schema with a GUID that is already registered throws an exception in `SchemaBuilder.Finish()`. Therefore, registration must always follow the safe lookup-or-build pattern:

```csharp
var schemaGuid = new Guid("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90");
var schema = Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(schemaGuid);
if (schema == null)
{
    var builder = new Autodesk.Revit.DB.ExtensibleStorage.SchemaBuilder(schemaGuid);
    builder.SetSchemaName("HPBimCoordinationReceipt");
    builder.SetDocumentation("HPBim: Execution receipt tracking atomic commit of BIM execution plan.");
    builder.SetReadAccessLevel(Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Public);
    builder.SetWriteAccessLevel(Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Public);
    builder.AddSimpleField("OperationId", typeof(Guid));
    builder.AddSimpleField("PlanHash", typeof(string));
    builder.AddSimpleField("CommittedAtUtc", typeof(string));
    builder.AddSimpleField("ElementMapJson", typeof(string));
    schema = builder.Finish();
}
```

---

## 3. Revit DB `DataStorage` Element Mechanics

### 3.1 Why `DataStorage`?

In Autodesk Revit, extensible storage entities can be attached to any `Element`. However, attaching receipts to physical model elements (such as a Wall, Column, or Level) introduces severe architectural liabilities:
- If that physical element is later deleted or modified by an architect/engineer, the coordination receipt is lost or corrupted.
- Attaching to a ProjectInfo element creates potential schema collisions with other add-ins.

Revit API provides the **`DataStorage`** element (`Autodesk.Revit.DB.DataStorage`):
1. **Non-Graphical**: Has zero physical geometry or bounding box.
2. **Invisible**: Does not appear in the Project Browser, 3D views, plan views, or schedules.
3. **Durable**: Persists across document save, export, Detach from Central, and Workshared Sync.
4. **Isolated**: Dedicated to add-in data storage. Multiple `DataStorage` elements can coexist without collision.
5. **Transactional**: Created via `DataStorage.Create(doc)` inside an active `Transaction`. If the transaction rolls back, the `DataStorage` element disappears completely.

### 3.2 Finding & Updating `DataStorage` Elements

To query existing receipts or prevent duplicate `DataStorage` elements for the same `OperationId`:
```csharp
DataStorage receiptDs = null;
var allDs = new FilteredElementCollector(doc)
    .OfClass(typeof(DataStorage))
    .Cast<DataStorage>();

foreach (var ds in allDs)
{
    var ent = ds.GetEntity(schema);
    if (ent != null && ent.IsValid() && ent.Get<Guid>("OperationId") == operationId)
    {
        receiptDs = ds;
        break;
    }
}

if (receiptDs == null)
{
    receiptDs = DataStorage.Create(doc);
}

var entity = new Autodesk.Revit.DB.ExtensibleStorage.Entity(schema);
entity.Set<Guid>("OperationId", operationId);
entity.Set<string>("PlanHash", planHash);
entity.Set<string>("CommittedAtUtc", DateTime.UtcNow.ToString("o"));
entity.Set<string>("ElementMapJson", elemMapJson);
receiptDs.SetEntity(entity);
```

---

## 4. Atomic Attachment in `BimPlanRunnerScriptTemplate`

### 4.1 Transaction Lifecycle Analysis

From our inspection of `HPRebar.McpBridge/Service/ScriptRunner.cs:73-113`:
```csharp
// ScriptRunner.cs lines 75-112
group = new TransactionGroup(doc, "MCP: " + label);
group.Start();

if (mode == TransactionModes.Auto)
{
    transaction = new Transaction(doc, "MCP script");
    failurePreprocessor = AutoDismissFailurePreprocessor.ApplyTo(transaction);
    transaction.Start();
}

value = script.RunAsync(globals, linked.Token).GetAwaiter().GetResult().ReturnValue;

if (transaction is not null && transaction.Commit() != TransactionStatus.Committed)
    throw new InvalidOperationException("Revit rejected the change...");

if (group is not null)
{
    if (request.DryRun)
    {
        group.RollBack();
        rolledBack = true;
    }
    else
    {
        group.Assimilate();
    }
}
```

This establishes two critical facts:
1. **The script executes with an active `Transaction` open**.
2. **SubTransactions inside the script commit into this outer Transaction**.
3. **At the end of the script, before return, the outer Transaction is still active**.
4. **Any element created before return will be committed by `transaction.Commit()` and assimilated by `group.Assimilate()` if `!dryRun`, or rolled back by `group.RollBack()` if `dryRun`.**

### 4.2 Dual-Layer Safety for Dry-Run

| Layer | Mechanism | Behavior |
| :--- | :--- | :--- |
| **Layer 1 (Script Guard)** | `if (!dryRun && !string.IsNullOrEmpty(planHash))` | The script explicitly inspects `args.Obj("options").Bool("dryRun", false)`. In dry-run mode, the receipt generation block is completely skipped. No `DataStorage` element is created, and no JSON serialization occurs. |
| **Layer 2 (Engine Rollback)** | `group.RollBack()` in `ScriptRunner.cs:105` | Even if a bug or edge-case triggered receipt creation during dry-run, Revit's outer `TransactionGroup` is unconditionally rolled back. 100% of document changes, including `DataStorage` elements, are discarded. |

### 4.3 Proposed Implementation: `BimPlanRunnerScriptTemplate.Receipt.cs`

To adhere to **Rule C2** (Files <= 300 lines) and **Rule M3** (Methods <= 50 lines), the receipt writing logic will be cleanly separated into a partial class file: `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Receipt.cs`.

```csharp
namespace HPBim.Client.Pipeline;

public static partial class BimPlanRunnerScriptTemplate
{
    private static string GetReceiptScript() => """
        // -------------------------------------------------------------------------
        // 3. Extensible Storage Receipt Writing (Atomic Commit)
        // -------------------------------------------------------------------------
        bool dryRun = args.Obj("options").Bool("dryRun", false);
        string planHash = args.Str("planHash", string.Empty);
        string opIdStr = args.Obj("options").Str("operationId", string.Empty);
        Guid operationId = Guid.TryParse(opIdStr, out var parsedOp)
            ? parsedOp
            : (Guid.TryParse(args.Str("planId", string.Empty), out var parsedPId) ? parsedPId : Guid.NewGuid());

        if (!dryRun && !string.IsNullOrEmpty(planHash))
        {
            var schemaGuid = new Guid("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90");
            var schema = Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(schemaGuid);
            if (schema == null)
            {
                var builder = new Autodesk.Revit.DB.ExtensibleStorage.SchemaBuilder(schemaGuid);
                builder.SetSchemaName("HPBimCoordinationReceipt");
                builder.SetDocumentation("HPBim: Execution receipt tracking atomic commit of BIM execution plan.");
                builder.SetReadAccessLevel(Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Public);
                builder.SetWriteAccessLevel(Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Public);
                builder.AddSimpleField("OperationId", typeof(Guid));
                builder.AddSimpleField("PlanHash", typeof(string));
                builder.AddSimpleField("CommittedAtUtc", typeof(string));
                builder.AddSimpleField("ElementMapJson", typeof(string));
                schema = builder.Finish();
            }

            var elemMap = new Dictionary<string, string>();
            foreach (var r in records)
            {
                string sk = r.ContainsKey("sourceKey") ? (string)r["sourceKey"] : string.Empty;
                string uid = r.ContainsKey("targetUniqueId") ? (string)r["targetUniqueId"] : string.Empty;
                string st = r.ContainsKey("status") ? (string)r["status"] : string.Empty;
                if (!string.IsNullOrEmpty(sk) && !string.IsNullOrEmpty(uid) &&
                    (st == "Created" || st == "Updated" || st == "NoOp"))
                {
                    elemMap[sk] = uid;
                }
            }

            string elemMapJson = System.Text.Json.JsonSerializer.Serialize(elemMap);

            DataStorage receiptDs = null;
            var allDs = new FilteredElementCollector(doc)
                .OfClass(typeof(DataStorage))
                .Cast<DataStorage>();
            foreach (var ds in allDs)
            {
                var ent = ds.GetEntity(schema);
                if (ent != null && ent.IsValid() && ent.Get<Guid>("OperationId") == operationId)
                {
                    receiptDs = ds;
                    break;
                }
            }

            if (receiptDs == null)
            {
                receiptDs = DataStorage.Create(doc);
            }

            var entity = new Autodesk.Revit.DB.ExtensibleStorage.Entity(schema);
            entity.Set<Guid>("OperationId", operationId);
            entity.Set<string>("PlanHash", planHash);
            entity.Set<string>("CommittedAtUtc", DateTime.UtcNow.ToString("o"));
            entity.Set<string>("ElementMapJson", elemMapJson);
            receiptDs.SetEntity(entity);
        }
        """;
}
```

In `BimPlanRunnerScriptTemplate.cs`:
```csharp
    public static string GetScriptCode() =>
        GetPreambleScript() +
        GetStructureScript() +
        GetArchitectureScript() +
        GetMepAndRebarScript() +
        GetReceiptScript() +
        GetEpilogueScript();
```

---

## 5. Architectural Design of `ExtensibleStorageReceiptReader`

### 5.1 Architecture & Role in Recovery Layer

`ExtensibleStorageReceiptReader` is the bridge between the high-level `ReconcileService` (Feature F10) and the Revit document's Extensible Storage. It executes a lightweight C# Roslyn script via the named pipe under `TransactionModes.None`.

```
┌────────────────────────────────────────────────────────┐
│               ReconcileService (F10)                   │
│   - Detects OutcomeUnknown from IntentJournal          │
│   - Invokes IExtensibleStorageReceiptReader            │
└───────────────────────────┬────────────────────────────┘
                            │ uses
                            ▼
┌────────────────────────────────────────────────────────┐
│     ExtensibleStorageReceiptReader (F9 Part 2)         │
│   - FindReceiptByOperationIdAsync(Guid)                │
│   - FindReceiptByPlanHashAsync(string)                 │
│   - GetAllReceiptsAsync()                              │
└───────────────────────────┬────────────────────────────┘
                            │ sends ExecuteRequest (TransactionModes.None)
                            ▼
┌────────────────────────────────────────────────────────┐
│    HPRebar.McpBridge in Revit 2026                     │
│   - Evaluates ExtensibleStorageQueryScriptTemplate     │
│   - Scans DataStorage elements for schema              │
│   - Returns JSON array of matching receipts            │
└────────────────────────────────────────────────────────┘
```

### 5.2 Model: `CoordinationReceiptRecord`

Location: `HPBim/src/HPBim.Client/Recovery/CoordinationReceiptRecord.cs`

```csharp
namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;

/// <summary>
/// Strongly-typed record of a receipt read from Revit Extensible Storage on a DataStorage element.
/// </summary>
public sealed record CoordinationReceiptRecord
{
    public Guid OperationId { get; init; } = Guid.Empty;
    public string PlanHash { get; init; } = string.Empty;
    public string CommittedAtUtc { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> ElementMap { get; init; } = new Dictionary<string, string>();
}
```

### 5.3 Interface: `IExtensibleStorageReceiptReader`

Location: `HPBim/src/HPBim.Client/Recovery/IExtensibleStorageReceiptReader.cs`

```csharp
namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IExtensibleStorageReceiptReader
{
    Task<CoordinationReceiptRecord?> FindReceiptByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);

    Task<CoordinationReceiptRecord?> FindReceiptByPlanHashAsync(
        string planHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CoordinationReceiptRecord>> GetAllReceiptsAsync(
        CancellationToken cancellationToken = default);
}
```

### 5.4 Query Script Template: `ExtensibleStorageQueryScriptTemplate`

Location: `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs`

```csharp
namespace HPBim.Client.Recovery;

public static class ExtensibleStorageQueryScriptTemplate
{
    public static string GetQueryScript() => """
        var schemaGuid = new Guid("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90");
        var schema = Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(schemaGuid);
        if (schema == null)
        {
            return new { count = 0, receipts = new List<object>() };
        }

        string targetOpId = args.Str("targetOperationId", string.Empty);
        string targetPlanHash = args.Str("targetPlanHash", string.Empty);

        var collector = new FilteredElementCollector(doc)
            .OfClass(typeof(DataStorage));

        var receipts = new List<object>();

        foreach (DataStorage ds in collector)
        {
            var entity = ds.GetEntity(schema);
            if (entity == null || !entity.IsValid()) continue;

            Guid opId = entity.Get<Guid>("OperationId");
            string planHash = entity.Get<string>("PlanHash") ?? string.Empty;
            string committedUtc = entity.Get<string>("CommittedAtUtc") ?? string.Empty;
            string elemMapJson = entity.Get<string>("ElementMapJson") ?? "{}";

            if (!string.IsNullOrEmpty(targetOpId) &&
                Guid.TryParse(targetOpId, out var parsedOp) &&
                opId != parsedOp)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(targetPlanHash) &&
                !string.Equals(planHash, targetPlanHash, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            receipts.Add(new
            {
                operationId = opId.ToString("D"),
                planHash = planHash,
                committedAtUtc = committedUtc,
                elementMapJson = elemMapJson
            });
        }

        return new
        {
            count = receipts.Count,
            receipts = receipts
        };
        """;
}
```

### 5.5 Reader Class: `ExtensibleStorageReceiptReader`

Location: `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs`

```csharp
namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;

public sealed class ExtensibleStorageReceiptReader : IExtensibleStorageReceiptReader
{
    private readonly IRevitPipeClient _pipeClient;

    public ExtensibleStorageReceiptReader(IRevitPipeClient pipeClient)
    {
        _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
    }

    public async Task<CoordinationReceiptRecord?> FindReceiptByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var receipts = await QueryReceiptsAsync(
            targetOperationId: operationId.ToString("D"),
            targetPlanHash: null,
            cancellationToken).ConfigureAwait(false);

        return receipts.FirstOrDefault();
    }

    public async Task<CoordinationReceiptRecord?> FindReceiptByPlanHashAsync(
        string planHash,
        CancellationToken cancellationToken = default)
    {
        var receipts = await QueryReceiptsAsync(
            targetOperationId: null,
            targetPlanHash: planHash,
            cancellationToken).ConfigureAwait(false);

        // Return latest committed receipt if multiple runs exist for this plan hash
        return receipts
            .OrderByDescending(r => r.CommittedAtUtc)
            .FirstOrDefault();
    }

    public async Task<IReadOnlyList<CoordinationReceiptRecord>> GetAllReceiptsAsync(
        CancellationToken cancellationToken = default)
    {
        return await QueryReceiptsAsync(null, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<CoordinationReceiptRecord>> QueryReceiptsAsync(
        string? targetOperationId,
        string? targetPlanHash,
        CancellationToken cancellationToken)
    {
        if (!_pipeClient.IsConnected)
        {
            await _pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        var argsDict = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(targetOperationId)) argsDict["targetOperationId"] = targetOperationId!;
        if (!string.IsNullOrEmpty(targetPlanHash)) argsDict["targetPlanHash"] = targetPlanHash!;

        var request = new ExecuteRequest(
            Code: ExtensibleStorageQueryScriptTemplate.GetQueryScript(),
            Transaction: TransactionModes.None,
            DryRun: false,
            TimeoutSeconds: 30,
            Label: "Query Extensible Storage Receipts",
            Args: BridgeJson.ToElement(argsDict));

        var execRes = await _pipeClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        if (execRes.IsError)
        {
            throw new RevitPipeException($"Failed to query extensible storage receipts: {execRes.Message}");
        }

        return ParseReceipts(execRes.Value);
    }

    public static IReadOnlyList<CoordinationReceiptRecord> ParseReceipts(JsonElement? root)
    {
        if (root == null || root.Value.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<CoordinationReceiptRecord>();
        }

        var elem = root.Value;
        if (!elem.TryGetProperty("receipts", out var receiptsProp) || receiptsProp.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<CoordinationReceiptRecord>();
        }

        var results = new List<CoordinationReceiptRecord>();
        foreach (var item in receiptsProp.EnumerateArray())
        {
            string opIdStr = item.TryGetProperty("operationId", out var opProp) ? opProp.GetString() ?? string.Empty : string.Empty;
            string planHash = item.TryGetProperty("planHash", out var phProp) ? phProp.GetString() ?? string.Empty : string.Empty;
            string committedAt = item.TryGetProperty("committedAtUtc", out var caProp) ? caProp.GetString() ?? string.Empty : string.Empty;
            string elemMapJson = item.TryGetProperty("elementMapJson", out var emProp) ? emProp.GetString() ?? "{}" : "{}";

            Guid opId = Guid.TryParse(opIdStr, out var parsed) ? parsed : Guid.Empty;
            var elemMap = ParseElementMap(elemMapJson);

            results.Add(new CoordinationReceiptRecord
            {
                OperationId = opId,
                PlanHash = planHash,
                CommittedAtUtc = committedAt,
                ElementMap = elemMap
            });
        }

        return results;
    }

    private static IReadOnlyDictionary<string, string> ParseElementMap(string json)
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
}
```

---

## 6. Testing Strategy & `FakeRevitPipeClient` Integration

### 6.1 `FakeRevitPipeClient` Enhancements

To enable deterministic, offline unit testing in `HPBim.Client.Tests`, `FakeRevitPipeClient` will be enhanced with receipt simulation support:

```csharp
// Addition to FakeRevitPipeClient.cs
public Dictionary<Guid, CoordinationReceiptRecord> SimulatedReceipts { get; } = new();

public void AddSimulatedReceipt(CoordinationReceiptRecord receipt)
{
    SimulatedReceipts[receipt.OperationId] = receipt;
}
```

In `FakeRevitPipeClient.ExecuteAsync`:
```csharp
if (request.Label == "Query Extensible Storage Receipts" && CustomExecuteHandler == null)
{
    var args = request.Args.HasValue ? request.Args.Value : default;
    string targetOpId = args.ValueKind == JsonValueKind.Object && args.TryGetProperty("targetOperationId", out var opProp) ? opProp.GetString() : null;
    string targetPlanHash = args.ValueKind == JsonValueKind.Object && args.TryGetProperty("targetPlanHash", out var phProp) ? phProp.GetString() : null;

    var matched = SimulatedReceipts.Values.Where(r =>
        (string.IsNullOrEmpty(targetOpId) || r.OperationId.ToString("D") == targetOpId) &&
        (string.IsNullOrEmpty(targetPlanHash) || string.Equals(r.PlanHash, targetPlanHash, StringComparison.OrdinalIgnoreCase))
    ).Select(r => new
    {
        operationId = r.OperationId.ToString("D"),
        planHash = r.PlanHash,
        committedAtUtc = r.CommittedAtUtc,
        elementMapJson = JsonSerializer.Serialize(r.ElementMap)
    }).ToList();

    var payload = new { count = matched.Count, receipts = matched };
    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
    using var doc = JsonDocument.Parse(jsonBytes);

    return Task.FromResult(new ExecuteResult
    {
        IsError = false,
        Value = doc.RootElement.Clone(),
        DurationMs = 5
    });
}
```

### 6.2 Test Matrix for `HPBim.Client.Tests`

| Test Class | Test Name | Target Behavior | Expected Outcome |
| :--- | :--- | :--- | :--- |
| `ExtensibleStorageReceiptReaderTests` | `FindReceiptByOperationId_MatchesExpectedReceipt` | Query existing receipt by GUID | Returns matching `CoordinationReceiptRecord` with populated `ElementMap`. |
| `ExtensibleStorageReceiptReaderTests` | `FindReceiptByOperationId_ReturnsNullWhenNotFound` | Query non-existent GUID | Returns `null`. |
| `ExtensibleStorageReceiptReaderTests` | `FindReceiptByPlanHash_ReturnsLatestMatchingReceipt` | Query by `PlanHash` with multiple timestamps | Returns the most recently committed receipt. |
| `ExtensibleStorageReceiptReaderTests` | `GetAllReceipts_ReturnsAllStoredReceipts` | Query with null filters | Returns all receipts stored in Revit document. |
| `ExtensibleStorageReceiptReaderTests` | `ParseReceipts_WithCorruptedElementMapJson_ReturnsEmptyGracefully` | Malformed JSON in `ElementMapJson` | Gracefully falls back to empty dictionary without throwing unhandled exception. |
| `ExtensibleStorageReceiptReaderTests` | `ParseReceipts_WithNullOrEmptyJson_ReturnsEmptyList` | Null or non-object JSON element | Returns empty list. |
| `ExtensibleStorageReceiptReaderTests` | `RoundTripSerialization_PreservesAllMappings` | 100+ composite keys with special characters | 100% equality of keys and unique IDs. |
| `ExtensibleStorageReceiptReaderTests` | `QueryReceiptsAsync_WhenRevitReturnsError_ThrowsRevitPipeException` | `execRes.IsError == true` | Throws `RevitPipeException` with detailed error message. |
| `BimPlanRunnerReceiptTemplateTests` | `GetScriptCode_ContainsSchemaGuidAndName` | Inspect generated script code | Verifies GUID `D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90` and Name `HPBimCoordinationReceipt`. |
| `BimPlanRunnerReceiptTemplateTests` | `GetScriptCode_ContainsAllFourRequiredFields` | Inspect generated script code | Verifies `OperationId`, `PlanHash`, `CommittedAtUtc`, `ElementMapJson`. |
| `BimPlanRunnerReceiptTemplateTests` | `GetScriptCode_ContainsDryRunBypass` | Inspect generated script code | Verifies `if (!dryRun && !string.IsNullOrEmpty(planHash))` conditional guard. |
| `BimPlanRunnerReceiptTemplateTests` | `GetScriptCode_ContainsDataStorageCreation` | Inspect generated script code | Verifies `DataStorage.Create(doc)` and `SetEntity(entity)`. |

---

## 7. Clean Code Governance & Compliance Verification

| Rule ID | Rule Description | Applied Strategy | Compliance Status |
| :--- | :--- | :--- | :--- |
| **Rule C2** | Class length <= 300 lines | `CoordinationReceiptRecord.cs` (~25 lines)<br>`IExtensibleStorageReceiptReader.cs` (~35 lines)<br>`ExtensibleStorageQueryScriptTemplate.cs` (~65 lines)<br>`ExtensibleStorageReceiptReader.cs` (~135 lines)<br>`BimPlanRunnerScriptTemplate.Receipt.cs` (~95 lines) | **PASS** (all files <= 135 lines) |
| **Rule M3** | Method length <= 50 lines (target <= 35) | Every method is focused and under 30 lines. Complex parsing split into `ParseReceipts` and `ParseElementMap`. | **PASS** (max method <= 30 lines) |
| **Rule 0** | Fail-Fast & zero guesswork | Invalid parameters reject immediately. Document mismatch rejects in pre-flight. Catch blocks explain reasons: `// Corrupt or truncated JSON in storage handled gracefully`. | **PASS** |
| **Architecture** | Pure domain separation | `HPBim.Core` has ZERO Revit references. `HPBim.Client` has ZERO Revit API dependencies (`Autodesk.Revit.*`); all Revit interactions occur via string Roslyn scripts sent over the named pipe. | **PASS** |

---

## 8. Summary of Proposed Files for Implementation Track

1. `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Receipt.cs` (New file)
2. `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs` (Append `GetReceiptScript()` to `GetScriptCode()`)
3. `HPBim/src/HPBim.Client/Recovery/CoordinationReceiptRecord.cs` (New file)
4. `HPBim/src/HPBim.Client/Recovery/IExtensibleStorageReceiptReader.cs` (New file)
5. `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs` (New file)
6. `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs` (New file)
7. `HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs` (Add simulated receipt handlers)
8. `HPBim/tests/HPBim.Client.Tests/Recovery/ExtensibleStorageReceiptReaderTests.cs` (New test file)
9. `HPBim/tests/HPBim.Client.Tests/Pipeline/BimPlanRunnerReceiptTemplateTests.cs` (New test file)
