# Investigation & Design Report: Synchronization of HPDynamo.E2E.Tests Tool Surface Contracts

- **Agent**: explorer_dynamo_m4_it2_3 (`teamwork_preview_explorer` / Investigation & Contract Synchronization)
- **Working directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m4_it2_3`
- **Parent Conversation ID**: `f448d195-c008-4b83-983a-30d0f3ab6338`
- **Date**: 2026-10-09

---

## 1. Executive Summary

During Milestone 4 verification, `challenger_dynamo_m4_2` identified a contract lag in `HPDynamo.E2E.Tests`:
- `HPDynamo/HPDynamo.E2E.Tests/Contracts/DynamoE2EContracts.cs` enumerated only 12 embedded seeds (omitting `batch_edit_graph`).
- `HPDynamo/HPDynamo.E2E.Tests/Tiers/Tier1_ContractsAndServerTests.cs` asserted 24 total tools and 12 seeds.
- Additional assertions in `Tier3_CrossFeatureInteractionTests.cs` also tested for 24 tools.

This lagged behind the authoritative project specification in `PROJECT.md` (lines 45, 75), `ORIGINAL_REQUEST.md` (lines 504-506), and the production server test suite `HPDynamo.Mcp.Server.Tests/Surface/ToolSurfaceTests.cs` (lines 71-74), which rigorously validate the **25-tool surface** (4 Core Tools + 8 Registry Meta Tools + 13 Embedded Seeds).

This report presents the complete root-cause analysis, exact file-level synchronization edits, and empirical verification proving 100% test pass (209/209 tests) under the updated 25-tool contract with zero regressions.

---

## 2. Problem Context & Root Cause Analysis

### 2.1 The 25-Tool Surface Standard

The HPDynamo MCP subsystem delivers 25 tools total:
1. **4 Core Tools**:
   - `execute_dynamo_code` (mutating/transactional Roslyn script runner)
   - `execute_designscript` (DesignScript parser & runner)
   - `get_dynamo_context` (system inspection & health check)
   - `cancel_execution` (correlated cancellation)
2. **8 Registry Meta Tools**:
   - `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`
   *(Note: The host engine suppresses `inspect_type`, reducing the standard 9 registry tools down to 8).*
3. **13 Embedded Seeds**:
   - Graph: `run_dyn_file`, `validate_dyn_file`, `batch_edit_graph`
   - Workspace: `get_active_graph_info`
   - Library: `search_nodes_library`, `inspect_node_type`
   - Node: `add_node_to_workspace`, `set_node_properties`
   - Wire: `connect_node_ports`, `disconnect_node_ports`
   - Data: `get_node_output_values`
   - Export: `export_graph_to_image`
   - CodeBlock: `create_code_block_node`

Total: **4 + 8 + 13 = 25 tools**.

### 2.2 Root Cause of Contract Lag

When `batch_edit_graph` was implemented in `HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/`:
- `ToolSurfaceTests.cs` in `HPDynamo.Mcp.Server.Tests` was updated to assert:
  - `12 static tools` (4 core + 8 registry)
  - `13 seeds` (including `batch_edit_graph`)
  - `25 combined tools`
- `Tier2_SeedAdversarialFormatAndRollbackTests.cs` in `HPDynamo.E2E.Tests` was updated to assert `13 tool.json` files on disk, explicitly enumerating `batch_edit_graph` among `expectedMutatingSeeds`.
- **However**, `DynamoE2EContracts.cs` (the simulated contract definition for E2E tests) was left untouched with only the original 12 seeds.
- Because `DynamoE2ETestHarness.BuildToolSurfaceList()` constructs the tool surface list by concatenating `CoreTools + RegistryMetaTools + EmbeddedSeeds`, its count remained $4 + 8 + 12 = 24$.
- Consequently, `Tier1_ContractsAndServerTests.cs` and `Tier3_CrossFeatureInteractionTests.cs` maintained obsolete assertions of `Assert.Equal(24, tools.Count)`.

---

## 3. Scope of Affected Files

Detailed inspection revealed 4 files requiring synchronization:

| # | File Path | Nature of Discrepancy |
|---|-----------|------------------------|
| 1 | `HPDynamo/HPDynamo.E2E.Tests/Contracts/DynamoE2EContracts.cs` | `EmbeddedSeeds` array missing `"batch_edit_graph"`. |
| 2 | `HPDynamo/HPDynamo.E2E.Tests/Tiers/Tier1_ContractsAndServerTests.cs` | Doc comment says 24 tools; `TC_T1_SRV_01` asserts 24; `TC_T1_SRV_08` asserts 12 seeds and lacks `batch_edit_graph`. |
| 3 | `HPDynamo/HPDynamo.E2E.Tests/Tiers/Tier3_CrossFeatureInteractionTests.cs` | `TC_T3_INT_03` asserts 24 tools; `TC_T3_INT_07` method name mentions 24 and asserts 24; `TC_T3_INT_08` asserts 24. |
| 4 | `HPDynamo/HPDynamo.E2E.Tests/Tiers/Tier5_SeedParameterFuzzingTests.cs` | Class doc comment mentions "all 12 Published Seeds" instead of 13. |

---

## 4. Exact Synchronization Design (Before & After)

### 4.1 `HPDynamo.E2E.Tests/Contracts/DynamoE2EContracts.cs`

**Lines 61-76**:
- **Before**:
```csharp
    public static readonly IReadOnlyList<string> EmbeddedSeeds = new ReadOnlyCollection<string>(new[]
    {
        "run_dyn_file",
        "get_active_graph_info",
        "search_nodes_library",
        "inspect_node_type",
        "add_node_to_workspace",
        "connect_node_ports",
        "disconnect_node_ports",
        "set_node_properties",
        "get_node_output_values",
        "export_graph_to_image",
        "validate_dyn_file",
        "create_code_block_node"
    });
