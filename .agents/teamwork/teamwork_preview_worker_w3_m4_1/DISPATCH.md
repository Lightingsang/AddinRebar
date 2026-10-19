## 2026-10-09T17:10:29Z
You are Worker W3 M4 for HPBim Wave 3 Remediation Milestone 4: Truthfulness of NoOp, Skip, and Rollback Semantics & ProvenanceHandles Preservation (R4).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_w3_m4_1
Your parent conversation ID is: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-09T14:54:56Z).
2. Read Explorer 2 findings:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_w3_survey_2\survey_report.md (specifically Section 2: Deep Dive: Requirement R4)
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_w3_survey_2\handoff.md
3. Read PROJECT.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

File Write Ownership:
You EXCLUSIVELY own:
- HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs
- HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs
- HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.Validation.cs (NEW partial class file to keep both files <= 300 lines per Rule C2!)
- HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs (if updating post-dispatch OutcomeUnknown resolution)
- HPBim/tests/HPBim.Core.Tests/Coordination/CompositeSourceKeyJsonConverterTests.cs
- HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.cs (and/or its partial files)
Do NOT modify files outside this scope!

Task Requirements (R4):
1. CompositeSourceKeyJsonConverter ProvenanceHandles 100% Preservation:
   - In HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs, fix Write:
     When value.ProvenanceHandles has items (Count > 0), serialize as a JSON object:
     {
       "docIdentifier": value.DocIdentifier,
       "category": value.Category.ToString(),
       "primaryHandle": value.PrimaryHandle,
       "provenanceHandles": [ ... ]
     }
     When value.ProvenanceHandles is empty/null, write the canonical string (value.ToString()) for backward compatibility.
     Ensure Read and ReadLegacyJsonObject seamlessly deserialize both object format and string format, fully preserving ProvenanceHandles.
     Also implement WriteAsPropertyName and ReadAsPropertyName if applicable.
   - Add unit test verifying roundtrip serialization/deserialization with ProvenanceHandles preserves 100% of handles with strict equality.

2. Strict NoOp TargetUniqueId Truthfulness & Ledger Matching:
   - In ValidateReturnedRecord, include ElementExecutionStatus.NoOp in the non-empty TargetUniqueId check (alongside Created and Updated). A NoOp record with missing/whitespace TargetUniqueId MUST be rejected with ERR_MISSING_TARGET_UNIQUE_ID.
   - When item.TargetUniqueId is specified (from ledger / plan), if returned record has NoOp, verify that rec.TargetUniqueId matches item.TargetUniqueId. If not, reject with ERR_NOOP_TARGET_MISMATCH.

3. Standardized Exact Skip Policy (Eliminate .Contains substring matching):
   - In IsValidSkipReason, eliminate string.Contains substring matching.
   - Require exact code equality on rec.ErrorCode against ValidSkipReasonCodes (or a standardized SkipReasonCodes policy).
   - Ensure an unhandled error or descriptive reason containing a skip code substring cannot masquerade as an approved skip.

4. Post-Dispatch Integrity Defect Reporting: OutcomeUnknown (Truthful Rollback):
   - When a post-dispatch integrity defect is detected client-side (e.g. foreign record, duplicate record, invalid enum, missing TargetUniqueId, invalid skip):
     Check whether the host actually confirmed rollback (execRes.RolledBack == true).
     - If execRes.RolledBack == true: it is truthful to report ERR_BATCH_ROLLED_BACK ("Transaction rolled back by Revit bridge").
     - If execRes.RolledBack == false: the Revit transaction was NOT rolled back. Do NOT claim "Transaction rolled back due to batch integrity defect". Instead:
       Report ElementExecutionStatus.OutcomeUnknown with ErrorCode = "ERR_POST_DISPATCH_INTEGRITY_DEFECT" for other items in the batch, and in ResolvePlanExecutionOutcome set isOutcomeUnknown = true and success = false.
       (The defective item preserves its specific defect code).
     - Update existing tests in BimPipelineEngineRecordReconciliationTests where RolledBack == false to assert OutcomeUnknown / ERR_POST_DISPATCH_INTEGRITY_DEFECT instead of ERR_BATCH_ROLLED_BACK.

5. Clean Code Governance (MANDATORY):
   - File length <= 300 lines (Rule C2). Extract validation methods to BimPipelineEngine.ElementRecords.Validation.cs to ensure BimPipelineEngine.ElementRecords.cs remains <= 300 lines.
   - Method length <= 50 lines (Rule M3).
   - ZERO code comments (Rule Q-B1).
   - ZERO unannotated empty catches (Rule Q-B2).
   - Pure netstandard2.0 in HPBim.Core and HPBim.Client (zero Autodesk references).

6. Verification:
   - Run tests: dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
   - Run tests: dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   - Run full HPBim test suite: dotnet test HPBim/HPBim.slnx
   - Ensure 100% tests PASS across the solution with 0 regressions.

Deliverables:
- Write report.md and handoff.md in your working directory.
- Send completion message to parent (8b5286ec-5528-473c-b9fe-c3867bdf1bf8).
