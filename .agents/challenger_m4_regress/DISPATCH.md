# DISPATCH — challenger_m4_regress

## 2026-09-20T15:36:00Z
- **Role**: M4 Build, Regression & ALC Isolation Challenger
- **Target**: Empirically verify Debug and Release builds, execute all test suites, verify ALC isolation and no Default ALC pollution, verify regression suite (`run-bridge-unattended.ps1`), and verify Civil 3D mirror parity.
- **Authoritative Requirements**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (specifically '## Follow-up — 2026-09-20T12:39:24Z').
- **Test Readiness Specification**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md`.
- **Worker Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m4_fix\handoff.md`.

## 2026-09-20T15:36:30Z
User request received:
- Milestone M4 (Build & Regression Verification)
- Parent orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
- Scope:
  1. Build Verification: `dotnet build HPAutoCad/HPAutoCad.slnx -c Release` (0 errors, 0 warnings across all 11 projects).
  2. Full Test Suite Execution:
     - `dotnet test HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj -c Release`
     - `dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj -c Release` (assert 60/60 mirror pass)
     - `dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj -c Release` (assert 280/280 pass)
  3. ALC Isolation Audit: Verify `HPAutoCad.Loader.dll` has no static references to `HPAutoCad.dll`, `WebView2`, or `MaterialDesignThemes`.
  4. Bundle Deployment Verification: Verify `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\` directory structure and files.
  5. Issue verdict: `APPROVE` or `REQUEST_CHANGES`.
