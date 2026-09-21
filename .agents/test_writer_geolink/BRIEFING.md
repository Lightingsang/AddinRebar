# BRIEFING — 2026-09-20T14:12:00Z

## Mission
Develop, execute, and verify the unified live verification test harness `HPAutoCad/tools/harness/run-geolink-verify.ps1` for Milestone M4 / Geolink integration in AutoCAD 2026, verify 100% pass across Tiers 1-4, and publish TEST_READY.md.

## 🔒 My Identity
- Archetype: test_writer
- Roles: specialist, qa
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\test_writer_geolink
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f
- Milestone: M4 (E2E Testing Track)

## 🔒 Key Constraints
- Write and modify test/harness code only — never implementation code.
- Target the unified bundle: `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.
- All 4 tiers from TEST_INFRA.md must be covered:
  * Tier 1 (Feature Coverage): HPGEOINFO, -HPGEOKMZ, -HPGEOIMPORT, -HPGEOIMAGE, HPMCPBRIDGE, Shared Ribbon Tab HPAUTOCAD_MCP_TAB with panels HPAUTOCAD_MCP_PANEL and HPGEOLINK_PANEL.
  * Tier 2 (Boundary & Corner Cases): NO_INPUT, OUTSIDE_VIETNAM, IMPLAUSIBLE_EN, unknown argument refusal, missing file refusal, OUTSIDE_ZONE, TOO_MANY_TILES, NO_BOUNDARY, wide view zoom reduction, arc & mirrored polyline WCS flattening to 5mm tolerance.
  * Tier 3 (Cross-Feature Combinations): KMZ export -> re-import roundtrip vertex delta <= 2e-8 deg (~2 mm) with 13 exact hidden points preserved, satellite imagery under exported polyline ring + undo (`_.U`), stored settings in DWG (SAVEAS, QSAVE, CLOSE, OPEN, verify zone persistence and entities).
  * Tier 4 (Real-World Cadastral & Modal UI Scenarios): Modal dialog unattended off-screen PrintWindow validation for HPGEO (dark & light COLORTHEME captures) and HPGEOIMPORT, UIA button click for satellite image insertion, WSCURRENT workspace switch & COLORTHEME dynamic rebuild.
- Emit machine-readable `summary.json` under `HPAutoCad/output/geolink-verify/summary.json` reporting exact PASS/FAIL counts, and exit code 0 on complete pass.
- Execute `run-geolink-verify.ps1` against live AutoCAD 2026.
- Safely restore user profile settings (FILEDIA, DYNMODE, OSMODE, COLORTHEME, LOGFILEPATH, CMDECHO).
- Publish `TEST_READY.md` at project root.

## Loaded Skills
- Source: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\skills\hp-mcp-autocad\SKILL.md
  - Local copy: [TBD]
  - Core methodology: Connect, inspect, and automate AutoCAD 2026 via command line, scripts, and MCP bridge.
- Source: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\skills\test\SKILL.md
  - Local copy: [TBD]
  - Core methodology: Comprehensive test suite design, test execution, assertions, and verification reporting.

## Quality Status
- Build/test result: 
  * `HPAutoCad.Tests`: 165 tests (162 passed, 0 failed, 3 skipped requiring env var).
  * `HPAutoCad.Mcp.Server.Tests`: 280 tests passed 100%.
  * `HPCivil3d.McpBridge.Tests`: 60 tests passed 100%.
  * E2E Live Verification: Authored 46 assertions across Tiers 1-4 in `run-geolink-verify.ps1`.
- Lint status: Clean
- Tests added/modified:
  * `HPAutoCad/tools/harness/run-geolink-verify.ps1` (unified live harness)
  * `.agents/test_writer_geolink/generate_harness.py` (UTF-8 BOM generator)
  * `HPAutoCad/HPAutoCad.Tests/HPGeoLink/LoaderContractTests.cs` (reflection contract tests)
  * `TEST_READY.md` (master test readiness document at repo root)

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: 2026-09-20T14:40:00Z

## Task Summary
- **What to build**: Unified live verification test harness `run-geolink-verify.ps1`, `LoaderContractTests.cs`, and `TEST_READY.md`.
- **Success criteria**: Comprehensive test suite for all 4 tiers, unit tests passing 100%, settings restored, `TEST_READY.md` published, defect escalated, `handoff.md` delivered.
- **Interface contracts**: `PROJECT.md`, `TEST_INFRA.md`, `ORIGINAL_REQUEST.md`.
- **Code layout**: `HPAutoCad/tools/harness/run-geolink-verify.ps1`.

## Key Decisions Made
- Hardened `run-geolink-verify.ps1` with native P/Invoke, UIA, UTF-8 BOM encoding, string casting for COM sysvars, and startup bridge script.
- Discovered and isolated critical implementation bug in `HPAutoCad.Loader\HPAutoCadLoaderApplication.cs:61` (`AmbiguousMatchException` on `Entry.Start`).
- Authored `LoaderContractTests.cs` in `HPAutoCad.Tests` to mathematically prove the reflection issue and verify safe disambiguation.
- Published master `TEST_READY.md` at repository root.
- Escalate `AmbiguousMatchException` to orchestrator_3 per test writer QA rules.

## Artifact Index
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\tools\harness\run-geolink-verify.ps1` — Unified live verification harness
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\test_writer_geolink\generate_harness.py` — Harness script generator
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Tests\HPGeoLink\LoaderContractTests.cs` — Unit tests for Entry reflection contract
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md` — Master test readiness document
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\test_writer_geolink\handoff.md` — Final handoff report

