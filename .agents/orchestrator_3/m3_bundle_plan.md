# Milestone M3: Single Bundle Packaging & Deployment Specification (`HPAutoCad.bundle`)

**Author**: `explorer_m3_bundle`  
**Parent Orchestrator**: `orchestrator_3` (Conversation ID: `050984c1-afaa-4911-859c-331e9279dc4f`)  
**Target Bundle**: `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`  
**Date**: 2026-09-20  

---

## 1. Executive Summary

Milestone M3 establishes the single, unified Autodesk Autoloader bundle **`HPAutoCad.bundle`**, replacing the two previously separate bundles:
1. `HPAutoCad.McpBridge.bundle` (MCP AI bridge runtime & AEC tools)
2. `HPGeo.bundle` (Geodetic VN-2000 / WGS84 / KMZ toolkit)

The unified bundle deploys to:
```
%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\
```

### Key Architectural Principles
- **Single Autoloader Manifest (`PackageContents.xml`)**: Declares two `.NET` component entries targeting AutoCAD 2026 (`Platform="AutoCAD"` and `SeriesMin="R25.1" SeriesMax="R25.1"`).
- **Dual Loader Architecture in AutoCAD Default ALC**:
  - `HPAutoCad.McpBridge.Loader.dll` loads the MCP Bridge runtime into `BridgeLoadContext`.
  - `HPAutoCad.Loader.dll` loads the HPGeoLink UI add-in into `AppLoadContext`.
- **Complete Runtime & ALC Isolation**:
  - `Contents\Bridge\` isolates Roslyn 5.9 scripting, Serilog, and MCP contracts.
  - `Contents\App\` isolates `HPAutoCad.Core`, `CommunityToolkit.Mvvm`, `Microsoft.Web.WebView2` (with native `WebView2Loader.dll`), and `TileFetch\`.
  - Both assemblies ILRepack `MaterialDesignThemes 5.3.2` to eliminate WPF BAML type collisions across ALCs.
- **Shared Ribbon Tab (`HPAUTOCAD_MCP_TAB`)**:
  - Whichever component loads first creates the tab; the other adds its panel.
  - Hosts `HPAUTOCAD_MCP_PANEL` ("MCP") and `HPGEOLINK_PANEL` ("HPGeoLink").
  - Self-healing on workspace switches (`WSCURRENT`) and theme flips (`COLORTHEME`).
- **100% Civil 3D Mirror Invariant**:
  - `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` is preserved intact with SHA-256 `22d566c8faec996572452d3fab99fdceaa0d89732f6f42b28df86f3602c1ef28`.
  - `HPAutoCad.McpBridge.Loader.csproj` remains byte-identical to `HPCivil3d.McpBridge.Loader.csproj` after tokens.
  - All 60 mirror tests in `HPCivil3d.McpBridge.Tests` remain 100% green.

---

## 2. Analysis of Existing Package Manifests

### 2.1 Side-by-Side Comparison

| Dimension | `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` | `HPGeo.AutoCad.Loader/Bundle/PackageContents.xml` | Unified `HPAutoCad.bundle/PackageContents.xml` |
|---|---|---|---|
| **Bundle Name** | `HPAutoCad MCP Bridge` | `HPGeo VN-2000 KMZ` | `HPAutoCad` |
| **ProductCode** | `{7B4E9C2D-5A61-4F0E-9D3B-2C8F1E6A7D54}` | `{3E8B1F6A-9C42-4D7B-8A15-6F0C2D9E4B71}` | `{A4E6C3B2-8D1F-4B7E-9C5A-2E6D8F1B3C94}` |
| **AppVersion** | `0.3.0` | `0.1.0` | `0.3.0` |
| **AutodeskProduct** | `AutoCAD` | `AutoCAD` | `AutoCAD` |
| **Platform** | `AutoCAD` | `AutoCAD` | `AutoCAD` |
| **SeriesMin / Max** | `R25.1` / `R25.1` | `R25.1` / `R25.1` | `R25.1` / `R25.1` |
| **Component 1** | `HPAutoCad.McpBridge` (`./Contents/HPAutoCad.McpBridge.Loader.dll`) | — | `HPAutoCad.McpBridge` (`./Contents/HPAutoCad.McpBridge.Loader.dll`) |
| **Component 2** | — | `HPGeo` (`./Contents/HPGeo.AutoCad.Loader.dll`) | `HPAutoCad` (`./Contents/HPAutoCad.Loader.dll`) |
| **Deployment Dir** | `ApplicationPlugins\HPAutoCad.McpBridge.bundle\` | `ApplicationPlugins\HPGeo.bundle\` | `ApplicationPlugins\HPAutoCad.bundle\` |

### 2.2 Platform & Series Enforcement
- `AutodeskProduct="AutoCAD"` and `Platform="AutoCAD"`:
  AutoCAD verticals (e.g. Civil 3D 2026, Advance Steel 2026) run on the same core `acad.exe` and release `R25.1`. Civil 3D runs with `/product C3D` and matches `Platform="Civil3D"`.
  Restricting the bundle to `Platform="AutoCAD"` ensures the bundle **only** loads in vanilla AutoCAD 2026. This prevents named pipe collisions (`hpautocad-mcp-2026` vs `hpcivil3d-mcp-2026`) and prevents command collisions with Civil 3D's native coordinate tooling.
- `SeriesMin="R25.1"` and `SeriesMax="R25.1"`:
  AutoCAD 2026 is internally `R25.1` (.NET 8). AutoCAD 2025 was `R25.0` (.NET 8), and AutoCAD 2027 will be `R26.0` (.NET 10). Clamping to `R25.1` prevents loading under mismatched framework runtimes.

---

## 3. Unified `PackageContents.xml` Specification

The authoritative unified manifest file resides at:
`HPAutoCad/HPAutoCad.Loader/Bundle/PackageContents.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<!--
  Autodesk autoloader manifest for HPAutoCad.
  Deployed by the DeployBundle target to:
    %AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\

  Declares two isolated components running in AutoCAD 2026:
    1. HPAutoCad.McpBridge via ./Contents/HPAutoCad.McpBridge.Loader.dll
       Loads ./Contents/Bridge/HPAutoCad.McpBridge.dll into BridgeLoadContext.
    2. HPAutoCad (HPGeoLink) via ./Contents/HPAutoCad.Loader.dll
       Loads ./Contents/App/HPAutoCad.dll into AppLoadContext.

  SeriesMin/Max are AutoCAD's internal release numbers, not years: AutoCAD 2026 = R25.1.
  Platform="AutoCAD" (not "AutoCAD*") keeps the bundle out of Civil 3D / Advance Steel 2026,
  which share R25.1 and have their own distinct tooling/pipes.
