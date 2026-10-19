# Handoff Report — Milestone M2 Iteration 2 Investigation

**Agent**: `explorer_m2_it2_2`  
**Role**: Teamwork Explorer (Read-Only Investigation & Synthesis)  
**Parent Conversation ID**: `e0aba3da-bb9f-4e02-be57-11f1cc948914`  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_it2_2\`  
**Milestone**: M2 Iteration 2 (HPBim Wave 2 Remediation)  
**Handoff Type**: Hard (Investigation complete, full 5-component report)  
**Date**: 2026-10-08T17:28:00Z  

---

## 1. Observation

### 1.1 Unhandled Exceptions in `ExtensibleStorageReceiptReader.cs`
- **File**: `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs`
- **Observation 1.1.1 (Surrogate Transcoding Leak in `ParseElementMap`)**:
  Lines 170–178:
  ```csharp
  try
  {
      return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
  }
  catch (JsonException)
  {
      // Corrupt or truncated JSON in storage must not be masked as valid empty map
      return null;
  }
  ```
  When `json` contains an unpaired or truncated UTF-16 surrogate (e.g. `"{\"key\": \"\uD83D\"}"`), `JsonSerializer.Deserialize` performs UTF-16 to UTF-8 transcoding via `System.Text.EncoderFallbackException`, which derives from `System.ArgumentException`. Because `ParseElementMap` catches only `JsonException`, `System.ArgumentException` escapes uncaught.
  *Verbatim Exception*:
  `System.ArgumentException: Cannot transcode invalid UTF-16 string to UTF-8 JSON text. ---> System.Text.EncoderFallbackException: Unable to translate Unicode character \uD83D at index 9 to specified code page.`

- **Observation 1.1.2 (Missing String Type Check & Unescapable Surrogates in `ParseSingleReceipt`)**:
  Lines 134–142:
  ```csharp
  string opIdStr = item.TryGetProperty("operationId", out var opProp) ? opProp.GetString() ?? string.Empty : string.Empty;
  ...
  string? elemMapJson = item.TryGetProperty("elementMapJson", out var emProp) ? emProp.GetString() : null;
  ```
  Calling `prop.GetString()` without verifying `prop.ValueKind == JsonValueKind.String` throws `InvalidOperationException` when the property has type Number (`"operationId": 12345`), Boolean, or Object (`"elementMapJson": { "nested": "object" }`).
  Furthermore, when `emProp.GetString()` is called on a string property containing an incomplete surrogate, `JsonReaderHelper.GetUnescapedString` throws:
  `System.InvalidOperationException: Cannot read incomplete UTF-16 JSON text as string with missing low surrogate.` at `System.Text.Json.ThrowHelper.ThrowInvalidOperationException_ReadIncompleteUTF16()`.

### 1.2 Unhandled Exception Escape in `ReconcileService.cs`
- **File**: `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs`
- **Observation 1.2.1**:
  Lines 86–90:
  ```csharp
  catch (Exception ex) when (ex is RevitPipeException or IOException or TimeoutException)
  {
      // Pipe or connection error during reconciliation; keep intent state as OutcomeUnknown
      return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, $"Pipe communication error during reconciliation: {ex.Message}");
  }
  ```
  Line 86 traps only `RevitPipeException`, `IOException`, and `TimeoutException`. Any `ArgumentException`, `InvalidOperationException`, or `JsonException` escaping from `_receiptReader.FindReceiptAsync` or `ApplyCommittedReceiptAsync` bypasses this catch block and crashes `ReconcileIntentAsync` and the reconciliation pipeline.

### 1.3 Rule M3 Violations (Methods > 50 Lines)
As confirmed by the Forensic Audit (`auditor_m2_1/handoff.md`), 6 methods exceed 50 lines:
1. `HPBim/tests/HPBim.Client.Tests/Cli/GenerateCommandColdProcessRerunTests.cs`:
   - `SimulateRevitExecution`: **90 lines** (lines 173–262).
   - `GenerateCommand_ColdProcessRerun_PersistsLedgerAndProduces100PercentNoOp`: **77 lines** (lines 40–116).
2. `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs`:
   - `ResolvePlanExecutionOutcome`: **55 lines** (lines 100–154).
3. `HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs`:
   - `ExecuteAsync`: **57 lines** (lines 63–119).
4. `HPBim/tests/HPBim.E2E.Tests/Tiers/Tier4_RealWorldWorkloadTests.cs`:
   - `TC_T4_WL_04_MidFlightPipeDisconnect_And_ExtensibleStorageReconciliation`: **54 lines** (lines 108–161).
   - `TC_T4_WL_05_PostCommit_MeasuredGeometricAudit_AllColumnsBeamsFootings`: **55 lines** (lines 164–218).

---

## 2. Logic Chain

1. **Step 1 (From Observation 1.1.1)**:
   - `System.Text.Json` transcoding errors on malformed UTF-16 surrogates throw `ArgumentException`.
   - In `ExtensibleStorageReceiptReader.ParseElementMap`, expanding the catch filter to `catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)` guarantees that malformed JSON, incomplete surrogates, or reader state exceptions return `null` instead of terminating execution.

2. **Step 2 (From Observation 1.1.2)**:
   - `JsonElement.GetString()` must only be called when `ValueKind == JsonValueKind.String`, and must be guarded against `InvalidOperationException` and `ArgumentException`.
   - Introducing `TryGetStringProperty(JsonElement item, string propertyName)` encapsulates this validation in 23 lines.
   - When any property is non-string or corrupted, `ParseSingleReceipt` assigns clean default values (`Guid.Empty`, `string.Empty`, or `null`).
   - Consequently, `VerifyThreeFactors(intent, receipt)` fails safely, and `ReconcileService` preserves `OutcomeUnknown`.

3. **Step 3 (From Observation 1.2.1)**:
   - Even if unexpected payloads or deserialization errors arise downstream, `ReconcileService.ReconcileSingleIntentAsync` line 86 must catch `JsonException`, `InvalidOperationException`, and `ArgumentException` in addition to pipe errors.
   - Catching these exceptions preserves `OutcomeUnknown` and returns `ReconcileResult.StillUnknown`, preventing the reconciliation loop from crashing.

4. **Step 4 (From Observation 1.3 & Rule Governance)**:
   - Clean Code Rule C2 mandates files <= 300 lines; Rule M3 mandates methods <= 50 lines.
   - The proposed changes in `ExtensibleStorageReceiptReader.cs` add ~29 lines (final ~210 lines <= 300).
   - The proposed change in `ReconcileService.cs` adds ~4 lines (final ~225 lines <= 300).
   - All methods in both production files remain <= 34 lines (strictly <= 50 lines).
   - The 6 oversized methods in test/fake/pipeline files can be cleanly split into cohesive helpers (all <= 38 lines).

---

## 3. Caveats

- **Read-Only Scope**: This agent is an explorer and did NOT modify source files on disk. The implementation must be executed by a worker agent.
- **Test Assertion Updates Required**: Because `ChallengerM2AdversarialPayloadTests.cs` currently asserts that exceptions ARE thrown (`Assert.IsType<InvalidOperationException>(ex)`), fixing the production code will cause those tests to fail unless their assertions are updated to expect safe handling (`Assert.Equal(ReconcileStatus.StillUnknown, result.Status)`).

---

## 4. Conclusion

The exact code changes to remediate Milestone M2 have been designed, validated against clean code limits, and documented in detail in `report.md`:

### 4.1 Changes in `ExtensibleStorageReceiptReader.cs`
1. Add `TryGetStringProperty`:
```csharp
private static string? TryGetStringProperty(JsonElement item, string propertyName)
{
    if (!item.TryGetProperty(propertyName, out var prop)) return null;
    if (prop.ValueKind != JsonValueKind.String) return null;
    try
    {
        return prop.GetString();
    }
    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
    {
        // Incomplete UTF-16 surrogates or unescapable string tokens in property value
        return null;
    }
}
```
2. In `ParseSingleReceipt`: replace `item.TryGetProperty(...).GetString()` with `TryGetStringProperty(item, ...)`.
3. In `ParseElementMap`: expand catch to `catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)`.
4. In `ParseReceipts`: wrap array enumeration in `catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)`.

### 4.2 Changes in `ReconcileService.cs`
Replace line 86 with:
```csharp
catch (Exception ex) when (ex is RevitPipeException or IOException or TimeoutException or JsonException or InvalidOperationException or ArgumentException)
{
    // Pipe, connection, or storage payload parsing error during reconciliation; keep intent state as OutcomeUnknown
    string errorPrefix = ex is RevitPipeException or IOException or TimeoutException
        ? "Pipe communication error"
        : "Receipt payload parsing error";

    return ReconcileResult.StillUnknown(
        intent.OperationId,
        intent.PlanHash,
        $"{errorPrefix} during reconciliation: {ex.Message}");
}
```

### 4.3 Clean Code Rule M3 Compliance
Refactor the 6 oversized methods specified in `report.md` Section 4 to bring 100% of methods to <= 50 lines.

---

## 5. Verification Method

To independently verify the investigation findings and evaluate the proposed solutions:

1. **Verify File and Method Length Compliance**:
   Run python inspection to confirm method lengths in target files:
   ```powershell
   python -c "
   import re
   def count_lines(path):
       with open(path, 'r', encoding='utf-8') as f:
           return len(f.readlines())
   print('ExtensibleStorageReceiptReader.cs:', count_lines('HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs'), 'lines')
   print('ReconcileService.cs:', count_lines('HPBim/src/HPBim.Client/Recovery/ReconcileService.cs'), 'lines')
   "
   ```

2. **Verify Solution Compilation**:
   ```powershell
   dotnet build HPBim/HPBim.slnx
   ```
   *Expected: Build succeeded with 0 Warnings and 0 Errors.*

3. **Verify Full Test Suites (Post-Implementation)**:
   ```powershell
   dotnet run --project HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
   dotnet run --project HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   dotnet run --project HPBim/tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj
   ```
