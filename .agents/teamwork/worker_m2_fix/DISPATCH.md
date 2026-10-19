## 2026-10-07T03:33:39Z

You are worker_m2_fix, a C# implementation worker for HPBim Milestone M2 Remediation.

Your working directory is:
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_fix\

First, READ the authoritative user request at:
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-07T02:05:11Z and ĐIỀU 0: Nguyên Tắc Vàng Tối Thượng.

Next, READ the remediation package prepared by explorer_m2_fix:
- Report: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_fix\handoff.md
- Analysis: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_fix\analysis.md
- Proposed FailFastGate: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_fix\proposed_FailFastGate.cs
- Proposed CadSourceSpec: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_fix\proposed_CadSourceSpec.cs
- Unified Patch: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_fix\remediation.patch

YOUR OBJECTIVE:
Apply the required changes to fix the 4 failing adversarial tests in FailFastGateAdversarialProbeTests and eliminate all speculation/bypass backdoors in CLI commands:

1. `HPBim/src/HPBim.Core/Coordination/CadSourceSpec.cs`:
   Change default `CrossCheckPassed = false;`

2. `HPBim/src/HPBim.Core/Coordination/FailFastGate.cs`:
   - Enforce IEEE 754 non-NaN, non-Infinity checks via `IsFinite` for TopElevation, BottomElevation, Width, Height, Diameter.
   - Enforce `!string.IsNullOrWhiteSpace(s)` for each element in Supports.
   - Add support requirement for `CoordinationCategory.Footing` (validSupports.Count >= 1).
   - Reject BottomElevation >= TopElevation when both are finite.
   - You can directly copy or replace with `proposed_FailFastGate.cs`.

3. `HPBim/src/HPBim.Cli/Commands/BuildGradeBeamNetworkCommand.cs`:
   Remove opt-in `--fail-fast` flag check: Fail-Fast is always mandatory (`if (unverifiedList.Count > 0) throw new FailFastCoordinationException(...)`).

4. `HPBim/src/HPBim.Cli/Commands/AnalyzeFootingsCommand.cs`:
   Remove opt-in `--fail-fast` flag check: Fail-Fast is always mandatory (`if (clusterViolations.Count > 0) throw new FailFastCoordinationException(...)`).

5. `HPBim/src/HPBim.Cli/Commands/GenerateGradeBeamsCommand.cs`:
   - Remove `--force-unverified` bypass flag: Fail-Fast is always strictly enforced.
   - Eliminate silent fallback to `defaultSymbol` (`B_200x400`). If symbol is missing in Revit, throw `InvalidOperationException($"[ĐIỀU 0] Không tìm thấy FamilySymbol '{typeName}' trong Revit. Cấm tự gán loại mặc định.")`.

6. `HPBim/src/HPBim.Cli/Commands/GenerateFoundationSlabsCommand.cs`:
   - Inject and enforce `IFailFastGate`.
   - Reject `isSpeculativeCluster == true`.
   - Run `_failFastGate.EvaluateCadSource` on all footings and throw `FailFastCoordinationException` if violations exist.

VERIFICATION:
Run:
`dotnet test G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Core.Tests\HPBim.Core.Tests.csproj`
`dotnet test G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\HPBim.Client.Tests.csproj`
Verify that 100% 314/314 tests pass (0 failed), including all 4 `FailFastGateAdversarialProbeTests`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

OUTPUT:
Write `handoff.md` and `progress.md` in `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_fix\` detailing the exact files modified and verbatim test command outputs.
Send a completion message back when done.
