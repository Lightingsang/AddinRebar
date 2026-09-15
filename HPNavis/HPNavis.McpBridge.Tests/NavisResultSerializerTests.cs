using System.Text.Json;
using HPNavis.McpBridge.Service;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPNavis.McpBridge.Tests;

/// <summary>
///     The result serializer on plain CLR values (Navisworks objects need Roamer): friendly type names, the
///     output bound that stops the walk instead of serialising everything first, cycles, and unserialisable
///     values falling back to text.
/// </summary>
public sealed class NavisResultSerializerTests
{
    private static readonly ScriptUnits Units = ScriptUnits.Millimeters;

    [Fact]
    public void Null_is_reported_as_null()
    {
        var (value, type, truncated) = new NavisResultSerializer(64 * 1024).Serialize(null, Units);

        Assert.Null(value);
        Assert.Equal("null", type);
        Assert.False(truncated);
    }

    [Fact]
    public void Anonymous_objects_and_generic_lists_get_friendly_type_names()
    {
        var serializer = new NavisResultSerializer(64 * 1024);

        var (value, type, _) = serializer.Serialize(new { sets = 2, name = "gatehouse" }, Units);
        Assert.Equal("object", type);
        Assert.Equal(2, value!.Value.GetProperty("sets").GetInt32());
        Assert.Equal("gatehouse", value.Value.GetProperty("name").GetString());

        var (list, listType, _) = serializer.Serialize(new List<string> { "a", "b" }, Units);
        Assert.Equal("List<String>", listType);
        Assert.Equal(2, list!.Value.GetArrayLength());

        var (dict, dictType, _) = serializer.Serialize(new Dictionary<string, int> { ["x"] = 1 }, Units);
        Assert.Equal("Dictionary<String, Int32>", dictType);
        Assert.Equal(1, dict!.Value.GetProperty("x").GetInt32());
    }

    [Fact]
    public void Output_beyond_the_limit_is_cut_while_it_is_written()
    {
        // 200k integers ≈ 1.3 MB of JSON against a 4 KB limit: the walk must stop near the limit, not after the full array
        var huge = Enumerable.Range(0, 200_000).ToArray();
        var serializer = new NavisResultSerializer(4 * 1024);

        var (value, type, truncated) = serializer.Serialize(huge, Units);

        Assert.True(truncated);
        Assert.Equal("Int32[]", type);
        var text = value!.Value.GetString()!;
        Assert.EndsWith("…[truncated]", text);
        Assert.StartsWith("[0,1,2,", text);
        Assert.InRange(text.Length, 1, 4 * 1024 + 64);
    }

    [Fact]
    public void The_walk_over_the_value_stops_near_the_limit_instead_of_visiting_every_item()
    {
        // a lazy sequence that counts how far the serializer pulled: with a 4 KB bound it must give up after a few
        // thousand short numbers, not after all 500 000 (the Stream overload flushes every ~15 KB and trips the bound)
        var pulled = 0;
        IEnumerable<int> Counting()
        {
            for (var i = 0; i < 500_000; i++) { pulled++; yield return i; }
        }

        var (_, _, truncated) = new NavisResultSerializer(4 * 1024).Serialize(Counting(), Units);

        Assert.True(truncated);
        Assert.InRange(pulled, 1, 20_000);
    }

    [Fact]
    public void Output_within_the_limit_is_returned_whole()
    {
        var (value, _, truncated) = new NavisResultSerializer(4 * 1024).Serialize(Enumerable.Range(0, 100).ToArray(), Units);

        Assert.False(truncated);
        Assert.Equal(100, value!.Value.GetArrayLength());
    }

    [Fact]
    public void Cycles_do_not_throw()
    {
        var node = new Node { Name = "root" };
        node.Parent = node;

        var (value, _, truncated) = new NavisResultSerializer(64 * 1024).Serialize(node, Units);

        Assert.False(truncated);
        Assert.Equal("root", value!.Value.GetProperty("name").GetString());
    }

    [Fact]
    public void An_unserialisable_value_falls_back_to_its_text()
    {
        var (value, type, _) = new NavisResultSerializer(64 * 1024).Serialize(new Throws(), Units);

        Assert.Equal("Throws", type);
        Assert.Equal("Throws", value!.Value.GetProperty("type").GetString());
        Assert.Contains("not serializable", value.Value.GetProperty("note").GetString());
    }

    [Fact]
    public void Bounded_stream_keeps_the_head_and_throws_once()
    {
        using var stream = new BoundedOutputStream(10);
        stream.Write(new byte[6], 0, 6);

        Assert.Throws<OutputLimitReachedException>(() => stream.Write(new byte[8], 0, 8));
        Assert.Equal(10, stream.Length);
        Assert.True(stream.LimitReached);

        stream.Write(new byte[3], 0, 3); // later flushes are ignored, not refused again
        Assert.Equal(10, stream.Length);
    }

    private sealed class Node
    {
        public string Name { get; set; } = string.Empty;

        public Node? Parent { get; set; }
    }

    private sealed class Throws
    {
        public string Boom => throw new InvalidOperationException("no");
    }
}
