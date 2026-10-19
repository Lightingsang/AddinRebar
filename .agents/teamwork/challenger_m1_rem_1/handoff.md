# Handoff Report — Milestone M1 Verification (challenger_m1_rem_1)

**Verdict**: **REQUEST_CHANGES**

---

## 1. Observation

1. **Defect 1 (P0 Functional Hole — Omitted `TargetUniqueId` Bypasses Validation)**:
   - File: `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs`, lines 167–174:
     ```csharp
     if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated && rec.TargetUniqueId != null && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
     {
         return rec with
         {
             Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_MISSING_TARGET_UNIQUE_ID",
             ErrorReason = $"Element '{item.Mark}' with status '{rec.Status}' MUST have a non-empty TargetUniqueId."
         };
     }
     ```
   - Parsing logic in `ParseSingleElementRecord` (lines 285–286):
     ```csharp
     string? targetUid = elem.TryGetProperty("targetUniqueId", out var p)
         ? (p.ValueKind == JsonValueKind.String ? p.GetString() : string.Empty) : null;
     ```
   - When a returned record has status `Created` or `Updated` but the `targetUniqueId` JSON property is omitted entirely:
     - `elem.TryGetProperty("targetUniqueId", out var p)` returns `false`.
     - `targetUid` is assigned `null`.
     - At line 167, the condition `rec.TargetUniqueId != null` evaluates to `false`.
     - The check is completely bypassed: the record is accepted with status `Created` and `TargetUniqueId = null`.
     - The batch succeeds (`Success = true`), ledger synchronization skips the element because `string.IsNullOrEmpty(rec.TargetUniqueId)` is true (`BimCoordinationLedgerService.Mappings.cs:93`), and idempotency is broken.
   - Empirical proof:
     - In `scratch/AdversarialM1Wave2Challenger/Program.cs`, check 3.4 failed:
       `[FAIL] 3.4 Created with targetUniqueId property completely omitted from JSON -> Success=True, Status=Created, Code=`

2. **Defect 2 (Clean Code Rule M3 Violation — Method Length Exceeds 50 Lines)**:
   - File: `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs`, lines 140–211:
     - Method `ReconcileSingleItem` spans 72 lines (lines 140 to 211 inclusive).
     - Repo rule `HP_CLEAN_CODE_CORE.md` (Rule M3) mandates: "Phương thức <= 50 dòng".
     - 72 lines violates Rule M3.

3. **Baseline Test Suite Status**:
   - `dotnet run --project HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`: 388 passed, 0 failed, 0 skipped.
   - `dotnet run --project HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj`: 429 passed, 0 failed, 0 skipped.
   - `dotnet run --project HPBim/tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj`: 42 passed, 0 failed, 0 skipped.
   - Total existing tests: 859 passed.

4. **Empirical Challenger Test Suite Results** (`scratch/AdversarialM1Wave2Challenger`):
   - Total adversarial checks: 39.
   - Passed: 38.
   - Failed: 1 (Check 3.4: Created record with omitted `targetUniqueId`).

---

## 2. Logic Chain

1. **R1 Specification Requirement**:
   - `ORIGINAL_REQUEST.md` (Milestone M1 Requirement R1):
     *"Từ chối ngay lập tức (OutcomeUnknown / Failed) nếu phát hiện: Record có status Created hoặc Updated nhưng thiếu TargetUniqueId."*
   - Acceptance Criteria:
     *"Strict 1:1 Bijection Test: Test chứng minh khi Revit trả về duplicate record, foreign record, status enum không hợp lệ, hoặc Created thiếu UniqueId, pipeline lập tức rollback và trả về Success = false."*

