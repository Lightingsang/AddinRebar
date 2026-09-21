# Milestone M3 Forensic Audit Report: Single Bundle Packaging, ALC Loader & Shared Ribbon Tab

**Work Product**: `HPAutoCad/HPAutoCad.Loader/`, `HPAutoCad/Directory.Build.props`, `HPAutoCad/HPAutoCad.slnx`, and `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`  
**Profile**: General Project (Development Mode)  
**Verdict**: **CLEAN**

---

## 1. Observation

### 1.1 Source Code and Architecture Inspection
1. **`HPAutoCad/HPAutoCad.Loader/AppLoadContext.cs`**:
   - Implements `internal sealed class AppLoadContext : AssemblyLoadContext` with non-collectible constructor `base(name: "HPAutoCad.App", isCollectible: false)`.
   - Utilizes `AssemblyDependencyResolver` initialized with `mainAssemblyPath` (`Contents\App\HPAutoCad.dll`).
   - Managed assembly probing respects host isolation: any assembly starting with prefixes `["Ac", "Ad", "Autodesk."]` returns `null` allowing AutoCAD Default ALC resolution to prevail.
   - Native unmanaged DLL resolution overrides `LoadUnmanagedDll`: first queries `_resolver.ResolveUnmanagedDllToPath(unmanagedDllName)`, and falls back to deterministic probing in `_appDirectory\runtimes\win-x64\native\` (e.g. for `WebView2Loader.dll`).
   - No dummy stubs, no fake returns, and no hardcoded assembly types.

2. **`HPAutoCad/HPAutoCad.Loader/HPGeoCommands.cs`**:
   - Contains authentic `[CommandMethod]` annotations registered across 8 AutoCAD commands:
     - `HPGEO` (Modal) -> `Invoke("dialog", "HPGEO")`
     - `HPGEODIALOG` (Modal) -> `Invoke("dialog", "HPGEODIALOG")`
     - `-HPGEOKMZ` (Modal) -> `Invoke("kmz-script", "-HPGEOKMZ")`
     - `HPGEOKMZ` (Modal) -> `Invoke("kmz-script", "HPGEOKMZ")`
     - `HPGEOIMPORT` (Modal) -> `Invoke("import", "HPGEOIMPORT")`
     - `-HPGEOIMPORT` (Modal) -> `Invoke("import-script", "-HPGEOIMPORT")`
     - `-HPGEOIMAGE` (Modal) -> `Invoke("image-script", "-HPGEOIMAGE")`
     - `HPGEOINFO` (Modal | NoUndoMarker) -> `Invoke("info", "HPGEOINFO")`
   - Dynamically retrieves delegates from `HPAutoCadLoaderApplication.App` (populated by `HPAutoCad.Entry.Start`), invokes them via `action.DynamicInvoke()`, handles `TargetInvocationException` unwrapping, logs to `LoaderLog`, and writes diagnostics to the active AutoCAD editor.

3. **`HPAutoCad/HPAutoCad.Loader/Ribbon/HPGeoLinkRibbonTab.cs` & `RibbonIcons.cs`**:
   - Interacts genuinely with `Autodesk.Windows` API (`ComponentManager.Ribbon`, `RibbonTab`, `RibbonPanel`, `RibbonButton`, `RibbonSplitButton`, `RibbonToolTip`).
   - Adheres to shared tab contract: targets tab `HPAUTOCAD_MCP_TAB` ("HPAutoCad"), creates or attaches panel `HPGEOLINK_PANEL` ("HPGeoLink") without disturbing sibling panels (such as `HPAUTOCAD_MCP_PANEL`).
   - Hooks `Application.SystemVariableChanged` for both `WSCURRENT` (workspace changes) and `COLORTHEME` (dark/light theme switching), automatically re-rendering vector glyphs on `Application.Idle`.
   - `RibbonIcons.cs` synthesizes crisp 32×32 resolution-independent vector icons using WPF `DrawingGroup` and `Geometry.Parse`, dynamically swapping ink color (`#E6E6E6` for dark `COLORTHEME=0`, `#3C3C3C` for light `COLORTHEME=1`) with HP Accent (`#0696D7`).

