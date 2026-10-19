# Handoff Report: Survey Explorer 2 (Wave 3 Remediation R3 & R4)

## 1. Observation

1. **R3 — False Receipt Rejection on Dynamic VersionGuid**:
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` lines 166–173:
     ```csharp
     if (targetDoc.VersionGuid != Guid.Empty &&
         !string.IsNullOrEmpty(receipt.TargetVersionGuid) &&
         Guid.TryParse(receipt.TargetVersionGuid, out var verGuid) &&
         verGuid != Guid.Empty &&
         targetDoc.VersionGuid != verGuid)
     {
         return false;
     }
     ```
   - `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs` lines 31–34, 60:
     ```csharp
     var docVer = Document.GetDocumentVersion(doc);
     string currentVerGuid = docVer != null ? docVer.VersionGUID.ToString("D") : Guid.Empty.ToString("D");
     // ...
     targetVersionGuid = currentVerGuid,
     ```
   - When Revit creates elements and commits a transaction, Revit internal document engine mutates `Document.GetDocumentVersion(doc).VersionGUID` to a new GUID ($V_2$). `targetDoc.VersionGuid` in the intent captures pre-flight state ($V_1$). During reconcile, `targetDoc.VersionGuid != verGuid` is `true`, causing `MatchSnapshotGuids` to return `false` and reject the operation's own valid receipt.

2. **R3 — Static Identity Matching Fails Open**:
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` lines 153–159:
     ```csharp
     if (!string.IsNullOrEmpty(targetDoc.PathName) && !string.IsNullOrEmpty(receipt.TargetDocPath))
     {
         if (!string.Equals(targetDoc.PathName, receipt.TargetDocPath, StringComparison.OrdinalIgnoreCase))
         {
             return false;
         }
     }
     ```
     If `receipt.TargetDocPath` is empty/null, this check is bypassed even when `targetDoc.PathName` is specified.
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` lines 175–183:
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
     If `receipt.TargetCentralGuid` is empty/null, this check is bypassed and execution falls through to `return true;`.
   - `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs` line 69:
     ```csharp
     bool pathMismatch = hasExpectedPath && !string.IsNullOrEmpty(doc.PathName) && !string.Equals(doc.PathName, expectedPath, StringComparison.OrdinalIgnoreCase);
     ```
     If `expectedPath` is provided and `doc.PathName` is empty, `pathMismatch` evaluates to `false`.

3. **R4 — NoOp Status Omits TargetUniqueId Validation**:
   - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` lines 154–158:
     ```csharp
     if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
     {
         return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_MISSING_TARGET_UNIQUE_ID",
             ErrorReason = $"Element '{item.Mark}' with status '{rec.Status}' MUST have a non-empty TargetUniqueId." };
     }
     ```
     `rec.Status == ElementExecutionStatus.NoOp` is omitted from validation. Records with `NoOp` and empty `TargetUniqueId` pass validation unflagged.

4. **R4 — Substring Matching on Skip Reasons**:
   - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` lines 224–228:
     ```csharp
     private static bool IsValidSkipReason(string? code, string? reason)
     {
         bool Match(string? s) => !string.IsNullOrWhiteSpace(s) && ValidSkipReasonCodes.Any(c => s.Contains(c, StringComparison.OrdinalIgnoreCase));
         return Match(code) || Match(reason);
     }
     ```
     Uses `.Contains(c)` on arbitrary error reasons, falsely accepting errors that mention a skip keyword in descriptive text.

5. **R4 — Untruthful "Transaction rolled back" Without Host Confirmation**:
   - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` lines 106–109, 185–203:
     ```csharp
     if (hasIntegrityDefect || isFailedRollback)
     {
         return ApplyStrictBatchRollback(reconciled, foreignRecords, duplicateKeys);
     }
     ```
     When an integrity defect is found client-side, but `execRes.RolledBack == false`, `ApplyStrictBatchRollback` sets:
     ```csharp
     ErrorCode = batchCode ?? "ERR_BATCH_ROLLED_BACK",
     ErrorReason = batchReason ?? (firstDefect != null
         ? $"Transaction rolled back due to batch integrity defect: [{firstDefect.ErrorCode}] {firstDefect.ErrorReason}"
         : "Transaction rolled back due to batch failure or integrity defect.")
     ```
     Revit host never rolled back the transaction. Claiming rollback is false.

6. **R4 — ProvenanceHandles Dropped in CompositeSourceKeyJsonConverter**:
   - `HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs` lines 105–120:
     ```csharp
     public override void Write(Utf8JsonWriter writer, CompositeSourceKey value, JsonSerializerOptions options)
     {
         if (value is null) { writer.WriteNullValue(); return; }
         if (value == CompositeSourceKey.Empty || string.IsNullOrWhiteSpace(value.PrimaryHandle))
         {
             writer.WriteStringValue(string.Empty);
             return;
         }
         writer.WriteStringValue(value.ToString());
     }
     ```
     `value.ToString()` is `$"[{DocIdentifier}]#[{Category}]:{PrimaryHandle}"`.
     `value.ProvenanceHandles` is completely ignored and dropped upon serialization.

7. **Project Tests Execution**:
   - Running `dotnet test HPBim.slnx` on .NET 10 (MTP runner):
     - `HPBim.Core.Tests`: passed (634ms)
     - `HPBim.Client.Tests`: passed (4s 640ms)
     - `HPBim.E2E.Tests`: passed (922ms)
     - Total: 965 passed, 0 failed, 0 skipped.

---

## 2. Logic Chain

