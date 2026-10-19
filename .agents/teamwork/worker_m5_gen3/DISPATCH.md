## 2026-10-18T10:44:44Z
You are worker_m5_gen3.
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_gen3\
Parent Conversation ID: 1e55a9de-0d52-405d-83fa-fa58bb05a293

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A Forensic Auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY DIRECT TEST RUNNER RULE:
NEVER run `dotnet test` under any circumstances (it locks TestResults logs and triggers Windows Unknown Hard Error).
To build and test, ALWAYS execute:
```powershell
dotnet build HPDcePro/HPDcePro.slnx -c Release --disable-build-servers -m:1
dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1
& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"
& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"
```

MANDATORY READING BEFORE WORK:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Master Scope: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen3\PROJECT.md
- Master Truth Matrix: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md
- Reviewer 1 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_gen3_1\handoff.md
- Reviewer 2 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_gen3_2\handoff.md
- Challenger Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_gen3_1\handoff.md

EXCLUSIVE WRITE OWNERSHIP:
- You own: `docs/rea-dce-pro/TRUTH_MATRIX.md`, `HPDcePro/src/**`, `HPDcePro/tests/**`, and your working directory.
- STRICT READ-ONLY INVARIANT: Zero modifications to `C:\Dce Pro\`. Exactly 1,554 files must remain untouched.

MISSION: Milestone M5 Final Build, 3-Layer Parity & Forensic Integrity Integration

TASKS:
1. Update TRUTH_MATRIX.md:
   - Refresh the summary table at the top of `docs/rea-dce-pro/TRUTH_MATRIX.md` (lines 25–35) to reflect the final certified status:
     All 602 endpoints are now 100% `HOÀN THÀNH` across Subsystems 1 to 9 (0 MOCK, 0 CHƯA GIẢI MÃ).
2. Minor Hardening & Edge-Case Resilience:
   - In `HPDcePro/src/HPDcePro.Revit/Generators/SlabRebarGenerator.cs`: clamp `lenXMm = Math.Max(100.0, ...)` and `lenYMm = Math.Max(100.0, ...)` against degenerate spans where span < 2*cover.
   - In `HPDcePro/src/HPDcePro.Revit/Generators/RevitFramingGenerator.cs`: deduplicate collinear identical support points.
3. Clean Code Governance Compliance Scan:
   - Verify every `.cs` file in `HPDcePro/` satisfies Rule C2 (<= 300 lines/file).
   - Verify every method satisfies Rule M3 (<= 50 lines/method).
   - Verify 0 dead code or commented statements (Rule Q-B1).
   - Verify 0 unannotated catch blocks (Rule Q-B2). All catches must have `// Reason: ...`.
4. Sequential Solution Builds:
   - `dotnet build HPDcePro/HPDcePro.slnx -c Release --disable-build-servers -m:1` (Assert 0 warnings, 0 errors).
   - `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` (Assert 0 warnings, 0 errors).
5. Direct Test Runner Execution:
   - Run `HPDcePro.Core.Tests.exe` (Assert 303/303 tests pass).
   - Run `HPDcePro.Adapters.Tests.exe` (Assert 165/165 tests pass).
   - Verify total 468 tests passed with 0 failures, 0 skipped.
6. Legacy Invariant Check:
   - Verify `C:\Dce Pro\` has exactly 1,554 files and zero modifications.
7. Write your comprehensive handoff report to:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_gen3\handoff.md`
   and notify the parent orchestrator via send_message.
