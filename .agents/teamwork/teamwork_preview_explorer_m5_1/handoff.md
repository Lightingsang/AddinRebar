# Handoff Report: Architecture Blueprint for HPDynamo.Mcp.Server.Tests

## 1. Observation

Direct investigation of sibling test projects, the shared MCP engine, and the HPDynamo subsystem yielded the following verified facts:

### 1.1 Project Structure & Build Configuration
1. **Sibling Server Test Projects**:
   - `HPEtabs/HPEtabs.Mcp.Server.Tests/HPEtabs.Mcp.Server.Tests.csproj` (lines 4-22):
     Targets `net10.0`, `OutputType=Exe`, `UseMicrosoftTestingPlatformRunner=true`. Package references: `xunit.v3` (3.1.0), `xunit.runner.visualstudio` (3.1.5), `Microsoft.Bcl.AsyncInterfaces` (10.0.12).
   - `HPNavis/HPNavis.Mcp.Server.Tests/HPNavis.Mcp.Server.Tests.csproj` (lines 4-22) and `HPCivil3d/HPCivil3d.Mcp.Server.Tests/HPCivil3d.Mcp.Server.Tests.csproj` mirror this exact layout.
   - Sibling project references strictly link:
     - The corresponding server project (`<ProjectReference Include="..\HPDynamo.Mcp.Server\HPDynamo.Mcp.Server.csproj"/>`)
     - `..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj`
     - `..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj`
     - `..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj`
   - Test double: Linked fake executor from shared core:
     `<Compile Include="..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs" Link="Fakes\FakeRevitExecutor.cs"/>`
   - `HPDynamo/Directory.Build.props` (lines 8-24): Pins SDK `10.0.300`, test runner to Microsoft Testing Platform, sets warnings, resolves `McpSharedDir` dynamically, and establishes `<Reference><Private>false</Private></Reference>` to prevent bin pollution.
   - `HPDynamo/HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj` (lines 17-20): Already declares:
     ```xml
     <ItemGroup>
         <InternalsVisibleTo Include="HPDynamo.Mcp.Server.Tests" />
         <InternalsVisibleTo Include="HPDynamo.E2E.Tests" />
     </ItemGroup>
     ```

### 1.2 Tool Surface & 24-Tool Invariant
2. **Dynamo Tool Configuration**:
   - `HPDynamo.Mcp.Server/Hosts/DynamoHostProfile.cs` (lines 18-52):
     - `CoreToolNames` has 4 tools: `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution`.
     - `SuppressedEngineToolName = "inspect_type"` (line 22).
     - `EnforceToolSurface(HostApplicationBuilder builder)` (lines 68-97): Explicitly strips `inspect_type` from `builder.Services` and `McpServerOptions.ToolCollection`.
   - **Tool Decomposition**:
     - 4 Core tools: `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution`.
     - 8 Meta tools: `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
     - 12 Seed tools: `run_dyn_file`, `get_active_graph_info`, `search_nodes_library`, `inspect_node_type`, `add_node_to_workspace`, `connect_node_ports`, `disconnect_node_ports`, `set_node_properties`, `get_node_output_values`, `export_graph_to_image`, `validate_dyn_file`, `create_code_block_node`.
     - **Exact Surface Invariant**: `4 + 8 + 12 = 24 tools` exposed to AI coding agents via `tools/list`.

### 1.3 Seed Manifest & Embedded Resources
3. **Embedded Resources vs Compiled Types**:
   - `HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj` (lines 33-34):
     `<Compile Remove="Registry\SeedLibrary\**\*.cs" />`
     `<EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)" />`
   - `HPDynamo.Mcp.Server/Registry/SeedLibrary/` contains exactly 12 tool folders across 8 categories (`CodeBlock`, `Data`, `Export`, `Graph`, `Library`, `Node`, `Wire`, `Workspace`).
   - Each tool directory contains 3 files: `code.cs`, `tool.json`, `examples.json`.
   - Total embedded resources: `12 * 3 = 36 manifest resources`.
   - Zero compiled seed script classes: `typeof(DynamoHostProfile).Assembly.GetTypes()` must contain 0 types derived from seed scripts.

