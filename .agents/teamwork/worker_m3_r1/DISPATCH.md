# Task Assignment: Milestone M3 Implementation Worker

## Agent Identity
- Name: `worker_m3_r1`
- Type: `teamwork_preview_worker`
- Roles: `implementer`, `tester`
- Working Directory: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_r1\`

## Authoritative Inputs
- Authoritative User Request: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read first)
- Master Plan: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md` (Milestone M3, Features F7 & F8)
- Architecture Reports from Explorers:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_1\report.md` (Document Identity Snapshot & Pre-Flight Gate)
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_2\report.md` (Composite Source Key & Provenance Tracking)
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_3\report.md` (Coordination Ledger & Idempotent Element Resolution)

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Clean Code Governance (STRICT)
- Rule C2: EVERY file in `src/` and `tests/` touched or created MUST be strictly <= 300 lines.
- Rule M3: EVERY method in touched or created files MUST be strictly <= 50 lines (target <= 35 lines).
- No empty catch blocks without explanatory comments.
- No commented-out code.
- Zero regressions across the full repository test suite (521 HPBim tests + McpShared + HPRebar tests).

## Exclusive Write Ownership
- `HPBim/src/HPBim.Core/Execution/DocumentIdentitySnapshot.cs`
- `HPBim/src/HPBim.Core/Execution/ElementPlanItem.cs`
- `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs`
- `HPBim/src/HPBim.Core/Coordination/LedgerElementMapping.cs`
- `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedger.cs`
- `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.Mappings.cs`
- `HPBim/src/HPBim.Core/Coordination/IdempotentElementResolver.cs`
- `HPBim/src/HPBim.Client/Pipeline/DocumentIdentityQuery.cs`
- `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs`
- `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Structure.cs`
- `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Architecture.cs`
- `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.MepAndRebar.cs`
- `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs`
- `HPBim/src/HPBim.Client/Pipeline/BimPipelineResult.cs`
- `HPBim/tests/HPBim.Core.Tests/Coordination/CompositeSourceKeyTests.cs`
- `HPBim/tests/HPBim.Core.Tests/Coordination/BimCoordinationLedgerIdempotencyTests.cs`
- `HPBim/tests/HPBim.Client.Tests/Pipeline/PreFlightGateTests.cs`
- `HPBim/tests/HPBim.Client.Tests/Pipeline/IdempotentExecutionTests.cs`
- `HPBim/tests/HPBim.Client.Tests/Mocks/FakeRevitPipeClient.cs`
- `HPBim/tests/HPBim.E2E.Tests/Fixtures/E2EFakeRevitPipeClient.cs`

## Key Implementation Packages

### Package 1: Document Identity Snapshot & Pre-Flight Gate (Feature F7)
1. Enhance `DocumentIdentitySnapshot`:
   - Properties: `Title`, `PathName`, `VersionGuid`, `NumberOfSaves`, `CentralGuid`, `ActiveViewName`.
   - Domain helpers: `IsEmpty`, `IsWorkshared`, `Matches(string actualTitle, Guid actualVersionGuid)`.
2. Update on-thread Pre-Flight Gate in `BimPlanRunnerScriptTemplate.cs`:
   - Retrieve `docVer = Autodesk.Revit.DB.Document.GetDocumentVersion(doc);`
   - Compare `expectedTitle` and `expectedVersionGuid`.
   - If mismatch, throw `InvalidOperationException($"[PRE_FLIGHT_FAILED] Target document mismatch: expected '{expectedTitle}' (VersionGUID {expectedVersionGuid}), but active document is '{doc.Title}' (VersionGUID {docVer.VersionGUID})");`
3. Add `DocumentIdentityQuery` in `HPBim.Client/Pipeline/DocumentIdentityQuery.cs`:
   - Executes read-only script with `TransactionModes.None` returning JSON snapshot.
4. Unit/Integration tests in `PreFlightGateTests.cs`:
   - Verify success on exact title and VersionGuid match.
   - Verify immediate rollback and error message on title mismatch.
   - Verify immediate rollback and error message on VersionGuid mismatch.

### Package 2: Composite Source Key & Provenance Tracking (Feature F8 Part 1)
1. Refactor `CompositeSourceKey`:
   - Canonical string: `[DocIdentifier]#[Category]:[PrimaryHandle]`.
   - Implement explicit `IEquatable<CompositeSourceKey>`, `IComparable<CompositeSourceKey>`, `Equals`, `GetHashCode`, `==`, `!=`:
     - Compare `DocIdentifier` and `PrimaryHandle` with `StringComparer.OrdinalIgnoreCase`.
     - Exclude `ProvenanceHandles` from identity comparison.
   - Parser: supports `[Doc]#[Cat]:[Handle]`, `Doc#Cat:Handle`, and bracketed handle `[Doc]#[Cat]:[Handle]`.
   - Legacy compatibility: allow empty doc in `TryParse` (`"[]#[Column]:C1"` and `"#Column:C2"`) to ensure zero regressions on M1 tests.
