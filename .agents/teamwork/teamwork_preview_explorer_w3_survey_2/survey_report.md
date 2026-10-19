# HPBim Wave 3 Remediation — Survey Report: R3 & R4

**Author**: Survey Explorer 2  
**Date**: 2026-10-09  
**Target Solution**: `HPBim/HPBim.slnx`  
**Scope**:  
- **R3**: Separation of Persistent Document Identity from Dynamic DocumentVersion GUID (P1)  
- **R4**: Truthfulness of NoOp, Skip, and Rollback Semantics & ProvenanceHandles Preservation (P0/P1)

---

## Executive Summary

This survey report provides the technical investigation, exact code locations, root cause traces, and actionable remediation strategies for Requirements R3 and R4 of HPBim Wave 3 Remediation.

1. **R3 (Document Identity vs Dynamic Version)**:
   - **Root Cause of False Receipt Rejection**: At pre-flight plan compilation, `intent.TargetDoc.VersionGuid` captures the snapshot before execution ($V_1$). During element creation, Revit API automatically mutates the document version to $V_2$. When `ReconcileService` queries receipts from Extensible Storage via `ExtensibleStorageQueryScriptTemplate`, the receipt dynamically carries the live $V_2$. `ReconcileService.MatchSnapshotGuids` rejects the receipt because $V_1 \neq V_2$, leaving legitimate committed operations stranded in `OutcomeUnknown` and defeating self-recovery.
   - **Fail-Open Static Identity Vulnerability**: `MatchTargetDocSnapshot` in `ReconcileService.cs` (lines 153-159) and `MatchSnapshotGuids` (lines 175-183) fail open if `receipt.TargetDocPath` or `receipt.TargetCentralGuid` is null/empty. If the intent specified a path or CentralGuid, a receipt with missing fields erroneously matches.
   - **Remediation**: Decouple static/persistent identity (`Title`, `PathName`, `CentralGuid`) from dynamic revision state (`VersionGuid`, `NumberOfSaves`). Enforce strict static identity equality during receipt recovery (disallowing empty fields when intent specifies them), while removing the dynamic `VersionGuid` equality requirement from recovery. Retain `VersionGuid` checks exclusively in pre-flight concurrency gates.

2. **R4 (NoOp, Skip, Rollback Semantics & ProvenanceHandles)**:
   - **NoOp Semantic Hole**: `ValidateReturnedRecord` in `BimPipelineEngine.ElementRecords.cs` (line 154) checks `TargetUniqueId` for `Created` and `Updated`, but completely omits `NoOp`. A `NoOp` record returning empty or null `TargetUniqueId` passes validation unflagged.
   - **Permissive Substring Matching for Skipped**: `IsValidSkipReason` in `BimPipelineEngine.ElementRecords.cs` (lines 224-228) uses `.Contains(c, ...)` on arbitrary error reasons, allowing inadvertent substrings in descriptive error messages to masquerade as valid skips.
   - **Untruthful "Transaction rolled back" Reporting**: In `BimPipelineEngine.ElementRecords.cs` (lines 106-109, 185-203), when a post-dispatch integrity defect is detected client-side but the Revit host did *not* roll back (`execRes.RolledBack == false`), the pipeline sets `ERR_BATCH_ROLLED_BACK` and declares `"Transaction rolled back due to batch integrity defect"`. This violates truthfulness because elements may have been committed on the host. It must transition to `OutcomeUnknown`.
   - **ProvenanceHandles Dropped in JSON Serialization**: `CompositeSourceKeyJsonConverter.Write` (lines 105-120) unconditionally outputs `writer.WriteStringValue(value.ToString())`. `ToString()` returns `[Doc]#[Category]:PrimaryHandle`, dropping the entire `ProvenanceHandles` array. Any serialized `CompositeSourceKey` loses all provenance upon deserialization.

---

## 1. Deep Dive: Requirement R3

### 1.1 Code Inventory & Architecture Locations

