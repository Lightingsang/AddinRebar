# BRIEFING — 2026-10-07T22:40:00Z

## Mission
Survey Focus Area 2: Investigate calculation and engineering domain logic in HPRebar.Core and HPRebar.Core.Tests, formulate TCVN 5574:2018 algorithms for beam flexure and shear, optimal rebar selection and clear spacing, internal force import mechanisms (Excel/ClosedXML and ETABS 22), and xUnit v3 test patterns.

## 🔒 My Identity
- Archetype: explorer
- Roles: explorer, synthesis
- Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_2\
- Original parent: 4db7c650-146a-4a05-a1e1-c0fb57e2733b
- Milestone: survey_focus_area_2

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Strictly NO reference to `Autodesk.Revit.*` in `HPRebar.Core`
- Clean Code Governance: classes <= 300 lines, methods <= 50 lines, 0 empty catch, 0 dead code
- Produce self-contained 5-component handoff report

## Current Parent
- Conversation ID: 4db7c650-146a-4a05-a1e1-c0fb57e2733b
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `HPRebar/HPRebar.Core/` (models, calculators, shared, csproj)
  - `HPRebar/HPRebar.Core.Tests/` (1530 tests passed in 3.6s, test patterns, test data fixtures)
  - `HPRebar/HPRebar/` (BeamRebar view models, sessions, services, RebarCreationService, ClosedXML package)
  - `HPEtabs/` (OAPI FrameForce in get_frame_forces, pipe `hpetabs-mcp-22`)
  - `HPBim/src/HPBim.Core/Tcvn/` (referenced for material parameters)
- **Key findings**:
  - `HPRebar.Core` currently has 0 structural design/resistance engines; it only has geometric layout calculators.
  - Formulated full TCVN 5574:2018 flexural (singly/doubly reinforced, $\xi_R, \alpha_R$) and shear ($Q \le Q_{b,max}$, $Q \le Q_b + Q_{sw}$, $s_{max}$, constructive spacing) math.
  - Formulated optimal rebar selection and clear spacing verification $t \ge \max(25, d)$.
  - Proposed pure domain force envelope models in `HPRebar.Core` and outer service adapters in `HPRebar` for ClosedXML and HPEtabs.
- **Unexplored areas**: None within Survey Focus Area 2 scope.

## Key Decisions Made
- Confirmed that `HPRebar.Core` must remain pure BCL without referencing `ClosedXML` or Revit API.
- Standardized force table schema into `BeamForceEnvelope` domain model.
- Completed comprehensive 5-component handoff report in `handoff.md`.

## Artifact Index
- DISPATCH.md — task instructions
- BRIEFING.md — persistent working memory
- progress.md — liveness heartbeat
- handoff.md — final 5-component handoff report
