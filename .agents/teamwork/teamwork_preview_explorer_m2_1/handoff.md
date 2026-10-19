# Handoff Report: HPDynamo Solution & Build Scaffolding (M2.1)

**Agent**: `teamwork_preview_explorer_m2_1`  
**Milestone**: M2.1 — HPDynamo Solution Setup, `Directory.Build.props`, and `global.json`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_1\`  
**Date**: 2026-10-08  
**Parent Agent**: `orchestrator_4` (`b20bb4a5-dd75-4c48-9f4d-621e02412393`)  

---

## 1. Observation

### 1.1 Existing Host Architectures & Reference Implementations
Inspection of sibling hosts across the repository (`HPCivil3d`, `HPNavis`, `HPEtabs`, `HPRobot`, `HPTekla`) reveals a strictly consistent, proven architectural pattern:
- **Solution format**: XML `.slnx` (e.g., `HPCivil3d/HPCivil3d.slnx:1-29`, `HPNavis/HPNavis.slnx:1-26`, `HPEtabs/HPEtabs.slnx:1-27`), configured with `Debug` and `Release` build types, a `/Solution Items/` virtual folder for metadata (`global.json`, `Directory.Build.props`, `README.md`), the host's own projects, and a `/Shared/` folder referencing the 3 host-neutral engines in `../McpShared/`:
  - `../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj` (DTO wire contracts; `netstandard2.0;net48`)
  - `../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj` (host bridge engine; `net8.0;net48`)
  - `../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj` (server engine; `net10.0`)
- **`global.json`**: Pinned across all hosts (`HPCivil3d/global.json:1-10`, `HPNavis/global.json:1-10`):
  ```json
  {
    "sdk": {
      "version": "10.0.300",
      "rollForward": "latestMinor",
      "allowPrerelease": true
    },
    "test": {
      "runner": "Microsoft.Testing.Platform"
    }
  }
  ```
  Verified active SDK via `dotnet --version` on this machine is `10.0.400`, which rolls forward cleanly from `10.0.300`.

### 1.2 Verification of Local Autodesk Revit 2026 & Dynamo 3.7 Installation
Direct inspection of the Windows registry and filesystem verified that Revit 2026 and Dynamo 3.7 are installed at the standard paths:
- **Registry Inspection**:
  Key `HKLM:\SOFTWARE\Autodesk\Revit\2026\REVIT-05:0409` contains `InstallationLocation = "C:\Program Files\Autodesk\Revit 2026\"`.
- **User AppData Directory**:
  `$env:APPDATA\Autodesk\Revit\Addins\2026` and `$env:APPDATA\Dynamo\Dynamo Revit\3.7` both exist.
- **Physical Assembly Verification**:
  All 12 required core and vertical assemblies were tested directly via `Test-Path` and confirmed present (`True`):
  1. `C:\Program Files\Autodesk\Revit 2026\RevitAPI.dll` (`True`)
  2. `C:\Program Files\Autodesk\Revit 2026\RevitAPIUI.dll` (`True`)
  3. `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DynamoCore.dll` (`True`)
  4. `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DynamoCoreWpf.dll` (`True`)
  5. `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DynamoServices.dll` (`True`)
  6. `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\ProtoCore.dll` (`True`)
  7. `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\ProtoGeometry.dll` (`True`)
  8. `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DesignScriptBuiltin.dll` (`True`)
  9. `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DSCoreNodes.dll` (`True`)
  10. `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\Revit\RevitNodes.dll` (`True`)
  11. `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\Revit\RevitServices.dll` (`True`)
  12. `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\Revit\DynamoRevitDS.dll` (`True`) — contains `RevitDynamoModel`

### 1.3 Baseline Test Suite Status (`HPDynamo.E2E.Tests`)
- `HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj` was built using `dotnet build`:
  `Build succeeded. 0 Warning(s), 0 Error(s)`.
- Executed acceptance suite via `powershell -ExecutionPolicy Bypass -File .\run-e2e-dynamo-tests.ps1`:
  `total: 84, failed: 0, succeeded: 84, skipped: 0, duration: 283ms`. Result: 100% green.

