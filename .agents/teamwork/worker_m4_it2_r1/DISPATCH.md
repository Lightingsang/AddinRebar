# Dispatch Instructions — worker_m4_it2_r1

## Identity & Context
- **Agent**: `worker_m4_it2_r1`
- **Archetype**: `teamwork_preview_worker`
- **Role**: `Milestone M4 Remediation Worker`
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\`
- **Authoritative User Request**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read this first).
- **Project Scope Document**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- **Gate Status**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_1\GATE_STATUS.md`.
- **Auditor Full Evidence Report**:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\report.md`
- **Explorer Investigation Reports (MANDATORY TO READ)**:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_it2_1\report.md` (Query Template M3 Remediation Design)
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_it2_2\report.md` (Receipt Reader Defensive Guard & Test Update)
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_it2_3\report.md` (Authentic Line Measurement & 24-File Census)

---

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

---

## Exclusive Write Ownership
You exclusively own and may edit the following files:
1. `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs`
2. `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs`
3. `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs`
4. `HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReceiptReaderTests.cs`
5. `HPBim/tests/HPBim.Client.Tests/Recovery/ExtensibleStorageReceiptReaderTests.cs`
6. `scratch/verify_m4_clean_code.py`

---

## Actionable Remediation Tasks

### Task 1: Refactor `ExtensibleStorageQueryScriptTemplate.cs` (Rule M3 Remediation)
- Decompose `GetQueryScript()` into:
  - `GetQueryHeaderScript()` (lines setting up schema GUID lookup, null guard, args, collector; strictly $\le 30$ lines, e.g. 17 lines).
  - `GetQueryFilterAndReturnScript()` (lines iterating collector, filtering by targetOpId and targetPlanHash, returning JSON payload; strictly $\le 30$ lines, e.g. 28 lines).
  - `GetQueryScript()` joining both helpers (3 lines).
- Ensure total file length $\le 60$ lines (strictly $\le 100$ lines, Rule C2 $\le 300$).
- Ensure max method length is strictly $\le 30$ lines (Rule M3 $\le 50$).

### Task 2: Defensive JSON Guard in `ExtensibleStorageReceiptReader.cs`
- In `ParseReceipts(JsonElement? root)`:
  - Inside the array enumeration loop, add `if (item.ValueKind != JsonValueKind.Object) continue;`.
  - Extract helper method `ParseSingleReceipt(JsonElement item)` (15 lines).
  - Keep `ParseReceipts` $\le 26$ lines.
  - Keep max method in file $\le 30$ lines (`QueryReceiptsAsync`), and file length $\le 170$ lines (Rule C2 $\le 300$).

### Task 3: Update `AdversarialReceiptReaderTests.cs`
- Update test `ParseReceipts_NonObjectArrayElements_ThrowsInvalidOperationException_ProvingMissingGuard` to assert graceful filtering (`Assert.Empty(receipts)`).
- Add `ParseReceipts_MixedObjectAndNonObjectElements_ExtractsOnlyValidObjects` proving non-object items are skipped while valid objects are extracted.

### Task 4: Decompose `GetPreambleScript()` in `BimPlanRunnerScriptTemplate.cs` (Complete Clean Code Clearance)
- Split `GetPreambleScript()` (133 lines) into 3 private helper methods:
  - `GetPreambleHelpersAndPreFlightScript()` (coordinate conversion helpers, doc identity pre-flight check; $\le 35$ lines).
  - `GetPreambleDatumCollectorsScript()` (level and grid collector dictionaries; $\le 30$ lines).
  - `GetPreambleTypeCollectorsScript()` (structural framing, column, foundation type collectors; $\le 35$ lines).
  - `GetPreambleScript()` joining the three helpers ($\le 5$ lines).
- Ensure `BimPlanRunnerScriptTemplate.cs` has NO methods $> 50$ lines, and total file length remains $\le 200$ lines (Rule C2 $\le 300$).

### Task 5: Verify with Authentic Measurement Tools & Build/Test
1. Run `python scratch/verify_m4_clean_code.py` -> MUST report ALL 24 FILES PASS (Exit code 0).
2. Run `dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj` -> MUST report OVERALL AUDIT VERDICT: CLEAN / 100% PASS (Exit code 0).
3. Run `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
4. Run `dotnet test HPBim/HPBim.slnx` -> 100% tests pass (Client + Core + E2E).
5. Run `dotnet test McpShared/McpShared.slnx` -> 100% tests pass.

---

## Deliverables & Handoff
- Formulate an authentic, truthful attestation table in `report.md` reporting exact physical disk line counts and method line counts.
- Deliver `report.md` and `handoff.md` in your working directory.
- Send a completion message back to parent.