2. Update `ElementPlanItem`:
   - Ensure `ProvenanceHandles` is preserved (`IReadOnlyList<string>`).
   - Add `TargetUniqueId` (`string?`) property.
3. Unit tests in `CompositeSourceKeyTests.cs`:
   - 19 test cases as specified in `explorer_m3_2/report.md`.

### Package 3: Coordination Ledger & Idempotency Resolution (Feature F8 Part 2)
1. Implement `LedgerElementMapping.cs` in `HPBim.Core/Coordination/`.
2. Update `BimCoordinationLedger`:
   - Add `Mappings: IReadOnlyDictionary<string, LedgerElementMapping>`.
   - Add `TryGetTarget`, `RegisterMapping`, `UpdateMapping`, `RemoveMapping`.
3. Create `BimCoordinationLedgerService.Mappings.cs` (partial class):
   - Provide `SyncFromExecutionRecords(IEnumerable<ElementExecutionRecord> records, string planHash)`.
   - Ensure `BimCoordinationLedgerService.cs` remains <= 295 lines (Rule C2).
4. Implement `IdempotentElementResolver` in `HPBim.Core/Coordination/`.
5. Update `BimPlanRunnerScriptTemplate.*.cs`:
   - In each category handler (Columns, Beams, Walls, Slabs, Openings, Rooms, MepFixtures, PipeRuns, Rebars):
     - If `TargetUniqueId` is provided, query `doc.GetElement(targetUniqueId)`.
     - If element exists and properties match -> return `NoOp`.
     - If element exists and modified -> update parameters and return `Updated`.
     - If element does not exist -> create new element, tag comments parameter with `SourceKey.ToString()`, and return `Created`.
6. Update `BimPipelineEngine.PlanExecution.cs`:
   - Pre-populate `item.TargetUniqueId` from ledger mapping if available.
   - Call `ledgerService.SyncFromExecutionRecords` after successful Apply commit.
   - Add summary counts in `BimPipelineResult` (`CreatedCount`, `UpdatedCount`, `NoOpCount`, `FailedCount`).
7. Comprehensive tests in `BimCoordinationLedgerIdempotencyTests.cs` and `IdempotentExecutionTests.cs`:
   - Rerun exact plan -> `Created == 0, NoOp == N`.
   - Deletion recovery -> deleted element recreated (`Created == 1`).

### Package 4: Clean Code & Verification
1. Run automated line-counter script across all modified/new files.
2. Run build: `dotnet build HPBim/HPBim.slnx`.
3. Run all test suites: `dotnet test HPBim/HPBim.slnx`, `dotnet test McpShared/McpShared.slnx`.
4. Attest exact file and method line counts in `report.md` and `handoff.md`.

## Deliverables
- `report.md`: Complete implementation report with test results, method line measurements, and Clean Code attestation.
- `handoff.md`: Self-contained handoff report.


## 2026-10-07T20:23:53Z
You are worker_m3_r1, a teamwork_preview_worker agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_r1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_r1\DISPATCH.md.
