# Milestone M3 Loader Review Report: ALC Isolation & Bundle Architecture

## 1. Observation

- **Deliverables Inspected**:
  1. `HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj`:
     - Lines 33-35: `<ProjectReference Include="..\HPAutoCad\HPAutoCad.csproj" ReferenceOutputAssembly="false" Private="false"/>` and `HPAutoCad.McpBridge.csproj` / `HPAutoCad.McpBridge.Loader.csproj`.
     - Lines 43-81: `DeployBundle` target deploys to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`, executes lock probing, purges legacy bundles, copies loaders to `Contents\`, and mirrors payload directories into isolated subfolders (`Contents\App\` and `Contents\Bridge\`).
     - Reflected assembly inspection of `bin\Debug\net8.0-windows\HPAutoCad.Loader.dll` reveals referenced assemblies: `System.Runtime`, `Acdbmgd`, `accoremgd`, `System.Runtime.Loader`, `AdWindows`, `System.ObjectModel`, `PresentationCore`, `WindowsBase`, `System.Linq`, `System.Threading`, `PresentationFramework`. **Zero references to `HPAutoCad`, `CommunityToolkit.Mvvm`, `WebView2`, `MaterialDesignThemes`, or `HPAutoCad.Core`**.
  2. `HPAutoCad/HPAutoCad.Loader/AppLoadContext.cs`:
     - Line 22: `public AppLoadContext(string mainAssemblyPath) : base(name: "HPAutoCad.App", isCollectible: false)`.
     - Lines 16, 35-38: Host assembly prefixes `["Ac", "Ad", "Autodesk."]` return `null` in `Load()`, correctly falling through to `AssemblyLoadContext.Default`.
     - Lines 51-74: `LoadUnmanagedDll()` resolves native DLLs first via `AssemblyDependencyResolver.ResolveUnmanagedDllToPath(unmanagedDllName)`, with fallback probing in `Path.Combine(_appDirectory, "runtimes", "win-x64", "native", ...)` handling both with/without `.dll` extension.
     - Confirmed `WebView2Loader.dll` exists in deployed bundle at `Contents\App\runtimes\win-x64\native\WebView2Loader.dll` with 163,680 bytes.
  3. `HPAutoCad/HPAutoCad.Loader/HPGeoCommands.cs` & `HPAutoCadLoaderApplication.cs`:
     - `HPAutoCadLoaderApplication.cs` line 13: `[assembly: CommandClass(typeof(HPGeoCommands))]`.
     - `HPAutoCadLoaderApplication.cs` line 12: `[assembly: ExtensionApplication(typeof(HPAutoCadLoaderApplication))]`.
     - Commands registered in `HPGeoCommands.cs`: `HPGEO` (line 14), `HPGEODIALOG` (line 17), `-HPGEOKMZ` (line 20), `HPGEOKMZ` (line 23), `HPGEOIMPORT` (line 26), `-HPGEOIMPORT` (line 29), `-HPGEOIMAGE` (line 32), `HPGEOINFO` (line 35).
     - Execution delegation: `Invoke(key, commandName)` retrieves delegate from `HPAutoCadLoaderApplication.App` (populated via reflection from `HPAutoCad.Entry.Start`) and executes `action.DynamicInvoke()`.
     - Exception handling (lines 70-78): `var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;` with diagnostic logging to `loader.log` and warning messages to `Application.DocumentManager.MdiActiveDocument.Editor`.
  4. `HPAutoCad/HPAutoCad.Loader/HPAutoCadLoaderApplication.cs`:
     - Implements `IExtensionApplication` (`Initialize()`, `Terminate()`).
     - `Initialize()` instantiates `AppLoadContext`, loads `Contents\App\HPAutoCad.dll`, invokes `HPAutoCad.Entry.Start`, registers delegates, and safely invokes `HPGeoLinkRibbonTab.Install()`.
     - `Terminate()` uninstalls Ribbon tab, calls `"stop"` delegate via `HPGeoCommands.Invoke("stop", null)`, and logs termination.
  5. `HPAutoCad/HPAutoCad.Loader/Ribbon/HPGeoLinkRibbonTab.cs`:
     - Mounts `HPGEOLINK_PANEL` onto shared tab `HPAUTOCAD_MCP_TAB` ("HPAutoCad").
     - Checks `tab.Panels.Count == 0` before removing tab on uninstall, preventing deletion of shared tab when `HPAutoCad.McpBridge` is co-loaded.
     - Subscribes to `COLORTHEME` and `WSCURRENT` system variable changes; switches vector ribbon icon ink (`RibbonIcons.cs`) dynamically.
  6. `HPAutoCad/HPAutoCad.Loader/Bundle/PackageContents.xml`:
     - Dual `ComponentEntry` nodes for `HPAutoCad.McpBridge.Loader.dll` and `HPAutoCad.Loader.dll`.
     - `SeriesMin="R25.1"` / `SeriesMax="R25.1"`, `Platform="AutoCAD"`.
  7. `HPAutoCad/Directory.Build.props`:
     - Sets `<DeployBundle>false</DeployBundle>` for `HPAutoCad.McpBridge.Loader` to prevent deployment of legacy bundle without modifying mirrored project files.

- **Build and Test Verification**:
  - `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug`: Succeeded (0 errors).
  - `dotnet build HPAutoCad/HPAutoCad.slnx -c Release`: Succeeded (0 errors).
  - `HPCivil3d.McpBridge.Tests`: 60 passed, 0 failed, 0 skipped (100% pass, mirror invariant fully preserved).
  - `HPAutoCad.Tests`: 158 passed, 0 failed, 3 skipped (live HTTP tile fetchers guarded by `HPGEO_LIVE_TILES=1`).
  - `HPAutoCad.Mcp.Server.Tests`: 280 passed, 0 failed, 0 skipped (100% pass).
  - `HPAutoCad.Aec.Tests`: 225 passed, 0 failed, 0 skipped (100% pass).

- **Deployed Filesystem Verification**:
  - `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\` contains:
    - `PackageContents.xml`
    - `Contents\HPAutoCad.Loader.dll`
    - `Contents\HPAutoCad.McpBridge.Loader.dll`
    - `Contents\App\` (with `HPAutoCad.dll` repacked with `MaterialDesignThemes`, `CommunityToolkit.Mvvm.dll`, `Microsoft.Web.WebView2.*.dll`, `runtimes\win-x64\native\WebView2Loader.dll`, `TileFetch\HPAutoCad.TileFetch.exe`)
    - `Contents\Bridge\` (`HPAutoCad.McpBridge.dll`, `HPAutoCad.Aec.dll`, Roslyn, Serilog)
  - Legacy bundles `HPAutoCad.McpBridge.bundle` and `HPGeo.bundle` were successfully deleted.

## 2. Logic Chain

1. Setting `<ReferenceOutputAssembly="false">` on project references inside `HPAutoCad.Loader.csproj` enforces build sequencing while generating zero compile-time references to `HPAutoCad.dll` or its third-party packages in `HPAutoCad.Loader.dll`. Inspection of the emitted assembly metadata confirmed only AutoCAD host assemblies (`Acdbmgd`, `accoremgd`, `AdWindows`) and .NET BCL are referenced, completely preventing dependency contamination in AutoCAD's Default ALC.
2. In `AppLoadContext.cs`, isolating third-party packages inside `Contents\App\` while returning `null` for `Ac*`, `Ad*`, and `Autodesk.*` ensures AutoCAD's core APIs bind uniquely from the host process Default ALC. The dual unmanaged resolution strategy (deps.json + probing `runtimes\win-x64\native\WebView2Loader.dll`) resolves `WebView2Loader.dll` deterministically.
3. Placing `[assembly: CommandClass(typeof(HPGeoCommands))]` and registering all 8 required commands with safe `Invoke` routing handles `TargetInvocationException` unwrapping, error logging to `loader.log`, and user reporting via the active editor without throwing unhandled exceptions to the AutoCAD process.
4. Implementing `IExtensionApplication` with independent try-catch blocks and event detachment (`SystemVariableChanged`, `ItemInitialized`, `Idle`) ensures robust lifecycle management.
5. In `HPGeoLinkRibbonTab.cs`, managing only `HPGEOLINK_PANEL` and preserving `HPAUTOCAD_MCP_TAB` when sibling panels exist satisfies the multi-add-in shared tab contract without order dependency.
6. Restricting changes to loader and build configuration while keeping `HPAutoCad.McpBridge` files untouched preserves the Civil 3D mirror invariant, confirmed by 60/60 passing tests in `HPCivil3d.McpBridge.Tests`.
7. No integrity violations (hardcoded test results, dummy facades, or task bypasses) were detected.

## 3. Caveats

- 3 tests in `HPAutoCad.Tests` (`Live_helper_fetches_four_real_tiles_...`, `Live_prefetch_...`, `Live_spike_...`) require live HTTP tile fetching and are skipped by design when `HPGEO_LIVE_TILES` is not set; all 158 offline unit tests passed.
- AutoCAD in-process interactive execution will be validated in Milestone M4 via the unattended MCP test harnesses.

## 4. Conclusion

**Verdict: APPROVE**

Milestone M3 satisfies all architectural, functional, isolation, and quality requirements. ALC loader separation is robust, command delegation is complete and exception-safe, the bundle packaging deploys accurately, and all static and mirror test suites pass 100%.

## 5. Verification Method

To independently verify this evaluation:
1. Compile solution:
   ```powershell
   dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
   dotnet build HPAutoCad/HPAutoCad.slnx -c Release
   ```
2. Verify Default ALC isolation via reflection:
   ```powershell
   [System.Reflection.Assembly]::LoadFrom("HPAutoCad\HPAutoCad.Loader\bin\Debug\net8.0-windows\HPAutoCad.Loader.dll").GetReferencedAssemblies() | Select-Object Name
   ```
3. Verify test suites:
   ```powershell
   dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests --no-build
   dotnet run --project HPAutoCad/HPAutoCad.Tests --no-build
   dotnet run --project HPAutoCad/HPAutoCad.Mcp.Server.Tests --no-build
   dotnet run --project HPAutoCad/HPAutoCad.Aec.Tests --no-build
   ```
4. Verify bundle deployment:
   ```powershell
   Test-Path "$env:APPDATA\Autodesk\ApplicationPlugins\HPAutoCad.bundle\PackageContents.xml"
   Test-Path "$env:APPDATA\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\HPAutoCad.Loader.dll"
   Test-Path "$env:APPDATA\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\App\HPAutoCad.dll"
   Test-Path "$env:APPDATA\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\App\runtimes\win-x64\native\WebView2Loader.dll"
   Test-Path "$env:APPDATA\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\Bridge\HPAutoCad.McpBridge.dll"
   ```