| Component | File Path | Line Range | Role |
|---|---|---|---|
| Domain Model | `HPBim/src/HPBim.Core/Execution/DocumentIdentitySnapshot.cs` | 1–64 | Immutable snapshot model (`Title`, `PathName`, `VersionGuid`, `NumberOfSaves`, `CentralGuid`). Contains `Matches()`. |
| Plan Hash | `HPBim/src/HPBim.Core/Execution/BimExecutionPlan.cs` | 43–65 | `ComputePlanHash` hashes `Title`, `PathName`, `VersionGuid`. |
| Query Service | `HPBim/src/HPBim.Client/Pipeline/DocumentIdentityQuery.cs` | 28–43, 76–97 | Executes `QueryScript` in Revit (None transaction) to read `doc.Title`, `doc.PathName`, `docVer.VersionGUID`, `doc.WorksharingCentralGUID`. |
| Pre-Flight Gate | `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs` | 53–83 | Pre-flight script asserting `doc.Title`, `doc.PathName`, and `actualVersionGuid == expectedVersionGuid`. |
| Receipt Storage Query | `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs` | 31–65 | Queries receipts in Revit Extensible Storage. Attaches current live `currentTitle`, `currentPath`, `currentVerGuid`, `currentCentGuid`. |
| Receipt Model | `HPBim/src/HPBim.Client/Recovery/CoordinationReceiptRecord.cs` | 9–20 | DTO representing receipt from Extensible Storage. |
| Reconcile Engine | `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` | 101–186 | `VerifyThreeFactors`, `MatchTargetDocId`, `MatchTargetDocSnapshot`, `MatchSnapshotGuids`. |

### 1.2 Root Cause Trace: Dynamic VersionGuid Mutation & False Receipt Rejection

1. **Pre-flight Plan Generation**:
   Before dispatching a plan, `DocumentIdentityQuery.QueryActiveDocumentAsync` queries Revit:
   ```csharp
   var docVer = Document.GetDocumentVersion(doc);
   // versionGuid = V_1 (e.g., 11111111-1111-1111-1111-111111111111)
   ```
   `ExecutionPlanBuilder` attaches this snapshot to `plan.TargetDoc`. `IntentJournal.RecordIntentAsync` persists it into `intent.TargetDoc.VersionGuid = V_1`.

2. **Revit Execution & Element Mutation**:
   `BimPlanRunnerScriptTemplate` executes on the Revit main thread under `TransactionModes.Auto`.
   New elements (`Column`, `Wall`, `Beam`, etc.) are created and committed.
   In Revit DB architecture, whenever elements are created or modified and the transaction is committed, `Document.GetDocumentVersion(doc).VersionGUID` **automatically advances to a new GUID** ($V_2$).

3. **Receipt Recovery Query**:
   When `ReconcileService.ReconcileIntentAsync` runs (either upon reconnect, crash recovery, or post-timeout audit), it queries Revit DataStorage entities using `ExtensibleStorageQueryScriptTemplate`:
   ```csharp
   // ExtensibleStorageQueryScriptTemplate.cs lines 31-34
   var docVer = Document.GetDocumentVersion(doc);
   string currentVerGuid = docVer != null ? docVer.VersionGUID.ToString("D") : Guid.Empty.ToString("D");
   // ...
   receipts.Add(new {
       // ...
       targetVersionGuid = currentVerGuid, // returns V_2
   });
   ```

4. **False Receipt Rejection in ReconcileService**:
   `ReconcileService.VerifyThreeFactors` evaluates:
   - Factor 1: `intent.OperationId == receipt.OperationId` $\rightarrow$ **MATCH**
   - Factor 2: `intent.PlanHash == receipt.PlanHash` $\rightarrow$ **MATCH**
   - Factor 3: `MatchTargetDocId(intent, receipt)` calls `MatchTargetDocSnapshot`, which calls `MatchSnapshotGuids`:
   ```csharp
   // ReconcileService.cs lines 166-173
   if (targetDoc.VersionGuid != Guid.Empty &&
       !string.IsNullOrEmpty(receipt.TargetVersionGuid) &&
       Guid.TryParse(receipt.TargetVersionGuid, out var verGuid) &&
       verGuid != Guid.Empty &&
       targetDoc.VersionGuid != verGuid) // V_1 != V_2 is TRUE
   {
       return false; // REJECTED!
   }
   ```
   Because $V_1 \neq V_2$, `MatchSnapshotGuids` returns `false`.
   `VerifyThreeFactors` returns `false`.
   `ReconcileService` concludes that the receipt does not match, leaves the intent status as `OutcomeUnknown`, and refuses to hydrate the ledger. Self-recovery is broken!