### 1.4 Script Quality Rules (Clean Code Standards)
4. **Script Quality Engine**:
   - `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptQuality.cs` (lines 14-48, 71-100):
     - `ScriptQuality.Find(string code)` runs Roslyn syntax inspection against:
       - `Q-B1` (Error): Commented-out code (Rule Q-B1).
       - `Q-B2` (Error): Empty catch blocks without reason comments (Rule Q-B2).
       - `Q-B3` (Error): Script line count > 300 (Rule C2).
       - `Q-W1` (Warning): Block line count > 50 (Rule M3).
       - `Q-W2` (Warning): Nesting depth > 3.
       - `Q-W3` (Warning): Vague identifiers (`tmp`, `temp`, `data`, `obj`, `res`, `val`, `foo`, `bar`, `stuff`, `thing`).
       - `Q-W4` (Warning): Boolean parameter on local functions.
       - `Q-W5` (Warning): Swallowing catch blocks.
   - `scripts/m4_challenge/Program.cs` (lines 114-154) verified that all 12 HPDynamo seeds pass `ScriptQuality.Find` with **0 errors and 0 warnings**.
   - Sibling baseline standard (`SeedQualityTests.cs` across `HPNavis`, `HPEtabs`, `HPAutoCad`): All seeds clean, `Baseline = []`.

### 1.5 ScriptGuard & Security Barrier
5. **Dynamo Security Profile**:
   - `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` (lines 231-248):
     - `GuardProfile.Dynamo` denies:
       - Identifiers: `MessageBox`, `TaskDialog`.
       - Members: `Show`, `ShowModal`, `Exit`, `Quit`, `CloseApplication`.
       - Members on identifier: `app.Exit`, `app.Quit`.
       - Namespaces: `System.Windows.Forms`, `HPDynamo.McpBridge`, `HPRebar.McpBridge.Core.Host`.
     - Host-neutral deny rules in `ScriptGuard.Check`:
       - `System.IO`, `System.Net`, `System.Diagnostics`, `System.Reflection`, `Process.Start`.
       - `#r` and `#load` script directives.
       - `global::` prefix bypass attempts.
   - All 12 seeds pass `ScriptGuard.Check(seed.Code, GuardProfile.Dynamo)` with **0 violations**.

### 1.6 Registry Lifecycle Architecture
6. **Tool Lifecycle Services**:
   - `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLifecycleService.cs` coordinates:
     - `ProposeAsync`: Validates schema, performs AST analysis, checks `ScriptGuard` and `ScriptQuality`. Sets status to `ToolStatus.Draft`.
     - `TestAsync`: Runs test cases with `dryRun=true` against `IBridgeExecutor`. Sets status to `tested`.
     - `Publish`: Prepares review markdown, sets status to `pending_approval` (manual) or `published` (auto).
     - `Approve`: Transitions to `ToolStatus.Published`, notifies `DynamicToolRegistrar`.
     - `Manage`: Transitions to `deprecated` (removes from tool surface) or `draft` (restores).
   - Test harness in `HPRebar.Mcp.Server.Tests/Registry/ToolRegistryTests.cs` (lines 19-79): Uses `RegistryFixture` with temporary database and fake named pipe listener.

---

## 2. Logic Chain

1. **Host-Neutral Separation**:
   - `HPDynamo.Mcp.Server.Tests` is an out-of-process server test suite. It verifies the stdio JSON-RPC server and seed registry.
   - Because it tests host-neutral tools, profiles, manifest resources, and the registry lifecycle, it does NOT require Revit or Dynamo binaries to run.
   - Therefore, targeting plain `net10.0` (matching `HPDynamo.Mcp.Server.csproj`) is optimal and enables fast execution on any developer machine or CI pipeline.

