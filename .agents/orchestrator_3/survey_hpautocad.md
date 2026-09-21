# Comprehensive Architectural Survey: HPAutoCad, ALC Loader, Ribbon, Bundle Structure & Civil 3D Mirror Constraints

**Author**: explorer_survey_cad  
**Date**: 2026-09-20  
**Target Milestone**: Migration of HPGeo into HPAutoCad as `HPGeoLink` mirroring HPRebar architecture  
**Recipient**: orchestrator_3 (`050984c1-afaa-4911-859c-331e9279dc4f`)  

---

## 1. Executive Summary

This report delivers a complete architectural map of `HPAutoCad/`, its bundle packaging, `AssemblyLoadContext` (ALC) loader isolation, Ribbon integration, MaterialDesign / WebView2 native dependency handling, and the mirror relationship with `HPCivil3d/`.

### Key Findings & Baseline Metrics
1. **Existing Test Suites Pass 100% (726 Total Tests)**:
   - `HPAutoCad.Aec.Tests`: **225/225 passed** (net10.0-windows, MTP runner, duration ~2.7s).
   - `HPAutoCad.Mcp.Server.Tests`: **280/280 passed** (net10.0-windows, MTP runner, seed compile checks, duration ~8.8s).
   - `HPCivil3d.McpBridge.Tests`: **60/60 passed** (net10.0, MTP runner, text mirror validation, duration ~0.5s).
   - `HPGeo.Tests`: **158 passed, 3 skipped live spikes = 161 total** (net10.0-windows, duration ~1.3s).
2. **ALC Isolation is Essential**: AutoCAD 2026 runs in a single process (`acad.exe`) using .NET 8.0. Plugins loaded into AutoCAD's Default ALC share dependencies. Different plugins using conflicting versions of `CommunityToolkit.Mvvm`, `Roslyn`, or `MaterialDesignThemes` cause fatal `FileLoadException` or BAML style binding collisions. Both `HPAutoCad.McpBridge` and `HPGeo.AutoCad` isolate their implementations into custom `AssemblyLoadContext` instances.
3. **Civil 3D Mirror Strict Fence**: `HPCivil3d/` mirrors `HPAutoCad/` via `HPCivil3d/tools/mirror-tokens.json` and `HPCivil3d.McpBridge.Tests/MirrorTests.cs`. It strictly scans `"HPAutoCad.McpBridge"`, `"HPAutoCad.McpBridge.Loader"`, and `"HPAutoCad.Mcp.Server"`. Any direct modification to mirrored files or hash-pinned files breaks the mirror tests unless handled under strict mirror protocols.
4. **Shared Ribbon Tab Pattern Already Proven**: Both `HPAutoCad.McpBridge.Loader` and `HPGeo.AutoCad.Loader` already share `TabId = "HPAUTOCAD_MCP_TAB"` and `TabTitle = "HPAutoCad"`. Whichever plugin loads first creates the tab; subsequent plugins find it by `TabId` and append their own panels (`MCP` and `VN2000`).
5. **Clear Path for HPRebar Architecture Adoption**: `HPAutoCad` can cleanly replicate `HPRebar` by introducing `HPAutoCad` (add-in UI, WPF/MVVM, .NET 8.0-windows) with feature folder `HPGeoLink/`, `HPAutoCad.Core` (host-free domain logic, .NET 8.0), `HPAutoCad.TileFetch` (out-of-process console helper), and `HPAutoCad.Tests` (net10.0-windows unit test suite).

---

## 2. Project Inventory & Solution Architecture (`HPAutoCad.slnx`)

The solution `HPAutoCad/HPAutoCad.slnx` uses XML-based `.slnx` format, pinning .NET SDK `10.0.300` and Microsoft Testing Platform (`global.json`).

### Constituent Projects