### 1.3 Analysis of Fail-Open Static Identity Vulnerabilities

In `ReconcileService.cs`:
1. **PathName Fail-Open (lines 153–159)**:
   ```csharp
   if (!string.IsNullOrEmpty(targetDoc.PathName) && !string.IsNullOrEmpty(receipt.TargetDocPath))
   {
       if (!string.Equals(targetDoc.PathName, receipt.TargetDocPath, StringComparison.OrdinalIgnoreCase))
           return false;
   }
   ```
   *Vulnerability*: If `targetDoc.PathName` is set to `"C:\Projects\Structural.rvt"`, but `receipt.TargetDocPath` is empty (e.g. unsaved model or path not returned), `!string.IsNullOrEmpty(receipt.TargetDocPath)` evaluates to `false`. The block is skipped and matching succeeds! This allows an operation targeted to a specific saved project path to match an unsaved or differently-routed document.

2. **CentralGuid Fail-Open (lines 175–183)**:
   ```csharp
   if (targetDoc.CentralGuid.HasValue &&
       targetDoc.CentralGuid.Value != Guid.Empty &&
       !string.IsNullOrEmpty(receipt.TargetCentralGuid) &&
       Guid.TryParse(receipt.TargetCentralGuid, out var centGuid) &&
       centGuid != Guid.Empty &&
       targetDoc.CentralGuid.Value != centGuid)
   {
       return false;
   }
   ```
   *Vulnerability*: If `targetDoc.CentralGuid` is specified (e.g. workshared central GUID $G_1$), but `receipt.TargetCentralGuid` is empty or null (e.g. detached model or non-workshared clone), the `if` check evaluates to `false` and execution drops through to `return true;`.

3. **Pre-Flight PathName Fail-Open (`BimPlanRunnerScriptTemplate.cs` line 69)**:
   ```csharp
   bool pathMismatch = hasExpectedPath && !string.IsNullOrEmpty(doc.PathName) && !string.Equals(doc.PathName, expectedPath, StringComparison.OrdinalIgnoreCase);
   ```
   *Vulnerability*: If `expectedPath` is provided but `doc.PathName` is empty, `pathMismatch` is `false`. Pre-flight executes on an unsaved model when a specific file path was required.

### 1.4 Recommended Strategy for R3

1. **Clear Architectural Separation**:
   - **Persistent Static Identity**: Properties that identify the document across transactions and saves:
     - `Title`: Document Title (file name or project title).
     - `PathName`: Absolute file path on disk (when document is saved).
     - `CentralGuid`: Worksharing central model GUID (when workshared).
   - **Dynamic Revision State**: Properties that reflect a point-in-time modification index:
     - `VersionGuid`: Updates on every element mutation/transaction.
     - `NumberOfSaves`: Increments on each save.

