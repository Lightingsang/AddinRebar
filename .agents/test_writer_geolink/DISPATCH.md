# DISPATCH — test_writer_geolink

## 2026-09-20T14:13:00Z
- **Role**: E2E Test Suite Creator & Live Harness Developer
- **Target**: Create `HPAutoCad/tools/harness/run-geolink-verify.ps1`, execute it against live AutoCAD 2026, verify 100% pass across Tiers 1-4, and publish `TEST_READY.md`.
- **Authoritative Requirements**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (specifically '## Follow-up — 2026-09-20T12:39:24Z').
- **Test Infra Specification**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\TEST_INFRA.md`.

## 2026-09-20T14:10:40Z
You are test_writer_geolink, the test writer and live harness engineer for the E2E Testing Track and Milestone M4.
Your parent orchestrator is orchestrator_3 (Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\test_writer_geolink\

MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically '## Follow-up — 2026-09-20T12:39:24Z').
Read the master project document at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md.
Read the test infrastructure specification at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\TEST_INFRA.md.
Read your dispatch at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\test_writer_geolink\DISPATCH.md.

Objective:
1. Develop the unified live verification test harness: `HPAutoCad/tools/harness/run-geolink-verify.ps1`.
   - Study `HPGeo/tools/acceptance.ps1`, `HPGeo/tools/dialog-check.ps1`, and `HPAutoCad/tools/harness/run-ribbon-check.ps1` as proven reference implementations.
   - Adapt the harness to target the unified bundle: `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.
   - Verify both loaders (`HPAutoCad.Loader.dll` and `HPAutoCad.McpBridge.Loader.dll`), checking logs in `%LocalAppData%\HPAutoCad\logs\` and `%LocalAppData%\HPGeo\logs\`.
   - Implement test cases covering all 4 tiers from TEST_INFRA.md:
     * Tier 1 (Feature Coverage): HPGEOINFO, -HPGEOKMZ, -HPGEOIMPORT, -HPGEOIMAGE, HPMCPBRIDGE, Shared Ribbon Tab HPAUTOCAD_MCP_TAB with panels HPAUTOCAD_MCP_PANEL and HPGEOLINK_PANEL.
     * Tier 2 (Boundary & Corner Cases): NO_INPUT on empty drawing, OUTSIDE_VIETNAM on wrong zone (cm=120), IMPLAUSIBLE_EN on unit=mm, unknown argument refusal, missing file refusal, OUTSIDE_ZONE on import, TOO_MANY_TILES refusal on oversized margin (margin=2000), NO_BOUNDARY on empty layer, wide view (area=100) automatic zoom reduction to fit 4096px cap, arc & mirrored polyline WCS flattening to 5mm tolerance.
     * Tier 3 (Cross-Feature Combinations): KMZ export -> re-import roundtrip vertex delta <= 2e-8 deg (~2 mm) with 13 exact hidden points preserved, satellite imagery under exported polyline ring + undo (`_.U`) cleanly removing RasterImage, stored settings in DWG (SAVEAS, QSAVE, CLOSE, OPEN, verify zone persistence and entities).
     * Tier 4 (Real-World Cadastral & Modal UI Scenarios): Modal dialog unattended off-screen PrintWindow validation for HPGEO (dark & light COLORTHEME captures) and HPGEOIMPORT, UIA button click for satellite image insertion, WSCURRENT workspace switch & COLORTHEME dynamic rebuild.
   - Emit machine-readable `summary.json` under `HPAutoCad/output/geolink-verify/summary.json` reporting exact PASS/FAIL counts, and exit code 0 on complete pass.
2. Execute `run-geolink-verify.ps1` against live AutoCAD 2026.
   - Ensure SECURELOAD prompt is answered "Always Load".
   - Ensure user profile settings (FILEDIA, DYNMODE, OSMODE, COLORTHEME, LOGFILEPATH, CMDECHO) are safely restored both cleanly and in finally/registry.
   - Verify that all assertions pass 100%.
3. Publish `TEST_READY.md` at project root (`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md`) per the format in PROJECT.md:
   - Runner command: `pwsh HPAutoCad/tools/harness/run-geolink-verify.ps1`
   - Coverage summary across Tiers 1-4
   - Feature checklist.
4. Deliver your complete report to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\test_writer_geolink\handoff.md`.
5. Maintain `progress.md` in your working directory.
6. Send a message back to parent (050984c1-afaa-4911-859c-331e9279dc4f) when done using send_message.