| Project | Target Framework | Dependencies | Role & Output |
|---|---|---|---|
| `HPAutoCad.McpBridge.Loader` | `net8.0-windows` | `AutoCAD.NET 25.1.0` (compile-time only), WPF | **What acad.exe loads directly.** Defines AutoCAD commands (`HPMCP*`), extension app (`IExtensionApplication`), creates `BridgeLoadContext`, starts bridge via reflection, builds Ribbon panel. Emits `HPAutoCad.McpBridge.Loader.dll`. |
| `HPAutoCad.McpBridge` | `net8.0-windows` | `AutoCAD.NET 25.1.0`, `CommunityToolkit.Mvvm 8.4.0`, `MaterialDesignThemes 5.3.2` (ILRepacked), `Serilog 4.4.0`, `HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPAutoCad.Aec` | **The isolated MCP bridge engine.** Runs inside `BridgeLoadContext` from `Contents\Bridge\`. Hosts `McpBridgeHost`, `MainThreadExecutor`, Roslyn `ScriptCompiler`, `AutocadBridgeStatusView`. `EnableDynamicLoading=true`. |
| `HPAutoCad.Aec` | `net8.0-windows` | `AutoCAD.NET 25.1.0`, `HPRebar.McpBridge.Core` | **The AEC tool engine.** Contains Cad adapters (layers, blocks, hatches, xrefs, change sets) and pure domain logic (Geometry, Spatial, Issues, Model, Classification, Relationships). |
| `HPAutoCad.Mcp.Server` | `net10.0` (Console) | `HPRebar.Mcp.Server.Core` | **The stdio MCP server executable.** Launched by host AI. Bridges stdio to named pipe `\\.\pipe\hpautocad-mcp-2026`. Embeds seed library in `Registry\SeedLibrary\**\*`. Zero AutoCAD API references. |
| `HPAutoCad.Aec.Tests` | `net10.0-windows` | `xunit.v3 3.1.0`, `HPAutoCad.Aec` | **AEC test suite.** 225 unit tests verifying geometry, spatial indexing, classification, change sets, and issue markup without running AutoCAD. |
| `HPAutoCad.Mcp.Server.Tests` | `net10.0-windows` | `xunit.v3 3.1.0`, `AutoCAD.NET 25.1.0`, `HPAutoCad.Mcp.Server` | **MCP server test suite.** 280 unit tests verifying registry lifecycle, seed schema validation, and compiling all seed C# scripts against AutoCAD API reference assemblies. |
| `McpShared/` (External References) | `netstandard2.0` / `net8.0` / `net10.0` | Shared contracts, bridge core, server core | Shared across Revit, AutoCAD, Navisworks, ETABS, and Civil 3D. Never modified directly for AutoCAD-specific features. |

---

## 3. Bundle Packaging & Runtime Layout

### Autodesk Autoloader Specification (`PackageContents.xml`)
AutoCAD automatically discovers and loads bundles placed in `%AppData%\Autodesk\ApplicationPlugins\` or `%ProgramData%\Autodesk\ApplicationPlugins\`.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0"
                    AutodeskProduct="AutoCAD"
                    ProductType="Application"
                    Name="HPAutoCad"
                    AppVersion="0.4.0"
                    Description="HPAutoCad Add-In: MCP AI Bridge and HPGeoLink Geodetic Toolkit"
                    ProductCode="{7B4E9C2D-5A61-4F0E-9D3B-2C8F1E6A7D54}">
  <CompanyDetails Name="HPRebar" />
  <Components Description="AutoCAD 2026 (.NET 8)">
    <RuntimeRequirements OS="Win64" Platform="AutoCAD" SeriesMin="R25.1" SeriesMax="R25.1" />
    <ComponentEntry AppName="HPAutoCad.McpBridge"
                    Version="0.3.0"
                    ModuleName="./Contents/HPAutoCad.McpBridge.Loader.dll"
                    AppType=".NET"
                    LoadOnAutoCADStartup="True" />
  </Components>
</ApplicationPackage>
```

#### Key Elements:
- `SeriesMin="R25.1"` and `SeriesMax="R25.1"`: Pinned to AutoCAD 2026 internal release (R25.1).
- `Platform="AutoCAD"`: **Crucial constraint.** Omitting the asterisk (`Platform="AutoCAD"`, not `"AutoCAD*"`) ensures Civil 3D and Advance Steel 2026 (which share `acad.exe` and `R25.1`) ignore this bundle, preventing pipe race conditions and coordinate system collisions.
- `ModuleName`: Path relative to the bundle root specifying the primary assembly AutoCAD loads into its default context.

### Proposed Bundle Runtime Layout (`HPAutoCad.bundle`)

Deploy target: `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`

```
HPAutoCad.bundle/
├── PackageContents.xml                                <-- Bundle manifest
└── Contents/
    ├── HPAutoCad.Loader.dll                           <-- Loader in AutoCAD Default ALC
    ├── HPAutoCad.Loader.pdb
    ├── Bridge/                                        <-- Isolated Bridge ALC ("HPAutoCad.McpBridge")
    │   ├── HPAutoCad.McpBridge.dll                   <-- Repacked (MD toolkit merged)
    │   ├── HPAutoCad.McpBridge.deps.json             <-- Read by AssemblyDependencyResolver
    │   ├── HPAutoCad.Aec.dll                         <-- Loose AEC engine
    │   ├── Microsoft.CodeAnalysis.dll                <-- Roslyn 5.9 private compiler
    │   ├── Microsoft.CodeAnalysis.CSharp.dll
    │   ├── CommunityToolkit.Mvvm.dll
    │   ├── Serilog.dll
    │   ├── Serilog.Sinks.File.dll
    │   ├── HPRebar.Mcp.Contracts.dll
    │   └── HPRebar.McpBridge.Core.dll
    └── App/                                           <-- Isolated Add-In ALC ("HPAutoCad.App")
        ├── HPAutoCad.dll                              <-- Repacked (MD toolkit merged, UI/MVVM)
        ├── HPAutoCad.deps.json                       <-- Read by AssemblyDependencyResolver
        ├── HPAutoCad.Core.dll                        <-- Pure geodetic/calculation library
        ├── Microsoft.Web.WebView2.Core.dll           <-- WebView2 WPF managed assemblies
        ├── Microsoft.Web.WebView2.Wpf.dll
        ├── CommunityToolkit.Mvvm.dll                 <-- Private copy in App ALC
        ├── runtimes/                                  <-- Native binaries resolved by deps.json
        │   └── win-x64/
        │       └── native/
        │           └── WebView2Loader.dll             <-- Native loader for WebView2
        └── TileFetch/                                 <-- Out-of-process helper directory
            ├── HPAutoCad.TileFetch.exe               <-- Tile download companion process
            ├── HPAutoCad.TileFetch.dll
            ├── HPAutoCad.TileFetch.deps.json
            └── HPAutoCad.TileFetch.runtimeconfig.json
```

