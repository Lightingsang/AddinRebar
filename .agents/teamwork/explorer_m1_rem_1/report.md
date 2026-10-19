# Investigation Report: Canonical String SourceKey Implementation in HPBim (R1.1)

**Agent**: `explorer_m1_rem_1`  
**Date**: 2026-10-08  
**Scope**: HPBim Coordination, Wire Contracts, ScriptArgs, Roslyn Runner, and Extensible Storage Receipt  
**Status**: INVESTIGATION_COMPLETE  

---

## 1. Executive Summary & Root Cause Analysis

### 1.1 The Objective
Normalize `SourceKey` to the canonical string format `[DocIdentifier]#[Category]:[PrimaryHandle]` across all components in `HPBim`:
- Domain model: `ElementPlanItem.SourceKey` and `CompositeSourceKey`
- Wire DTO: `ExecuteRequest.Args` serialized via `BridgeJson`
- Script arguments: `ScriptArgs` consumed inside the Revit Roslyn environment
- Roslyn execution: element creation, `ALL_MODEL_INSTANCE_COMMENTS` parameter setting, and returned execution `records`
- Receipt storage: `HPBimCoordinationReceipt` schema in Revit Extensible Storage
- Ledger mapping: `BimCoordinationLedgerService` and `ReconcileService` mapping restoration
- Guarantee 100% roundtrip equality through JSON serialization and deserialization.

### 1.2 The Root Cause
A critical data loss and format mismatch occurs during the execution lifecycle due to the absence of a custom JSON converter for `CompositeSourceKey`:
1. `CompositeSourceKey` is defined as a standard C# `sealed record` with properties (`DocIdentifier`, `Category`, `PrimaryHandle`, `ProvenanceHandles`). Without a `JsonConverter`, `System.Text.Json` (and `BridgeJson`) serializes it as a **JSON object**:
   ```json
   {
     "docIdentifier": "NhaPho_5x18m",
     "category": 4,
     "primaryHandle": "C1",
     "provenanceHandles": []
   }
   ```
2. When `BridgeJson.ToElement(plan)` serializes `BimExecutionPlan` for `ExecuteRequest.Args`, `item.SourceKey` is packed as a JSON object inside `args.items[i].sourceKey`.
3. In `BimPlanRunnerScriptTemplate.cs` (line 160), the script executes:
   ```csharp
   string sourceKey = item.Str("sourceKey", string.Empty);
   ```
   In `ScriptArgs.cs` (line 121), `ToStr(JsonElement)` on a `JsonValueKind.Object` returns `e.GetRawText()`. Consequently, `sourceKey` in the script becomes the raw JSON string:
   `"{\"docIdentifier\":\"NhaPho_5x18m\",\"category\":4,\"primaryHandle\":\"C1\",\"provenanceHandles\":[]}"`.
4. This raw JSON object string propagates everywhere:
   - Written to the element's `Comments` parameter in Revit.
   - Returned in `ExecuteResult` inside `records[i]["sourceKey"]`.
   - Stored in Revit Extensible Storage `HPBimCoordinationReceipt.ElementMapJson` as:
     `{"{\"docIdentifier\":\"NhaPho_5x18m\",...}": "unique-id-123"}`.
5. In `BimPipelineEngine.PlanExecution.cs` (line 256):
   ```csharp
   SourceKey = CompositeSourceKey.TryParse(rawSourceKey, out var parsedSk) ? parsedSk! : new CompositeSourceKey(string.Empty, cat, mark),
   ```
   `CompositeSourceKey.TryParse` fails to parse JSON curly braces! As a result, it falls back to `new CompositeSourceKey(string.Empty, cat, mark)`, **destroying `DocIdentifier` and substituting `mark` for `PrimaryHandle`**.
6. In `ReconcileService.cs` (line 97) and `CoordinationCommand.cs` (line 223):
   ```csharp
   if (CompositeSourceKey.TryParse(keyStr, out var sourceKey) && sourceKey != null)
   ```
   Because `keyStr` from Extensible Storage is the raw JSON curly brace string, `TryParse` returns `false`, causing **0 mappings to be restored during reconciliation**.
