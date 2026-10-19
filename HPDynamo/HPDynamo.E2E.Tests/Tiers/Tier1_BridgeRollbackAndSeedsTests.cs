namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Threading.Tasks;
using HPDynamo.E2E.Tests.Contracts;
using HPDynamo.E2E.Tests.Harness;
using Xunit;

/// <summary>
/// Tier 1: Category-Partition (Feature Coverage) - Part 2:
/// Dual-Lifecycle Bridge, Dual-Level Rollback Engine, Security Opt-In Gate, and Seed Execution.
/// </summary>
public sealed class Tier1_BridgeRollbackAndSeedsTests
{
    [Fact]
    public void TC_T1_BRG_01_HeadlessStartup_InitialStateIsHeadless()
    {
        var bridge = new DynamoE2EFakeBridge();
        Assert.False(bridge.IsLiveCanvasAttached);
    }

    [Fact]
    public void TC_T1_BRG_02_LiveCanvasHook_AttachesWorkspaceSuccessfully()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas("FramingProject");
        Assert.True(bridge.IsLiveCanvasAttached);
        Assert.Equal("FramingProject", bridge.ActiveCanvasWorkspace.Name);
    }

    [Fact]
    public void TC_T1_BRG_03_LiveCanvasHook_DetachesCleanlyBackToHeadless()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas("TestWorkspace");
        bridge.DetachLiveCanvas();
        Assert.False(bridge.IsLiveCanvasAttached);
    }

    [Fact]
    public void TC_T1_BRG_04_ContextLifecycleFlag_ReflectsLiveCanvasAttachment()
    {
        var bridge = new DynamoE2EFakeBridge();
        var contextBefore = bridge.GetContext();
        Assert.False(contextBefore.IsLiveCanvasAttached);

        bridge.AttachLiveCanvas("CanvasWorkspace");
        var contextAfter = bridge.GetContext();
        Assert.True(contextAfter.IsLiveCanvasAttached);
    }

    [Fact]
    public async Task TC_T1_ROL_01_DryRunExecution_RollsBackCreatedRevitElements()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        var req = new DynamoExecutionRequest("doc.CreateRebar();", DryRun: true);
        var result = await bridge.ExecuteAsync(req);

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T1_ROL_02_CommitExecution_PersistsCreatedRevitElements()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        var req = new DynamoExecutionRequest("doc.CreateRebar();", DryRun: false);
        var result = await bridge.ExecuteAsync(req);

        Assert.True(result.Success);
        Assert.False(result.RolledBack);
        Assert.Equal(1, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T1_ROL_03_DryRunExecution_ReportsRolledBackTrue()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        var req = new DynamoExecutionRequest("// harmless calculation", DryRun: true);
        var result = await bridge.ExecuteAsync(req);
        Assert.True(result.RolledBack);
    }

    [Fact]
    public async Task TC_T1_ROL_04_CommitExecution_ReportsRolledBackFalse()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        var req = new DynamoExecutionRequest("// harmless calculation", DryRun: false);
        var result = await bridge.ExecuteAsync(req);
        Assert.False(result.RolledBack);
    }

    [Fact]
    public void TC_T1_SEC_01_OptInToggle_DefaultStateIsDisabled()
    {
        var bridge = new DynamoE2EFakeBridge();
        Assert.False(bridge.IsExecutionEnabled);
    }

    [Fact]
    public async Task TC_T1_SEC_02_OptInDisabled_RejectsExecutionWithCode32001()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = false };
        var req = new DynamoExecutionRequest("int a = 1;");
        var result = await bridge.ExecuteAsync(req);

        Assert.False(result.Success);
        Assert.Equal(DynamoE2EContracts.OptInDisabledErrorCode, result.ErrorCode);
        Assert.Contains(DynamoE2EContracts.OptInDisabledMessageFragment, result.ErrorMessage);
    }

    [Fact]
    public async Task TC_T1_SEC_03_OptInEnabled_AllowsScriptExecution()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        var req = new DynamoExecutionRequest("int a = 1;");
        var result = await bridge.ExecuteAsync(req);
        Assert.True(result.Success);
    }

    [Fact]
    public void TC_T1_SEC_04_ScriptGuard_DeniesSystemIO()
    {
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript("System.IO.File.Delete(\"test.txt\");");
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.IO"));
    }

    [Fact]
    public void TC_T1_SEC_05_ScriptGuard_DeniesProcess()
    {
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript("System.Diagnostics.Process.Start(\"cmd.exe\");");
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("Process"));
    }

    [Fact]
    public void TC_T1_SEC_06_ScriptGuard_ValidScriptPassesChecks()
    {
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript("var pt = Autodesk.DesignScript.Geometry.Point.ByCoordinates(0, 0, 0);");
        Assert.True(quality.IsValid);
        Assert.Empty(quality.Errors);
    }

    [Fact]
    public void TC_T1_SED_01_CanvasAuthoring_AddNode_CreatesNodeSuccessfully()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas();
        var node = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "Point.Origin", "DSCoreNodes.Point", 50, 100);

        Assert.Single(bridge.ActiveCanvasWorkspace.Nodes);
        Assert.Equal("Point.Origin", node.Name);
    }

    [Fact]
    public void TC_T1_SED_02_CanvasAuthoring_ConnectPorts_CreatesWireSuccessfully()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas();
        var n1 = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "Node1", "Type1", 0, 0);
        var n2 = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "Node2", "Type2", 200, 0);
        var wire = DynamoE2ETestHarness.ConnectPortsOnCanvas(bridge, n1.Id, 0, n2.Id, 0);

        Assert.Single(bridge.ActiveCanvasWorkspace.Wires);
        Assert.Equal(n1.Id, wire.SourceNodeId);
        Assert.Equal(n2.Id, wire.TargetNodeId);
    }

    [Fact]
    public void TC_T1_SED_03_CanvasAuthoring_ExportGraphImage_ReturnsValidPngHeader()
    {
        byte[] bytes = DynamoE2ETestHarness.GenerateMockGraphImage();
        Assert.True(bytes.Length >= 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x4E, bytes[2]); // 'N'
        Assert.Equal(0x47, bytes[3]); // 'G'
    }

    [Fact]
    public void TC_T1_SED_04_GraphSpec_SerializesAndDeserializesCleanly()
    {
        var graph = DynamoE2ETestHarness.CreateSampleRebarGraph();
        string json = graph.ToJson();
        var deserialized = DynamoGraphSpec.FromJson(json);

        Assert.NotNull(deserialized);
        Assert.Equal(graph.Name, deserialized!.Name);
        Assert.Equal(2, deserialized.Nodes.Count);
        Assert.Single(deserialized.Wires);
    }
}
