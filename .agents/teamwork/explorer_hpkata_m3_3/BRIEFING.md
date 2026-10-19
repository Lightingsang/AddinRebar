# BRIEFING — 2026-10-18T05:10:09Z

## Mission
Investigate Features 11 & 12 (Ribbon PNG Icons & Visual Diagram Binding) in HPKata, architecting KataIconProvider and visual asset bindings for HPKata.AutoCad, HPKata.Revit, and HPKata.UI with vector/drawing fallbacks, STA compatibility, and clean code compliance.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_3
- Original parent: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- Milestone: M3 (Features 11 & 12)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Solution: HPKata/HPKata.slnx (.NET 8.0, SDK 10.0.300)
- Single-Node Build Rule: dotnet build / test with --disable-build-servers -m:1
- UI Framework Constraint: Retain System.Windows.Window + MaterialDesignThemes v5.2.1. Do NOT switch to MahApps.Metro. Retain all Theme.xaml StaticResource keys. Ensure all 13 WPF windows instantiate on STA thread without throwing under both Dark and Light themes.
- Clean Code compliance:
  - File length <= 250 lines (Rule C2 <= 300)
  - Method length <= 40 lines (Rule M3 <= 50)
  - 0 unannotated catch blocks (Rule Q-B2)
  - Zero compiler warnings (TreatWarningsAsErrors=true), no commented-out code (Q-B1)

## Current Parent
- Conversation ID: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- Updated: 2026-10-18T05:10:09Z

## Investigation State
- **Explored paths**: [Initial orientation in ORIGINAL_REQUEST.md and PROJECT.md]
- **Key findings**: [TBD]
- **Unexplored areas**:
  - C:\HPKata\Icon\Ribbon\ (25 icons)
  - C:\HPKata\dangthep\ (101 WMF files)
  - C:\HPKata\Cot\ (4 WMF files)
  - C:\HPKata\IMG\ (3 PNG files)
  - HPKata.AutoCad CUIX / Ribbon tab button image loading
  - HPKata.Revit RibbonPanel button image loading
  - HPKata.UI 13 WPF windows image bindings and WMF handling
  - HPKata.Core / HPKata.UI KataIconProvider and VisualAssetService architecture
  - Test design for HPKata.UI.Tests and HPKata.E2E.Tests

## Key Decisions Made
- Starting systematic read-only investigation across asset files, host ribbon mechanisms, WPF image bindings, and test suites.

## Artifact Index
- DISPATCH.md — Dispatch specifications and instructions
- BRIEFING.md — Persistent situational awareness
- progress.md — Heartbeat and progress tracking
- handoff.md — Final 5-component handoff report
