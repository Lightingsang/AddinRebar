# BRIEFING — 2026-09-07T15:47:00Z

## Mission
Investigate the target architecture of AddinRebar (`HPRebar`) to establish concrete blueprints and contracts for `Foundation Rebar`.

## 🔒 My Identity
- Archetype: explorer
- Roles: explorer, investigator, synthesizer
- Working directory: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_target_2
- Original parent: 2ff0ff8b-87f5-4c14-be1c-b37017b7f55d
- Milestone: Foundation Rebar Target Architecture Investigation

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Strictly examine target architecture of HPRebar (HPRebar.Core, HPRebar.Core.Tests, HPRebar/HPRebar)
- No code modifications in src/tests/etc.

## Current Parent
- Conversation ID: 2ff0ff8b-87f5-4c14-be1c-b37017b7f55d
- Updated: 2026-09-07T15:47:00Z

## Investigation State
- **Explored paths**:
  - `HPRebar/HPRebar.Core/` (`BeamRebar/`, `ColumnRebar/`, `.csproj`)
  - `HPRebar/HPRebar.Core.Tests/` (`BeamRebar/`, `ColumnRebar/`, `.csproj`)
  - `HPRebar/HPRebar/` (`Beam Rebar/`, `Column Rebar/`, `Application.cs`, `Resources/Themes/Theme.xaml`)
- **Key findings**:
  - `HPRebar.Core` is `netstandard2.0` with Polyfill, zero Revit references. Pure records, immutable structs, stateless static calculators in mm.
  - `HPRebar.Core.Tests` is `net8.0` with xUnit v3 and Microsoft.Testing.Platform runner, test factories with mm inputs.
  - Feature Folder Convention: `HPRebar/HPRebar/<Feature Name With Spaces>/` with PascalCase file-scoped namespaces (`HPRebar.FoundationRebar`), required subfolders `Models/`, `View/`, `View Models/`, and orchestrator holding `TransactionGroup`.
  - Application.cs registers PushButtons on tab `"HPRebar"`, panel `"Rebar"` using shared icon URIs.
  - Multi-version convention uses `#if REVIT2024_OR_GREATER` with comment `// Multi-version: <Topic>`.
- **Unexplored areas**: None regarding target architecture.

## Key Decisions Made
- Fully documented contracts and blueprints for `FoundationRebar` across Core, Tests, and Revit add-in layers.

## Artifact Index
- handoff.md — Comprehensive 5-component handoff report for Foundation Rebar target architecture
