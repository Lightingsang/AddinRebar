namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Text.Json;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.JsonRpc;
using Xunit;

/// <summary>
/// Empirical Challenger tests for R1.1: Canonical String SourceKey roundtrip,
/// serialization fidelity, receipt mappings, and dictionary behavior.
/// </summary>
public sealed class EmpiricalChallengerSourceKeyRoundtripTests
{
    [Theory]
    [InlineData("Drawing_01.dwg", CoordinationCategory.Column, "0x4A1F")]
    [InlineData("TowerA_Floor03", CoordinationCategory.Beam, "B-101:Span-A")]
    [InlineData("MoHinh_Tang1", CoordinationCategory.Slab, "Slab.Main.01")]
    [InlineData("Model-2026.04", CoordinationCategory.Wall, "W_220_Ext")]
    [InlineData("SitePlan", CoordinationCategory.Pile, "Pile-D400-01")]
    public void SourceKey_CanonicalRoundtrip_ComplexAndSpecialStrings(
        string doc, CoordinationCategory cat, string handle)
    {
        var original = CompositeSourceKey.Create(doc, cat, handle);
        string canonicalStr = original.ToString();

        Assert.StartsWith("[", canonicalStr);
        Assert.Contains($"#[{cat}]:", canonicalStr);

        var parsed = CompositeSourceKey.Parse(canonicalStr);
        Assert.Equal(original, parsed);
        Assert.Equal(original.GetHashCode(), parsed.GetHashCode());

        string json = JsonSerializer.Serialize(original);
        Assert.Equal($"\"{canonicalStr}\"", json);

        var deserialized = JsonSerializer.Deserialize<CompositeSourceKey>(json);
        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void SourceKey_WireRoundtrip_AcrossPlanScriptArgsReceipt()
    {
        var key = CompositeSourceKey.Create("Structure_V1", CoordinationCategory.Column, "Col_01");
        var plan = new BimExecutionPlan
        {
            PlanId = Guid.NewGuid(),
            PlanHash = "hash-999",
            Items = new[]
            {
                new ElementPlanItem
                {
                    SourceKey = key,
                    Category = CoordinationCategory.Column,
                    Mark = "Col_01",
                    SequenceOrder = 1
                }
            }
        };

        var elem = BridgeJson.ToElement(plan);
        Assert.NotNull(elem);
        string wireSourceKey = elem.Value.GetProperty("items")[0].GetProperty("sourceKey").GetString()!;
        Assert.Equal("[Structure_V1]#[Column]:Col_01", wireSourceKey);

        var receiptMap = new Dictionary<string, string> { [wireSourceKey] = "revit-unique-id-col-1" };
        string receiptJson = JsonSerializer.Serialize(receiptMap);
        var restoredMap = JsonSerializer.Deserialize<Dictionary<string, string>>(receiptJson)!;

        Assert.True(restoredMap.ContainsKey(key.ToString()));
        Assert.Equal("revit-unique-id-col-1", restoredMap[key.ToString()]);
    }

    [Fact]
    public void SourceKey_DictionaryKeys_CaseInsensitiveRoundtrip()
    {
        var dict = new Dictionary<CompositeSourceKey, string>();
        var key1 = CompositeSourceKey.Create("DocA", CoordinationCategory.GradeBeam, "GB-01");
        dict[key1] = "UniqueId-GB1";

        var key2 = CompositeSourceKey.Create("doca", CoordinationCategory.GradeBeam, "gb-01");
        Assert.True(dict.ContainsKey(key2));
        Assert.Equal("UniqueId-GB1", dict[key2]);

        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.Initialize("ProjectX");
        ledgerService.RegisterMapping(LedgerElementMapping.Create(
            key1, "RvtUid-1", 1001L, CoordinationCategory.GradeBeam, "GB-01", "hash-1"));

        Assert.True(ledgerService.TryGetTarget(key2, out var targetUid));
        Assert.Equal("RvtUid-1", targetUid);
    }

    [Fact]
    public void SourceKey_EmptyDocIdentifier_RoundtripAndLooseParsing()
    {
        var key = CompositeSourceKey.Create(string.Empty, CoordinationCategory.Opening, "Door-101");
        string canonical = key.ToString();
        Assert.Equal("[]#[Opening]:Door-101", canonical);

        Assert.True(CompositeSourceKey.TryParse(canonical, out var parsed, allowEmptyDoc: true));
        Assert.Equal(key, parsed);
        Assert.Equal(string.Empty, parsed!.DocIdentifier);
    }

    [Theory]
    [InlineData("not-a-source-key")]
    [InlineData("[doc]#[NonExistentCategory]:handle")]
    [InlineData("")]
    public void SourceKey_MalformedString_TryParseFailsSafely(string malformed)
    {
        bool success = CompositeSourceKey.TryParse(malformed, out var result);
        Assert.False(success);
        Assert.Null(result);
    }
}
