namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class FindingR6_PreflightAndAtomicTests
{
    private readonly CanvasEditor _editor = new();

    [Fact]
    public void Preflight_WireConnectingToRemovedComponent_FailsValidation()
    {
        var targetGuid = Guid.NewGuid();
        var sourceGuid = Guid.NewGuid();

        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "componentsToRemove": [
                "{{targetGuid}}"
            ],
            "wiresToConnect": [
                { "source": "{{sourceGuid}}", "sourceOutput": 0, "target": "{{targetGuid}}", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: g =>
            g == sourceGuid || g == targetGuid ? TestProxyFactory.CreateComponent() : null);

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "wiresToConnect" && e.Reason.Contains("marked for removal in componentsToRemove"));
    }

    [Fact]
    public void Preflight_WireDisconnectingFromRemovedComponent_FailsValidation()
    {
        var targetGuid = Guid.NewGuid();
        var sourceGuid = Guid.NewGuid();

        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "componentsToRemove": [
                "{{sourceGuid}}"
            ],
            "wiresToDisconnect": [
                { "source": "{{sourceGuid}}", "sourceOutput": 0, "target": "{{targetGuid}}", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: g =>
            g == sourceGuid || g == targetGuid ? TestProxyFactory.CreateComponent() : null);

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "wiresToDisconnect" && e.Reason.Contains("marked for removal in componentsToRemove"));
    }

    [Fact]
    public void Preflight_OutOfRangePort_ExistingComponent_FailsValidation()
    {
        var compGuid = Guid.NewGuid();
        var comp = TestProxyFactory.CreateComponent("Addition", "+");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.InstanceGuid = compGuid;
        proxy.Inputs.Add(TestProxyFactory.CreateParam("A"));
        proxy.Inputs.Add(TestProxyFactory.CreateParam("B")); // 2 inputs: index 0 and 1

        var otherGuid = Guid.NewGuid();
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "wiresToConnect": [
                { "source": "{{otherGuid}}", "sourceOutput": 0, "target": "{{compGuid}}", "targetInput": 999 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: g =>
            g == compGuid ? comp : TestProxyFactory.CreateParam("X"));

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "wiresToConnect" && e.Reason.Contains("Port index 999") && e.Reason.Contains("exceeds input port count"));
    }

    [Fact]
    public void Preflight_OutOfRangePort_CreatedComponent_FailsValidation()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "addNode" }
            ],
            "wiresToConnect": [
                { "source": "addNode", "sourceOutput": 5, "target": "addNode", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(
            jsonDoc.RootElement,
            findObject: null,
            canCreateObject: _ => true,
            getPortBounds: t => string.Equals(t, "Addition", StringComparison.OrdinalIgnoreCase) ? (2, 1) : null);

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "wiresToConnect" && e.Reason.Contains("Port index 5") && e.Reason.Contains("exceeds output port count"));
    }

    [Fact]
    public void Preflight_OutOfRangePort_StandaloneParam_FailsValidation()
    {
        var paramGuid = Guid.NewGuid();
        var otherGuid = Guid.NewGuid();
        var param = TestProxyFactory.CreateParam("Number");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.InstanceGuid = paramGuid;

        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "wiresToConnect": [
                { "source": "{{paramGuid}}", "sourceOutput": 2, "target": "{{otherGuid}}", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: g =>
            g == paramGuid ? param : TestProxyFactory.CreateParam("Target"));

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "wiresToConnect" && e.Reason.Contains("parameters only have port index 0"));
    }
}
