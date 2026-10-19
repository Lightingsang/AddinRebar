# Dispatch Instructions — reviewer_final_it2_1

You are reviewer_final_it2_1, a teamwork_preview_reviewer agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_final_it2_1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Worker remediation report and handoff:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\handoff.md

Your mission:
1. Objectively and adversarially review the remediation code in:
   - `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs` (verify method span <= 35 lines, helpers <= 10 lines, file <= 210 lines).
   - `HPBim/tests/HPBim.Client.Tests/Connection/TimeoutPolicyTier5AdversarialTests.cs` (verify explicit non-throw assertion).
2. Verify that Rule M3 and Rule C2 are strictly satisfied across the entire file and test class.
3. Run verification:
   - `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
   - `dotnet test HPBim/HPBim.slnx` -> confirm 770/770 tests pass.
4. Deliver `report.md` and explicit verdict (`APPROVE` or `REQUEST_CHANGES`) in `handoff.md`, then send a message back to parent.


## 2026-10-08T00:06:59Z

[Message] timestamp=2026-10-08T00:06:59Z sender=44938268-52fa-4afa-af1c-06dc2c4f2512 priority=MESSAGE_PRIORITY_HIGH content=You are reviewer_final_it2_1, a teamwork_preview_reviewer agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_final_it2_1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_final_it2_1\DISPATCH.md.
Worker remediation report and handoff:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\handoff.md

Your mission:
1. Objectively and adversarially review the remediation code in:
   - `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs` (verify method span <= 35 lines, helpers <= 10 lines, file <= 210 lines).
   - `HPBim/tests/HPBim.Client.Tests/Connection/TimeoutPolicyTier5AdversarialTests.cs` (verify explicit non-throw assertion).
2. Verify that Rule M3 and Rule C2 are strictly satisfied across the entire file and test class.
3. Run verification:
   - `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
   - `dotnet test HPBim/HPBim.slnx` -> confirm 770/770 tests pass.
4. Deliver report.md and explicit verdict (APPROVE or REQUEST_CHANGES) in handoff.md, then send a message back to parent.