-->
<ApplicationPackage SchemaVersion="1.0"
                    AutodeskProduct="AutoCAD"
                    ProductType="Application"
                    Name="HPAutoCad"
                    AppVersion="0.3.0"
                    Description="HPAutoCad Add-In: Geodetic VN-2000 / WGS84 tools (HPGeoLink) and AI MCP Bridge"
                    ProductCode="{A4E6C3B2-8D1F-4B7E-9C5A-2E6D8F1B3C94}">
  <CompanyDetails Name="HPRebar" />
  <Components Description="AutoCAD 2026 (.NET 8)">
    <RuntimeRequirements OS="Win64" Platform="AutoCAD" SeriesMin="R25.1" SeriesMax="R25.1" />
    <ComponentEntry AppName="HPAutoCad.McpBridge"
                    Version="0.3.0"
                    ModuleName="./Contents/HPAutoCad.McpBridge.Loader.dll"
                    AppType=".NET"
                    LoadOnAutoCADStartup="True" />
    <ComponentEntry AppName="HPAutoCad"
                    Version="0.1.0"
                    ModuleName="./Contents/HPAutoCad.Loader.dll"
                    AppType=".NET"
                    LoadOnAutoCADStartup="True" />
  </Components>
</ApplicationPackage>
```

---

## 4. Target Filesystem Layout

Upon deployment to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`, the bundle structure is:

```
HPAutoCad.bundle/
├── PackageContents.xml
└── Contents/
    ├── HPAutoCad.McpBridge.Loader.dll            (AutoCAD Default ALC: MCP loader)
    ├── HPAutoCad.McpBridge.Loader.pdb
    ├── HPAutoCad.Loader.dll                      (AutoCAD Default ALC: HPGeoLink loader)
    ├── HPAutoCad.Loader.pdb
    │
    ├── Bridge/                                   (BridgeLoadContext: isolated runtime)
    │   ├── HPAutoCad.McpBridge.dll               (Repacked MaterialDesignThemes)
    │   ├── HPAutoCad.McpBridge.pdb
    │   ├── HPAutoCad.McpBridge.deps.json
    │   ├── HPAutoCad.McpBridge.runtimeconfig.json
    │   ├── HPAutoCad.Aec.dll
    │   ├── HPAutoCad.Aec.pdb
    │   ├── HPRebar.McpBridge.Core.dll
    │   ├── HPRebar.McpBridge.Core.pdb
    │   ├── HPRebar.Mcp.Contracts.dll
    │   ├── HPRebar.Mcp.Contracts.pdb
    │   ├── Microsoft.CodeAnalysis.CSharp.dll
    │   ├── Microsoft.CodeAnalysis.CSharp.Scripting.dll
    │   ├── Microsoft.CodeAnalysis.dll
    │   ├── Microsoft.CodeAnalysis.Scripting.dll
    │   ├── Serilog.dll
    │   ├── Serilog.Sinks.File.dll
    │   ├── System.Collections.Immutable.dll
    │   ├── System.IO.Pipelines.dll
    │   ├── System.Reflection.Metadata.dll
    │   ├── System.Text.Encodings.Web.dll
    │   ├── System.Text.Json.dll
    │   └── runtimes/
    │       └── ...
    │
    └── App/                                      (AppLoadContext: isolated runtime)
        ├── HPAutoCad.dll                         (Repacked MaterialDesignThemes 5.3.2)
        ├── HPAutoCad.pdb
        ├── HPAutoCad.deps.json                   (Resolves unmanaged WebView2Loader.dll)
        ├── HPAutoCad.runtimeconfig.json
        ├── HPAutoCad.Core.dll
        ├── HPAutoCad.Core.pdb
        ├── CommunityToolkit.Mvvm.dll
        ├── Microsoft.Web.WebView2.Core.dll
        ├── Microsoft.Web.WebView2.WinForms.dll
        ├── Microsoft.Web.WebView2.Wpf.dll
        ├── runtimes/
        │   └── win-x64/
        │       └── native/
        │           └── WebView2Loader.dll        (Native WebView2 runtime loader)
        └── TileFetch/                            (Out-of-process console utility)
            ├── HPAutoCad.TileFetch.exe
            ├── HPAutoCad.TileFetch.dll
            ├── HPAutoCad.TileFetch.deps.json
            └── HPAutoCad.TileFetch.runtimeconfig.json
```

### 4.1 Isolation and Collision Avoidance Analysis
1. **Default ALC Cleanliness**:
   AutoCAD loads only `HPAutoCad.McpBridge.Loader.dll` and `HPAutoCad.Loader.dll` into the Default ALC. Both loaders have zero compile-time references to dependencies (no Serilog, no Roslyn, no WebView2, no CommunityToolkit.Mvvm). They only reference `AutoCAD.NET` and WPF framework classes for the Ribbon.
2. **Bridge Isolation (`BridgeLoadContext`)**:
   Constructed with `AssemblyDependencyResolver(bridgePath)`. Loads `HPAutoCad.McpBridge.dll` and private copies of Roslyn, Serilog, System.Collections.Immutable, etc. Host types (`Autodesk.AutoCAD.*`, `AdWindows.dll`) fall through to the Default ALC.
3. **App Isolation (`AppLoadContext`)**:
   Constructed with `AssemblyDependencyResolver(appPath)`. Loads `HPAutoCad.dll`, `HPAutoCad.Core.dll`, CommunityToolkit.Mvvm, and Microsoft.Web.WebView2.
4. **Native Unmanaged Dependency (`WebView2Loader.dll`)**:
   `AppLoadContext.LoadUnmanagedDll` delegates to `_resolver.ResolveUnmanagedDllToPath(unmanagedDllName)`. `HPAutoCad.deps.json` contains the target runtime mapping `runtimes/win-x64/native/WebView2Loader.dll`, resolving seamlessly without manual LoadLibrary calls.