---

## 4. ALC Loader & Dynamic Assembly Isolation Deep-Dive

### The ALC Boundary Mechanism

```
+-------------------------------------------------------------------------+
|                              acad.exe Process                           |
|                                                                         |
|  +-------------------------------------------------------------------+  |
|  |                         Default ALC                               |  |
|  |  - AutoCAD Runtime & WPF (AcMgd, AcCoreMgd, AcDbMgd, AdWindows)    |  |
|  |  - HPAutoCad.Loader.dll                                           |  |
|  |    * IExtensionApplication (Initialize / Terminate)               |  |
|  |    * CommandMethod handlers (HPMCP*, HPGEO*)                      |  |
|  |    * McpRibbonTab / HPGeoLinkRibbonTab                            |  |
|  +-------------------+---------------------------+-------------------+  |
|                      |                           |                      |
|         (Reflection) |              (Reflection) |                      |
|                      v                           v                      |
|  +-----------------------------------+   +---------------------------+  |
|  |    BridgeLoadContext (ALC 1)      |   |   AppLoadContext (ALC 2)  |  |
|  |  "HPAutoCad.McpBridge"            |   |  "HPAutoCad.App"          |  |
|  |  Path: Contents\Bridge\           |   |  Path: Contents\App\      |  |
|  |                                   |   |                           |  |
|  |  - HPAutoCad.McpBridge.dll        |   |  - HPAutoCad.dll          |  |
|  |  - Roslyn 5.9 (private)           |   |  - HPAutoCad.Core.dll     |  |
|  |  - HPAutoCad.Aec.dll              |   |  - WebView2 (managed)     |  |
|  |  - CommunityToolkit.Mvvm (copy 1) |   |  - CommunityToolkit.Mvvm  |  |
|  |  - MaterialDesign (repacked)      |   |  - MaterialDesign (repack)|  |
|  |  - Serilog                        |   |  - WebView2Loader.dll     |  |
|  +-----------------------------------+   +---------------------------+  |
+-------------------------------------------------------------------------+
```

### 1. `AssemblyDependencyResolver` Operation
Both `BridgeLoadContext` and `GeoLoadContext` implement:
```csharp
public sealed class BridgeLoadContext : AssemblyLoadContext
{
    private static readonly string[] HostAssemblyPrefixes = ["Ac", "Ad", "Autodesk."];
    private readonly AssemblyDependencyResolver _resolver;

    public BridgeLoadContext(string mainAssemblyPath) : base(name: "HPAutoCad.McpBridge", isCollectible: false)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? string.Empty;
        // Never duplicate AutoCAD host assemblies: return null to fall through to Default ALC
        if (HostAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return null;

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}
```

### 2. Why Fallthrough (`return null`) is Necessary
- Types like `Autodesk.AutoCAD.DatabaseServices.Database`, `Document`, and `Transaction` must match the exact runtime instance created by `acad.exe`.
- Returning `null` instructs the runtime to fallback to the Default ALC.
- In `.csproj`, `<PackageReference Include="AutoCAD.NET" ExcludeAssets="runtime" PrivateAssets="all"/>` guarantees that host assemblies are never written into `deps.json`.

### 3. Reflection-Only Entry Points
The loader references neither `HPAutoCad.McpBridge` nor `HPAutoCad` at compile time:
```csharp
var context = new BridgeLoadContext(bridgePath);
var assembly = context.LoadFromAssemblyPath(bridgePath);
var entry = assembly.GetType("HPAutoCad.McpBridge.BridgeEntry")!;
var start = entry.GetMethod("Start", BindingFlags.Public | BindingFlags.Static)!;
var handle = start.Invoke(null, [Path.GetDirectoryName(bridgePath)!, log]);
var bridgeDelegates = (IReadOnlyDictionary<string, Delegate>)handle!;
```
Delegates (`Action`, `Func<string>`) are type-compatible across load contexts because their definitions live in the core runtime (`System.Private.CoreLib`).

---

## 5. Ribbon Integration Architecture

### The Shared Tab Convention
In AutoCAD, creating multiple tabs for tools from the same vendor clutters the UI. The established rule across HP add-ins is:
**"One tab (`HPAutoCad`), one panel per tool, never a new tab."**

