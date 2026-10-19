# Dispatch Instructions — reviewer_tier5_2

You are reviewer_tier5_2, a teamwork_preview_reviewer agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_tier5_2\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Challenger Tier 5 reports and handoffs:
- Core Challenger: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_1\report.md and handoff.md
- Client Challenger: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_2\report.md and handoff.md

## Mission:
1. Independently and adversarially review the 100 new Tier 5 Adversarial Test cases across `HPBim.Core.Tests` and `HPBim.Client.Tests` for edge-case coverage and non-regression.
2. Verify that the adversarial tests genuinely stress:
   - Extreme inputs (NaN, Infinity, unicode diacritics, negative budgets, non-object JSON array elements).
   - Zero-tolerance geometric thresholds (0.0mm section, 1.0mm elevation, 2.0mm pile center).
   - Full lifecycle idempotency (Recreate -> NoOp transitions, ghost element suppression).
   - Timeout policy boundaries and journal compaction.
3. Verify Clean Code & Governance compliance:
   - Rule C2: All test files <= 300 physical lines.
   - Rule M3: All test methods <= 50 physical lines.
   - Q-B1/Q-B2: No commented-out code, no empty catches without comments.
4. Execute test validation:
   - Run `dotnet test HPBim/HPBim.slnx` -> confirm 770/770 tests pass.
   - Run `dotnet test McpShared/McpShared.slnx` -> confirm 908/908 tests pass.
5. Deliver `report.md` and explicit verdict (`APPROVE` or `REQUEST_CHANGES`) in `handoff.md`, then send a message back to parent.


## 2026-10-07T23:40:34Z
[Message] timestamp=2026-10-07T23:40:34Z sender=44938268-52fa-4afa-af1c-06dc2c4f2512 priority=MESSAGE_PRIORITY_HIGH content=You are reviewer_tier5_2, a teamwork_preview_reviewer agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_tier5_2\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_tier5_2\DISPATCH.md.
Challenger reports & handoffs to review:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_1\report.md and handoff.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_2\report.md and handoff.md

Your mission:
1. Independently and adversarially review the 100 new Tier 5 Adversarial Test cases across HPBim.Core.Tests and HPBim.Client.Tests for edge-case coverage, robustness, and non-regression.
2. Verify Clean Code & Governance compliance:
   - Rule C2: All test files <= 300 physical lines.
   - Rule M3: All test methods <= 50 physical lines.
   - Rule 0: Zero speculation, strict fail-fast on mismatch.
3. Run verification:
   - dotnet test HPBim/HPBim.slnx -> confirm 770/770 tests pass.
   - dotnet test McpShared/McpShared.slnx -> confirm 908/908 tests pass.
4. Deliver report.md and explicit verdict (APPROVE or REQUEST_CHANGES) in handoff.md, then send a message back to parent.