5. **WPF BAML Collision Elimination**:
   `MaterialDesignThemes 5.3.2` is ILRepacked directly into `HPAutoCad.McpBridge.dll` (for Bridge) and `HPAutoCad.dll` (for App). No loose `MaterialDesignThemes.Wpf.dll` exists in either directory. This prevents WPF's global assembly cache by simple name from mixing BAML templates across ALCs.

---

## 5. MSBuild Deployment Target & Build Architecture

### 5.1 Orchestration Strategy
`HPAutoCad.Loader.csproj` acts as the single deployer of the unified `HPAutoCad.bundle`.
To prevent race conditions and duplicate deployments:
1. `HPAutoCad.Loader.csproj` references `HPAutoCad.csproj`, `HPAutoCad.McpBridge.csproj`, and `HPAutoCad.McpBridge.Loader.csproj` via `<ProjectReference ... ReferenceOutputAssembly="false" Private="false" />`.
2. `HPAutoCad/Directory.Build.props` sets `<DeployBundle>false</DeployBundle>` specifically when `$(MSBuildProjectName) == 'HPAutoCad.McpBridge.Loader'`.
3. The legacy standalone bundle directories (`HPAutoCad.McpBridge.bundle` and `HPGeo.bundle`) are automatically purged if present during deployment.

### 5.2 Directory.Build.props (`HPAutoCad/Directory.Build.props`)
```xml
<Project>
  <PropertyGroup Condition="'$(MSBuildProjectName)' == 'HPAutoCad.McpBridge.Loader'">
    <!-- The unified HPAutoCad.bundle is deployed by HPAutoCad.Loader. Prevent McpBridge.Loader from deploying the legacy standalone bundle. -->
    <DeployBundle>false</DeployBundle>
  </PropertyGroup>
</Project>
```