---

## 2. Logic Chain

### 2.1 Solution File (`HPDynamo.slnx`)
1. The project specification (`PROJECT.md:103-159` and `DISPATCH.md:1-8`) dictates that `HPDynamo/` contains five subsystem projects:
   - `HPDynamo.Mcp.Server` (.NET 10.0 console application, stdio transport)
   - `HPDynamo.McpBridge` (.NET 8.0-windows7.0 in-process bridge running inside Revit.exe)
   - `HPDynamo.Mcp.Server.Tests` (.NET 10.0 xUnit v3 / MTP unit test suite)
   - `HPDynamo.McpBridge.Tests` (.NET 8.0-windows7.0 xUnit v3 / MTP unit test suite)
   - `HPDynamo.E2E.Tests` (.NET 10.0 xUnit v3 / MTP acceptance test suite)
2. Along with these, the solution must reference the 3 projects in `../McpShared/` inside a virtual `/Shared/` folder by relative path, matching `HPCivil3d.slnx`, `HPNavis.slnx`, and `HPEtabs.slnx`.
3. MSBuild strictly requires every project referenced in an `.slnx` to exist on disk. Therefore, for `dotnet build HPDynamo/HPDynamo.slnx` to pass on initialization, `HPDynamo.slnx` should be established alongside minimal project scaffolding (or project stubs) for all 5 projects.

