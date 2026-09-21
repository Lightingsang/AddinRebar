# HPAutoCad.Loader Architectural & Implementation Specification (Milestone M3)

> **Document Status**: Complete Plan  
> **Author**: `explorer_m3_loader`  
> **Target Path**: `HPAutoCad/HPAutoCad.Loader/`  
> **Target Solution**: `HPAutoCad/HPAutoCad.slnx`  
> **Parent Orchestration**: `orchestrator_3` (Milestone M3: Single Bundle, Loader & Shared Ribbon)  
> **Date**: 2026-09-20  

---

## 1. Executive Summary & Architectural Mission

The `HPAutoCad.Loader` project serves as the AutoCAD extension entry point for the migrated `HPGeoLink` tool suite within the unified `HPAutoCad.bundle`. 

### Key Design Mandates
1. **Isolated Assembly Loading (`AppLoadContext`)**:
   Load `Contents\App\HPAutoCad.dll` and all its transitive dependencies (`CommunityToolkit.Mvvm`, `Microsoft.Web.WebView2`, `HPAutoCad.Core`) inside a dedicated `AssemblyLoadContext` ("`HPAutoCad.App`"). This prevents runtime type collisions with AutoCAD's Default ALC and the MCP bridge's ALC (`BridgeLoadContext` in `Contents\Bridge\`).
2. **Zero Direct Add-In Assembly References**:
   The loader project must **never** reference `HPAutoCad.dll` as a compile-time assembly dependency (`ReferenceOutputAssembly="false"`). If referenced at compile time, the .NET JIT compiler would resolve dependencies directly into the Default ALC upon loader execution, defeating assembly isolation.
3. **Transparent Command Registration & Invocation**:
   Declare AutoCAD commands (`HPGEO`, `HPGEODIALOG`, `-HPGEOKMZ`, `HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `-HPGEOIMAGE`, `HPGEOINFO`) in the Default ALC using `[CommandMethod]`. Forward execution across the ALC boundary via `System.Action` / `System.Delegate` instances acquired through reflection from `HPAutoCad.Entry.Start()`.
4. **Shared Ribbon Protocol (`HPAUTOCAD_MCP_TAB`)**:
   Hook into AutoCAD's Ribbon system using `Autodesk.Windows` (AdWindows.dll in Default ALC), contributing the `HPGEOLINK_PANEL` panel ("HPGeoLink") alongside `HPAUTOCAD_MCP_PANEL` ("MCP") on the single shared tab `HPAUTOCAD_MCP_TAB`.
5. **Civil 3D Mirror Safety Invariant**:
   Guarantee 100% test pass rate for `HPCivil3d.McpBridge.Tests` (`MirrorTests.cs`). Prove that introducing `HPAutoCad.Loader` has zero side effects on the Civil 3D mirror token table and counterpart hashing.

---

## 2. Architectural Context & Multi-ALC Isolation

In AutoCAD 2026 running on .NET 8, AutoCAD's host process (`acad.exe`) loads extension assemblies (`IExtensionApplication`) into the default `AssemblyLoadContext` (`AssemblyLoadContext.Default`). Multiple add-ins loading conflicting versions of shared libraries (such as `CommunityToolkit.Mvvm`, `Microsoft.CodeAnalysis`, or `Microsoft.Web.WebView2`) into the Default ALC causes fatal `FileLoadException` or silent type cast mismatches.

