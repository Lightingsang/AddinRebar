# Dispatch Instructions — reviewer_tier5_1

You are reviewer_tier5_1, a teamwork_preview_reviewer agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_tier5_1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Challenger Tier 5 reports and handoffs:
- Core Challenger: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_1\report.md and handoff.md
- Client Challenger: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_2\report.md and handoff.md

## Mission:
1. Independently and objectively review the 100 new Tier 5 Adversarial Test cases added across `HPBim.Core.Tests` (36 tests) and `HPBim.Client.Tests` (64 tests).
   - Target files in `HPBim.Core.Tests/Adversarial/`:
     - `BimModelSchemaValidatorAdversarialCoverageTests.cs`
     - `IdempotentElementResolverAdversarialCoverageTests.cs`
     - `CompositeSourceKeyAdversarialCoverageTests.cs`
     - `MeasuredGeometricVerifierAdversarialCoverageTests.cs`
   - Target files in `HPBim.Client.Tests/`:
     - `Connection/IntentJournalTier5AdversarialTests.cs`
     - `Connection/TimeoutPolicyTier5AdversarialTests.cs`
     - `Recovery/ReceiptReaderTier5AdversarialTests.cs`
     - `Recovery/ReconcileServiceTier5AdversarialTests.cs`
     - `Pipeline/BimPlanRunnerScriptTemplateTier5AdversarialTests.cs`
     - `Pipeline/BimPipelineEngineTier5AdversarialTests.cs`
2. Verify test validity, authentic assertion rigor, and absence of dummy mocks.
3. Verify Clean Code Compliance:
   - Rule C2: All test files <= 300 physical lines.
   - Rule M3: All test methods <= 50 physical lines.
   - Rule 0: Zero speculation, strict failure on discrepancies.
4. Execute test validation:
   - Run `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
   - Run `dotnet test HPBim/HPBim.slnx` -> 100% tests pass (770/770).
5. Deliver `report.md` and explicit verdict (`APPROVE` or `REQUEST_CHANGES`) in `handoff.md`, then send a message back to parent.


## 2026-10-07T23:40:34Z
[Message] timestamp=2026-10-07T23:40:34Z sender=44938268-52fa-4afa-af1c-06dc2c4f2512 priority=MESSAGE_PRIORITY_HIGH content=You are reviewer_tier5_1, a teamwork_preview_reviewer agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_tier5_1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_tier5_1\DISPATCH.md.
Challenger reports & handoffs to review:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_1\report.md and handoff.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_2\report.md and handoff.md

Your mission:
1. Objectively and adversarially review the 100 new Tier 5 Adversarial Test cases authored across HPBim.Core.Tests (36 tests) and HPBim.Client.Tests (64 tests).
2. Verify test validity, absence of dummy mocks/shortcuts, and true assertion rigor.
3. Verify Clean Code compliance:
   - Rule C2: All test files <= 300 physical lines.
   - Rule M3: All test methods <= 50 physical lines.
4. Run verification:
   - dotnet build HPBim/HPBim.slnx
   - dotnet test HPBim/HPBim.slnx
5. Deliver report.md and explicit verdict (APPROVE or REQUEST_CHANGES) in handoff.md, then send a message back to parent.