```
- **After**:
```csharp
    public static readonly IReadOnlyList<string> EmbeddedSeeds = new ReadOnlyCollection<string>(new[]
    {
        "run_dyn_file",
        "get_active_graph_info",
        "search_nodes_library",
        "inspect_node_type",
        "add_node_to_workspace",
        "connect_node_ports",
        "disconnect_node_ports",
        "set_node_properties",
        "get_node_output_values",
        "export_graph_to_image",
        "validate_dyn_file",
        "create_code_block_node",
        "batch_edit_graph"
    });
```

---

### 4.2 `HPDynamo.E2E.Tests/Tiers/Tier1_ContractsAndServerTests.cs`

**Line 12**:
- **Before**:
```csharp
/// McpShared Contracts, Host Profiles, Server Tools, and 24-Tool Surface Invariants.
```
- **After**:
```csharp
/// McpShared Contracts, Host Profiles, Server Tools, and 25-Tool Surface Invariants.
```

**Lines 78-83 (`TC_T1_SRV_01`)**:
- **Before**:
```csharp
    [Fact]
    public void TC_T1_SRV_01_ToolSurface_Count_IsExactly24Tools()
    {
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(24, tools.Count);
    }
```
- **After**:
```csharp
    [Fact]
    public void TC_T1_SRV_01_ToolSurface_Count_IsExactly25Tools()
    {
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(25, tools.Count);
    }
```

**Lines 124-131 (`TC_T1_SRV_08`)**:
- **Before**:
```csharp
    [Fact]
    public void TC_T1_SRV_08_EmbeddedSeeds_ContainsAll12SpecifiedSeeds()
    {
        Assert.Equal(12, DynamoE2EContracts.EmbeddedSeeds.Count);
        Assert.Contains("run_dyn_file", DynamoE2EContracts.EmbeddedSeeds);
        Assert.Contains("create_code_block_node", DynamoE2EContracts.EmbeddedSeeds);
    }
```
- **After**:
```csharp
    [Fact]
    public void TC_T1_SRV_08_EmbeddedSeeds_ContainsAll13SpecifiedSeeds()
    {
        Assert.Equal(13, DynamoE2EContracts.EmbeddedSeeds.Count);
        Assert.Contains("run_dyn_file", DynamoE2EContracts.EmbeddedSeeds);
        Assert.Contains("create_code_block_node", DynamoE2EContracts.EmbeddedSeeds);
        Assert.Contains("batch_edit_graph", DynamoE2EContracts.EmbeddedSeeds);
    }
