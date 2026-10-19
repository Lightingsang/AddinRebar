# Investigation & Design Report: Non-Object JSON Array Guard in ExtensibleStorageReceiptReader

- **Agent**: `explorer_m4_it2_2`
- **Archetype**: `teamwork_preview_explorer`
- **Role**: `Receipt Reader Malformed Payload Guard Explorer`
- **Target File**: `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs`
- **Date**: 2026-10-07T21:55:00Z
- **Status**: Complete (Read-Only Investigation)

---

## 1. Executive Summary

During adversarial review and stress testing of Milestone M4 (Recovery & Reconciliation), an unhandled exception vulnerability was identified at `ExtensibleStorageReceiptReader.cs:119`. In `ParseReceipts(JsonElement? root)`, the method enumerates elements in the `"receipts"` JSON array and invokes `item.TryGetProperty(...)` under the assumption that every element is a JSON Object (`JsonValueKind.Object`).

When a payload contains non-object elements (e.g. primitives such as integers, strings, booleans, or nulls resulting from network corruption, storage tampering, or adversarial payloads), `System.Text.Json` throws `InvalidOperationException: The requested operation requires an element of type 'Object', but the target element has type '...'`. This terminates receipt parsing and crashes the auto-reconciliation pipeline.

This report documents the root cause, evaluates two mitigation designs against strict Clean Code metrics ($\le 35$ lines per method, $\le 200$ lines per file), specifies the recommended implementation, and details the necessary test updates in `AdversarialReceiptReaderTests.cs`.

---

## 2. Problem Analysis & Root Cause

### 2.1 Code Inspection

In `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs`, lines 105–139:

```csharp
105: public static IReadOnlyList<CoordinationReceiptRecord> ParseReceipts(JsonElement? root)
106: {
107:     if (root == null || root.Value.ValueKind != JsonValueKind.Object)
108:     {
109:         return Array.Empty<CoordinationReceiptRecord>();
110:     }
111: 
112:     var elem = root.Value;
113:     if (!elem.TryGetProperty("receipts", out var receiptsProp) || receiptsProp.ValueKind != JsonValueKind.Array)
114:     {
115:         return Array.Empty<CoordinationReceiptRecord>();
116:     }
117: 
118:     var results = new List<CoordinationReceiptRecord>();
119:     foreach (var item in receiptsProp.EnumerateArray())
120:     {
121:         string opIdStr = item.TryGetProperty("operationId", out var opProp) ? opProp.GetString() ?? string.Empty : string.Empty;
122:         string planHash = item.TryGetProperty("planHash", out var phProp) ? phProp.GetString() ?? string.Empty : string.Empty;
123:         string committedAt = item.TryGetProperty("committedAtUtc", out var caProp) ? caProp.GetString() ?? string.Empty : string.Empty;
124:         string elemMapJson = item.TryGetProperty("elementMapJson", out var emProp) ? emProp.GetString() ?? "{}" : "{}";
125: 
126:         Guid opId = Guid.TryParse(opIdStr, out var parsed) ? parsed : Guid.Empty;
127:         var elemMap = ParseElementMap(elemMapJson);
128: 
129:         results.Add(new CoordinationReceiptRecord
130:         {
131:             OperationId = opId,
132:             PlanHash = planHash,
133:             CommittedAtUtc = committedAt,
134:             ElementMap = elemMap
135:         });
136:     }
137: 
138:     return results;
139: }
```

### 2.2 Mechanism of Failure

1. `root` and `elem` are verified to be `JsonValueKind.Object`, and `receiptsProp` is verified to be `JsonValueKind.Array`.
2. However, inside the loop (`foreach (var item in receiptsProp.EnumerateArray())`), there is **no check on `item.ValueKind`**.
3. At line 121, `item.TryGetProperty("operationId", out var opProp)` is called directly on `item`.
4. In `System.Text.Json`, `JsonElement.TryGetProperty` has the precondition that `ValueKind == JsonValueKind.Object`. If `item` is `JsonValueKind.Number` (e.g. `123`) or `JsonValueKind.String` (e.g. `"some-string"`), `TryGetProperty` throws:
   ```
   System.InvalidOperationException: The requested operation requires an element of type 'Object', but the target element has type 'Number'.
   ```
5. This failure was explicitly proven by `challenger_m4_2` in test `ParseReceipts_NonObjectArrayElements_ThrowsInvalidOperationException_ProvingMissingGuard`.

---

