# Handoff Report: Milestone M3 Single Bundle Packaging & Deployment Plan

**Agent**: `explorer_m3_bundle`  
**Recipient**: `orchestrator_3` (Conversation ID: `050984c1-afaa-4911-859c-331e9279dc4f`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_bundle\`  
**Date**: 2026-09-20  

---

## 1. Observation
- `HPAutoCad/HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` declares `HPAutoCad.McpBridge` with `ModuleName="./Contents/HPAutoCad.McpBridge.Loader.dll"`, `OS="Win64"`, `Platform="AutoCAD"`, `SeriesMin="R25.1"`, and `SeriesMax="R25.1"`.
- In `HPCivil3d/tools/mirror-tokens.json` (lines 361–365), `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` is tracked in `ownedCounterparts` pinned to SHA-256 `22d566c8faec996572452d3fab99fdceaa0d89732f6f42b28df86f3602c1ef28`. Python SHA-256 computation over CRLF-normalized text yields verbatim `22d566c8faec996572452d3fab99fdceaa0d89732f6f42b28df86f3602c1ef28`.
- `HPCivil3d.McpBridge.Tests/MirrorTests.cs` verifies all 60 mirror tests. Running `dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests` passes 60/60 tests (duration: 323ms).
- `HPGeo/HPGeo.AutoCad.Loader/Bundle/PackageContents.xml` declares `HPGeo` with `ModuleName="./Contents/HPGeo.AutoCad.Loader.dll"`.
- In `HPAutoCad/HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj`, `<DeployBundle>` deploys to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\`.
- In `HPAutoCad/HPAutoCad/HPAutoCad.csproj`, `CopyTileFetchHelper` places `TileFetch/` in output, and `RepackMaterialDesign` merges `MaterialDesignThemes` directly into `HPAutoCad.dll`. `Microsoft.Web.WebView2` NuGet places `WebView2Loader.dll` in `runtimes/win-x64/native/`.
- Running `dotnet run --project HPAutoCad/HPAutoCad.Tests` passes 161 tests (158 succeeded, 3 live tests skipped without real tile endpoint).
- Running `dotnet run --project HPAutoCad/HPAutoCad.Mcp.Server.Tests` passes 280 tests (100%).

---

## 2. Logic Chain
1. AutoCAD Autoloader parses `PackageContents.xml` and discovers multiple `<ComponentEntry>` items inside `<Components>`. By declaring both `./Contents/HPAutoCad.McpBridge.Loader.dll` and `./Contents/HPAutoCad.Loader.dll`, AutoCAD loads both components into its Default ALC at startup.
2. Because `HPAutoCad.McpBridge.Loader` creates `BridgeLoadContext` pointing to `Contents/Bridge/HPAutoCad.McpBridge.dll`, and `HPAutoCad.Loader` creates `AppLoadContext` pointing to `Contents/App/HPAutoCad.dll`, the two execution domains remain completely isolated in memory.
3. Both main assemblies (`HPAutoCad.McpBridge.dll` and `HPAutoCad.dll`) merge `MaterialDesignThemes 5.3.2` via ILRepack. Consequently, no loose `MaterialDesignThemes.Wpf.dll` exists in either directory, completely avoiding cross-ALC WPF BAML binding collisions.
4. `HPCivil3d.McpBridge.Tests` strictly inspects `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, and `HPAutoCad.Mcp.Server`. By leaving `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` and `HPAutoCad.McpBridge.Loader.csproj` byte-identical, and placing the unified manifest in `HPAutoCad/HPAutoCad.Loader/Bundle/PackageContents.xml`, the Civil 3D mirror boundary remains 100% compliant.
5. Setting `<DeployBundle>false</DeployBundle>` for `HPAutoCad.McpBridge.Loader` in `HPAutoCad/Directory.Build.props` ensures building `HPAutoCad.slnx` does not deploy the legacy standalone bundle. Instead, `HPAutoCad.Loader` manages the atomic deployment of `HPAutoCad.bundle` and purges any legacy bundles (`HPAutoCad.McpBridge.bundle` and `HPGeo.bundle`).
6. Both loaders share the `HPAUTOCAD_MCP_TAB` ribbon tab contract, with idempotent creation semantics: whichever loads first creates the tab; the second adds its panel. Both panels dynamically rebuild on `COLORTHEME` flips and recover on `WSCURRENT` workspace switches.

---

## 3. Caveats
- `HPAutoCad.bundle` requires AutoCAD 2026 (`R25.1`). On machines running AutoCAD with `SECURELOAD=1` or `2`, the unattended verification harness (`run-bridge-unattended.ps1`) automatically clicks "Always Load" on the security dialog to trust the bundle permanently.
- Deployment target file-lock probe intentionally fails with MSB3061 if `acad.exe` is running and locking loaded DLLs. This is expected and desirable to prevent partial deployments.

---

## 4. Conclusion
The comprehensive packaging and deployment specification for `HPAutoCad.bundle` is formulated, verified against repository constraints, and written to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_bundle_plan.md`

All objectives from the dispatch are fulfilled:
1. Unified `PackageContents.xml` designed and specified.
2. Target filesystem layout under `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\` fully specified.
3. MSBuild `DeployBundle` target and build order graph planned.
4. Civil 3D mirror invariants strictly preserved with SHA-256 verification.

---

## 5. Verification Method
1. Inspect the generated plan:
   `view_file g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_bundle_plan.md`
2. Independently verify Civil 3D mirror tests pass:
   `dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests` (Result: 60/60 tests passed, 0 failed).
3. Independently verify geodetic and MCP server test suites:
   `dotnet run --project HPAutoCad/HPAutoCad.Tests` (Result: 161 tests passed/skipped).
   `dotnet run --project HPAutoCad/HPAutoCad.Mcp.Server.Tests` (Result: 280 tests passed).
4. Verify SHA-256 hash preservation:
   `python -c "import hashlib; text = open('HPAutoCad/HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml', 'rb').read().decode('utf-8-sig').replace('\r\n', '\n'); print(hashlib.sha256(text.encode('utf-8')).hexdigest())"`
   (Result matches pinned hash `22d566c8faec996572452d3fab99fdceaa0d89732f6f42b28df86f3602c1ef28`).
