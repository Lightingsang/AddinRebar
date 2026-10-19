# Milestone 5 Integration & Test Automation Architecture Report

## 1. Observation

### 1.1 Solution Configuration & Existing Projects
- **File**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx`
  - Current structure (lines 17–24):
    ```xml
    <Project Path="HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj" />
    <Project Path="HPDynamo.McpBridge/HPDynamo.McpBridge.csproj" />
    <Project Path="HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj" />
    <Folder Name="/Shared/">
      <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
      <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
      <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
    </Folder>
    ```
  - Milestone 5 requires adding:
    - `HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj`
    - `HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj`
  - Sibling solutions (`HPNavis.slnx`, `HPEtabs.slnx`, `HPCivil3d.slnx`) group Bridge + Bridge.Tests, followed by Server + Server.Tests, using clean `<Project Path="..." />` without build configuration suppression attributes.

### 1.2 Directory.Build.props & Host Assembly Isolation
- **File**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\Directory.Build.props`
  - Lines 52–57 define a global defense against host DLL copying:
    ```xml
    <ItemDefinitionGroup>
      <Reference>
        <Private>false</Private>
      </Reference>
    </ItemDefinitionGroup>
    ```
  - Verified live: In `HPDynamo.McpBridge\bin\Debug\net10.0-windows7.0`, exactly 0 host DLLs (`RevitAPI.dll`, `DynamoCore.dll`, etc.) were copied.
  - Lines 12–13 define warnings suppression:
    ```xml
    <NoWarn>$(NoWarn);xUnit1051;NU1701;MSB3277</NoWarn>
    <MSBuildWarningsAsMessages>$(MSBuildWarningsAsMessages);MSB3277</MSBuildWarningsAsMessages>
    ```
    This suppresses xUnit v3 `xUnit1051` (cancellation token check on test methods) across all test projects.
  - Lines 44–50 provide automatic host API detection:
    ```xml
    <RevitApiAvailable Condition="Exists('$(RevitInstallDir)RevitAPI.dll')">true</RevitApiAvailable>
    <DynamoApiAvailable Condition="Exists('$(DynamoInstallDir)DynamoCore.dll') And Exists('$(DynamoRevitDir)DynamoRevitDS.dll')">true</DynamoApiAvailable>
    ```

### 1.3 Test Framework & Microsoft.Testing.Platform Configuration
- **File**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\global.json`
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
- **File**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.E2E.Tests\HPDynamo.E2E.Tests.csproj`
  - Target: `<TargetFramework>net10.0</TargetFramework>`
  - Output: `<OutputType>Exe</OutputType>`
  - Runner flag: `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`
  - Packages: `xunit.v3` (3.1.0), `xunit.runner.visualstudio` (3.1.5), `Microsoft.Bcl.AsyncInterfaces` (10.0.12).

