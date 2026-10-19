# Handoff Report: 1:1 Cardinality & Truncation / Synthesized Created Elimination (R1.2)

**Agent**: `explorer_m1_rem_2`  
**Date**: 2026-10-08  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_rem_2\`  
**Target Milestone**: M1 (Remediation R1.2)  
**Parent Agent**: `orchestrator_5` (`f9a0ee81-542e-45ae-b53d-c6a316aeff55`)

---

## 1. Observation

1. **Synthesized `Created` Status in Production Code**:
   In `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.PlanExecution.cs` lines 207–226:
   ```csharp
   private static IReadOnlyList<ElementExecutionRecord> ParseElementRecords(ExecuteResult execRes, BimExecutionPlan plan)
   {
       if (execRes.Value != null && execRes.Value.Value.ValueKind == JsonValueKind.Object)
       {
           var root = execRes.Value.Value;
           if (root.TryGetProperty("records", out var recordsProp) && recordsProp.ValueKind == JsonValueKind.Array)
           {
               return recordsProp.EnumerateArray().Select(ParseSingleElementRecord).ToList();
           }
       }

       return plan.Items.Select(item => new ElementExecutionRecord
       {
           SourceKey = item.SourceKey,
           Category = item.Category,
           Mark = item.Mark,
           Status = execRes.IsError ? ElementExecutionStatus.Failed : ElementExecutionStatus.Created,
           ErrorReason = execRes.IsError ? execRes.Message : null
       }).ToList();
   }
   ```
   Line 223 directly assigns `Status = ElementExecutionStatus.Created` when `execRes.Value` lacks a `records` property and `!execRes.IsError`.

2. **Truncation Flag Ignored & 64KB Bridge Limit**:
   In `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar.McpBridge\Service\ResultSerializer.cs` lines 50–59:
   `ResultSerializer` sets `Truncated = true` and converts `Value` to a string with `…[truncated]` if serialized output exceeds `_maxOutputBytes` (64KB).
   In `McpShared\HPRebar.Mcp.Contracts\Messages\ExecuteResult.cs` line 41:
   `public bool Truncated { get; set; }` exists on the contract.
   In `BimPipelineEngine.PlanExecution.cs`:
   There is zero reference to `execRes.Truncated`. When `Value` becomes a truncated string, line 209 evaluates `ValueKind == JsonValueKind.Object` to `false`, falling into line 223 which synthesizes `Created` for all items.

3. **Absence of 1:1 Cardinality Reconciliation**:
   In `BimPipelineEngine.PlanExecution.cs` line 214:
   `return recordsProp.EnumerateArray().Select(ParseSingleElementRecord).ToList();`
   If `recordsProp` contains fewer elements than `plan.Items` (e.g. 5 out of 50), the missing 45 elements are completely omitted from the return list rather than flagged as `OutcomeUnknown`.

4. **Missing `OutcomeUnknown` Enum Member**:
   In `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Execution\ElementExecutionStatus.cs` lines 6–32:
   `ElementExecutionStatus` only contains:
   `Created = 1`, `Updated = 2`, `NoOp = 3`, `Skipped = 4`, `Failed = 5`.
   `OutcomeUnknown` is absent.

5. **Test Suite Baseline**:
   Running `dotnet test tests/HPBim.Core.Tests` in `HPBim/`: 417 passed, 0 failed.
   Running `dotnet test tests/HPBim.Client.Tests` in `HPBim/`: 322 passed, 0 failed.
   Running `dotnet test tests/HPBim.E2E.Tests` in `HPBim/`: 42 passed, 0 failed.
   Total HPBim tests currently green: 781 tests.

---

## 2. Logic Chain

1. From Observation 1, `BimPipelineEngine.PlanExecution.cs` line 223 synthesizes `ElementExecutionStatus.Created` for every item in `plan.Items` whenever `execRes.Value` is not an object with a `"records"` array, provided `execRes.IsError` is false.
2. From Observation 2, when large models (>64KB serialized output) run in Revit, `HPRebar.McpBridge` truncates `Value` to a JSON string and sets `execRes.Truncated = true`. Because `Value` is a string, line 209 (`ValueKind == JsonValueKind.Object`) returns false. Thus, any truncated execution falls directly into line 223 and claims 100% of elements were `Created`, even though no records were received.
3. From Observation 3, when `records` is returned but has fewer elements than `plan.Items` (deficit), the returned list has fewer items than `plan.Items`. Missing items are never marked `OutcomeUnknown` and are never reconciled 1:1.
4. From Observation 4, `ElementExecutionStatus` cannot represent that an element's outcome is unknown because `OutcomeUnknown` does not exist in the enum.
5. Therefore, resolving R1.2 requires:
   - Adding `OutcomeUnknown = 6` to `ElementExecutionStatus`.
   - Adding an explicit `execRes.Truncated` gate in `ParseElementRecords` that marks all items `OutcomeUnknown`.
   - Forbidding `Created` synthesis when `records` is absent (marking `Failed` if rolled back, or `OutcomeUnknown` otherwise).
   - Performing strict 1:1 matching by `CompositeSourceKey` across `plan.Items` and marking any deficit as `OutcomeUnknown`.
   - Propagating `isOutcomeUnknown = true` and `Success = false` in `ResolvePlanExecutionOutcome`, `ExecutePlanAsync`, and `BimPipelineResult`.

---

## 3. Caveats

1. In `HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs`, the default fake execution handler returned `Value = null`. Upgrading `FakeRevitPipeClient` to simulate realistic 1:1 records payload from `request.Args` is required so existing tests don't break when the synthetic fallback is removed.
2. In `SingleExecutionPlanTests.cs` lines 120–175, an existing test mocked only 2 records for a full model plan and asserted `Assert.Equal(2, result.ElementRecords.Count)`. Under strict 1:1 cardinality, the result will have 1:1 records matching `plan.Items.Count` (with the remaining items marked `OutcomeUnknown`). Worker should adjust the test setup or assertion to test 2 items against a 2-item plan, or assert 1:1 cardinality.
3. No source code files outside `.agents/teamwork/explorer_m1_rem_2/` were modified during this read-only investigation.

---

## 4. Conclusion

The root cause of R1.2 violations is located entirely within `ParseElementRecords` and `ResolvePlanExecutionOutcome` in `HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs` coupled with the missing `OutcomeUnknown` enum value in `HPBim.Core/Execution/ElementExecutionStatus.cs`.

A complete, production-ready fix strategy and exact code snippets have been authored in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_rem_2\report.md`

Worker can directly apply Snippets 1 through 6 to achieve 100% compliance with R1.2, eliminate synthesized `Created`, enforce 1:1 cardinality, and ensure all truncation/deficits are marked `OutcomeUnknown`.

---

## 5. Verification Method

1. **Compilation Check**:
   ```bash
   dotnet build g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx
   ```
2. **Core Tests**:
   ```bash
   dotnet test g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Core.Tests
   ```
3. **Client Tests**:
   ```bash
   dotnet test g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests
   ```
4. **Adversarial Truncation & Deficit Verification**:
   Inspect or run the newly added tests recommended in Section 6 of `report.md`:
   - `ExecutePlanAsync` with `Truncated = true` -> verifies all items marked `OutcomeUnknown`, `IsOutcomeUnknown == true`, `Success == false`.
   - `ExecutePlanAsync` with `Value = null` -> verifies `CreatedCount == 0`, all items marked `OutcomeUnknown` or `Failed`.
   - `ExecutePlanAsync` with 5 items in plan and 3 returned -> verifies exactly 5 records returned (1:1 cardinality) with 2 marked `OutcomeUnknown` and `ErrorCode == "ERR_RECORD_DEFICIT"`.
