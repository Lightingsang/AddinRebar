# Handoff Report — explorer_m4_3

**Role**: Seed Compilation & Solution Integration Explorer  
**Subsystem**: Autodesk Robot Structural Analysis Professional 2026 MCP (`HPRobot`)  
**Target Recipient**: Project Orchestrator (`orchestrator_7`) / Implementation Worker  
**Date**: 2026-09-21  

---

## 1. Observation

1. **Sister Host Seed Compilation Implementation**:
   - `HPEtabs/HPEtabs.Mcp.Server.Tests/SeedLibraryCompileTests.cs` (lines 17–167) and `HPSap2000/HPSap2000.Mcp.Server.Tests/SeedLibraryCompileTests.cs` (lines 17–164) both implement in-memory Roslyn compilation of seeds via `CSharpCompilation.Create(...)`.
   - In both files, `FindWrapper()` resolves the interop wrapper assembly dynamically via a 3-tier lookup:
     1. Environment variable (`HPETABS_ETABS_DIR`, `HPSAP2000_SAP2000_DIR`)
     2. Registry key `HKLM\SOFTWARE\Classes\CLSID\{clsid}\LocalServer32`
     3. Default `%ProgramFiles%` directory
   - In both files (lines 30, 41), non-installed machines are handled dynamically with `Assert.SkipWhen(compiled is null, "<Host> not installed (<Wrapper>.dll not found)")`.
   - In both files, the test project `.csproj` (`HPEtabs.Mcp.Server.Tests.csproj` and `HPSap2000.Mcp.Server.Tests.csproj`) contains **zero** `<Reference Include="..."/>` to the vendor DLL. Vendor types are loaded purely via `MetadataReference.CreateFromFile(wrapper)`.

2. **Robot 2026 Registry & Interop File Probe**:
   - Running PowerShell command `Get-ItemProperty -Path "Registry::HKEY_CLASSES_ROOT\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32"` returned:
     ```
     (default) : C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\robot.exe
     ```
   - Running PowerShell command `Get-Item "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"` returned:
     ```
     FullName      : C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll
     Length        : 1892360
     LastWriteTime : 19/02/2025 11:14 PM
     ```
   - `HPRobot/Directory.Build.props` (lines 15–26) registers the exact same CLSID `{F7870790-CDE5-11D1-8FF1-00A02447BAAE}` and sets `RobotApiAvailable = true`.

3. **Current Solution Structure in `HPRobot/HPRobot.slnx`**:
   - Lines 19–21 of `HPRobot/HPRobot.slnx`:
     ```xml
       <Project Path="HPRobot.McpBridge/HPRobot.McpBridge.csproj" />
       <Project Path="HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj" />
       <Project Path="HPRobot.Mcp.Server/HPRobot.Mcp.Server.csproj" />
     ```
   - `HPRobot.Mcp.Server.Tests.csproj` is currently uncommitted and not yet registered in `HPRobot.slnx`.

4. **Existing Test Suite Baseline**:
   - Running `dotnet test HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj` outputs:
     ```
     Test run summary: Passed!
       total: 197
       failed: 0
       succeeded: 197
       skipped: 0
       duration: 9s 052ms
     ```
   - Running `dotnet test HPRobot.slnx` from `HPRobot/` outputs:
     ```
     Test run summary: Passed!
       total: 197
       failed: 0
       succeeded: 197
       skipped: 0
       duration: 9s 639ms
     ```

5. **Robot Script Contracts in `McpShared`**:
   - `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs` lines 188–208 define:
     - `RobotImports`: `["RobotOM", "System", "System.Collections.Generic", "System.Linq", "HPRebar.McpBridge.Core.Scripting"]`
     - `RobotGlobals`: `["robot", "structure", "units", "ct", "log", "progress", "args"]`
     - `RobotHeavyMaxTimeoutSeconds`: `300`

---

## 2. Logic Chain

1. **From Observation 1**:
   The sister host pattern decouples compile-time dependencies from test projects. By compiling seed code in-memory through Roslyn (`CSharpCompilation.Create`), `HPRobot.Mcp.Server.Tests` can be built and restored on any machine, including headless CI/CD agents where Autodesk software is not installed.