2. **Strict Invariant Maintenance**:
   - Standard MCP servers in this ecosystem maintain an invariant of 24 tools.
   - Standard hosts have 4 core tools: `execute_<host>_code`, `get_<host>_context`, `inspect_type`, `cancel_execution`.
   - Dynamo requires an additional core tool: `execute_designscript` (to evaluate native Dynamo geometry and DesignScript expressions).
   - If `inspect_type` remained active, the tool count would be `5 + 8 + 12 = 25`.
   - To maintain the 24-tool invariant, `DynamoHostProfile.EnforceToolSurface` suppresses `inspect_type`.
   - The test suite must assert that `inspect_type` is absent, while `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, and `cancel_execution` are present, confirming the 24-tool total.

3. **Compilation Defense (Static Type vs Embedded Resource)**:
   - AI models authoring C# scripts might accidentally compile `.cs` files into the assembly.
   - The server must distribute seed scripts purely as embedded text resources, which are compiled at runtime by the bridge's Roslyn engine inside Revit.
   - The test suite must assert both that:
     1. Exactly 36 manifest resources exist in `SeedLibrary/`.
     2. No types defined in `HPDynamo.Mcp.Server.dll` originate from the seed files.

4. **Clean Code Rule Enforcement (C2 & M3)**:
   - Rule C2 stipulates that files must not exceed 300 lines.
   - Rule M3 stipulates that methods must not exceed 50 lines.
   - Bundling all server tests into 1 or 2 files would violate Rule C2 (sibling tests such as `HPRebar.Mcp.Server.Tests/Registry/ToolRegistryTests.cs` grew to 482 lines, which triggered clean-code audit findings).
   - To guarantee compliance with Rule C2, the test suite must be partitioned into 8 cohesive, single-responsibility files across 4 logical directories (`Surface/`, `Seeds/`, `Registry/`, `Protocol/`), keeping each file comfortably between 100 and 220 lines.

---

## 3. Caveats

1. **In-Process Bridge Tests are Out of Scope**:
   - `HPDynamo.McpBridge.Tests` (targeting `net8.0-windows7.0` and verifying Revit transactions, `TransactionGroup`, and `RevitDynamoModel`) is separate and handled in milestone M5-Bridge.
2. **Live Pipe Execution**:
   - Tests in this suite utilize `FakeRevitExecutor` linked from `HPRebar.Mcp.Server.Core.Tests/Fakes/` behind a real named pipe listener. They do not launch `Revit.exe`.
3. **No Baseline Exceptions Needed**:
   - Sibling test projects have historical baselines for legacy seeds. In HPDynamo, all 12 seeds are newly authored to 100% clean-code compliance; `Baseline` is strictly `[]`.

---

## 4. Conclusion & Architectural Blueprint

### 4.1 Project File Specification: `HPDynamo.Mcp.Server.Tests.csproj`

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
        <!-- global.json pins test.runner = Microsoft.Testing.Platform; xunit.v3 speaks it natively -->
        <OutputType>Exe</OutputType>
        <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="xunit.v3" Version="3.1.0" />
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
        <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />
    </ItemGroup>

    <ItemGroup>
        <!-- HPDynamo server and host-neutral shared engine dependencies -->
        <ProjectReference Include="..\HPDynamo.Mcp.Server\HPDynamo.Mcp.Server.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
    </ItemGroup>

    <ItemGroup>
        <!-- Shared fake executor linked from McpShared tests -->
        <Compile Include="..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs" Link="Fakes\FakeRevitExecutor.cs" />
    </ItemGroup>

</Project>
```

### 4.2 Solution Registration (`HPDynamo.slnx`)
Add the test project entry under the test projects section:
```xml
  <Project Path="HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj" />
```

### 4.3 Directory & File Layout

```
HPDynamo.Mcp.Server.Tests/
├── HPDynamo.Mcp.Server.Tests.csproj       (~40 lines)
├── Fakes/
│   └── FakeRevitExecutor.cs              (Linked from McpShared)
├── Fixtures/
│   └── DynamoRegistryFixture.cs          (~95 lines) - Temp db, tool store, pipe client & listener
├── Surface/
│   ├── ToolSurfaceTests.cs               (~160 lines) - 24 tools invariant, 4 core, 8 meta, inspect_type suppression
│   └── HostProfileBindingTests.cs        (~120 lines) - DynamoHostProfile invariants, options binding, scheme, CLI name
├── Seeds/
│   ├── SeedManifestTests.cs              (~110 lines) - 36 resources, 0 compiled classes, category alignment
│   ├── SeedSchemaValidationTests.cs      (~190 lines) - ToolRecord JSON schemas, argument parity, examples validation
│   ├── ScriptQualityTests.cs             (~85 lines)  - ScriptQuality.Find: 0 errors, 0 warnings (Q-B1, Q-B2, C2, M3)
│   └── ScriptGuardTests.cs               (~150 lines) - ScriptGuard.Check against GuardProfile.Dynamo + security boundaries
├── Registry/
│   ├── RegistryLifecycleTests.cs         (~210 lines) - Propose, test, publish, approve workflow & review generation
│   └── RegistrySearchAndManageTests.cs   (~170 lines) - search_tools, get_tool, manage_tool deprecate/restore
└── Protocol/
    └── DynamoToolsOverPipeTests.cs       (~180 lines) - Stdio tools over named pipe with FakeRevitExecutor
```

