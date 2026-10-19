namespace HPBim.Core.Tests.Coordination;

using System;
using System.Collections.Generic;
using System.Text.Json;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using Xunit;

public sealed class EmpiricalChallenger2M4ProvenanceTests
{
    private readonly JsonSerializerOptions _options = new();

    [Fact]
    public void Roundtrip_TenProvenanceHandlesWithSymbolsAndUnicode_PreservesExactArray()
    {
        var original = CompositeSourceKey.Create(
            "Drawing_5x18m.dwg",
            CoordinationCategory.Column,
            "C1_AXIS_X1-YA",
            "handle:0x1A4F",
            "axis:Trục_1",
            "axis:Trục_A",
            "block:Móng_ĐC-1",
            "line:\"quoted_entity\"",
            "space handle 123",
            "cad_layer:KẾT_CẤU_CỘT",
            "handle:0xFFFF",
            "sub_element#42",
            "final_prov_99");

        string json = JsonSerializer.Serialize(original, _options);
        var restored = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);

        Assert.NotNull(restored);
        Assert.Equal(original, restored);
        Assert.Equal(10, restored!.ProvenanceHandles.Count);
        for (int i = 0; i < original.ProvenanceHandles.Count; i++)
        {
            Assert.Equal(original.ProvenanceHandles[i], restored.ProvenanceHandles[i]);
        }
    }

    [Fact]
    public void Roundtrip_ElementPlanItemWithProvenanceHandles_PreservesSourceKeyHandles()
    {
        var key = CompositeSourceKey.Create(
            "Structural_Plan.dwg",
            CoordinationCategory.Beam,
            "B101",
            "h_cad_axis_x1",
            "h_cad_axis_x2",
            "h_cad_line_top");

        var planItem = new ElementPlanItem
        {
            SourceKey = key,
            Category = CoordinationCategory.Beam,
            Mark = "B101",
            SequenceOrder = 1,
            TargetUniqueId = "revit-unique-id-beam-101"
        };

        string json = JsonSerializer.Serialize(planItem, _options);
        var restoredItem = JsonSerializer.Deserialize<ElementPlanItem>(json, _options);

        Assert.NotNull(restoredItem);
        Assert.Equal(planItem.SourceKey, restoredItem!.SourceKey);
        Assert.Equal(3, restoredItem.SourceKey.ProvenanceHandles.Count);
        Assert.Equal("h_cad_axis_x1", restoredItem.SourceKey.ProvenanceHandles[0]);
        Assert.Equal("h_cad_axis_x2", restoredItem.SourceKey.ProvenanceHandles[1]);
        Assert.Equal("h_cad_line_top", restoredItem.SourceKey.ProvenanceHandles[2]);
    }

    [Fact]
    public void Roundtrip_EmptyProvenanceHandles_RetainsCanonicalStringFormat()
    {
        var key = CompositeSourceKey.Create("DocA.dwg", CoordinationCategory.Slab, "S1");
        string json = JsonSerializer.Serialize(key, _options);

        Assert.Equal("\"[DocA.dwg]#[Slab]:S1\"", json);

        var restored = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);
        Assert.NotNull(restored);
        Assert.Equal(key, restored);
        Assert.Empty(restored!.ProvenanceHandles);
    }

    [Fact]
    public void Deserialize_LegacyObjectWithNumericCategoryAndProvenance_ParsesCorrectly()
    {
        string json = """
        {
            "docIdentifier": "ModelC.dwg",
            "category": 4,
            "primaryHandle": "COL-44",
            "provenanceHandles": ["h_grid1", "h_grid2"]
        }
        """;

        var restored = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);
        Assert.NotNull(restored);
        Assert.Equal("ModelC.dwg", restored!.DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, restored.Category);
        Assert.Equal("COL-44", restored.PrimaryHandle);
        Assert.Equal(2, restored.ProvenanceHandles.Count);
        Assert.Equal("h_grid1", restored.ProvenanceHandles[0]);
        Assert.Equal("h_grid2", restored.ProvenanceHandles[1]);
    }

    [Fact]
    public void Deserialize_JsonObjectWithKeyAndProvenance_ParsesCorrectly()
    {
        string json = """
        {
            "key": "[NhaPho]#[Wall]:W-10",
            "provenanceHandles": ["line1", "line2", "line3"]
        }
        """;

        var restored = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);
        Assert.NotNull(restored);
        Assert.Equal("NhaPho", restored!.DocIdentifier);
        Assert.Equal(CoordinationCategory.Wall, restored.Category);
        Assert.Equal("W-10", restored.PrimaryHandle);
        Assert.Equal(3, restored.ProvenanceHandles.Count);
        Assert.Equal("line1", restored.ProvenanceHandles[0]);
        Assert.Equal("line2", restored.ProvenanceHandles[1]);
        Assert.Equal("line3", restored.ProvenanceHandles[2]);
    }
}