2. **Causal Chain for Defect 1**:
   - A created/updated BIM element in Revit must always possess a valid UniqueId.
   - If a runner or bridge emits a record with status `Created` without the `targetUniqueId` property, that record literally "lacks TargetUniqueId" (thiếu TargetUniqueId).
   - In `ParseSingleElementRecord`, missing property results in `TargetUniqueId = null`.
   - In `ReconcileSingleItem:167`, the author wrote `rec.TargetUniqueId != null && string.IsNullOrWhiteSpace(rec.TargetUniqueId)`.
   - The clause `rec.TargetUniqueId != null` explicitly filters out `null`, meaning the check ONLY executes when `TargetUniqueId` is non-null but whitespace/empty.
   - Consequently, any record completely lacking `targetUniqueId` bypasses validation and is treated as a successful creation.
   - When `SyncFromExecutionRecords` executes, it requires `!string.IsNullOrEmpty(rec.TargetUniqueId)`. Since `TargetUniqueId` is null, the mapping is silently dropped from the ledger.
   - Fix: Replace `rec.TargetUniqueId != null && string.IsNullOrWhiteSpace(rec.TargetUniqueId)` with `string.IsNullOrWhiteSpace(rec.TargetUniqueId)`. `string.IsNullOrWhiteSpace(null)` is `true`, correctly catching `null`, `""`, and whitespace strings while preserving `NoOp` and `Skipped` immunity.

3. **Causal Chain for Defect 2**:
   - `ReconcileSingleItem` currently handles: (1) dequeuing, (2) category validation, (3) mark validation, (4) unique id validation, (5) skip allowance validation, (6) rollback propagation, and (7) deficit outcome-unknown generation.
   - This single method totals 72 lines.
   - Fix: Extract element validation (steps 2–6) into a private helper `ValidateReturnedRecord(ElementPlanItem item, ElementExecutionRecord rec, bool isFailedRollback)`, bringing both methods well under 40 lines.

---

## 3. Caveats

- All other Requirement R1 corner cases were proven robust:
  - Legitimate duplicate source keys in plan across levels are preserved and matched 1:1.
  - Excess duplicate returned records trigger `ERR_DUPLICATE_RECORD` and batch rollback.
  - Foreign returned records trigger `ERR_FOREIGN_RECORD` and batch rollback.
  - Enum case-insensitivity functions correctly for valid names (`created`, `NoOp`, `SKIPPED`).
  - Invalid enum strings (e.g., `BogusStatus`, `Pending`, `0`) trigger `ERR_INVALID_STATUS_ENUM`.
  - Category and Mark mismatches are reliably intercepted.
  - Approved skip codes (`SKIP_FROZEN_ZONE`, `SKIP_STAGE_DISABLED`, etc.) and `SkipAllowed=true` succeed; unapproved skips trigger `ERR_INTEGRITY_DEFECT_SKIPPED` and rollback.
  - 1000-element batch stress test completed in 28ms with all-or-nothing rollback verified.

---

## 4. Conclusion

- **Verdict**: **REQUEST_CHANGES**.
- **Action items for worker**:
  1. Fix `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs:167`:
     Change:
     ```csharp
     if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated && rec.TargetUniqueId != null && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
     ```
     To:
     ```csharp
     if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
     ```
  2. Add unit test in `HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.cs` verifying that a `Created` (and `Updated`) record where `targetUniqueId` property is completely omitted from JSON triggers `ERR_MISSING_TARGET_UNIQUE_ID` and batch rollback.
  3. Refactor `ReconcileSingleItem` (lines 140–211) to comply with Clean Code Rule M3 ($\le 50$ lines).

---

## 5. Verification Method

1. Run the empirical adversarial test harness:
   ```bash
   dotnet run --project scratch/AdversarialM1Wave2Challenger/AdversarialM1Wave2Challenger.csproj
   ```
   *Expected after fix*: 39 passed, 0 failed, exit code 0.

2. Run full solution test suites:
   ```bash
   dotnet run --project HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   dotnet run --project HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
   dotnet run --project HPBim/tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj
   ```

3. Verify line counts and method length (PowerShell):
   ```powershell
   (Get-Content 'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs').Count # must be <= 300
   ```