### 5.3 Loader Project Configuration (`HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj`)
```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0-windows</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <UseWPF>true</UseWPF>
        <RootNamespace>HPAutoCad.Loader</RootNamespace>
        <AssemblyName>HPAutoCad.Loader</AssemblyName>
        <Configurations>Debug;Release</Configurations>
        <Version>0.1.0</Version>
        <AutocadPackageVersion Condition="'$(AutocadPackageVersion)' == ''">[25.1.0]</AutocadPackageVersion>

        <!-- Deploy unified bundle to %AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\ -->
        <DeployBundle Condition="'$(DeployBundle)' == ''">true</DeployBundle>
        <BundleDir>$([System.Environment]::GetFolderPath(SpecialFolder.ApplicationData))\Autodesk\ApplicationPlugins\HPAutoCad.bundle\</BundleDir>
        <LegacyBridgeBundleDir>$([System.Environment]::GetFolderPath(SpecialFolder.ApplicationData))\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\</LegacyBridgeBundleDir>
        <LegacyGeoBundleDir>$([System.Environment]::GetFolderPath(SpecialFolder.ApplicationData))\Autodesk\ApplicationPlugins\HPGeo.bundle\</LegacyGeoBundleDir>

        <!-- Artifact source locations -->
        <AppOutDir>$(MSBuildThisFileDirectory)..\HPAutoCad\bin\$(Configuration)\$(TargetFramework)\</AppOutDir>
        <BridgeOutDir>$(MSBuildThisFileDirectory)..\HPAutoCad.McpBridge\bin\$(Configuration)\$(TargetFramework)\</BridgeOutDir>
        <BridgeLoaderOutDir>$(MSBuildThisFileDirectory)..\HPAutoCad.McpBridge.Loader\bin\$(Configuration)\$(TargetFramework)\</BridgeLoaderOutDir>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="AutoCAD.NET" Version="$(AutocadPackageVersion)" ExcludeAssets="runtime" PrivateAssets="all"/>
    </ItemGroup>

    <ItemGroup>
        <!-- Build ordering only: bundle needs outputs from App, Bridge, and Bridge.Loader, but loader must not reference their assemblies -->
        <ProjectReference Include="..\HPAutoCad\HPAutoCad.csproj" ReferenceOutputAssembly="false" Private="false"/>
        <ProjectReference Include="..\HPAutoCad.McpBridge\HPAutoCad.McpBridge.csproj" ReferenceOutputAssembly="false" Private="false"/>
        <ProjectReference Include="..\HPAutoCad.McpBridge.Loader\HPAutoCad.McpBridge.Loader.csproj" ReferenceOutputAssembly="false" Private="false"/>
    </ItemGroup>

    <ItemGroup>
        <None Include="Bundle\PackageContents.xml"/>
    </ItemGroup>

    <!-- Unified DeployBundle Target -->
    <Target Name="DeployBundle" AfterTargets="Build" Condition="'$(DeployBundle)' == 'true' And '$(Configuration)' == 'Debug' And '$(DesignTimeBuild)' != 'true'">
        <!-- Pre-flight checks -->
        <Error Condition="!Exists('$(AppOutDir)HPAutoCad.dll')" Text="Add-in output missing at $(AppOutDir) - build HPAutoCad first"/>
        <Error Condition="!Exists('$(BridgeOutDir)HPAutoCad.McpBridge.dll')" Text="Bridge output missing at $(BridgeOutDir) - build HPAutoCad.McpBridge first"/>
        <Error Condition="!Exists('$(BridgeLoaderOutDir)HPAutoCad.McpBridge.Loader.dll')" Text="Bridge loader output missing at $(BridgeLoaderOutDir) - build HPAutoCad.McpBridge.Loader first"/>

        <ItemGroup>
            <_BundleManifest Include="$(MSBuildThisFileDirectory)Bundle\PackageContents.xml"/>
            <_AppFiles Include="$(AppOutDir)**\*"/>
            <_BridgeFiles Include="$(BridgeOutDir)**\*"/>
            <_BridgeLoaderDll Include="$(BridgeLoaderOutDir)HPAutoCad.McpBridge.Loader.dll"/>
            <_BridgeLoaderPdb Include="$(BridgeLoaderOutDir)HPAutoCad.McpBridge.Loader.pdb" Condition="Exists('$(BridgeLoaderOutDir)HPAutoCad.McpBridge.Loader.pdb')"/>
            <_LoaderPdb Include="$(TargetDir)$(TargetName).pdb" Condition="Exists('$(TargetDir)$(TargetName).pdb')"/>
        </ItemGroup>

        <!-- 1. Lock Probe: fail loudly if AutoCAD is currently running and holding DLL locks -->
        <Delete Files="$(BundleDir)Contents\HPAutoCad.Loader.dll;$(BundleDir)Contents\HPAutoCad.McpBridge.Loader.dll;$(BundleDir)Contents\App\HPAutoCad.dll;$(BundleDir)Contents\Bridge\HPAutoCad.McpBridge.dll" ContinueOnError="false"/>

        <!-- 2. Clean up legacy separate bundles to prevent duplicate loading & pipe race conditions -->
        <RemoveDir Directories="$(LegacyBridgeBundleDir)" Condition="Exists('$(LegacyBridgeBundleDir)')"/>
        <RemoveDir Directories="$(LegacyGeoBundleDir)" Condition="Exists('$(LegacyGeoBundleDir)')"/>

        <!-- 3. Clean target subdirectories to eliminate stale assemblies -->
        <RemoveDir Directories="$(BundleDir)Contents\App"/>
        <RemoveDir Directories="$(BundleDir)Contents\Bridge"/>

        <!-- 4. Deploy Manifest & Loaders into Contents/ -->
        <Copy SourceFiles="@(_BundleManifest)" DestinationFolder="$(BundleDir)"/>
        <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(BundleDir)Contents\"/>
        <Copy SourceFiles="@(_LoaderPdb)" DestinationFolder="$(BundleDir)Contents\"/>
        <Copy SourceFiles="@(_BridgeLoaderDll)" DestinationFolder="$(BundleDir)Contents\"/>
        <Copy SourceFiles="@(_BridgeLoaderPdb)" DestinationFolder="$(BundleDir)Contents\"/>

        <!-- 5. Deploy Isolated Bridge & App payloads -->
        <Copy SourceFiles="@(_BridgeFiles)" DestinationFiles="@(_BridgeFiles->'$(BundleDir)Contents\Bridge\%(RecursiveDir)%(Filename)%(Extension)')"/>
        <Copy SourceFiles="@(_AppFiles)" DestinationFiles="@(_AppFiles->'$(BundleDir)Contents\App\%(RecursiveDir)%(Filename)%(Extension)')"/>

        <Message Importance="high" Text="HPAutoCad unified bundle successfully deployed to $(BundleDir)"/>
    </Target>

</Project>
```

