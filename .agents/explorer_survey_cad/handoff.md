# Handoff Report: HPAutoCad Architecture & Civil 3D Mirror Survey

**Agent**: explorer_survey_cad  
**Type**: Hard Handoff (Task Complete)  
**Date**: 2026-09-20  
**Recipient**: orchestrator_3 (`050984c1-afaa-4911-859c-331e9279dc4f`)  
**Primary Deliverable**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\survey_hpautocad.md`

---

## 1. Observation

1. **Solution Structure**:
   - `HPAutoCad/HPAutoCad.slnx` currently contains: `HPAutoCad.McpBridge.Loader`, `HPAutoCad.McpBridge`, `HPAutoCad.Aec`, `HPAutoCad.Mcp.Server`, `HPAutoCad.Aec.Tests`, `HPAutoCad.Mcp.Server.Tests`, and links to `McpShared/` projects (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`).
   - `HPGeo/HPGeo.slnx` currently contains: `HPGeo.AutoCad.Loader`, `HPGeo.AutoCad`, `HPGeo.Core`, `HPGeo.TileFetch`, and `HPGeo.Tests`.
2. **Test Baselines (Directly Measured)**:
   - `dotnet run --project HPAutoCad/HPAutoCad.Aec.Tests/HPAutoCad.Aec.Tests.csproj`: 225 passed, 0 failed, duration 2.7s.
   - `dotnet run --project HPAutoCad/HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj`: 280 passed, 0 failed, duration 8.8s.
   - `dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj`: 60 passed, 0 failed, duration 0.5s.
   - `dotnet run --project HPGeo/HPGeo.Tests/HPGeo.Tests.csproj`: 158 passed, 3 skipped live spikes, duration 1.3s.
3. **Bundle & ALC Setup**:
   - `HPAutoCad.McpBridge.Loader` creates `BridgeLoadContext` (`AssemblyLoadContext("HPAutoCad.McpBridge")`) from `Contents\Bridge\HPAutoCad.McpBridge.dll`.
   - `HPGeo.AutoCad.Loader` creates `GeoLoadContext` (`AssemblyLoadContext("HPGeo.AutoCad")`) from `Contents\App\HPGeo.AutoCad.dll`.
   - Both loaders define `HostAssemblyPrefixes = ["Ac", "Ad", "Autodesk."]`, returning `null` so host types resolve from AutoCAD's Default context.
   - Both loaders register Ribbon tabs with `TabId = "HPAUTOCAD_MCP_TAB"` and `TabTitle = "HPAutoCad"`.
4. **Repacking & Native Assets**:
   - Both `HPAutoCad.McpBridge.csproj` and `HPGeo.AutoCad.csproj` implement target `RepackMaterialDesign` via `ILRepack 2.0.46`, merging `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, and `Microsoft.Xaml.Behaviors.dll` into the main add-in DLL.
   - `Microsoft.Web.WebView2` outputs `runtimes\win-x64\native\WebView2Loader.dll`. `AssemblyDependencyResolver.ResolveUnmanagedDllToPath` in `GeoLoadContext` resolves this path automatically from `.deps.json`.
5. **HPCivil3d Mirror Constraints**:
   - `HPCivil3d/tools/mirror-tokens.json` specifies 48 ordered string replacements, 24 mirrored files, 13 Civil-owned files, and 10 owned counterparts pinned by SHA-256.
   - `HPCivil3d.McpBridge.Tests/MirrorTests.cs:68` and `:87` explicitly scan only `"HPAutoCad.McpBridge"`, `"HPAutoCad.McpBridge.Loader"`, and `"HPAutoCad.Mcp.Server"`.

---

## 2. Logic Chain

1. *From Observation 5*: `MirrorTokenTable.SourceFiles` explicitly targets only `"HPAutoCad.McpBridge"`, `"HPAutoCad.McpBridge.Loader"`, and `"HPAutoCad.Mcp.Server"`.
   *Therefore*: Creating new projects (`HPAutoCad`, `HPAutoCad.Core`, `HPAutoCad.TileFetch`, `HPAutoCad.Tests`) within `HPAutoCad.slnx` will not trigger `Every_AutoCAD_bridge_source_file_is_either_mirrored_or_an_owned_counterpart`.
2. *From Observation 5*: The mirror comparison engine has `stripCivilOnlyBlocks` (`// civil-only: begin ... // civil-only: end`) but no `autocad-only` mechanism.
   *Therefore*: Any modifications made directly to the 24 mirrored files in `HPAutoCad.McpBridge` or `HPAutoCad.McpBridge.Loader` will immediately break `MirrorTests` unless either mirrored in Civil 3D or covered by an existing token.
