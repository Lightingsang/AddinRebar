# Dispatch: worker_m4_beam (teamwork_preview_worker)

## Identity & Parent
- Identity: `worker_m4_beam` (teamwork_preview_worker)
- Working Directory: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_beam\`
- Parent: `orchestrator_3` (Conversation ID: `4db7c650-146a-4a05-a1e1-c0fb57e2733b`)

## Mandatory Inputs & Governance
- Authoritative User Request: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read section `## 2026-10-07T22:29:51Z`)
- Project Scope Document: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Survey Findings: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_2\handoff.md` (read full TCVN formulas & tables)
- Clean Code Governance: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`

## File Ownership (Exclusive)
You have exclusive write ownership over:
- `HPRebar/HPRebar.Core/Tcvn5574/` (all new files)
- `HPRebar/HPRebar.Core.Tests/Tcvn5574/` (all new tests)
Do NOT modify `HPRebar/HPRebar/HPRebar.csproj` or UI files.

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Mission: Milestone M4 — TCVN 5574:2018 Engine & Unit Tests in HPRebar.Core
1. Implement in `HPRebar/HPRebar.Core/Tcvn5574/`:
   - `Materials/MaterialGrades.cs`: Enums for `ConcreteGrade` (B15, B20, B25, B30, B35, B40) and `SteelGrade` (CB240-T, CB300-V, CB400-V, CB500-V).
   - `Materials/ConcreteMaterial.cs`: $R_b, R_{bt}, E_b$ lookup per TCVN 5574:2018 Tables 6, 7, 10.
   - `Materials/SteelMaterial.cs`: $R_s, R_{sc}, R_{sw}, E_s, \xi_R, \alpha_R$ calculation per Tables 13, 14, 15 and Clause 8.1.2.3.1.
   - `Models/FlexureDesignResult.cs`: $A_s, A_s', \alpha_m, \xi, \zeta$, singly/doubly flag, reinforcement ratios $\mu, \mu_{min}$.
   - `Models/ShearDesignResult.cs`: $Q, Q_{b,max}$ (crush limit check), $Q_{b,min}, Q_b, Q_{sw}$, required spacing $s_{calc}$, maximum spacing $s_{max}$, constructive limits per Clause 10.3.2.1.
   - `Calculators/TcvnBeamFlexureCalculator.cs`: Pure C# flexural design for rectangular cross-section ($b \times h$).
   - `Calculators/TcvnBeamShearCalculator.cs`: Pure C# shear design with inclined section crack limit and diagonal crushing check.
   - `Calculators/TcvnBeamRebarOptimizer.cs`: Optimal bar selection across standard diameters (14 to 32 mm), multi-layer check, and clear spacing validation $t \ge \max(25\text{mm}, d)$ per Clause 10.3.1.2.
   - `Models/BeamForceEnvelope.cs`: Station, $M^+, M^-, Q$ DTO.
   - `Models/BeamCalculationReportModel.cs`: DTO modeling verification status (ĐẠT / NG) and summary.
2. Hard Constraints:
   - ZERO references to `Autodesk.Revit.*` in `HPRebar.Core` (Rule D1).
   - ZERO references to `ClosedXML` in `HPRebar.Core` (Rule D1).
   - Every file $\le 300$ lines, every method $\le 50$ lines.
3. Unit Tests in `HPRebar/HPRebar.Core.Tests/Tcvn5574/`:
   - Implement comprehensive tests for:
     - `ConcreteMaterialTests`: Table values for B15 to B40.
     - `SteelMaterialTests`: Table values and boundary parameters $\xi_R, \alpha_R$.
     - `TcvnBeamFlexureCalculatorTests`: Singly reinforced beams, doubly reinforced beams, minimum steel ratio $\mu_{min} = 0.1\%$.
     - `TcvnBeamShearCalculatorTests`: Web crushing limit check ($Q > 0.3 R_b b h_0$), concrete-only shear capacity, stirrup spacing calculation and clamp to $s_{max}$.
     - `TcvnBeamRebarOptimizerTests`: Clear spacing validation ($t \ge 25\text{mm}$ bottom, $t \ge 30\text{mm}$ top), crowded rebar detection, optimal bar diameter picking.
4. Run tests:
   ```bash
   dotnet test HPRebar.Core.Tests
   ```
   Ensure 100% PASS (all existing 1530 tests + all new TCVN tests).

## Output
Write your comprehensive implementation and verification report to:
`G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_beam\handoff.md`
and notify orchestrator_3 via `send_message`.


## 2026-10-07T22:44:48Z
You are worker_m4_beam (teamwork_preview_worker).
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_beam\
Your parent is orchestrator_3 (conversation ID: 4db7c650-146a-4a05-a1e1-c0fb57e2733b).

Read your dispatch file first:
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_beam\DISPATCH.md
Read ORIGINAL_REQUEST.md at:
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (check section ## 2026-10-07T22:29:51Z).
Read survey report: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_2\handoff.md (detailed TCVN 5574:2018 formulas, tables, and algorithms).
Read PROJECT.md at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your mission is Milestone M4: TCVN 5574:2018 Engine & Tests in HPRebar.Core:
1. Implement pure domain classes in HPRebar/HPRebar.Core/Tcvn5574/:
   - ConcreteMaterial.cs (B15 to B40, Rb, Rbt, Eb per Tables 6, 7, 10).
   - SteelMaterial.cs (CB240-T, CB300-V, CB400-V, CB500-V, Rs, Rsc, Rsw, xi_R, alpha_R per Tables 13, 14, 15).
   - TcvnBeamFlexureCalculator.cs (singly/doubly reinforced flexural design: As, As', alpha_m, xi, mu).
   - TcvnBeamShearCalculator.cs (web crush check Q <= 0.3*Rb*b*h0, Qb, Qsw, s_calc, s_max).
   - TcvnBeamRebarOptimizer.cs (optimal bar diameter/count selection, clear spacing validation t >= max(25mm, d) per Clause 10.3.1.2).
   - BeamForceEnvelope.cs and BeamCalculationReportModel.cs.
2. ZERO references to Autodesk.Revit.* or ClosedXML in HPRebar.Core (hard constraint Rule D1). File <= 300 lines, method <= 50 lines.
3. Implement extensive unit tests in HPRebar/HPRebar.Core.Tests/Tcvn5574/.
4. Run dotnet test HPRebar.Core.Tests and verify 100% PASS with 0 failures.
