# Task Assignment: challenger_tier5_1

## Identity & Role
- **Agent Name**: challenger_tier5_1
- **Archetype**: teamwork_preview_challenger
- **Role**: Tier 5 White-Box Coverage Challenger — HPBim.Core (Final Milestone)
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_1\`
- **Project Root**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\`

## Authoritative Inputs (Read First)
1. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read this first).
2. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
3. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md`.
4. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`.

## Mission Scope
You are initiating **Phase 2: Adversarial Coverage Hardening (Tier 5)** for `HPBim.Core`:
1. **White-Box Source Code Analysis**:
   - Deeply inspect all implementation source files in `HPBim/src/HPBim.Core/` (Coordination/, Execution/, Models/, QC/).
   - Compare with existing test suites in `HPBim/tests/HPBim.Core.Tests/`.
   - Systematically identify any untested code paths, edge cases, boundary conditions, tolerance anomalies, null handling, and unexpected error branches.
2. **Empirical Adversarial Stress-Testing**:
   - Write and execute white-box adversarial test cases targeting:
     - `BimModelSchemaValidator`: Corrupted JSON nodes, extreme numbers, empty arrays, unicode marks.
     - `IdempotentElementResolver`: Ambiguous ledger mappings, tombstoned items, multi-handle provenance collisions.
     - `CompositeSourceKey`: Malformed string parsing, exotic delimiters, case-insensitive comparison edge cases.
     - `MeasuredGeometricVerifier`: Decimal rounding near tolerance boundaries (0.0mm, 1.0mm, 2.0mm), missing parameters, unmeasurable curves.
     - `CoordinationTolerances`: Extreme coordinate offsets and scale conversions.
3. **Execution & Regression Verification**:
   - Run `dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj` and your new test cases.
   - Verify that all tests pass 100% and zero regressions are introduced.
4. **Deliverables**:
   - Produce a detailed gap report in `report.md` specifying any uncovered branches and newly authored test cases.
   - Deliver `handoff.md` with explicit verdict: `APPROVE` (if no critical coverage gaps remain and all tests pass) or `CHALLENGE_FAILED` (if unmitigated bugs or severe gaps are found).
   - Send message back to parent.


## 2026-10-07T22:56:20Z
You are challenger_tier5_1, a teamwork_preview_challenger agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_1\DISPATCH.md.

Your mission for Milestone Final (Phase 2: Tier 5 Adversarial Coverage Hardening — HPBim.Core):
1. Perform white-box analysis of HPBim/src/HPBim.Core/ to identify untested code paths, edge cases, null checks, and boundary conditions.
2. Formulate and execute empirical adversarial tests in HPBim.Core.Tests.
3. Run tests and verify 100% pass rate with zero regressions.
4. Deliver report.md (gap report + adversarial test cases) and handoff.md with verdict (APPROVE or CHALLENGE_FAILED), then send a message back to parent.
