# Survey Report: McpShared Architecture & Changes for HPGrasshopper Host

**Target Host:** McNeel Grasshopper in Revit 2026 via Rhino.Inside.Revit (Rhino 8)  
**Surveyor:** `explorer_survey_1` (McpShared Architect Explorer)  
**Date:** 2026-10-10  
**Scope:** `McpShared/` (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`, and related test projects)  

---

## 1. Executive Summary & Architectural Baseline

`McpShared` is the host-neutral Model Context Protocol (MCP) foundation for all CAD/BIM bridges in the repository (Revit, AutoCAD, Civil 3D, Navisworks, ETABS, SAP2000, Robot, Power BI, Excel, Tekla, and Dynamo).

The integration of **HPGrasshopper** follows the clean host-seam architecture proven by existing hosts—in particular its closest sibling, **HPDynamo** (which also operates as an in-process dual-host engine inside Revit 2026).

All changes to `McpShared` are **strictly additive and non-breaking**:
1. `HPRebar.Mcp.Contracts` (netstandard2.0; net48): Exposes wire constants, method prefixes, script contracts, and context models.
2. `HPRebar.McpBridge.Core` (net8.0; net48): Exposes static security guard rules (`GuardProfile.Grasshopper`) and AST transaction analyzer rules (`AnalyzerProfile.Grasshopper`).
3. `HPRebar.Mcp.Server.Core` (net10.0): Exposes static server profile (`HostProfile.Grasshopper`) with core tool definitions, environment prefixes, registry categories, and timeout clamps.
4. `McpShared Test Suites`: Verifies 100% host neutrality, zero assembly leaks, and full roundtrip fidelity with unit and challenger test suites.

Current Baseline Test Status:
- `HPRebar.Mcp.Server.Core.Tests`: **915 / 915 PASS** (0 failures, 0 skipped).
- `HPRebar.McpBridge.Core.Net48Tests`: **115 / 115 PASS** (0 failures, 0 skipped).

---

## 2. User Requirements Traceability Matrix

| Requirement | Source (ORIGINAL_REQUEST.md) | McpShared Touchpoint | Status & Action |
|---|---|---|---|
| **Named Pipe `hpgrasshopper-mcp-2026`** | R1, R3 | `PipeNaming.GrasshopperHost`, `PipeNaming.For("grasshopper", 2026)` | Verified; explicit switch mapping |
| **Method prefix `grasshopper.`** | R3 | `JsonRpcMethods.GrasshopperPrefix = "grasshopper."` | Verified; full wire compatibility |
| **5 Core MCP Tools + Registry** | R2, R4 | `HostProfile.Grasshopper.CoreToolNames` | Registered 7 core tool names (5 tools + context + cancel) |
| **Guard & Analyzer Profiles** | R3 | `GuardProfile.Grasshopper`, `AnalyzerProfile.Grasshopper` | Configured to allow RhinoCommon, Grasshopper, RhinoInside.Revit, Revit API; deny modal UI & quit |
| **Script Imports & Globals** | R2.4, R3 | `HostScriptContracts.GrasshopperImports`, `GrasshopperGlobals` | Added globals: `doc`, `canvas`, `rhinoDoc`, `revitDoc`, `ct`, `log`, `progress`, `args` |
| **Context Shaping** | R2, R3 | `ContextResult.Grasshopper`, `GrasshopperInfo`, `ContextService.cs` | Added `GrasshopperInfo` record; `ContextService` drops Revit fields |
| **Zero Regressions & Host Neutrality** | R5, AGENTS.md | `HostNeutralityTests.cs`, `GrasshopperProfileTests.cs` | Assert zero assembly leakage of Rhino/Grasshopper into McpShared |

---

## 3. Detailed Component Investigation

### 3.1. `PipeNaming.cs` (`HPRebar.Mcp.Contracts`)

#### Current Behavior:
- Contains host constants for 11 hosts: `RevitHost`, `AutocadHost`, `NavisHost`, `EtabsHost`, `Civil3dHost`, `Sap2000Host`, `PowerBiHost`, `ExcelHost`, `RobotHost`, `TeklaHost`, `DynamoHost`.
- `PipeNaming.For(string host, int version)` normalizes host via `host.Trim().ToLowerInvariant()` and evaluates a switch expression.
- Default fallback `_ => "hp" + key + "-mcp-" + version` already yields `"hpgrasshopper-mcp-2026"` for `"grasshopper"`, but standard practice across the repository is to define an explicit public constant and explicit switch branch.

#### Required Changes:
```csharp
/// <summary>
///     McNeel Grasshopper in Revit via Rhino.Inside.Revit; pipe <c>hpgrasshopper-mcp-{version}</c> (e.g. 2026).
/// </summary>
public const string GrasshopperHost = "grasshopper";
```
In switch expression:
```csharp
GrasshopperHost => "hpgrasshopper-mcp-" + version,
```

---

### 3.2. `JsonRpcMethods.cs` (`HPRebar.Mcp.Contracts`)

#### Current Behavior:
- Host prefixes are structured as `"{hostId}."` (e.g. `RevitPrefix = "revit."`, `DynamoPrefix = "dynamo."`).
- Suffixes are host-neutral: `"execute"`, `"context"`, `"inspect"`, `"cancel"`, `"analyze"`, `"progress"`, `"log"`, `"status"`.
- Bridges dispatch on suffix (`JsonRpcMethods.Suffix(method)`), so `grasshopper.execute` routes to the execute handler automatically.

#### Required Changes:
```csharp
public const string GrasshopperPrefix = "grasshopper.";
```

---

### 3.3. `HostScriptContracts.cs` (`HPRebar.Mcp.Contracts`)

#### Current Behavior:
- Central registry of default script `using` namespaces, global variable identifiers, and heavy run timeout limits.
- Referenced by:
  - Bridges: Roslyn compiler setup (`ScriptOptions.AddImports(...)`).
  - Servers: Tool prompt generation, tool validation.
  - Tests: Compiling embedded seeds against reference assemblies.

#### Required Changes:
1. **`GrasshopperImports`**:
```csharp
/// <summary>
///     Default `using`s of a Grasshopper script. Covers RhinoCommon, Grasshopper core,
///     Grasshopper parameters/data, Rhino.Inside.Revit, Revit API, and bridge scripting.
/// </summary>
public static readonly string[] GrasshopperImports =
{
    "System", "System.Linq", "System.Collections.Generic",
    "Rhino", "Rhino.Geometry",
    "Grasshopper", "Grasshopper.Kernel", "Grasshopper.Kernel.Data", "Grasshopper.Kernel.Types",
    "RhinoInside.Revit",
    "Autodesk.Revit.DB", "Autodesk.Revit.UI",
    "HPRebar.McpBridge.Core.Scripting",
};
```
2. **`GrasshopperGlobals`**:
Per requirement R2.4, global variables are:
- `doc`: Active `GH_Document`
- `canvas`: `GH_Canvas`
- `rhinoDoc`: Active `RhinoDoc`
- `revitDoc`: Active Revit `Document`
- Plus standard bridge plumbing globals: `ct` (CancellationToken), `log` (Action<string>), `progress` (Action<int, int?, string?>), `args` (ScriptArgs).
```csharp
public static readonly string[] GrasshopperGlobals = { "doc", "canvas", "rhinoDoc", "revitDoc", "ct", "log", "progress", "args" };
```
3. **`GrasshopperHeavyMaxTimeoutSeconds`**:
```csharp
/// <summary>
///     Longest Grasshopper run allowed when executing heavy operations (e.g. full canvas re-solve, GH Player batch execution).
/// </summary>
public const int GrasshopperHeavyMaxTimeoutSeconds = 600;
```

---

### 3.4. `GuardProfile.cs` & `ScriptGuard.cs` (`HPRebar.McpBridge.Core`)

#### Current Behavior:
- `ScriptGuard` parses script syntax trees into Roslyn AST and applies a base deny-list (Process, Thread, File, Directory, Reflection, Marshal, `#r`, `#load`, etc.).
- `GuardProfile` defines host-specific dangerous APIs:
  - Modal UI that freezes the UI/Main thread and blocks Named Pipe communication.
  - Process/Host exit functions (`Quit`, `Exit`).
  - Bridge internals namespaces (`HPGrasshopper.McpBridge`, `HPRebar.McpBridge.Core.Host`).
  - `System.Windows.Forms` (must be denied to avoid popup modal blocking).

#### Required Changes:
In `GuardProfile.cs`:
```csharp
/// <summary>
///     Grasshopper in Revit via Rhino.Inside.Revit (.NET 8 / Revit 2026).
///     Denied: Modal dialogs (MessageBox, TaskDialog) that freeze the main thread and pipe communication,
///     application shutdown (RhinoApp.Exit, Quit, CloseApplication), direct process exits, and bridge internals.
/// </summary>
public static readonly GuardProfile Grasshopper = new GuardProfile(
    "Grasshopper",
    deniedIdentifiers: new[] { "MessageBox", "TaskDialog" },
    deniedMembers: new[]
    {
        "Show", "ShowModal",
        "Exit", "Quit", "CloseApplication",
    },
    deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["app"] = new[] { "Exit", "Quit" },
        ["rhinoDoc"] = new[] { "Dispose" },
    },
    deniedNamespaces: new[]
    {
        "System.Windows.Forms",
        "HPGrasshopper.McpBridge",
        "HPRebar.McpBridge.Core.Host",
    });
```
*Verification:* RhinoCommon, Grasshopper, RhinoInside.Revit, and Autodesk.Revit namespaces are NOT denied and can be freely used by scripts.

---

### 3.5. `AnalyzerProfile.cs` (`HPRebar.McpBridge.Core`)

#### Current Behavior:
- `ScriptAnalyzer` walks syntax nodes to detect whether scripts manage their own transactions (requiring `transaction: "manual"`).
- Hosts managing Revit elements (`Revit`, `Dynamo`) specify `Transaction`, `TransactionGroup`, and `SubTransaction` in `transactionTypeNames`.
- Grasshopper scripts running inside Revit via Rhino.Inside.Revit follow the exact same Revit transaction lifecycle when mutating Revit elements.

#### Required Changes:
```csharp
/// <summary>
///     Grasshopper in Revit via Rhino.Inside.Revit: scripts modifying Revit elements construct
///     <c>new Transaction(doc, …)</c>, <c>new TransactionGroup(doc, …)</c>, or <c>new SubTransaction(doc, …)</c>.
/// </summary>
public static readonly AnalyzerProfile Grasshopper = new AnalyzerProfile(
    transactionTypeNames: new[] { "Transaction", "TransactionGroup", "SubTransaction" },
    transactionMethodNames: Array.Empty<string>());
```

---

### 3.6. `HostProfile.cs` & `IHostProfile.cs` (`HPRebar.Mcp.Server.Core`)

#### Current Behavior:
- `HostProfile` is the dependency injection configuration model for each host.
- Drives server startup, tool registry validation, prompt templates, and named-pipe connection.
- `CoreToolNames` prevents user-defined registry tools from shadowing built-in tools.

#### Required Changes:
In `HostProfile.cs`:
```csharp
public static readonly HostProfile Grasshopper = new HostProfile
{
    HostId = PipeNaming.GrasshopperHost,
    DisplayName = "Grasshopper",
    ServerName = "HPGrasshopper MCP",
    ProductFolder = "HPGrasshopper",
    EnvPrefix = "HPGRASSHOPPER_MCP_",
    DefaultVersion = 2026,
    ValidVersions = new[] { 2026 },
    MethodPrefix = JsonRpcMethods.GrasshopperPrefix,
    ExecuteToolName = "execute_grasshopper_code",
    ContextToolName = "get_grasshopper_context",
    ResourceScheme = "grasshopper",
    Categories = new[] { "Canvas", "Component", "Parameter", "Geometry", "Revit", "Python", "Player", "Generic" },
    CoreToolNames = new[]
    {
        "execute_grasshopper_code",
        "get_grasshopper_context",
        "read_grasshopper_canvas",
        "run_grasshopper_player",
        "batch_edit_grasshopper_canvas",
        "export_grasshopper_canvas_image",
        "cancel_execution"
    },
    ScriptImports = HostScriptContracts.GrasshopperImports,
    ScriptContractSummary =
        "Globals: doc (GH_Document), canvas (GH_Canvas), rhinoDoc (RhinoDoc), revitDoc (Document), ct, log(string), progress(cur,total,msg), args. " +
        "Dual-host execution: Grasshopper canvas in Rhino.Inside.Revit 2026. " +
        "transaction: auto when modifying Revit elements or Grasshopper canvas; none when reading; manual if managing its own Transaction.",
    HostAssembly = typeof(HostProfile).Assembly,
    CliExecutable = "HPGrasshopper.Mcp.Server.exe",
    MaxTimeoutSeconds = HostScriptContracts.GrasshopperHeavyMaxTimeoutSeconds,
    BridgeNotConnectedHint = "Open Revit 2026 with Rhino.Inside.Revit and Grasshopper, ensure HPGrasshopper MCP Bridge is running (pipe hpgrasshopper-mcp-2026).",
    TimeoutSemanticsHint = "The script or definition solver may still be running in Grasshopper/Revit; check Grasshopper status before retrying.",
};
```

---

### 3.7. `ContextResult.cs` & `ContextService.cs` (`HPRebar.Mcp.Contracts` & `HPRebar.Mcp.Server.Core`)

#### Current Behavior:
- `ContextResult` is the DTO returned by `*.context`.
- Contains common fields: `Host`, `HostVersion`, `DocTitle`, `DocPath`, `Units`, `ActiveView`, `Selection`, `ExecutionEnabled`.
- Contains host-specific nested blocks: `Autocad`, `Navis`, `Etabs`, `Civil3d`, `Sap2000`, `PowerBi`, `Excel`, `Robot`, `Tekla`, `Dynamo`.
- `ContextService.Shape(...)`: Automatically strips legacy fields `revitVersion` and `isFamily` for all non-Revit hosts.
- Serialization options (`DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`) ensure null host blocks are omitted from the wire JSON payload.

#### Required Changes:
In `ContextMessages.cs`:
1. Add property to `ContextResult`:
```csharp
/// <summary>Grasshopper-only facts; null for the other hosts (and omitted from the JSON).</summary>
public GrasshopperInfo? Grasshopper { get; set; }
```
2. Define record `GrasshopperInfo`:
```csharp
/// <summary>
///     What a Grasshopper script needs to know: whether Grasshopper editor/canvas is live or headless,
///     Grasshopper and Rhino versions, active definition name and path, component/wire/selection counts,
///     modified status, and whether Rhino.Inside.Revit is active.
/// </summary>
public sealed record GrasshopperInfo(
    bool IsEditorLoaded,
    string? GrasshopperVersion,
    string? RhinoVersion,
    string? ActiveDefinitionName,
    string? ActiveDefinitionPath,
    int ComponentCount,
    int WireCount,
    int SelectedComponentCount,
    bool HasUnsavedChanges,
    bool IsRhinoInsideRevitActive);
```
In `ContextService.cs`:
- No code change required! `ContextService.Shape(...)` already functions for any `HostId != PipeNaming.RevitHost`.

---

### 3.8. Harness & Automation (`McpShared/tools/live-verify-quality.py`)

#### Current Behavior:
- `live-verify-quality.py` verifies ADR-0007 script quality adherence and tool publish/execute lifecycle across live bridges.
- Maps each host in `HOSTS` dictionary: `folder, env_prefix, version_key, default_version, context_tool, snapshot_folder`.

#### Required Changes:
In `McpShared/tools/live-verify-quality.py`:
```python
"grasshopper": ("HPGrasshopper", "HPGRASSHOPPER_MCP_", "HostVersion", 2026, "get_grasshopper_context", None),
```

---

## 4. Test Architecture & Verification Plan

### 4.1. Existing Baseline Suites (To Maintain at 100% PASS)
1. `HPRebar.Mcp.Server.Core.Tests`: 915 tests currently passing.
2. `HPRebar.McpBridge.Core.Net48Tests`: 115 tests currently passing.

### 4.2. Tests to Add to `HPRebar.Mcp.Server.Core.Tests`

#### 1. `GrasshopperTestProfile.cs`:
Test fixture creating mock `HostProfile.Grasshopper`, sample tool candidate records, and fake pipe names for integration testing.

#### 2. `GrasshopperProfileTests.cs` (11 Unit Tests):
- `Grasshopper_constants_produce_the_pipe_prefix_imports_and_globals`: Validates pipe name `hpgrasshopper-mcp-2026`, prefix `grasshopper.`, imports, globals, max timeout 600s.
- `Grasshopper_guard_profile_denies_dialogs_quit_and_base_list`: Validates denial of `MessageBox`, `TaskDialog`, `Exit`, `Quit`, `CloseApplication`, `System.Windows.Forms`, `HPGrasshopper.McpBridge`, `#r`, `#load`.
- `Global_alias_does_not_bypass_the_grasshopper_bridge_namespace_denial`: Validates `global::` bypass blocking.
- `Grasshopper_guard_profile_lets_rhino_and_grasshopper_manipulation_through`: Validates clean passing of Point, Curve, GH_Document, and canvas calls.
- `Grasshopper_analyzer_profile_detects_transactions`: Validates detection of `new Transaction(...)`, `TransactionGroup`, `SubTransaction`.
- `Hints_and_timeout_ceiling_survive_WithHostAssembly_and_default_to_null`: Validates profile immutability and hints preservation.
- `Validator_ceiling_follows_the_grasshopper_profile`: Validates that `ToolValidator` allows up to 600s and blocks 601s.
- `ConfigureOptions_seeds_HostVersion_from_the_profile_and_configuration_still_wins`: Validates DI options binding.
- `Wire_additions_are_invisible_when_unused`: Validates wire cleanliness (Revit context output does not contain `"grasshopper"`).
- `Grasshopper_info_round_trips_in_camel_case_and_is_omitted_when_null`: Validates camelCase serialization and omission of null fields.
- `Context_shape_for_grasshopper_drops_revit_fields_and_keeps_grasshopper_block`: Validates that `ContextService` drops `revitVersion` and `isFamily`.

#### 3. `GrasshopperMilestone1ChallengerTests.cs` (7 Adversarial Tests):
- Concurrency / simultaneous pipe creation.
- Case-insensitivity in host identification (`GRASSHOPPER`, `Grasshopper`, `grasshopper`).
- Boundary condition on timeout clamping (rejecting < 5s, honoring custom clamping).
- Empty or whitespace host validation.
- ToolValidator handling of Grasshopper-specific core tool shadows (e.g. attempting to publish a tool named `run_grasshopper_player` is rejected).

#### 4. `HostNeutralityTests.cs` Updates:
- Add `"RhinoCommon"`, `"Grasshopper"`, `"RhinoInside.Revit"` to `HostApiAssemblies` array to guarantee McpShared never references host assemblies.
- Add assertions for `PipeNaming.For("grasshopper", 2026)` and `PipeNaming.For(PipeNaming.GrasshopperHost, 2026)`.

---

## 5. Potential Risks & Clean Code Governance

1. **Clean Code Rules (C2, M3, Q-B1, Q-B2):**
   - File length <= 300 lines (Rule C2).
   - Method length <= 50 lines (Rule M3).
   - Zero commented-out code (Q-B1).
   - Zero empty catch blocks without comment (Q-B2).
   - All additions to `PipeNaming.cs`, `JsonRpcMethods.cs`, `HostScriptContracts.cs`, `GuardProfile.cs`, `AnalyzerProfile.cs`, and `HostProfile.cs` keep each file well under the 300-line ceiling (e.g., `PipeNaming.cs` is currently 95 lines, `JsonRpcMethods.cs` is 73 lines, `AnalyzerProfile.cs` is 95 lines, `HostProfile.cs` is 137 lines).

2. **Host Neutrality Rule:**
   - `McpShared` must never reference any Rhino, Grasshopper, or Rhino.Inside.Revit assemblies.
   - All types remain pure strings, records, or DTO primitives.

3. **Wire Compatibility:**
   - All wire changes are additive (`GrasshopperInfo` in `ContextResult`, new constants).
   - Existing bridges (Revit, AutoCAD, Navisworks, etc.) and tests remain completely unaffected.