## 3. Design Options & Line Budget Analysis

### 3.1 Design Options

#### Option A: Inline Guard within `ParseReceipts`
Insert `if (item.ValueKind != JsonValueKind.Object) continue;` directly inside the loop.
- **Physical Line Count of `ParseReceipts`**:
  - Current lines: 35 (L105–L139).
  - Adding multiline block (`if (...) { continue; }`): expands method to **39 lines**, violating the dispatch constraint ($\le 35$ lines).
  - Adding single-line `if (item.ValueKind != JsonValueKind.Object) continue;` and eliminating empty lines: reaches **34 lines** (uncomfortably close to the 35-line boundary).

#### Option B: Extract Helper Method `ParseSingleReceipt` (Recommended)
Decompose `ParseReceipts` into two methods:
1. `ParseReceipts`: Handles array validation, enumeration, non-object filtering, and collection accumulation.
2. `ParseSingleReceipt`: Maps properties of a confirmed object `JsonElement` to `CoordinationReceiptRecord`.

### 3.2 Design B Implementation Detail

```csharp
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
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            results.Add(ParseSingleReceipt(item));
        }

        return results;
    }

    private static CoordinationReceiptRecord ParseSingleReceipt(JsonElement item)
    {
        string opIdStr = item.TryGetProperty("operationId", out var opProp) ? opProp.GetString() ?? string.Empty : string.Empty;
        string planHash = item.TryGetProperty("planHash", out var phProp) ? phProp.GetString() ?? string.Empty : string.Empty;
        string committedAt = item.TryGetProperty("committedAtUtc", out var caProp) ? caProp.GetString() ?? string.Empty : string.Empty;
        string elemMapJson = item.TryGetProperty("elementMapJson", out var emProp) ? emProp.GetString() ?? "{}" : "{}";

        return new CoordinationReceiptRecord
        {
            OperationId = Guid.TryParse(opIdStr, out var parsed) ? parsed : Guid.Empty,
            PlanHash = planHash,
            CommittedAtUtc = committedAt,
            ElementMap = ParseElementMap(elemMapJson)
        };
    }
```

---

## 4. Method and File Metric Verification

### 4.1 Method Lengths (Target $\le 35$ lines; Clean Code Rule M3 $\le 50$ lines)

| Method Name | Estimated Span | Physical Line Count | Compliance ($\le 35$) | Compliance ($\le 50$) |
|---|---|---|---|---|
| `ExtensibleStorageReceiptReader` (ctor) | L21–L24 | 4 lines | PASS | PASS |
| `FindReceiptByOperationIdAsync` | L26–L36 | 11 lines | PASS | PASS |
| `FindReceiptByPlanHashAsync` | L38–L50 | 13 lines | PASS | PASS |
| `FindReceiptAsync` | L52–L66 | 15 lines | PASS | PASS |
| `GetAllReceiptsAsync` | L68–L72 | 5 lines | PASS | PASS |
| `QueryReceiptsAsync` | L74–L103 | 30 lines | PASS | PASS |
| `ParseReceipts` | L105–L130 | **26 lines** | **PASS** | **PASS** |
| `ParseSingleReceipt` (New Helper) | L132–L146 | **15 lines** | **PASS** | **PASS** |
| `ParseElementMap` | L148–L165 | 18 lines | PASS | PASS |

- **Maximum method length in file**: **30 lines** (`QueryReceiptsAsync`), which is strictly $\le 35$ lines.
- `ParseReceipts` is **26 lines** ($\le 35$).
- `ParseSingleReceipt` is **15 lines** ($\le 35$).

### 4.2 File Length (Target $\le 200$ lines; Clean Code Rule C2 $\le 300$ lines)

- Current file length: **160 lines**.
- Line delta under Design B:
  - `ParseReceipts` reduces from 35 lines to 26 lines (-9 lines).
  - Helper `ParseSingleReceipt` adds 15 lines + 1 blank line separator (+16 lines).
  - Net delta: $+7$ lines.
- Projected file length: **167 lines**.
- **Compliance**: $167 \le 200$ lines (**PASS**). $167 \le 300$ lines (**PASS**).

---

## 5. Test Suite Impact & Recommended Test Updates

### 5.1 Test Analysis

In `HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReceiptReaderTests.cs`, line 101:

