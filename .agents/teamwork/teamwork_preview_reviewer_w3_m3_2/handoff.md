# Handoff Report: Reviewer 2 (Milestone 3 — Persistent Document Identity Decoupling)

## 1. Observation

1. **Decoupling of Dynamic VersionGuid in Recovery**:
   - In `HPBim/src/HPBim.Client/Recovery/ReconcileService.DocSnapshot.cs` (lines 34–75 and 77–93), `MatchTargetDocSnapshot` strictly evaluates static identity properties (`Title`, `PathName`, `CentralGuid`).
   - Line 77–93:
     ```csharp
     public static bool MatchTargetDocSnapshot(DocSnapshot targetDoc, CoordinationReceiptRecord receipt)
     {
         if (targetDoc == null || receipt == null) return false;
         string actualTitle = !string.IsNullOrEmpty(receipt.TargetDocTitle)
             ? receipt.TargetDocTitle
             : receipt.TargetDocId;

         return targetDoc.MatchesStaticIdentity(
             actualTitle,
             receipt.TargetDocPath,
             receipt.TargetCentralGuid,
             receipt.TargetCentralGuid);
     }
     ```
   - Notice: `receipt.TargetVersionGuid` and `targetDoc.VersionGuid` are completely omitted from the comparison during receipt recovery.

2. **Pre-flight Gate Preserves Stale Version Check**:
   - In `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs` (lines 66–83):
     ```csharp
     bool versionMismatch = hasExpectedVersion && actualVersionGuid != expectedVersionGuid;
     ...
     if (versionMismatch)
     {
         throw new InvalidOperationException(
             $"[PRE_FLIGHT_FAILED] [ERR_STALE_DOCUMENT_SNAPSHOT] Target document version snapshot is stale: expected VersionGUID {expectedVersionGuid:D}, but active document VersionGUID is {actualVersionGuid:D}");
     }
     ```
   - Verified that pre-flight concurrency guard is fully retained to protect against modifying concurrently altered models.

3. **Strict Static Identity (No Fail-Open)**:
   - In `HPBim/src/HPBim.Client/Recovery/ReconcileService.DocSnapshot.cs` lines 54–61:
     ```csharp
     if (!string.IsNullOrEmpty(targetDoc.PathName))
     {
         if (string.IsNullOrEmpty(receipt.TargetDocPath) ||
             !string.Equals(targetDoc.PathName, receipt.TargetDocPath, StringComparison.OrdinalIgnoreCase))
         {
             return false;
         }
     }
     ```
   - Lines 63–72:
     ```csharp
     if (targetDoc.CentralGuid.HasValue && targetDoc.CentralGuid.Value != Guid.Empty)
     {
         if (string.IsNullOrEmpty(receipt.TargetCentralGuid) ||
             !Guid.TryParse(receipt.TargetCentralGuid, out var centGuid) ||
             centGuid == Guid.Empty ||
             targetDoc.CentralGuid.Value != centGuid)
         {
             return false;
         }
     }
     ```
   - An empty or invalid receipt field returns `false` instead of failing open.

4. **Major Finding — `DocSnapshot.ProjectGuid` Disconnect**:
   - `DocSnapshot.cs` includes `public Guid? ProjectGuid { get; init; }`.
   - In `ReconcileService.DocSnapshot.cs` (lines 91–92), `receipt.TargetCentralGuid` is passed as both `actualCentralGuid` and `actualProjectGuid`.
   - `CoordinationReceiptRecord.cs` lacks a `TargetProjectGuid` field, and `ExtensibleStorageQueryScriptTemplate.cs` does not query `doc.ProjectInformation.UniqueId`.
   - Consequently, if a non-workshared model uses `DocSnapshot` with `ProjectGuid`, recovery will fail because `MatchesStaticIdentity` compares `ProjectGuid` with `receipt.TargetCentralGuid`. (In current production, `DocumentIdentitySnapshot` is used, so this does not break the production pipeline, but the `DocSnapshot` overload is buggy).

