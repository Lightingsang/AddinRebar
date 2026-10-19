namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Text.Json;
using HPBim.Client.Pipeline;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.JsonRpc;
using Xunit;

public sealed class SourceKeyWireRoundtripTests
{
    [Fact]
    public void CompositeSourceKey_PreservesCanonicalEquality_ThroughBridgeJsonAndScriptArgs()
    {
        var key = CompositeSourceKey.Create("NhaPho_5x18m", CoordinationCategory.Column, "C1");
        var item = new ElementPlanItem
        {
            SourceKey = key,
            Category = CoordinationCategory.Column,
            Mark = "C1",
            SequenceOrder = 1
        };

        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-123",
            Items = new[] { item }
        };

        // 1. Serialize via BridgeJson
        var element = BridgeJson.ToElement(plan);
        Assert.NotNull(element);

        // 2. Deserialize via JsonElement
        var root = element.Value;
        var items = root.GetProperty("items");
        var firstItem = items[0];
        string wireSourceKey = firstItem.GetProperty("sourceKey").GetString() ?? string.Empty;

        // Assert wire string is canonical format
        Assert.Equal("[NhaPho_5x18m]#[Column]:C1", wireSourceKey);

        // 3. Parse back to CompositeSourceKey
        Assert.True(CompositeSourceKey.TryParse(wireSourceKey, out var parsedKey));
        Assert.Equal(key, parsedKey);
        Assert.Equal(key.DocIdentifier, parsedKey!.DocIdentifier);
        Assert.Equal(key.Category, parsedKey.Category);
        Assert.Equal(key.PrimaryHandle, parsedKey.PrimaryHandle);
    }
}
