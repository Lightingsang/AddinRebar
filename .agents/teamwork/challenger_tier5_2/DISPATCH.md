# Task Assignment: challenger_tier5_2

## Identity & Role
- **Agent Name**: challenger_tier5_2
- **Archetype**: teamwork_preview_challenger
- **Role**: Tier 5 White-Box Coverage Challenger — HPBim.Client (Final Milestone)
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_2\`
- **Project Root**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\`

## Authoritative Inputs (Read First)
1. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read this first).
2. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
3. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md`.
4. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`.

## Mission Scope
You are initiating **Phase 2: Adversarial Coverage Hardening (Tier 5)** for `HPBim.Client`:
1. **White-Box Source Code Analysis**:
   - Deeply inspect all implementation source files in `HPBim/src/HPBim.Client/` (Connection/, Recovery/, Pipeline/).
   - Compare with existing test suites in `HPBim/tests/HPBim.Client.Tests/`.
   - Systematically identify any untested code paths, edge cases, recovery branches, and timeout boundary conditions.
2. **Empirical Adversarial Stress-Testing**:
   - Write and execute white-box adversarial test cases targeting:
     - `IntentJournal`: File corruption, truncated JSONL records, concurrent append locks, rapid status mutations.
     - `TimeoutPolicy`: Boundary durations, zero/negative times, queue deadline expirations, transport drops.
     - `ExtensibleStorageReceiptReader`: Malformed schema JSON, non-object array items, corrupted receipt fields, missing GUIDs.
     - `ReconcileService`: Complex multi-intent reconciliation, interleaved committed/rolled-back/unknown intents.
     - `BimPlanRunnerScriptTemplate`: String template assembly, pre-flight gate parameter injection, coordinate precision helpers.
     - `BimPipelineEngine`: Disconnect simulation, partial dry-run rollbacks, warning capture without masking.
3. **Execution & Regression Verification**:
   - Run `dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj` and your new test cases.
   - Verify that all tests pass 100% and zero regressions are introduced.
4. **Deliverables**:
   - Produce a detailed gap report in `report.md` specifying any uncovered branches and newly authored test cases.
   - Deliver `handoff.md` with explicit verdict: `APPROVE` (if no critical coverage gaps remain and all tests pass) or `CHALLENGE_FAILED` (if unmitigated bugs or severe gaps are found).
   - Send message back to parent.


## 2026-10-07T22:56:20Z
[Message] timestamp=2026-10-07T22:56:20Z sender=44938268-52fa-4afa-af1c-06dc2c4f2512 priority=MESSAGE_PRIORITY_HIGH
Your mission for Milestone Final (Phase 2: Tier 5 Adversarial Coverage Hardening — HPBim.Client):
1. Perform white-box analysis of HPBim/src/HPBim.Client/ to identify untested code paths, recovery edge cases, timeout boundaries, and script template handling.
2. Formulate and execute empirical adversarial tests in HPBim.Client.Tests.
3. Run tests and verify 100% pass rate with zero regressions.
4. Deliver report.md (gap report + adversarial test cases) and handoff.md with verdict (APPROVE or CHALLENGE_FAILED), then send a message back to parent.