7. In `AdversarialReceiptReaderTests.cs` (lines 215–225), a test named `ElementPlanItem_CompositeSourceKey_SerializesAsJsonObject` had erroneously codified this broken behavior as expected.

---

## 2. Evidence Chain & Detailed Observations

### Observation 1: `CompositeSourceKey.ToString()` already generates the canonical string
- **File**: `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs`, line 104
- **Code**:
  ```csharp
  public override string ToString() => $"[{DocIdentifier}]#[{Category}]:{PrimaryHandle}";
  ```
- **Verification**: `CompositeSourceKeyTests.cs` (line 62) proves that for `new CompositeSourceKey("D", CoordinationCategory.Slab, "S1")`, `ToString()` produces `"[D]#[Slab]:S1"`. `CompositeSourceKey.Parse("[D]#[Slab]:S1")` parses it back identically.

### Observation 2: `CompositeSourceKey` lacks `[JsonConverter]`
- **File**: `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs`, lines 8–12
- **Code**:
  ```csharp
  public sealed record CompositeSourceKey : IEquatable<CompositeSourceKey>, IComparable<CompositeSourceKey>
  ```
- **Finding**: No `[JsonConverter]` attribute exists on `CompositeSourceKey`. `System.Text.Json` defaults to property reflection, producing a multi-property JSON object instead of a string.

### Observation 3: `BridgeJson` uses default converters only
- **File**: `McpShared/HPRebar.Mcp.Contracts/JsonRpc/BridgeJson.cs`, lines 14–23
- **Finding**: `BridgeJson.Options` only specifies naming policies and character encoders; it has zero host-specific converters registered. Therefore, type-level `[JsonConverter]` attributes are required for custom serialization.

### Observation 4: ScriptArgs string coercion on JsonValueKind.Object
- **File**: `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptArgs.cs`, lines 114–122
- **Code**:
  ```csharp
  private static string? ToStr(JsonElement e) => e.ValueKind switch
  {
      JsonValueKind.String => e.GetString(),
      JsonValueKind.Number => e.GetRawText(),
      JsonValueKind.True => "true",
      JsonValueKind.False => "false",
      JsonValueKind.Null or JsonValueKind.Undefined => null,
      _ => e.GetRawText(),
  };
  ```
- **Finding**: When `sourceKey` is an object, `ToStr` outputs `e.GetRawText()`. When `sourceKey` is a JSON string, `ToStr` outputs `e.GetString()`. Making `sourceKey` serialize as a string ensures `item.Str("sourceKey")` returns `"[DocIdentifier]#[Category]:[PrimaryHandle]"` directly.

### Observation 5: Extensible Storage receipt serialization
- **File**: `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Receipt.cs`, lines 41–54
- **Code**:
  ```csharp
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
  ```
- **Finding**: When `r["sourceKey"]` is the canonical string, `elemMap` stores `{ "[Doc]#[Category]:[Handle]": "uid" }`, which directly matches the expectation of `ReconcileService`.

### Observation 6: `BimCoordinationLedgerService.ApplyLoadedLedger` drops `_mappings`
- **File**: `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.cs`, lines 259–277
- **Finding**: `ApplyLoadedLedger` populates `_elements` and `_rfis` from `loaded`, but never hydrates `_mappings` from `loaded.Mappings`. In addition, `SyncLedgerState` does not assign `_mappings` to `_ledger.Mappings`.

---

## 3. Inventory of Affected Files, Classes, and Methods