---

### 4.4 Detailed Test Suite Designs

#### Suite 1: `ToolSurfaceTests.cs` (Namespace: `HPDynamo.Mcp.Server.Tests.Surface`)
- **Purpose**: Verify the strict 24-tool catalog exposed by `HPDynamo.Mcp.Server` and safety annotations.
- **Test Methods**:
  1. `ToolSurface_ContainsExactly24ToolsAcrossCoreRegistryAndSeeds()`:
     - Builds host with `McpServerHost.CreateBuilder([], DynamoHostProfile.Instance)`.
     - Applies `DynamoHostProfile.EnforceToolSurface(builder)`.
     - Queries `host.Services.GetServices<McpServerTool>()` (12 static tools: 4 core + 8 registry).
     - Loads 12 seeds via `SeedInstaller.LoadSeeds(typeof(DynamoHostProfile).Assembly)`.
     - Asserts combined distinct tool count equals **exactly 24**.
  2. `ToolSurface_InspectTypeTool_IsSuppressed()`:
     - Asserts `inspect_type` is NOT present in registered `McpServerTool` collection or `McpServerOptions.ToolCollection`.
  3. `CoreTools_ContainsExpectedFourToolsWithCorrectAnnotations()`:
     - Asserts presence of `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution`.
     - Verifies `execute_dynamo_code`: `DestructiveHint == true`, `ReadOnlyHint == false`.
     - Verifies `execute_designscript`: `DestructiveHint == true`, `ReadOnlyHint == false`.
     - Verifies `get_dynamo_context`: `ReadOnlyHint == true`, `DestructiveHint == false`.
  4. `RegistryMetaTools_ContainsAllEightRequiredTools()`:
     - Verifies `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
  5. `Seeds_ContainsAllTwelvePublishedSeedTools()`:
     - Verifies the 12 seeds: `run_dyn_file`, `get_active_graph_info`, `search_nodes_library`, `inspect_node_type`, `add_node_to_workspace`, `connect_node_ports`, `disconnect_node_ports`, `set_node_properties`, `get_node_output_values`, `export_graph_to_image`, `validate_dyn_file`, `create_code_block_node`.

#### Suite 2: `HostProfileBindingTests.cs` (Namespace: `HPDynamo.Mcp.Server.Tests.Surface`)
- **Purpose**: Verify Dynamo host profile configuration, options binding, and conventions.
- **Test Methods**:
  1. `DynamoHostProfile_DeclaresRequiredMetadata()`:
     - Asserts `HostId == "dynamo"`, `DisplayName == "Dynamo"`, `ServerName == "HPDynamo MCP"`.
     - Pipe name: `profile.PipeName(2026) == "hpdynamo-mcp-2026"`.
     - Method prefix: `profile.Method("execute") == "dynamo.execute"`.
     - Default version: `2026`.
     - Max timeout: `HostScriptContracts.DynamoHeavyMaxTimeoutSeconds` (600s).
     - Script imports match `HostScriptContracts.DynamoImports`.
  2. `ConfigureOptions_BindsPipeAndRegistryPaths()`:
     - Executes `McpServerHost.ConfigureOptions`.
     - Asserts `BridgeOptions.PipeName == "hpdynamo-mcp-2026"`.
     - Asserts `RegistryOptions.LibraryPath` and `DbPath` contain `HPDynamo\McpServer`.
  3. `DynamoHostProfile_Categories_CoverAllRequiredDomains()`:
     - Asserts categories include `Nodes`, `Wire`, `Workspace`, `Geometry`, `Revit`, `Python`, `Package`, `Generic`, `CodeBlock`, `Data`, `Export`, `Graph`, `Library`.

#### Suite 3: `SeedManifestTests.cs` (Namespace: `HPDynamo.Mcp.Server.Tests.Seeds`)
- **Purpose**: Verify manifest resource embedding, zero compiled static classes, and folder alignment.
- **Test Methods**:
  1. `ManifestResources_ContainsExactly36Resources()`:
     - Inspects `typeof(DynamoHostProfile).Assembly.GetManifestResourceNames()`.
     - Filters by prefix `SeedLibrary/`.
     - Asserts count equals 36 (12 seeds * 3 files: `tool.json`, `code.cs`, `examples.json`).
  2. `AssemblyTypes_ContainsZeroCompiledSeedScriptClasses()`:
     - Queries `typeof(DynamoHostProfile).Assembly.GetTypes()`.
     - Asserts none of the types match seed names (`run_dyn_file`, `add_node_to_workspace`, etc.) or reside in `HPDynamo.Mcp.Server.Registry.SeedLibrary`.
     - Validates that `<Compile Remove="Registry\SeedLibrary\**\*.cs" />` is effective.
  3. `SeedCategories_MatchPhysicalDirectoryStructure()`:
     - Asserts that for every seed, `SeedLibrary/{Category}/{Name}/` exactly matches its folder path.

#### Suite 4: `SeedSchemaValidationTests.cs` (Namespace: `HPDynamo.Mcp.Server.Tests.Seeds`)
- **Purpose**: Verify tool schemas, argument parity, and example structures for all 12 seeds.
- **Test Methods**:
  1. `EverySeed_ToolJson_ConformsToToolRecordSchema(string seedKey)` (`[Theory]`, `[MemberData]`):
     - Parses `tool.json` as `ToolRecord`.
     - Asserts `Name` matches directory, `Category` is valid, `Host == "dynamo"`, `HostVersions` contains `"2026"`, `Status == "published"`.
     - Asserts `Description.Length >= 20`, `Transaction` is in `["auto", "manual", "none"]`, `TimeoutSeconds` in `[5, 600]`.
     - Asserts `InputSchema` is JSON object (`type: "object"`).
  2. `EverySeed_ExamplesJson_HasValidStructureAndParity(string seedKey)` (`[Theory]`, `[MemberData]`):
     - Parses `examples.json`.
     - Asserts array length >= 2 with distinct arguments.
     - Asserts each example includes `title` and `args`.
     - Asserts each arg key is declared in `InputSchema.properties`.
     - Asserts all `required` schema fields are present in every example.
     - Asserts no double escaping (`\\\\` or `\\"`).
  3. `EverySeed_CodeArgs_MatchesInputSchemaProperties(string seedKey)` (`[Theory]`, `[MemberData]`):
     - Analyzes seed code via `ScriptAnalyzer.Analyze(code, AnalyzerProfile.Dynamo)`.
     - Asserts `readArgs` is a subset of declared schema properties.
     - Asserts all declared schema properties are utilized in code.