1. **R3 Logic Chain**:
   - Observation 1.1 shows that `ReconcileService.MatchSnapshotGuids` enforces equality between pre-flight `targetDoc.VersionGuid` and post-execution `receipt.TargetVersionGuid`.
   - Observation 1.1 shows that `ExtensibleStorageQueryScriptTemplate` queries `docVer.VersionGUID` dynamically on the active document at reconcile time, after element transactions have committed.
   - Creating elements in Revit mutates `VersionGUID`. Therefore, post-execution version $V_2$ never equals pre-execution snapshot $V_1$.
   - Consequently, `ReconcileService` rejects the receipt of its own committed transaction.
   - Therefore, dynamic `VersionGuid` equality must be excluded from recovery receipt matching.
   - Observation 1.2 shows that `PathName` and `CentralGuid` checks fail open when receipt fields are null or empty.
   - Therefore, static identity matching must enforce that if the intent specified a path or CentralGuid, the receipt must contain an exact match; missing receipt fields must cause a match failure.

2. **R4 Logic Chain**:
   - Observation 1.3 shows that `ValidateReturnedRecord` only guards `Created` and `Updated` against missing `TargetUniqueId`.
   - A `NoOp` signifies that a pre-existing element exists in the host document matching the plan item.
   - If `NoOp` lacks a `TargetUniqueId`, the assertion of an existing element is untruthful and unverifiable.
   - Therefore, `TargetUniqueId` must be strictly required for `NoOp`, and must match the ledger/plan TargetUniqueId.
   - Observation 1.4 shows that `IsValidSkipReason` uses `.Contains` on arbitrary strings.
   - Free-form error messages containing skip substrings bypass skip authorization.
   - Therefore, skip reasons must be verified strictly by exact matching on `rec.ErrorCode` against defined skip codes.
   - Observation 1.5 shows that `ApplyStrictBatchRollback` unconditionally stamps `ERR_BATCH_ROLLED_BACK` and "Transaction rolled back" even when `execRes.RolledBack == false`.
   - Client code cannot roll back a committed Revit transaction after receiving an execution result.
   - Stating that a transaction was rolled back when the host did not roll back is an untruthful report.
   - Therefore, when an integrity defect occurs without host rollback confirmation, the pipeline must report `OutcomeUnknown` with `ERR_POST_DISPATCH_INTEGRITY_DEFECT`, enabling Extensible Storage auto-reconciliation.
   - Observation 1.6 shows that `CompositeSourceKeyJsonConverter.Write` outputs only `value.ToString()`, omitting `value.ProvenanceHandles`.
   - Consequently, any serialized key loses its provenance handles, breaking roundtrip equality.
   - Therefore, `Write` must serialize a JSON object with `provenanceHandles` whenever `value.ProvenanceHandles.Count > 0`.

---

## 3. Caveats

1. **Pre-flight vs Recovery Scope**:
   Removing `VersionGuid` comparison applies strictly to **Recovery / Reconcile** (`ReconcileService.cs`). In **Pre-flight** (`BimPlanRunnerScriptTemplate.cs`), checking `VersionGuid` is intentional to prevent execution against a document modified concurrently between plan compilation and dispatch.
2. **File Length Limit (Rule C2)**:
   `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` is currently **275 lines**. Adding NoOp validation and defect handling directly to this file will breach the 300-line limit. The implementer must split validation logic into a new partial file: `BimPipelineEngine.ElementRecords.Validation.cs`.
3. **No Code Written**:
   As an explorer, this investigation is read-only. No source files were modified.

---

## 4. Conclusion

The root causes for R3 and R4 defects are localized, isolated, and have well-defined, minimal remediation steps:
- **R3**: Update `ReconcileService.cs` (`MatchTargetDocSnapshot`) to match static fields (`Title`, `PathName`, `CentralGuid`) strictly without failing open, and eliminate dynamic `VersionGuid` matching from recovery. Add `MatchesPersistentIdentity` to `DocumentIdentitySnapshot.cs`. Fix pre-flight path check in `BimPlanRunnerScriptTemplate.cs`.
- **R4**:
  1. Enforce non-empty `TargetUniqueId` for `NoOp` and match against expected ledger ID in `ValidateReturnedRecord`.
  2. Replace `.Contains` with exact equality matching on `rec.ErrorCode` in `IsValidSkipReason`.
  3. Bifurcate integrity defect handling: if `execRes.RolledBack == false`, report `OutcomeUnknown` with `ERR_POST_DISPATCH_INTEGRITY_DEFECT` instead of claiming rollback.
  4. Update `CompositeSourceKeyJsonConverter.Write` to serialize as JSON object with `provenanceHandles` when `ProvenanceHandles.Count > 0`.

---

## 5. Verification Method

1. **Verify Baseline Test Pass**:
   ```bash
   dotnet test HPBim/HPBim.slnx
   ```
   Must pass all 965 tests.

2. **Verify Proposed Code Files**:
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` (lines 138–186)
   - `HPBim/src/HPBim.Core/Execution/DocumentIdentitySnapshot.cs` (lines 31–57)
   - `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs` (lines 53–83)
   - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` (lines 106–228)
   - `HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs` (lines 105–120)

3. **Invalidation Conditions**:
   - If `dotnet test HPBim/HPBim.slnx` fails any tests after remediation.
   - If a test serializes a `CompositeSourceKey` with `ProvenanceHandles = ["h1", "h2"]` and deserializing it results in empty `ProvenanceHandles`.
   - If an operation creates elements, advances `VersionGuid`, and `ReconcileService` rejects the receipt with `StillUnknown` due to version GUID mismatch.
   - If a record with `status = "NoOp"` and empty `targetUniqueId` passes `ValidateReturnedRecord` with `Status == NoOp`.