```

---

### 4.3 `HPDynamo.E2E.Tests/Tiers/Tier3_CrossFeatureInteractionTests.cs`

**Lines 67-69 (`TC_T3_INT_03`)**:
- **Before**:
```csharp
        // 3. Publish to tool surface
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(24, tools.Count);
```
- **After**:
```csharp
        // 3. Publish to tool surface
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(25, tools.Count);
```

**Lines 125-136 (`TC_T3_INT_07`)**:
- **Before**:
```csharp
    [Fact]
    public void TC_T3_INT_07_DualLifecycleSwitch_HeadlessToLiveCanvas_Maintains24ToolsSurface()
    {
        var bridge = new DynamoE2EFakeBridge();
        var toolsBefore = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(24, toolsBefore.Count);
        Assert.False(bridge.GetContext().IsLiveCanvasAttached);

        bridge.AttachLiveCanvas("LiveSession");
        var toolsAfter = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(24, toolsAfter.Count);
        Assert.True(bridge.GetContext().IsLiveCanvasAttached);
    }
```
- **After**:
```csharp
    [Fact]
    public void TC_T3_INT_07_DualLifecycleSwitch_HeadlessToLiveCanvas_Maintains25ToolsSurface()
    {
        var bridge = new DynamoE2EFakeBridge();
        var toolsBefore = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(25, toolsBefore.Count);
        Assert.False(bridge.GetContext().IsLiveCanvasAttached);

        bridge.AttachLiveCanvas("LiveSession");
        var toolsAfter = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(25, toolsAfter.Count);
        Assert.True(bridge.GetContext().IsLiveCanvasAttached);
    }
```

**Lines 148-150 (`TC_T3_INT_08`)**:
- **Before**:
```csharp
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(24, tools.Count);
```
- **After**:
```csharp
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(25, tools.Count);
```

---

### 4.4 `HPDynamo.E2E.Tests/Tiers/Tier5_SeedParameterFuzzingTests.cs`

**Line 11**:
- **Before**:
```csharp
/// Tier 5: Adversarial Parameter Fuzzing Suite for all 12 Published Seeds.
```
- **After**:
```csharp
/// Tier 5: Adversarial Parameter Fuzzing Suite for all 13 Published Seeds.
```

---

## 5. Empirical Verification Results

Following the changes, the test suites were compiled and executed against the live repo:

### 5.1 Targeted Surface Invariant Verification
```
dotnet test HPDynamo/HPDynamo.E2E.Tests --filter "FullyQualifiedName~TC_T1_SRV_01|FullyQualifiedName~TC_T1_SRV_08|FullyQualifiedName~TC_T3_INT_07"
```
**Result**:
- `TC_T1_SRV_01_ToolSurface_Count_IsExactly25Tools`: **Passed**
- `TC_T1_SRV_08_EmbeddedSeeds_ContainsAll13SpecifiedSeeds`: **Passed**
- `TC_T3_INT_07_DualLifecycleSwitch_HeadlessToLiveCanvas_Maintains25ToolsSurface`: **Passed**
- Duration: 3s 518ms

### 5.2 Full HPDynamo.E2E.Tests Suite
```
dotnet test HPDynamo/HPDynamo.E2E.Tests
```
**Result**:
```
Run tests: 'HPDynamo.E2E.Tests.dll' [net10.0|x64]
Passed! - Failed: 0, Passed: 209, Skipped: 0, Total: 209, Duration: 3s 474ms
Tests succeeded: 'HPDynamo.E2E.Tests.dll' [net10.0|x64]
```

### 5.3 Full Regression Across Host Suites
1. `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests`:
   - `Passed! - Failed: 0, Passed: 127, Skipped: 0, Total: 127, Duration: 5s 861ms` (100% Pass)
2. `dotnet test HPDynamo/HPDynamo.McpBridge.Tests`:
   - `Passed! - Failed: 0, Passed: 170, Skipped: 0, Total: 170, Duration: 6s 482ms` (100% Pass)

Total across HPDynamo projects: **506 / 506 tests passed (100% Green)**.

---

## 6. Deliverable Artifacts

1. `e2e_contracts_sync.patch` in this working directory containing unified git patch format.
2. `report.md` (this report) providing full architectural analysis and verification data.
3. `handoff.md` self-contained 5-component handoff report.