#### Suite 5: `ScriptQualityTests.cs` (Namespace: `HPDynamo.Mcp.Server.Tests.Seeds`)
- **Purpose**: Enforce Clean Code AST quality rules across all 12 embedded seeds.
- **Test Methods**:
  1. `AllSeeds_PassScriptQualityWithZeroErrors()`:
     - Iterates over all 12 seeds.
     - Runs `ScriptQuality.Find(seed.Code)`.
     - Asserts `findings.Where(f => f.Severity == ScriptQuality.Error)` is empty.
     - Verifies:
       - No commented-out code (`Q-B1`).
       - No empty catch blocks without reason (`Q-B2`).
       - Script line count <= 300 (`Q-B3` / Rule C2).
  2. `AllSeeds_PassScriptQualityWithZeroWarnings()`:
     - Asserts `findings.Where(f => f.Severity == ScriptQuality.Warning)` is empty.
     - Verifies:
       - Block lines <= 50 (`Q-W1` / Rule M3).
       - Max nesting depth <= 3 (`Q-W2`).
       - No vague variable names (`Q-W3`).
       - No boolean function parameters (`Q-W4`).
       - No swallowing catch blocks (`Q-W5`).
  3. `Baseline_IsEmpty()`:
     - Asserts `Baseline.Length == 0`, confirming 100% compliance without legacy debt.