4. **`HPAutoCad/HPAutoCad.Loader/Bundle/PackageContents.xml` & Packaging**:
   - Declares dual components: `HPAutoCad.McpBridge` (`./Contents/HPAutoCad.McpBridge.Loader.dll`) and `HPAutoCad` (`./Contents/HPAutoCad.Loader.dll`).
   - Enforces `Platform="AutoCAD"` and `SeriesMin="R25.1" SeriesMax="R25.1"`.
   - `HPAutoCad.Loader.csproj` defines an automated `DeployBundle` target:
     - Enforces pre-flight checks ensuring payload assemblies exist.
     - Performs lock probe deletion on target DLLs to catch running CAD locks early.
     - Cleans legacy bundles (`HPAutoCad.McpBridge.bundle` and `HPGeo.bundle`) from `%AppData%\Autodesk\ApplicationPlugins\`.
     - Cleans stale target subdirectories `Contents\App` and `Contents\Bridge`.
     - Deploys manifest, loaders, and isolated `App/` and `Bridge/` folder structures.
   - `HPAutoCad/Directory.Build.props` selectively sets `<DeployBundle>false</DeployBundle>` for `HPAutoCad.McpBridge.Loader`, ensuring only the unified loader deploys the bundle without modifying `HPAutoCad.McpBridge.Loader.csproj` or breaking Civil 3D mirror parity.

### 1.2 Bundle Deployment Filesystem Audit
Empirically verified on disk at `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`:
- `PackageContents.xml` (valid XML manifest)
- `Contents\HPAutoCad.Loader.dll` & `HPAutoCad.Loader.pdb`
- `Contents\HPAutoCad.McpBridge.Loader.dll` & `HPAutoCad.McpBridge.Loader.pdb`
- `Contents\App\`:
  - `HPAutoCad.dll` (10,684,416 bytes — contains repacked `MaterialDesignThemes` 5.3.2)
  - `HPAutoCad.Core.dll` & `.pdb`
  - `HPAutoCad.deps.json` & `HPAutoCad.runtimeconfig.json`
  - `CommunityToolkit.Mvvm.dll`
  - `Microsoft.Web.WebView2.*.dll`
  - `runtimes\win-x64\native\WebView2Loader.dll`
  - `TileFetch\HPAutoCad.TileFetch.exe`
- `Contents\Bridge\`:
  - `HPAutoCad.McpBridge.dll` (with repacked `MaterialDesignThemes`)
  - `HPAutoCad.Aec.dll`
  - Roslyn compilers (`Microsoft.CodeAnalysis*.dll`)
  - `Serilog*.dll`, `System.Text.Json.dll`, `HPRebar.Mcp.Contracts.dll`, `HPRebar.McpBridge.Core.dll`
- Confirmed legacy bundles `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle` and `HPGeo.bundle` are absent (`False`).

### 1.3 Empirical Build and Test Execution
1. **Compilation**:
   - `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -m:1 -nodereuse:false`: Succeeded (0 errors, bundle deployed).
   - `dotnet build HPAutoCad/HPAutoCad.slnx -c Release -m:1 -nodereuse:false`: Succeeded (0 errors).
2. **Civil 3D Mirror Invariant**:
   - `dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests`:
     - Total: 60, Failed: 0, Succeeded: 60, Skipped: 0 (100% pass).
3. **Geodetic Unit Tests**:
   - `dotnet run --project HPAutoCad/HPAutoCad.Tests`:
     - Total: 161, Failed: 0, Succeeded: 158, Skipped: 3 (100% of offline suite passed; 3 network tests skipped as designed).
4. **AutoCAD MCP Server Tests**:
   - `dotnet run --project HPAutoCad/HPAutoCad.Mcp.Server.Tests`:
     - Total: 280, Failed: 0, Succeeded: 280, Skipped: 0 (100% pass).
5. **AEC Tool Engine Tests**:
   - `dotnet run --project HPAutoCad/HPAutoCad.Aec.Tests`:
     - Total: 225, Failed: 0, Succeeded: 225, Skipped: 0 (100% pass).

---

## 2. Logic Chain

1. **Isolation & Collision Prevention**: By hosting `HPAutoCad` inside `AppLoadContext` ("HPAutoCad.App") and `HPAutoCad.McpBridge` inside `BridgeLoadContext` ("HPAutoCad.McpBridge"), each payload resolves distinct dependency trees (e.g. WebView2 vs Roslyn) without polluting each other or the AutoCAD Default ALC.
2. **True Host Fallthrough**: By having `AppLoadContext.Load` return `null` for any assembly matching `["Ac", "Ad", "Autodesk."]`, AutoCAD API references are guaranteed to bind to the host process's already loaded native types, avoiding type-identity mismatches when passing parameters across ALC boundaries.
3. **Deterministic Native Resolution**: By providing explicit probing in `LoadUnmanagedDll` for `_appDirectory\runtimes\win-x64\native\WebView2Loader.dll`, WebView2 runtime initialization is safeguarded against arbitrary working directory mutations in AutoCAD.
4. **Mirror Test Stability**: Because `HPAutoCad.Loader` was authored as a separate project and `HPAutoCad.McpBridge.Loader` was preserved without modifying its mirrored source or csproj files (using `Directory.Build.props` for the MSBuild override), `HPCivil3d.McpBridge.Tests` passes 60/60 tests with zero drift.
5. **No Cheating Observed**: Code analysis confirmed absence of hardcoded outputs, empty method facades, bypassed packaging scripts, or muted test assertions. All deliverables implement genuine domain and integration logic.

---

## 3. Caveats

- In-process execution within a live AutoCAD 2026 GUI process (with ribbon UI clicks, dialog popup interactions, and named pipe communication) is scheduled for Milestone M4 via unattended MCP test harnesses.
- As previously documented, 3 tests in `HPAutoCad.Tests` (`Live_helper_fetches_four_real_tiles_...`, `Live_prefetch_...`, `Live_spike_...`) require `HPGEO_LIVE_TILES=1` and internet access, hence are skipped in offline builds by design.

---

## 4. Conclusion

**Verdict: CLEAN**

Milestone M3 work products (`HPAutoCad.Loader`, `PackageContents.xml`, `HPGeoLinkRibbonTab`, `RibbonIcons`, `AppLoadContext`, and bundle deployment) are authentic, robust, and completely satisfy all integrity requirements under Development Mode. No integrity violations, shortcuts, or facade implementations were found.

---

## 5. Verification Method

To independently reproduce this forensic audit:

1. **Inspect Deployed Files**:
   ```powershell
   Get-ChildItem -Recurse "$env:APPDATA\Autodesk\ApplicationPlugins\HPAutoCad.bundle"
   Test-Path "$env:APPDATA\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle" # Returns False
   Test-Path "$env:APPDATA\Autodesk\ApplicationPlugins\HPGeo.bundle"             # Returns False
   ```

2. **Clean Build**:
   ```powershell
   dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -m:1 -nodereuse:false
   dotnet build HPAutoCad/HPAutoCad.slnx -c Release -m:1 -nodereuse:false
   ```

3. **Execute Full Test Battery**:
   ```powershell
   dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests
   dotnet run --project HPAutoCad/HPAutoCad.Tests
   dotnet run --project HPAutoCad/HPAutoCad.Mcp.Server.Tests
   dotnet run --project HPAutoCad/HPAutoCad.Aec.Tests
   ```