```
+-----------------------------------------------------------------------------------------+
|                                    Ribbon Tab: "HPAutoCad"                              |
|                                (Id: HPAUTOCAD_MCP_TAB)                                  |
|                                                                                         |
|  +--------------------------+  +-----------------------------------------------------+  |
|  |       Panel: "MCP"       |  |                 Panel: "HPGeoLink"                  |  |
|  | (HPAUTOCAD_MCP_PANEL)    |  |               (HPGEOLINK_PANEL)                     |  |
|  |                          |  |                                                     |  |
|  |     [Large Button]       |  |     [Large Button]       [Stacked Buttons]          |  |
|  |       MCP Bridge         |  |         VN2000              Import KMZ              |  |
|  |  (HPAUTOCAD_MCP_BRIDGE)  |  |      (HPGEO_DIALOG)         Insert Satellite Map    |  |
|  |   Cmd: HPMCPBRIDGE       |  |       Cmd: HPGEO            Geo Info                |  |
|  +--------------------------+  +-----------------------------------------------------+  |
+-----------------------------------------------------------------------------------------+
```

### Tab Lifecycle & Resilience
1. **Idempotent Tab Creation**:
   ```csharp
   var ribbon = ComponentManager.Ribbon;
   var tab = ribbon.FindTab(TabId);
   if (tab is null)
   {
       tab = new RibbonTab { Id = TabId, Title = TabTitle, IsVisible = true };
       ribbon.Tabs.Add(tab);
   }
   if (tab.Panels.Any(p => p.Source?.Id == PanelId)) return;
   tab.Panels.Add(BuildPanel());
   ```
2. **Rebuilding on AutoCAD Events**:
   - `WSCURRENT` (Workspace Switch): AutoCAD rebuilds the entire Ribbon from CUI, dropping runtime-created tabs. Listening to `Application.SystemVariableChanged` and queuing recreation on `Application.Idle` restores the panels seamlessly.
   - `COLORTHEME` (Dark/Light Switch): Vector icon ink color depends on background luminance (`COLORTHEME == 0` is dark, `1` is light). On theme change, the panel is removed and reconstructed with theme-matched icons.
   - `ComponentManager.ItemInitialized`: Catches late ribbon initialization after `RIBBON` command is run following `RIBBONCLOSE`.

---

## 6. Packaging, Repacking & Native Collisions

### MaterialDesignThemes Repacking (Spike S0-B Findings)
- **Problem**: WPF resolves BAML resources and styles by simple assembly name. If `MaterialDesignThemes.Wpf.dll` exists loose in two different ALCs within `acad.exe` (e.g., `Bridge/` and `App/`), WPF binds to the last-loaded assembly across all ALCs, corrupting styles or throwing cross-domain exceptions.
- **Solution**: Target `RepackMaterialDesign` in `.csproj` using `ILRepack 2.0.46`:
  ```xml
  <Target Name="RepackMaterialDesign" AfterTargets="CopyFilesToOutputDirectory" Condition="Exists('$(OutDir)MaterialDesignThemes.Wpf.dll')">
      <PropertyGroup>
          <_RepackExe>$(PkgILRepack)\tools\ILRepack.exe</_RepackExe>
          <_RepackLib>@(ReferencePath->'%(RelativeDir)'->Distinct()->'/lib:&quot;%(Identity) &quot;', ' ')</_RepackLib>
      </PropertyGroup>
      <Exec Command="&quot;$(_RepackExe)&quot; /union /parallel /noRepackRes $(_RepackLib) /out:&quot;$(OutDir)$(AssemblyName).dll&quot; &quot;@(IntermediateAssembly->'%(FullPath)')&quot; &quot;$(OutDir)MaterialDesignThemes.Wpf.dll&quot; &quot;$(OutDir)MaterialDesignColors.dll&quot; &quot;$(OutDir)Microsoft.Xaml.Behaviors.dll&quot;"/>
      <Delete Files="$(OutDir)MaterialDesignThemes.Wpf.dll;$(OutDir)MaterialDesignColors.dll;$(OutDir)Microsoft.Xaml.Behaviors.dll;$(OutDir)MaterialDesignThemes.Wpf.xml"/>
  </Target>
  ```
- **Crucial Rules**:
  - `IntermediateAssembly` (the obj DLL) is primary input, preventing double-merge on incremental builds.
  - Toolkit DLLs are deleted immediately after repack so `DeployBundle` never copies them into the bundle.
  - `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]` is required in `ThemeInfo.cs`.