#### Suite 6: `ScriptGuardTests.cs` (Namespace: `HPDynamo.Mcp.Server.Tests.Seeds`)
- **Purpose**: Verify security guard compliance for all 12 seeds and test security barriers.
- **Test Methods**:
  1. `AllSeeds_PassScriptGuardWithZeroViolations(string seedKey)` (`[Theory]`, `[MemberData]`):
     - Runs `ScriptGuard.Check(seed.Code, GuardProfile.Dynamo)`.
     - Asserts `diagnostics.Count == 0`.
  2. `ScriptGuard_DeniesModalDialogs()`:
     - Negative test: Scripts calling `MessageBox.Show(...)` or `TaskDialog.Show(...)` return guard violations.
  3. `ScriptGuard_DeniesForbiddenNamespacesAndProcess()`:
     - Negative test: Scripts referencing `System.IO.File`, `System.Net.Http`, `System.Diagnostics.Process`, or `System.Windows.Forms` are blocked.
  4. `ScriptGuard_DeniesScriptTriviaAndDirectives()`:
     - Negative test: `#r "foo.dll"` and `#load "script.csx"` are rejected.
  5. `ScriptGuard_DeniesGlobalNamespaceBypass()`:
     - Negative test: `global::System.IO.File.Delete("test")` is rejected.

#### Suite 7: `RegistryLifecycleTests.cs` (Namespace: `HPDynamo.Mcp.Server.Tests.Registry`)
- **Purpose**: Test end-to-end dynamic tool proposal, testing, publishing, and approval lifecycle.
- **Harness**: `DynamoRegistryFixture` (temp database, directory, and `FakeRevitExecutor`).
- **Test Methods**:
  1. `Lifecycle_ProposeValidTool_CreatesDraftAndSavesMetadata()`:
     - Proposes valid candidate script with schema and examples.
     - Asserts `outcome.Accepted == true`, `outcome.Record.Status == ToolStatus.Draft`, `tool.json` exists on disk.
  2. `Lifecycle_TestTool_ExecutesDryRunAndTransitionsToTested()`:
     - Calls `lifecycle.TestAsync("custom_dynamo_tool", null, false)`.
     - Asserts 2 examples pass, `dryRun=true` passed to fake executor, status transitions to `tested`.
  3. `Lifecycle_PublishManualPolicy_TransitionsToPendingApprovalAndGeneratesReview()`:
     - Calls `lifecycle.Publish("custom_dynamo_tool")`.
     - Asserts status is `pending_approval`, review markdown exists with code quality and dryRun results.
  4. `Lifecycle_ApproveTool_TransitionsToPublishedAndUpdatesToolSurface()`:
     - Calls `lifecycle.Approve("custom_dynamo_tool", "lead_engineer")`.
     - Asserts status is `published`, `DynamicToolRegistrar.RegisteredNames` contains tool name.
  5. `Lifecycle_PublishAutoPolicy_DirectlyPublishesTestedTool()`:
     - Configures `RegistryOptions.PublishPolicy = "auto"`.
     - Proposes, tests, and publishes -> status is immediately `published`.
  6. `Lifecycle_ProposeInvalidTool_ReturnsFailuresAndSavesNothing()`:
     - Submits script with `System.IO.File.Delete` or `COMPILE_ERROR`.
     - Asserts `Accepted == false`, error report lists issues, no files written to store.

