# Dispatch: worker_m4_remediation_beam (teamwork_preview_worker)

## Identity & Parent
- Identity: `worker_m4_remediation_beam` (teamwork_preview_worker)
- Working Directory: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation_beam\`
- Parent: `orchestrator_3` (Conversation ID: `4db7c650-146a-4a05-a1e1-c0fb57e2733b`)

## Mandatory Inputs & Governance
- Authoritative User Request: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read section `## 2026-10-07T22:29:51Z`)
- Project Scope Document: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Gate Rejection Report from Challenger 1: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_beam_1\handoff.md` (Read full report!)
- Clean Code Governance: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`

## File Ownership (Exclusive)
You have exclusive write ownership over:
- `HPRebar/HPRebar.Core/Tcvn5574/Calculators/TcvnBeamShearCalculator.cs`
- `HPRebar/HPRebar.Core/Tcvn5574/Calculators/TcvnBeamFlexureCalculator.cs`
- `HPRebar/HPRebar.Core.Tests/Tcvn5574/`
Do NOT modify `HPRebar.csproj` or UI files.

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Mission: Remediation of 2 Defects in Milestone M4

### Defect 1 (CRITICAL — Safety Flaw in Stirrup Spacing):
- **Location**: `HPRebar/HPRebar.Core/Tcvn5574/Calculators/TcvnBeamShearCalculator.cs:181-201`
- **Root Cause**: When shear force is high and $s_{calc} < 50\text{ mm}$, `RoundToPracticalSpacing` clamped up to $50\text{ mm}$ ($s_{rec} > s_{calc}$ providing only ~41% of required capacity), but `IsCompliant` was hardcoded to `true`.
- **Fix**:
  ```csharp
  bool isSpacingAdequate = sCalc >= recommendedSpacing && sCalc >= 50.0;
  return new ShearDesignResult
  {
      ...
      IsCompliant = isSpacingAdequate,
      Message = isSpacingAdequate
          ? "Calculated shear reinforcement satisfies TCVN 5574:2018 inclined section equilibrium."
          : $"Stirrups inadequate: required spacing s_calc = {sCalc:F1}mm < 50mm minimum. Increase stirrup legs or diameter."
  };
  ```

### Defect 2 (MEDIUM — Section Geometry Validation):
- **Location**: `HPRebar/HPRebar.Core/Tcvn5574/Calculators/TcvnBeamFlexureCalculator.cs:54-63`
- **Root Cause**: `ValidateInputs` does not check `h > a + aPrime`. When $h \le a + a'$, degenerate depths produce negative $(h_0 - a')$ and corrupt doubly reinforced calculations.
- **Fix**:
  In `ValidateInputs`, add:
  ```csharp
  if (h <= a + aPrime)
  {
      throw new ArgumentOutOfRangeException(nameof(h), "Total height h must be greater than sum of centroid distances (a + aPrime).");
  }
  ```

### Verification:
1. Run all unit tests:
   ```bash
   dotnet test HPRebar.Core.Tests
   ```
   Ensure 100% PASS across all 1674+ tests with 0 failures!
2. Ensure Clean Code rules: file $\le 300$ lines, method $\le 50$ lines, Rule D1 strictly satisfied.

## Output
Write your remediation report to:
`G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation_beam\handoff.md`
and notify orchestrator_3 via `send_message`.


## 2026-10-07T23:49:54Z
You are worker_m4_remediation_beam (teamwork_preview_worker).
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation_beam\
Your parent is orchestrator_3 (conversation ID: 4db7c650-146a-4a05-a1e1-c0fb57e2733b).

Read your dispatch file first:
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation_beam\DISPATCH.md
Read the challenger rejection evidence:
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_beam_1\handoff.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your mission is to remediate the 2 specific defects in Milestone M4:
1. In TcvnBeamShearCalculator.cs: Set IsCompliant = (sCalc >= recommendedSpacing && sCalc >= 50.0). Provide warning message when inadequate.
2. In TcvnBeamFlexureCalculator.cs: In ValidateInputs, check if (h <= a + aPrime) throw new ArgumentOutOfRangeException(nameof(h), "...").
3. Run dotnet test HPRebar.Core.Tests and confirm 100% PASS across all 1674+ tests with 0 failures.

Write your report to: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation_beam\handoff.md.
When done, report to orchestrator_3 via send_message.