### `WebView2Loader.dll` Native Asset Handling
- `Microsoft.Web.WebView2 1.0.4191.47` packages `WebView2Loader.dll` in `runtimes\win-x64\native\`.
- In `HPGeo.AutoCad.csproj`, `EnableDynamicLoading=true` ensures `deps.json` contains:
  ```json
  "runtimeTargets": {
    "runtimes/win-x64/native/WebView2Loader.dll": {
      "rid": "win-x64",
      "assetType": "native"
    }
  }
  ```
- When `GeoLoadContext.LoadUnmanagedDll("WebView2Loader.dll")` executes, `AssemblyDependencyResolver.ResolveUnmanagedDllToPath` resolves the absolute path to `Contents\App\runtimes\win-x64\native\WebView2Loader.dll`.
- **Packaging Guardrail**: `DeployBundle` must copy `$(AppOutDir)**\*` recursively to preserve `runtimes\win-x64\native\WebView2Loader.dll`.

### Out-of-Process `TileFetch` Architecture
- AutoCAD workstations in enterprise environments frequently have firewall rules blocking `acad.exe` from outbound internet access.
- `HPAutoCad.TileFetch.exe` is a standalone .NET 8 console executable residing in `Contents\App\TileFetch\`.
- The add-in launches it as a child process via `System.Diagnostics.Process`, passing request arguments and receiving tile status via stdout. Because child processes run under their own executable identity, they bypass `acad.exe` firewall blocks.

---

## 7. HPCivil3d Mirror Mechanism & Strict Guardrails

### Mirror Relationship (ADR-01 Option A)
`HPCivil3d/` was created by copying `HPAutoCad/` with tokens applied, because Civil 3D 2026 is an AutoCAD vertical running on the exact same `acad.exe` (R25.1).

### Mirror Inventory (`HPCivil3d/tools/mirror-tokens.json`)
- **48 Ordered Tokens**: Replaces identifiers (e.g. `"HPAutoCad.McpBridge"` -> `"HPCivil3d.McpBridge"`, `"HPMCPBRIDGE"` -> `"HPC3DMCPBRIDGE"`, `AutocadScriptGlobals` -> `Civil3dScriptGlobals`, `PipeNaming.AutocadHost` -> `PipeNaming.Civil3dHost`).
- **24 Mirrored Files**: Compared line-for-line in `MirrorTests.cs`. Includes all loader files, runner, serializers, status view, and themes:
  - `HPAutoCad.McpBridge.Loader/BridgeActions.cs`
  - `HPAutoCad.McpBridge.Loader/BridgeLoadContext.cs`
  - `HPAutoCad.McpBridge.Loader/BridgeLoaderApplication.cs`
  - `HPAutoCad.McpBridge.Loader/BridgeLoaderCommands.cs`
  - `HPAutoCad.McpBridge.Loader/LoaderLog.cs`
  - `HPAutoCad.McpBridge.Loader/Ribbon/McpRibbonTab.cs`
  - `HPAutoCad.McpBridge.Loader/Ribbon/RibbonCommandHandler.cs`
  - `HPAutoCad.McpBridge.Loader/Ribbon/RibbonIcons.cs`
  - `HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj`
  - `HPAutoCad.McpBridge/MainThreadExecutor.cs`
  - `HPAutoCad.McpBridge/Model/AutocadScriptGlobals.cs`
  - `HPAutoCad.McpBridge/Service/AutocadScriptRunner.cs`
  - `HPAutoCad.McpBridge/Service/AutocadResultSerializer.cs`
  - `HPAutoCad.McpBridge/Service/AutocadContextReader.cs`
  - `HPAutoCad.McpBridge/Service/DatabaseChangeCounter.cs`
  - `HPAutoCad.McpBridge/Service/AutocadVersionMap.cs`
  - `HPAutoCad.McpBridge/View/AutocadBridgeStatusView.xaml`
  - `HPAutoCad.McpBridge/View/AutocadBridgeStatusView.xaml.cs`
  - `HPAutoCad.McpBridge/Resources/Themes/AutocadTheme.xaml`
  - `HPAutoCad.McpBridge/Resources/Themes/MaterialBridge.xaml`
  - `HPAutoCad.McpBridge/Resources/Themes/ThemeDark.xaml`
  - `HPAutoCad.McpBridge/Resources/Themes/ThemeLight.xaml`
  - `HPAutoCad.McpBridge/Resources/Themes/MaterialThemeBridge.cs`
  - `HPAutoCad.McpBridge/Resources/Themes/IHostTheme.cs`
  - `HPAutoCad.McpBridge/Resources/Themes/ThemeInfo.cs`
  - `HPAutoCad.McpBridge/Service/AutocadHostTheme.cs`
  - `HPAutoCad.Mcp.Server/Program.cs`
  - `HPAutoCad.Mcp.Server/appsettings.json`
  - `HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.csproj`
- **10 Owned Counterparts Pinned by SHA-256**:
  Hand-ported files whose source text hash is pinned:
  1. `HPAutoCad.McpBridge/BridgeEntry.cs` (`d6ae4b3398e837433cecc4b5273ea00d470bb8af35d4e41dd3f12589e75c986f`)
  2. `HPAutoCad.McpBridge/Service/ScriptingSelfCheck.cs` (`c5b1668a378a8571ac9610365e696eefc023380c56f56ee11b8344976872b19d`)
  3. `HPAutoCad.McpBridge/HPAutoCad.McpBridge.csproj` (`67a534f6f95404106fa4b2025feb0781799c38ca4486509271ad4884b3428b23`)
  4. `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` (`22d566c8faec996572452d3fab99fdceaa0d89732f6f42b28df86f3602c1ef28`)
  5. `HPAutoCad.McpBridge.Loader/Properties/launchSettings.json` (`12719c1a074a8150892aaab0ec2bed1c9c004cd23d46abf50ba476fb2b6975f4`)
  6. `HPAutoCad.Mcp.Server/Hosts/AutocadHostProfile.cs` (`b9e29e06433a9c2de0ecba0451378a0038d59a6328754d4548921d6ba9bcdde1`)
  7. `HPAutoCad.Mcp.Server/Tools/ExecuteAutocadCodeTool.cs` (`bf3bced2e5ccaf7a590a7a6531c9ff5ef11cda5c9e42138acf54463b12badd15`)
  8. `HPAutoCad.Mcp.Server/Tools/AutocadContextTool.cs` (`3050d68c41ac727e16150ad9da5e80f0d86c1a00819aab83bc41853db23832b3`)
  9. `HPAutoCad.Mcp.Server/Prompts/AutocadScriptPrompts.cs` (`426f386a600bdfa5281b95e9fcc41b24c5c48da405a67901cf6e2b5a6c5d2d6f`)
  10. `HPAutoCad.Mcp.Server/Resources/AutocadDocumentResources.cs` (`b800029027568763d6197dedee15da58b0a2a79593e4f940f2d4c2b2898f535e`)

### Critical Mirror Assertions & Safe Boundaries
In `HPCivil3d.McpBridge.Tests/MirrorTests.cs`:
```csharp
var unknown = MirrorTokenTable.SourceFiles(root, "HPAutoCad.McpBridge", "HPAutoCad.McpBridge.Loader", "HPAutoCad.Mcp.Server")
    .Where(path => !known.Contains(Path.GetFullPath(path)))
    .ToArray();
