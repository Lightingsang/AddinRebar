# Dispatch Instructions — challenger_m4_it2_1

## Identity & Context
- **Agent**: `challenger_m4_it2_1`
- **Archetype**: `teamwork_preview_challenger`
- **Role**: `Query Script & Array Parsing Challenger (M4 It 2)`
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_it2_1\`
- **Authoritative User Request**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read this first).
- **Project Scope Document**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- **Worker Report & Handoff**:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\report.md`
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\handoff.md`

## Mission
1. Empirically and adversarially challenge `ExtensibleStorageQueryScriptTemplate.cs`:
   - Verify that `GetQueryScript()` generates valid, functional Roslyn C# script.
   - Verify that concatenated script produces identical tokens and compiles cleanly.
   - Run tests in `ExtensibleStorageReceiptReaderTests.cs`.
2. Empirically and adversarially challenge `ExtensibleStorageReceiptReader.cs`:
   - Run adversarial parsing tests in `AdversarialReceiptReaderTests.cs`:
     - Non-object elements in array (integers, strings, booleans, nulls) are gracefully skipped without exceptions.
     - Mixed arrays correctly extract valid objects while ignoring primitives.
     - Malformed JSON maps are handled safely.
3. Run test suites:
   - `dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj --filter-class "*ReceiptReader*"`
4. Deliver `report.md` and explicit verdict (`APPROVE` or `CHALLENGE_FAILED`) in `handoff.md`, then send a message back to parent.

## 2026-10-07T22:10:36Z
You are challenger_m4_it2_1, a teamwork_preview_challenger agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_it2_1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_it2_1\DISPATCH.md.
The worker remediation report and handoff are at:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\handoff.md

Your mission:
1. Empirically and adversarially challenge ExtensibleStorageQueryScriptTemplate.cs:
   - Verify that GetQueryScript() generates valid, functional Roslyn C# script.
   - Run tests in ExtensibleStorageReceiptReaderTests.cs.
2. Empirically and adversarially challenge ExtensibleStorageReceiptReader.cs:
   - Run adversarial parsing tests in AdversarialReceiptReaderTests.cs:
     - Non-object elements in array (integers, strings, booleans, nulls) are gracefully skipped without exceptions.
     - Mixed arrays correctly extract valid objects while ignoring primitives.
     - Malformed JSON maps are handled safely.
3. Run test suites:
   - dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj --filter-class "*ReceiptReader*"
4. Deliver report.md and explicit verdict (APPROVE or CHALLENGE_FAILED) in handoff.md, then send a message back to parent.
