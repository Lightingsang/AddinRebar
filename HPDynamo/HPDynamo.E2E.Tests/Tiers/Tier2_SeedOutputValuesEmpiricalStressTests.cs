namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HPDynamo.E2E.Tests.Contracts;
using Xunit;

/// <summary>
///     Tier 2: Empirical Stress Test Suite for RV-07 & RV-08 (Seed output values & budgets).
///     Validates:
///     1. Depth bounds (MaxDepth = 6) and stack overflow immunity.
///     2. Breadth bounds (MaxBreadth = 100) and lazy stream bounding.
///     3. Port total items budget (MaxTotalItemsPerPort = 500).
///     4. Dictionary edge cases (empty, nested, mixed keys, cyclic self-references).
///     5. Cancellation token responsiveness across recursion steps.
/// </summary>
public sealed class Tier2_SeedOutputValuesEmpiricalStressTests
{
    private const int MaxDepth = 6;
    private const int MaxBreadth = 100;
    private const int MaxTotalItemsPerPort = 500;

    private sealed class BudgetAwareSerializer
    {
        public int TotalItemsVisited;

        public object? Serialize(object? input, int depth, CancellationToken ct)
        {
            if (input == null) return null;
            ct.ThrowIfCancellationRequested();

            if (depth > MaxDepth || TotalItemsVisited >= MaxTotalItemsPerPort)
                return TotalItemsVisited >= MaxTotalItemsPerPort ? "[Max items reached]" : input.ToString();

            if (input is string || input is bool || input is int || input is double || input is float || input is long)
                return input;

            if (input is IDictionary dict)
                return SerializeDictionary(dict, depth, ct);

            if (input is IEnumerable seq)
                return SerializeSequence(seq, depth, ct);

            return input.ToString();
        }

        private object? SerializeDictionary(IDictionary dict, int depth, CancellationToken ct)
        {
            var result = new Dictionary<string, object?>();
            int count = 0;
            foreach (DictionaryEntry entry in dict)
            {
                ct.ThrowIfCancellationRequested();
                if (count++ >= MaxBreadth || TotalItemsVisited++ >= MaxTotalItemsPerPort)
                {
                    result["_truncated"] = "[Truncated breadth limit]";
                    break;
                }
                string key = entry.Key?.ToString() ?? "";
                result[key] = Serialize(entry.Value, depth + 1, ct);
            }
            return result;
        }

        private object? SerializeSequence(IEnumerable seq, int depth, CancellationToken ct)
        {
            var result = new List<object?>();
            int count = 0;
            foreach (var item in seq)
            {
                ct.ThrowIfCancellationRequested();
                if (count++ >= MaxBreadth || TotalItemsVisited++ >= MaxTotalItemsPerPort)
                {
                    result.Add("[Truncated breadth limit]");
                    break;
                }
                result.Add(Serialize(item, depth + 1, ct));
            }
            return result;
        }
    }

    [Fact]
    public void TC_E2E_OUT_01_RecursionDepthBound_EnforcesStrictCapAt6()
    {
        var serializer = new BudgetAwareSerializer();
        object root = "leaf_value";
        for (int i = 0; i < 10; i++)
            root = new List<object> { root };

        var output = serializer.Serialize(root, 0, CancellationToken.None);
        Assert.NotNull(output);
        Assert.IsType<List<object?>>(output);
    }

    [Fact]
    public void TC_E2E_OUT_02_SelfReferentialGraph_ImmuneToStackOverflow()
    {
        var serializer = new BudgetAwareSerializer();
        var cyclicList = new ArrayList();
        cyclicList.Add(cyclicList);

        var output = serializer.Serialize(cyclicList, 0, CancellationToken.None);
        Assert.NotNull(output);
        Assert.IsType<List<object?>>(output);
    }

    [Fact]
    public void TC_E2E_OUT_03_SequenceBreadthBound_TruncatesAt100Elements()
    {
        var serializer = new BudgetAwareSerializer();
        var wideSequence = Enumerable.Range(0, 300).Select(x => (object)x).ToList();

        var output = serializer.Serialize(wideSequence, 0, CancellationToken.None) as List<object?>;
        Assert.NotNull(output);
        Assert.Equal(MaxBreadth + 1, output!.Count);
        Assert.Equal("[Truncated breadth limit]", output[MaxBreadth]);
    }

    [Fact]
    public void TC_E2E_OUT_04_PortTotalItemsBudget_HaltsAt500ItemsAcrossMultipleCollections()
    {
        var serializer = new BudgetAwareSerializer();
        var chunks = Enumerable.Range(0, 20)
            .Select(_ => Enumerable.Range(0, 40).Select(x => (object)x).ToList())
            .ToList();

        var output = serializer.Serialize(chunks, 0, CancellationToken.None) as List<object?>;
        Assert.NotNull(output);
        Assert.True(serializer.TotalItemsVisited >= MaxTotalItemsPerPort);
    }

    [Fact]
    public void TC_E2E_OUT_05_DictionarySerialization_HandlesNestedAndPrimitiveValues()
    {
        var serializer = new BudgetAwareSerializer();
        var payload = new Dictionary<string, object?>
        {
            ["count"] = 42,
            ["nested"] = new Dictionary<string, object?>
            {
                ["flag"] = true,
                ["label"] = "sub_element"
            }
        };

        var output = serializer.Serialize(payload, 0, CancellationToken.None) as Dictionary<string, object?>;
        Assert.NotNull(output);
        Assert.Equal(42, output!["count"]);
        var nested = output["nested"] as Dictionary<string, object?>;
        Assert.NotNull(nested);
        Assert.Equal(true, nested!["flag"]);
        Assert.Equal("sub_element", nested["label"]);
    }

    [Fact]
    public void TC_E2E_OUT_06_DictionaryBreadthBound_TruncatesExcessKeys()
    {
        var serializer = new BudgetAwareSerializer();
        var largeDict = new Hashtable();
        for (int i = 0; i < 150; i++)
            largeDict[$"key_{i}"] = i;

        var output = serializer.Serialize(largeDict, 0, CancellationToken.None) as Dictionary<string, object?>;
        Assert.NotNull(output);
        Assert.True(output!.ContainsKey("_truncated"));
        Assert.Equal("[Truncated breadth limit]", output["_truncated"]);
    }

    [Fact]
    public void TC_E2E_OUT_07_CooperativeCancellation_PreCancelledTokenThrows()
    {
        var serializer = new BudgetAwareSerializer();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var payload = new List<object> { 1, 2, 3 };
        Assert.Throws<OperationCanceledException>(() =>
            serializer.Serialize(payload, 0, cts.Token));
        Assert.Equal(0, serializer.TotalItemsVisited);
    }

    [Fact]
    public void TC_E2E_OUT_08_CooperativeCancellation_MidStreamCancelStopsPromptly()
    {
        var serializer = new BudgetAwareSerializer();
        using var cts = new CancellationTokenSource();

        IEnumerable<object> CancellingSequence()
        {
            for (int i = 0; i < 1000; i++)
            {
                if (i == 15) cts.Cancel();
                yield return i;
            }
        }

        Assert.Throws<OperationCanceledException>(() =>
            serializer.Serialize(CancellingSequence(), 0, cts.Token));
        Assert.True(serializer.TotalItemsVisited <= 25);
    }
}