2. **Scope Rules**:
   - **Pre-Flight Gate (`BimPlanRunnerScriptTemplate.cs`)**:
     Validates both Static Identity and Dynamic `VersionGuid`. Before starting a mutation batch, we must guarantee that the active document is the exact one intended and has not been concurrently modified since plan generation (`ERR_STALE_DOCUMENT_SNAPSHOT`).
     *Fix fail-open*: If `hasExpectedPath` is true, require `!string.IsNullOrEmpty(doc.PathName) && string.Equals(doc.PathName, expectedPath, StringComparison.OrdinalIgnoreCase)`.
   - **Recovery / Reconcile Gate (`ReconcileService.cs`)**:
     Validates **Static Identity ONLY**.
     - `Title`: If `targetDoc.Title` is present, `actualTitle` must be non-empty and match (case-insensitive).
     - `PathName`: If `targetDoc.PathName` is present, `receipt.TargetDocPath` must be non-empty and match (case-insensitive). If missing, return `false` (no fail-open).
     - `CentralGuid`: If `targetDoc.CentralGuid` is present and non-empty, `receipt.TargetCentralGuid` must be non-empty, parse to a valid GUID, and match `targetDoc.CentralGuid.Value`. If missing, return `false` (no fail-open).
     - `VersionGuid`: **Do NOT compare `targetDoc.VersionGuid` with `receipt.TargetVersionGuid` during recovery.** Because element creation mutates `VersionGuid`, a difference between the pre-dispatch snapshot and post-commit receipt is expected and healthy.
   - **Update Domain Helper in `DocumentIdentitySnapshot.cs`**:
     Add `public bool MatchesPersistentIdentity(string actualTitle, string? actualPathName, Guid? actualCentralGuid = null)` to provide a single, tested source of truth for persistent identity comparison.

---

## 2. Deep Dive: Requirement R4

### 2.1 Code Inventory & Architecture Locations

| Component | File Path | Line Range | Role |
|---|---|---|---|
| Element Records Reconciliation | `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` | 18–39, 85–112 | `ParseElementRecords`, `ReconcileRecordsStrict1To1`, `IsIntegrityDefect`. |
| Item Validation | `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` | 141–170 | `ValidateReturnedRecord` (validates Category, Mark, TargetUniqueId, Skipped, Rollback). |
| Skip Policy | `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` | 13–16, 221–228 | `ValidSkipReasonCodes`, `IsSkipAllowed`, `IsValidSkipReason`. |
| Strict Batch Rollback | `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` | 185–203 | `ApplyStrictBatchRollback` (sets error reasons and batch rollbacks). |
| Plan Execution Pipeline | `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs` | 21–53, 102–135 | `ExecutePlanAsync`, `ResolvePlanExecutionOutcome`, `PopulateTargetUniqueIds`. |
| JSON Converter | `HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs` | 13–121 | Serializes and deserializes `CompositeSourceKey`. |
| Value Object | `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs` | 12–144 | `CompositeSourceKey` record definition with `ProvenanceHandles`. |

### 2.2 Trace NoOp Validation: Missing TargetUniqueId & Ledger Verification

1. **Current Code in `ValidateReturnedRecord`**:
   ```csharp
   // BimPipelineEngine.ElementRecords.cs lines 154-158
   if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
   {
       return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_MISSING_TARGET_UNIQUE_ID",
           ErrorReason = $"Element '{item.Mark}' with status '{rec.Status}' MUST have a non-empty TargetUniqueId." };
   }
   ```
2. **The Defect**:
   Notice that the condition specifically checks `is ElementExecutionStatus.Created or ElementExecutionStatus.Updated`.
   It **completely ignores `ElementExecutionStatus.NoOp`**!
   If Revit returns:
   ```json
   { "sourceKey": "[Doc]#[Column]:C1", "category": "Column", "mark": "C1", "status": "NoOp", "targetUniqueId": "" }
   ```
   The validation passes!
3. **Why this violates Truthfulness & Idempotency**:
   A `NoOp` status asserts: "This element was already created in Revit and its state in the document matches the plan; therefore no modification was made."
   If an element is a `NoOp`, it *must* have an identity in Revit (`targetUniqueId`). A `NoOp` without a `targetUniqueId` is fictitious.