### 2.1 Multi-Context Topology in `HPAutoCad.bundle`

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            AutoCAD 2026 Process                             │
│                                 (acad.exe)                                  │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                    Default AssemblyLoadContext                        │  │
│  │                                                                       │  │
│  │  - AutoCAD Core Runtimes (acdbmgd, accoremgd, acmgd)                   │  │
│  │  - Autodesk.Windows (AdWindows.dll, Ribbon System, WPF)               │  │
│  │  - HPAutoCad.McpBridge.Loader.dll (Bridge Commands & Ribbon MCP)      │  │
│  │  - HPAutoCad.Loader.dll (HPGeo Commands & Ribbon HPGeoLink)           │  │
│  └───────────────────────┬───────────────────────┬───────────────────────┘  │
│                          │                       │                          │
│             creates &    │                       │  creates &               │
│             delegates to │                       │  delegates to            │
│                          ▼                       ▼                          │
│  ┌───────────────────────────────┐   ┌───────────────────────────────────┐  │
│  │      BridgeLoadContext        │   │          AppLoadContext           │  │
│  │      ("HPAutoCad.McpBridge")  │   │          ("HPAutoCad.App")        │  │
│  ├───────────────────────────────┤   ├───────────────────────────────────┤  │
│  │ Base: Contents\Bridge\        │   │ Base: Contents\App\               │  │
│  │ Deps: McpBridge.deps.json     │   │ Deps: HPAutoCad.deps.json         │  │
│  │                               │   │                                   │  │
│  │ - Roslyn 5.9 Compiler         │   │ - HPAutoCad.dll (Add-In Core)     │  │
│  │ - System.Collections.Immutable│   │ - HPAutoCad.Core.dll (Geodetic)   │  │
│  │ - Serilog & Sinks             │   │ - CommunityToolkit.Mvvm 8.4.0     │  │
│  │ - McpBridge.Core & Contracts  │   │ - Microsoft.Web.WebView2.Core     │  │
│  │ - ILRepacked MaterialDesign   │   │ - runtimes\win-x64\native\        │  │
│  │   (Merged into McpBridge.dll) │   │   WebView2Loader.dll (Unmanaged)  │  │
│  │                               │   │ - ILRepacked MaterialDesign       │  │
│  │                               │   │   (Merged into HPAutoCad.dll)     │  │
│  └───────────────────────────────┘   └───────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Host Assembly Fallthrough Rule
Any type belonging to the host API (`Ac*`, `Ad*`, `Autodesk.*`) must **never** be loaded into custom ALCs. If a duplicate copy of `acdbmgd.dll` were loaded into `AppLoadContext`, an `ObjectId` or `Database` passed from AutoCAD would belong to a distinct CLR `Type`, resulting in `InvalidCastException`.
`AppLoadContext.Load()` intercepts requests starting with `Ac`, `Ad`, or `Autodesk.` and returns `null`, forcing the runtime to resolve them from the Default ALC.

### 2.3 BAML Resource Isolation via ILRepack
WPF binds BAML resources and controls by assembly simple name across all ALCs within the same process. Having loose copies of `MaterialDesignThemes.Wpf.dll` in both `Contents\Bridge\` and `Contents\App\` would cause BAML cross-contamination. 
Both `HPAutoCad.McpBridge.csproj` and `HPAutoCad.csproj` execute the `RepackMaterialDesign` post-build target via `ILRepack.exe`, merging `MaterialDesignThemes` directly into their respective assemblies and deleting loose DLLs. Consequently, no loose `MaterialDesignThemes.Wpf.dll` exists in either directory.

---

## 3. Analysis of Legacy `HPGeo.AutoCad.Loader` & Design Evolution

### 3.1 Legacy Implementation Review (`HPGeo/HPGeo.AutoCad.Loader/`)
1. **`HPGeo.AutoCad.Loader.csproj`**:
   - Targeted `net8.0-windows` with `<UseWPF>true</UseWPF>` and `AutoCAD.NET [25.1.0]`.
   - Used `<ProjectReference Include="..\HPGeo.AutoCad\HPGeo.AutoCad.csproj" ReferenceOutputAssembly="false" Private="false"/>`.
   - Deployed standalone bundle to `%AppData%\Autodesk\ApplicationPlugins\HPGeo.bundle\`.
2. **`GeoLoadContext.cs`**:
   - Subclassed `AssemblyLoadContext("HPGeo.AutoCad", isCollectible: false)`.
   - Used `AssemblyDependencyResolver` pointed at `HPGeo.AutoCad.dll`.
   - Filtered `["Ac", "Ad", "Autodesk."]`.
   - Overrode `LoadUnmanagedDll` with `_resolver.ResolveUnmanagedDllToPath`.
3. **`HPGeoCommands.cs`**:
   - Defined 6 commands: `HPGEO`, `-HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `-HPGEOIMAGE`, `HPGEOINFO`.
   - Looked up delegates from static dictionary `HPGeoLoaderApplication.App`.
   - Unwrapped `TargetInvocationException` and reported failures to both `Editor` and `LoaderLog`.
4. **`HPGeoLoaderApplication.cs`**:
   - Implemented `IExtensionApplication`.
   - Loaded `HPGeo.AutoCad.dll` into `GeoLoadContext` and invoked `Entry.Start(appDir, product, acadVersion)`.
   - Installed `HPGeoRibbonTab` and uninstalled on `Terminate`.
5. **`LoaderLog.cs`**:
   - BCL-only static logger writing to `%LocalAppData%\HPGeo\logs\loader.log`.

