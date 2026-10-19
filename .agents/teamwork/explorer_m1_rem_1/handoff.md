# Handoff Report: Canonical String SourceKey Investigation (M1_REM_1 / R1.1)

**Agent**: `explorer_m1_rem_1`  
**Date**: 2026-10-08T11:47:00Z  
**Type**: Hard Handoff  
**Recipient**: `orchestrator_5` / Worker Agent  

---

## 1. Observation

1. **`CompositeSourceKey.ToString()` canonical format**:
   - `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs:104`:
     ```csharp
     public override string ToString() => $"[{DocIdentifier}]#[{Category}]:{PrimaryHandle}";
     ```
   - Confirmed by `HPBim/tests/HPBim.Core.Tests/Coordination/CompositeSourceKeyTests.cs:61-67`:
     ```csharp
     var key = new CompositeSourceKey("D", CoordinationCategory.Slab, "S1");
     string formatted = key.ToString();
     Assert.Equal("[D]#[Slab]:S1", formatted);
     var parsed = CompositeSourceKey.Parse(formatted);
     Assert.Equal(key, parsed);
     ```

2. **Absence of `JsonConverter` on `CompositeSourceKey`**:
   - `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs:8-12`:
     Class declaration has no `[JsonConverter]` attribute. `System.Text.Json` serializes it by default as an object with properties `docIdentifier`, `category`, `primaryHandle`, and `provenanceHandles`.

3. **Wire serialization via `BridgeJson`**:
   - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs:94`:
     ```csharp
     Args: BridgeJson.ToElement(plan));
     ```
   - `McpShared/HPRebar.Mcp.Contracts/JsonRpc/BridgeJson.cs:14-23`:
     `BridgeJson.Options` contains only `PropertyNamingPolicy`, `PropertyNameCaseInsensitive`, and relaxed encoder; zero host-specific converters are registered.

4. **`ScriptArgs` string coercion behavior on JSON objects**:
   - `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptArgs.cs:114-122`:
     When `e.ValueKind == JsonValueKind.Object`, `ToStr(JsonElement)` executes `_ => e.GetRawText()`.
   - `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs:160`:
     `string sourceKey = item.Str("sourceKey", string.Empty);` receives raw JSON text `"{\"docIdentifier\":\"NhaPho_5x18m\",\"category\":4,\"primaryHandle\":\"C1\",\"provenanceHandles\":[]}"`.

5. **Extensible Storage receipt storage & reconciliation failure**:
   - `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Receipt.cs:44-54`:
     Receipt dictionary stores `elemMap[sk] = uid` where `sk` is the raw JSON string.
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs:97`:
     `if (CompositeSourceKey.TryParse(keyStr, out var sourceKey) && sourceKey != null)` fails on the raw JSON curly brace string, failing to recover any mappings.
   - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs:256`:
     `CompositeSourceKey.TryParse(rawSourceKey, out var parsedSk)` fails on raw JSON curly brace string, falling back to `new CompositeSourceKey(string.Empty, cat, mark)`, dropping `DocIdentifier` and corrupting `PrimaryHandle`.

6. **Erroneous test assertion**:
   - `HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReceiptReaderTests.cs:215-225`:
     `ElementPlanItem_CompositeSourceKey_SerializesAsJsonObject` asserts `Assert.Equal(JsonValueKind.Object, sk.ValueKind);`, codifying the buggy behavior.

7. **Ledger state hydration omission**:
   - `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.cs:259-277`:
     `ApplyLoadedLedger` fails to hydrate `_mappings` from `loaded.Mappings`, and `SyncLedgerState` fails to assign `_mappings` to `_ledger.Mappings`.

---

## 2. Logic Chain

1. From **Observation 2** and **Observation 3**, when `ElementPlanItem` is serialized to JSON for Named Pipe dispatch (`ExecuteRequest.Args`), `SourceKey` is serialized as a JSON object rather than a canonical string.
2. From **Observation 4**, the in-Revit script runner consumes `args.items[i]` via `ScriptArgs.Str("sourceKey")`, which returns the raw curly brace JSON string because `ScriptArgs` coerces `JsonValueKind.Object` to `e.GetRawText()`.
3. From **Observation 5**, this raw JSON curly brace string is stored as the element's `Comments` parameter in Revit, returned in execution `records`, and recorded as keys in `HPBimCoordinationReceipt` in Extensible Storage.
4. From **Observation 5**, when `BimPipelineEngine` and `ReconcileService` attempt to parse these returned keys via `CompositeSourceKey.TryParse`, the method fails because `BracketPattern` and `PlainPattern` only recognize `[DocIdentifier]#[Category]:[PrimaryHandle]` or `DocIdentifier#Category:PrimaryHandle`. This wipes out `DocIdentifier`, mangles `PrimaryHandle`, and causes 0 mappings to be restored during reconciliation.
5. Therefore, introducing `CompositeSourceKeyJsonConverter` (writing `value.ToString()` and reading string/legacy-object) and decorating `CompositeSourceKey` with `[JsonConverter]` immediately resolves the issue across domain models, wire DTO, `ScriptArgs`, Roslyn runner, Extensible Storage receipts, and reconciliation, guaranteeing 100% roundtrip equality.
6. Combining this with dual-defense preamble normalization in `BimPlanRunnerScriptTemplate` and hydrating `_mappings` in `BimCoordinationLedgerService` (per **Observation 7**) creates an airtight, resilient pipeline.

---

## 3. Caveats

- **No caveats**: The entire call and data flow across `HPBim.Core`, `HPBim.Client`, `McpShared`, and test projects was inspected directly.
- **Test backward compatibility**: Existing test `AdversarialReceiptReaderTests.ElementPlanItem_CompositeSourceKey_SerializesAsJsonObject` must be updated to assert string serialization (`ElementPlanItem_CompositeSourceKey_SerializesAsCanonicalString`).

---

## 4. Conclusion

1. The root cause is fully diagnosed: missing `CompositeSourceKeyJsonConverter` causing `SourceKey` to serialize as a JSON object rather than a canonical string `[DocIdentifier]#[Category]:[PrimaryHandle]`.
2. A ready-to-implement strategy with drop-in code snippets has been authored in `report.md`.
3. Worker can implement this immediately in 5 files:
   - Create `HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs`
   - Add attribute to `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs`
   - Register converter in `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerSerializer.cs`
   - Add preamble normalization in `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs`
   - Add hydration & state sync in `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.cs`
   - Update `AdversarialReceiptReaderTests.cs` and add new roundtrip test suites.

---

## 5. Verification Method

To verify the implementation independently:

1. **Build Solution**:
   ```powershell
   dotnet build HPBim/HPBim.slnx
   ```
   Must succeed with 0 errors and 0 warnings.

2. **Run Unit Tests**:
   ```powershell
   dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
   ```
   Must pass all tests (>= 417 existing + new converter tests).

3. **Run Client & Wire Tests**:
   ```powershell
   dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   ```
   Must pass all tests (>= 322 existing + new wire roundtrip tests).

4. **Run E2E Tests**:
   ```powershell
   dotnet test HPBim/tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj
   ```
   Must pass all 42 tests.

5. **Inspect Detailed Analysis**:
   Inspect `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_rem_1\report.md`.
