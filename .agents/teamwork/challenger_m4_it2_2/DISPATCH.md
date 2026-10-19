# Dispatch Instructions — challenger_m4_it2_2

## 2026-10-07T22:10:36Z

## Identity & Context
- **Agent**: `challenger_m4_it2_2`
- **Archetype**: `teamwork_preview_challenger`
- **Role**: `Preamble & Idempotency Challenger (M4 It 2)`
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_it2_2\`
- **Authoritative User Request**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read this first).
- **Project Scope Document**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- **Worker Report & Handoff**:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\report.md`
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\handoff.md`

## Mission
1. Empirically and adversarially challenge `BimPlanRunnerScriptTemplate.cs`:
   - Verify that decomposed `GetPreambleScript()` maintains full fidelity: coordinate helpers (`Ft`, `Mm`, `ToXyz`), target document pre-flight verification, level/grid caching, and type collectors.
   - Run pre-flight gate tests: `dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj --filter-class "*PreFlightGateTests*"`
2. Empirically and adversarially challenge Auto-Reconciliation & Idempotency:
   - Run `AutoReconcileIdempotencyTests.cs`:
     - Disconnect after commit -> receipt found -> auto-reconcile marks Committed -> rerun creates 0 duplicate elements (`CreatedCount == 0`, `NoOpCount == 4`).
     - Disconnect before commit -> receipt absent -> auto-reconcile marks Failed -> rerun creates elements cleanly.
   - Run `ReconcileServiceTests.cs`.
3. Deliver `report.md` and explicit verdict (`APPROVE` or `CHALLENGE_FAILED`) in `handoff.md`, then send a message back to parent.