3. *From Observations 3 and 4*: `HPAutoCad.McpBridge` and `HPGeo.AutoCad` use separate subdirectories (`Contents\Bridge\` and `Contents\App\`) and repack `MaterialDesignThemes` into their respective assemblies.
   *Therefore*: Combining both into a single bundle `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\` with subdirectories `Contents\Bridge\` and `Contents\App\` maintains complete assembly and BAML isolation.
4. *From Observation 3*: Both loaders already share `TabId = "HPAUTOCAD_MCP_TAB"`.
   *Therefore*: Both panels ("MCP" and "HPGeoLink") can be hosted on the single "HPAutoCad" tab seamlessly.

---

## 3. Caveats

- **AutoCAD Process Locking**: When AutoCAD 2026 is open, it holds file locks on deployed DLLs in `%AppData%\Autodesk\ApplicationPlugins\`. Builds must use `-p:DeployBundle=false` when AutoCAD is running.
- **Elevation / Admin Rights**: Writing to `%AppData%\Autodesk\ApplicationPlugins\` does not require admin rights, but running unattended harnesses that invoke UI Automation (`run-bridge-unattended.ps1`) requires an active desktop session.
- **Unmanaged WebView2 Dependency**: Any bundle cleaner or build target must preserve the `runtimes\win-x64\native\` folder structure, or WebView2 initialization in AutoCAD will fail with `DllNotFoundException: WebView2Loader.dll`.

---

## 4. Conclusion

The architecture of `HPAutoCad` and `HPGeo` is verified and ready for unification:
1. Replicate the HPRebar architecture in `HPAutoCad.slnx` by introducing:
   - `HPAutoCad` (.NET 8.0-windows, AutoCAD 2026, WPF/MVVM) with feature folder `HPGeoLink/`
   - `HPAutoCad.Core` (.NET 8.0, host-free) with feature folder `HPGeoLink/`
   - `HPAutoCad.TileFetch` (.NET 8.0 console helper)
   - `HPAutoCad.Tests` (net10.0-windows, xUnit v3 / MTP)
2. Package into a single bundle `HPAutoCad.bundle` in `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.
3. Retain `HPAutoCad.McpBridge.Loader` and `HPAutoCad.McpBridge` intact to ensure 100% green Civil 3D mirror assertions (`HPCivil3d.McpBridge.Tests`).
4. Full details, diagrams, and code structures are documented in:
   `.agents/orchestrator_3/survey_hpautocad.md`.

---

## 5. Verification Method

To independently reproduce and verify all baseline findings:
1. `dotnet run --project HPAutoCad/HPAutoCad.Aec.Tests/HPAutoCad.Aec.Tests.csproj` -> Assert 225/225 pass.
2. `dotnet run --project HPAutoCad/HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj` -> Assert 280/280 pass.
3. `dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj` -> Assert 60/60 pass.
4. `dotnet run --project HPGeo/HPGeo.Tests/HPGeo.Tests.csproj` -> Assert 158 pass (3 live spikes skipped).
5. Inspect report at `.agents/orchestrator_3/survey_hpautocad.md`.
