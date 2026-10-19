# DISPATCH — Explorer M2 Fix (Remediation for Forensic Audit Failure)

## Mission
Investigate and design the exact technical fix for the Forensic Integrity Violation and test failures reported in Milestone M2 Iteration 1.

## References
- Authoritative User Request: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (section `## 2026-10-07T02:05:11Z`)
- Project Scope: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_11\PROJECT.md`
- Normative Quy Chế: `C:\Users\STR-HP03\.gemini\antigravity\brain\087c0df3-fff1-40c1-83ce-c0d4c6cb6acd\quy_che_phoi_hop_cad_revit_chuan_ky_su_ket_cau.md`

## Full Forensic Audit Evidence (MANDATORY — UNFILTERED & VERBATIM)
From Forensic Auditor `auditor_m2_1` (`G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_1\handoff.md`):
- Verdict: **INTEGRITY VIOLATION**
- Evidence: Independent execution of `dotnet test tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj` failed with 4 errors in `FailFastGateAdversarialProbeTests`.
- Flaws in `FailFastGate.cs`:
  1. Fails to reject non-numeric values (`NaN`, `Infinity`) for elevations and dimensions because in IEEE 754 arithmetic `NaN <= 0` evaluates to false.
  2. Permits empty/whitespace supports (`new[] { "", "   " }`) to pass through without throwing `FailFastCoordinationException` because it only checks `Supports.Count < 2`.
  3. Lacks support validation for `CoordinationCategory.Footing`.

From Challenger `challenger_m2_2` and `reviewer_m2_2`:
- Failing test file: `HPBim/tests/HPBim.Core.Tests/Coordination/FailFastGateAdversarialProbeTests.cs`:
  1. `EvaluateCadSource_NaNDimensions_MustFailGate` (FAILED: Expected False, Actual True)
  2. `EvaluateCadSource_NaNElevation_MustFailGate` (FAILED: Expected False, Actual True)
  3. `EvaluateCadSource_InfinityElevation_MustFailGate` (FAILED: Expected False, Actual True)
  4. `EvaluateCadSource_EmptyOrWhitespaceSupports_MustFailGate` (FAILED: Expected False, Actual True)
- CLI Remnant Backdoors & Speculation:
  1. `BuildGradeBeamNetworkCommand.cs` & `AnalyzeFootingsCommand.cs`: Fail-Fast is gated behind an optional `--fail-fast` CLI flag rather than being default. Line-pairing (180..320mm) and 1800mm cluster heuristics remain active.
  2. `GenerateGradeBeamsCommand.cs`: Contains `--force-unverified` bypass flag and silent fallback to `defaultSymbol` (`B_200x400`).
  3. `GenerateFoundationSlabsCommand.cs`: Completely bypasses `FailFastGate` and creates 57 footings from scratch JSON.
  4. `CadSourceSpec.CrossCheckPassed`: Defaults to `true`, allowing elements with zero cross-check sources to pass FF-6 automatically.

## Tasks
1. Formulate the exact code fix in `FailFastGate.cs`:
   - Use `double.IsFinite(val)` (rejecting `NaN` and `Infinity`).
   - Validate each string in `Supports` with `!string.IsNullOrWhiteSpace(s)`.
   - Add support validation for `CoordinationCategory.Footing` (e.g. piles / soil foundation).
   - Ensure `CadSourceSpec.CrossCheckPassed` defaults to `false` unless explicitly verified.
2. Formulate the remediation for CLI commands in `HPBim.Cli/Commands/`:
   - Make `FailFastGate` mandatory and default (remove `--fail-fast` opt-in requirement; fail fast on any violation).
   - Remove the `--force-unverified` bypass flag in `GenerateGradeBeamsCommand`.
   - Remove or gate `GenerateFoundationSlabsCommand` to strictly enforce FailFastGate.
3. Formulate the verification command to guarantee 100% green tests (314/314 PASS).
4. Deliver report and handoff.md in `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_fix\`.


## 2026-10-07T03:19:04Z
[Message] sender=11067cc4-24c4-4007-9827-ff3e7c0e82f5 priority=MESSAGE_PRIORITY_HIGH
Milestone M2 failed Forensic Integrity Audit (auditor_m2_1) and Challenger review (challenger_m2_2, challenger_m2_1).
FULL AUDIT EVIDENCE:
1. 4 test failures in HPBim/tests/HPBim.Core.Tests/Coordination/FailFastGateAdversarialProbeTests.cs:
   - EvaluateCadSource_NaNDimensions_MustFailGate
   - EvaluateCadSource_NaNElevation_MustFailGate
   - EvaluateCadSource_InfinityElevation_MustFailGate
   - EvaluateCadSource_EmptyOrWhitespaceSupports_MustFailGate
2. In FailFastGate.cs:
   - double.NaN and double.Infinity pass because NaN <= 0 is false. Needs !double.IsFinite().
   - Empty/whitespace supports pass. Needs !string.IsNullOrWhiteSpace().
   - Missing support check for Footings.
   - CadSourceSpec.CrossCheckPassed defaults to true.
3. In HPBim.Cli/Commands:
   - Optional --fail-fast flag in BuildGradeBeamNetworkCommand and AnalyzeFootingsCommand (must be default/mandatory).
   - --force-unverified bypass flag in GenerateGradeBeamsCommand (must be removed).
   - GenerateFoundationSlabsCommand creates 57 footings without calling FailFastGate.

Investigate and formulate the exact code fix strategy to resolve every single audit violation and guarantee 314/314 tests pass.
Deliver your report and handoff.md in G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_fix\ and send completion message to parent.