### 2.2 Directory.Build.props Resolution Architecture
1. Autodesk publishes no official NuGet package for Dynamo 3.7 matching Revit 2026. Therefore, the bridge and seed compile tests must reference assemblies directly from the local installation with `<Private>false</Private>` (never copy them to output, preventing duplicate load conflicts).
2. The property file must provide a 4-tier resolution hierarchy:
   - Explicit CLI override `-p:DynamoInstallDir=<dir>` or env `HPDYNAMO_DYNAMO_DIR`
   - Explicit CLI override `-p:RevitInstallDir=<dir>` or env `HPREBAR_REVIT_DIR`
   - Automatic 64-bit Registry detection: `HKLM\SOFTWARE\Autodesk\Revit\2026\REVIT-05:0409\InstallationLocation`
   - Standard 64-bit filesystem fallback: `$(ProgramW6432)\Autodesk\Revit 2026\AddIns\DynamoForRevit\` and `$(ProgramW6432)\Autodesk\Revit 2026\`
3. Furthermore, `Directory.Build.props` must expose explicit properties pointing to the shared projects in `McpShared/` (`McpContractsProjectPath`, `McpBridgeCoreProjectPath`, `McpServerCoreProjectPath`) so subprojects can reference them cleanly without repeated relative path clutter.
4. Early diagnostics: Expose `DynamoApiAvailable` and `RevitApiAvailable` boolean flags so that projects compiling against Dynamo/Revit can emit clean, actionable diagnostic errors instead of hundreds of `CS0246` errors when running on a host without Dynamo/Revit installed.

### 2.3 Dependency Graph & Clean Code Isolation
1. **Direction of Dependencies**:
   - `HPDynamo.Mcp.Server` $\rightarrow$ `McpShared/HPRebar.Mcp.Server.Core` $\rightarrow$ `McpShared/HPRebar.Mcp.Contracts`
   - `HPDynamo.McpBridge` $\rightarrow$ `McpShared/HPRebar.McpBridge.Core` $\rightarrow$ `McpShared/HPRebar.Mcp.Contracts`
   - `HPDynamo.Mcp.Server.Tests` $\rightarrow$ `HPDynamo.Mcp.Server`, `McpShared`
   - `HPDynamo.McpBridge.Tests` $\rightarrow$ `HPDynamo.McpBridge`, `McpShared`
   - `HPDynamo.E2E.Tests` $\rightarrow$ `McpShared`
2. **Strict Invariants**:
   - Zero dependencies from `HPDynamo/` to `HPAutoCad`, `HPCivil3d`, `HPNavis`, `HPEtabs`, `HPRebar`, `HPRobot`, `HPTekla`, `HPExcel`, `HPPowerBi`, or `HPBim`.
   - Zero circular dependencies.
   - `HPDynamo.Mcp.Server` must NEVER reference `RevitAPI.dll` or `DynamoCore.dll` — it handles script strings and JSON-RPC dispatch only.

---

## 3. Caveats

1. **MSBuild Solution Build Pre-requisite**:
   If an `.slnx` lists a project file that does not yet exist on disk, `dotnet build HPDynamo.slnx` will fail with `MSB3202: The project file ... was not found`. To prevent broken builds when `HPDynamo.slnx` is checked in during M2, the worker should scaffold minimal stub `.csproj` files for `HPDynamo.Mcp.Server`, `HPDynamo.McpBridge`, `HPDynamo.Mcp.Server.Tests`, and `HPDynamo.McpBridge.Tests` (if not already implementing their full contents).
2. **Run-time vs Build-time Host Binding**:
   `HPDynamo.McpBridge` references `RevitAPI.dll` and `DynamoCore.dll` with `<Private>false</Private>`. At runtime inside `Revit.exe`, these assemblies are already loaded into the process; the bridge hooks them via the shared `AssemblyLoadContext` without copying private duplicates.

---

## 4. Conclusion & Line-by-Line Implementation Guidance

The exact, copy-paste-ready specifications for `global.json`, `Directory.Build.props`, `HPDynamo.slnx`, and the project configurations are formulated below:

### 4.1 `HPDynamo/global.json`
Location: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\global.json`
```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestMinor",
    "allowPrerelease": true
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

### 4.2 `HPDynamo/Directory.Build.props`
Location: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\Directory.Build.props`
```xml
<Project>

  <!--
    Directory.Build.props for HPDynamo (Revit 2026 & Dynamo 3.7 MCP Subsystem).
    Enforces shared properties, paths to McpShared, and assembly resolution
    for Autodesk Revit 2026 and Dynamo for Revit 3.7.
  -->
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <NoWarn>$(NoWarn);xUnit1051;NU1701</NoWarn>

    <!-- Version Pins -->
    <RevitYear Condition="'$(RevitYear)' == ''">2026</RevitYear>
    <DynamoVersion Condition="'$(DynamoVersion)' == ''">3.7</DynamoVersion>

    <!-- McpShared Projects Relative Resolution -->
    <McpSharedDir Condition="'$(McpSharedDir)' == ''">$([System.IO.Path]::GetFullPath('$(MSBuildThisFileDirectory)..\McpShared\'))</McpSharedDir>
    <McpContractsProjectPath>$(McpSharedDir)HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj</McpContractsProjectPath>
    <McpBridgeCoreProjectPath>$(McpSharedDir)HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj</McpBridgeCoreProjectPath>
    <McpServerCoreProjectPath>$(McpSharedDir)HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj</McpServerCoreProjectPath>

    <!-- Revit 2026 Base Directory Resolution -->
    <RevitInstallDir Condition="'$(RevitInstallDir)' == '' And '$(HPREBAR_REVIT_DIR)' != ''">$(HPREBAR_REVIT_DIR)</RevitInstallDir>
    <_RevitRegLocation Condition="'$(RevitInstallDir)' == ''">$([MSBuild]::GetRegistryValueFromView('HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\Revit\$(RevitYear)\REVIT-05:0409', 'InstallationLocation', null, RegistryView.Registry64))</_RevitRegLocation>
    <RevitInstallDir Condition="'$(RevitInstallDir)' == '' And '$(_RevitRegLocation)' != ''">$(_RevitRegLocation)</RevitInstallDir>
    <RevitInstallDir Condition="'$(RevitInstallDir)' == ''">$(ProgramW6432)\Autodesk\Revit $(RevitYear)\</RevitInstallDir>
    <RevitInstallDir Condition="!HasTrailingSlash('$(RevitInstallDir)')">$(RevitInstallDir)\</RevitInstallDir>

    <!-- Dynamo for Revit Directory Resolution -->
    <DynamoInstallDir Condition="'$(DynamoInstallDir)' == '' And '$(HPDYNAMO_DYNAMO_DIR)' != ''">$(HPDYNAMO_DYNAMO_DIR)</DynamoInstallDir>
    <DynamoInstallDir Condition="'$(DynamoInstallDir)' == ''">$(RevitInstallDir)AddIns\DynamoForRevit\</DynamoInstallDir>
    <DynamoInstallDir Condition="!HasTrailingSlash('$(DynamoInstallDir)')">$(DynamoInstallDir)\</DynamoInstallDir>

    <!-- Dynamo Revit Subdirectory (RevitNodes.dll, RevitServices.dll, DynamoRevitDS.dll) -->
    <DynamoRevitDir>$(DynamoInstallDir)Revit\</DynamoRevitDir>

    <!-- Deployment Folders (per-user AppData) -->
    <RevitAddinsDeployDir>$([System.Environment]::GetFolderPath(SpecialFolder.ApplicationData))\Autodesk\Revit\Addins\$(RevitYear)\</RevitAddinsDeployDir>
    <DynamoViewExtensionsDeployDir>$([System.Environment]::GetFolderPath(SpecialFolder.ApplicationData))\Dynamo\Dynamo Revit\$(DynamoVersion)\viewExtensions\</DynamoViewExtensionsDeployDir>

    <!-- API Availability Diagnostics -->
    <RevitApiAvailable>false</RevitApiAvailable>
    <RevitApiAvailable Condition="Exists('$(RevitInstallDir)RevitAPI.dll')">true</RevitApiAvailable>

    <DynamoApiAvailable>false</DynamoApiAvailable>
    <DynamoApiAvailable Condition="Exists('$(DynamoInstallDir)DynamoCore.dll') And Exists('$(DynamoRevitDir)DynamoRevitDS.dll')">true</DynamoApiAvailable>
  </PropertyGroup>

</Project>
```

