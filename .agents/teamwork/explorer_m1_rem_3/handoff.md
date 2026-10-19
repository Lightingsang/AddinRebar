# Handoff Report: Strict Batch Transaction Rollback & Script Runner Heuristic Fallbacks (R1.3 & R1.4)

## 1. Observation
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.cs`:
  - Lines 151–152:
    ```csharp
    var pipingSysType = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipingSystemType)).Cast<Autodesk.Revit.DB.Plumbing.PipingSystemType>().FirstOrDefault();
    var pipeType = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipeType)).Cast<Autodesk.Revit.DB.Plumbing.PipeType>().FirstOrDefault();
    ```
  - Lines 167–189: Each item runs in an isolated `SubTransaction(doc)`. When an exception occurs or a symbol is missing, `subTx.RollBack()` is called and status `"Failed"` is recorded, but execution continues across the batch without throwing an exception out of the script.
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.Receipt.cs`:
  - Lines 20–21:
    ```csharp
    if (!dryRun && !string.IsNullOrEmpty(planHash))
    ```
    Receipt is written to Extensible Storage regardless of whether any items in `records` failed (`records.Any(r => r["status"] == "Failed")`).
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar.McpBridge\Service\ScriptRunner.cs`:
  - Lines 81–112: Under `TransactionModes.Auto`, `transaction.Commit()` and `group.Assimilate()` are executed if the script completes without throwing an exception.
  - Lines 127–132: `RollBack(transaction, group)` is triggered only inside `catch (Exception exception)`.
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.Structure.cs`:
  - Lines 162–170:
    ```csharp
    var loc = geom.Obj("location");
    var pt = ToXyz(loc.Double("x"), loc.Double("y"), 0);
    var colInst = doc.Create.NewFamilyInstance(pt, sym, baseLvl, Autodesk.Revit.DB.Structure.StructuralType.Column);
    ```
    Neither `rotationDeg` nor `BuiltInParameter.FAMILY_TOP_LEVEL_PARAM` is handled.
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\ScriptGenerators\Stage7MepGenerator.cs`:
  - Lines 82–90: Uses `.FirstOrDefault()` for both `PipingSystemType` and `PipeType`.
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages7To8.cs`:
  - Lines 30–38: Uses `.FirstOrDefault()` for both `PipingSystemType` and `PipeType`.
- `dotnet test tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj`: 417 passed (518ms).
- `dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`: 322 passed (3.9s).
- `dotnet test tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj`: 42 passed (491ms).

## 2. Logic Chain
1. Observations in `ScriptRunner.cs` demonstrate that Revit transactions under `TransactionModes.Auto` only roll back if the executed script throws an unhandled exception. Normal script returns cause `transaction.Commit()` to be called.
2. In `BimPlanRunnerScriptTemplate.cs`, catching errors per-item via `SubTransaction` and returning a summary without throwing allows partial successful elements to commit in Revit even when sibling elements failed, directly violating the All-or-Nothing batch guarantee.
3. In `BimPlanRunnerScriptTemplate.Receipt.cs`, writing receipts without checking `records.Any(r => r["status"] == "Failed")` leads to invalid synchronization records in Extensible Storage for corrupted or partial models.
4. Therefore, when `records.Any(r => r["status"] == "Failed")` is true, the script must both bypass receipt creation and throw an `InvalidOperationException($"[BATCH_TRANSACTION_FAILED] ... ||PAYLOAD:{recordsJson}")`.
5. This causes `ScriptRunner.cs` to roll back the entire transaction, sets `RolledBack = true`, and returns `IsError = true`.
6. Observations in `BimPlanRunnerScriptTemplate.Structure.cs` confirm that columns are created with 0° rotation and default unconstrained top level because `rotationDeg` is never applied and `FAMILY_TOP_LEVEL_PARAM` is never set. Applying `ElementTransformUtils.RotateElement` and setting `FAMILY_TOP_LEVEL_PARAM` eliminates this heuristic omission.
7. Observations in `BimPlanRunnerScriptTemplate.cs`, `Stage7MepGenerator.cs`, and `CompositeBimGenerator.Stages7To8.cs` prove that `.FirstOrDefault()` binds whatever arbitrary pipe and system type is indexed first in Revit. Collecting types into dictionaries by name and explicitly requiring matching entries with `ERR_SYMBOL_NOT_FOUND` eliminates all heuristic guessing.

## 3. Caveats
- No changes were made directly to production files during this exploration phase (strictly read-only protocol).
- Worker must ensure `BimPlanRunnerScriptTemplate.Structure.cs` (currently 268 lines) does not exceed 300 lines by extracting column handling into `BimPlanRunnerScriptTemplate.Columns.cs` (Rule C2).
- Worker must ensure `BimPipelineEngine.PlanExecution.cs` (currently 271 lines) stays under 300 lines when updating `ParseElementRecords`.

## 4. Conclusion
The root causes of review blockers R1.3 and R1.4 have been fully diagnosed with exact line-level precision.
A step-by-step implementation strategy and drop-in code snippets have been provided in `report.md`.
Worker can execute these remediation steps immediately with zero ambiguity and verify against acceptance tests.

## 5. Verification Method
- Independent automated tests:
  ```bash
  dotnet test tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
  dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
  dotnet test tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj
  ```
- File inspection:
  - Inspect `BimPlanRunnerScriptTemplate.Receipt.cs` to verify receipt creation is skipped and `[BATCH_TRANSACTION_FAILED]` is thrown when any item failed.
  - Inspect `BimPlanRunnerScriptTemplate.Columns.cs` to verify rotation and `FAMILY_TOP_LEVEL_PARAM` assignment.
  - Search for `FirstOrDefault` across `HPBim/src` to verify 0 occurrences remain for `PipeType` or `PipingSystemType`.
- Invalidation conditions:
  - If a batch containing a failed element produces a receipt in Extensible Storage or commits partial elements (`RolledBack == false`).
  - If any C# file exceeds 300 lines (Rule C2).
