# BRIEFING — 2026-09-07T15:46:30Z

## Mission
Analyze and formulate the exact mathematical formulas, data structures, and edge-case behaviors for Foundation Rebar domain logic in HPRebar.Core/FoundationRebar/.

## 🔒 My Identity
- Archetype: explorer
- Roles: read-only mathematical & domain geometry explorer
- Working directory: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_geometry_2
- Original parent: 2ff0ff8b-87f5-4c14-be1c-b37017b7f55d
- Milestone: M1_Analysis

## 🔒 Key Constraints
- Read-only investigation — do NOT implement or modify source code
- Zero dependencies on Autodesk.Revit.* in HPRebar.Core
- Pure domain models and stateless calculators in netstandard2.0

## Current Parent
- Conversation ID: 2ff0ff8b-87f5-4c14-be1c-b37017b7f55d
- Updated: 2026-09-07T15:46:30Z

## Investigation State
- **Explored paths**: `HPRebar.Core/`, `HPRebar.Core.Tests/`, `BeamRebar`, `ColumnRebar`, `ORIGINAL_REQUEST.md`, `explorer_target_2`.
- **Key findings**:
  * Formulated complete orthonormal local coordinate frame ($\vec{u}_X, \vec{u}_Y, \vec{u}_Z$) supporting arbitrary rotation in the XY plane.
  * Derived exact 4-layer vertical elevations ensuring tangential physical stacking without collision or gap.
  * Formulated bar distribution mathematics for EqualSpacing, FixedSpacingCentered, and FixedSpacingFromStart (`floor(D/s)+1`).
  * Formulated 3D polyline generation for straight and 90° hooked bars (upward for bottom mat, downward for top mat).
  * Cataloged all edge-case guardrails (insufficient slab thickness, non-positive spacing, excessive side covers, short curves).
- **Unexplored areas**: None within domain geometry scope.

## Key Decisions Made
- Use right-hand orthonormal coordinate system with $P_0$ at bottom face corner $(X_0, Y_0, Z_{bot})$, $\vec{u}_Y = \vec{u}_Z \times \vec{u}_X$.
- Support both `EqualSpacing` and `FixedSpacingCentered` distribution algorithms.
- Enforce strict thickness validation: $H \ge c_{bot} + c_{top} + \sum d_{active}$.

## Artifact Index
- `F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_geometry_2\handoff.md` — Full 5-component mathematical derivation & class contract design report.
- `F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_geometry_2\progress.md` — Liveness heartbeat.
- `F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_geometry_2\DISPATCH.md` — Dispatch log.
