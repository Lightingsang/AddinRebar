# Dispatch: explorer_survey_beam_2

## Identity
- Identity: explorer_survey_beam_2 (teamwork_preview_explorer)
- Working Directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_2\
- Parent: orchestrator_3 (Conversation ID: 4db7c650-146a-4a05-a1e1-c0fb57e2733b)

## Authoritative Inputs
- Read `ORIGINAL_REQUEST.md` at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (specifically check section `## 2026-10-07T22:29:51Z`)
- Read Clean Code Governance: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`

## Mission & Scope: Survey Focus Area 2 (TCVN 5574:2018 Engine, ETABS/Excel Import, HPRebar.Core & Tests)
Investigate the calculation and engineering domain logic in `HPRebar/HPRebar.Core/` and `HPRebar/HPRebar.Core.Tests/`:
1. Check existing calculators, beam models, rebar models in `HPRebar.Core`:
   - Are there existing TCVN or Eurocode/ACI calculation engines?
   - What beam cross-section and rebar layout data structures exist?
   - Ensure strictly NO reference to `Autodesk.Revit.*` in `HPRebar.Core` (hard constraint).
2. TCVN 5574:2018 requirements for beam flexure and shear calculation:
   - Flexural design of rectangular section ($b \times h$): concrete grade (B20, B25, B30, etc. with $R_b, R_{bt}, \xi_R, \alpha_R$), steel grade (CB240-T, CB300-V, CB400-V, CB500-V with $R_s, R_{sc}, R_{sw}$), calculation of required tension steel $A_s$ (and compression steel $A_s'$ if needed).
   - Shear design: shear force $Q$ along the span (support left, midspan, support right), concrete shear capacity $Q_b$, required shear reinforcement $A_{sw} / s$, maximum stirrup spacing $s_{max}$, minimum shear reinforcement.
   - Bar selection & spacing optimization algorithm: optimal bar diameter and count, checking clear spacing $t \ge \max(25\text{mm}, d)$ according to TCVN 5574:2018 clause 10.3.
3. Internal force data import:
   - Excel import (`ClosedXML`): expected table structure (Station, Case/Combo, $M_{max}, M_{min}, V_{max}$, etc.)
   - ETABS 22 integration: inspect `HPEtabs/` in repo, understand how frame forces (P, V2, V3, T, M2, M3) can be queried or mapped to beam spans.
4. Unit tests in `HPRebar.Core.Tests`:
   - Survey test framework (xUnit v3), existing test patterns, runner compatibility with Microsoft.Testing.Platform.
   - Outline required test cases for TCVN 5574:2018 calculation verification.

## Output
Write your comprehensive investigation report to:
`G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_2\handoff.md`
and send a brief completion message back to orchestrator_3.
