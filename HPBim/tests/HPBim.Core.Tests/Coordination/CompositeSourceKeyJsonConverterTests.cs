namespace HPBim.Core.Tests.Coordination;

using System;
using System.Collections.Generic;
using System.Text.Json;
using HPBim.Core.Coordination;
using Xunit;

public sealed class CompositeSourceKeyJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public CompositeSourceKeyJsonConverterTests()
    {
        _options = new JsonSerializerOptions();
    }

    [Fact]
    public void Serialize_ReturnsCanonicalString()
    {
        var key = CompositeSourceKey.Create("Drawing1.dwg", CoordinationCategory.Column, "2B4F");
        string json = JsonSerializer.Serialize(key, _options);

        Assert.Equal("\"[Drawing1.dwg]#[Column]:2B4F\"", json);
    }

    [Fact]
    public void Roundtrip_MaintainsStrictEquality()
    {
        var original = CompositeSourceKey.Create("NhaPho_5x18m", CoordinationCategory.Beam, "B101");
        string json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal(original, deserialized);
        Assert.Equal(original.DocIdentifier, deserialized!.DocIdentifier);
        Assert.Equal(original.Category, deserialized.Category);
        Assert.Equal(original.PrimaryHandle, deserialized.PrimaryHandle);
    }

    [Fact]
    public void Roundtrip_WithProvenanceHandles_PreservesAllHandles()
    {
        var original = CompositeSourceKey.Create(
            "StructuralModel.dwg",
            CoordinationCategory.Column,
            "C1",
            "handle_axis_x1",
            "handle_axis_ya",
            "handle_cad_block_99");

        string json = JsonSerializer.Serialize(original, _options);
        Assert.Contains("\"provenanceHandles\":[", json);
        Assert.Contains("\"handle_axis_x1\"", json);
        Assert.Contains("\"handle_axis_ya\"", json);
        Assert.Contains("\"handle_cad_block_99\"", json);

        var deserialized = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);
        Assert.NotNull(deserialized);
        Assert.Equal(original.DocIdentifier, deserialized!.DocIdentifier);
        Assert.Equal(original.Category, deserialized.Category);
        Assert.Equal(original.PrimaryHandle, deserialized.PrimaryHandle);
        Assert.Equal(3, deserialized.ProvenanceHandles.Count);
        Assert.Equal(original.ProvenanceHandles, deserialized.ProvenanceHandles);
    }

    [Fact]
    public void Roundtrip_EmptyProvenanceHandles_PreservesCanonicalString()
    {
        var original = CompositeSourceKey.Create("Arch.dwg", CoordinationCategory.Wall, "W1");
        string json = JsonSerializer.Serialize(original, _options);

        Assert.Equal("\"[Arch.dwg]#[Wall]:W1\"", json);

        var deserialized = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);
        Assert.NotNull(deserialized);
        Assert.Equal(original, deserialized);
        Assert.Empty(deserialized!.ProvenanceHandles);
    }

    [Fact]
    public void DictionaryKey_Roundtrip_MaintainsEquality()
    {
        var dict = new Dictionary<CompositeSourceKey, string>
        {
            [CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1")] = "TargetUniqueId_C1"
        };

        string json = JsonSerializer.Serialize(dict, _options);
        Assert.Contains("\"[DocA]#[Column]:C1\":\"TargetUniqueId_C1\"", json);

        var restored = JsonSerializer.Deserialize<Dictionary<CompositeSourceKey, string>>(json, _options);
        Assert.NotNull(restored);
        var expectedKey = CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1");
        Assert.True(restored!.ContainsKey(expectedKey));
        Assert.Equal("TargetUniqueId_C1", restored[expectedKey]);
    }

    [Fact]
    public void Deserialize_FromJsonObjectWithKeyProperty_Success()
    {
        string json = """
        {
            "key": "[DocCustom]#[Column]:COL-55",
            "provenanceHandles": ["ph1", "ph2"]
        }
        """;

        var deserialized = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);
        Assert.NotNull(deserialized);
        Assert.Equal("DocCustom", deserialized!.DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, deserialized.Category);
        Assert.Equal("COL-55", deserialized.PrimaryHandle);
        Assert.Equal(2, deserialized.ProvenanceHandles.Count);
        Assert.Equal("ph1", deserialized.ProvenanceHandles[0]);
        Assert.Equal("ph2", deserialized.ProvenanceHandles[1]);
    }

    [Fact]
    public void Deserialize_FromString_PlainFormat_Success()
    {
        string json = "\"NhaPho_5x18m#Slab:S1\"";
        var deserialized = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal("NhaPho_5x18m", deserialized!.DocIdentifier);
        Assert.Equal(CoordinationCategory.Slab, deserialized.Category);
        Assert.Equal("S1", deserialized.PrimaryHandle);
    }

    [Fact]
    public void Deserialize_FromLegacyJsonObject_Success()
    {
        string json = """
        {
            "docIdentifier": "ModelA",
            "category": "Column",
            "primaryHandle": "COL-12",
            "provenanceHandles": ["h1", "h2"]
        }
        """;

        var deserialized = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal("ModelA", deserialized!.DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, deserialized.Category);
        Assert.Equal("COL-12", deserialized.PrimaryHandle);
        Assert.Equal(2, deserialized.ProvenanceHandles.Count);
        Assert.Contains("h1", deserialized.ProvenanceHandles);
    }

    [Fact]
    public void Deserialize_FromLegacyJsonObject_NumericCategory_Success()
    {
        string json = """
        {
            "docIdentifier": "ModelB",
            "category": 4,
            "primaryHandle": "C99"
        }
        """;

        var deserialized = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal("ModelB", deserialized!.DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, deserialized.Category);
        Assert.Equal("C99", deserialized.PrimaryHandle);
    }

    [Fact]
    public void Serialize_EmptyKey_SerializesEmptyString()
    {
        var key = CompositeSourceKey.Empty;
        string json = JsonSerializer.Serialize(key, _options);

        Assert.Equal("\"\"", json);
    }

    [Fact]
    public void Deserialize_EmptyString_ReturnsEmptyKey()
    {
        string json = "\"\"";
        var deserialized = JsonSerializer.Deserialize<CompositeSourceKey>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal(CompositeSourceKey.Empty, deserialized);
    }

    [Fact]
    public void BimCoordinationLedgerSerializer_PreservesCanonicalFormat()
    {
        var ledger = new BimCoordinationLedger
        {
            Project = "TestProject"
        };
        var mapping = LedgerElementMapping.Create(
            CompositeSourceKey.Create("Doc1", CoordinationCategory.Column, "C1"),
            "unique-id-abc",
            12345L,
            CoordinationCategory.Column,
            "C1",
            "hash-123");

        ledger = ledger.RegisterMapping(mapping);

        string json = BimCoordinationLedgerSerializer.Serialize(ledger);
        Assert.Contains("[Doc1]#[Column]:C1", json);

        var restored = BimCoordinationLedgerSerializer.Deserialize(json);
        Assert.True(restored.Mappings.ContainsKey("[Doc1]#[Column]:C1"));
        var restoredMapping = restored.Mappings["[Doc1]#[Column]:C1"];
        Assert.Equal("unique-id-abc", restoredMapping.TargetUniqueId);
    }
}
