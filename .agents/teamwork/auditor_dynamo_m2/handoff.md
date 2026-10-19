# Forensic Audit Report — Milestone 2 (HPDynamo MCP)

**Work Product**: Milestone 2 Touchpoints (`DynamoEvaluationSync.cs`, `CanvasGraphRunner.cs`, `DynamoScriptGlobals.cs`, `run_dyn_file/code.cs`, `DynamoExecutionEngine.cs`, `DynamoBridgeCoordinator.cs`, `Milestone2Tests.cs`)  
**Profile**: General Project  
**Integrity Mode**: Development  
**Auditor**: `auditor_dynamo_m2`  
**Date**: 2026-10-09  
**Verdict**: **INTEGRITY VIOLATION**

---

## 1. Observation

Direct empirical observations across all 7 audited files:

### Check 1: Zero Dummy Facades & Hardcoded Test Returns
- **Status**: **PASS**
- `DynamoEvaluationSync.cs`: Genuine `DispatcherFrame` message pump and `EvaluationCompleted` unhooking in `finally`.
- `DynamoExecutionEngine.cs`: Captures `linkedToken` from `RevitTransactionScope.Execute` (lines 62, 81, 113) and forwards it directly to runners.
- `DynamoBridgeCoordinator.cs`: Authentic 3-state atomic CAS machine (`WorkItemTracker`) tracking `Queued`, `Running`, `Completed`; preserves `_busy = 1` until completion and drains queue on `Dispose()`.
- `run_dyn_file/code.cs`: Awaits `RunAndWait(() => activeCanvasWs.Run())` before querying terminal output nodes.

### Check 2: Clean Code Line Limits (File <= 300, Method <= 50)
- **Status**: **FAIL (Rule M3 Method Length Exceeded)**
- **File Lengths (Limit: <= 300 lines)**:
  - `DynamoEvaluationSync.cs`: 86 lines (PASS)
  - `CanvasGraphRunner.cs`: 228 lines (PASS)
  - `DynamoScriptGlobals.cs`: 80 lines (PASS)
  - `run_dyn_file/code.cs`: 121 lines (PASS)
  - `DynamoExecutionEngine.cs`: 182 lines (PASS)
  - `DynamoBridgeCoordinator.cs`: 296 lines (PASS)
  - `Milestone2Tests.cs`: 220 lines (PASS)
- **Method Lengths (Limit: <= 50 lines - Rule M3)**:
  - `CanvasGraphRunner.cs`:
    - `ExecuteDesignScript` (lines 120–189): **70 lines total**, **61 lines method body** (`{ ... }`). **FAIL (Exceeds 50 lines)**.
    - `RunDynFile` (lines 67–118): **52 lines total** (signature + body; body alone is 43 lines).
  - All other methods across the 6 remaining files are strictly <= 50 lines.

### Check 3: Empty Catches & Dead Code (Rule Q-B1, Q-B2)
- **Status**: **PASS**
- Commented-out dead code: 0 lines detected.
- Empty catch blocks: 0 empty catches detected. All catch blocks contain structured logging (`Log.Warning`, `Log.Debug`, `Log.Error`) or documented justifications with return values.

### Check 4: Test Suite Execution & Assertion Authenticity
- **Status**: **PASS**
- All 5 test suites pass 100% with 0 failures and 0 skipped (Total: 1,453 / 1,453 tests):
  1. `HPDynamo.McpBridge.Tests`: 126 passed, 0 failed, 0 skipped
  2. `HPDynamo.Mcp.Server.Tests`: 112 passed, 0 failed, 0 skipped
  3. `HPDynamo.E2E.Tests`: 201 passed, 0 failed, 0 skipped
  4. `HPRebar.Mcp.Server.Core.Tests`: 899 passed, 0 failed, 0 skipped
  5. `HPRebar.McpBridge.Core.Net48Tests`: 115 passed, 0 failed, 0 skipped
- `Milestone2Tests.cs`: Zero tautologies (`Assert.True(true)` does not exist). Every test asserts authentic state changes, CAS transitions, cancellation isolation, queue drainage exceptions, or argument validation.

---

## 2. Logic Chain

1. **Mandatory Audit Requirement**: The audit assignment and DISPATCH.md stipulate:
   - "Clean Code Line Limits: Every file must be <= 300 lines (Rule C2). Every method must be <= 50 lines (Rule M3)."
   - "Audit Veto Rule: If ANY integrity violation, dummy facade, hardcoded test return, or blocking clean code defect is found, report INTEGRITY VIOLATION. Otherwise report CLEAN."
2. **Defect Location**: In `HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs`, method `ExecuteDesignScript` spans lines 120 through 189:
   - Method signature: lines 120–126
   - Opening brace `{`: line 127
   - Closing brace `}`: line 189
   - Total lines: 70 lines (exceeds 50).
   - Method body lines between braces: 61 lines (lines 128–188, exceeds 50).
3. **Remediation Feasibility**: In `DynamoEvaluationSync.cs`, Worker M2 correctly extracted `CheckCompletion` to keep `RunAndWait` under 50 lines. However, in `CanvasGraphRunner.cs`, `ExecuteDesignScript` bundles node placement, evaluation waiting, error checking, output extraction, and cleanup into a single 70-line method. Extracting helper methods (e.g., `PlaceCodeBlockNode`, `CleanupCodeBlockNode`) will easily bring this method well below the 50-line limit.
4. **Conclusion of Logic Chain**: Because one mandatory forensic check (method length <= 50) failed, the auditor is bound by rule to report INTEGRITY VIOLATION and reject the work product until refactored.

---

## 3. Caveats

- **Functional Correctness is High**: The actual implementation of RV-02 (evaluation sync and rollback on error), RV-03 (`linkedToken` forwarding), and RV-05 (3-state atomic queue tracking preserving Revit thread ownership) is genuinely authentic and functioning, as evidenced by 1,453 passing tests.
- **No Production Code Edited by Auditor**: In accordance with the auditor's non-negotiable constraint ("Audit-only — do NOT modify implementation code"), no code was modified by this auditor. Remediation must be performed by worker_m2 or the designated implementer.

---

## 4. Conclusion

**Verdict: INTEGRITY VIOLATION**

The work product fails mandatory check 2 due to a method length limit violation (Rule M3):
- File: `HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs`
- Method: `ExecuteDesignScript` (Lines 120–189: 70 lines total, 61 lines body).
- Required: Every method must be $\le 50$ lines.

**Action Required**:
- Worker M2 must refactor `ExecuteDesignScript` in `CanvasGraphRunner.cs` to be $\le 50$ lines (e.g., extract `PlaceCodeBlock` or `CleanupNode`).
- (Recommended): Also compact or extract `RunDynFile` (currently 52 lines total) to ensure both methods are cleanly $\le 50$ lines including signatures.

---

## 5. Verification Method

To reproduce this finding and independently verify:

1. **Verify Method Line Count in CanvasGraphRunner.cs**:
   ```python
   # Run AST/line check
   python scratch/audit_m2_forensics.py
   ```
   *Output confirms*:
   `ExecuteDesignScript (L120-L189): total=70, body=61 [OVER_50]`

2. **Verify Test Suite Baseline**:
   ```powershell
   dotnet test HPDynamo\HPDynamo.McpBridge.Tests
   dotnet test HPDynamo\HPDynamo.Mcp.Server.Tests
   dotnet test HPDynamo\HPDynamo.E2E.Tests
   ```
   *Expected*: All pass cleanly.
