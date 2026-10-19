namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HPDynamo.E2E.Tests.Contracts;
using HPDynamo.E2E.Tests.Harness;
using Xunit;

/// <summary>
/// Tier 4: Real-World Workload Scenarios & Application Benchmarks.
/// Validates realistic production engineering pipelines and adversarial resilience.
/// </summary>
public sealed class Tier4_RealWorldWorkloadTests
{
    [Fact]
    public async Task TC_T4_RW_01_HeadlessParametricRebarGraphExecution()
    {
        // Scenario RW-01: Headless Parametric Rebar Framing Graph Execution
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        var sampleGraph = DynamoE2ETestHarness.CreateSampleRebarGraph();

        // 1. Parameter overrides for TCVN rebar detailing
        var overrides = new Dictionary<string, object>
        {
            ["SpanLength"] = 6000.0,
            ["BarDiameter"] = 20,
            ["StirrupSpacing"] = 150
        };

        // 2. Validate graph JSON
        string graphJson = sampleGraph.ToJson();
        var validatedGraph = DynamoGraphSpec.FromJson(graphJson);
        Assert.NotNull(validatedGraph);

        // 3. Execute graph headlessly under TransactionGroup rollback
        var runReq = new DynamoExecutionRequest("doc.CreateRebar(); doc.CreateElement();", DryRun: true);
        var result = await bridge.ExecuteAsync(runReq);

        // 4. Assert full rollback and zero ghost elements
        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public void TC_T4_RW_02_LiveCanvasVisualNodeConstructionAndExport()
    {
        // Scenario RW-02: Live Canvas Visual Node Construction & Export
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas("ReinforcementFraming");

        // 1. Create CodeBlock node with DesignScript content
        var codeNode = DynamoE2ETestHarness.AddNodeToCanvas(
            bridge, "DesignScript.CodeBlock", "Dynamo.Graph.Nodes.CodeBlockNodeModel", 100, 150);
        codeNode.Properties["Script"] = "x = 100; y = 200; pt = Point.ByCoordinates(x, y, 0);";

        // 2. Place Curve node and wire ports
        var curveNode = DynamoE2ETestHarness.AddNodeToCanvas(
            bridge, "Curve.ByPoints", "DSCoreNodes.Curve", 450, 150);
        var wire = DynamoE2ETestHarness.ConnectPortsOnCanvas(bridge, codeNode.Id, 0, curveNode.Id, 0);

        // 3. Configure title and inspect canvas workspace
        codeNode.Properties["Title"] = "Reinforcement Anchor";
        var context = bridge.GetContext();

        Assert.Equal(2, context.NodesCount);
        Assert.Equal(1, context.WiresCount);

        // 4. Export graph image artifact
        byte[] imageBytes = DynamoE2ETestHarness.GenerateMockGraphImage();
        Assert.True(imageBytes.Length >= 8);
        Assert.Equal(0x89, imageBytes[0]);
    }

    [Fact]
    public async Task TC_T4_RW_03_AiAutonomousAssistantLoop()
    {
        // Scenario RW-03: AI Autonomous Assistant Multi-Turn Workflow
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };

        // Step 1: Query context
        var ctx = bridge.GetContext();
        Assert.True(ctx.IsConnected);
        Assert.Equal("2026.0", ctx.RevitVersion);

        // Step 2: Search node library
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Contains("search_nodes_library", tools);

        // Step 3: Verify node contracts
        Assert.Contains("inspect_node_type", tools);

        // Step 4: Execute DesignScript
        var execReq = new DynamoExecutionRequest(
            "var pt = Autodesk.DesignScript.Geometry.Point.ByCoordinates(0, 0, 0);", DryRun: false);
        var execResult = await bridge.ExecuteAsync(execReq);
        Assert.True(execResult.Success);

        // Step 5: Query active graph info
        var finalCtx = bridge.GetContext();
        Assert.NotNull(finalCtx.ActiveWorkspaceName);
    }

    [Fact]
    public async Task TC_T4_RW_04_ResilienceAndSecurityBreachDefense()
    {
        // Scenario RW-04: Adversarial Security Defense & Fault Recovery
        var bridge = new DynamoE2EFakeBridge();

        // 1. Attempt forbidden script injection (Process.Start) -> Blocked
        var scriptBlockResult = bridge.ValidateScript("System.Diagnostics.Process.Start(\"cmd.exe\");");
        Assert.False(scriptBlockResult.IsValid);

        // 2. Attempt execution while Opt-In is OFF -> Blocked with -32001
        var unauthExec = await bridge.ExecuteAsync(new DynamoExecutionRequest("int a = 1;"));
        Assert.False(unauthExec.Success);
        Assert.Equal(DynamoE2EContracts.OptInDisabledErrorCode, unauthExec.ErrorCode);

        // 3. User ticks "Allow AI Dynamo execution"
        bridge.IsExecutionEnabled = true;

        // 4. Valid execution proceeds smoothly immediately afterward
        var validExec = await bridge.ExecuteAsync(new DynamoExecutionRequest("int val = 42;", DryRun: false));
        Assert.True(validExec.Success);
        Assert.False(validExec.RolledBack);
    }
}
