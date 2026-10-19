## 2026-10-10T12:30:12Z

You are explorer_gh_m5_3 (E2E Acceptance Test Suite Tiers 3, 4 & 5 Design Explorer).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_3

MANDATORY FIRST STEP:
Read the user requirements from:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-10T06:56:11Z.

Also read:
- Master project index: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
- Test Infra specification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_INFRA.md
- Reference: Inspect `HPDynamo/HPDynamo.E2E.Tests/Tiers/Tier3_*.cs`, `Tier4_*.cs`, `Tier5_*.cs`.

OBJECTIVE:
Design the test cases for Tier 3 (Cross-Feature Combinations), Tier 4 (Real-World Scenarios), and Tier 5 (Adversarial Hardening):
1. Tier 3: Pairwise Combinatorial & Cross-Feature Matrix:
   - Design >=12 test cases covering feature interactions (e.g. Canvas Read + Batch Edit; Batch Edit + Player Run; Player Run + Dual Safety Rollback; Dynamic Registry Tool Proposal + Execution + Retirement; Script Guard + Cancellation).
2. Tier 4: Real-World Application Scenarios (from `TEST_INFRA.md`):
   - RW-01: Parametric Tower Structure (Grids, Columns, Floors, Canvas solve)
   - RW-02: Facade Adaptive Panelization (Surface divide, adaptive component placement, Player)
   - RW-03: Automated Rebar Detailing (Curve-to-rebar, stirrup distribution, TransactionGroup)
   - RW-04: DryRun Safety Verification (Canvas edit, Revit mutations, 100% rollback confirmation)
   - RW-05: Tool Registry Dynamic Lifecycle (Draft -> Test -> Propose -> Publish -> Execute)
   - RW-06: Solar Analysis & DirectShape Bake (Solar vector eval, directshape mesh bake)
   - Design at least 6 comprehensive test cases.
3. Tier 5: Adversarial Hardening:
   - Roslyn AST mutations, parameter fuzzing, security opt-in switch, stress cancellation under load.
4. Clean Code compliance:
   - All files <= 300 lines (Rule C2), all methods <= 50 lines (Rule M3).
5. Deliver your test design to:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_3\e2e_tier3_tier4_tier5_design.md`
   and write a self-contained `handoff.md`. Call send_message when done.
