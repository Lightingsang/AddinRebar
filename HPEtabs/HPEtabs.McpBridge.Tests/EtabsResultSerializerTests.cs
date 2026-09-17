using System.Text.Json;
using HPEtabs.McpBridge.Service;
using Xunit;

namespace HPEtabs.McpBridge.Tests;

/// <summary>What a script's return value looks like to the AI: plain values and shapes pass, dictionaries of any kind walk, output is capped.</summary>
public sealed class EtabsResultSerializerTests
{
    private readonly EtabsResultSerializer _serializer = new(4 * 1024);

    [Fact]
    public void Generic_dictionaries_serialise_as_objects()
    {
        var report = new Dictionary<string, string> { ["PointObj"] = "ret=0 count=12", ["FrameObj"] = "ret=0 count=34" };

        var (value, type, truncated) = _serializer.Serialize(report);

        Assert.False(truncated);
        Assert.Equal("Dictionary`2", type);
        Assert.Equal("ret=0 count=34", value!.Value.GetProperty("FrameObj").GetString());
    }

    [Fact]
    public void Anonymous_objects_arrays_and_enums_keep_their_shape()
    {
        var (value, _, _) = _serializer.Serialize(new { present = ETABSv1.eUnits.kN_mm_C, names = new[] { "P1", "P2" }, n = 2, ret = 0 });

        Assert.Equal("kN_mm_C", value!.Value.GetProperty("present").GetString());
        Assert.Equal(2, value.Value.GetProperty("names").GetArrayLength());
        Assert.Equal(JsonValueKind.Number, value.Value.GetProperty("n").ValueKind);
    }

    [Fact]
    public void Oversized_output_is_replaced_by_a_preview_and_flagged()
    {
        var (value, _, truncated) = _serializer.Serialize(Enumerable.Range(0, 2000).Select(i => new { i, text = new string('x', 40) }).ToArray());

        Assert.True(truncated);
        Assert.True(value!.Value.TryGetProperty("truncatedPreview", out _));
    }

    [Fact]
    public void Null_and_scalars_are_passed_through()
    {
        Assert.Equal((null, "null", false), _serializer.Serialize(null));
        var (value, type, _) = _serializer.Serialize(42);
        Assert.Equal("Int32", type);
        Assert.Equal(42, value!.Value.GetInt32());
    }
}