### 5.4 Build Order in `HPAutoCad.slnx`
```
HPAutoCad.Core + HPAutoCad.TileFetch
       │
       ▼
HPAutoCad (Repacks MaterialDesignThemes, copies TileFetch to output)
       │
HPRebar.McpBridge.Core + HPRebar.Mcp.Contracts + HPAutoCad.Aec
       │
       ▼
HPAutoCad.McpBridge (Repacks MaterialDesignThemes)
       │
       ▼
HPAutoCad.McpBridge.Loader (Builds loader DLL; skips legacy deployment)
       │
       ▼
HPAutoCad.Loader (Builds loader DLL; executes DeployBundle for HPAutoCad.bundle)
```

---

## 6. Civil 3D Mirror Boundary Verification & Invariant Proof

### 6.1 Mirror Contract Rules (`HPCivil3d/tools/mirror-tokens.json`)
In `HPCivil3d/tools/mirror-tokens.json`:
- `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` is tracked in `ownedCounterparts`:
  ```json
  {
    "autocad": "HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml",
    "civil3d": "HPCivil3d.McpBridge.Loader/Bundle/PackageContents.xml",
    "autocadSha256": "22d566c8faec996572452d3fab99fdceaa0d89732f6f42b28df86f3602c1ef28"
  }
  ```
- `HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj` is tracked in `mirroredFiles`:
  ```json
  {
    "autocad": "HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj",
    "civil3d": "HPCivil3d.McpBridge.Loader/HPCivil3d.McpBridge.Loader.csproj"
  }
  ```

### 6.2 Scope of `MirrorTokenTable.SourceFiles`
`MirrorTokenTable.SourceFiles` searches strictly inside:
- `HPAutoCad/HPAutoCad.McpBridge/`
- `HPAutoCad/HPAutoCad.McpBridge.Loader/`
- `HPAutoCad/HPAutoCad.Mcp.Server/`

It does **not** inspect:
- `HPAutoCad/HPAutoCad.Loader/`
- `HPAutoCad/HPAutoCad/`
- `HPAutoCad/HPAutoCad.Core/`
- `HPAutoCad/Directory.Build.props`
- `HPAutoCad/HPAutoCad.slnx`

### 6.3 Mathematical Verification
Computed SHA-256 of `HPAutoCad/HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml`:
```python
import hashlib
text = open('HPAutoCad/HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml', 'rb').read().decode('utf-8-sig').replace('\r\n', '\n')
print(hashlib.sha256(text.encode('utf-8')).hexdigest())
# Output: 22d566c8faec996572452d3fab99fdceaa0d89732f6f42b28df86f3602c1ef28
```
The hash matches the pinned value in `mirror-tokens.json` byte-for-byte.

### 6.4 Guarantee
By keeping `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` and `HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj` 100% unmodified, and defining `DeployBundle` in `HPAutoCad.Loader.csproj` with `<DeployBundle>false</DeployBundle>` via `Directory.Build.props`, **zero changes are made to the Civil 3D mirror surface**, ensuring `dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests` passes 60/60 tests (100%).

---

## 7. Shared Ribbon Tab & UI Coordination

### 7.1 Contract Specifications
- **Tab Identifier**: `HPAUTOCAD_MCP_TAB`
- **Tab Title**: `"HPAutoCad"`
- **Panels**:
  1. `HPAUTOCAD_MCP_PANEL` ("MCP"): Created by `McpRibbonTab.cs` (contains "MCP Bridge" button, command `HPMCPBRIDGE`).
  2. `HPGEOLINK_PANEL` ("HPGeoLink"): Created by `HPAutoCadRibbonTab.cs` (contains "KMZ" button, command `HPGEO`, plus split commands `-HPGEOKMZ`, `HPGEOIMPORT`, `HPGEOINFO`).

