# Handoff Report: R2 (P1 — Recovery & Cross-Session Idempotency) Survey

**Agent**: `explorer_survey_pipeline_2` (teamwork_preview_explorer)  
**Parent Orchestrator ID**: `44938268-52fa-4afa-af1c-06dc2c4f2512`  
**Timestamp**: 2026-10-07T21:08:00Z  
**Type**: Hard Handoff (Investigation & Survey Complete)

---

## 1. Observation

1. **Document Identity & Freshness**:
   - `HPRebar/HPRebar.McpBridge/Service/RevitContextReader.cs:28-32` reads only `doc.Title`, `doc.PathName`, `doc.IsFamilyDocument`, `doc.IsReadOnly`, and `doc.IsModifiable`. It does not query or expose `DocumentVersion.VersionGUID` or `NumberOfSaves`.
   - `HPRebar/HPRebar.McpBridge/Service/ScriptRunner.cs:34-45` verifies only that `doc is not null` and checks read-only/modifiable flags. It contains no pre-flight verification to assert that the active document matches the target document intended by the client plan.
   - In Revit API official documentation, `Document.GetDocumentVersion(Document doc)` is a public static method returning `DocumentVersion` with `VersionGUID` (Guid) and `NumberOfSaves` (int), and `doc.GetWorksharingCentralGUID()` returns the Central Model GUID.

2. **Element Identification & Duplication**:
   - `HPBim/src/HPBim.Core/Coordination/CoordinationElementRecord.cs:11-18` defines `Id`, `Category`, `Code`, `CadSourceSpec`, `RevitTargetSpec`.
   - `HPBim/src/HPBim.Core/Coordination/CadSourceSpec.cs:11-13` defines a single string `Handle`, lacking support for composite multi-entity provenance.
   - `HPBim/src/HPBim.Cli/Commands/GenerateGradeBeamsCommand.cs:201-235` creates beams using `doc.Create.NewFamilyInstance(...)` without checking whether a beam with that identifier already exists. Rerunning the command creates duplicate overlapping elements.
   - In `state/bim_coordination_ledger.json`, `elements` array is currently empty (`[]`), and no `SourceKey -> UniqueId` mapping table exists.

3. **Intent Logging & Extensible Storage**:
   - `HPBim/src/HPBim.Client/Connection/RevitPipeClient.cs:95-102` sends requests immediately to the named pipe without logging intent to a local persistent journal.
   - `HPRebar/HPRebar/KataRebar/Service/KataRebarStorage.cs:12-52` implements `Autodesk.Revit.DB.ExtensibleStorage.Schema` with `SchemaBuilder`, GUID `6F2C8B4E-1A7D-4C3B-9E5F-2D8A0B6C4E71`, and fields `HostUniqueId`, `KataNumber`, `Beam`.
   - `HPRebar/HPRebar/KataRebar/Service/KataRebarCleanupService.cs:20-41` successfully queries this Extensible Storage entity to selectively identify and clean previous elements.
   - Neither `HPBim` nor `HPRebar.McpBridge` currently creates or manages an Extensible Storage entity on a document-level `Autodesk.Revit.DB.ExtensibleStorage.DataStorage` element for batch coordination operations.

4. **Timeouts & Disconnects**:
   - `HPBim/src/HPBim.Client/Connection/RevitPipeClient.cs:98, 133-141` uses a single timeout CTS for request dispatch and response await. If it expires, it throws `RevitPipeException(..., isTimeout: true)` and drops the connection, regardless of whether Revit has started or finished execution.
   - `HPRebar/HPRebar.McpBridge/Service/ScriptRunner.cs:50` clamps script execution timeout to `Math.Clamp(request.TimeoutSeconds, 5, 120)` seconds.
   - When a pipe timeout occurs, the client has no means to distinguish between an unstarted request and a committed transaction, and lacks an auto-reconciliation mechanism.

5. **Geometric Verification vs Element Counts**:
   - `HPBim/src/HPBim.Client/Verification/RevitModelVerifier.cs:48-75` queries element counts via `new FilteredElementCollector(doc).OfClass(...).GetElementCount()` and compares purely aggregate integers (`expCounts` vs `actCounts`).
   - `HPBim/src/HPBim.Core/Qc/FoundationQcEngine.cs:54-67` implements Step 2 as an integer count check (`piles != 196`, `footings != 72`).
   - `HPBim/src/HPBim.Cli/Commands/RevitFoundationQueryService.cs:83-126, 159-204, 241-270` demonstrates direct extraction of Solid planar faces, bounding boxes, level elevations, and location curves from the Revit API.
   - `HPBim/src/HPBim.Cli/Commands/AuditColumnsCommand.cs:174-180` relies on hardcoded string matching (`comments == "Cổ Cột Móng"`, `ccCount == 72`).

---

## 2. Logic Chain

