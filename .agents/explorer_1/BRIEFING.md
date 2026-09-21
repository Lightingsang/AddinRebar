# BRIEFING — 2026-09-20T22:24:00Z

## Mission
Investigate Core Logic & Test Architecture for Smart Plot Pro in HPAutoCad (HPAutoCad.Core and HPAutoCad.Tests) and design complete specifications and architecture for downstream implementers.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_1
- Original parent: 5a9f631f-8834-48f7-a8a5-72b226a4b480
- Milestone: Smart Plot Pro Core Logic & Test Architecture Investigation

## 🔒 Key Constraints
- Read-only investigation — do NOT implement source code in project directories
- Strictly explore and document architecture, designs, test patterns, math, and code structures
- Output self-contained 5-component handoff report to `handoff.md`

## Current Parent
- Conversation ID: 5a9f631f-8834-48f7-a8a5-72b226a4b480
- Updated: 2026-09-20T22:25:50Z

## Investigation State
- **Explored paths**:
  - `HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj` (net8.0, 0 AutoCAD dependencies, InternalsVisibleTo HPAutoCad.Tests)
  - `HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj` (net10.0-windows, xUnit v3 3.1.0, MTP runner)
  - `HPAutoCad/HPAutoCad/HPAutoCad.csproj` (net8.0-windows, RepackMaterialDesign target)
  - `HPAutoCad/HPAutoCad.slnx` (Debug/Release configurations)
  - `HPAutoCad/HPGeoLink/` & `HPAutoCad.Aec/` architecture & settings patterns
- **Key findings**:
  - HPAutoCad.Core is pure net8.0 with zero references to AutoCAD, Nice3point, or WPF.
  - HPAutoCad.Tests runs xUnit v3 via Microsoft.Testing.Platform; currently 238 passing, 3 skipped, 0 failing.
  - Solution builds cleanly in Debug (0 errors).
  - Designed complete pure models (`PlotItem`, `PlotBounds`, `PlotConfiguration`, `PlotPreset`, `PlotPresetCollection`, `PlotResult`, Enums).
  - Designed `PlotOrderService` with tolerance overlap band row clustering algorithm.
  - Designed `LayoutRangeParser` with zero-exception guarantee and DoS protection.
  - Designed `FileNameService` with token replacement and invalid character sanitization.
  - Designed `PresetService` with path resolution, atomic write, and default preset generation.
- **Unexplored areas**: None within Core Logic & Test Architecture scope.

## Key Decisions Made
- `PlotBounds` designed as a dedicated readonly record struct with `VerticalOverlap` and `OverlapsVertically` methods to keep math clean and unit-testable.
- `PlotOrderService` clusters frames into rows using dynamic vertical overlap ratio (default 50%) rather than fixed Y deltas, ensuring resilience to mixed paper sizes (A1/A3) and slight drawing misalignments.
- `LayoutRangeParser` uses `int.TryParse`, clamped bounded loop (max 10,000 items), and returns distinct sorted 1-based indices, immune to all malformed inputs.
- `PresetService` takes optional `filePath` in constructor to allow isolated test fixtures without polluting user's `%AppData%`.
- All design files for Core will reside in `HPAutoCad.Core/SmartPlot/Models/` and `HPAutoCad.Core/SmartPlot/Services/`.
- All tests will reside in `HPAutoCad.Tests/SmartPlot/`.

## Artifact Index
- handoff.md — Comprehensive findings, detailed specifications, C# source skeletons, and test designs
- progress.md — Liveness heartbeat and status

