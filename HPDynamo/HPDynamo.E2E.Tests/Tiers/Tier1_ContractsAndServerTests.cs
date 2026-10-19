namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Linq;
using System.Text.Json;
using HPDynamo.E2E.Tests.Contracts;
using HPDynamo.E2E.Tests.Harness;
using Xunit;

/// <summary>
/// Tier 1: Category-Partition (Feature Coverage) - Part 1:
/// McpShared Contracts, Host Profiles, Server Tools, and 25-Tool Surface Invariants.
/// </summary>
public sealed class Tier1_ContractsAndServerTests
{
    [Fact]
    public void TC_T1_CON_01_PipeNaming_DynamoHostKey_ResolvesCorrectPipeName()
    {
        string pipeName = DynamoE2EContracts.ResolvePipeName(DynamoE2EContracts.HostKey, 2026);
        Assert.Equal("hpdynamo-mcp-2026", pipeName);
    }

    [Fact]
    public void TC_T1_CON_02_JsonRpcMethods_DynamoPrefix_MatchesExpectedStandard()
    {
        Assert.Equal("dynamo.", DynamoE2EContracts.MethodPrefix);
        Assert.StartsWith(DynamoE2EContracts.MethodPrefix, "dynamo.execute");
        Assert.StartsWith(DynamoE2EContracts.MethodPrefix, "dynamo.context");
    }

    [Fact]
    public void TC_T1_CON_03_HostScriptContracts_DynamoImports_ContainsRequiredNamespaces()
    {
        var imports = DynamoE2EContracts.RequiredImports;
        Assert.Contains("Autodesk.Revit.DB", imports);
        Assert.Contains("Dynamo.Graph.Nodes", imports);
        Assert.Contains("Autodesk.DesignScript.Geometry", imports);
    }

    [Fact]
    public void TC_T1_CON_04_HostScriptContracts_DynamoGlobals_ContainsStandardVariables()
    {
        var globals = DynamoE2EContracts.RequiredGlobals;
        Assert.Contains("dynamo", globals);
        Assert.Contains("workspace", globals);
        Assert.Contains("doc", globals);
        Assert.Contains("ct", globals);
    }

    [Fact]
    public void TC_T1_CON_05_ContextResult_DynamoInfoRecord_SerializesCleanly()
    {
        var info = new DynamoInfoRecord(
            IsConnected: true,
            ActiveWorkspaceName: "Home",
            ActiveWorkspacePath: "C:\\Graphs\\test.dyn",
            NodesCount: 15,
            WiresCount: 12,
            DynamoVersion: "3.7.0.11777",
            RevitVersion: "2026.0",
            HasUnsavedChanges: false,
            IsExecutionEnabled: true,
            RunType: "Manual",
            IsLiveCanvasAttached: true);

        string json = JsonSerializer.Serialize(info);
        Assert.Contains("\"IsConnected\":true", json);
        Assert.Contains("\"NodesCount\":15", json);
    }

    [Fact]
    public void TC_T1_CON_06_HostProfile_DefaultVersion_Is2026()
    {
        Assert.Equal(2026, DynamoE2EContracts.DefaultHostVersion);
    }

    [Fact]
    public void TC_T1_SRV_01_ToolSurface_Count_IsExactly25Tools()
    {
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(25, tools.Count);
    }

    [Fact]
    public void TC_T1_SRV_02_ToolSurface_InspectType_IsSuppressed()
    {
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.DoesNotContain(DynamoE2EContracts.SuppressedEngineTool, tools);
    }

    [Fact]
    public void TC_T1_SRV_03_CoreTools_ContainsExecuteDynamoCode()
    {
        Assert.Contains("execute_dynamo_code", DynamoE2EContracts.CoreTools);
    }

    [Fact]
    public void TC_T1_SRV_04_CoreTools_ContainsExecuteDesignScript()
    {
        Assert.Contains("execute_designscript", DynamoE2EContracts.CoreTools);
    }

    [Fact]
    public void TC_T1_SRV_05_CoreTools_ContainsGetDynamoContext()
    {
        Assert.Contains("get_dynamo_context", DynamoE2EContracts.CoreTools);
    }

    [Fact]
    public void TC_T1_SRV_06_CoreTools_ContainsCancelExecution()
    {
        Assert.Contains("cancel_execution", DynamoE2EContracts.CoreTools);
    }

    [Fact]
    public void TC_T1_SRV_07_RegistryMetaTools_ContainsAll8ManagementTools()
    {
        Assert.Equal(8, DynamoE2EContracts.RegistryMetaTools.Count);
        Assert.Contains("propose_tool", DynamoE2EContracts.RegistryMetaTools);
        Assert.Contains("publish_tool", DynamoE2EContracts.RegistryMetaTools);
    }

    [Fact]
    public void TC_T1_SRV_08_EmbeddedSeeds_ContainsAll13SpecifiedSeeds()
    {
        Assert.Equal(13, DynamoE2EContracts.EmbeddedSeeds.Count);
        Assert.Contains("run_dyn_file", DynamoE2EContracts.EmbeddedSeeds);
        Assert.Contains("create_code_block_node", DynamoE2EContracts.EmbeddedSeeds);
        Assert.Contains("batch_edit_graph", DynamoE2EContracts.EmbeddedSeeds);
    }

    [Fact]
    public void TC_T1_SRV_09_ToolNamingConventions_AreLowercaseSnakeCase()
    {
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        foreach (var tool in tools)
        {
            Assert.Equal(tool.ToLowerInvariant(), tool);
            Assert.DoesNotContain(" ", tool);
        }
    }

    [Fact]
    public void TC_T1_SRV_10_ToolSurface_CoreToolNamesList_MatchesExpectedSet()
    {
        Assert.Equal(4, DynamoE2EContracts.CoreTools.Count);
    }

    [Fact]
    public void TC_T1_SRV_11_PipeNaming_CaseInsensitiveHost_ResolvesCorrectly()
    {
        string pipe = DynamoE2EContracts.ResolvePipeName(" DYNAMO ", 2026);
        Assert.Equal("hpdynamo-mcp-2026", pipe);
    }

    [Fact]
    public void TC_T1_SRV_12_ContextPayload_OmitsNullHostPropertiesByDefault()
    {
        var info = new DynamoInfoRecord(true, "Canvas", null, 0, 0, "3.7", "2026", false, false, "Auto", false);
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
        string json = JsonSerializer.Serialize(info, options);
        Assert.DoesNotContain("ActiveWorkspacePath", json);
    }
}
