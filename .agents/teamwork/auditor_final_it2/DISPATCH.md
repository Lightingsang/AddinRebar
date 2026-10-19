# Dispatch Instructions — auditor_final_it2

## 2026-10-08T00:06:59Z

You are auditor_final_it2, a teamwork_preview_auditor agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_final_it2\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Prior audit failure report:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_tier5\report.md
Worker remediation report and handoff:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\handoff.md

Your mission:
Perform comprehensive forensic integrity re-evaluation of Milestone Final (Tier 5 Adversarial Coverage Hardening):
1. **Rule M3 Re-Measurement**:
   - Measure method `ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately` in `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs` using Roslyn compiler AST to confirm it is strictly <= 50 physical lines (worker reported 34 lines).
   - Measure all newly extracted helper methods to confirm strictly <= 50 lines.
2. **Exhaustive Census Re-Verification**:
   - Run `dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj` -> verify OVERALL AUDIT VERDICT: CLEAN / 100% PASS (0 M3 violations, 0 C2 violations across all 10 files and 95 members).
3. **Attestation Veracity & Non-Bypass Scan**:
   - Confirm worker attestations match disk reality.
   - Confirm zero source code modifications in `HPBim/src/`.
   - Confirm zero lines of commented-out code (Q-B1) and all catch blocks have rationale comments (Q-B2).
4. **Build & Test Verification**:
   - Run `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
   - Run `dotnet test HPBim/HPBim.slnx` -> 770/770 pass 100%.
   - Run `dotnet test McpShared/McpShared.slnx` -> 908/908 pass 100%.
5. **Deliverable**:
   - Write forensic audit report in `report.md`.
   - Deliver binary verdict (`CLEAN` or `INTEGRITY_VIOLATION`) in `handoff.md`, then send a message back to parent.
