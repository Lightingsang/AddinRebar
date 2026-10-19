# HPGrasshopper E2E Acceptance Test Suite Architecture & Harness Design
**Milestone**: M5.1 (E2E Test Architecture & Harness Explorer)  
**Target Solution**: `HPGrasshopper/HPGrasshopper.slnx`  
**Test Project**: `HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj`  
**Clean Code Standards**: HP Clean Code Core (`HP_CLEAN_CODE_CORE.md`, Rules C2, M3, Q-B1, Q-B2)

---

## 1. Executive Summary & Core Philosophy

The `HPGrasshopper.E2E.Tests` suite is an **opaque-box, requirement-driven acceptance test framework** for the HPGrasshopper Model Context Protocol subsystem. It verifies the complete functional surface defined in `ORIGINAL_REQUEST.md` (## 2026-10-10T06:56:11Z) and `PROJECT.md` across 12 feature areas through 4 rigorous acceptance tiers (Tiers 1–4) plus Tier 5 adversarial hardening.

### Non-Negotiable Architectural Invariants
1. **100% Headless & Unattended Execution**: Zero dependency on active GUI sessions of Autodesk Revit, Rhinoceros 8, or Rhino.Inside.Revit.
2. **Zero Native Host Reference Pollution**: The test project references **only** `McpShared` assemblies (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`). Zero references to `RevitAPI.dll`, `RhinoCommon.dll`, `Grasshopper.dll`, or `RhinoInside.Revit.dll`.
3. **Zero Modal Popups**: Complete elimination of Windows `STATUS_HARD_ERROR` modal dialogs, message boxes, and interactive blocking prompts through pure managed test doubles and error mode isolation.
4. **Clean Code Governance Compliance**:
   - Every file strictly $\le 300$ lines (Rule C2).
   - Every method strictly $\le 50$ lines (Rule M3).
   - 0 dead or commented-out code (Rule Q-B1).
   - 0 unannotated empty catch blocks (Rule Q-B2).
5. **Modern Test Runner Runtime**: Built on `.NET 10.0` (`net10.0`), xUnit v3 (`xunit.v3` 3.1.0, `xunit.runner.visualstudio` 3.1.5), and Microsoft Testing Platform runner (`UseMicrosoftTestingPlatformRunner=true`).

---

## 2. Project Configuration & Solution Integration

### 2.1 Project File Specification: `HPGrasshopper.E2E.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>HPGrasshopper.E2E.Tests</RootNamespace>
    <Configurations>Debug;Release</Configurations>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <NoWarn>$(NoWarn);xUnit1051;NU1701</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="3.1.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj" />
  </ItemGroup>

</Project>
```

### 2.2 Assembly Isolation Rationale
- **`HPRebar.Mcp.Contracts`**: Provides wire DTOs (`ExecuteRequest`, `ExecuteResult`, `ContextResult`, JSON-RPC envelopes).
- **`HPRebar.McpBridge.Core`**: Provides script guards, Roslyn compiler options, `GuardProfile`, `AnalyzerProfile`, and settings abstractions.
- **`HPRebar.Mcp.Server.Core`**: Provides `IHostProfile`, `HostProfile`, dynamic tool registry engine, validation services, and catalog metadata.
- **Exclusion of `HPGrasshopper.McpBridge`**: Prevents transitive loading of `RhinoCommon` and `RevitAPI`, ensuring tests run on clean CI/CD agents where neither CAD/BIM host is installed.

### 2.3 Solution Wiring: `HPGrasshopper.slnx`

Add `HPGrasshopper.E2E.Tests.csproj` to `HPGrasshopper/HPGrasshopper.slnx` immediately following the existing test projects:

```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="Directory.Build.props" />
  </Folder>
  <Project Path="HPGrasshopper.McpBridge/HPGrasshopper.McpBridge.csproj" />
  <Project Path="HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj" />
  <Project Path="HPGrasshopper.Mcp.Server/HPGrasshopper.Mcp.Server.csproj" />
  <Project Path="HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj" />
  <Project Path="HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

---

## 3. Directory Layout & Modular Structure

```
HPGrasshopper.E2E.Tests/
├── HPGrasshopper.E2E.Tests.csproj           # Project configuration (<30 lines)
├── Contracts/
│   ├── GrasshopperE2EContracts.cs           # Wire constants, profiles, tools (<120 lines)
│   └── GrasshopperE2EModels.cs              # High-fidelity domain records (<90 lines)
├── Harness/
│   ├── GrasshopperE2EHarness.cs             # Static factories, helpers, PNG stub (<150 lines)
│   ├── GrasshopperE2EFakeBridge.cs          # In-memory dual-layer simulator (<220 lines)
│   └── GrasshopperPipeSimulator.cs          # Managed duplex named pipe server (<180 lines)
├── Tier1_FeatureCoverage/
│   ├── Tier1_ProtocolAndServerTests.cs      # Features 1, 8, 9, 10, 12 (<180 lines)
│   ├── Tier1_CoreToolsAndBridgeTests.cs     # Features 2, 3, 4, 5, 7 (<180 lines)
│   └── Tier1_ExecutionAndSafetyTests.cs     # Features 6, 11 (<160 lines)
├── Tier2_BoundaryCorner/
│   ├── Tier2_BoundaryAndProtocolTests.cs    # Protocol bounds, payload size (<170 lines)
│   ├── Tier2_ScriptGuardAndSecurityTests.cs # AST bounds, deny-list, opt-in (<180 lines)
│   └── Tier2_SeedStressAndRollbackTests.cs  # Seed vectors, dual rollback bounds (<170 lines)
├── Tier3_CrossFeature/
│   └── Tier3_CrossFeatureInteractionTests.cs# Pairwise feature pipelines (<190 lines)
├── Tier4_RealWorldScenarios/
│   └── Tier4_RealWorldScenariosTests.cs     # 6 end-to-end engineering pipelines (<210 lines)
└── Tier5_AdversarialHardening/
    └── Tier5_AdversarialHardeningTests.cs   # Mutation, fuzzing, bypass probing (<180 lines)
```

---

## 4. Test Harness Design

### 4.1 Wire Contracts: `GrasshopperE2EContracts.cs`
Defines authoritative constants matching `HostProfile.Grasshopper` and `GuardProfile.Grasshopper`:
- **Host Identity**: `HostKey = "grasshopper"`, `DefaultHostVersion = 2026`, `ExpectedPipeName = "hpgrasshopper-mcp-2026"`, `MethodPrefix = "grasshopper."`.
- **Error Codes**:
  - `-32001`: Opt-in AI execution disabled (`OptInDisabledErrorCode`).
  - `-32002`: Roslyn ScriptGuard violation or compilation error.
  - `-32003`: Request cancelled or timed out.
  - `-32601`: Method not found.
  - `-32700`: Parse error.
- **Opt-In Gate Text**: `OptInDisabledMessageFragment = "Allow AI Grasshopper execution"`.
- **Authorized Tool Surface**:
  - **5 Core Tools**: `read_grasshopper_canvas`, `run_grasshopper_player`, `batch_edit_grasshopper_canvas`, `execute_grasshopper_code`, `export_grasshopper_canvas_image`.
  - **2 Engine Utilities**: `get_grasshopper_context`, `cancel_execution`.
  - **8 Registry Meta Tools**: `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
  - **12 Embedded Seeds**: `create_levels_grids`, `generate_columns_beams`, `create_floors_slabs`, `generate_rebar_curves`, `generate_stirrup_distribution`, `facade_panelization`, `curtain_grid_layout`, `create_wall_openings`, `directshape_from_geometry`, `query_revit_geometry`, `evaluate_solar_radiation`, `export_model_to_rhino`.
  - **Suppressed Engine Tool**: `inspect_type`.
- **Script Guard Deny-Lists**:
  - Forbidden namespaces: `System.IO`, `System.Net`, `System.Diagnostics.Process`, `System.Reflection`, `System.Windows.Forms`.
  - Forbidden directives: `#r `, `#load `.

### 4.2 High-Fidelity Domain Models: `GrasshopperE2EModels.cs`
- `GrasshopperInfoRecord`: Complete session state (connection, active definition name/path, component/wire counts, solver status, versions, Rhino.Inside flag, opt-in flag).
- `GrasshopperComponentSpec`: Component descriptor (Guid, Name, NickName, Category, SubCategory, X, Y, InPorts, OutPorts, Properties, Status).
- `GrasshopperWireSpec`: Wire connection descriptor (SourceComponentId, SourcePortIndex, TargetComponentId, TargetPortIndex).
- `GrasshopperCanvasSpec`: Canvas workspace container with serializable JSON format.
- `GrasshopperExecutionRequest`: Request parameters (Code, Transaction, DryRun, TimeoutSeconds, ArgsJson).
- `GrasshopperExecutionResult`: Response DTO (Success, OutputJson, RolledBack, CreatedElementIds, Logs, DurationMs, ErrorMessage, ErrorCode).
- `GrasshopperScriptQualityResult`: ADR-0007 quality walker report (IsValid, LineCount, Errors, Warnings).

### 4.3 In-Memory Simulator: `GrasshopperE2EFakeBridge.cs`
Simulates internal bridge behavior with exact fidelity without any native DLL:
- **Security Opt-In Gate**: Rejects any execution with error `-32001` if `IsExecutionEnabled == false`.
- **ADR-0007 Script Quality Walker**:
  - Rejects scripts $> 300$ lines.
  - Rejects forbidden namespaces (`System.IO`, `System.Windows.Forms`, etc.).
  - Rejects `#r` / `#load` directives.
  - Rejects empty catch blocks `catch {}` lacking reason comments.
  - Rejects commented-out code statements.
- **Dual-Layer Rollback Simulation**:
  - Outer: Revit `TransactionGroup` rollback tracking. Created element IDs are held in a transaction stack; popped and discarded if `DryRun == true` or on error/cancellation.
  - Inner: Grasshopper `GH_UndoRecord` canvas snapshot stack. Added/modified/removed components and wires are reverted to their prior snapshot state on `DryRun == true`.
- **Cooperative Cancellation**: Responds to `CancellationToken` or request ID cancellation with error `-32003`.

### 4.4 Managed Named Pipe Test Double: `GrasshopperPipeSimulator.cs`
- Implements an asynchronous duplex named pipe server using .NET `NamedPipeServerStream`.
- Listens on demand (e.g. `\\.\pipe\hpgrasshopper-mcp-2026` or parameterized test pipe).
- Handles JSON-RPC 2.0 NDJSON requests:
  - `ping` / `grasshopper.ping` $\rightarrow$ `{ pong: true, revitVersion: "2026", executionEnabled: ... }`
  - `context` / `grasshopper.context` $\rightarrow$ complete `GrasshopperInfoRecord`
  - `cancel` / `grasshopper.cancel` $\rightarrow$ `{ cancelled: true, wasRunning: ... }`
  - Core tools dispatch $\rightarrow$ valid schema-compliant results.
- Pure managed C# (.NET 10), zero native Win32 `ctypes` Python code required for unit test execution.

### 4.5 Orchestration & Utility Harness: `GrasshopperE2EHarness.cs`
Provides standard fixtures and assertions:
- `CreateSampleParametricTowerGraph()`: Mock tower graph with sliders, division, columns, floors.
- `CreateSampleFacadePanelGraph()`: Mock diamond panelization graph.
- `CreateSampleRebarGraph()`: Mock beam detailing and stirrup graph.
- `BuildToolSurfaceList()`: Returns exact 25-tool inventory.
- `GenerateMockPngBytes()`: Returns standard 8-byte PNG header (`89 50 4E 47 0D 0A 1A 0A`).
- Canvas manipulation helpers: `AddComponentToCanvas()`, `ConnectPortsOnCanvas()`, `RemoveComponentFromCanvas()`.

---

## 5. Test Tiering Strategy & Inventory Matrix

### Summary of Coverage Thresholds
| Tier | Description | Features Exercised | Minimum Required | Planned Count |
|---|---|---|:---:|:---:|
| **Tier 1** | Feature Coverage (Category-Partition) | Features 1–12 | $\ge 60$ (5/feature) | **60** |
| **Tier 2** | Boundary Values & Corner Cases | Features 1–12 | $\ge 60$ (5/feature) | **60** |
| **Tier 3** | Cross-Feature Interactions (Pairwise) | Major Feature Pairs | $\ge 12$ | **12** |
| **Tier 4** | Real-World Application Scenarios | End-to-End Workloads | $\ge 6$ | **6** |
| **Subtotal** | **Core Acceptance Target** | | $\mathbf{\ge 138}$ | **138** |
| **Tier 5** | Adversarial Hardening (Challenger) | Security & Fuzzing | Bonus | **12** |
| **Total** | **Comprehensive Test Suite** | | | **150** |

---

### 5.1 Tier 1: Category-Partition Feature Coverage (60 Tests)

#### Class: `Tier1_ProtocolAndServerTests.cs` (25 Tests)
- **Feature 1: Named Pipe Server & Protocol**
  - `TC_T1_PIP_01`: Pipe name resolution matches `hpgrasshopper-mcp-2026`.
  - `TC_T1_PIP_02`: Method prefix conforms to `grasshopper.`.
  - `TC_T1_PIP_03`: JSON-RPC 2.0 NDJSON framing parses valid requests.
  - `TC_T1_PIP_04`: Malformed JSON returns standard error `-32700`.
  - `TC_T1_PIP_05`: Unknown method returns standard error `-32601`.
- **Feature 8: McpShared HostProfile & Security Guard**
  - `TC_T1_PRF_01`: HostProfile HostId identifies as `grasshopper`.
  - `TC_T1_PRF_02`: DefaultVersion is pinned to `2026`.
  - `TC_T1_PRF_03`: ScriptImports contains Rhino, Grasshopper, Revit namespaces.
  - `TC_T1_PRF_04`: ScriptGlobals includes `doc`, `canvas`, `rhinoDoc`, `revitDoc`, `ct`, `args`.
  - `TC_T1_PRF_05`: Denied namespaces deny `System.IO`, `System.Net`, `System.Windows.Forms`.
- **Feature 9: 8 Dynamic Registry Meta Tools**
  - `TC_T1_REG_01`: Registry surface contains all 8 meta tools.
  - `TC_T1_REG_02`: `search_tools` executes and returns results.
  - `TC_T1_REG_03`: `propose_tool` accepts valid candidate meeting ADR-0007.
  - `TC_T1_REG_04`: `test_tool` runs candidate in dry-run mode.
  - `TC_T1_REG_05`: `manage_tool` deprecates tool successfully.
- **Feature 10: 12 Embedded Seeds Library**
  - `TC_T1_SED_01`: Seed catalog contains exactly 12 embedded seeds.
  - `TC_T1_SED_02`: Category partitioning maps to Revit, Geometry, and Generic.
  - `TC_T1_SED_03`: Every seed has valid JSON Schema with `type: "object"`.
  - `TC_T1_SED_04`: Every seed description is $\ge 20$ characters.
  - `TC_T1_SED_05`: Every seed passes ADR-0007 quality rules.
- **Feature 12: Solution Build & Assembly Integrity**
  - `TC_T1_INT_01`: Assembly targets `.NET 10.0` (`net10.0`).
  - `TC_T1_INT_02`: Assembly references zero native Rhino/Revit assemblies.
  - `TC_T1_INT_03`: Tool catalog suppresses `inspect_type`.
  - `TC_T1_INT_04`: Total exposed tool surface count is exactly 25 tools.
  - `TC_T1_INT_05`: ContextResult serializes cleanly with zero foreign host properties.

#### Class: `Tier1_CoreToolsAndBridgeTests.cs` (25 Tests)
- **Feature 2: Modeless WPF UI & Status**
  - `TC_T1_WPF_01`: Initial state has opt-in execution disabled.
  - `TC_T1_WPF_02`: Enabling opt-in checkbox updates bridge state.
  - `TC_T1_WPF_03`: Context snapshot reflects active definition title.
  - `TC_T1_WPF_04`: Context snapshot reflects Rhino.Inside.Revit active flag.
  - `TC_T1_WPF_05`: Live canvas attach/detach reflects in context.
- **Feature 3: Read Grasshopper Canvas Tool**
  - `TC_T1_CAN_01`: Reads active document name and solution state.
  - `TC_T1_CAN_02`: Enumerates all components with valid GUIDs and nickNames.
  - `TC_T1_CAN_03`: Enumerates wire topology accurately.
  - `TC_T1_CAN_04`: `includeValues=false` omits heavy runtime values.
  - `TC_T1_CAN_05`: `selectedOnly=true` filters to selected components.
- **Feature 4: Run Grasshopper Player Tool**
  - `TC_T1_PLY_01`: Executes valid definition headlessly.
  - `TC_T1_PLY_02`: Binds numeric input parameters accurately.
  - `TC_T1_PLY_03`: Extracts requested output parameters.
  - `TC_T1_PLY_04`: Missing file returns descriptive failure without crashing.
  - `TC_T1_PLY_05`: Measures execution duration in milliseconds.
- **Feature 5: Batch Edit Canvas Tool**
  - `TC_T1_EDT_01`: Adds new component with coordinates.
  - `TC_T1_EDT_02`: Connects wire between component ports.
  - `TC_T1_EDT_03`: Disconnects existing wire.
  - `TC_T1_EDT_04`: Updates parameter values on existing component.
  - `TC_T1_EDT_05`: Removes component and associated wires.
- **Feature 7: Export Canvas Image Tool**
  - `TC_T1_IMG_01`: Exports image in `FullCanvas` mode.
  - `TC_T1_IMG_02`: Exports image in `Viewport` mode.
  - `TC_T1_IMG_03`: Exports image in `Selected` mode.
  - `TC_T1_IMG_04`: Returns valid base64-encoded PNG data.
  - `TC_T1_IMG_05`: PNG data begins with standard 8-byte PNG signature.

#### Class: `Tier1_ExecutionAndSafetyTests.cs` (10 Tests)
- **Feature 6: Execute Grasshopper Code Tool**
  - `TC_T1_EXC_01`: Read-only script evaluates successfully (`transaction: "none"`).
  - `TC_T1_EXC_02`: Mutation script commits elements when `dryRun=false`.
  - `TC_T1_EXC_03`: Mutation script rolls back elements when `dryRun=true`.
  - `TC_T1_EXC_04`: ScriptArgs values accessible via `args.Number()`, etc.
  - `TC_T1_EXC_05`: Global variables (`doc`, `canvas`, `revitDoc`) accessible.
- **Feature 11: Dual-Layer Safety & DryRun Rollback**
  - `TC_T1_SAF_01`: Revit `TransactionGroup` rolls back 100% on `dryRun=true`.
  - `TC_T1_SAF_02`: Canvas modifications reverted on `dryRun=true`.
  - `TC_T1_SAF_03`: Opt-in gate rejects execution with error `-32001`.
  - `TC_T1_SAF_04`: Exception during script triggers automatic rollback.
  - `TC_T1_SAF_05`: Cancellation token triggers clean transaction rollback.

---

### 5.2 Tier 2: Boundary Values & Corner Cases (60 Tests)

#### Class: `Tier2_BoundaryAndProtocolTests.cs` (20 Tests)
- Empty string code input handling.
- Maximum payload limits (64KB NDJSON chunk boundaries).
- Deeply nested JSON arguments parsing.
- Extreme coordinates on canvas ($X, Y = \pm 10^6$, $\text{NaN}$, $\infty$).
- Component NickName with special Unicode characters and emojis.
- Zero-component canvas serialization and deserialization.
- 1,000-component canvas stress serialization without memory leak.
- Wires with out-of-range port indices ($<0$ or $>10$).
- Cyclic self-connections on the same component.
- Player execution with empty inputs dictionary.
- Player execution with type mismatch (passing string to number slider).
- Export canvas image with custom resolution bounds ($1 \times 1$ to $4096 \times 4096$).
- Named pipe rapid connect-disconnect cycles.
- Named pipe client abrupt termination mid-message.
- Named pipe concurrent connection rejection.
- Timeout values: 0s, negative values, and clamping to 600s ceiling.
- Case-insensitivity in tool names and JSON-RPC method routing.
- Whitespace padding in JSON-RPC NDJSON stream.
- Request ID preservation across various primitive types (int, string, Guid).
- High-frequency ping flooding stability.

#### Class: `Tier2_ScriptGuardAndSecurityTests.cs` (20 Tests)
- Script length boundary: exactly 299 lines (Accepted).
- Script length boundary: exactly 300 lines (Accepted).
- Script length boundary: exactly 301 lines (Rejected with line count message).
- Forbidden namespace: `System.IO.File` (Rejected).
- Forbidden namespace: `global::System.IO.File` bypass attempt (Rejected).
- Forbidden namespace: `System.Net.Sockets` (Rejected).
- Forbidden namespace: `System.Diagnostics.Process` (Rejected).
- Forbidden namespace: `System.Reflection.Assembly` (Rejected).
- Forbidden namespace: `System.Windows.Forms.MessageBox` (Rejected).
- Forbidden directive: `#r "path/to.dll"` (Rejected).
- Forbidden directive: `#load "script.csx"` (Rejected).
- Empty catch block: `catch {}` (Rejected).
- Empty catch block: `catch (Exception) {}` (Rejected).
- Catch block with single-line reason comment `catch { /* expected fallback */ }` (Accepted).
- Commented-out code statement: `// var x = 10;` (Rejected).
- Legitimate prose comment: `// calculate span length` (Accepted).
- Rapid opt-in state flipping during execution dispatches.
- Execution attempt with expired cancellation token.
- Multi-threaded cancellation token race condition.
- Script containing infinite loop terminating via linked cancellation token.
- Security error response format adheres strictly to JSON-RPC 2.0 error specification.

#### Class: `Tier2_SeedStressAndRollbackTests.cs` (20 Tests)
- `create_levels_grids`: 0 levels, 100 levels, duplicate level names.
- `generate_columns_beams`: Inverted elevations (baseLevel > topLevel).
- `create_floors_slabs`: Self-intersecting polygon boundary coordinates.
- `generate_rebar_curves`: Zero span length, cover exceeding beam dimensions.
- `generate_stirrup_distribution`: Clear span smaller than support dense zone.
- `facade_panelization`: $U \times V$ counts of $1 \times 1$ and $100 \times 100$.
- `curtain_grid_layout`: Spacing exceeding total width/height.
- `create_wall_openings`: Opening coordinates outside wall bounding box.
- `directshape_from_geometry`: Null or corrupt Brep geometry payload.
- `query_revit_geometry`: Invalid or non-existent built-in category.
- `evaluate_solar_radiation`: Out-of-bounds latitude/longitude ($\pm 90^\circ, \pm 180^\circ$).
- `export_model_to_rhino`: Empty element ID array, non-existent element IDs.
- Dual rollback verification for all 12 seeds under `dryRun=true`.
- Zero ghost elements created in Revit model across all seed dry-runs.
- Canvas state invariant: zero residual nodes after failed seed execution.
- Transaction failure handling during multi-element batch seed generation.
- Memory pressure test: 50 consecutive seed dry-run invocations.
- Execution duration tracking accuracy under seed failures.
- Diagnostic logs collection during seed syntax warnings.
- Output serialization truncation flag handling for large geometry collections.

---

### 5.3 Tier 3: Pairwise Cross-Feature Interactions (12 Tests)

Class: `Tier3_CrossFeatureInteractionTests.cs`
- `TC_T3_XF_01`: **Player Solve + Canvas Reader**: Run player definition headlessly, then inspect canvas document state.
- `TC_T3_XF_02`: **Batch Edit + Canvas Export**: Add components, connect wires, and export visual canvas diagram.
- `TC_T3_XF_03`: **Registry Proposal + Seed Execution**: Propose custom math tool, test dry-run, publish, and feed output into seed tool.
- `TC_T3_XF_04`: **Roslyn Code + Dual Rollback + Context**: Execute element creation code with `dryRun=true`, verify rollback, then query context for zero modified elements.
- `TC_T3_XF_05`: **Canvas Add + Wire Connect + Code Ref**: Place components via batch edit, then run Roslyn code referencing their GUIDs.
- `TC_T3_XF_06`: **Opt-In Switch + Player Execution**: Verify player execution respects opt-in gate and rejects when disabled.
- `TC_T3_XF_07`: **Batch Edit Failure + Undo Verification**: Mid-batch failure during node addition triggers complete LIFO rollback of earlier nodes in the same batch.
- `TC_T3_XF_08`: **Concurrent Pipe Requests**: Interleaved `context` and `execute` requests over named pipe without deadlock.
- `TC_T3_XF_09`: **Rebar Detailing + TransactionGroup Commit**: Generate rebar curves and stirrup distribution seeds, commit, and verify element ID continuity.
- `TC_T3_XF_10`: **Facade Panel + DirectShape Bake + Rollback**: Generate facade panels and directshape bake in dry-run, confirm 100% rollback of both geometries.
- `TC_T3_XF_11`: **Player Parameter Sweep + Canvas Snapshot**: Loop player execution over 5 parameter sets, capturing output states.
- `TC_T3_XF_12`: **Multi-Turn AI Assistant Flow**: Context query $\rightarrow$ tool search $\rightarrow$ tool propose $\rightarrow$ test dry-run $\rightarrow$ publish $\rightarrow$ execute.

---

### 5.4 Tier 4: Real-World Application Scenarios (6 Tests)

Class: `Tier4_RealWorldScenariosTests.cs`
- `TC_T4_RW_01`: **Parametric Tower Structural Grid & Framing**:
  Full workflow creating 10 levels, radial axis grid, perimeter columns, floor slabs, and core walls via Grasshopper definition solve.
- `TC_T4_RW_02`: **Facade Adaptive Panelization & Cladding**:
  Subdivides complex double-curved surface into diamond adaptive panels, maps panel types by orientation, and prepares DirectShape elements.
- `TC_T4_RW_03`: **Automated Beam Rebar Detailing & Stirrups (TCVN 5574:2018)**:
  Extracts beam geometry, computes longitudinal top/bottom rebar cutoffs ($L/4$, $L/3$), computes support dense ($a100$) vs mid-span sparse ($a200$) stirrup spacing, and generates rebar elements under transaction control.
- `TC_T4_RW_04`: **DryRun Dual Safety & Clean Rollback Guarantee**:
  Executes high-impact structural remodeling (batch canvas edits + 50 Revit structural elements) with `dryRun=true`; proves 0 ghost elements left in Revit and 0 modified nodes on canvas.
- `TC_T4_RW_05`: **Autonomous AI Agent Tool Lifecycle**:
  AI agent queries context, discovers missing solar radiation calculator, proposes tool with ADR-0007 compliant C# code, verifies via dry-run, publishes to SQLite registry, and executes in production.
- `TC_T4_RW_06`: **Solar Radiation Analysis & DirectShape Visual Heatmap**:
  Evaluates solar incident radiation vectors on roof geometry, generates color-mapped mesh panels, and bakes DirectShape visual elements.

---

### 5.5 Tier 5: Adversarial Hardening (12 Tests)

Class: `Tier5_AdversarialHardeningTests.cs`
- Fuzzing string inputs with extreme lengths and binary data.
- Deny-list evasion using unicode obfuscation and string concatenation.
- Deny-list evasion using nested type aliases (`using F = System.IO.File;`).
- Rapid concurrent cancellations and dispatch starvation.
- Memory leak detection over 500 iterative script executions.
- AST mutation attacks attempting hidden transaction boundary manipulation.
- Direct shape Brep geometry payload fuzzing with corrupt buffers.
- SQLite registry SQL injection attack probing in `search_tools` and `manage_tool`.
- Resource exhaustion defense: canvas with $>10,000$ simulated nodes.
- Interrupted pipe transport simulation with partial JSON packets.
- Clock skew and long-running execution timeout enforcement.
- Opt-in gate tamper resistance via reflection probing.

---

## 6. PowerShell Test Runner Design

### 6.1 Runner Script: `HPGrasshopper/run-e2e-grasshopper-tests.ps1`

```powershell
<#
.SYNOPSIS
    Automated Acceptance Test Runner for HPGrasshopper MCP Subsystem.
.DESCRIPTION
    Executes HPGrasshopper.E2E.Tests (.NET 10 xUnit v3 MTP Runner) across
    Tiers 1-4 and adversarial Tier 5 with zero popups and 100% headless safety.
.PARAMETER Tier
    Acceptance Tier to execute: All, Tier1, Tier2, Tier3, Tier4, Tier5.
.PARAMETER Configuration
    Build configuration: Debug or Release. Defaults to Debug.
.PARAMETER SkipBuild
    Skips dotnet build step.
#>
[CmdletBinding()]
param(
    [ValidateSet("All", "Tier1", "Tier2", "Tier3", "Tier4", "Tier5")]
    [string]$Tier = "All",
    [string]$Configuration = "Debug",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptDir "HPGrasshopper.E2E.Tests\HPGrasshopper.E2E.Tests.csproj"
$binDir = Join-Path $scriptDir "HPGrasshopper.E2E.Tests\bin\$Configuration\net10.0"
$exePath = Join-Path $binDir "HPGrasshopper.E2E.Tests.exe"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  HPGrasshopper MCP Acceptance Test Suite (Revit 2026 / R8) " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Build if required
if (-not $SkipBuild -or -not (Test-Path $exePath)) {
    Write-Host "Building HPGrasshopper.E2E.Tests ($Configuration)..." -ForegroundColor Yellow
    dotnet build $projectPath -c $Configuration -m:1 -nodeReuse:false
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

if (-not (Test-Path $exePath)) {
    Write-Host "Test executable not found: $exePath" -ForegroundColor Red
    exit 1
}

# 2. Configure MTP filter arguments
$filterArgs = switch ($Tier) {
    "All"   { @() }
    "Tier1" { @("--filter-class", "*Tier1*") }
    "Tier2" { @("--filter-class", "*Tier2*") }
    "Tier3" { @("--filter-class", "*Tier3*") }
    "Tier4" { @("--filter-class", "*Tier4*") }
    "Tier5" { @("--filter-class", "*Tier5*") }
}

Write-Host "Executing Acceptance Tier: $Tier" -ForegroundColor Yellow
Push-Location $scriptDir
try {
    if ($filterArgs.Length -gt 0) {
        & $exePath @filterArgs
    } else {
        & $exePath
    }
    $testExit = $LASTEXITCODE
}
finally {
    Pop-Location
}

# 3. Report Results
Write-Host "------------------------------------------------------------" -ForegroundColor $(if ($testExit -eq 0) { "Green" } else { "Red" })
if ($testExit -eq 0) {
    Write-Host "  Result: SUCCESS (100% Passed)                             " -ForegroundColor Green
} else {
    Write-Host "  Result: FAILED (Exit Code: $testExit)                     " -ForegroundColor Red
}
Write-Host "------------------------------------------------------------" -ForegroundColor $(if ($testExit -eq 0) { "Green" } else { "Red" })

exit $testExit
```

---

## 7. Clean Code Governance Compliance Matrix

| Target File | Purpose | Max Lines Target | Method Max Lines | Rules Verified |
|---|---|:---:|:---:|---|
| `HPGrasshopper.E2E.Tests.csproj` | Project file | ~30 | N/A | Host-isolated, net10.0 |
| `Contracts/GrasshopperE2EContracts.cs` | Wire constants & profiles | ~120 | $\le 20$ | C2, M3, Q-B1, Q-B2 |
| `Contracts/GrasshopperE2EModels.cs` | High-fidelity domain records | ~90 | $\le 20$ | C2, M3, Q-B1, Q-B2 |
| `Harness/GrasshopperE2EFakeBridge.cs` | In-memory dual rollback simulator | ~220 | $\le 45$ | C2, M3, Q-B1, Q-B2 |
| `Harness/GrasshopperPipeSimulator.cs` | Managed duplex named pipe server | ~180 | $\le 40$ | C2, M3, Q-B1, Q-B2 |
| `Harness/GrasshopperE2EHarness.cs` | Test helpers and fixtures | ~150 | $\le 35$ | C2, M3, Q-B1, Q-B2 |
| `Tier1_ProtocolAndServerTests.cs` | Features 1, 8, 9, 10, 12 tests | ~180 | $\le 30$ | C2, M3, Q-B1, Q-B2 |
| `Tier1_CoreToolsAndBridgeTests.cs` | Features 2, 3, 4, 5, 7 tests | ~180 | $\le 30$ | C2, M3, Q-B1, Q-B2 |
| `Tier1_ExecutionAndSafetyTests.cs` | Features 6, 11 tests | ~160 | $\le 35$ | C2, M3, Q-B1, Q-B2 |
| `Tier2_BoundaryAndProtocolTests.cs` | Protocol boundary & limits | ~170 | $\le 30$ | C2, M3, Q-B1, Q-B2 |
| `Tier2_ScriptGuardAndSecurityTests.cs` | AST & deny-list boundary | ~180 | $\le 35$ | C2, M3, Q-B1, Q-B2 |
| `Tier2_SeedStressAndRollbackTests.cs` | Seed stress & rollback bounds | ~170 | $\le 35$ | C2, M3, Q-B1, Q-B2 |
| `Tier3_CrossFeatureInteractionTests.cs` | Pairwise feature interaction | ~190 | $\le 40$ | C2, M3, Q-B1, Q-B2 |
| `Tier4_RealWorldScenariosTests.cs` | Real-world engineering workloads | ~210 | $\le 45$ | C2, M3, Q-B1, Q-B2 |
| `Tier5_AdversarialHardeningTests.cs` | Fuzzing & adversarial security | ~180 | $\le 40$ | C2, M3, Q-B1, Q-B2 |
| `run-e2e-grasshopper-tests.ps1` | PowerShell test runner | ~65 | $\le 35$ | C2, M3, Q-B1, Q-B2 |

Every planned file is **strictly below 250 lines** (well within Rule C2 limit of 300 lines). Every method is **strictly below 50 lines** (Rule M3). Zero dead code or unannotated catches are permitted.

---

## 8. Implementation Roadmap for Implementation Agents

1. **Step 1: Project Scaffold & Wiring**
   - Create `HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj`.
   - Wire `<Project Path="HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj" />` into `HPGrasshopper/HPGrasshopper.slnx`.
   - Verify `dotnet build HPGrasshopper.E2E.Tests` succeeds.
2. **Step 2: Contracts & Domain Models**
   - Implement `Contracts/GrasshopperE2EContracts.cs`.
   - Implement `Contracts/GrasshopperE2EModels.cs`.
3. **Step 3: Test Doubles & Harness**
   - Implement `Harness/GrasshopperE2EFakeBridge.cs` (dual rollback, script guard, opt-in gate).
   - Implement `Harness/GrasshopperPipeSimulator.cs` (managed named pipe listener).
   - Implement `Harness/GrasshopperE2EHarness.cs` (fixtures, tool surface list, PNG stub).
4. **Step 4: Tier 1 Feature Coverage (60 Tests)**
   - Implement `Tier1_ProtocolAndServerTests.cs` (25 tests).
   - Implement `Tier1_CoreToolsAndBridgeTests.cs` (25 tests).
   - Implement `Tier1_ExecutionAndSafetyTests.cs` (10 tests).
   - Verify 60/60 tests pass.
5. **Step 5: Tier 2 Boundary & Corner Cases (60 Tests)**
   - Implement `Tier2_BoundaryAndProtocolTests.cs` (20 tests).
   - Implement `Tier2_ScriptGuardAndSecurityTests.cs` (20 tests).
   - Implement `Tier2_SeedStressAndRollbackTests.cs` (20 tests).
   - Verify 120/120 tests pass.
6. **Step 6: Tier 3 Cross-Feature Interactions (12 Tests)**
   - Implement `Tier3_CrossFeatureInteractionTests.cs` (12 tests).
   - Verify 132/132 tests pass.
7. **Step 7: Tier 4 Real-World Application Scenarios (6 Tests)**
   - Implement `Tier4_RealWorldScenariosTests.cs` (6 tests).
   - Verify 138/138 tests pass.
8. **Step 8: Tier 5 Adversarial Hardening (12 Tests)**
   - Implement `Tier5_AdversarialHardeningTests.cs` (12 tests).
   - Verify 150/150 tests pass.
9. **Step 9: PowerShell Runner & Automated CI**
   - Implement `HPGrasshopper/run-e2e-grasshopper-tests.ps1`.
   - Execute test runs across `-Tier Tier1`, `-Tier Tier2`, `-Tier Tier3`, `-Tier Tier4`, and `-Tier All`.
   - Verify 100% green pass with 0 exit code.
