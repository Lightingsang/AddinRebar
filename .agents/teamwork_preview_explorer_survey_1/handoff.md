# Handoff Report — McpShared Survey & Architectural Design for Tekla Structures 2025

- **Author**: Teamwork Preview Explorer Survey 1 (`teamwork_preview_explorer_survey_1`)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_1`
- **Date**: 2026-09-22
- **Handoff Type**: Hard (Investigation complete)

---

## 1. Observation

1. **Test Suite Baseline**:
   - Running `dotnet test HPRebar.Mcp.Server.Core.Tests` in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared` produced:
     ```
     Running tests from ...\HPRebar.Mcp.Server.Core.Tests.dll (net10.0|x64) passed (3s 413ms)
     Test run summary: Passed!
       total: 613
       failed: 0
       succeeded: 613
       skipped: 0
     ```
   - Running `dotnet test HPRebar.McpBridge.Core.Net48Tests` in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared` produced:
     ```
     Running tests from ...\HPRebar.McpBridge.Core.Net48Tests.exe (net48|x64) passed (2s 462ms)
     Test run summary: Passed!
       total: 72
       failed: 0
       succeeded: 72
       skipped: 0
     ```
2. **Existing Host Contracts in `McpShared`**:
   - `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs` lines 15–53 define host IDs:
     - `RevitHost = "revit"`, `AutocadHost = "autocad"`, `NavisHost = "navis"`, `EtabsHost = "etabs"`, `Civil3dHost = "civil3d"`, `Sap2000Host = "sap2000"`, `PowerBiHost = "powerbi"`, `ExcelHost = "excel"`, `RobotHost = "robot"`.
     - Lines 68–81 implement `PipeNaming.For(string host, int version)` returning `hp{host}-mcp-{version}`.
   - `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs` lines 35–43 define wire method prefixes:
     - `RevitPrefix = "revit."`, `AutocadPrefix = "autocad."`, `NavisPrefix = "navis."`, `EtabsPrefix = "etabs."`, `Civil3dPrefix = "civil3d."`, `Sap2000Prefix = "sap2000."`, `PowerBiPrefix = "powerbi."`, `ExcelPrefix = "excel."`, `RobotPrefix = "robot."`.
     - Lines 55–62 define `Suffix(method)` extracting the substring after the first `.`.
   - `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs` lines 12–209 define per-host imports, globals, and heavy timeout ceilings.
   - `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs` lines 24–45 define host-specific nullable info blocks on `ContextResult` (`Autocad`, `Navis`, `Etabs`, `Civil3d`, `Sap2000`, `PowerBi`, `Excel`, `Robot`).
   - `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` lines 12–193 define static instances for `Revit`, `Autocad`, `Navis`, `Etabs`, `Sap2000`, `PowerBi`, `Excel`, `Robot`, `Civil3d`.
   - `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs` lines 10–56 define transaction recognition rules for the 9 hosts.
3. **Runtime Precedent for .NET Framework 4.8**:
   - `HPNavis/README.md` lines 8–12 document that Navisworks runs on .NET Framework 4.8 and uses `PluginAssemblyResolver` to isolate Roslyn and system dependencies in the absence of `AssemblyLoadContext`.
   - `HPRebar.McpBridge.Core` is multi-targeted for `net8.0;net48`. Lines 56–58 of `MainThreadQueue.cs` and lines 211–225 of `GuardProfile.cs` demonstrate `#if NET48` conditional compilation for runtime compatibility (Stopwatch clock, `IReadOnlyCollection` vs `IReadOnlySet`, `PipeSecurity`).
4. **Method Dispatching Mechanics**:
   - `McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs` lines 106–149 route incoming JSON-RPC requests based strictly on `JsonRpcMethods.Suffix(method)`.
   - Suffixes (`ping`, `cancel`, `inspect`, `analyze`, `context`, `execute`) match regardless of the host prefix (`tekla.execute` maps to `ExecuteSuffix`).
   - Lines 179–185: `ProgressMethodFor("tekla.execute")` produces `"tekla.progress"`.

---

## 2. Logic Chain

1. **Additive Contract Safety**:
   - *Premise*: `PipeNaming.For`, `JsonRpcMethods.For`, `ContextResult`, and `HostScriptContracts` are consumed by 9 distinct hosts.
   - *Evidence*: `BridgeJson.Options` configures `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` (`BridgeJson.cs:18`).
   - *Deduction*: Adding `TeklaHost = "tekla"`, `TeklaPrefix = "tekla."`, `TeklaImports`, `TeklaGlobals`, and a nullable `TeklaInfo? Tekla` to `ContextResult` introduces zero breaking changes because unused nullable fields are completely omitted from JSON payloads of existing hosts.
