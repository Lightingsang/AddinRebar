# BRIEFING — 2026-09-07T07:36:00Z

## Mission
Design the comprehensive unit test specifications for verifying HPRebar.Core/BeamRebar/ under xUnit v3 (fixtures, 4-tier test cases, boundary conditions, edge cases, assertion precision).

## 🔒 My Identity
- Archetype: spec_miner
- Roles: M1 Test Suite Specification Miner
- Working directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_m1_3
- Original parent: e303874c-1ef4-4fd0-9596-71bbccff874a
- Milestone: M1 — Beam Rebar Core Mathematics & Data Model

## 🔒 Key Constraints
- Read-only on production source code (do NOT implement anything, specification miner role).
- Design xUnit v3 unit test specifications targeting HPRebar.Core.Tests for HPRebar.Core/BeamRebar/.
- 4-Tier test cases: Tier 1 Feature Coverage, Tier 2 Boundary/Corner, Tier 3 Combinations, Tier 4 Real-world structural framing.
- Match ColumnRebar test patterns (TestBeamData builders, exact assert criteria, tolerance/precision, 100% pass rate under dotnet test HPRebar.Core.Tests).
- Deliver test_spec_plan.md and handoff.md in working directory.

## Current Parent
- Conversation ID: e303874c-1ef4-4fd0-9596-71bbccff874a
- Updated: not yet

## Task Summary
- **What to build**: Test specification plan for BeamRebar in HPRebar.Core.Tests
- **Success criteria**: Comprehensive test plan covering 4 tiers, data fixtures, exact assertion criteria, edge cases, 100% test pass target.
- **Interface contracts**: PROJECT.md, ORIGINAL_REQUEST.md, survey_source_analysis.md
- **Code layout**: HPRebar.Core/BeamRebar/, HPRebar.Core.Tests/BeamRebar/

## Key Decisions Made
- Designed 94 unit tests across 6 calculator suites: Stirrup Distribution (18), Main Longitudinal Bars (20), Additional Reinforcement (16), Side/Skin Bars (14), Special Hanging Bars (12), Canvas Transform (14).
- Structured shared test fixtures in `TestBeamData.cs` matching `ColumnRebar.TestSections` pattern with fluent builders.
- Enforced strict 4-tier classification: Tier 1 Feature Coverage, Tier 2 Boundary/Corner, Tier 3 Realistic Combinations, Tier 4 Real-world framing cases.
- Configured exact assertion standards: `Precision = 6` (1.0e-6 mm), exact integer counts, planar polyline Y checks, and explicit exception assertions.

## Artifact Index
- `test_spec_plan.md` — Comprehensive unit test specification plan for HPRebar.Core/BeamRebar/
- `handoff.md` — 5-component handoff report
