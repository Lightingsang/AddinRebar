## 2026-10-10T13:05:21Z
You are explorer_gh_m5_it2_3 (Real-World Workloads & Clean Code Remediation Explorer).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_3

MANDATORY FIRST STEP:
Read the user requirements from:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-10T06:56:11Z.

Also read:
- Master project index: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
- Test Infra specification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_INFRA.md
- FULL Forensic Auditor Evidence Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_gh_m5_1\handoff.md

CONTEXT:
Auditor flagged Tier 4 real-world workloads (`RW-01` to `RW-06`) as asserting local arithmetic (e.g. `Assert.Equal(72, expectedPanels)`) rather than testing real parametric scripts, definitions, or tool executions.
Also, Tier 5 cancellation and AST tests were flagged for asserting on local variables (`int busy = 1; Interlocked.Exchange(ref busy, 0)`).

OBJECTIVE:
Design genuine test implementations for Tier 4 and Tier 5:
1. For RW-01 to RW-06 in `Tier4_RealWorldWorkloadTests.cs`:
   - Define real Grasshopper Roslyn C# scripts or parametric workload definitions (e.g. tower structural grid generation, adaptive panel coordinate transformation, automated rebar parameter mapping).
   - Pass them through `ScriptGuard.Check`, validate syntax, analyze AST with `ScriptAnalyzer`, and execute against `ExecuteRequest`/`ExecuteResult` wire DTOs.
2. For Tier 5 tests in `Tier5_CancellationStressTests.cs` and `Tier5_RoslynAstMutationTests.cs`:
   - Test genuine CancellationToken propagation through `ExecuteRequest` and test real Roslyn AST mutations against `ScriptGuard.Check`.
3. Ensure all test classes strictly comply with Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines).
4. Deliver findings to:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_3\workloads_and_ast_remediation.md`
   and write a self-contained `handoff.md`. Call send_message when done.