### 4.3 `HPDynamo/HPDynamo.slnx`
Location: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx`
```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="Directory.Build.props" />
    <File Path="README.md" />
  </Folder>
  <!-- HPDynamo MCP: In-Process Dual-Lifecycle Bridge running inside Revit.exe / Dynamo 3.7,
       stdio MCP Server executable launched by AI coding agents, and unit / E2E acceptance test suites.
       Shared engine, always referenced by relative path, never copied:
         ../McpShared/HPRebar.Mcp.Contracts
         ../McpShared/HPRebar.McpBridge.Core
         ../McpShared/HPRebar.Mcp.Server.Core -->
  <Project Path="HPDynamo.McpBridge/HPDynamo.McpBridge.csproj" />
  <Project Path="HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj" />
  <Project Path="HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj" />
  <Project Path="HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj" />
  <Project Path="HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

### 4.4 `HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj`
Location: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\HPDynamo.Mcp.Server.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>HPDynamo.Mcp.Server</RootNamespace>
    <Configurations>Debug;Release</Configurations>
    <!-- stdio MCP server: host AI launches this exe as child process and owns its lifetime -->
    <InvariantGlobalization>true</InvariantGlobalization>
    <!-- e_sqlite3 native library extracted for single-file publish -->
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="HPDynamo.Mcp.Server.Tests" />
    <InternalsVisibleTo Include="HPDynamo.E2E.Tests" />
  </ItemGroup>

  <ItemGroup>
    <!-- Host-neutral bootstrap, pipe client, registry engine, meta tools -->
    <ProjectReference Include="$(McpServerCoreProjectPath)" />
  </ItemGroup>

  <ItemGroup>
    <!-- Seed tools library embedded resources -->
    <Compile Remove="Registry\SeedLibrary\**\*.cs" />
    <EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)" />
  </ItemGroup>