2. **From Observation 1 & 2**:
   Resolving `Interop.RobotOM.dll` via the 3-step hierarchy (`HPROBOT_ROBOT_DIR` → Registry `LocalServer32` → Default `%ProgramFiles%\Autodesk\...`) successfully finds `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll` on this developer machine, while gracefully falling back to returning `null` if absent.
3. **From Observation 1 & 2**:
   Using `Assert.SkipWhen(compiled is null, "Autodesk Robot Structural Analysis Professional 2026 not installed (Interop.RobotOM.dll not found)")` ensures:
   - On machines without Robot: tests dynamically skip without failing the build or test suite.
   - On machines with Robot: tests actively compile all 12 embedded seeds against the real vendor metadata.
4. **From Observation 3**:
   Adding `<Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />` directly following `HPRobot.Mcp.Server/HPRobot.Mcp.Server.csproj` in `HPRobot/HPRobot.slnx` integrates the project into the solution structure, matching `HPEtabs.slnx` and `HPSap2000.slnx`.
5. **From Observation 4 & 5**:
   Because `HPRobot.Mcp.Server.Tests` targets `net10.0` while `HPRobot.McpBridge.Tests` targets `net8.0-windows`, uses separate bin directories, and performs in-memory analysis without binding live named pipes or spawning `robot.exe`, there is mathematically zero risk of interference with the 197 passing bridge tests.

---

## 3. Caveats

1. **Vendor Interop Types**:
   This test suite compiles scripts against `Interop.RobotOM.dll` metadata only; it does not invoke the Robot FEA solver or COM automation at runtime (live COM execution is the responsibility of `HPRobot/tools/harness/run-live-verify.ps1`).
2. **Test Name Alignment**:
   In `PROJECT.md` line 233, the test file is listed as `SeedCompilationTests.cs`. In sister hosts, the filename is `SeedLibraryCompileTests.cs`. The implementation worker may use `SeedCompilationTests.cs` as specified in `PROJECT.md` while incorporating the exact logic verified from `SeedLibraryCompileTests.cs`.

---

## 4. Conclusion

1. **Seed Compilation Test Design**:
   - Implement `SeedCompilationTests.cs` in `HPRobot.Mcp.Server.Tests` using dynamic Roslyn compilation.
   - Use `FindWrapper()` with the 3-tier lookup chain to locate `Interop.RobotOM.dll`.
   - Use `Assert.SkipWhen(compiled is null, ...)` for graceful non-installed skipping.
   - Include 12 theory runs for seed compilation, 12 theory runs for transaction/tag validation, 1 fact for CS1061 API misuse rejection, and 1 fact for safety guard validation.
2. **Solution Integration**:
   - Add `<Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />` to `HPRobot/HPRobot.slnx`.
3. **Execution Commands**:
   - Solution build: `dotnet build HPRobot/HPRobot.slnx`
   - Bridge tests (197 tests): `dotnet test HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj`
   - Server tests (~80 tests): `dotnet test HPRobot/HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj`
   - All tests: `dotnet test HPRobot.slnx` (from `HPRobot/`)

---

## 5. Verification Method

To verify these findings:
1. **Inspect Interop File and Registry**:
   ```powershell
   Test-Path "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
   Get-ItemProperty -Path "Registry::HKEY_CLASSES_ROOT\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32"
   ```
2. **Inspect Sister Host Reference Implementation**:
   - `HPEtabs/HPEtabs.Mcp.Server.Tests/SeedLibraryCompileTests.cs`
   - `HPSap2000/HPSap2000.Mcp.Server.Tests/SeedLibraryCompileTests.cs`
3. **Verify Baseline Tests**:
   ```bash
   dotnet test HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj
   ```
   Assert: 197 passed, 0 failed, 0 skipped.
4. **Invalidation Conditions**:
   - If `Interop.RobotOM.dll` is removed from `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\`, `FindWrapper()` returns `null` and `Assert.SkipWhen` must skip the tests instead of failing.