| Path | Class / Struct | Method / Property | Impact & Required Modification |
|---|---|---|---|
| `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs` | `CompositeSourceKey` | Class declaration | Add `[JsonConverter(typeof(CompositeSourceKeyJsonConverter))]` attribute |
| `HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs` | `CompositeSourceKeyJsonConverter` | New Class | Implement custom converter: writes canonical string, reads string (canonical/plain) and legacy object |
| `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerSerializer.cs` | `BimCoordinationLedgerSerializer` | `CreateOptions(bool indented)` | Add `options.Converters.Add(new CompositeSourceKeyJsonConverter());` |
| `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.cs` | `BimCoordinationLedgerService` | `ApplyLoadedLedger`, `SyncLedgerState` | Hydrate `_mappings` from `loaded.Mappings`; synchronize `_mappings` to `_ledger.Mappings` |
| `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs` | `BimPlanRunnerScriptTemplate` | Preamble / Item loop | Add defensive `NormalizeSourceKey(item)` helper to ensure canonical string even if legacy JSON object passed |
| `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Receipt.cs` | `BimPlanRunnerScriptTemplate` | `GetReceiptStorageScript` | Verify receipt map keys use canonical `sourceKey` string |
| `HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReceiptReaderTests.cs` | `AdversarialReceiptReaderTests` | `ElementPlanItem_CompositeSourceKey_SerializesAsJsonObject` | Rename and update to `ElementPlanItem_CompositeSourceKey_SerializesAsCanonicalString` |
| `HPBim/tests/HPBim.Core.Tests/Coordination/CompositeSourceKeyJsonConverterTests.cs` | `CompositeSourceKeyJsonConverterTests` | New Test Suite | Comprehensive tests for 100% roundtrip equality, legacy object parsing, null handling |
| `HPBim/tests/HPBim.Client.Tests/Pipeline/SourceKeyWireRoundtripTests.cs` | `SourceKeyWireRoundtripTests` | New Test Suite | End-to-end wire roundtrip test: `ExecutionPlan` -> `BridgeJson` -> `ScriptArgs` -> Runner -> `ExecuteResult` -> `Ledger`/`Receipt` |

---

## 4. Step-by-Step Fix Strategy for Worker

### Step 1: Create `CompositeSourceKeyJsonConverter.cs` in `HPBim.Core/Coordination/`
- Target Framework: `netstandard2.0` (zero Revit API dependencies).
- In `Write`:
  - If `value is null || value == CompositeSourceKey.Empty || string.IsNullOrWhiteSpace(value.PrimaryHandle)`, write empty string `""` or null.
  - Otherwise, call `writer.WriteStringValue(value.ToString())` to output `[DocIdentifier]#[Category]:[PrimaryHandle]`.
- In `Read`:
  - Handle `JsonTokenType.String`: if string is empty/whitespace, return `CompositeSourceKey.Empty`. Otherwise, call `CompositeSourceKey.TryParse(raw, out var key, allowEmptyDoc: true)` and return `key!`.
  - Handle `JsonTokenType.StartObject`: tolerant backward compatibility for legacy JSON objects (`docIdentifier`, `category` as string or int, `primaryHandle`, `provenanceHandles`).
  - Handle `JsonTokenType.Null`: return `CompositeSourceKey.Empty`.
  - Comply with Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines per method).

### Step 2: Attribute `CompositeSourceKey` with the Converter
- In `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs`:
  ```csharp
  [JsonConverter(typeof(CompositeSourceKeyJsonConverter))]
  public sealed record CompositeSourceKey : IEquatable<CompositeSourceKey>, IComparable<CompositeSourceKey>
  ```
- Import `System.Text.Json.Serialization;`.

### Step 3: Register Converter in `BimCoordinationLedgerSerializer`
- In `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerSerializer.cs`, method `CreateOptions`:
  ```csharp
  options.Converters.Add(new CompositeSourceKeyJsonConverter());
  ```

### Step 4: Fix `BimCoordinationLedgerService` Hydration & State Sync
- In `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.cs`:
  - In `ApplyLoadedLedger(BimCoordinationLedger loaded)`:
    ```csharp
    _mappings.Clear();
    if (loaded.Mappings != null)
    {
        foreach (var kvp in loaded.Mappings)
        {
            _mappings[kvp.Key] = kvp.Value;
        }
    }
    ```
  - In `SyncLedgerState()`:
    ```csharp
    _ledger = _ledger with
    {
        UpdatedAt = DateTime.UtcNow,
        Summary = CalculateSummary(),
        Elements = _elements.Values.ToList().AsReadOnly(),
        Rfis = _rfis.ToList().AsReadOnly(),
        Mappings = new Dictionary<string, LedgerElementMapping>(_mappings, StringComparer.OrdinalIgnoreCase)
    };
    ```