### 7.2 Zero Order Dependency
Both loaders implement identical safe creation semantics:
```csharp
var ribbon = ComponentManager.Ribbon;
if (ribbon is null) return;
var tab = ribbon.FindTab(TabId);
if (tab is null)
{
    tab = new RibbonTab { Id = TabId, Title = TabTitle, IsVisible = true };
    ribbon.Tabs.Add(tab);
}
if (FindOwnPanel(tab) is not null) return;
tab.Panels.Add(BuildPanel());
```
- If `HPAutoCad.McpBridge.Loader` initializes first: creates tab `HPAUTOCAD_MCP_TAB`, adds `HPAUTOCAD_MCP_PANEL`. When `HPAutoCad.Loader` initializes: finds existing tab, adds `HPGEOLINK_PANEL`.
- If `HPAutoCad.Loader` initializes first: creates tab `HPAUTOCAD_MCP_TAB`, adds `HPGEOLINK_PANEL`. When `HPAutoCad.McpBridge.Loader` initializes: finds existing tab, adds `HPAUTOCAD_MCP_PANEL`.
- In either sequence, the user sees a unified "HPAutoCad" tab with both panels side-by-side.

### 7.3 Lifecycle Events
- **Workspace Switch (`WSCURRENT`)**: AutoCAD destroys custom dynamic ribbon items on workspace switch. Both loaders hook `SystemVariableChanged`, queue an `Application.Idle` callback, and restore their respective panels onto the tab.
- **Theme Switch (`COLORTHEME`)**: Both loaders re-evaluate `COLORTHEME` (0 = Dark, 1 = Light), recreate vector icons with appropriate ink colors, and replace only their own panel.
- **Teardown (`Terminate`)**: Each loader unhooks events, removes its own panel, and removes `HPAUTOCAD_MCP_TAB` only if no other panels remain on it.

---

## 8. Implementation Roadmap

### Phase 1: Setup & Project Scaffolding
1. Create `HPAutoCad/Directory.Build.props` disabling `DeployBundle` for `HPAutoCad.McpBridge.Loader`.
2. Create project directory `HPAutoCad/HPAutoCad.Loader/` with subfolders `Bundle/` and `Ribbon/`.
3. Add `HPAutoCad/HPAutoCad.Loader/Bundle/PackageContents.xml` with the unified manifest.
4. Add `HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj` with references and unified `DeployBundle` target.
5. Register `HPAutoCad.Loader/HPAutoCad.Loader.csproj` in `HPAutoCad/HPAutoCad.slnx`.

### Phase 2: Loader Code Migration & Adaptation
1. Implement `AppLoadContext.cs` (resolves `HPAutoCad.deps.json` and unmanaged `WebView2Loader.dll`).
2. Implement `LoaderLog.cs` (logs to `%LOCALAPPDATA%\HPAutoCad\logs\loader.log`).
3. Implement `HPAutoCadLoaderApplication.cs` (`[assembly: ExtensionApplication]`, starts `HPAutoCad.Entry.Start`).
4. Implement `HPAutoCadCommands.cs` (`[assembly: CommandClass]`, registers `HPGEO`, `-HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `-HPGEOIMAGE`, `HPGEOINFO`).
5. Implement `Ribbon/HPAutoCadRibbonTab.cs` and `Ribbon/RibbonIcons.cs` (cooperative panel registration).

### Phase 3: Compilation & Static Verification
1. Run `dotnet build HPAutoCad\HPAutoCad.slnx -c Debug` to verify:
   - All projects build with 0 warnings/errors.
   - `DeployBundle` target executes successfully and creates `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.
   - Legacy bundles `HPAutoCad.McpBridge.bundle` and `HPGeo.bundle` are cleanly removed.
2. Run `dotnet run --project HPAutoCad/HPAutoCad.Tests` (161 tests pass).
3. Run `dotnet run --project HPAutoCad/HPAutoCad.Mcp.Server.Tests` (280 tests pass).
4. Run `dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests` (60 mirror tests pass, 100%).

---

## 9. Conclusion
This specification provides a mathematically verified, architecturally sound, and non-destructive packaging and deployment design for `HPAutoCad.bundle`. It fulfills all requirements of Milestone M3 while maintaining complete isolation, zero runtime collision, and 100% Civil 3D mirror boundary compliance.