Assert.True(unknown.Length == 0);
```

#### Vital Rules for Any HPAutoCad Changes:
1. **Scope Boundary**: `MirrorTokenTable.SourceFiles` **ONLY** scans `"HPAutoCad.McpBridge"`, `"HPAutoCad.McpBridge.Loader"`, and `"HPAutoCad.Mcp.Server"`.
2. **New Projects Are Invisible to Mirror Tests**: Adding `HPAutoCad`, `HPAutoCad.Core`, `HPAutoCad.TileFetch`, and `HPAutoCad.Tests` to `HPAutoCad/` will **NOT** trigger `Every_AutoCAD_bridge_source_file_is_either_mirrored_or_an_owned_counterpart`.
3. **Modifying Mirrored Files is High Risk**: If any of the 24 mirrored files is edited in `HPAutoCad`, the exact change must be mirrored in `HPCivil3d` (subject to tokens and `civil-only` blocks) or `MirrorTests` will fail.
4. **No `autocad-only` Syntax Exists**: The mirror engine only recognizes `// civil-only: begin ... end`. It cannot strip lines added exclusively to AutoCAD in mirrored files!
5. **Loader Project Identity**: `HPAutoCad.McpBridge.Loader` cannot be renamed without modifying `mirror-tokens.json` and `HPCivil3d.McpBridge.Loader`.

---

## 8. Architectural Recommendations: Mirroring HPRebar in HPAutoCad

### Solution Layout Comparison

| Dimension | `HPRebar` (Reference Pattern) | Proposed `HPAutoCad` Pattern |
|---|---|---|
| **Add-In Host Project** | `HPRebar/HPRebar.csproj` (.NET 8.0-windows) | `HPAutoCad/HPAutoCad.csproj` (.NET 8.0-windows) |
| **Pure Domain Logic** | `HPRebar.Core/HPRebar.Core.csproj` (`netstandard2.0`) | `HPAutoCad.Core/HPAutoCad.Core.csproj` (.NET 8.0) |
| **Unit Test Suite** | `HPRebar.Core.Tests/` (net8.0, xUnit v3 / MTP) | `HPAutoCad.Tests/` (net10.0-windows, xUnit v3 / MTP) |
| **Feature Folders** | `ColumnRebar/`, `BeamRebar/`, `FoundationRebar/` | `HPGeoLink/` (and future features, e.g. `LayerTools/`) |
| **Subfolder Structure** | `Commands/`, `Models/`, `Service/`, `View/`, `ViewModels/` | `Commands/`, `Model/`, `Service/`, `View/`, `ViewModel/` |
| **Theming & Controls** | `Resources/Themes/Theme.xaml` + `MaterialBridge.xaml` | `Resources/Themes/Theme.xaml` + `MaterialBridge.xaml` |
| **MCP Integration** | `HPRebar.McpBridge/` + `HPRebar.Mcp.Server/` | `HPAutoCad.McpBridge/` + `HPAutoCad.Mcp.Server/` |
| **AEC Engine** | N/A | `HPAutoCad.Aec/` + `HPAutoCad.Aec.Tests/` |
| **Helper Process** | N/A | `HPAutoCad.TileFetch/` (Console out-of-process) |

### Detailed Project Proposals