### Step 5: Add Dual-Defense Normalization in `BimPlanRunnerScriptTemplate.cs`
- In `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs`, inside `GetPreambleHelpersAndPreFlightScript()`:
  Add a script-level helper to normalize any incoming item's `sourceKey`:
  ```csharp
  string NormalizeSourceKey(ScriptArgs it)
  {
      string raw = it.Str("sourceKey", string.Empty);
      if (string.IsNullOrEmpty(raw) || !raw.StartsWith("{")) return raw;
      var obj = it.Obj("sourceKey");
      string d = obj.Str("docIdentifier", string.Empty);
      string c = obj.Str("category", string.Empty);
      string h = obj.Str("primaryHandle", string.Empty);
      return $"[{d}]#[{c}]:{h}";
  }
  ```
  And in the item loop:
  ```csharp
  string sourceKey = NormalizeSourceKey(item);
  ```

### Step 6: Update Existing Test
- In `HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReceiptReaderTests.cs`:
  Replace `ElementPlanItem_CompositeSourceKey_SerializesAsJsonObject` with:
  ```csharp
  [Fact]
  public void ElementPlanItem_CompositeSourceKey_SerializesAsCanonicalString()
  {
      var key = CompositeSourceKey.Create("Doc", CoordinationCategory.Column, "C1");
      var item = new ElementPlanItem { SourceKey = key };

      var elem = BridgeJson.ToElement(item);
      Assert.NotNull(elem);
      var sk = elem.Value.GetProperty("sourceKey");
      Assert.Equal(JsonValueKind.String, sk.ValueKind);
      Assert.Equal("[Doc]#[Column]:C1", sk.GetString());
  }
  ```

### Step 7: Author Comprehensive Unit & Integration Tests
- Add `CompositeSourceKeyJsonConverterTests.cs` to `HPBim.Core.Tests`.
- Add `SourceKeyWireRoundtripTests.cs` to `HPBim.Client.Tests`.
- Execute `dotnet test` on all test projects to confirm 0 failures and 0 regressions.

---

## 5. Concrete Code Snippets for Worker

### 5.1 `CompositeSourceKeyJsonConverter.cs`
Create file at `HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs`:

```csharp
namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// JSON converter for CompositeSourceKey serializing directly to the canonical string
/// format: [DocIdentifier]#[Category]:[PrimaryHandle] with 100% roundtrip fidelity.
/// Also supports deserializing legacy JSON objects for backward compatibility.
/// </summary>
public sealed class CompositeSourceKeyJsonConverter : JsonConverter<CompositeSourceKey>
{
    public override CompositeSourceKey? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return CompositeSourceKey.Empty;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw)) return CompositeSourceKey.Empty;
            return CompositeSourceKey.TryParse(raw, out var parsed, allowEmptyDoc: true)
                ? parsed!
                : throw new JsonException($"Invalid CompositeSourceKey string format: '{raw}'");
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return ReadLegacyJsonObject(ref reader);
        }

        throw new JsonException($"Expected string or object for CompositeSourceKey, got {reader.TokenType}.");
    }

    private static CompositeSourceKey ReadLegacyJsonObject(ref Utf8JsonReader reader)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        string docId = string.Empty;
        string primaryHandle = string.Empty;
        CoordinationCategory category = CoordinationCategory.Column;
        List<string>? provenance = null;

        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, "docIdentifier", StringComparison.OrdinalIgnoreCase))
            {
                docId = prop.Value.GetString() ?? string.Empty;
            }
            else if (string.Equals(prop.Name, "primaryHandle", StringComparison.OrdinalIgnoreCase))
            {
                primaryHandle = prop.Value.GetString() ?? string.Empty;
            }
            else if (string.Equals(prop.Name, "category", StringComparison.OrdinalIgnoreCase))
            {
                category = ParseCategoryProperty(prop.Value);
            }
            else if (string.Equals(prop.Name, "provenanceHandles", StringComparison.OrdinalIgnoreCase) &&
                     prop.Value.ValueKind == JsonValueKind.Array)
            {
                provenance = new List<string>();
                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s)
                    {
                        provenance.Add(s);
                    }
                }
            }
        }

        return new CompositeSourceKey(docId, category, primaryHandle, provenance);
    }

    private static CoordinationCategory ParseCategoryProperty(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int catInt) &&
            Enum.IsDefined(typeof(CoordinationCategory), catInt))
        {
            return (CoordinationCategory)catInt;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            string catStr = element.GetString() ?? string.Empty;
            if (Enum.TryParse<CoordinationCategory>(catStr, true, out var cat))
            {
                return cat;
            }
        }

        return CoordinationCategory.Column;
    }

    public override void Write(Utf8JsonWriter writer, CompositeSourceKey value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        if (value == CompositeSourceKey.Empty || string.IsNullOrWhiteSpace(value.PrimaryHandle))
        {
            writer.WriteStringValue(string.Empty);
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}
```