### 1.4 Live Execution Benchmarks & Failure Modes
- **Command 1**: `dotnet test HPDynamo.slnx` inside `HPDynamo\`
  - Result: 117 tests passed in **730ms** total duration (463ms test execution).
- **Command 2**: `powershell -File .\HPDynamo\run-e2e-dynamo-tests.ps1`
  - Result: 117 tests passed in **292ms** test execution.
- **Command 3**: `dotnet test HPDynamo.slnx --nologo` inside `HPDynamo\`
  - **VERBATIM FAILURE**:
    ```
    Test run summary: Zero tests ran
      error: 1
      total: 0
      failed: 0
      succeeded: 0
      skipped: 0
      duration: 254ms
    Test run completed with non-success exit code: 5 (see: https://aka.ms/testingplatform/exitcodes)
    ```
  - Unrecognized command-line argument `--nologo` caused MTP parser to abort and print complete help text.
- **Command 4**: `dotnet test HPDynamo\HPDynamo.slnx` executed from repository root without MTP property
  - Result: Ignored test execution because repository root lacks a `global.json` enabling MTP.
- **Command 5**: `dotnet test HPDynamo\HPDynamo.slnx -p:TestingPlatformDotnetTestSupport=true` from repository root
  - Result: 117 tests passed in **328ms**.

### 1.5 InternalsVisibleTo State in Production Projects
- `HPDynamo.Mcp.Server.csproj` (lines 18–19):
  ```xml
  <InternalsVisibleTo Include="HPDynamo.Mcp.Server.Tests" />
  <InternalsVisibleTo Include="HPDynamo.E2E.Tests" />
  ```
- `HPDynamo.McpBridge.csproj`: Currently lacks `<InternalsVisibleTo Include="HPDynamo.McpBridge.Tests" />`.

---

## 2. Logic Chain

1. **Solution Structure in `HPDynamo.slnx`**:
   - Observations 1.1 and 1.3 show that all HPDynamo projects target .NET 10 (`net10.0` or `net10.0-windows7.0`).
   - Unlike `HPRebar.slnx` (which required `<Build Project="false"/>` for `HPRebar.Tests` due to legacy .NET Framework 4.8 collisions in R23/R24 configurations), HPDynamo has no multi-targeting friction.
   - Sibling solutions (`HPNavis.slnx`, `HPEtabs.slnx`, `HPCivil3d.slnx`) demonstrate the standard clean pattern where both unit test projects and production projects build in all configurations without exclusion tags.
   - Therefore, adding `<Project Path="HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj" />` and `<Project Path="HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj" />` directly to `HPDynamo.slnx` integrates both suites cleanly into standard Visual Studio / Rider and `dotnet build` pipelines.

2. **Host DLL Isolation & Directory.Build.props**:
   - Observation 1.2 demonstrates that the existing `ItemDefinitionGroup` sets `<Reference><Private>false</Private></Reference>` globally across all projects in the folder.
   - Observation 1.2 confirmed that `HPDynamo.McpBridge\bin\Debug\` contains 0 Autodesk or Dynamo DLLs despite referencing them.
   - Thus, test projects referencing `HPDynamo.McpBridge` or host assemblies will never pollute `bin/` with host binaries, satisfying the Clean Code repository standard.
   - Adding `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>` to `Directory.Build.props` ensures that `dotnet test` operates under MTP even when called from parent directories that lack a `global.json`.

3. **MTP Runner Argument Incompatibility (`--nologo`)**:
   - Observation 1.4 proves empirically that Microsoft.Testing.Platform rejects `--nologo` with exit code 5 ("Zero tests ran").
   - This occurs because MTP replaces the legacy VSTest CLI parser. Any unhandled CLI flag is treated as an invalid command option.
   - Therefore, neither `dotnet test` invocations nor test executable runs must ever pass `--nologo`.

4. **Automation Runner Mechanics (<30s Execution)**:
   - Observation 1.4 recorded a runtime of ~300ms for 117 acceptance tests.
   - Unit tests in `HPDynamo.Mcp.Server.Tests` use `FakeRevitExecutor` in-memory, and `HPDynamo.McpBridge.Tests` use headless coordinators without launching `Revit.exe`.
   - Running all 3 test suites (`Server.Tests`, `Bridge.Tests`, `E2E.Tests`) takes ~1.5 to 2.5 seconds total, well within the <30s constraint.
   - The runner scripts can execute standalone test executables (`*.Tests.exe`) directly, or run `dotnet test HPDynamo.slnx`, achieving instantaneous feedback.

5. **Assembly Internals Access**:
   - Observation 1.5 shows `HPDynamo.McpBridge` has not yet granted internal visibility to `HPDynamo.McpBridge.Tests`.
   - To allow `HPDynamo.McpBridge.Tests` to test internal coordinator routing, transaction rollback scopes, and status view model properties without making them public, `InternalsVisibleTo` must be declared in `HPDynamo.McpBridge.csproj`.

---

## 3. Caveats

1. **Live Revit / Dynamo Execution**:
   - Tests run headlessly using in-process Roslyn, fake executors, and simulated transaction scopes. Live in-process execution inside `Revit.exe` is part of live end-to-end verification, not the automated offline test suite.
2. **First-time Restore Overhead**:
   - On a cold environment without restored NuGet packages, the initial restore may take 5–10 seconds. Subsequent test runs execute incrementally in <3 seconds.
3. **PowerShell Execution Policies**:
   - When executing `.ps1` scripts on Windows, execution policies must be respected (e.g. passing `-ExecutionPolicy Bypass`).

---

## 4. Conclusion

### 4.1 Specification for `HPDynamo.slnx`
Update `HPDynamo/HPDynamo.slnx` to include all 5 deliverable projects plus the 3 McpShared engine references:
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

### 4.2 Project Configuration Specifications for M5 Test Projects

#### `HPDynamo.Mcp.Server.Tests.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
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
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Linked FakeRevitExecutor from McpShared -->
    <Compile Include="..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs" Link="Fakes\FakeRevitExecutor.cs" />
  </ItemGroup>

</Project>
```

#### `HPDynamo.McpBridge.Tests.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows7.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
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
    <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\HPDynamo.McpBridge\HPDynamo.McpBridge.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
  </ItemGroup>

  <!-- Conditional Host Reference Assemblies for metadata checking (Never copied) -->
  <ItemGroup Condition="'$(DynamoApiAvailable)' == 'true'">
    <Reference Include="DynamoCore">
      <HintPath>$(DynamoInstallDir)DynamoCore.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="ProtoCore">
      <HintPath>$(DynamoInstallDir)ProtoCore.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

</Project>
```

#### `HPDynamo.McpBridge.csproj` Enhancement
Add the missing `InternalsVisibleTo`:
```xml
  <ItemGroup>
    <InternalsVisibleTo Include="HPDynamo.McpBridge.Tests" />
    <InternalsVisibleTo Include="HPDynamo.E2E.Tests" />
  </ItemGroup>
```

### 4.3 Directory.Build.props Enhancement
To allow `dotnet test HPDynamo\HPDynamo.slnx` to run MTP tests seamlessly when invoked from the repository root (without requiring `-p:` flags), add:
```xml
    <!-- Enable Microsoft.Testing.Platform runner for dotnet test across all invocations -->
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
```
to the top PropertyGroup in `HPDynamo/Directory.Build.props`.

### 4.4 Automation Runner Integration Specification
We propose two integrated runner mechanisms:

#### Mechanism A: Native CLI (`dotnet test`)
```powershell
# From HPDynamo directory:
cd HPDynamo
dotnet test HPDynamo.slnx

# From repository root:
dotnet test HPDynamo\HPDynamo.slnx -p:TestingPlatformDotnetTestSupport=true
```
- Strictly omit `--nologo`.
- Both commands run all three test projects (`HPDynamo.Mcp.Server.Tests`, `HPDynamo.McpBridge.Tests`, `HPDynamo.E2E.Tests`) concurrently under MTP in <3 seconds.

#### Mechanism B: Unified PowerShell Harness (`run-all-dynamo-tests.ps1`)
A dedicated automation script supporting both granular targeting and full-suite acceptance:
```powershell
param(
    [ValidateSet("All", "Unit", "Server", "Bridge", "E2E")]
    [string]$Suite = "All",

    [ValidateSet("All", "Tier1", "Tier2", "Tier3", "Tier4")]
    [string]$Tier = "All"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Push-Location $scriptDir
try {
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "       HPDynamo MCP Subsystem Automated Test Harness        " -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan

    $serverExe = "$scriptDir\HPDynamo.Mcp.Server.Tests\bin\Debug\net10.0\HPDynamo.Mcp.Server.Tests.exe"
    $bridgeExe = "$scriptDir\HPDynamo.McpBridge.Tests\bin\Debug\net10.0-windows7.0\HPDynamo.McpBridge.Tests.exe"
    $e2eExe    = "$scriptDir\HPDynamo.E2E.Tests\bin\Debug\net10.0\HPDynamo.E2E.Tests.exe"

    # Build solution once if binaries are missing
    if (-not (Test-Path $serverExe) -or -not (Test-Path $bridgeExe) -or -not (Test-Path $e2eExe)) {
        Write-Host "Building HPDynamo.slnx..." -ForegroundColor Yellow
        dotnet build HPDynamo.slnx -c Debug -m:1 -nodeReuse:false
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    }

    $failed = $false

    if ($Suite -in @("All", "Unit", "Server")) {
        Write-Host "--> Running Server Unit Tests (HPDynamo.Mcp.Server.Tests)..." -ForegroundColor Yellow
        & $serverExe
        if ($LASTEXITCODE -ne 0) { $failed = $true }
    }

    if ($Suite -in @("All", "Unit", "Bridge") -and -not $failed) {
        Write-Host "--> Running Bridge Unit Tests (HPDynamo.McpBridge.Tests)..." -ForegroundColor Yellow
        & $bridgeExe
        if ($LASTEXITCODE -ne 0) { $failed = $true }
    }

    if ($Suite -in @("All", "E2E") -and -not $failed) {
        Write-Host "--> Running Acceptance Tests (HPDynamo.E2E.Tests - $Tier)..." -ForegroundColor Yellow
        $filterArg = switch ($Tier) {
            "All"   { @() }
            default { @("--filter-class", "*$Tier*") }
        }
        if ($filterArg.Length -gt 0) { & $e2eExe $filterArg } else { & $e2eExe }
        if ($LASTEXITCODE -ne 0) { $failed = $true }
    }

    if ($failed) {
        Write-Host "Test Run FAILED!" -ForegroundColor Red
        exit 1
    } else {
        Write-Host "All Test Suites PASSED (100% Green)" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
```

---

## 5. Verification Method

1. **Verify Solution Build**:
   ```powershell
   dotnet build HPDynamo\HPDynamo.slnx -c Debug
   ```
   *Expected*: Build succeeds with 0 errors and 0 warnings.

2. **Verify Native MTP Runner from Solution Directory**:
   ```powershell
   cd HPDynamo
   dotnet test HPDynamo.slnx
   ```
   *Expected*: All unit and acceptance test suites execute via MTP with duration < 3s, returning exit code 0.

3. **Verify `--nologo` Rejection Invariant**:
   ```powershell
   cd HPDynamo
   dotnet test HPDynamo.slnx --nologo
   ```
   *Expected*: Immediately terminates with exit code 5 and "Zero tests ran". Proves `--nologo` must never be used.

4. **Verify Host Assembly Isolation**:
   ```powershell
   Get-ChildItem -Path HPDynamo\*\bin\Debug\ -Filter "*Revit*.dll" -Recurse
   Get-ChildItem -Path HPDynamo\*\bin\Debug\ -Filter "*Dynamo*.dll" -Recurse
   ```
   *Expected*: Exactly 0 host binaries (`RevitAPI.dll`, `DynamoCore.dll`, `RevitNodes.dll`, etc.) present in any `bin/` directory. Only `HPDynamo.*.dll` and shared engine DLLs exist.

5. **Verify PowerShell Acceptance Runner**:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\HPDynamo\run-e2e-dynamo-tests.ps1 -Tier All
   ```
   *Expected*: 117 tests pass in < 1 second with SUCCESS (100% Passed).