#### 1. `HPAutoCad` (`HPAutoCad/HPAutoCad/HPAutoCad.csproj`)
- **TFM**: `net8.0-windows`
- **Output**: `Contents\App\HPAutoCad.dll` (loaded into isolated ALC)
- **Features Folder**: `HPGeoLink/`
  - `Commands/`: `HPGeoDialogCommand.cs`, `HPGeoKmzScriptCommand.cs`, `HPGeoImportCommand.cs`, `HPGeoImportScriptCommand.cs`, `HPGeoImageScriptCommand.cs`, `HPGeoInfoCommand.cs`, `ImageryConsole.cs`
  - `Model/`: `DocumentSettingsStore.cs`, `UserSettingsStore.cs`
  - `Service/`: `DrawingReader.cs`, `DrawingWriter.cs`, `DrawingContext.cs`, `ImageryPipeline.cs`, `RasterInserter.cs`, `TileStitcher.cs`, `HelperTileFetcher.cs`, `GoogleEarthLauncher.cs`
  - `View/`: `GeoExportWindow.xaml`, `GeoImportWindow.xaml`, `CrsSelectionView.xaml`, `MapPanel.xaml`
  - `ViewModel/`: `GeoExportViewModel.cs`, `GeoImportViewModel.cs`, `CrsSelectionViewModel.cs`
