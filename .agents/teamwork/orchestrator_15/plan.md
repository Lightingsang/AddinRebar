# Remediation Plan: HPGrasshopper Re-audit Remediation (N1–N10)

## Overview
Remediate the 10 audit findings (N1–N10) documented in `plans/reports/261013-0600-hpgrasshopper-reaudit.md` across HPGrasshopper, ensuring zero regressions, strict Clean Code governance (C2, M3, Q-B1, Q-B2, ADR-0007), zero modifications to `McpShared/`, and 100% filter-free passing tests.

## Architecture & Code Boundaries
- **Target Deliverable**: `HPGrasshopper/`
  - `HPGrasshopper.McpBridge` (Revit add-in / in-process execution engine)
  - `HPGrasshopper.Mcp.Server` (Console MCP server / tool registry / seed library)
  - `HPGrasshopper.McpBridge.Tests`, `HPGrasshopper.Mcp.Server.Tests`, `HPGrasshopper.E2E.Tests`
- **Shared Subsystem (STRICTLY IMMUTABLE)**: `McpShared/` (0 modifications permitted)

## Milestones Status
| # | Name | Scope | Dependencies | Status |
|---|------|-------|--------------|--------|
| M1 | P1 Remediation | Findings N1–N5 | none | **DONE** |
| M2 | P2 Remediation | Findings N6–N10 | M1 | **DONE** |
| M3 | Full Regression & Governance | Full suites, ADR-0007, McpShared check | M1, M2 | **DONE** |
| M4 | Final Synthesis & Handoff | Final reporting to Sentinel | M3 | **IN_PROGRESS** |

### Milestone M1: Priority P1 Findings Remediation (N1–N5) — [DONE]
- **N1 — Rebar Seed Atomicity (`generate_rebar_curves`)**:
  - File: `HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs`
  - Action: Validate all curve sets (coplanarity, non-empty, endpoints, bar type availability) before starting rebar creation. If any set fails during or after mutation, throw an exception so the enclosing Revit Transaction automatically rolls back completely.
- **N2 — Truthful Rollback Outcome (`RevitTransactionScope`)**:
  - File: `HPGrasshopper.McpBridge/Execution/RevitTransactionScope.cs`
  - Action: Check `CanvasRollbackResult` from `ICanvasUndoScope`. Combine Revit rollback and canvas rollback into `ExecutionOutcome`. If canvas rollback fails or is partial, report errors and do not return clean `RolledBack = true` / `IsError = false`.
- **N3 — Batch Solve Safe Boundary & Cancellation (`CanvasEditor` & `GrasshopperExecutionEngine`)**:
  - Files: `HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs`, `HPGrasshopper.McpBridge/Tools/CanvasEditor.cs`
  - Action: Pass `CancellationToken` and undo scope into `ExecuteBatch`. Check token at checkpoints before mutation and before final solve/commit. Defer canvas commit until final outcome aligns with Revit transaction boundary.
- **N4 — Baseline Object Lifecycle Ledger (`CanvasUndoScope`)**:
  - File: `HPGrasshopper.McpBridge/Execution/CanvasUndoScope.cs`
  - Action: Fix baseline object tracking for Remove -> Add sequences (prevent baseline objects from being deleted on rollback). Differentiate net transitions between pre-existing baseline objects and newly created objects. Full snapshot of parameters/wires before mutation.
- **N5 — Mandatory Target Identity & Path Normalization (`McpBridgeWorkRequest` & Coordinator)**:
  - Files: `HPGrasshopper.McpBridge/Coordinator/McpBridgeWorkRequest.cs`, `Coordinator/GrasshopperWorkCoordinator.cs`, `Host/GrasshopperAddinApplication.cs`
  - Action: Enforce `RequireTargetIdentity = true` on mutating public routes when target identity is specified. Normalize empty vs null document paths consistently across cache, context extractor, and validator.

### Milestone M2: Priority P2 Findings Remediation (N6–N10)
- **N6 — Player Data Assignment Verification (`PlayerDataTreeHelper`)**:
  - File: `HPGrasshopper.McpBridge/Tools/PlayerDataTreeHelper.cs`
  - Action: Check Boolean return value of `AddVolatileData`. Stage/validate all inputs before mutation. Record rejected inputs into `rejectedInputs`. Prevent index shifting on null/unsupported items.
- **N7 — Preserve Duplicate Output Nicknames (`PlayerOutputExtractor`)**:
  - File: `HPGrasshopper.McpBridge/Tools/PlayerOutputExtractor.cs`
  - Action: Preserve all outputs when multiple components share the same nickname in `requestedOutputs` (using list preservation or unique GUID suffixing without clobbering prior alias).
- **N8 — Preserve Diagnostics on Error & camelCase JSON (`GrasshopperExecutionEngine`)**:
  - File: `HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs`
  - Action: Preserve original diagnostic payload (errors, warnings, rejectedInputs, duration) when Player/script throws in Revit. Use `BridgeJson.Options` for camelCase serialization in `MapToExecuteResult`.
- **N9 — Container Schema Validation for Batch Operations (`CanvasBatchValidator` & `CanvasEditor`)**:
  - Files: `HPGrasshopper.McpBridge/Tools/CanvasBatchValidator.cs`, `HPGrasshopper.McpBridge/Tools/CanvasEditor.cs`
  - Action: Preflight validate that operation containers (`componentsToAdd`, `wiresToConnect`, etc.) are JSON arrays if provided; reject malformed non-array containers instead of silent no-op.
- **N10 — Stale / Unavailable Context Marker (`GrasshopperContextExtractor`)**:
  - File: `HPGrasshopper.McpBridge/Coordinator/GrasshopperContextExtractor.cs`
  - Action: Provide immutable, consistent snapshot with timestamp and explicit `isStale` / `unavailable` markers when UI thread is unavailable; do not mix default assumed values with actual document identity.

### Milestone M3: Full Regression & Clean Code Governance
- **HPGrasshopper Test Suites**: Run 100% filter-free `dotnet test` on:
  - `HPGrasshopper.McpBridge.Tests`
  - `HPGrasshopper.Mcp.Server.Tests`
  - `HPGrasshopper.E2E.Tests`
- **McpShared Regression Suite**: Run 100% filter-free tests on `McpShared/` (`HPRebar.Mcp.Server.Core.Tests`, etc.).
- **Verify 0 McpShared Modifications**: Ensure git status shows 0 changes to `McpShared/`.
- **ADR-0007 Script Quality Walker**: Verify 12/12 embedded seeds pass quality checks without warnings/errors.
- **Clean Code Audit**: Verify Rule C2 (<= 300 lines/file), Rule M3 (<= 50 lines/method), Rule Q-B1 (0 dead code), Rule Q-B2 (0 unannotated catch).

### Milestone M4: Final Synthesis & Sentinel Handoff
- Synthesize all results, write comprehensive `handoff.md`, and report completion to Sentinel via `send_message`.
