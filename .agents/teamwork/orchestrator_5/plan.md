# Milestone Execution Plan: HPBim P0/P1 Remediation

## Executive Overview
Remediate the 10 blocking review findings across 3 sequential milestones following the Teamwork Project Orchestration standard:
Explorer(3) -> Worker(1) -> Reviewer(2) -> Challenger(2) -> Forensic Auditor(1) -> Gate.

---

## Milestone M1: Phase 1 — P0 Core Wire, Cardinality & Strict Batch Transaction
- **Focus**:
  1. Canonical String `SourceKey`: `[DocIdentifier]#[Category]:[PrimaryHandle]` on wire DTOs, `ElementPlanItem`, `ScriptArgs`, Roslyn runner, `HPBimCoordinationReceipt`.
  2. Cardinality 1:1 & No Synthesized `Created`: Strict 1:1 check between plan items and Revit return records; when records missing or response truncated >64KB, mark `OutcomeUnknown`.
  3. Strict All-or-Nothing Batch Rollback: Rollback transaction if any element fails; no receipt in Extensible Storage; CLI exit code 1.
  4. Script Runner Robustness: Apply `rotationDeg` and `topLevel` for Columns; no `FirstOrDefault()` for pipes, throw `ERR_SYMBOL_NOT_FOUND` if symbol missing.
- **Cycle**:
  - Dispatch 3 Explorers in parallel to analyze existing implementations and draft exact remediation diffs.
  - Dispatch 1 Worker to implement M1 fixes and verify tests in `HPBim.Core.Tests` and `HPBim.Client.Tests`.
  - Dispatch 2 Reviewers independently to verify Clean Code and correctness.
  - Dispatch 2 Challengers to write adversarial unit and wire tests.
  - Dispatch 1 Forensic Auditor to check for integrity violations.
  - Evaluate Gate.

---

## Milestone M2: Phase 2 — P1 Document Identity, Complete Hash & Journal Hydration
- **Focus**:
  1. Target Document Identity Binding: Active doc query via Revit API thread (Title, Path, VersionGUID) in `generate`, validate in pre-flight runner.
  2. Comprehensive Plan Hash: Hash all geometric properties (polygon vertices, thickness, elevation, parameters, family, host, doc id).
  3. Hydration & Persistence: `LoadFromFile` hydrates `_mappings`; `UpdateStatusAsync` reloads on cache miss; atomic file replace with file lock.
  4. 3-Factor Reconcile: `OperationId` + `PlanHash` + `TargetDocId` matching required for `Committed`.
- **Cycle**:
  - Explorers -> Worker -> Reviewers -> Challengers -> Forensic Auditor -> Gate.

---

## Milestone M3: Phase 3 — Measured Geometric Verification QA/QC & E2E Tests Refactoring
- **Focus**:
  1. Real Geometric Verification: Wire `MeasuredGeometricVerifier` into `VerifyCommand`; live queries for solids/curves/elevations; return `Inconclusive` when comparison data missing; eliminate hardcoded metric literals.
  2. Remove Shadow Mocks in E2E Tests: Refactor `HPBim.E2E.Tests` to directly test production classes (`HPBim.Core`, `HPBim.Client`), only mocking Named Pipe transport I/O boundary.
  3. Full Repo Zero Regressions Verification: Ensure 100% of repo test suites pass.
- **Cycle**:
  - Explorers -> Worker -> Reviewers -> Challengers -> Forensic Auditor -> Gate.

---

## Exit Criteria
All acceptance criteria met, all gates pass with CLEAN audit verdicts, full regression pass across repo. Final handoff to Sentinel.
