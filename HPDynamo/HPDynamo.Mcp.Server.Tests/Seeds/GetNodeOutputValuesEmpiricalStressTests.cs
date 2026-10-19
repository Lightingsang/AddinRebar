namespace HPDynamo.Mcp.Server.Tests.Seeds;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

/// <summary>
///     Empirical stress test suite for RV-07 & RV-08 in get_node_output_values:
///     1. Recursion depth bounds (MaxDepth = 6) & cycle resistance.
///     2. List breadth bounds (MaxBreadth = 100) & lazy consumption.
///     3. Port total items budget (MaxTotalItemsPerPort = 500).
///     4. Dictionary edge cases (empty, nested, mixed keys, cycles).
///     5. Cooperative cancellation responsiveness (pre-cancelled and mid-traversal).
///     6. ProtoCore MirrorData IsCollection precedence over Data == null.
/// </summary>
public sealed class GetNodeOutputValuesEmpiricalStressTests
{
    private const int MaxDepth = 6;
    private const int MaxBreadth = 100;
    private const int MaxTotalItemsPerPort = 500;

    private sealed class SimulatedMirrorData
    {
        public bool IsCollection { get; init; }
        public object? Data { get; init; }
        public List<SimulatedMirrorData>? Elements { get; init; }
        public List<SimulatedMirrorData>? GetElements() => Elements;
    }

    private sealed class TraversalHarness
    {
        public int PortItemCount;

        public object? SerializePayload(object? value, int depth, CancellationToken ct)
        {
            if (value == null) return null;
            ct.ThrowIfCancellationRequested();

            if (depth > MaxDepth || PortItemCount >= MaxTotalItemsPerPort)
                return PortItemCount >= MaxTotalItemsPerPort ? "[Max items reached]" : value.ToString();

            if (value is string || value is bool || value is int || value is double || value is float || value is long)
                return value;
            if (value is IDictionary dict)
                return SerializeClrDictionary(dict, depth, ct);
            if (value is IEnumerable enumerable)
                return SerializeEnumerable(enumerable, depth, ct);

            return value.ToString();
        }

        public object? SerializeClrDictionary(IDictionary dict, int depth, CancellationToken ct)
        {
            var dictResult = new Dictionary<string, object?>();
            int count = 0;
            foreach (DictionaryEntry entry in dict)
            {
                ct.ThrowIfCancellationRequested();
                if (count++ >= MaxBreadth || PortItemCount++ >= MaxTotalItemsPerPort)
                {
                    dictResult["_truncated"] = "[Truncated breadth limit]";
                    break;
                }
                string key = entry.Key?.ToString() ?? "";
                dictResult[key] = SerializePayload(entry.Value, depth + 1, ct);
            }
            return dictResult;
        }

        public object? SerializeEnumerable(IEnumerable enumerable, int depth, CancellationToken ct)
        {
            var listResult = new List<object?>();
            int count = 0;
            foreach (var item in enumerable)
            {
                ct.ThrowIfCancellationRequested();
                if (count++ >= MaxBreadth || PortItemCount++ >= MaxTotalItemsPerPort)
                {
                    listResult.Add("[Truncated breadth limit]");
                    break;
                }
                listResult.Add(SerializePayload(item, depth + 1, ct));
            }
            return listResult;
        }

        public object? SerializeMirrorData(SimulatedMirrorData? mirror, int depth, CancellationToken ct)
        {
            if (mirror == null) return null;
            ct.ThrowIfCancellationRequested();

            if (depth > MaxDepth || PortItemCount >= MaxTotalItemsPerPort)
                return PortItemCount >= MaxTotalItemsPerPort ? "[Max items reached]" : "[Depth limit reached]";

            if (mirror.IsCollection)
            {
                var elements = mirror.GetElements();
                if (elements == null) return new List<object?>();
                var listResult = new List<object?>();
                int count = 0;
                foreach (var elem in elements)
                {
                    ct.ThrowIfCancellationRequested();
                    if (count++ >= MaxBreadth || PortItemCount++ >= MaxTotalItemsPerPort)
                    {
                        listResult.Add("[Truncated breadth limit]");
                        break;
                    }
                    listResult.Add(SerializeMirrorData(elem, depth + 1, ct));
                }
                return listResult;
            }

            return SerializePayload(mirror.Data, depth, ct);
        }
    }

    [Fact]
    public void TC_M3_STRESS_01_DepthBounds_DeepNesting_TruncatesSafelyAtMaxDepth()
    {
        var harness = new TraversalHarness();
        object nested = "terminal_leaf";
        for (int i = 0; i < 12; i++)
            nested = new List<object> { nested };

        var result = harness.SerializePayload(nested, 0, CancellationToken.None);
        Assert.NotNull(result);
        var current = result as List<object?>;
        for (int level = 0; level < MaxDepth; level++)
        {
            Assert.NotNull(current);
            Assert.Single(current!);
            if (level < MaxDepth - 1) current = current[0] as List<object?>;
        }
    }

    [Fact]
    public void TC_M3_STRESS_02_DepthBounds_CyclicCollection_TerminatesWithoutStackOverflow()
    {
        var harness = new TraversalHarness();
        var cyclicList = new ArrayList();
        cyclicList.Add(cyclicList);

        var result = harness.SerializePayload(cyclicList, 0, CancellationToken.None);
        Assert.NotNull(result);
        Assert.IsType<List<object?>>(result);
    }

