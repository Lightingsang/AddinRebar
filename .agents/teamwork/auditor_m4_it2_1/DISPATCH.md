# Dispatch Instructions — auditor_m4_it2_1

## Identity & Context
- **Agent**: `auditor_m4_it2_1`
- **Archetype**: `teamwork_preview_auditor`
- **Role**: `Forensic Integrity Auditor (M4 It 2)`
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_it2_1\`
- **Authoritative User Request**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read this first).
- **Project Scope Document**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- **Previous Audit Report**:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\report.md`
- **Worker Remediation Report & Handoff**:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\report.md`
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\handoff.md`

## Mission
Perform comprehensive forensic integrity re-evaluation of Milestone M4 following remediation by `worker_m4_it2_r1`:
1. **Clean Code Compliance & Rule M3 Re-Verification**:
   - Rule C2: Measure all 24 files created or modified to confirm strictly $\le 300$ physical lines.
   - Rule M3: Measure every method across all 24 files to confirm strictly $\le 50$ physical lines.
   - Specifically verify that `ExtensibleStorageQueryScriptTemplate.cs:GetQueryScript()` violation is resolved:
     - `GetQueryHeaderScript` is $\le 30$ lines.
     - `GetQueryFilterAndReturnScript` is $\le 30$ lines.
     - `GetQueryScript` is $\le 5$ lines.
   - Specifically verify that `BimPlanRunnerScriptTemplate.cs:GetPreambleScript()` has been decomposed into methods $\le 35$ lines.
2. **Attestation Veracity Verification**:
   - Verify that the worker's master 24-file attestation table in `worker_m4_it2_r1/report.md` matches physical disk reality using both Roslyn AST and Python lexer.
3. **Forensic Integrity & Non-Bypass Scan**:
   - Verify that non-object JSON array elements in `ExtensibleStorageReceiptReader.cs` are authentically filtered.
   - Check for hardcoded test results, fake mocks, dummy facades, commented-out code, and empty catch blocks without comments.
4. **Build & Test Verification**:
   - Run `dotnet build HPBim/HPBim.slnx` $\rightarrow$ 0 errors, 0 warnings.
   - Run `dotnet test HPBim/HPBim.slnx` $\rightarrow$ 100% tests pass.
   - Run `dotnet test McpShared/McpShared.slnx` $\rightarrow$ 100% tests pass.
   - Run `dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj` $\rightarrow$ verify exit code 0.
   - Run `python scratch/verify_m4_clean_code.py` $\rightarrow$ verify exit code 0.
5. **Deliverable**:
   - Write forensic audit report in `report.md`.
   - Deliver binary verdict (`CLEAN` or `INTEGRITY_VIOLATION`) in `handoff.md`, then send a message back to parent.
