# DISPATCH — reviewer_m4_live

## 2026-09-20T15:36:00Z
- **Role**: M4 Live Verification & UI Reviewer
- **Target**: Review live AutoCAD 2026 verification results, screenshots, modal dialog PrintWindow captures, sysvar safety, and log outputs.
- **Authoritative Requirements**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (specifically '## Follow-up — 2026-09-20T12:39:24Z').
- **Test Readiness Specification**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md`.
- **Live Verification Summary**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\output\geolink-verify\summary.json`.
- **Worker Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m4_fix\handoff.md`.

## 2026-09-20T15:35:30Z
You are reviewer_m4_live, an objective and adversarial reviewer for Milestone M4.
Your parent orchestrator is orchestrator_3 (Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m4_live\

MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically '## Follow-up — 2026-09-20T12:39:24Z').
Read the master project document at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md.
Read the test readiness document at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Read the worker's handoff report at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m4_fix\handoff.md.
Read the live verification summary at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\output\geolink-verify\summary.json.
Read your dispatch at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m4_live\DISPATCH.md.

Review Scope:
1. Live Verification Coverage: Inspect `HPAutoCad/output/geolink-verify/summary.json`. Verify that all 45 checks across Tiers 1-4 passed with 0 failures.
2. Visual Evidence Audit: Inspect visual screenshots in `HPAutoCad/output/geolink-verify/`:
   - `ribbon-tab.png` and `ribbon-tab-theme1.png`: Verify shared tab HPAUTOCAD_MCP_TAB with MCP and HPGeoLink panels.
   - `dialog-dark.png` and `dialog-light.png`: Verify HPGEO export dialog rendered cleanly under both COLORTHEMEs.
   - `dialog-import.png`: Verify HPGEOIMPORT dialog rendered cleanly.
   - `image-in-autocad.png`: Verify satellite raster is inserted under the boundary ring in AutoCAD model space.
3. System Safety & Logs: Check `HPAutoCad/output/geolink-verify/` logs: verify zero `[ERR]` lines in HPGeoLink logs, and confirm that profile system variables (FILEDIA, DYNMODE, OSMODE, CMDECHO, LOGFILEPATH, COLORTHEME) were restored.
4. Issue your verdict: `APPROVE` or `REQUEST_CHANGES` with concrete evidence.

Output Requirements:
- Write your complete review report to: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m4_live\handoff.md with your explicit Verdict.
- Maintain progress.md in your working directory.
- Send a message back to parent (050984c1-afaa-4911-859c-331e9279dc4f) when done using send_message.
