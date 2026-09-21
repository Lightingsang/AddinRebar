# Dispatch for challenger_m3_mirror

## 2026-09-20T14:01:00Z
Task: Civil 3D Mirror Invariant Verification, AEC Regression Suite, and Git Cleanliness for Milestone M3.
Parent Orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md
Worker M3 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_bundle\handoff.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m3_mirror\
Verdict Target: handoff.md with APPROVE or REQUEST_CHANGES

Verification Focus:
1. Run `HPCivil3d.McpBridge.Tests`: verify all 60 tests pass 100%.
2. Verify Civil 3D mirror invariant against `HPCivil3d/tools/mirror-tokens.json`:
   - Confirm zero drift across all 24 mirrored files.
   - Confirm SHA-256 hash for `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` matches pinned hash `22d566c8faec996572452d3fab99fdceaa0d89732f6f42b28df86f3602c1ef28`.
3. Run `HPAutoCad.Aec.Tests`: verify all 225 tests pass 100%.
4. Check git status across protected boundaries (`McpShared`, `HPCivil3d`, `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, `HPAutoCad.Mcp.Server`). Confirm zero unintended modifications.
