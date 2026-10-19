# Architectural Decomposition Report: CanvasGraphRunner (M2 Iteration 2)

**Agent**: `explorer_dynamo_m2_it2_2`  
**Role**: Scope Explorer for M2 Iteration 2 (Audit Remediation)  
**Target File**: `HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs`  
**Date**: 2026-10-09  

---

## 1. Problem Analysis & Forensic Audit Findings

During the forensic audit of Milestone 2 (`auditor_dynamo_m2`), the auditor issued an **INTEGRITY VIOLATION** under **Rule M3** (Clean Code standard: Method length $\le 50$ lines):
1. **Primary Defect**: `ExecuteDesignScript` in `CanvasGraphRunner.cs` spans lines 120–189:
   - Total lines: **70 lines** (exceeds 50).
   - Method body: **61 lines** (exceeds 50).
2. **Secondary Warning**: `RunDynFile` spans lines 67–118:
   - Total lines: **52 lines** (signature + body; body is 43 lines).
   - While body $\le 50$, total line count exceeds 50 by 2 lines, triggering audit warnings.
3. **File Length**: `CanvasGraphRunner.cs` currently has 229 lines, well within the 300-line limit (Rule C2).

---

## 2. Decomposition Architecture & Design

To resolve the violation with **zero behavior changes** and guarantee that **every single method** is strictly $\le 50$ lines under both total line and body line measurements, we decompose `CanvasGraphRunner.cs` by extracting cohesive private static helper methods.

### 2.1 Extracted Helper Responsibilities

1. **`PlaceCodeBlockNode`** (Node Placement):
   - Computes placement coordinates ($X, Y$) relative to existing nodes.
   - Instantiates `CodeBlockNodeModel` with `Guid.NewGuid()`, library services, and element resolver.
   - Dispatches `DynamoModel.CreateNodeCommand`.
   - **Line count**: 22 lines total, 16 lines body.

2. **`WaitForDesignScriptEvaluation`** (Evaluation Synchronization & State Checking):
   - Invokes `DynamoEvaluationSync.RunAndWait` (30s timeout).
   - Handles cancellation and timeout exception throwing.
   - Checks `cbn.State == ElementState.Error` and throws `InvalidOperationException`.
   - **Line count**: 26 lines total, 19 lines body.

3. **`DeleteCanvasNode`** (Rollback & Ephemeral Node Deletion):
   - Dispatches `DynamoModel.DeleteModelCommand(nodeId)`.
   - Shared between normal `dryRun=true` cleanup and error `catch` rollback.
   - **Line count**: 4 lines total, 2 lines body.

4. **`OpenWorkspaceFile`** (Canvas Workspace Loading):
   - Logs opening operation and dispatches `DynamoModel.OpenFileCommand`.
   - Validates and returns active `HomeWorkspaceModel`.
   - **Line count**: 10 lines total, 5 lines body.

5. **`WaitForDynFileEvaluation`** (Dyn File Evaluation & Error Checking):
   - Invokes `DynamoEvaluationSync.RunAndWait` (60s timeout).
   - Handles cancellation and timeout exception throwing.
   - Validates `errorCount == 0`, throwing `InvalidOperationException` if nodes are in error state.
   - **Line count**: 27 lines total, 20 lines body.

---

## 3. Method Line Count & Compliance Ledger

Measured using the forensic auditor's exact AST/brace parser (`scratch/audit_m2_forensics.py`):

| Method Name | Visibility | Total Lines | Body Lines | Limit (<= 50) | Status |
|:---|:---|:---:|:---:|:---:|:---:|
| `RunActiveGraph` | `public` | 44 | 38 | <= 50 | **PASS** |
| `RunDynFile` | `public` | 30 | 22 | <= 50 | **PASS** |
| `ExecuteDesignScript` | `public` | 40 | 32 | <= 50 | **PASS** |
| `OpenWorkspaceFile` | `private static` | 10 | 5 | <= 50 | **PASS** |
| `WaitForDynFileEvaluation` | `private static` | 27 | 20 | <= 50 | **PASS** |
| `PlaceCodeBlockNode` | `private static` | 22 | 16 | <= 50 | **PASS** |
| `WaitForDesignScriptEvaluation` | `private static` | 26 | 19 | <= 50 | **PASS** |
| `DeleteCanvasNode` | `private static` | 4 | 2 | <= 50 | **PASS** |
| `ApplyParameterOverrides` | `private static` | 15 | 13 | <= 50 | **PASS** |
| `ExtractOutputs` | `private static` | 21 | 19 | <= 50 | **PASS** |

### File-Level Compliance (Rule C2)
- **Total File Lines**: **270 lines** (Limit: $\le 300$ lines).
- **Compliance**: **PASS** (30 lines under ceiling).
- **Rule M3 Compliance**: **100% PURE PASS** (all 10 methods $\le 44$ lines total, with at least 6 lines of headroom).

---

## 4. Invariance & Behavior Preservation Audit

1. **Public API Invariance**:
   - `RunActiveGraph`: Signature, arguments, exceptions, return object shape identical.
   - `RunDynFile`: Signature, arguments, exceptions, return object shape identical.
   - `ExecuteDesignScript`: Signature, arguments, exceptions, return object shape identical.
2. **Exception Contracts Preserved**:
   - `ArgumentNullException.ThrowIfNull(viewModel)`: Preserved.
   - `ArgumentException` on empty `code`: Preserved.
   - `FileNotFoundException` on missing `filePath`: Preserved.
   - `OperationCanceledException` when cancellation requested: Preserved.
   - `InvalidOperationException` on evaluation timeout, node errors, or canvas evaluation failures: Preserved with exact verbatim messages.
3. **Execution Semantics Preserved**:
   - Ephemeral node deletion on `dryRun = true`: Preserved via `DeleteCanvasNode`.
   - Ephemeral node deletion on `catch (Exception)`: Preserved via `DeleteCanvasNode`.
   - Logging strings and format strings: Verbatim identical.

---

## 5. Artifacts Created

1. **`proposed_CanvasGraphRunner.cs`**:
   - Location: `.agents/teamwork/explorer_dynamo_m2_it2_2/proposed_CanvasGraphRunner.cs`
   - Fully formatted, ready-to-deploy replacement source file.
2. **`CanvasGraphRunner.patch`**:
   - Location: `.agents/teamwork/explorer_dynamo_m2_it2_2/CanvasGraphRunner.patch`
   - Git unified diff patch against current `HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs`.
3. **`verify_decomposition.py`**:
   - Location: `.agents/teamwork/explorer_dynamo_m2_it2_2/verify_decomposition.py`
   - Automated line-count validator confirming 10/10 methods pass Rule M3 and file passes Rule C2.
