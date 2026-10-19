# Scope: HPBim P0/P1 Remediation (Pipeline & Coordination Ledger)

## Architecture & Code Boundaries
- **Product Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\`
- **Core Domain**: `HPBim/HPBim.Core/` (`netstandard2.0` - pure logic, zero Revit references)
  - `Coordination/` (`CompositeSourceKey`, `ElementPlanItem`, `BimExecutionPlan`, `ExecutionBatch`, `CoordinationTolerances`, etc.)
  - `Journal/` (`IntentJournal`, `JournalRecord`, `BimCoordinationLedgerService`, `BimCoordinationLedgerSerializer`)
  - `Verification/` (`MeasuredGeometricVerifier`, `GeometricVerificationRule`)
- **Client & Automation**: `HPBim/HPBim.Client/` (.NET 8.0/.NET 10.0 - Named pipe communication, Roslyn runners, CLI commands)
  - `Execution/` (`BimPlanRunner`, `ReconcileService`, `ActiveDocumentQuery`)
  - `Commands/` (`GenerateCommand`, `VerifyCommand`, `ValidateCommand`)
- **Testing Suites**:
  - `HPBim/HPBim.Core.Tests/`
  - `HPBim/HPBim.Client.Tests/`
  - `HPBim/HPBim.E2E.Tests/` (remove all shadow test contracts, test production classes directly)

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Canonical String SourceKey | Chuẩn hóa `SourceKey` thành chuỗi canonical `[DocIdentifier]#[Category]:[PrimaryHandle]` trên toàn bộ wire DTO, `ScriptArgs`, Roslyn runner, receipt. Roundtrip equality 100%. | M1 | R1 |
| 2 | Cardinality 1:1 & No Synthesized Created | Đối soát 1:1 items và execution records. Cấm tạo `Created` khi thiếu record hoặc response truncated >64KB; đánh dấu `OutcomeUnknown`. | M1 | R1 |
| 3 | Strict All-or-Nothing Batch Rollback | Rollback toàn bộ transaction khi có cấu kiện lỗi, không ghi receipt Extensible Storage, CLI báo exit code 1. | M1 | R1 |
| 4 | Elimination of Heuristic Fallbacks | Áp dụng rotationDeg và topLevel cho Column; cấm FirstOrDefault() cho Pipe/PipingSystem, báo ERR_SYMBOL_NOT_FOUND nếu thiếu. | M1 | R1 |
| 5 | Target Document Identity Binding | CLI generate truy vấn Title, Path, VersionGUID từ Revit API; gắn vào ExecutionPlan và IntentJournal. Pre-flight kiểm tra tính đồng bộ. | M2 | R2 |
| 6 | Complete Plan Hash | ComputePlanHash bao phủ profile polygon, độ dày, cao độ, thông số tiết diện, family, host, top level, document identity. | M2 | R2 |
| 7 | Ledger & Journal Persistence Across Restarts | LoadFromFile hydrate `_mappings`; UpdateStatusAsync nạp lại đĩa khi cache miss; Atomic file replace (.tmp -> replace) có file lock. | M2 | R2 |
| 8 | 3-Factor Reconcile Verification | ReconcileService chỉ công nhận Committed khi khớp OperationId, PlanHash, TargetDocId. Giữ OutcomeUnknown nếu thiếu. | M2 | R2 |
| 9 | Measured Geometric Verification | Nối MeasuredGeometricVerifier vào VerifyCommand; truy vấn Solid/Curve/Elevation thực từ Revit API qua pipe; cấm số liệu gán sẵn, trả Inconclusive nếu thiếu. | M3 | R3 |
| 10 | Shadow Mocks Elimination in E2E Tests | Xóa sạch shadow mocks trong HPBim.E2E.Tests, test trực tiếp production classes, chỉ fake ở Named Pipe transport I/O boundary. | M3 | R3 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Phase 1: P0 Core Wire, Cardinality & Batch Rollback | Items 1, 2, 3, 4: Canonical SourceKey, 1:1 cardinality, strict batch rollback, no heuristic fallbacks | none | DONE |
| M2 | Phase 2: P1 Document Identity, Hash & Persistence | Items 5, 6, 7, 8: Document identity binding, complete plan hash, ledger/journal hydration, 3-factor reconcile | M1 | IN_PROGRESS |
| M3 | Phase 3: Measured Geometric QA/QC & E2E Real Classes | Items 9, 10: MeasuredGeometricVerifier CLI hook, eliminate shadow mocks in E2E tests, full regression pass | M2 | PLANNED |