- **Repack Target**: Integrates `RepackMaterialDesign` to merge `MaterialDesignThemes` into `HPAutoCad.dll`.
- **TileFetch Target**: Integrates `CopyTileFetchHelper` to copy `HPAutoCad.TileFetch` binaries into `$(OutDir)TileFetch\`.

#### 2. `HPAutoCad.Core` (`HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj`)
- **TFM**: `net8.0` (zero AutoCAD API references, 100% testable on any machine)
- **Features Folder**: `HPGeoLink/`
  - `Catalog/`: `ProvinceCatalog.cs`, `Province.cs`, `CentralMeridian.cs`, `Data/vn2000-provinces.json`
  - `Projection/`: `Ellipsoid.cs`, `TmParameters.cs`, `TransverseMercator.cs`, `GeocentricConverter.cs`, `Helmert7.cs`, `Vn2000Wgs84Transform.cs`
  - `Conversion/`: `Vn2000Converter.cs`, `ConversionModels.cs`, `ExportArguments.cs`
  - `Kml/`: `KmzExportPipeline.cs`, `KmlDocumentBuilder.cs`, `KmlGeoMath.cs`, `KmlColor.cs`, `KmzWriter.cs`, `KmlExportOptions.cs`
  - `Import/`: `ImportPlanner.cs`, `KmlReader.cs`, `CoordinateTextParser.cs`
  - `Imagery/`: `TileFetcher.cs`, `TileCache.cs`, `TileCoverage.cs`, `RasterWarper.cs`, `WebMercator.cs`, `AffineFit.cs`, `ImageryProvider.cs`, `ImageArguments.cs`, `ImageZoneResolver.cs`, `OutputRaster.cs`, `TileFetchProtocol.cs`
  - `Model/`: `GeoPoint.cs`, `PlanePoint.cs`
  - `Geometry/`: `BulgeTessellator.cs`
  - `Settings/`: `GeoSettings.cs`

#### 3. `HPAutoCad.TileFetch` (`HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj`)
- **TFM**: `net8.0` console executable.
- **Reference**: `HPAutoCad.Core` only.
- **Output**: Copied into `Contents\App\TileFetch\`.

#### 4. `HPAutoCad.Tests` (`HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj`)
- **TFM**: `net10.0-windows`
- **Runner**: Microsoft Testing Platform (`global.json` / `xunit.v3 3.1.0`)
- **References**: `HPAutoCad.Core`, `HPAutoCad` (WPF view model testing without AutoCAD)
- **Fixtures**: Golden JSON fixtures ported directly from `HPGeo.Tests/Fixtures/` (all 161 tests intact).

---

## 9. Loader & Bundle Integration Strategy

To achieve a single bundle `HPAutoCad.bundle` deployed to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\` while guaranteeing **zero mirror regressions** with `HPCivil3d`, we evaluated two architectural strategies:

### Strategy Comparison

```
+-------------------------------------------------------------------------------------------------+
| Strategy A: Dual-Loader Component Entries in Single Bundle (RECOMMENDED)                       |
|                                                                                                 |
|  HPAutoCad.bundle/PackageContents.xml                                                           |
|    - ComponentEntry 1: ./Contents/HPAutoCad.McpBridge.Loader.dll                                |
|    - ComponentEntry 2: ./Contents/HPAutoCad.Loader.dll (for HPGeoLink & UI)                    |
|                                                                                                 |
|  * HPCivil3d Mirror Impact: ZERO. HPAutoCad.McpBridge.Loader is untouched.                      |
|  * Ribbon Behavior: McpRibbonTab and HPGeoRibbonTab already share TabId HPAUTOCAD_MCP_TAB;      |
|    whichever loads first creates the tab, the other appends its panel.                          |
+-------------------------------------------------------------------------------------------------+
| Strategy B: Single Unified Loader Assembly (Alternative)                                        |
|                                                                                                 |
|  HPAutoCad.bundle/PackageContents.xml                                                           |
|    - Single ComponentEntry: ./Contents/HPAutoCad.Loader.dll                                     |
|    - HPAutoCad.Loader loads both BridgeLoadContext and AppLoadContext                           |
|                                                                                                 |
|  * HPCivil3d Mirror Impact: HIGH. HPAutoCad.McpBridge.Loader would be replaced or modified.    |
|    Requires modifying mirror-tokens.json, adding new tokens, and adjusting MirrorTests.cs.     |
+-------------------------------------------------------------------------------------------------+
```

### Recommendation: Strategy A (Dual-Loader in Single Bundle)
1. **Bundle Folder Unification**:
   - `HPAutoCad.bundle` becomes the single deployed folder.
   - `HPAutoCad.McpBridge.Loader.csproj` updates `<BundleDir>` to deploy into `HPAutoCad.bundle\` (with mirror token updated).
   - `HPAutoCad.Loader.csproj` also deploys into `HPAutoCad.bundle\`.
2. **PackageContents.xml**:
   Declares both `HPAutoCad.McpBridge.Loader.dll` and `HPAutoCad.Loader.dll` under `<Components Platform="AutoCAD" SeriesMin="R25.1" SeriesMax="R25.1">`.
3. **Isolation Guarantee**:
   - Bridge runs in its own `BridgeLoadContext` with Roslyn and AEC tools.
   - Add-in runs in its own `AppLoadContext` with WebView2 and HPGeoLink.
   - Neither contaminates AutoCAD's Default ALC.
4. **Mirror Test Safety**:
   `HPCivil3d.McpBridge.Tests` remains 100% green because `HPAutoCad.McpBridge.Loader` source files are preserved.

---

## 10. Autonomous Closed-Loop Verification Protocol

To satisfy requirement **R3** ("Mandatory Closed-Loop Verification via MCP AutoCAD"), every tool implementation and migration step must pass through the closed-loop verification cycle:

```
+-------------------------------------------------------------------------+
|                    Autonomous Closed-Loop MCP Cycle                     |
|                                                                         |
|   1. Build Projects (dotnet build HPAutoCad.slnx -c Debug)              |
|          |                                                              |
|          v                                                              |
|   2. Run Static Tests:                                                  |
|      - HPAutoCad.Aec.Tests (225 tests)                                  |
|      - HPAutoCad.Mcp.Server.Tests (280 tests)                           |
|      - HPCivil3d.McpBridge.Tests (60 tests)                             |
|      - HPAutoCad.Tests (161 tests)                                      |
|          |                                                              |
|          v                                                              |
|   3. Deploy Bundle to %AppData%\Autodesk\ApplicationPlugins\             |
|          |                                                              |
|          v                                                              |
|   4. Launch AutoCAD with bridge.scr:                                    |
|      - Script: HPMCPBRIDGE / HPMCPSTART                                 |
|      - Handle SECURELOAD via UIA                                        |
|      - Enable AI Execution via UIA                                      |
|          |                                                              |
|          v                                                              |
|   5. Execute Live Scenarios via Named Pipe or MCP Server:               |
|      - Test HPGEOINFO, HPGEOKMZ, HPGEOIMPORT, HPGEO                     |
|      - Validate WebView2 map panel & screenshot dialog                  |
|      - Check logs for exceptions or errors (-32001, -32002, -32003)     |
|          |                                                              |
|          v                                                              |
|   6. If Error -> Diagnose root cause -> Self-correct code -> Re-test    |
|   7. If Clean -> Declare verification COMPLETE                          |
+-------------------------------------------------------------------------+
```

### Existing Harness Scripts Ready for Reuse:
- `HPAutoCad/tools/harness/run-bridge-unattended.ps1`: Starts AutoCAD, automates SECURELOAD and UIA opt-in, runs pipe scenarios.
- `HPAutoCad/tools/harness/run-ribbon-check.ps1`: Verifies ribbon tab and button creation via UI Automation.
- `HPGeo/tools/dialog-check.ps1`: Automated dialog launcher, UIA button click, and WebView2 verification (to be ported to `HPAutoCad/tools/harness/`).

---

## 11. Conclusion & Actionable Next Steps

1. **Architecture is Validated**: The existing infrastructure in `HPAutoCad` and `HPGeo` is mature, robust, and well-tested (726 passing tests).
2. **Clean Separation**: Geodetic logic in `HPGeo.Core` has zero CAD dependencies, making its migration into `HPAutoCad.Core/HPGeoLink/` frictionless.
3. **Mirror Guardrails Understood**: By keeping `HPAutoCad.McpBridge.Loader` and `HPAutoCad.McpBridge` intact and adding the new add-in layer alongside them in `HPAutoCad.bundle`, Civil 3D mirror tests are completely protected against accidental regressions.
4. **Immediate Next Step**: The implementation team can proceed with Phase 1 (creating `HPAutoCad.Core`, `HPAutoCad`, `HPAutoCad.TileFetch`, and `HPAutoCad.Tests` within `HPAutoCad.slnx`).
