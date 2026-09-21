# Technical Specification & Implementation Plan: Milestone M2 Repack, Packaging & Solution Integration

**Milestone**: M2 (Add-In Layer, UI Feature & Solution Integration)  
**Deliverable**: `HPAutoCad/HPAutoCad/HPAutoCad.csproj`, ILRepack Target, Native Asset Bundling, Helper Copying, `HPAutoCad.Tests` Refactoring, and `HPAutoCad.slnx` Registration  
**Author**: `explorer_m2_repack`  
**Target Audience**: `orchestrator_3` / Workers (`worker_m2` / implementation agents)  
**Date**: 2026-09-20  

---

## 1. Executive Summary & Purpose

Milestone M2 establishes the main AutoCAD Add-In project `HPAutoCad` (`HPAutoCad/HPAutoCad/HPAutoCad.csproj`) targeting AutoCAD 2026 (.NET 8.0-windows, WPF, MVVM) and integrates it into the solution `HPAutoCad.slnx`.

This document specifies the exact implementation requirements for:
1. **`HPAutoCad.csproj` Definition**: Modern SDK-style project configured for AutoCAD 2026, WPF, MVVM, WebView2, and dynamic ALC loading.
2. **`RepackMaterialDesign` Target**: Deterministic, idempotent ILRepack merge of `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, and `Microsoft.Xaml.Behaviors.dll` directly into `HPAutoCad.dll` to eliminate BAML resource collisions across AutoCAD AssemblyLoadContexts.
3. **Native `WebView2Loader.dll` Bundling**: Handling native x64 assets and `deps.json` resolution for embedded Chromium browser maps.
4. **`CopyTileFetchHelper` Target**: Copying the companion out-of-process console utility `HPAutoCad.TileFetch.exe` into `$(OutDir)TileFetch\`.
5. **`HPAutoCad.Tests` Update & Support Class Retirement**: Referencing `HPAutoCad.csproj`, retiring 10 duplicate support classes from Milestone M1, and guaranteeing 100% pass rate (161 tests).
6. **`HPAutoCad.slnx` Registration**: Registering `HPAutoCad/HPAutoCad.csproj` in the solution.
7. **Civil 3D Mirror Invariant**: Guaranteeing that the 60 mirror assertions in `HPCivil3d.McpBridge.Tests` remain 100% untouched and passing.

---

## 2. Project Definition: `HPAutoCad/HPAutoCad/HPAutoCad.csproj`

### 2.1 File Location
- **Path**: `HPAutoCad/HPAutoCad/HPAutoCad.csproj`
- **Output Assembly**: `bin\$(Configuration)\net8.0-windows\HPAutoCad.dll`

### 2.2 Key Properties Explained
- **`TargetFramework`**: `net8.0-windows` — Required for WPF (`UseWPF=true`) and AutoCAD 2026 (built on .NET 8).
- **`EnableDynamicLoading`**: `true` — Essential for isolated AssemblyLoadContext loading. Generates `HPAutoCad.deps.json`, allowing `AssemblyDependencyResolver` to resolve runtime dependencies and native unmanaged libraries.
- **`AutocadPackageVersion`**: `[25.1.0]` — AutoCAD 2026 API reference assemblies.
- **`AutoCAD.NET`**: `ExcludeAssets="runtime" PrivateAssets="all"` — Compiles against the API but NEVER copies `AcCoreMgd.dll`, `AcDbMgd.dll`, or `AcMgd.dll` to the output folder. `acad.exe` provides these in the Default ALC.
- **`ILRepack`**: `PrivateAssets="all" ExcludeAssets="all" GeneratePathProperty="true"` — Generates `$(PkgILRepack)` so the custom build target can invoke `$(PkgILRepack)\tools\ILRepack.exe`.
- **`InternalsVisibleTo`**: Grants access to `HPAutoCad.Tests` so domain services and viewmodel internals can be tested without `acad.exe`.

### 2.3 Complete Project File XML Specification

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <!-- The add-in proper: drawing readers/writers, commands, WPF MVVM dialogs, and geodetic services.
             Loaded by HPAutoCad.Loader into its own AssemblyLoadContext from Contents\App\, so dependencies
             (CommunityToolkit.Mvvm, WebView2, HPAutoCad.Core) never collide with copies loaded by other plugins. -->
        <TargetFramework>net8.0-windows</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <UseWPF>true</UseWPF>
        <RootNamespace>HPAutoCad</RootNamespace>
        <AssemblyName>HPAutoCad</AssemblyName>
        <Configurations>Debug;Release</Configurations>
        <Version>0.1.0</Version>
        <!-- EnableDynamicLoading writes the deps.json the loader's AssemblyDependencyResolver reads and copies every
             dependency next to the DLL. -->
        <EnableDynamicLoading>true</EnableDynamicLoading>
        <!-- AutoCAD 2026 base release: R25.1 on .NET 8 — compile against it, never copy it (acad.exe supplies assemblies). -->
        <AutocadPackageVersion Condition="'$(AutocadPackageVersion)' == ''">[25.1.0]</AutocadPackageVersion>
        <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="AutoCAD.NET" Version="$(AutocadPackageVersion)" ExcludeAssets="runtime" PrivateAssets="all" />
        <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
        <!-- Control styles, merged into this assembly by the RepackMaterialDesign target below. -->
        <PackageReference Include="MaterialDesignThemes" Version="5.3.2" />
        <PackageReference Include="ILRepack" Version="2.0.46" PrivateAssets="all" ExcludeAssets="all" GeneratePathProperty="true" />
        <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.4191.47" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\HPAutoCad.Core\HPAutoCad.Core.csproj" />
    </ItemGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="HPAutoCad.Tests" />
    </ItemGroup>

    <ItemGroup>
        <!-- Build order only: the tile-fetch helper is a separate process, never a compile-time reference. -->
        <ProjectReference Include="..\HPAutoCad.TileFetch\HPAutoCad.TileFetch.csproj" ReferenceOutputAssembly="false" Private="false" />
    </ItemGroup>

    <!-- The helper ships beside the add-in as TileFetch\HPAutoCad.TileFetch.exe (+ its dll, deps and runtimeconfig), so the
         bundle deploy (which copies this project's whole output folder) carries it into Contents\App\TileFetch\.
         acad.exe may be denied the network by a per-program firewall rule; a process of its own is not. -->
    <Target Name="CopyTileFetchHelper" AfterTargets="Build" Condition="'$(DesignTimeBuild)' != 'true'">
        <ItemGroup>
            <_HelperFiles Include="$(MSBuildThisFileDirectory)..\HPAutoCad.TileFetch\bin\$(Configuration)\net8.0\**\*" />
        </ItemGroup>
        <Error Condition="'@(_HelperFiles)' == ''" Text="HPAutoCad.TileFetch output missing - build HPAutoCad.TileFetch first" />
        <Copy SourceFiles="@(_HelperFiles)" DestinationFiles="@(_HelperFiles->'$(OutDir)TileFetch\%(RecursiveDir)%(Filename)%(Extension)')" SkipUnchangedFiles="true" />
    </Target>

    <!-- The toolkit is merged into this assembly: WPF binds BAML types by simple name and takes the last-loaded copy across
         AssemblyLoadContexts, so a loose MaterialDesignThemes.Wpf.dll would be mixed with the HPAutoCad MCP bridge's in the same
         acad.exe (spike S0-B). Primary input = the obj DLL, so an incremental build never merges twice; the loose files are deleted
         before the loader's DeployBundle copies Contents\App. -->
    <Target Name="RepackMaterialDesign" AfterTargets="CopyFilesToOutputDirectory" Condition="Exists('$(OutDir)MaterialDesignThemes.Wpf.dll')">
        <PropertyGroup>
            <_RepackExe>$(PkgILRepack)\tools\ILRepack.exe</_RepackExe>
            <_RepackLib>@(ReferencePath->'%(RelativeDir)'->Distinct()->'/lib:&quot;%(Identity) &quot;', ' ')</_RepackLib>
        </PropertyGroup>
        <Exec Command="&quot;$(_RepackExe)&quot; /union /parallel /noRepackRes $(_RepackLib) /out:&quot;$(OutDir)$(AssemblyName).dll&quot; &quot;@(IntermediateAssembly->'%(FullPath)')&quot; &quot;$(OutDir)MaterialDesignThemes.Wpf.dll&quot; &quot;$(OutDir)MaterialDesignColors.dll&quot; &quot;$(OutDir)Microsoft.Xaml.Behaviors.dll&quot;"/>
        <Delete Files="$(OutDir)MaterialDesignThemes.Wpf.dll;$(OutDir)MaterialDesignColors.dll;$(OutDir)Microsoft.Xaml.Behaviors.dll;$(OutDir)MaterialDesignThemes.Wpf.xml"/>
        <Message Importance="high" Text="RepackMaterialDesign: toolkit merged into $(AssemblyName).dll"/>
    </Target>

</Project>
```

