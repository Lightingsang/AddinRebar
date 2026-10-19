# Dispatch Instructions — challenger_final_it2_1

You are challenger_final_it2_1, a teamwork_preview_challenger agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_final_it2_1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Worker remediation report and handoff:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\handoff.md

Your mission:
1. Empirically verify that the refactored `ReconcileServiceTier5AdversarialTests.cs` preserves 100% of its original assertion and stress capabilities:
   - Run `dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj --filter-class "*ReconcileServiceTier5AdversarialTests*"` -> 6/6 tests pass.
   - Verify that interleaved status testing (Committed, RolledBack, Committed with malformed keys) executes and validates correctly.
2. Run full Client test suite:
   - `dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj` -> 311/311 pass.
3. Deliver `report.md` and explicit verdict (`APPROVE` or `CHALLENGE_FAILED`) in `handoff.md`, then send a message back to parent.

## 2026-10-08T00:06:59Z
You are challenger_final_it2_1, a teamwork_preview_challenger agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_final_it2_1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Worker remediation report and handoff:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\handoff.md

Your mission:
1. Empirically verify that the refactored `ReconcileServiceTier5AdversarialTests.cs` preserves 100% of its original assertion and stress capabilities:
   - Run `dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj --filter-class "*ReconcileServiceTier5AdversarialTests*"` -> 6/6 tests pass.
   - Verify that interleaved status testing (Committed, RolledBack, Committed with malformed keys) executes and validates correctly.
2. Run full Client test suite:
   - `dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj` -> 311/311 pass.
3. Deliver report.md and explicit verdict (APPROVE or CHALLENGE_FAILED) in handoff.md, then send a message back to parent.
