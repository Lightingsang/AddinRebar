# BRIEFING — 2026-09-07T07:25:27Z

## Mission
Investigate and specify Revit 2025/2026 API requirements, contracts, unit conversions, geometry extraction, and anti-patterns for continuous beam rebar generation in HPRebar.

## 🔒 My Identity
- Archetype: spec-miner
- Roles: Specification Miner, Revit API & Integration Specialist
- Working directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_revit_1
- Original parent: e303874c-1ef4-4fd0-9596-71bbccff874a
- Milestone: Continuous Beam Rebar Specification

## 🔒 Key Constraints
- Read-only on production source code: do NOT implement, only specify and document.
- Zero deprecated APIs: no DisplayUnitType/UnitType, ElementId.Value as long in Revit 2024+, UnitUtils with ForgeTypeId.
- Strict feature-folder convention: HPRebar/HPRebar/Beam Rebar/ with Models, View, View Models subfolders.
- HPRebar.Core has zero Autodesk.Revit.* dependencies.
- Atomic TransactionGroup management in BeamRebarOrchestrator.
- Output specification to revit_api_spec.md and handoff report to handoff.md.

## Current Parent
- Conversation ID: e303874c-1ef4-4fd0-9596-71bbccff874a
- Updated: 2026-09-07T07:25:27Z

## Task Summary
- **What to build**: Comprehensive Revit 2025/2026 API specification for continuous beam rebar generation (R02_BeamsRebar port to HPRebar).
- **Success criteria**: Exhaustive specification covering Rebar.CreateFromCurves/CreateFromRebarShape, geometry extraction, support detection, TransactionGroup safety, ViewSection/Dimension/Tag creation, Unit conversions, Edge Cases, and Features Discovered table.
- **Interface contracts**: revit_api_spec.md
- **Code layout**: AGENTS.md § Feature Folder Convention

## Key Decisions Made
- Use shape-driven Rebar.CreateFromCurves for longitudinal beam bars (top, bottom, additional, side) for parametric schedule and tag compatibility.
- Use Rebar.CreateFromRebarShape for stirrups and intermediate ties, using RebarShapeDrivenAccessor.SetLayoutAsNumberWithSpacing.
- Wrap all rebar/view generation in a single outer TransactionGroup("Beam Rebar") with inner named transactions and IFailuresPreprocessor.
- Enforce strict unit boundary: HPRebar.Core in mm, Revit integration layer converts via RevitUnits.

## Loaded Skills
- Source: revit-addin (f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\skills\revit-addin\SKILL.md)
  - Core methodology: Nice3point.Revit.Templates architecture, multi-version SDK, MVVM Toolkit.
- Source: revit-test (f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\skills\revit-test\SKILL.md)
  - Core methodology: Pure logic xUnit tests in Core without Revit dependencies; in-process integration testing.

## Artifact Index
- revit_api_spec.md — Authoritative Revit API specification for continuous beam rebar.
- handoff.md — 5-component handoff report for the orchestrator.
- progress.md — Liveness heartbeat.