4. **Remediation**:
   - Include `ElementExecutionStatus.NoOp` in the check:
     ```csharp
     if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated or ElementExecutionStatus.NoOp && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
     {
         return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_MISSING_TARGET_UNIQUE_ID",
             ErrorReason = $"Element '{item.Mark}' with status '{rec.Status}' MUST have a non-empty TargetUniqueId." };
     }
     ```
   - Enforce Ledger / Plan Target Matching:
     When an element is planned with an expected `item.TargetUniqueId` (pre-populated from ledger via `PopulateTargetUniqueIds`), if the returned record has `NoOp`, `rec.TargetUniqueId` must match `item.TargetUniqueId`:
     ```csharp
     if (rec.Status == ElementExecutionStatus.NoOp &&
         !string.IsNullOrEmpty(item.TargetUniqueId) &&
         !string.Equals(rec.TargetUniqueId, item.TargetUniqueId, StringComparison.OrdinalIgnoreCase))
     {
         return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_NOOP_TARGET_MISMATCH",
             ErrorReason = $"NoOp element '{item.Mark}' returned TargetUniqueId '{rec.TargetUniqueId}' which does not match ledger TargetUniqueId '{item.TargetUniqueId}'." };
     }
     ```

### 2.3 Trace Skipped Validation: Permissive Substring Matching (.Contains)

1. **Current Code**:
   ```csharp
   // BimPipelineEngine.ElementRecords.cs lines 224-228
   private static bool IsValidSkipReason(string? code, string? reason)
   {
       bool Match(string? s) => !string.IsNullOrWhiteSpace(s) && ValidSkipReasonCodes.Any(c => s.Contains(c, StringComparison.OrdinalIgnoreCase));
       return Match(code) || Match(reason);
   }
   ```
2. **The Flaws**:
   - `s.Contains(c)` checks if the code is a substring of `code` OR `reason`.
   - If an error reason happens to include words like `"Failed because SKIP_FROZEN_ZONE was invalid"`, or `"Cannot proceed: SKIP_PRECONDITION_FAILED is not applicable"`, `Match(reason)` returns `true`! An unhandled exception or failed check is falsely approved as an intentional skip.
   - In `IsSkipAllowed` (lines 221-222):
     ```csharp
     private static bool IsSkipAllowed(ElementPlanItem item, ElementExecutionRecord rec) =>
         (item.Parameters.TryGetValue("SkipAllowed", out var sa) && sa is bool b && b) || IsValidSkipReason(rec.ErrorCode, rec.ErrorReason);
     ```
     If `item.Parameters["SkipAllowed"] == true`, ANY arbitrary reason is accepted without validating reason code.
3. **Remediation**:
   - Eliminate `.Contains` substring matching.
   - Require exact code equality on `rec.ErrorCode`:
     `!string.IsNullOrWhiteSpace(rec.ErrorCode) && ValidSkipReasonCodes.Contains(rec.ErrorCode)`.
   - Provide strongly typed enum or constant policy: `SkipReasonCodes` (`SKIP_FROZEN_ZONE`, `SKIP_STAGE_DISABLED`, `SKIP_PRECONDITION_FAILED`, `SKIP_PREREQUISITE_FAILED`, `SKIP_USER_EXCLUDED`).
   - If an item permits skips, check whether `item.Parameters` specifies allowed skip codes (e.g. `AllowedSkipCodes` list) and verify `rec.ErrorCode` belongs to that exact list. If not, reject with `ERR_INTEGRITY_DEFECT_SKIPPED`.

### 2.4 Trace Post-Dispatch Integrity Defect Handling: Untruthful "Transaction rolled back"

1. **Current Code in `ReconcileRecordsStrict1To1` & `ApplyStrictBatchRollback`**:
   ```csharp
   // Lines 106-109
   if (hasIntegrityDefect || isFailedRollback)
   {
       return ApplyStrictBatchRollback(reconciled, foreignRecords, duplicateKeys);
   }
   ```
   And in `ApplyStrictBatchRollback` (lines 194-201):
   ```csharp
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
   ```