2. **Seamless Dispatch Compatibility**:
   - *Premise*: The server exe sends `tekla.execute` and `tekla.context` down the named pipe.
   - *Evidence*: `RequestDispatcher.DispatchAsync` executes `switch (JsonRpcMethods.Suffix(method))` (`RequestDispatcher.cs:106`).
   - *Deduction*: Because `"tekla.execute"` yields suffix `"execute"` and `"tekla.context"` yields suffix `"context"`, `RequestDispatcher` requires zero structural modifications to handle Tekla requests.
3. **NET Framework 4.8 In-Process Model**:
   - *Premise*: Tekla Structures 2025 runs on CLR v4.0.30319 (.NET Framework 4.8).
   - *Evidence*: `HPNavis` successfully operates as an in-process plugin on .NET Framework 4.8 using `HPRebar.McpBridge.Core`'s `net48` target and `HPRebar.Mcp.Contracts`'s `net48` target, verified by 72 tests in `HPRebar.McpBridge.Core.Net48Tests`.
   - *Deduction*: `HPTekla.McpBridge` can follow the identical design pattern: target `net48`, reference the `net48` assets of `McpShared`, and reuse `PluginAssemblyResolver` to guarantee reliable Roslyn script compilation without runtime DLL collisions.
4. **Host Isolation Verification**:
   - *Premise*: McpShared must never take a reference to any host API assembly.
   - *Evidence*: `HostNeutralityTests.Shared_assemblies_reference_no_host_api` dynamically asserts that no assembly referenced by McpShared starts with banned host names (`RevitAPI`, `AcDbMgd`, etc.).
   - *Deduction*: Adding `"Tekla.Structures"` to the deny-list in `HostNeutralityTests` will formally guard against accidental architectural cross-contamination.

---

## 3. Caveats

1. **Tekla Structures Installation Check**: Tekla Structures 2025.0 installation directory was not directly accessed in this survey because the survey was conducted purely within `McpShared` codebase boundaries.
2. **Commit Policy on `dryRun`**: Tekla Open API does not implement standard C# `Transaction` objects like Revit; it relies on `model.CommitChanges()`. The survey proposes placing `"CommitChanges"` under `deniedMembersOnIdentifier: ["model"] = new[] { "CommitChanges" }` in `GuardProfile.Tekla` so that the bridge executor retains authoritative control over committing only when `dryRun == false`. An alternative is letting scripts call it, but that would bypass `dryRun`. The bridge-owned commit model is recommended.

---

## 4. Conclusion

- `McpShared` is fully ready and architected to accept Trimble Tekla Structures 2025 as its 10th CAD host.
- Exact required additions in `McpShared`:
  1. `PipeNaming.cs`: Define `TeklaHost = "tekla"` and `TeklaHost => "hptekla-mcp-" + version`.
  2. `JsonRpcMethods.cs`: Define `TeklaPrefix = "tekla."`.
  3. `HostScriptContracts.cs`: Define `TeklaImports`, `TeklaGlobals`, and `TeklaHeavyMaxTimeoutSeconds = 600`.
  4. `ContextMessages.cs`: Add `TeklaInfo? Tekla` to `ContextResult` and declare `TeklaInfo` record.
  5. `GuardProfile.cs`: Declare `GuardProfile.Tekla` blocking `MessageBox`, `Picker` (interactive picking), application exit, and `model.CommitChanges()`.
  6. `AnalyzerProfile.cs`: Declare `AnalyzerProfile.Tekla`.
- Test additions required:
  - `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaTestProfile.cs` & `TeklaProfileTests.cs`.
  - Update `HostNeutralityTests.cs` (add `"Tekla.Structures"` to banned assembly checks).
  - Update `ScriptCompilerNet48Tests.cs` (add Tekla profile guard/analyzer verification on `net48`).

Detailed findings, code snippets, and complete comparative matrix are available in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_1\report.md`

---

## 5. Verification Method

To verify these conclusions independently:
1. **Run Current Test Suites**:
   ```bash
   cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared"
   dotnet test HPRebar.Mcp.Server.Core.Tests
   dotnet test HPRebar.McpBridge.Core.Net48Tests
   ```
   *Expected*: 613/613 tests pass in Server.Core.Tests, 72/72 tests pass in Net48Tests.
2. **Inspect Host Neutrality**:
   Inspect `McpShared/HPRebar.Mcp.Server.Core.Tests/HostNeutralityTests.cs` to verify how shared assemblies are tested against host assembly references.
3. **Inspect .NET Framework 4.8 In-Process Precedent**:
   View `HPNavis/README.md` and `HPNavis/HPNavis.McpBridge/PluginAssemblyResolver.cs` to confirm how .NET Framework 4.8 handles Roslyn compiler binding.
