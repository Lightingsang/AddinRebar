# Dispatch Instructions — reviewer_m4_it2_1

## Identity & Context
- **Agent**: `reviewer_m4_it2_1`
- **Archetype**: `teamwork_preview_reviewer`
- **Role**: `Correctness & Interface Reviewer (M4 It 2)`
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_it2_1\`
- **Authoritative User Request**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read this first).
- **Project Scope Document**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- **Worker Report & Handoff**:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\report.md`
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\handoff.md`

## Mission
1. Objectively and adversarially review all remediation code delivered by `worker_m4_it2_r1`:
   - `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs` (decomposition into `GetQueryHeaderScript` + `GetQueryFilterAndReturnScript` + `GetQueryScript`)
   - `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs` (defensive check `if (item.ValueKind != JsonValueKind.Object) continue;` + `ParseSingleReceipt`)
   - `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs` (decomposition of `GetPreambleScript`)
   - `HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReceiptReaderTests.cs` & `ExtensibleStorageReceiptReaderTests.cs`
2. Verify interface contracts, functional fidelity, and compilation with Revit MCP contracts.
3. Run verification:
   - `dotnet build HPBim/HPBim.slnx`
   - `dotnet test HPBim/HPBim.slnx`
4. Deliver `report.md` and explicit verdict (`APPROVE` or `REQUEST_CHANGES`) in `handoff.md`, then send a message back to parent.

## 2026-10-07T22:10:36Z
You are reviewer_m4_it2_1, a teamwork_preview_reviewer agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_it2_1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_it2_1\DISPATCH.md.
The worker remediation report and handoff are at:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\handoff.md

Your mission:
1. Objectively and adversarially review all remediation code delivered by worker_m4_it2_r1:
   - HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs (decomposition into GetQueryHeaderScript + GetQueryFilterAndReturnScript + GetQueryScript).
   - HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs (defensive check if (item.ValueKind != JsonValueKind.Object) continue; + ParseSingleReceipt).
   - HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs (decomposition of GetPreambleScript).
   - HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReceiptReaderTests.cs & ExtensibleStorageReceiptReaderTests.cs.
2. Verify interface contracts, functional fidelity, and compilation.
3. Run verification:
   - dotnet build HPBim/HPBim.slnx
   - dotnet test HPBim/HPBim.slnx
4. Deliver report.md and explicit verdict (APPROVE or REQUEST_CHANGES) in handoff.md, then send a message back to parent.