1. From Observation 1, because neither the pipe client nor the Revit bridge asserts document title and version freshness before running scripts, any dispatch can inadvertently execute against an unintended or concurrently modified document. Therefore, introducing a `DocumentIdentitySnapshot` (Title, Path, VersionGUID, SaveCount) and an on-thread pre-flight check inside the script execution preamble is necessary to prevent state corruption.
2. From Observation 2, because element creation in `GenerateGradeBeamsCommand` does not check prior existence and elements lack globally unique composite keys, rerunning commands causes element duplication. Therefore, establishing the Composite Source Key format `[DocIdentifier]#[Category]:[PrimaryHandle]` with `ProvenanceHandles`, and tracking `SourceKey -> Revit UniqueId` in `BimCoordinationLedger`, will guarantee idempotency (creating missing elements, updating changed elements, and marking unchanged elements as `NoOp`).
3. From Observation 3, because network pipes can fail mid-transmission and `RevitPipeClient` currently lacks a persistent pre-transmission record, operations that fail mid-flight leave the system in an indeterminate state. By pairing a local append-only `IntentJournal` with an atomic Extensible Storage receipt written to a hidden `DataStorage` element inside the same Revit transaction as the elements, the system guarantees all-or-nothing persistence across both the local client and the RVT document.
4. From Observation 4, because a single client timeout conflates network write, Revit event queue delay, and script run time, client-side timeouts can abort while Revit is actually committing. By separating timeouts into Transport (5-10s), Queue Deadline (15-30s), and Execution Budget (30-120s), and setting client wait time $T_{\text{client}} \ge T_{\text{trans}} + T_{\text{queue}} + T_{\text{exec}} + T_{\text{grace}}$, premature disconnections are prevented. In the event of an unavoidable crash, marking the intent as `OutcomeUnknown` and querying the `DataStorage` receipt upon reconnect enables automatic, deterministic reconciliation.
5. From Observation 5, because `RevitModelVerifier` and current QA/QC steps evaluate aggregate element counts (`GetElementCount()`), models with severe positional, dimensional, or elevation defects can falsely report PASS if the element count happens to match. By utilizing the direct Revit API geometric extraction techniques demonstrated in `RevitFoundationQueryService` (Solid planar face elevations, curve endpoints, symbol parameters) and comparing each mapped element against `BimExecutionPlan` within `CoordinationTolerances` (Section 0.0mm, Elevation 1.0mm, Position 1.0mm/2.0mm), geometric verification becomes completely independent of count literals.

---

## 3. Caveats

- **No Source Code Modified**: In strict adherence to the read-only exploration constraint, no existing source files in `HPBim/` or `McpShared/` were altered during this survey.
- **Client Test Build Note**: Running `dotnet test tests/HPBim.Client.Tests` identified that `FakeRevitPipeClient.cs` currently fails to build due to a missing `CancelAsync` implementation on `IRevitPipeClient` (touched by peer activities). `HPBim.Core.Tests` builds and passes 100% (268/268 tests).
- **Extensible Storage Schema Registration**: In Revit API, once a `Schema` is registered in memory, its field definition cannot be modified in the same Revit process session. Any schema design changes require updating the Schema GUID. The proposed schema (`D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90`) should use a new GUID.

---

## 4. Conclusion

The architectural investigation for R2 (P1 — Recovery & Cross-Session Idempotency) is complete. The roadmap for implementation is concrete and well-bounded:
1. Implement `DocumentIdentitySnapshot` and the on-thread Pre-Flight Gate.
2. Formulate the Composite Source Key `[DocIdentifier]#[Category]:[PrimaryHandle]`, `ProvenanceHandles`, and the `SourceKey -> UniqueId` ledger mapping to enforce idempotency.
3. Introduce a local `IntentJournal` and atomic `DataStorage` Extensible Storage receipts.
4. Implement 3-tier timeout separation and auto-reconciliation on reconnect for `OutcomeUnknown`.
5. Replace count-based verifiers with direct Revit API geometric measurement against `BimExecutionPlan` under `CoordinationTolerances`.

All details, code designs, and interaction diagrams have been written to `report.md`.

---

## 5. Verification Method

To independently verify the observations and analysis documented in this report:

1. **Verify Extensible Storage Pattern in Codebase**:
   - Inspect `HPRebar/HPRebar/KataRebar/Service/KataRebarStorage.cs` lines 12–52 to verify the schema building and read/write implementation.
   - Inspect `HPRebar/HPRebar/KataRebar/Service/KataRebarCleanupService.cs` lines 20–41 to verify how Extensible Storage is used for idempotency and cleanup.
2. **Verify Count Flaw in Existing Verifier**:
   - Inspect `HPBim/src/HPBim.Client/Verification/RevitModelVerifier.cs` lines 48–75 to verify that verification is entirely count-based (`GetElementCount()`).
3. **Verify Direct Geometry Extraction in Query Service**:
   - Inspect `HPBim/src/HPBim.Cli/Commands/RevitFoundationQueryService.cs` lines 83–126 and 159–204 to confirm the exact Solid face and polygon centroid extraction logic.
4. **Verify Core Tests Pass**:
   - Run `dotnet test tests/HPBim.Core.Tests` in `HPBim/` to confirm that all 268 domain coordination and QC tests run green.
5. **Inspect Generated Deliverables**:
   - Read `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_pipeline_2\report.md`.