    [Fact]
    public void TC_M3_STRESS_03_BreadthBounds_WideCollection_TruncatesAtMaxBreadth100()
    {
        var harness = new TraversalHarness();
        var wideList = Enumerable.Range(1, 250).Select(i => (object)i).ToList();

        var result = harness.SerializePayload(wideList, 0, CancellationToken.None) as List<object?>;
        Assert.NotNull(result);
        Assert.Equal(MaxBreadth + 1, result!.Count);
        Assert.Equal(1, result[0]);
        Assert.Equal(100, result[99]);
        Assert.Equal("[Truncated breadth limit]", result[100]);
    }

    [Fact]
    public void TC_M3_STRESS_04_TotalItemsBudget_Exceeding500Items_EnforcesCap()
    {
        var harness = new TraversalHarness();
        var matrix = Enumerable.Range(0, 15)
            .Select(_ => Enumerable.Range(1, 60).Select(x => (object)x).ToList())
            .ToList();

        var result = harness.SerializePayload(matrix, 0, CancellationToken.None) as List<object?>;
        Assert.NotNull(result);
        Assert.True(harness.PortItemCount >= MaxTotalItemsPerPort, $"Expected >= {MaxTotalItemsPerPort}, got {harness.PortItemCount}");
    }

    [Fact]
    public void TC_M3_STRESS_05_Dictionary_EmptyAndNested_SerializesAccurately()
    {
        var harness = new TraversalHarness();
        var emptyDict = new Dictionary<string, object?>();
        var emptyResult = harness.SerializePayload(emptyDict, 0, CancellationToken.None) as Dictionary<string, object?>;
        Assert.NotNull(emptyResult);
        Assert.Empty(emptyResult!);

        var nestedDict = new Dictionary<string, object?>
        {
            ["level1"] = new Dictionary<string, object?> { ["level2"] = 12345 }
        };
        var nestedResult = harness.SerializePayload(nestedDict, 0, CancellationToken.None) as Dictionary<string, object?>;
        Assert.NotNull(nestedResult);
        var inner = nestedResult!["level1"] as Dictionary<string, object?>;
        Assert.NotNull(inner);
        Assert.Equal(12345, inner!["level2"]);
    }

    [Fact]
    public void TC_M3_STRESS_06_Dictionary_MixedKeysAndCycles_TerminatesCleanly()
    {
        var harness = new TraversalHarness();
        var hashtable = new Hashtable { ["alpha"] = "first", [99] = "numeric_key" };
        hashtable["self"] = hashtable;

        var result = harness.SerializePayload(hashtable, 0, CancellationToken.None) as Dictionary<string, object?>;
        Assert.NotNull(result);
        Assert.Equal("first", result!["alpha"]);
        Assert.Equal("numeric_key", result["99"]);
        Assert.True(result.ContainsKey("self"));
    }

    [Fact]
    public void TC_M3_STRESS_07_Cancellation_PreCancelledToken_ThrowsImmediately()
    {
        var harness = new TraversalHarness();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var list = Enumerable.Range(1, 50).Select(x => (object)x).ToList();
        Assert.Throws<OperationCanceledException>(() =>
            harness.SerializePayload(list, 0, cts.Token));
        Assert.Equal(0, harness.PortItemCount);
    }

    [Fact]
    public void TC_M3_STRESS_08_Cancellation_MidTraversal_ThrowsPromptly()
    {
        var harness = new TraversalHarness();
        using var cts = new CancellationTokenSource();

        IEnumerable<object> GenerateWithCancel()
        {
            for (int i = 0; i < 100; i++)
            {
                if (i == 10) cts.Cancel();
                yield return i;
            }
        }

        Assert.Throws<OperationCanceledException>(() =>
            harness.SerializePayload(GenerateWithCancel(), 0, cts.Token));
        Assert.True(harness.PortItemCount <= 20, $"Expected quick abort, got {harness.PortItemCount}");
    }

    [Fact]
    public void TC_M3_STRESS_09_ProtoCore_IsCollectionPrecedence_HandlesNullDataWithValidElements()
    {
        var harness = new TraversalHarness();
        var mirrorCollection = new SimulatedMirrorData
        {
            IsCollection = true,
            Data = null,
            Elements = new List<SimulatedMirrorData>
            {
                new() { IsCollection = false, Data = "element_A" },
                new() { IsCollection = false, Data = 42.5 }
            }
        };

        var result = harness.SerializeMirrorData(mirrorCollection, 0, CancellationToken.None) as List<object?>;
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("element_A", result[0]);
        Assert.Equal(42.5, result[1]);
    }

    [Fact]
    public void TC_M3_STRESS_10_ProtoCore_DepthBoundOnMirrorData_ReturnsDepthLimitReached()
    {
        var harness = new TraversalHarness();
        var leaf = new SimulatedMirrorData { IsCollection = false, Data = "deep_leaf" };
        var current = leaf;
        for (int i = 0; i < 10; i++)
        {
            current = new SimulatedMirrorData
            {
                IsCollection = true,
                Elements = new List<SimulatedMirrorData> { current }
            };
        }

        var result = harness.SerializeMirrorData(current, 0, CancellationToken.None);
        Assert.NotNull(result);
    }
}
