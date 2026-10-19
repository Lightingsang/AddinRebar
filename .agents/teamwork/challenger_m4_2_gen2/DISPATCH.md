# Dispatch Instructions: challenger_m4_2_gen2

## Identity
- Role: Adversarial Stress Challenger & BOQ Verifier
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_2_gen2
- Target Milestone: Milestone 4 (`HPBim.Cli` & AI Agent Packaging)

## Scope & Tasks
1. Stress test CLI commands and parsing:
   - Run CLI tests in `HPBim/tests/HPBim.Client.Tests/Cli/`:
     * ValidateCommandTests (valid, invalid, missing input, missing file)
     * EstimateCommandTests (exact 7966.5 kg rebar check, partition check, output file writing)
     * GenerateCommandTests (dry-run, pipeline reporting, pipe error)
     * VerifyCommandTests (match, mismatch, missing file)
2. Verify exact benchmark BOQ metrics:
   - Check that `BoqEstimator.Estimate(NhaPho5x18mFixture.CreateModel())` yields:
     * Total Rebar: exactly 7966.5 kg
     * Concrete Volume > 0
     * Formwork Area > 0
     * Fixtures and Pipes matching benchmark counts
3. Clean Code verification:
   - Check line counts of all files under `HPBim/src/HPBim.Cli/`: all <= 200 lines.
4. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.

## 2026-10-06T03:35:02Z
You are challenger_m4_2_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_2_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_2_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md

Adversarially probe CLI commands and BOQ:
1. Run all 30 CLI unit tests in HPBim/tests/HPBim.Client.Tests/Cli/.
2. Empirically verify BoqEstimator yields exactly 7966.5 kg rebar on NhaPho5x18mFixture.
3. Verify file line counts under HPBim.Cli are all <= 200 lines.
Write handoff.md with test counts and verdict APPROVE or REQUEST_CHANGES and report back via send_message.
