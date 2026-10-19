## 2026-10-10T13:18:06Z
[Message] timestamp=2026-10-10T13:18:06Z sender=65736fe1-0fe9-4743-9fff-fd7f010af55d priority=MESSAGE_PRIORITY_HIGH content=You are worker_gh_m5_it2 (Milestone M5 Iteration 2 Implementation Worker).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_gh_m5_it2

MANDATORY FIRST STEP:
Read the user requirements from:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-10T06:56:11Z.

Also read:
- Master project index: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
- Test Infra specification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_INFRA.md
- Full Forensic Auditor Evidence Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_gh_m5_1\handoff.md
- Explorer 1 Remediation Specification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_1\trivial_assertions_remediation.md
- Explorer 2 Genuine Contracts Blueprint: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_2\genuine_contracts_design.md
- Explorer 3 Real-World Workloads & AST Blueprint: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_3\workloads_and_ast_remediation.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

EXCLUSIVE WRITE OWNERSHIP:
You exclusively own:
- `HPGrasshopper/HPGrasshopper.Mcp.Server/HPGrasshopper.Mcp.Server.csproj`
- `HPGrasshopper/HPGrasshopper.E2E.Tests/**`
- `run-e2e-grasshopper-tests.ps1`
- `TEST_READY.md`
- `PROJECT.md`

CONTEXT & PROBLEM:
Milestone M5 Gate 1 was vetoed by Forensic Auditor (auditor_gh_m5_1: INTEGRITY VIOLATION) and Reviewers:
1. `GrasshopperE2EFakeBridge.cs` was a dummy keyword-matching simulator (`Contains("throw new")`, `Contains("Create")`) that didn't compile scripts or run McpShared security guardrails.
2. `HPGrasshopper.E2E.Tests` was completely disconnected from product deliverables: it didn't reference `HPGrasshopper.Mcp.Server`, never invoked `McpShared` classes, and certified local shadow constants in `GrasshopperE2EContracts.cs`.
3. Over 20 test cases asserted local primitive variables (`Assert.True(isReadOnly)`), constants, or arithmetic (`12 * 6 == 72`) rather than testing real contracts.
4. Tier 4 workloads and Tier 5 cancellation/AST tests tested local delegates and dummy primitives.

TASK OBJECTIVE:
Implement the complete, genuine architecture and remediation designed by Explorers 1, 2, and 3:

1. Wire Real Production Assemblies:
   - In `HPGrasshopper/HPGrasshopper.Mcp.Server/HPGrasshopper.Mcp.Server.csproj`, add `<InternalsVisibleTo Include="HPGrasshopper.E2E.Tests" />`.
   - In `HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj`, add project reference to `..\HPGrasshopper.Mcp.Server\HPGrasshopper.Mcp.Server.csproj`.

2. Eliminate Shadow Contracts & Dummy Facade Bridge:
   - Eliminate all shadow constants in `GrasshopperE2EContracts.cs`; map all tests to canonical production sources in `McpShared` and `HPGrasshopper.Mcp.Server` (`PipeNaming.GrasshopperHost`, `PipeNaming.PipeFor`, `JsonRpcMethods.GrasshopperPrefix`, `HostProfile.Grasshopper`, `GuardProfile.Grasshopper`, `AnalyzerProfile.Grasshopper`, `HostScriptContracts`, `SeedInstaller.LoadSeeds`, `McpServerHost.RegistryToolNames`, `ToolValidator`, `ExecuteRequest`, `ExecuteResult`).
   - Implement `Harness/GrasshopperE2EBridge.cs` (or `GrasshopperTestExecutor.cs`) implementing `ICorrelatedCancelExecutor` and `IBridgeExecutor`. Execute real scripts via Roslyn `ScriptCompiler` (or test executor), enforce `ScriptGuard.Check(code, GuardProfile.Grasshopper)` and `ScriptQuality.Find(code)`, and manage transactional state mutations and rollbacks (`GrasshopperStateLedger`).
   - Replace fake pipe simulation with genuine `PipeListener` / `RequestDispatcher` from `McpShared`.

3. Apply Trivial Assertions Replacements (Tiers 1, 2, 3):
   - Apply all 32 drop-in method replacements from `explorer_gh_m5_it2_1/trivial_assertions_remediation.md` to `Tiers/Tier1_*.cs`, `Tiers/Tier2_*.cs`, and `Tiers/Tier3_*.cs`.
   - Ensure every single assertion evaluates real returned values, AST properties, or guardrail validation outcomes.

4. Implement Genuine Tier 4 Workloads & Tier 5 Adversarial Tests:
   - Apply real Roslyn script definitions and AST/guardrail validations for RW-01 through RW-06 in `Tier4_RealWorldWorkloadTests.cs` per `explorer_gh_m5_it2_3/workloads_and_ast_remediation.md`.
   - Apply genuine cancellation stress testing and AST mutation testing in `Tier5_CancellationStressTests.cs` and `Tier5_RoslynAstMutationTests.cs`.

5. Clean Code Governance Compliance:
   - Rule C2: EVERY single .cs file in `HPGrasshopper.E2E.Tests` strictly <= 300 lines (target <= 230 lines).
   - Rule M3: EVERY method strictly <= 50 lines (target <= 35 lines).
   - Rule Q-B1: Zero commented-out or dead code.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Run: `python scratch/verify_clean_code_ast.py "HPGrasshopper/HPGrasshopper.E2E.Tests" 50` to confirm 0 violations!

6. Verification Commands:
   - `dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release` (0 errors, 0 warnings).
   - `dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests -c Release` (100% tests PASS, exactly 0 modal popups).
   - `powershell -ExecutionPolicy Bypass -File .\run-e2e-grasshopper-tests.ps1` (100% tests PASS across Tier 1, 2, 3, 4, 5, and All).
   - `dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests -c Release` (110/110 PASS, 0 modal popups).
   - `dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests -c Release` (77/77 PASS).

7. Re-publish & Handoff:
   - Update `TEST_READY.md` and `PROJECT.md` with final verified counts and genuine architecture attestation.
   - Write comprehensive report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_gh_m5_it2\handoff.md`.
   - Call `send_message` to notify the orchestrator when finished.