</Project>
```

### 4.5 `HPDynamo.McpBridge/HPDynamo.McpBridge.csproj`
Location: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\HPDynamo.McpBridge.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows7.0</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>HPDynamo.McpBridge</RootNamespace>
    <Configurations>Debug;Release</Configurations>
    <Version>0.1.0</Version>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="$(McpContractsProjectPath)" />
    <ProjectReference Include="$(McpBridgeCoreProjectPath)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="MaterialDesignThemes" Version="5.3.2" />
    <PackageReference Include="Serilog" Version="4.4.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
    <PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all" />
  </ItemGroup>

  <!-- Diagnostic Guard against missing Revit / Dynamo installations -->
  <Target Name="RequireDynamoApi" BeforeTargets="ResolveAssemblyReferences;CoreCompile">
    <Error Condition="'$(DynamoApiAvailable)' != 'true'"
           Text="Dynamo 3.7 / Revit 2026 not found (looked for DynamoCore.dll in '$(DynamoInstallDir)' and DynamoRevitDS.dll in '$(DynamoRevitDir)'). Install Revit 2026 with Dynamo, or set HPDYNAMO_DYNAMO_DIR." />
  </Target>

  <!-- Revit 2026 Core Assemblies (never copy: supplied by Revit.exe) -->
  <ItemGroup>
    <Reference Include="RevitAPI">
      <HintPath>$(RevitInstallDir)RevitAPI.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="RevitAPIUI">
      <HintPath>$(RevitInstallDir)RevitAPIUI.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <!-- Dynamo 3.7 Core & Revit Assemblies (never copy: supplied by DynamoForRevit) -->
  <ItemGroup>
    <Reference Include="DynamoCore">
      <HintPath>$(DynamoInstallDir)DynamoCore.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="DynamoCoreWpf">
      <HintPath>$(DynamoInstallDir)DynamoCoreWpf.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="DynamoServices">
      <HintPath>$(DynamoInstallDir)DynamoServices.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="ProtoCore">
      <HintPath>$(DynamoInstallDir)ProtoCore.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="ProtoGeometry">
      <HintPath>$(DynamoInstallDir)ProtoGeometry.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="DesignScriptBuiltin">
      <HintPath>$(DynamoInstallDir)DesignScriptBuiltin.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="DSCoreNodes">
      <HintPath>$(DynamoInstallDir)DSCoreNodes.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="RevitNodes">
      <HintPath>$(DynamoRevitDir)RevitNodes.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="RevitServices">
      <HintPath>$(DynamoRevitDir)RevitServices.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="DynamoRevitDS">
      <HintPath>$(DynamoRevitDir)DynamoRevitDS.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

</Project>
```

### 4.6 `HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj`
Location: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>HPDynamo.Mcp.Server.Tests</RootNamespace>
    <Configurations>Debug;Release</Configurations>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="3.1.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\HPDynamo.Mcp.Server\HPDynamo.Mcp.Server.csproj" />
    <ProjectReference Include="$(McpServerCoreProjectPath)" />
    <ProjectReference Include="$(McpContractsProjectPath)" />
  </ItemGroup>

</Project>
```

### 4.7 `HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj`
Location: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows7.0</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>HPDynamo.McpBridge.Tests</RootNamespace>
    <Configurations>Debug;Release</Configurations>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="3.1.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\HPDynamo.McpBridge\HPDynamo.McpBridge.csproj" />
    <ProjectReference Include="$(McpBridgeCoreProjectPath)" />
    <ProjectReference Include="$(McpContractsProjectPath)" />
  </ItemGroup>

</Project>
```

---

## 5. Verification Method

Once the files are created by the implementation worker, the following sequence verifies clean resolution and complete build integrity:

```bash
# 1. Verify global.json SDK pin and test runner
dotnet --version

# 2. Build entire HPDynamo solution
dotnet build HPDynamo/HPDynamo.slnx -c Debug

# 3. Verify acceptance test suite runs 100% green
powershell -ExecutionPolicy Bypass -File .\run-e2e-dynamo-tests.ps1

# 4. Verify zero regressions in McpShared core tests
dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj
dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj
```

### Invalidation Conditions:
- If `HPDynamo/HPDynamo.slnx` references a non-existent file on disk, `dotnet build HPDynamo.slnx` will fail. Scaffold minimal project stubs simultaneously.
- If `HPDynamo.McpBridge` attempts to copy local host assemblies (`<Private>true</Private>`), runtime assembly load conflicts will arise in Revit.exe. Keep `<Private>false</Private>`.
- If any project in `HPDynamo` introduces a dependency to another host outside `McpShared`, Clean Code Governance rule is violated.
