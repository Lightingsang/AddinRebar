# DISPATCH — worker_m4_fix

## 2026-09-20T14:41:00Z
- **Role**: M4 Live Verification & Loader Fix Worker
- **Target**: Fix `HPAutoCad/HPAutoCad.Loader/HPAutoCadLoaderApplication.cs` reflection resolution, build & deploy `HPAutoCad.bundle`, execute `run-geolink-verify.ps1` live in AutoCAD 2026, verify 100% pass, and verify regression suites.
- **Authoritative Requirements**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (specifically '## Follow-up — 2026-09-20T12:39:24Z').
- **Test Readiness Specification**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md`.
- **Test Writer Escalation Report**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\test_writer_geolink\handoff.md`.

## 2026-09-20T14:42:00Z
Received full task invocation for Milestone M4 (Live Verification & Loader Fix):
- Fix reflection resolution defect in `HPAutoCad/HPAutoCad.Loader/HPAutoCadLoaderApplication.cs`
- Build Debug & Release, deploy `HPAutoCad.bundle`
- Run test suites (`HPAutoCad.Tests`, `HPCivil3d.McpBridge.Tests`)
- Run live verification `run-geolink-verify.ps1` in AutoCAD 2026, check `summary.json`
- Run regression checks `run-bridge-unattended.ps1`
- Handoff report and progress tracking.