2. **The Lie**:
   Suppose Revit executes the batch successfully from its perspective:
   `execRes.IsError = false; execRes.RolledBack = false;` (the transaction was committed, elements are in the RVT document).
   However, the records returned contain an integrity defect:
   - A foreign record was returned (`ERR_FOREIGN_RECORD`)
   - A duplicate record was returned (`ERR_DUPLICATE_RECORD`)
   - An invalid enum string was returned (`ERR_INVALID_STATUS_ENUM`)
   - An item was returned with status `Created` but missing `TargetUniqueId` (`ERR_MISSING_TARGET_UNIQUE_ID`)
   - An item was skipped with an unauthorized skip reason (`ERR_INTEGRITY_DEFECT_SKIPPED`)
   The client detects `hasIntegrityDefect == true`.
   Because `ApplyStrictBatchRollback` runs unconditionally, it sets:
   `Status = ElementExecutionStatus.Failed`, `ErrorCode = "ERR_BATCH_ROLLED_BACK"`, and
   `ErrorReason = "Transaction rolled back due to batch integrity defect..."`!
   **Revit NEVER rolled back the transaction!** The elements exist in the Revit model.
   The client is claiming a rollback occurred when no rollback was performed by the host.
3. **Remediation & Transition to `OutcomeUnknown`**:
   The handling must strictly bifurcate based on whether host rollback was confirmed:
   - **Case A: Host Confirmed Rollback** (`isFailedRollback == true` / `execRes.RolledBack == true`):
     The host rolled back the transaction. It is truthful to report `Status = ElementExecutionStatus.Failed`, `ErrorCode = "ERR_BATCH_ROLLED_BACK"`, and `ErrorReason = "Transaction rolled back by Revit bridge."`
   - **Case B: Post-Dispatch Integrity Defect Without Host Rollback** (`isFailedRollback == false` && `hasIntegrityDefect == true`):
     The host transaction was NOT rolled back. Elements may exist in the document.
     The client CANNOT report "Transaction rolled back".
     The result MUST be marked **`OutcomeUnknown`**:
     - On the defective item: `Status = ElementExecutionStatus.Failed` (preserving the specific defect code, e.g. `ERR_MISSING_TARGET_UNIQUE_ID`).
     - On all other batch items: `Status = ElementExecutionStatus.OutcomeUnknown`, `ErrorCode = "ERR_POST_DISPATCH_INTEGRITY_DEFECT"`, and `ErrorReason = $"Host transaction was NOT rolled back, but post-dispatch integrity defect occurred: [{firstDefect.ErrorCode}] {firstDefect.ErrorReason}. Model state is ambiguous; reconciliation required."`
     - In `ResolvePlanExecutionOutcome`: `isOutcomeUnknown = true; success = false;`.
     - This correctly triggers auto-reconciliation via Extensible Storage receipts, discovering what was actually committed rather than falsifying a rollback.

### 2.5 Trace `CompositeSourceKeyJsonConverter`: The `ProvenanceHandles` Drop Bug

1. **Current Code in `CompositeSourceKeyJsonConverter.cs`**:
   ```csharp
   // CompositeSourceKeyJsonConverter.cs lines 105-120
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
   ```
2. **The Bug**:
   Look at line 119:
   `writer.WriteStringValue(value.ToString());`
   And in `CompositeSourceKey.cs` line 106:
   `public override string ToString() => $"[{DocIdentifier}]#[{Category}]:{PrimaryHandle}";`
   The property `ProvenanceHandles` (defined at line 26: `public IReadOnlyList<string> ProvenanceHandles { get; init; }`) is **never written**!
   Whenever any `CompositeSourceKey` with provenance handles (e.g. `["h1", "h2"]`) is serialized to JSON:
   It serializes strictly to the canonical string: `"[Doc]#[Category]:PrimaryHandle"`.
   All provenance handles are discarded.
   When deserialized back via `JsonSerializer.Deserialize<CompositeSourceKey>`:
   `reader.TokenType == JsonTokenType.String` calls `CompositeSourceKey.TryParse(raw, out var parsed)`, which constructs:
   `new CompositeSourceKey(doc, category, handle)` with `ProvenanceHandles = Array.Empty<string>()`.
