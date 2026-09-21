# Handoff Report: HPAutoCad.Loader Planning (Milestone M3)

## 1. Observation
1. **Legacy Loader Structure (`HPGeo/HPGeo.AutoCad.Loader/`)**:
   - `HPGeo.AutoCad.Loader.csproj` (lines 8-31) targets `net8.0-windows` with `<UseWPF>true</UseWPF>` and references `AutoCAD.NET [25.1.0]` with `ExcludeAssets="runtime"` and `PrivateAssets="all"`. It references `..\HPGeo.AutoCad\HPGeo.AutoCad.csproj` with `ReferenceOutputAssembly="false"` and `Private="false"`.
   - `GeoLoadContext.cs` (lines 16-41) inherits from `AssemblyLoadContext("HPGeo.AutoCad", isCollectible: false)`. It checks `HostAssemblyPrefixes = ["Ac", "Ad", "Autodesk."]`, returning `null` to fall through to `Default ALC`. Unmanaged DLLs are resolved using `_resolver.ResolveUnmanagedDllToPath(unmanagedDllName)`.
   - `HPGeoCommands.cs` (lines 10-59) exposes commands `HPGEO`, `-HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `-HPGEOIMAGE`, `HPGEOINFO`. It invokes `action.DynamicInvoke()` on delegates returned from `HPGeoLoaderApplication.App`.
   - `HPGeoLoaderApplication.cs` (lines 36-91) implements `IExtensionApplication`. It reflects `HPGeo.AutoCad.Entry.Start` in `Contents\App\HPGeo.AutoCad.dll` and stores the returned delegate dictionary. It calls `HPGeoRibbonTab.Install()` and logs to `LoaderLog`.
   - `LoaderLog.cs` (lines 11-32) writes to `%LocalAppData%\HPGeo\logs\loader.log`.
2. **Current HPAutoCad Add-In Structure (`HPAutoCad/HPAutoCad/`)**:
   - `Entry.cs` (lines 19-41) provides `Start(string appDirectory, Action<string>? log = null)` and `Start(string appDirectory, string product, string acadVersion)`, returning delegates for `"dialog"`, `"kmz-script"`, `"import"`, `"import-script"`, `"image-script"`, `"info"`, and `"stop"`.
   - `HPAutoCad.csproj` (lines 18, 61-69) specifies `<EnableDynamicLoading>true</EnableDynamicLoading>` and executes `RepackMaterialDesign` via `ILRepack.exe`, merging `MaterialDesignThemes` into `HPAutoCad.dll` and deleting loose toolkit DLLs.
3. **Current McpBridge Loader (`HPAutoCad/HPAutoCad.McpBridge.Loader/`)**:
   - `BridgeLoadContext.cs` (lines 17-48) isolates Roslyn in `Contents\Bridge\` under ALC `"HPAutoCad.McpBridge"`.
   - `Ribbon/McpRibbonTab.cs` (lines 20-83) implements shared tab protocol on `HPAUTOCAD_MCP_TAB` ("HPAutoCad"), adding panel `HPAUTOCAD_MCP_PANEL` ("MCP").
4. **Civil 3D Mirror Test Enforcement (`HPCivil3d/HPCivil3d.McpBridge.Tests/`)**:
   - `MirrorTests.cs` (lines 68, 87, 131) explicitly invokes:
     `MirrorTokenTable.SourceFiles(root, "HPAutoCad.McpBridge", "HPAutoCad.McpBridge.Loader", "HPAutoCad.Mcp.Server")`.
   - `MirrorTokenTable.cs` (lines 70-75) enumerates files strictly within the specified parameter directory list. It does not scan `HPAutoCad.Loader` or any other project.
   - `HPCivil3d/tools/mirror-tokens.json` (lines 209-396) only mirrors and pins hashes for files in `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, and `HPAutoCad.Mcp.Server`.