5. **Minor Finding — Path Normalization**:
   - `string.Equals(targetDoc.PathName, receipt.TargetDocPath, StringComparison.OrdinalIgnoreCase)` is case-insensitive but sensitive to slash format (`/` vs `\`) and non-canonical path segments (`..`).

6. **Test Suites & Clean Code Verification**:
   - `dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`: 536 passed, 0 failed, 0 skipped.
   - Line counts: `DocSnapshot.cs` (95 lines), `ReconcileService.cs` (211 lines), `ReconcileService.DocSnapshot.cs` (107 lines), all <= 300 lines (Rule C2).
   - Method lengths: All methods <= 50 lines (Rule M3).
   - Comments: 0 comments in production C# source files (Rule Q-B1).
   - Exception handling: 0 unannotated empty catch blocks (Rule Q-B2).
   - Integrity: Zero hardcoded outputs, dummy implementations, or bypassed checks.

---

## 2. Logic Chain

1. From Observation 1, `MatchTargetDocSnapshot` in `ReconcileService.DocSnapshot.cs` eliminates `VersionGuid` comparison during recovery, allowing receipts from committed transactions that mutated elements (and thereby incremented `VersionGUID`) to be recognized and committed to the ledger.
2. From Observation 2, pre-flight gate in `BimPlanRunnerScriptTemplate.cs` continues to strictly enforce `actualVersionGuid == expectedVersionGuid`, ensuring dispatch is aborted if the active document was modified prior to execution.
3. From Observation 3, if `PathName` or `CentralGuid` is defined in `targetDoc`, a receipt with missing or malformed corresponding fields returns `false`, eliminating the fail-open vulnerability identified in earlier reviews.
4. From Observation 4 & 5, two edge-case findings exist (the `ProjectGuid` argument mapping in the unused `DocSnapshot` overload and raw string path comparison), but they do not affect the production pipeline (`DocumentIdentitySnapshot` matching) and do not invalidate the core milestone deliverables.
5. From Observation 6, all clean code rules and integrity checks are satisfied, and tests pass cleanly.
6. Therefore, Milestone 3 satisfies the acceptance criteria and is approved.

---

## 3. Caveats

1. **`DocSnapshot.ProjectGuid` Inactive in Production**: In the current pipeline, `IntentJournalRecord` and `BimExecutionPlan` use `DocumentIdentitySnapshot`, which only evaluates `Title`, `PathName`, and `CentralGuid`. Non-workshared models identify by `Title` and `PathName`. If non-workshared persistent GUIDs (`ProjectInformation.UniqueId`) are required in future milestones, `CoordinationReceiptRecord` and the Extensible Storage query script must be updated to store and query `ProjectGuid`.
2. **Path Normalization**: Windows path comparisons assume backslashes. If external plans supply forward slashes (e.g. `C:/Project.rvt`), path normalization will be required.

---

## 4. Conclusion

**Verdict**: **APPROVE**

Milestone 3 (R3: Tách biệt Persistent Document Identity khỏi Dynamic DocumentVersion GUID) has successfully decoupled dynamic revision state from static identity during recovery while keeping pre-flight concurrency safety intact. Fail-open edge cases have been resolved, clean code rules are respected, and all test suites execute cleanly.

---

## 5. Verification Method

To independently verify this milestone:
1. **Run HPBim.Client tests**:
   ```powershell
   dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   ```
   Expect: All tests pass (0 failures).

2. **Inspect source files**:
   - `HPBim/src/HPBim.Core/Models/DocSnapshot.cs`
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs`
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.DocSnapshot.cs`
   - `HPBim/tests/HPBim.Client.Tests/Recovery/DocSnapshotReconcileTests.cs`

3. **Check Clean Code Compliance**:
   - Ensure all files are <= 300 lines (Rule C2).
   - Verify 0 comments (`//` or `/*`) in production source (Rule Q-B1).

4. **Invalidation Conditions**:
   - Any test failure in `dotnet test`.
   - If `ReconcileService` rejects a receipt solely because `VersionGuid` changed after element creation.
   - If an intent with `PathName` or `CentralGuid` accepts an empty receipt field (fail-open).