---

## 3. Deep Dive: `RepackMaterialDesign` Target & BAML Isolation

### 3.1 Architectural Root Cause (Spike S0-B Findings)
WPF resolves BAML resources (`pack://application:,,,/...`) and custom controls (`PackIcon`, `Card`, `BundledTheme`) by **simple assembly name** (`MaterialDesignThemes.Wpf`).
If two different add-ins or contexts inside `acad.exe` load loose copies of `MaterialDesignThemes.Wpf.dll` (e.g., `Contents\Bridge\` for MCP Bridge and `Contents\App\` for HPGeoLink):
- WPF binds to whichever copy was loaded last across all ALCs.
- Controls instantiated in ALC 1 attempt to use BAML templates from ALC 2, throwing cross-ALC type cast exceptions or corrupting styles (e.g., `SmartHint` template crashes).
- **The Repo Invariant**: `MaterialDesignThemes` is merged directly into each add-in assembly (`HPAutoCad.dll`, `HPAutoCad.McpBridge.dll`, `HPCivil3d.McpBridge.dll`, `HPRebar.dll`). No loose `MaterialDesignThemes.Wpf.dll` is ever deployed into AutoCAD or Revit plugins.

### 3.2 Target Mechanics & Command Breakdown
```xml
<Target Name="RepackMaterialDesign" AfterTargets="CopyFilesToOutputDirectory" Condition="Exists('$(OutDir)MaterialDesignThemes.Wpf.dll')">
    <PropertyGroup>
        <_RepackExe>$(PkgILRepack)\tools\ILRepack.exe</_RepackExe>
        <_RepackLib>@(ReferencePath->'%(RelativeDir)'->Distinct()->'/lib:&quot;%(Identity) &quot;', ' ')</_RepackLib>
    </PropertyGroup>
    <Exec Command="&quot;$(_RepackExe)&quot; /union /parallel /noRepackRes $(_RepackLib) /out:&quot;$(OutDir)$(AssemblyName).dll&quot; &quot;@(IntermediateAssembly->'%(FullPath)')&quot; &quot;$(OutDir)MaterialDesignThemes.Wpf.dll&quot; &quot;$(OutDir)MaterialDesignColors.dll&quot; &quot;$(OutDir)Microsoft.Xaml.Behaviors.dll&quot;"/>
    <Delete Files="$(OutDir)MaterialDesignThemes.Wpf.dll;$(OutDir)MaterialDesignColors.dll;$(OutDir)Microsoft.Xaml.Behaviors.dll;$(OutDir)MaterialDesignThemes.Wpf.xml"/>
    <Message Importance="high" Text="RepackMaterialDesign: toolkit merged into $(AssemblyName).dll"/>
</Target>
```

1. **`Condition="Exists('$(OutDir)MaterialDesignThemes.Wpf.dll')"`**:
   Guarantees the target only executes when the loose toolkit DLL is present in the output directory.
2. **`IntermediateAssembly` as Primary Input**:
   `"@(IntermediateAssembly->'%(FullPath)')"` points to `obj\Debug\net8.0-windows\HPAutoCad.dll`.
   - *Why this matters*: On incremental builds, merging into `$(OutDir)$(AssemblyName).dll` directly would cause double-merging. Using the clean `obj` DLL as the primary input guarantees that repacking is 100% idempotent.
3. **ILRepack Switches**:
   - `/union`: Merges duplicate types, duplicate attribute declarations, and forwarders.
   - `/parallel`: Uses multi-threaded IL processing.
   - `/noRepackRes`: Preserves resource names and BAML pack URIs without altering them to internal prefixed namespaces.
   - `$(_RepackLib)`: Passes all referenced library directories as `/lib:"..."` arguments so ILRepack resolves all external framework dependencies.
4. **Assemblies Merged**:
   - `MaterialDesignThemes.Wpf.dll`
   - `MaterialDesignColors.dll`
   - `Microsoft.Xaml.Behaviors.dll`
5. **Post-Repack Cleanup**:
   Deletes `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll`, and `MaterialDesignThemes.Wpf.xml` from `$(OutDir)`.
   - *Why this matters*: When `DeployBundle` recursively copies `$(OutDir)**\*` into `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\App\`, zero loose toolkit assemblies are deployed.
6. **Excluded from Merge (Kept Loose)**:
   - `CommunityToolkit.Mvvm.dll` — Has no BAML resources; cleanly isolated inside `AppLoadContext`.
   - `Microsoft.Web.WebView2.Core.dll` — Managed wrapper; cleanly isolated inside `AppLoadContext`.

### 3.3 Mandatory `ThemeInfo.cs` Attribute (Spike S0-A Requirement)
When `generic.baml` is merged into `HPAutoCad.dll`, WPF's ThemeDictionary loader requires the assembly to declare its theme resource location.
**Worker Action Item**: Create `HPAutoCad/HPAutoCad/Resources/Themes/ThemeInfo.cs`:
```csharp
using System.Windows;

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
```
*Failure Mode*: Without this attribute, `PackIcon` and `Card` fail to find their default styles at runtime, throwing XAML parse exceptions.

---

## 4. Native `WebView2Loader.dll` Bundling & Resolution

### 4.1 Asset Placement
`Microsoft.Web.WebView2` (v1.0.4191.47) NuGet package contains MSBuild targets that automatically copy native loader binaries into the project build output:
```
$(OutDir)
└── runtimes/
    ├── win-x64/
    │   └── native/
    │       └── WebView2Loader.dll   (64-bit native C++ DLL, ~160 KB)
    ├── win-x86/
    │   └── native/
    │       └── WebView2Loader.dll
    └── win-arm64/
        └── native/
            └── WebView2Loader.dll
```

### 4.2 How `deps.json` Enables Runtime Resolution
Because `<EnableDynamicLoading>true</EnableDynamicLoading>` is set in `HPAutoCad.csproj`, .NET SDK generates `bin\Debug\net8.0-windows\HPAutoCad.deps.json`.
It records the unmanaged runtime targets:
```json
"Microsoft.Web.WebView2/1.0.4191.47": {
  "runtimeTargets": {
    "runtimes/win-x64/native/WebView2Loader.dll": {
      "rid": "win-x64",
      "assetType": "native",
      "fileVersion": "1.0.4191.47"
    }
  }
}
```

### 4.3 ALC Resolution Chain
In `HPAutoCad.Loader` (Milestone M3):
1. `AppLoadContext` is instantiated with the path to `Contents\App\HPAutoCad.dll`:
   ```csharp
   _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
   ```
2. When WPF initializes `WebView2`, the control calls `LoadLibrary("WebView2Loader.dll")`.
3. The runtime triggers:
   ```csharp
   protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
   {
       var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
       return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
   }
   ```
4. `_resolver.ResolveUnmanagedDllToPath("WebView2Loader.dll")` inspects `HPAutoCad.deps.json`, matches `win-x64`, and resolves the full absolute path:
   `...\Contents\App\runtimes\win-x64\native\WebView2Loader.dll`.
5. `LoadUnmanagedDllFromPath` loads the native DLL cleanly into the AutoCAD process without requiring `WebView2Loader.dll` to be placed in `C:\Windows\System32` or AutoCAD's root directory.

### 4.4 Packaging Guardrail
Any bundle deployment script or target MUST recursively copy `$(OutDir)**\*` (or specifically `runtimes\win-x64\native\WebView2Loader.dll`) into `Contents\App\`.

---

## 5. Companion Process: `CopyTileFetchHelper` MSBuild Target

### 5.1 Architecture & Firewall Bypass
Workstations running AutoCAD in enterprise environments often have firewall policies preventing `acad.exe` from connecting to the public internet (causing `WSAEACCES` socket exceptions).
To fetch satellite map tiles reliably:
- Tile fetching is executed out-of-process via `HPAutoCad.TileFetch.exe`.
- `HPAutoCad.TileFetch.exe` runs under its own executable identity, completely bypassing per-application firewall blocks on `acad.exe`.
- It executes in background, writing tiles to the local disk cache (`%LocalAppData%\HPGeo\tiles\...`) and reporting progress over `stdout`.

### 5.2 Build Order & Target Execution
In `HPAutoCad.csproj`:
```xml
<ItemGroup>
    <!-- Build order only: the tile-fetch helper is a separate process, never a compile-time reference. -->
    <ProjectReference Include="..\HPAutoCad.TileFetch\HPAutoCad.TileFetch.csproj" ReferenceOutputAssembly="false" Private="false" />
</ItemGroup>

<Target Name="CopyTileFetchHelper" AfterTargets="Build" Condition="'$(DesignTimeBuild)' != 'true'">
    <ItemGroup>
        <_HelperFiles Include="$(MSBuildThisFileDirectory)..\HPAutoCad.TileFetch\bin\$(Configuration)\net8.0\**\*" />
    </ItemGroup>
    <Error Condition="'@(_HelperFiles)' == ''" Text="HPAutoCad.TileFetch output missing - build HPAutoCad.TileFetch first" />
    <Copy SourceFiles="@(_HelperFiles)" DestinationFiles="@(_HelperFiles->'$(OutDir)TileFetch\%(RecursiveDir)%(Filename)%(Extension)')" SkipUnchangedFiles="true" />
</Target>
```

### 5.3 Output Layout
When built, `$(OutDir)TileFetch\` contains:
- `HPAutoCad.TileFetch.exe`
- `HPAutoCad.TileFetch.dll`
- `HPAutoCad.TileFetch.deps.json`
- `HPAutoCad.TileFetch.runtimeconfig.json`
- `HPAutoCad.Core.dll`
- `HPAutoCad.TileFetch.pdb` (Debug)

### 5.4 Host Service Lookup Contract
In `HPAutoCad/HPGeoLink/Service/HelperTileFetcher.cs`:
```csharp
public const string HelperFolder = "TileFetch";
public const string HelperExe = "HPAutoCad.TileFetch.exe";

public static string? DefaultExePath()
{
    try
    {
        var dir = Path.GetDirectoryName(typeof(HelperTileFetcher).Assembly.Location);
        if (dir is null) return null;
        var path = Path.Combine(dir, HelperFolder, HelperExe);
        return File.Exists(path) ? path : null;
    }
    catch (Exception)
    {
        return null;
    }
}
```
This guarantees that `HelperTileFetcher` automatically discovers `Contents\App\TileFetch\HPAutoCad.TileFetch.exe`.

---

## 6. Unit Test Suite Refactoring: `HPAutoCad.Tests`

### 6.1 Baseline Verification
The geodetic unit test suite in `HPAutoCad/HPAutoCad.Tests/` currently contains **161 unit tests**.
Running `dotnet run --project HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj` produces:
```
Test run summary: Passed!
  total: 161
  failed: 0
  succeeded: 158
  skipped: 3  (set HPGEO_LIVE_TILES=1 to hit real provider)
  duration: ~1.1s
```

### 6.2 The Milestone M1 Support Hack
During Milestone M1, `HPAutoCad.csproj` did not exist yet. To allow ViewModels and imagery pipeline tests to compile and verify against `HPAutoCad.Core`, **10 duplicate support classes** were placed into `HPAutoCad.Tests/HPGeoLink/Support/`.

### 6.3 Step-by-Step Refactoring Instructions for Worker

#### Step 1: Update `HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj`
Add project reference to `..\HPAutoCad\HPAutoCad.csproj`:
```xml
    <ItemGroup>
        <ProjectReference Include="..\HPAutoCad.Core\HPAutoCad.Core.csproj" />
        <ProjectReference Include="..\HPAutoCad\HPAutoCad.csproj" />
        <ProjectReference Include="..\HPAutoCad.TileFetch\HPAutoCad.TileFetch.csproj" ReferenceOutputAssembly="false" />
    </ItemGroup>
```

#### Step 2: Delete Duplicate Files in `HPAutoCad.Tests/HPGeoLink/Support/`
Permanently delete the 10 duplicate support files and remove the `Support/` directory:
1. `HPAutoCad.Tests/HPGeoLink/Support/CrsSelectionViewModel.cs`
2. `HPAutoCad.Tests/HPGeoLink/Support/GeoExportItems.cs`
3. `HPAutoCad.Tests/HPGeoLink/Support/GeoExportViewModel.Commands.cs`
4. `HPAutoCad.Tests/HPGeoLink/Support/GeoExportViewModel.cs`
5. `HPAutoCad.Tests/HPGeoLink/Support/GeoImportViewModel.cs`
6. `HPAutoCad.Tests/HPGeoLink/Support/HPGeoLog.cs`
7. `HPAutoCad.Tests/HPGeoLink/Support/HelperTileFetcher.cs`
8. `HPAutoCad.Tests/HPGeoLink/Support/IGeoExportShell.cs`
9. `HPAutoCad.Tests/HPGeoLink/Support/ImageryPipeline.cs`
10. `HPAutoCad.Tests/HPGeoLink/Support/TileStitcher.cs`

#### Step 3: Update `using` Statements in Test Files
Only **4 test files** in `HPAutoCad.Tests/HPGeoLink/` reference `HPAutoCad.Tests.HPGeoLink.Support`. Update them as follows:

1. **`GeoExportViewModelTests.cs`**:
   Replace:
   ```csharp
   using HPAutoCad.Tests.HPGeoLink.Support;
   ```
   With:
   ```csharp
   using HPAutoCad.HPGeoLink.Model;
   using HPAutoCad.HPGeoLink.Service;
   using HPAutoCad.HPGeoLink.ViewModel;
   ```

2. **`GeoImportViewModelTests.cs`**:
   Replace:
   ```csharp
   using HPAutoCad.Tests.HPGeoLink.Support;
   ```
   With:
   ```csharp
   using HPAutoCad.HPGeoLink.Model;
   using HPAutoCad.HPGeoLink.Service;
   using HPAutoCad.HPGeoLink.ViewModel;
   ```

3. **`TileFetchHelperTests.cs`**:
   Replace:
   ```csharp
   using HPAutoCad.Tests.HPGeoLink.Support;
   ```
   With:
   ```csharp
   using HPAutoCad.HPGeoLink.Service;
   ```

4. **`ImageryPipelineTests.cs`**:
   Replace:
   ```csharp
   using HPAutoCad.Tests.HPGeoLink.Support;
   ```
   With:
   ```csharp
   using HPAutoCad.HPGeoLink.Service;
   ```

#### Step 4: Verification Command
Execute:
```bash
dotnet run --project HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj
```
**Acceptance Criteria**: Must yield `total: 161, failed: 0, succeeded: 158, skipped: 3`.

---

## 7. Solution Integration: `HPAutoCad/HPAutoCad.slnx`

### 7.1 Solution Format
The repository uses the modern `.slnx` solution format (XML-based, supported natively by .NET 9/10 SDK and Visual Studio / Rider).

### 7.2 Updated `HPAutoCad.slnx` Specification
Register `<Project Path="HPAutoCad/HPAutoCad.csproj" />` directly below `HPAutoCad.Core`:

```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="README.md" />
  </Folder>
  <!-- AutoCAD MCP: the bridge (loader + isolated bridge) that runs inside acad.exe and the stdio server exe the
       host AI launches. Shared engine, always referenced by relative path, never copied:
         ../McpShared/HPRebar.Mcp.Contracts
         ../McpShared/HPRebar.McpBridge.Core
         ../McpShared/HPRebar.Mcp.Server.Core -->
  <Project Path="HPAutoCad.Core/HPAutoCad.Core.csproj" />
  <Project Path="HPAutoCad/HPAutoCad.csproj" />
  <Project Path="HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj" />
  <Project Path="HPAutoCad.Tests/HPAutoCad.Tests.csproj" />
  <Project Path="HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj" />
  <Project Path="HPAutoCad.Aec/HPAutoCad.Aec.csproj" />
  <Project Path="HPAutoCad.Aec.Tests/HPAutoCad.Aec.Tests.csproj" />
  <Project Path="HPAutoCad.McpBridge/HPAutoCad.McpBridge.csproj" />
  <Project Path="HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.csproj" />
  <Project Path="HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

---

## 8. Civil 3D Mirror Invariant & Safety Proof

### 8.1 The Mirror Fence Mechanism
Civil 3D 2026 is an AutoCAD vertical running on the identical `acad.exe` binary (AutoCAD.NET 25.1, .NET 8).
The Civil 3D MCP bridge in `HPCivil3d/` mirrors the AutoCAD MCP bridge in `HPAutoCad/` line-for-line, enforced by `HPCivil3d.McpBridge.Tests/MirrorTests.cs` using token transformations defined in `HPCivil3d/tools/mirror-tokens.json`.

### 8.2 Scope of Mirror Tests
As proven in `HPCivil3d.McpBridge.Tests/MirrorTests.cs` (lines 68 & 87):
```csharp
MirrorTokenTable.SourceFiles(root, "HPAutoCad.McpBridge", "HPAutoCad.McpBridge.Loader", "HPAutoCad.Mcp.Server")
```
The mirror tests strictly scan **only three project folders**:
1. `HPAutoCad/HPAutoCad.McpBridge/`
2. `HPAutoCad/HPAutoCad.McpBridge.Loader/`
3. `HPAutoCad/HPAutoCad.Mcp.Server/`

### 8.3 Invariant Rule & Proof of Zero Impact
- **Proof**: `HPAutoCad/HPAutoCad/` (the Add-In UI project), `HPAutoCad.Core`, `HPAutoCad.TileFetch`, and `HPAutoCad.Tests` are outside the mirror search scope.
- **Rule**: Implementation workers for Milestone M2 MUST NOT modify any files inside `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, or `HPAutoCad.Mcp.Server`.
- **Baseline Verification**:
  ```bash
  dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj
  ```
  Current status: **60 tests passed, 0 failed, 0 skipped**.
- **Continuous Gate**: After completing M2 implementation, `HPCivil3d.McpBridge.Tests` must be re-run to verify that all 60 tests still pass 100%.

---

## 9. Implementation Sequence for Worker (`worker_m2`)

To execute Milestone M2 packaging cleanly without circular dependency errors:

1. **Step 1 — Create Project File**:
   Create `HPAutoCad/HPAutoCad/HPAutoCad.csproj` with the exact XML from Section 2.3.
2. **Step 2 — Register in Solution**:
   Update `HPAutoCad/HPAutoCad.slnx` to include `<Project Path="HPAutoCad/HPAutoCad.csproj" />` per Section 7.2.
3. **Step 3 — Add Mandatory `ThemeInfo.cs`**:
   Ensure `HPAutoCad/HPAutoCad/Resources/Themes/ThemeInfo.cs` contains:
   ```csharp
   using System.Windows;

   [assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
   ```
4. **Step 4 — Compile Solution**:
   Run `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` to ensure `HPAutoCad.csproj`, `HPAutoCad.TileFetch`, and `CopyTileFetchHelper` compile and execute.
5. **Step 5 — Update `HPAutoCad.Tests.csproj`**:
   Add `<ProjectReference Include="..\HPAutoCad\HPAutoCad.csproj" />`.
6. **Step 6 — Delete Support Classes**:
   Delete all 10 files in `HPAutoCad.Tests/HPGeoLink/Support/` and delete the folder.
7. **Step 7 — Update Test Usings**:
   Update usings in `GeoExportViewModelTests.cs`, `GeoImportViewModelTests.cs`, `TileFetchHelperTests.cs`, `ImageryPipelineTests.cs`.
8. **Step 8 — Verify Test Suites**:
   - `dotnet run --project HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj` $\rightarrow$ 161 passed (158 passed, 3 skipped).
   - `dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj` $\rightarrow$ 60 passed.

---

## 10. Acceptance Criteria Checklist for M2 Repack

- [ ] `HPAutoCad/HPAutoCad/HPAutoCad.csproj` exists, targets `net8.0-windows`, has `EnableDynamicLoading=true`, `UseWPF=true`.
- [ ] `AutoCAD.NET 25.1.0` has `ExcludeAssets="runtime" PrivateAssets="all"`.
- [ ] `RepackMaterialDesign` target merges `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll` into `HPAutoCad.dll` and deletes loose toolkit files.
- [ ] `runtimes\win-x64\native\WebView2Loader.dll` is emitted in `$(OutDir)`.
- [ ] `CopyTileFetchHelper` target copies `HPAutoCad.TileFetch.exe` and outputs into `$(OutDir)TileFetch\`.
- [ ] `HPAutoCad/HPAutoCad.slnx` builds cleanly with 0 errors.
- [ ] `HPAutoCad.Tests` references `HPAutoCad.csproj`, all 10 files in `HPGeoLink/Support/` are deleted, and all 161 tests pass.
- [ ] `HPCivil3d.McpBridge.Tests` passes 60/60 tests with zero regressions.