### 5.2 Attribute in `CompositeSourceKey.cs`
In `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs`:
```csharp
using System.Text.Json.Serialization;

// ...
[JsonConverter(typeof(CompositeSourceKeyJsonConverter))]
public sealed record CompositeSourceKey : IEquatable<CompositeSourceKey>, IComparable<CompositeSourceKey>
```

### 5.3 Registration in `BimCoordinationLedgerSerializer.cs`
In `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerSerializer.cs`:
```csharp
        options.Converters.Add(new CompositeSourceKeyJsonConverter());
        options.Converters.Add(new CoordinationCategoryJsonConverter());
        options.Converters.Add(new RfiStatusJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
```

### 5.4 Dual-Defense Preamble Script in `BimPlanRunnerScriptTemplate.cs`
In `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs`, in `GetPreambleHelpersAndPreFlightScript()`:
```csharp
        double Ft(double mm) => mm / 304.8;
        double Mm(double ft) => ft * 304.8;
        XYZ ToXyz(double xMm, double yMm, double zMm = 0.0) => new XYZ(Ft(xMm), Ft(yMm), Ft(zMm));

        string NormalizeSourceKey(ScriptArgs it)
        {
            string raw = it.Str("sourceKey", string.Empty);
            if (string.IsNullOrEmpty(raw) || !raw.StartsWith("{")) return raw;
            var obj = it.Obj("sourceKey");
            string d = obj.Str("docIdentifier", string.Empty);
            string c = obj.Str("category", string.Empty);
            string h = obj.Str("primaryHandle", string.Empty);
            return $"[{d}]#[{c}]:{h}";
        }
```
And in `GetPreambleTypeCollectorsScript()` (around line 160):
```csharp
        // 2. Sequential Execution of Items
        foreach (var item in items)
        {
            string sourceKey = NormalizeSourceKey(item);
            string category = item.Require("category");
            string mark = item.Str("mark", string.Empty);
```

### 5.5 Hydration in `BimCoordinationLedgerService.cs`
In `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.cs`:
```csharp
    private void ApplyLoadedLedger(BimCoordinationLedger loaded)
    {
        _elements.Clear();
        _rfis.Clear();
        _mappings.Clear();
        _ledger = loaded;

        if (loaded.Elements != null)
        {
            foreach (var el in loaded.Elements)
            {
                _elements[el.Id] = el;
            }
        }

        if (loaded.Rfis != null)
        {
            _rfis.AddRange(loaded.Rfis);
        }

        if (loaded.Mappings != null)
        {
            foreach (var kvp in loaded.Mappings)
            {
                _mappings[kvp.Key] = kvp.Value;
            }
        }
    }

    private BimCoordinationLedger SyncLedgerState()
    {
        _ledger = _ledger with
        {
            UpdatedAt = DateTime.UtcNow,
            Summary = CalculateSummary(),
            Elements = _elements.Values.ToList().AsReadOnly(),
            Rfis = _rfis.ToList().AsReadOnly(),
            Mappings = new Dictionary<string, LedgerElementMapping>(_mappings, StringComparer.OrdinalIgnoreCase)
        };
        return _ledger;
    }
```

---

## 6. Verification Criteria for Worker

1. `dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj`: All tests pass (>= 417 existing + new converter tests).
2. `dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`: All tests pass (>= 322 existing + new wire roundtrip tests).
3. `dotnet test HPBim/tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj`: All tests pass (>= 42 existing tests).
4. Total test count across repo remains 100% green with zero regressions.
5. Roundtrip equality test verifies:
   `JsonSerializer.Deserialize<CompositeSourceKey>(JsonSerializer.Serialize(key)) == key` for all permutations.