```csharp
    [Fact]
    public void ParseReceipts_NonObjectArrayElements_ThrowsInvalidOperationException_ProvingMissingGuard()
    {
        string raw = """
        {
            "receipts": [
                123,
                "some-string"
            ]
        }
        """;

        using var doc = JsonDocument.Parse(raw);
        // Empirically demonstrates the absence of `if (item.ValueKind != JsonValueKind.Object) continue;`
        var ex = Assert.Throws<InvalidOperationException>(
            () => ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement));
        Assert.Contains("requires an element of type 'Object'", ex.Message);
    }
```

Once the defensive guard is introduced, `ExtensibleStorageReceiptReader.ParseReceipts` will gracefully skip `123` and `"some-string"` and return `Array.Empty<CoordinationReceiptRecord>()`. Therefore, the test asserting `Assert.Throws<InvalidOperationException>` will fail.

### 5.2 Recommended Test Changes

The test in `AdversarialReceiptReaderTests.cs` should be updated to assert graceful filtering:

```csharp
    [Fact]
    public void ParseReceipts_NonObjectArrayElements_GracefullyIgnoredOrHandled()
    {
        string raw = """
        {
            "receipts": [
                123,
                "some-string"
            ]
        }
        """;

        using var doc = JsonDocument.Parse(raw);
        var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
        Assert.Empty(receipts);
    }

    [Fact]
    public void ParseReceipts_MixedObjectAndNonObjectElements_ExtractsOnlyValidObjects()
    {
        var op = Guid.NewGuid();
        string raw = $$"""
        {
            "receipts": [
                123,
                "some-string",
                {
                    "operationId": "{{op}}",
                    "planHash": "valid-hash",
                    "committedAtUtc": "2026-10-07T22:00:00Z",
                    "elementMapJson": "{}"
                },
                null,
                true
            ]
        }
        """;

        using var doc = JsonDocument.Parse(raw);
        var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
        Assert.Single(receipts);
        Assert.Equal(op, receipts[0].OperationId);
        Assert.Equal("valid-hash", receipts[0].PlanHash);
    }
```

---

## 6. Actionable Implementation Diff

For the implementer (`worker_m4_it2_2` or designated worker):

```diff
--- a/HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs
+++ b/HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs
@@ -118,25 +118,29 @@ public static IReadOnlyList<CoordinationReceiptRecord> ParseReceipts(JsonElement
         var results = new List<CoordinationReceiptRecord>();
         foreach (var item in receiptsProp.EnumerateArray())
         {
-            string opIdStr = item.TryGetProperty("operationId", out var opProp) ? opProp.GetString() ?? string.Empty : string.Empty;
-            string planHash = item.TryGetProperty("planHash", out var phProp) ? phProp.GetString() ?? string.Empty : string.Empty;
-            string committedAt = item.TryGetProperty("committedAtUtc", out var caProp) ? caProp.GetString() ?? string.Empty : string.Empty;
-            string elemMapJson = item.TryGetProperty("elementMapJson", out var emProp) ? emProp.GetString() ?? "{}" : "{}";
-
-            Guid opId = Guid.TryParse(opIdStr, out var parsed) ? parsed : Guid.Empty;
-            var elemMap = ParseElementMap(elemMapJson);
-
-            results.Add(new CoordinationReceiptRecord
+            if (item.ValueKind != JsonValueKind.Object)
             {
-                OperationId = opId,
-                PlanHash = planHash,
-                CommittedAtUtc = committedAt,
-                ElementMap = elemMap
-            });
+                continue;
+            }
+
+            results.Add(ParseSingleReceipt(item));
         }
 
         return results;
     }
+
+    private static CoordinationReceiptRecord ParseSingleReceipt(JsonElement item)
+    {
+        string opIdStr = item.TryGetProperty("operationId", out var opProp) ? opProp.GetString() ?? string.Empty : string.Empty;
+        string planHash = item.TryGetProperty("planHash", out var phProp) ? phProp.GetString() ?? string.Empty : string.Empty;
+        string committedAt = item.TryGetProperty("committedAtUtc", out var caProp) ? caProp.GetString() ?? string.Empty : string.Empty;
+        string elemMapJson = item.TryGetProperty("elementMapJson", out var emProp) ? emProp.GetString() ?? "{}" : "{}";
+
+        return new CoordinationReceiptRecord
+        {
+            OperationId = Guid.TryParse(opIdStr, out var parsed) ? parsed : Guid.Empty,
+            PlanHash = planHash,
+            CommittedAtUtc = committedAt,
+            ElementMap = ParseElementMap(elemMapJson)
+        };
+    }
```
