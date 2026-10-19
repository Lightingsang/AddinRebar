# BRIEFING — 2026-10-18T02:53:00Z

## Mission
Investigate and produce the exact implementation specification for Feature 4: Complete purge of all 9 `kata_pro` occurrences in `HPKata/`, specify static analysis test, and verify zero regressions across all 926 existing tests.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigator, test strategist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_3
- Original parent: b578e01d-ac14-4013-9369-951a857a2c11
- Milestone: M1 (N1–N5 Remediation Investigation)
- [2026-10-18T02:47:26Z] Subagent: explorer_m1_3 (M1 Codebase Purge & Integrity Explorer)
- [2026-10-18T02:47:26Z] Roles: codebase purge investigator, static analysis test architect, regression verifier
- [2026-10-18T02:47:26Z] Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_3\
- [2026-10-18T02:47:26Z] Current parent: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- [2026-10-18T02:47:26Z] Milestone: M1 — Standalone C:\HPKata\ Environment & Asset Migration (Feature 4: Codebase Purge & Integrity)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement or modify production code
- McpShared is STRICTLY IMMUTABLE (0 edits)
- Produce handoff.md with 5-Component structure + Proposed Test Suite design with exact assertions
- [2026-10-18T02:47:26Z] Clean Code Governance: Every .cs file in HPKata/ <= 300 lines (Rule C2), every method <= 50 lines (Rule M3).
- [2026-10-18T02:47:26Z] Zero compiler warnings (TreatWarningsAsErrors=true), no commented-out code (Q-B1), no empty catch blocks (Q-B2).
- [2026-10-18T02:47:26Z] Single-Node Build Rule: Always run dotnet build and dotnet test with --disable-build-servers -m:1.
- [2026-10-18T02:47:26Z] Read-only investigation: provide exact diffs and test specifications, do not edit files in HPKata/ directly.

## Current Parent
- Conversation ID: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- Updated: 2026-10-18T02:53:00Z

## Investigation State
- **Explored paths**:
  - `HPKata/HPKata.AutoCad/Commands/SetupController.cs` (line 50)
  - `HPKata/HPKata.AutoCad/GoiLenh_HPKata.lsp` (lines 7 & 42)
  - `HPKata/HPKata.Core.Tests/Configuration/KataConfigParserTests.cs` (lines 11 & 83)
  - `HPKata/tools/run_m3_empirical_challenge_tests.ps1` (lines 221, 222, 247)
  - `HPKata/tools/run_m2_empirical_tests.ps1` (line 10)
  - `HPKata/HPKata.AutoCad.Tests/GoiLenhLspSyntaxTests.cs`
  - `HPKata/HPKata.E2E.Tests/Tier1_FeatureCoverage/Tier1_F10_AutoCadDetailingCommandsTests.cs`
  - Full codebase scan across all `.cs`, `.xaml`, `.lsp`, `.csproj`, `.xml`, `.addin`, `.ps1`
- **Key findings**:
  - Exactly 9 occurrences of `kata_pro` exist across 5 files in `HPKata/`.
  - Zero other occurrences exist in any `.xaml`, `.csproj`, `.xml`, `.addin` files.
  - Test baseline confirmed: exactly 926 tests currently exist and all 926 pass (0 failures, 0 skipped):
    - `HPKata.AutoCad.Tests`: 75 passed
    - `HPKata.Core.Tests`: 540 passed
    - `HPKata.E2E.Tests`: 160 passed
    - `HPKata.Revit.Tests`: 92 passed
    - `HPKata.UI.Tests`: 59 passed
  - Zero regressions: Replacing the 9 occurrences causes 0 test failures because tests do not assert `kata_pro` or `KATA_PRO` text, and `KataConfigParserTests` gracefully handles file existence checks.
  - Static analysis test `CodebasePurgeIntegrityTests` designed for `HPKata.Core.Tests` (and PowerShell check `verify_zero_kata_pro.ps1`), fully compliant with Clean Code Rules C2 (95 lines) and M3 (<= 18 lines/method).
- **Unexplored areas**: None. Scope fully exhausted.

## Key Decisions Made
- [2026-10-18T02:53:00Z] Formulated exact unified diffs for all 9 occurrences.
- [2026-10-18T02:53:00Z] Designed `CodebasePurgeIntegrityTests` for `HPKata.Core.Tests/Configuration/` to guarantee permanent CI enforcement of 0 `kata_pro` occurrences.
- [2026-10-18T02:53:00Z] Designed `verify_zero_kata_pro.ps1` for standalone / pre-commit verification.

## Artifact Index
- DISPATCH.md — Dispatch log
- BRIEFING.md — Persistent memory
- progress.md — Liveness & progress tracking
- handoff.md — Final investigation & specification report