#### Suite 8: `RegistrySearchAndManageTests.cs` (Namespace: `HPDynamo.Mcp.Server.Tests.Registry`)
- **Purpose**: Test catalog search, tool retrieval, and deprecation/restoration management.
- **Test Methods**:
  1. `Manage_DeprecateTool_RemovesFromActiveSurface()`:
     - Calls `lifecycle.Manage(toolName, "deprecate", "replaced by v2", "admin")`.
     - Asserts status is `ToolStatus.Deprecated`, tool unregistered from registrar.
  2. `Manage_RestoreTool_TransitionsBackToDraft()`:
     - Calls `lifecycle.Manage(toolName, "restore", null, "admin")`.
     - Asserts status is `ToolStatus.Draft`.
  3. `SearchTools_FindsMatchingPublishedTools()`:
     - Calls `f.Manager.SearchAsync("node", "Node", null)`.
     - Asserts matching tools returned with categories and descriptions.
  4. `GetTool_ReturnsFullRecordWithSchemaAndExamples()`:
     - Calls `f.Manager.Get(toolName)`.
     - Asserts schema, code, and examples match stored record.

#### Suite 9: `DynamoToolsOverPipeTests.cs` (Namespace: `HPDynamo.Mcp.Server.Tests.Protocol`)
- **Purpose**: Verify tool execution and context shaping over named pipe protocol using `FakeRevitExecutor`.
- **Test Methods**:
  1. `ContextTool_ReturnsDynamoInfoAndHidesRevitOnlyFields()`:
     - Configures fake executor with `DynamoInfo` (`isLive=true`, `nodeCount=15`, `activeWorkspaceName="Main"`).
     - Calls `DynamoContextTool.GetContextAsync()`.
     - Verifies JSON contains `host: "dynamo"`, `hostVersion: "2026"`, `dynamo: {...}`.
     - Verifies `revitVersion` and `isFamily` are omitted for non-Revit context.
  2. `ExecuteDynamoCode_PassesParametersAndReceivesResult()`:
     - Invokes `ExecuteDynamoCodeTool.ExecuteAsync("return 42;", "auto", dryRun: true)`.
     - Verifies `LastExecuteRequest.Code` matches and `DryRun == true`.
  3. `ExecuteDesignScript_PrefixesLabelAndPassesCode()`:
     - Invokes `ExecuteDesignScriptTool.ExecuteAsync("pt = Point.ByCoordinates(0,0,0);", "auto", dryRun: false, label: "CreatePoint")`.
     - Verifies `LastExecuteRequest.Label` is `"DesignScript: CreatePoint"`.
  4. `CancelExecution_DispatchesCancellation()`:
     - Calls cancellation tool and asserts `CancelCalls` incremented on fake executor.

---

### 4.5 Clean Code Compliance Matrix

| Rule | Requirement | Architectural Enforcement in Blueprint |
|---|---|---|
| **C2** | Max file length <= 300 lines | Suite partitioned across 8 files; all projected between 85 and 210 lines. |
| **M3** | Max method length <= 50 lines | Test methods scoped to single assertions or parameterized theories (15-40 lines). |
| **Q-B1** | No commented-out code | Zero commented lines allowed; AST verified in `ScriptQualityTests`. |
| **Q-B2** | No empty catch without comment | No empty catch blocks in test code or fixtures. |
| **PCC-N1**| Descriptive test names | Expressive names stating Subject, State, and Expected Behavior. |
| **PCC-D1**| Strict dependency direction | Tests only reference `HPDynamo.Mcp.Server` and `McpShared`; no cross-references. |

---

## 5. Verification Method

Once implemented by the downstream coding agent, independently verify the test suite using:

1. **Build Verification**:
   ```bash
   dotnet build HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj -c Debug
   ```
   *Expected*: Build succeeds with 0 errors and 0 warnings.

2. **Test Execution**:
   ```bash
   dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj
   ```
   *Expected*: 100% tests pass (projected ~45-55 tests across the 9 suites).

3. **Solution Build & Regression Check**:
   ```bash
   dotnet build HPDynamo/HPDynamo.slnx -c Debug
   dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj
   ```
   *Expected*: Zero regressions across existing McpShared tests (815 tests passing).

4. **Invalidation Conditions**:
   - Any test file exceeding 300 lines (violates Rule C2).
   - Any test method exceeding 50 lines (violates Rule M3).
   - Direct references to `HPDynamo.McpBridge` or `Autodesk.Revit.*` in `HPDynamo.Mcp.Server.Tests.csproj`.
   - Any failure in the 24-tool invariant (e.g. failure to suppress `inspect_type`).