3. **Notice in `CompositeSourceKeyJsonConverter.Read`**:
   Lines 31-34 already have full support for deserializing an object:
   ```csharp
   if (reader.TokenType == JsonTokenType.StartObject)
   {
       return ReadLegacyJsonObject(ref reader);
   }
   ```
   `ReadLegacyJsonObject` (lines 39-70) correctly extracts `provenanceHandles` array and constructs `new CompositeSourceKey(docId, category, primaryHandle, provenance)`.
   The omission was purely in `Write`!
4. **Remediation**:
   Update `CompositeSourceKeyJsonConverter.Write`:
   ```csharp
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

       if (value.ProvenanceHandles != null && value.ProvenanceHandles.Count > 0)
       {
           writer.WriteStartObject();
           writer.WriteString("docIdentifier", value.DocIdentifier);
           writer.WriteString("category", value.Category.ToString());
           writer.WriteString("primaryHandle", value.PrimaryHandle);
           writer.WriteStartArray("provenanceHandles");
           foreach (var handle in value.ProvenanceHandles)
           {
               writer.WriteStringValue(handle);
           }
           writer.WriteEndArray();
           writer.WriteEndObject();
           return;
       }

       writer.WriteStringValue(value.ToString());
   }
   ```
   Also add dictionary property name handling:
   ```csharp
   public override void WriteAsPropertyName(Utf8JsonWriter writer, CompositeSourceKey value, JsonSerializerOptions options) =>
       writer.WritePropertyName(value?.ToString() ?? string.Empty);

   public override CompositeSourceKey ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
       string? raw = reader.GetString();
       return !string.IsNullOrWhiteSpace(raw) && CompositeSourceKey.TryParse(raw, out var key) ? key! : CompositeSourceKey.Empty;
   }
   ```
   This ensures:
   - When `ProvenanceHandles` are present, they are preserved 100% roundtrip.
   - When `ProvenanceHandles` are absent, it writes the canonical string `"[Doc]#[Category]:PrimaryHandle"`, maintaining 100% wire and schema compatibility.

---

## 3. Impact on Existing Tests & Test Suite Analysis

A full test run of `HPBim.slnx` (`HPBim.Core.Tests`, `HPBim.Client.Tests`, `HPBim.E2E.Tests`) passes **965 / 965 tests (100% green)** in 4.9 seconds.

### 3.1 Tests Requiring Updates or Additions for R3:
- `ReconcileDocumentIdentityTests.cs`:
  - `MatchDocumentIdentity_SameTitleDifferentVersionGuid_ReturnsFalse_PreservesOutcomeUnknown` (lines 72-112):
    This test in Wave 2 asserted that different `VersionGuid` caused rejection.
    Under Wave 3 R3, dynamic `VersionGuid` mutation after element creation MUST NOT cause receipt rejection. This test must be updated to verify the new rule: when `Title`, `PathName`, and `CentralGuid` match, dynamic `VersionGuid` differences between intent and receipt do NOT reject the receipt (`Dynamic DocumentVersion Resilience Test`).
  - Add test: `MatchDocumentIdentity_IntentHasPath_ReceiptMissingPath_ReturnsFalse_PreservesOutcomeUnknown` (verifying fail-open is closed).
  - Add test: `MatchDocumentIdentity_IntentHasCentralGuid_ReceiptMissingCentralGuid_ReturnsFalse_PreservesOutcomeUnknown` (verifying fail-open is closed).

### 3.2 Tests Requiring Updates or Additions for R4:
- `BimPipelineEngineRecordReconciliationTests.cs`:
  - Existing tests (lines 110-137, 158-161, 218-221) set `CustomExecuteHandler` with `RolledBack = false` and currently assert `ERR_BATCH_ROLLED_BACK`.
  - When updated to truthfulness: when `RolledBack = false` and an integrity defect occurs, the non-defective items must be asserted as `OutcomeUnknown` with `ERR_POST_DISPATCH_INTEGRITY_DEFECT`, and `result.IsOutcomeUnknown == true` (not `ERR_BATCH_ROLLED_BACK`).
