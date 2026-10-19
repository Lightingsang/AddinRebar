# Dispatch Instructions — challenger_final_it2_2

You are challenger_final_it2_2, a teamwork_preview_challenger agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_final_it2_2\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Worker remediation report and handoff:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\handoff.md

Your mission:
1. Empirically verify that the full solution test suite builds and passes with 100% success and 0 regressions:
   - Run `dotnet test HPBim/HPBim.slnx` -> confirm 770/770 pass.
   - Run `dotnet test McpShared/McpShared.slnx` -> confirm 908/908 pass.
2. Verify the MTP-compliant namespace filter execution:
   - `dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj --filter-namespace HPBim.Core.Tests.Adversarial` -> 36/36 pass.
3. Deliver `report.md` and explicit verdict (`APPROVE` or `CHALLENGE_FAILED`) in `handoff.md`, then send a message back to parent.


## 2026-10-08T00:06:59Z
[Message] timestamp=2026-10-08T00:06:59Z sender=44938268-52fa-4afa-af1c-06dc2c4f2512 priority=MESSAGE_PRIORITY_HIGH
You are challenger_final_it2_2, a teamwork_preview_challenger agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_final_it2_2\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_final_it2_2\DISPATCH.md.
Worker remediation report and handoff:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\handoff.md

Your mission:
1. Empirically verify that the full solution test suite builds and passes with 100% success and 0 regressions:
   - Run `dotnet test HPBim/HPBim.slnx` -> confirm 770/770 pass.
   - Run `dotnet test McpShared/McpShared.slnx` -> confirm 908/908 pass.
2. Verify the MTP-compliant namespace filter execution:
   - `dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj --filter-namespace HPBim.Core.Tests.Adversarial` -> 36/36 pass.
3. Deliver report.md and explicit verdict (APPROVE or CHALLENGE_FAILED) in handoff.md, then send a message back to parent.
