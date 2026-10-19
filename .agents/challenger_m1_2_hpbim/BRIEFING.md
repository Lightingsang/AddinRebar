# BRIEFING — 2026-10-06T00:10:00Z

## Mission
Empirical adversarial verification and stress testing of TCVN detailing rules, QC clash engine, clearance checker, and BIM model serialization in HPBim.Core.

## 🔒 My Identity
- Archetype: challenger
- Roles: critic, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m1_2_hpbim
- Original parent: f21aade0-effc-4aa5-a071-ae17d49dabe3
- Milestone: M1 (TCVN Detailing & QC Engine Adversarial Verification)
- Instance: 2 of 2

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code in `HPBim/src/HPBim.Core`
- Empirical verification mandatory — must write and run tests/oracles, no trusting unverified claims
- Report verdict: APPROVE or REQUEST_CHANGES

## Current Parent
- Conversation ID: f21aade0-effc-4aa5-a071-ae17d49dabe3
- Updated: not yet

## Review Scope
- **Files to review**:
  * `HPBim/src/HPBim.Core/Tcvn/TcvnRebarCalculator.cs`
  * `HPBim/src/HPBim.Core/Tcvn/TcvnStandards.cs`
  * `HPBim/src/HPBim.Core/Tcvn/ConcreteCoverRules.cs`
  * `HPBim/src/HPBim.Core/Qc/QcClashDetector.cs`
  * `HPBim/src/HPBim.Core/Qc/ClearanceChecker.cs`
  * `HPBim/src/HPBim.Core/Qc/SpatialGridIndex.cs`
  * `HPBim/src/HPBim.Core/Qc/QcEngine.cs`
  * `HPBim/src/HPBim.Core/Serialization/BimModelSerializer.cs`
  * `HPBim/src/HPBim.Core/Models/*.cs`
- **Interface contracts**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\PROJECT.md`
- **Review criteria**: Mathematical correctness, edge case resilience, TCVN 5574:2018 compliance, vncad ground truth fidelity, roundtrip serialization fidelity.

## Key Decisions Made
- Executed dedicated empirical test harness console `runner/Runner.csproj` referencing `HPBim.Core.csproj`.
- Executed 95 assertions across TCVN formulas, commercial splices, stirrup zoning, QC clashes, clearance thresholds, and serialization.
- Verdict reached: REQUEST_CHANGES due to critical blockers in serialization.

## Artifact Index
- `progress.md` — Liveness heartbeat and task progress
- `DISPATCH.md` — Incoming message log
- `BRIEFING.md` — Working memory and status
- `runner/Program.cs` — Empirical verification test harness source
- `runner/Runner.csproj` — Harness project definition
- `handoff.md` — 5-component handoff report

## Attack Surface
- **Hypotheses tested**:
  * TCVN rebar unit weight formula matches vncad ground-truth within 0.001 kg/m (CONFIRMED PASS).
  * Commercial splice calculator behavior for L < 11.7m, L = 11.7m, L = 15m, L = 25m (CONFIRMED PASS).
  * Stirrup zoning a100/a200 for beams and columns (CONFIRMED PASS).
  * Column vs opening clash detection and penetration depth (CONFIRMED PASS).
  * Clearance threshold at 99mm vs 100mm vs 150mm (CONFIRMED PASS).
  * BimModelSerializer round-trip fidelity (FAILED - CRITICAL BUGS REPRODUCED).
- **Vulnerabilities found**:
  * BUG 1 (CRITICAL): `Vector2D` recursive property cycle (`LeftNormal` / `RightNormal`) causes `JsonException` object cycle crash at depth 64 on any model with walls/beams.
  * BUG 2 (CRITICAL): `Polygon2D` cannot be deserialized by `System.Text.Json` due to type mismatch between constructor parameter `IEnumerable<Point2D>` and property `IReadOnlyList<Point2D>`.
  * BUG 3 (MEDIUM): `QcClashDetector` false positive on perimeter beams due to default `includeBoundary: true` in `Polygon2D.Contains`.
  * BUG 4 (LOW): `ClearanceChecker` column width subtraction ignores column aspect ratio/depth on rectangular columns.
- **Untested angles**:
  * Revit 2026 live named pipe transport (Milestone 3 scope).

## Loaded Skills
None specified in dispatch.
