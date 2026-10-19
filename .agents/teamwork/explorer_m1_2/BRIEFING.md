# BRIEFING — 2026-10-18T02:55:00Z

## Mission
Investigate and design the exact implementation specification for Milestone 1 Features 2 & 3: Asset Staging from `C:\kata_pro\` to `HPKata/Distribution/Assets/` and `C:\HPKata\`, exact path rewriting rules for `Goi lenh.lsp`, `GoiLenh_HPKata.lsp`, and `menukata.cui` (0 kata_pro occurrences), and design of the automated staging script and C# staging service.

## 🔒 My Identity
- Archetype: Explorer
- Roles: Read-only investigation, evidence collection, proposed fix design
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_2\
- Original parent: b578e01d-ac14-4013-9369-951a857a2c11
- Milestone: Phase 1 Investigation (N3 & N4)
- Updated Identity: explorer_m1_2 (M1 Asset Staging & Path Rewriter Explorer)
- Current Milestone: M1 — Standalone C:\HPKata\ Environment & Asset Migration

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- McpShared is STRICTLY IMMUTABLE (0 edits)
- Produce comprehensive handoff.md with 5-component report + exact line numbers & proposed fix signatures
- Zero occurrences of `kata_pro` (case-insensitive) in target rewritten files and HPKata source
- Total line count of 238 lines and positions of 86 configuration parameters (lines 152–237) in `Goi lenh.lsp` and `GoiLenh_HPKata.lsp` MUST be strictly preserved
- Clean Code Governance: C# files <= 300 lines (C2), methods <= 50 lines (M3), zero warnings (TreatWarningsAsErrors=true)

## Current Parent
- Conversation ID: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- Updated: 2026-10-18T02:47:26Z

## Investigation State
- **Explored paths**:
  - `C:\kata_pro\` (root files, `dangthep\`, `Cot\`, `Icon\`, `Icon\Ribbon\`, `IMG\`, `Temp\`)
  - `HPKata/PROJECT.md`
  - `HPKata/HPKata.AutoCad/GoiLenh_HPKata.lsp`
  - `HPKata/HPKata.AutoCad/Commands/SetupController.cs`
  - `HPKata/HPKata.AutoCad.Tests/GoiLenhLspSyntaxTests.cs`
  - `HPKata/HPKata.Core/Configuration/KataConfigParser.cs`
  - `HPKata/HPKata.Core.Tests/Configuration/KataConfigParserTests.cs`
  - `HPKata/HPKata.AutoCad/Bundle/PackageContents.xml`
- **Key findings**:
  - Exactly 11 root files from `C:\kata_pro\` + 1 repo file (`GoiLenh_HPKata.lsp`) + 4 asset directories (`dangthep`: 162 WMFs, `Cot`: 13 WMFs, `Icon`: 133 files including `Ribbon\`, `IMG`: 37 PNGs, `Temp`: empty). Total = 357 files, 3.43 MB.
  - Exactly 6 occurrences of `kata_pro` in `C:\kata_pro\Goi lenh.lsp` (lines 5, 8, 13, 22, 25, 42).
  - Exactly 2 occurrences of `kata_pro` in `HPKata/HPKata.AutoCad/GoiLenh_HPKata.lsp` (lines 7, 42).
  - Exactly 14 occurrences of `kata_pro` in `C:\kata_pro\menukata.cui` (lines 40, 41, 51, 52, 62, 63, 73, 74, 84, 85, 95, 96, 106, 107).
  - `menukata.cuix` is a ZIP package with embedded relative icon references and has 0 `kata_pro` occurrences.
  - Rewriting preserves exact line counts (238 lines) and positions of 86 configuration parameters (lines 152–237) in `Goi lenh.lsp` and `GoiLenh_HPKata.lsp`.
- **Unexplored areas**: Feature 1 (`KataInstallEnvironment`) is being explored concurrently by `explorer_m1_1`.

## Key Decisions Made
- Staging architecture: Both repository staging `HPKata/Distribution/Assets/` and live runtime `C:\HPKata\` receive identical staged and rewritten assets.
- Staging exclusions: Obsolete legacy binaries (100 MB of `Kata_pro64*.dll`, GstarCad, ZwCad, etc.) are strictly excluded.
- Dual implementation: Designed both an unattended PowerShell staging tool (`HPKata/tools/stage_assets.ps1`) and a Clean Code C# service (`AssetStagingService.cs` / `KataPathRewriter.cs`) for `HPKata.Installer`.

## Artifact Index
- DISPATCH.md — incoming dispatch instructions and UTC history
- BRIEFING.md — persistent situational awareness and memory
- progress.md — liveness heartbeat
- handoff.md — comprehensive 5-component M1 Features 2 & 3 specification