### 3.2 Evolution & Enhancements for `HPAutoCad.Loader`
| Component | Legacy `HPGeo.AutoCad.Loader` | New `HPAutoCad.Loader` | Rationale / Benefit |
|---|---|---|---|
| **Assembly / Project Name** | `HPGeo.AutoCad.Loader` | `HPAutoCad.Loader` | Unified branding within `HPAutoCad` ecosystem. |
| **Target Bundle Layout** | Standalone `HPGeo.bundle` | Subfolder in `HPAutoCad.bundle/Contents/` | Consolidates MCP Bridge and HPGeoLink into a single bundle. |
| **ALC Name** | `"HPGeo.AutoCad"` | `"HPAutoCad.App"` | Clearly denotes the App ALC versus `"HPAutoCad.McpBridge"`. |
| **Unmanaged Probing** | Resolver-only | Resolver + Fallback Probing (`runtimes\win-x64\native\`) | Bulletproof resolution for `WebView2Loader.dll` across CAD runtimes. |
| **Command Aliases** | `HPGEO` only | `HPGEO` + `HPGEODIALOG`; `-HPGEOKMZ` + `HPGEOKMZ` | Satisfies both CLI acceptance tests and UI command conventions. |
| **Entry Point Contract** | 3 string parameters | Flexible invocation (`appDir`, `Action<string>`) with fallback | Direct integration with `HPAutoCad.Entry.Start` while maintaining backward compatibility. |
| **Log Location** | `%LocalAppData%\HPGeo\logs\` | Dual-write: `%LocalAppData%\HPAutoCad\logs\` + legacy `%LocalAppData%\HPGeo\logs\` | Meets updated repository conventions while preserving compatibility with legacy test harnesses. |
| **Ribbon Integration** | `HPGEO_VN2000_PANEL` on `HPAUTOCAD_MCP_TAB` | `HPGEOLINK_PANEL` on `HPAUTOCAD_MCP_TAB` | Aligns with `PROJECT.md` contract for shared tab. |

---

## 4. Project Specification: `HPAutoCad.Loader.csproj`

### 4.1 Path and Solution Location
- **Path**: `HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj`
- **TFM**: `net8.0-windows`
- **Output Assembly**: `HPAutoCad.Loader.dll`

### 4.2 Verbatim Project File Design

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <!-- What AutoCAD loads: the extension application, commands, and ribbon integration,
             with no dependencies beyond the AutoCAD API and .NET runtime. The add-in proper
             (Contents\App\HPAutoCad.dll) is loaded into an isolated AssemblyLoadContext
             so its dependencies (WebView2, MVVM Toolkit, etc.) never collide with AutoCAD Default ALC. -->
        <TargetFramework>net8.0-windows</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <UseWPF>true</UseWPF>
        <RootNamespace>HPAutoCad.Loader</RootNamespace>
        <AssemblyName>HPAutoCad.Loader</AssemblyName>
        <Configurations>Debug;Release</Configurations>
        <Version>0.1.0</Version>
        <!-- AutoCAD 2026 base release: R25.1 on .NET 8. ExcludeAssets=runtime ensures zero AutoCAD DLLs are copied. -->
        <AutocadPackageVersion Condition="'$(AutocadPackageVersion)' == ''">[25.1.0]</AutocadPackageVersion>
        <AppOutDir>$(MSBuildThisFileDirectory)..\HPAutoCad\bin\$(Configuration)\$(TargetFramework)\</AppOutDir>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="AutoCAD.NET" Version="$(AutocadPackageVersion)" ExcludeAssets="runtime" PrivateAssets="all" />
    </ItemGroup>

    <ItemGroup>
        <!-- Build order dependency only: the loader must NEVER reference HPAutoCad.dll at compile time,
             which would cause the JIT to drag it into AutoCAD's Default ALC on load. -->
        <ProjectReference Include="..\HPAutoCad\HPAutoCad.csproj" ReferenceOutputAssembly="false" Private="false" />
    </ItemGroup>

</Project>
```

### 4.3 Key Property Decisions
1. `<UseWPF>true</UseWPF>`: Enables referencing `Autodesk.Windows` types (`RibbonTab`, `RibbonPanel`, `RibbonButton`, `DrawingImage`, `ImageSource`) from `AdWindows.dll`.
   * *Critical Gotcha*: `UseWPF` suppresses implicit `System.IO` importing to prevent collisions with `System.Windows.Shapes.Path`. Explicit `using System.IO;` must be declared in files touching files/paths.
2. `<ProjectReference ... ReferenceOutputAssembly="false" Private="false" />`: Ensures `HPAutoCad.csproj` compiles before `HPAutoCad.Loader.csproj` without adding compile-time references or copying assemblies.

---

## 5. AppLoadContext Specification (`AppLoadContext.cs`)

### 5.1 File Overview
- **File**: `HPAutoCad/HPAutoCad.Loader/AppLoadContext.cs`
- **Base Class**: `System.Runtime.Loader.AssemblyLoadContext`
- **Context Name**: `"HPAutoCad.App"`
- **Collectibility**: `isCollectible: false` (Non-collectible; survives lifetime of AutoCAD process).

### 5.2 Verbatim Code Implementation

```csharp
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace HPAutoCad.Loader;

/// <summary>
/// Isolated AssemblyLoadContext for the HPAutoCad (HPGeoLink) add-in.
/// Resolves dependencies (CommunityToolkit.Mvvm, WebView2, HPAutoCad.Core) from Contents\App\
/// using HPAutoCad.deps.json. Host assemblies (AutoCAD API) fall through to the Default ALC.
/// </summary>
internal sealed class AppLoadContext : AssemblyLoadContext
{
    private static readonly string[] HostAssemblyPrefixes = ["Ac", "Ad", "Autodesk."];

    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _mainAssemblyPath;
    private readonly string _appDirectory;

    public AppLoadContext(string mainAssemblyPath) : base(name: "HPAutoCad.App", isCollectible: false)
    {
        _mainAssemblyPath = mainAssemblyPath ?? throw new ArgumentNullException(nameof(mainAssemblyPath));
        _appDirectory = Path.GetDirectoryName(mainAssemblyPath) ?? string.Empty;
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? string.Empty;

        // Never load a second copy of AutoCAD or Autodesk API assemblies into the custom ALC.
        // Returning null allows resolution to fall through to AssemblyLoadContext.Default.
        if (HostAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        // Resolve managed assemblies using HPAutoCad.deps.json
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path is not null && File.Exists(path))
        {
            LoaderLog.Write($"load {name} {assemblyName.Version} <- {Path.GetFileName(path)}");
            return LoadFromAssemblyPath(path);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        // 1. Resolve unmanaged native DLL (e.g. WebView2Loader.dll) via deps.json runtime targets
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (path is not null && File.Exists(path))
        {
            LoaderLog.Write($"load unmanaged {unmanagedDllName} <- {Path.GetFileName(path)}");
            return LoadUnmanagedDllFromPath(path);
        }

        // 2. Fallback probe: runtimes\win-x64\native\ under App directory
        if (!string.IsNullOrEmpty(_appDirectory))
        {
            var probePath = Path.Combine(_appDirectory, "runtimes", "win-x64", "native", 
                unmanagedDllName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? unmanagedDllName : unmanagedDllName + ".dll");
            if (File.Exists(probePath))
            {
                LoaderLog.Write($"load unmanaged (fallback) {unmanagedDllName} <- {probePath}");
                return LoadUnmanagedDllFromPath(probePath);
            }
        }

        return IntPtr.Zero;
    }
}
```

---

## 6. Command System Specification (`HPGeoCommands.cs`)

### 6.1 Command Class Architecture
- **File**: `HPAutoCad/HPAutoCad.Loader/HPGeoCommands.cs`
- **Class**: `public sealed class HPGeoCommands` (must be public with parameterless constructor for AutoCAD reflection).
- **Attribute**: `[assembly: CommandClass(typeof(HPGeoCommands))]` declared at assembly scope.

### 6.2 Command Mapping Matrix
| Command Name | Flags | Add-In Delegate Key | Description / Behavior |
|---|---|---|---|
| `HPGEO` | `CommandFlags.Modal` | `"dialog"` | Primary interactive export dialog (`GeoExportWindow`). |
| `HPGEODIALOG` | `CommandFlags.Modal` | `"dialog"` | Explicit alias for interactive export dialog. |
| `-HPGEOKMZ` | `CommandFlags.Modal` | `"kmz-script"` | Scriptable command-line KMZ export prompt. |
| `HPGEOKMZ` | `CommandFlags.Modal` | `"kmz-script"` | Non-hyphenated alias for scriptable export. |
| `HPGEOIMPORT` | `CommandFlags.Modal` | `"import"` | Interactive import dialog (`GeoImportWindow`). |
| `-HPGEOIMPORT` | `CommandFlags.Modal` | `"import-script"` | Scriptable command-line KML/KMZ import. |
| `-HPGEOIMAGE` | `CommandFlags.Modal` | `"image-script"` | Scriptable satellite imagery download & raster insertion. |
| `HPGEOINFO` | `CommandFlags.Modal \| CommandFlags.NoUndoMarker` | `"info"` | Displays geodetic metadata, version, active CRS, and settings. |

### 6.3 Verbatim Code Implementation

```csharp
using System;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;

namespace HPAutoCad.Loader;

/// <summary>
/// AutoCAD commands registered in the Default ALC. Each command forwards execution to the
/// appropriate delegate in HPAutoCad.dll loaded inside AppLoadContext.
/// </summary>
public sealed class HPGeoCommands
{
    [CommandMethod("HPGEO", CommandFlags.Modal)]
    public void Dialog() => Invoke("dialog", "HPGEO");

    [CommandMethod("HPGEODIALOG", CommandFlags.Modal)]
    public void DialogAlias() => Invoke("dialog", "HPGEODIALOG");

    [CommandMethod("-HPGEOKMZ", CommandFlags.Modal)]
    public void KmzScript() => Invoke("kmz-script", "-HPGEOKMZ");

    [CommandMethod("HPGEOKMZ", CommandFlags.Modal)]
    public void KmzScriptAlias() => Invoke("kmz-script", "HPGEOKMZ");

    [CommandMethod("HPGEOIMPORT", CommandFlags.Modal)]
    public void Import() => Invoke("import", "HPGEOIMPORT");

    [CommandMethod("-HPGEOIMPORT", CommandFlags.Modal)]
    public void ImportScript() => Invoke("import-script", "-HPGEOIMPORT");

    [CommandMethod("-HPGEOIMAGE", CommandFlags.Modal)]
    public void ImageScript() => Invoke("image-script", "-HPGEOIMAGE");

    [CommandMethod("HPGEOINFO", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
    public void Info() => Invoke("info", "HPGEOINFO");

    /// <summary>
    /// Invokes one add-in entry point delegate.
    /// If commandName is null, console messaging is suppressed (used for background tasks like Terminate).
    /// </summary>
    internal static void Invoke(string key, string? commandName)
    {
        var app = HPAutoCadLoaderApplication.App;
        var ed = Application.DocumentManager.MdiActiveDocument?.Editor;

        if (app is null)
        {
            var reason = HPAutoCadLoaderApplication.StartupError ?? "not started";
            if (commandName is not null)
            {
                ed?.WriteMessage($"\n{commandName}: HPAutoCad did not start ({reason}). See {LoaderLog.LogDirectory}\\loader.log\n");
            }
            return;
        }

        if (!app.TryGetValue(key, out var action))
        {
            if (commandName is not null)
            {
                ed?.WriteMessage($"\n{commandName}: entry point '{key}' missing in the add-in.\n");
            }
            return;
        }

        try
        {
            action.DynamicInvoke();
        }
        catch (Exception exception)
        {
            var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            LoaderLog.Write($"{commandName ?? key} failed", cause);
            if (commandName is not null)
            {
                ed?.WriteMessage($"\n{commandName} failed: {cause.Message}\n");
            }
        }
    }
}
```

---

## 7. Loader Application Specification (`HPAutoCadLoaderApplication.cs`)

### 7.1 Lifecycle Management
- **File**: `HPAutoCad/HPAutoCad.Loader/HPAutoCadLoaderApplication.cs`
- **Interfaces**: `Autodesk.AutoCAD.Runtime.IExtensionApplication`
- **Attributes**:
  ```csharp
  [assembly: ExtensionApplication(typeof(HPAutoCadLoaderApplication))]
  [assembly: CommandClass(typeof(HPGeoCommands))]
  ```

### 7.2 Detailed Execution Flow
```
AutoCAD Startup (acad.exe loads HPAutoCad.Loader.dll into Default ALC)
  │
  ▼
HPAutoCadLoaderApplication.Initialize()
  │
  ├── 1. Diagnostic Startup Log (Product, Version, CLR Framework)
  │
  ├── 2. Resolve App Assembly Path (`Contents\App\HPAutoCad.dll`)
  │      └── Throws FileNotFoundException if assembly missing
  │
  ├── 3. Instantiate AppLoadContext(appPath)
  │
  ├── 4. Load Assembly into Context (`context.LoadFromAssemblyPath(appPath)`)
  │
  ├── 5. Locate Entry Type (`HPAutoCad.Entry`) via Reflection
  │
  ├── 6. Invoke `Entry.Start(appDir, Action<string>)`
  │      └── Stores returned delegate dictionary in `App`
  │
  ├── 7. Install Shared Ribbon Tab Panel (`HPGeoRibbonTab.Install()`)
  │      └── Guarded with try-catch so Ribbon issues never disable commands
  │
  ▼
AutoCAD Ready / Idle
  │
AutoCAD Shutdown
  │
  ▼
HPAutoCadLoaderApplication.Terminate()
  │
  ├── 1. Uninstall Shared Ribbon Tab Panel (`HPGeoRibbonTab.Uninstall()`)
  ├── 2. Invoke `"stop"` delegate via `HPGeoCommands.Invoke("stop", null)`
  └── 3. Write Termination Log
```

### 7.3 Verbatim Code Implementation

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;
using HPAutoCad.Loader;
using HPAutoCad.Loader.Ribbon;

[assembly: ExtensionApplication(typeof(HPAutoCadLoaderApplication))]
[assembly: CommandClass(typeof(HPGeoCommands))]

namespace HPAutoCad.Loader;

/// <summary>
/// AutoCAD extension entry point for HPAutoCad.
/// Instantiates AppLoadContext, loads Contents\App\HPAutoCad.dll, retrieves entry delegates via
/// reflection, and mounts the HPGeoLink panel onto the shared HPAutoCad Ribbon tab.
/// </summary>
public sealed class HPAutoCadLoaderApplication : IExtensionApplication
{
    private const string AppFolder = "App";
    private const string AppAssemblyFile = "HPAutoCad.dll";
    private const string EntryTypeName = "HPAutoCad.Entry";
    private const string EntryMethodName = "Start";

    /// <summary>The delegates returned by the add-in; null until Initialize succeeds.</summary>
    internal static IReadOnlyDictionary<string, Delegate>? App { get; private set; }

    /// <summary>Recorded failure reason if add-in startup throws an exception.</summary>
    internal static string? StartupError { get; private set; }

    public static string Version { get; } =
        typeof(HPAutoCadLoaderApplication).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? typeof(HPAutoCadLoaderApplication).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";

    public void Initialize()
    {
        var loaderPath = Assembly.GetExecutingAssembly().Location;
        var loaderDir = Path.GetDirectoryName(loaderPath)!;
        var appPath = Path.Combine(loaderDir, AppFolder, AppAssemblyFile);
        var product = SystemVariable("PRODUCT");
        var acadVersion = SystemVariable("ACADVER");

        LoaderLog.Write($"HPAutoCad loader {Version} in {product} {acadVersion}: loader={loaderPath}; runtime={RuntimeInformation.FrameworkDescription}");

        try
        {
            if (!File.Exists(appPath))
            {
                throw new FileNotFoundException("Add-in assembly missing beside the loader", appPath);
            }

            var context = new AppLoadContext(appPath);
            var assembly = context.LoadFromAssemblyPath(appPath);
            var entry = assembly.GetType(EntryTypeName) 
                        ?? throw new TypeLoadException($"{EntryTypeName} not found in {AppAssemblyFile}");
            
            var start = entry.GetMethod(EntryMethodName, BindingFlags.Public | BindingFlags.Static)
                        ?? throw new MissingMethodException(EntryTypeName, EntryMethodName);

            // Invoke Entry.Start: supports both Start(appDir, Action<string>) and legacy Start(appDir, product, acadVersion)
            var parameters = start.GetParameters();
            object? handle = null;

            if (parameters.Length == 2 && parameters[1].ParameterType == typeof(Action<string>))
            {
                handle = start.Invoke(null, [Path.GetDirectoryName(appPath)!, new Action<string>(msg => LoaderLog.Write(msg))]);
            }
            else if (parameters.Length == 3)
            {
                handle = start.Invoke(null, [Path.GetDirectoryName(appPath)!, product, acadVersion]);
            }
            else
            {
                handle = start.Invoke(null, [Path.GetDirectoryName(appPath)!]);
            }

            App = handle as IReadOnlyDictionary<string, Delegate>
                  ?? throw new InvalidCastException($"{EntryTypeName}.{EntryMethodName} must return IReadOnlyDictionary<string, Delegate>");

            var alcName = AssemblyLoadContext.GetLoadContext(assembly)?.Name ?? "default";
            LoaderLog.Write($"HPAutoCad {Version} loaded in {product} {acadVersion}: add-in started in load context '{alcName}' with {App.Count} entry points");
        }
        catch (Exception exception)
        {
            var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            StartupError = cause.GetType().Name + ": " + cause.Message;
            LoaderLog.Write("HPAutoCad add-in failed to start", exception);
        }

        // Install shared Ribbon tab panel. Guarded independently so a Ribbon failure never crashes the add-in.
        try
        {
            HPGeoRibbonTab.Install();
        }
        catch (Exception exception)
        {
            LoaderLog.Write("ribbon install failed", exception);
        }
    }

    public void Terminate()
    {
        try
        {
            HPGeoRibbonTab.Uninstall();
            HPGeoCommands.Invoke("stop", null);
            LoaderLog.Write($"HPAutoCad loader {Version} terminated");
        }
        catch (Exception exception)
        {
            LoaderLog.Write("terminate failed", exception);
        }
    }

    internal static string SystemVariable(string name)
    {
        try
        {
            return Application.GetSystemVariable(name)?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
```

---

## 8. Diagnostic Logging Specification (`LoaderLog.cs`)

### 8.1 Dual-Write Strategy
To satisfy both modern repository layout rules (`%LocalAppData%\HPAutoCad\logs\loader.log`) and existing acceptance test scripts (`%LocalAppData%\HPGeo\logs\loader.log`), `LoaderLog` writes to the primary directory and replicates to the legacy directory in a fault-tolerant manner.

### 8.2 Verbatim Code Implementation

```csharp
using System;
using System.IO;

namespace HPAutoCad.Loader;

/// <summary>
/// BCL-only diagnostic logger for HPAutoCad.Loader.
/// Operates before add-in ALC initialization without any third-party dependencies.
/// Uses a dual-write mechanism to ensure compatibility with both HPAutoCad and HPGeo harnesses.
/// </summary>
internal static class LoaderLog
{
    public static readonly string PrimaryLogDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPAutoCad", "logs");

    public static readonly string LegacyLogDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPGeo", "logs");

    public static string LogDirectory => PrimaryLogDirectory;

    private static readonly object Gate = new();

    public static void Write(string message, Exception? exception = null)
    {
        try
        {
            lock (Gate)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var threadId = Environment.CurrentManagedThreadId;
                var line = $"{timestamp} [{threadId}] {message}" + (exception is null ? "" : Environment.NewLine + exception);

                WriteToFile(PrimaryLogDirectory, "loader.log", line);
                WriteToFile(LegacyLogDirectory, "loader.log", line);
            }
        }
        catch
        {
            // Diagnostics must never throw or disrupt CAD execution
        }
    }

    private static void WriteToFile(string directory, string filename, string content)
    {
        try
        {
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, filename), content + Environment.NewLine);
        }
        catch
        {
            // Silent fallback for secondary write failures
        }
    }
}
```

---

## 9. Ribbon Coordination Interface

### 9.1 Shared Tab Protocol (`HPAUTOCAD_MCP_TAB`)
In accordance with repository standards (documented in `AGENTS.md` and `PROJECT.md`), all HP add-ins for AutoCAD share a single ribbon tab:
- **Tab Id**: `HPAUTOCAD_MCP_TAB`
- **Tab Title**: `HPAutoCad`
- **Panels**:
  1. `HPAUTOCAD_MCP_PANEL` ("MCP") owned by `HPAutoCad.McpBridge.Loader`
  2. `HPGEOLINK_PANEL` ("HPGeoLink") owned by `HPAutoCad.Loader`

### 9.2 Lifecycle Resilience
`HPGeoRibbonTab` maintains robustness across dynamic AutoCAD UI events:
- **Workspace Switch (`WSCURRENT`)**: AutoCAD recreates ribbon tabs from the active CUIx file, discarding code-added elements. Listened via `Application.SystemVariableChanged` → queued to `Application.Idle` → `EnsureCreated()`.
- **Theme Switch (`COLORTHEME`)**: Flipping between Dark (0) and Light (1) triggers ribbon rebuild with theme-adapted vector icon brushes.
- **Late Ribbon Init (`ComponentManager.ItemInitialized`)**: Handles lazy initialization if the Ribbon was not loaded at add-in startup.
- **Independent Teardown**: `Uninstall()` removes only its own panel (`HPGEOLINK_PANEL`). It removes the tab only if no other panels remain.

---

## 10. Civil 3D Mirror Safety: Formal Proof & Analysis

### 10.1 Mirror Architecture Context
The Civil 3D integration in `HPCivil3d/` mirrors the AutoCAD bridge in `HPAutoCad/` (governed by ADR-01 option A). Mirror consistency is enforced by `HPCivil3d.McpBridge.Tests/MirrorTests.cs`.

### 10.2 Mathematical & Code Analysis of Test Constraints

In `HPCivil3d/HPCivil3d.McpBridge.Tests/MirrorTests.cs`:
```csharp
[Fact]
public void Every_AutoCAD_bridge_source_file_is_either_mirrored_or_an_owned_counterpart()
{
    var root = MirrorTokenTable.AutocadPath("");
    ...
    var unknown = MirrorTokenTable.SourceFiles(root, "HPAutoCad.McpBridge", "HPAutoCad.McpBridge.Loader", "HPAutoCad.Mcp.Server")
        .Where(path => !known.Contains(Path.GetFullPath(path)))
        .Select(path => Path.GetRelativePath(root, path))
        .ToArray();

    Assert.True(unknown.Length == 0, ...);
}
```
And in `MirrorTokenTable.SourceFiles`:
```csharp
public static IEnumerable<string> SourceFiles(string root, params string[] projects) => projects
    .SelectMany(project => Directory.EnumerateFiles(Path.Combine(root, project), "*.*", SearchOption.AllDirectories))
    ...
```

### 10.3 Proof of Zero Impact
1. **Explicit Project Whitelisting**:
   The test calls `MirrorTokenTable.SourceFiles(root, "HPAutoCad.McpBridge", "HPAutoCad.McpBridge.Loader", "HPAutoCad.Mcp.Server")`. It only scans these three directories. It **does not scan** `HPAutoCad.Loader`, `HPAutoCad`, `HPAutoCad.Core`, `HPAutoCad.TileFetch`, or `HPAutoCad.Tests`.
2. **Zero Modification to Whitelisted Files**:
   The creation of `HPAutoCad/HPAutoCad.Loader/` leaves all files in `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, and `HPAutoCad.Mcp.Server` untouched.
3. **Sha256 Hash Pins Preserved**:
   The `ownedCounterparts` in `HPCivil3d/tools/mirror-tokens.json` pin sha256 hashes of specific files (e.g. `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml`). Because `HPAutoCad.Loader` is a new project, these pinned files remain bit-for-bit identical.
4. **Conclusion**:
   Introducing `HPAutoCad/HPAutoCad.Loader/` has **zero effect** on `HPCivil3d.McpBridge.Tests`. All 55 mirror tests will remain 100% PASS.

---

## 11. Solution Integration & Build Layout

### 11.1 Integration into `HPAutoCad.slnx`
Add the new project to `HPAutoCad/HPAutoCad.slnx`:
```xml
  <Project Path="HPAutoCad.Loader/HPAutoCad.Loader.csproj" />
```

### 11.2 Target File Structure
```
HPAutoCad/
├── HPAutoCad.Loader/
│   ├── AppLoadContext.cs
│   ├── HPAutoCad.Loader.csproj
│   ├── HPAutoCadLoaderApplication.cs
│   ├── HPGeoCommands.cs
│   ├── LoaderLog.cs
│   └── Ribbon/
│       ├── HPGeoRibbonTab.cs
│       └── RibbonIcons.cs
├── HPAutoCad/
│   ├── Entry.cs
│   └── HPAutoCad.csproj
├── HPAutoCad.Core/
├── HPAutoCad.McpBridge.Loader/
├── HPAutoCad.McpBridge/
├── HPAutoCad.Mcp.Server/
└── HPAutoCad.slnx
```

---

## 12. Verification & Acceptance Criteria Matrix

| Check Item | Validation Command / Method | Expected Result |
|---|---|---|
| **Compilation** | `dotnet build HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj -c Debug` | Build succeeds with 0 errors and 0 warnings. |
| **No Host References** | `dotnet build HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj -c Debug -v:n` | Output contains no `HPAutoCad.dll` in compile inputs. |
| **Civil 3D Mirror Parity** | `dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests` | 55/55 mirror tests pass (100% green). |
| **Dynamic Loading** | Inspect `Contents\App\HPAutoCad.deps.json` | File contains entries for `WebView2`, `Mvvm`, and `runtimeTargets`. |
| **Native Library Packaging** | Inspect `Contents\App\runtimes\win-x64\native\` | `WebView2Loader.dll` is present for runtime resolution. |
| **Command Forwarding** | Execute `HPGEOINFO` in AutoCAD 2026 | Prints geodetic version and configuration without unhandled exceptions. |
| **Dual Logging** | Inspect `%LocalAppData%\HPAutoCad\logs\loader.log` & `%LocalAppData%\HPGeo\logs\loader.log` | Both logs contain identical startup and assembly load traces. |
| **Ribbon Creation** | Launch AutoCAD 2026 with `run-ribbon-check.ps1` | `HPAUTOCAD_MCP_TAB` hosts both `MCP` and `HPGeoLink` panels. |
