# Dispatch Instructions — auditor_tier5

You are auditor_tier5, a teamwork_preview_auditor agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_tier5\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Challenger Tier 5 reports and handoffs:
- Core Challenger: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_1\report.md and handoff.md
- Client Challenger: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_2\report.md and handoff.md

## Mission:
Perform comprehensive forensic integrity audit of Milestone Final (Tier 5 Adversarial Coverage Hardening):
1. **Integrity Forensics & Non-Bypass Scan**:
   - Verify that all 100 new Tier 5 tests are genuine empirical assertions, NOT trivial dummy passes (`Assert.True(true)`), hardcoded shortcuts, or facade implementations.
   - Verify that no implementation code in `HPBim/src/` was modified or bypassed to make tests pass.
   - Check for dead code, commented-out code, and empty catch blocks.
2. **Clean Code & Attestation Veracity**:
   - Rule C2: Measure all new test files to confirm strictly <= 300 physical lines.
   - Rule M3: Measure every test method to confirm strictly <= 50 physical lines.
   - Run syntax auditors (`scratch/verify_m4_clean_code.py` or `scratch/RoslynSyntaxAuditor`).
3. **Build & Test Verification**:
   - Run `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
   - Run `dotnet test HPBim/HPBim.slnx` -> confirm 770/770 pass 100%.
   - Run `dotnet test McpShared/McpShared.slnx` -> confirm 908/908 pass 100%.
4. **Deliverable**:
   - Write forensic audit report in `report.md`.
   - Deliver binary verdict (`CLEAN` or `INTEGRITY_VIOLATION`) in `handoff.md`, then send a message back to parent.


## 2026-10-07T23:40:34Z
You are auditor_tier5, a teamwork_preview_auditor agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_tier5\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_tier5\DISPATCH.md.
Challenger reports & handoffs to review:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_1\report.md and handoff.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_tier5_2\report.md and handoff.md

Your mission:
Perform comprehensive forensic integrity audit of Milestone Final (Tier 5 Adversarial Coverage Hardening):
1. Integrity Forensics & Non-Bypass Scan:
   - Verify that all 100 new Tier 5 tests are genuine empirical assertions, NOT trivial dummy passes (Assert.True(true)), hardcoded shortcuts, or facade implementations.
   - Verify that no implementation code in HPBim/src/ was bypassed or compromised.
   - Check for dead code, commented-out code, and empty catch blocks.
2. Clean Code & Attestation Veracity:
   - Rule C2: Measure all new test files to confirm strictly <= 300 physical lines.
   - Rule M3: Measure every test method to confirm strictly <= 50 physical lines.
3. Build & Test Verification:
   - Run dotnet build HPBim/HPBim.slnx -> 0 errors, 0 warnings.
   - Run dotnet test HPBim/HPBim.slnx -> confirm 770/770 pass 100%.
   - Run dotnet test McpShared/McpShared.slnx -> confirm 908/908 pass 100%.
4. Deliverable:
   - Write forensic audit report in report.md.
   - Deliver binary verdict (CLEAN or INTEGRITY_VIOLATION) in handoff.md, then send a message back to parent.