- `BimPipelineEngineRecordReconciliationTests.SkipAndRollback.cs`:
  - Add test: `ExecutePlanAsync_NoOpWithMissingTargetUniqueId_RejectsAsIntegrityDefect`.
  - Add test: `ExecutePlanAsync_NoOpWithTargetUniqueIdMismatch_RejectsAsIntegrityDefect`.
  - Add test: `ExecutePlanAsync_SkippedWithArbitraryReasonSubstring_Rejected`.
  - Update `ExecutePlanAsync_PlanItemWithSkipAllowedParameter_PreservesSkippedStatus` to adhere to standardized skip policy.
- `CompositeSourceKeyJsonConverterTests.cs`:
  - Add test: `Roundtrip_WithProvenanceHandles_PreservesAllHandlesStrictEquality` verifying 100% roundtrip preservation of `ProvenanceHandles`.

---

## 4. Clean Code Governance Compliance Matrix

Applies to all upcoming changes per `docs/clean-code/HP_CLEAN_CODE_CORE.md`:

| Rule | Requirement | Target File / Area | Pre-Remediation Status | Remediation Guidance |
|---|---|---|---|---|
| **C2** | File $\le 300$ lines | `BimPipelineEngine.ElementRecords.cs` | Currently **275 lines** | **Critical**: High risk of exceeding 300 lines. Split helper validation methods into a new partial file: `BimPipelineEngine.ElementRecords.Validation.cs`. |
| **C2** | File $\le 300$ lines | `ReconcileService.cs` | Currently **229 lines** | Safe (~240 lines after clean static identity matching). |
| **C2** | File $\le 300$ lines | `CompositeSourceKeyJsonConverter.cs` | Currently **122 lines** | Safe (~145 lines after `Write` update). |
| **C2** | File $\le 300$ lines | `DocumentIdentitySnapshot.cs` | Currently **64 lines** | Safe (~85 lines after `MatchesPersistentIdentity`). |
| **M3** | Method $\le 50$ lines | `ReconcileRecordsStrict1To1`, `ValidateReturnedRecord`, `ApplyStrictBatchRollback` | All currently $\le 30$ lines | Maintain short methods; decompose batch outcome dispatching. |
| **Q-B1** | 0 Commented-out code | All files | Clean | Ensure no commented code in pull requests. |
| **Q-B2** | 0 Unannotated empty catch | All files | Clean | Any catch block must have clear explanatory rationale comment. |
| **N** | Clear naming | `IsSkipAllowed`, `IsIntegrityDefect` | Good | Avoid vague names like `data`, `obj`, `tmp`. |
| **R** | Dependency Rules | `HPBim.Core` | Pure `netstandard2.0` | Zero references to `Autodesk.Revit.*`. |

---

## 5. Next Steps for Implementer

1. **Step 1 (R3 Core)**: Update `DocumentIdentitySnapshot.cs` with `MatchesPersistentIdentity`.
2. **Step 2 (R3 Client)**: Refactor `MatchTargetDocSnapshot` and `MatchSnapshotGuids` in `ReconcileService.cs`: eliminate dynamic `VersionGuid` equality requirement in recovery; close fail-open gaps on `PathName` and `CentralGuid`.
3. **Step 3 (R4 Core)**: Update `CompositeSourceKeyJsonConverter.cs` to write JSON object when `ProvenanceHandles.Count > 0`, and implement property name overrides.
4. **Step 4 (R4 Client)**: Split `BimPipelineEngine.ElementRecords.cs` into `BimPipelineEngine.ElementRecords.Validation.cs` to respect Rule C2 ($\le 300$ lines). Enforce non-empty `TargetUniqueId` for `NoOp` and ledger matching. Replace loose `.Contains` skip matching with exact enum/code validation. Transition post-dispatch integrity defects without host rollback to `OutcomeUnknown`.
5. **Step 5 (Tests)**: Update existing tests in `ReconcileDocumentIdentityTests.cs` and `BimPipelineEngineRecordReconciliationTests.cs`, and add new test cases for `ProvenanceHandles` roundtrip, `NoOp` validation, and dynamic version resilience.