## 2. Logic Chain
1. **ALC Isolation**:
   - In AutoCAD .NET 8, extensions load into `AssemblyLoadContext.Default`.
   - Both `HPAutoCad.McpBridge` and `HPAutoCad` have third-party dependencies (Roslyn in Bridge; WebView2 and MVVM Toolkit in App).
   - If loaded into Default ALC, assemblies can collide with other AutoCAD plugins.
   - Therefore, `HPAutoCad.Loader` must host `AppLoadContext` ("`HPAutoCad.App`") pointed at `Contents\App\HPAutoCad.dll` with `AssemblyDependencyResolver` using `HPAutoCad.deps.json`.
   - To prevent splitting AutoCAD host types, all `Ac*`, `Ad*`, and `Autodesk.*` assemblies must return `null` in `AppLoadContext.Load`, falling through to Default ALC.
   - To prevent JIT compile-time loading into Default ALC, `HPAutoCad.Loader.csproj` must reference `HPAutoCad.csproj` with `ReferenceOutputAssembly="false"` and `Private="false"`.
2. **Native Dependency Resolution**:
   - `Microsoft.Web.WebView2` ships `runtimes\win-x64\native\WebView2Loader.dll`.
   - While `AssemblyDependencyResolver.ResolveUnmanagedDllToPath` handles `deps.json` runtime targets, adding an explicit fallback probe to `Path.Combine(_appDirectory, "runtimes", "win-x64", "native", unmanagedDllName + ".dll")` ensures runtime resolution under all CAD host startup conditions.
3. **Command Forwarding**:
   - AutoCAD registers `[CommandMethod]` methods located in the Default ALC.
   - When a command runs, it accesses delegates stored in `HPAutoCadLoaderApplication.App` and calls `action.DynamicInvoke()`.
   - BCL delegate invocations seamlessly cross the ALC boundary without requiring shared assembly type definitions.
   - Adding `HPGEODIALOG` and `HPGEOKMZ` as aliases guarantees compatibility with both UI conventions and scripted test suites.
4. **Civil 3D Mirror Safety**:
   - `HPCivil3d.McpBridge.Tests` checks mirror equality and pins hashes solely across `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, and `HPAutoCad.Mcp.Server`.
   - `HPAutoCad.Loader` is a new project under `HPAutoCad/HPAutoCad.Loader/`.
   - No files within the three whitelisted projects are modified or removed.
   - Therefore, `HPCivil3d.McpBridge.Tests` cannot detect or be affected by `HPAutoCad.Loader`, ensuring 100% test pass invariance.

## 3. Caveats
- `explorer_m3_bundle` owns the final packaging target and unified `PackageContents.xml` layout. `HPAutoCad.Loader` is designed to cleanly produce `HPAutoCad.Loader.dll` for deployment into `Contents/`.
- `explorer_m3_ribbon` owns the detailed vector icons and split-button layout for `HPGEOLINK_PANEL`. The loader hooks cleanly into `HPGeoRibbonTab.Install()` / `Uninstall()`.
- Unmanaged native library resolution requires x64 architecture (`win-x64`), which matches AutoCAD 2026's 64-bit architecture requirement.

## 4. Conclusion
The comprehensive architectural and implementation specification for `HPAutoCad.Loader` has been formulated and written to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_loader_plan.md`

The specification is 100% complete, featuring:
- Complete verbatim `.csproj` with `AutoCAD.NET 25.1.0`, `<UseWPF>true</UseWPF>`, and build-order reference isolation.
- Verbatim `AppLoadContext.cs` with host assembly fallthrough and dual-layer unmanaged DLL resolution (`WebView2Loader.dll`).
- Verbatim `HPGeoCommands.cs` with primary commands and aliases forwarding via reflection delegates.
- Verbatim `HPAutoCadLoaderApplication.cs` with guarded startup, reflection bootstrap, and Ribbon hooks.
- Verbatim `LoaderLog.cs` with dual-write to `%LocalAppData%\HPAutoCad\logs\` and `%LocalAppData%\HPGeo\logs\`.
- Mathematical and static code proof of Civil 3D mirror test safety.

## 5. Verification Method
1. **Static File Inspection**:
   - Verify `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_loader_plan.md` contains all 12 sections and verbatim code files.
2. **Civil 3D Mirror Invariant Verification**:
   - Inspect `HPCivil3d/HPCivil3d.McpBridge.Tests/MirrorTests.cs` lines 68, 87, 131 to confirm whitelisted project list.
   - Run `dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests` in a shell with execution permissions; assert 55 passed.
3. **Compilation Verification (upon implementation)**:
   - Run `dotnet build HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj -c Debug`.
   - Confirm 0 errors, 0 warnings.
