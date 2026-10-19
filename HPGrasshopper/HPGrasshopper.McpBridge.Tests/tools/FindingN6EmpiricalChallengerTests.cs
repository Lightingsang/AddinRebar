namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class FindingN6EmpiricalChallengerTests
{
    [Fact]
    public void N6_TargetRefusingConversion_AddVolatileDataFalseCaptured_InputsNotIncremented()
    {
        var param = TestProxyFactory.CreateParam("TargetParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.AddVolatileDataReturnValue = false;

        using var json = JsonDocument.Parse("42.0");
        var applied = PlayerDataTreeHelper.SetParamValue(param, json.RootElement, out var errorReason);

        Assert.False(applied);
        Assert.NotNull(errorReason);
        Assert.Contains("refused", errorReason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(proxy.VolatileDataAdded);
    }

    [Fact]
    public void N6_TargetRefusingConversion_Array_CapturesFalseAndStructuredError()
    {
        var param = TestProxyFactory.CreateParam("ListParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.AddVolatileDataReturnValue = false;

        using var json = JsonDocument.Parse("[10.0, 20.0, 30.0]");
        var applied = PlayerDataTreeHelper.SetParamValue(param, json.RootElement, out var errorReason);

        Assert.False(applied);
        Assert.NotNull(errorReason);
        Assert.Contains("refused", errorReason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(proxy.VolatileDataAdded);
    }

    [Fact]
    public void N6_CommitStagedInputs_WhenAddVolatileDataReturnsFalse_AppliedCountZeroAndRecordsFailure()
    {
        var param = TestProxyFactory.CreateParam("RefusingParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.AddVolatileDataReturnValue = false;

        using var json = JsonDocument.Parse("99.9");
        var stageOk = PlayerDataTreeHelper.TryStageParamValue(param, json.RootElement, out var stagedItems, out var stageErr);
        Assert.True(stageOk);
        Assert.Null(stageErr);
        Assert.NotNull(stagedItems);

        var (appliedCount, rejectedInputs) = InvokeCommitStagedInputs(new[]
        {
            (param, "RefusingParam", (IReadOnlyList<StagedItem>)stagedItems!)
        });

        Assert.Equal(0, appliedCount);
        Assert.Single(rejectedInputs);
        Assert.Equal("RefusingParam", rejectedInputs[0].Name);
        Assert.Contains("refused", rejectedInputs[0].Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(proxy.VolatileDataAdded);
    }

    [Fact]
    public void N6_CommitStagedInputs_WhenSuccessful_AppliedCountIncrements()
    {
        var param = TestProxyFactory.CreateParam("AcceptingParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.AddVolatileDataReturnValue = true;

        using var json = JsonDocument.Parse("123.45");
        var stageOk = PlayerDataTreeHelper.TryStageParamValue(param, json.RootElement, out var stagedItems, out var stageErr);
        Assert.True(stageOk);
        Assert.NotNull(stagedItems);

        var (appliedCount, rejectedInputs) = InvokeCommitStagedInputs(new[]
        {
            (param, "AcceptingParam", (IReadOnlyList<StagedItem>)stagedItems!)
        });

        Assert.Equal(1, appliedCount);
        Assert.Empty(rejectedInputs);
        Assert.Single(proxy.VolatileDataAdded);
        Assert.Equal(123.45, Assert.IsAssignableFrom<GH_Number>(proxy.VolatileDataAdded[0]).Value);
    }

    [Fact]
    public void N6_TreePayload_MalformedBranchPath_RejectedAtomically_NoPartialSilentSuccess()
    {
        var param = TestProxyFactory.CreateParam("TreeParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);

        using var json = JsonDocument.Parse("""
        {
            "{0}": [1],
            "{a}": [2]
        }
        """);

        var stageSuccess = PlayerDataTreeHelper.TryStageParamValue(param, json.RootElement, out var staged, out var stageError);
        Assert.False(stageSuccess);
        Assert.Null(staged);
        Assert.NotNull(stageError);
        Assert.Contains("{a}", stageError);

        var applySuccess = PlayerDataTreeHelper.SetParamValue(param, json.RootElement, out var applyError);
        Assert.False(applySuccess);
        Assert.NotNull(applyError);
        Assert.Contains("{a}", applyError);
        Assert.Empty(proxy.VolatileDataAdded);
    }

    [Fact]
    public void N6_TreePayload_MalformedPathAtEnd_RejectsEntireTree()
    {
        var param = TestProxyFactory.CreateParam("MultiBranchTree");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);

        using var json = JsonDocument.Parse("""
        {
            "{0}": [10, 20],
            "{1;0}": [30],
            "{bad_branch}": [40]
        }
        """);

        var stageSuccess = PlayerDataTreeHelper.TryStageParamValue(param, json.RootElement, out var staged, out var stageError);
        Assert.False(stageSuccess);
        Assert.Null(staged);
        Assert.NotNull(stageError);
        Assert.Contains("{bad_branch}", stageError);
        Assert.Empty(proxy.VolatileDataAdded);
    }

    [Fact]
    public void N6_NullItemsInArray_IndexShiftingPrevented_ExplicitlyRejected()
    {
        var param = TestProxyFactory.CreateParam("ArrayParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);

        using var json = JsonDocument.Parse("[10, null, 20]");
        var stageSuccess = PlayerDataTreeHelper.TryStageParamValue(param, json.RootElement, out var staged, out var stageError);
        Assert.False(stageSuccess);
        Assert.Null(staged);
        Assert.NotNull(stageError);
        Assert.Contains("index 1 is null", stageError, StringComparison.OrdinalIgnoreCase);

        var applySuccess = PlayerDataTreeHelper.SetParamValue(param, json.RootElement, out var applyError);
        Assert.False(applySuccess);
        Assert.NotNull(applyError);
        Assert.Contains("null", applyError, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(proxy.VolatileDataAdded);
    }

    [Fact]
    public void N6_LeadingNullItemInArray_Index0RejectedExplicitly()
    {
        var param = TestProxyFactory.CreateParam("LeadingNullParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);

        using var json = JsonDocument.Parse("[null, 100]");
        var stageSuccess = PlayerDataTreeHelper.TryStageParamValue(param, json.RootElement, out var staged, out var stageError);
        Assert.False(stageSuccess);
        Assert.Null(staged);
        Assert.NotNull(stageError);
        Assert.Contains("index 0 is null", stageError, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(proxy.VolatileDataAdded);
    }

    [Fact]
    public void N6_MultiInputStaging_WhenParam2Fails_Param1IsNotMutated()
    {
        var param1 = TestProxyFactory.CreateParam("Param1");
        var param2 = TestProxyFactory.CreateParam("Param2");
        var proxy1 = TestProxyFactory.GetProxy<TestParamProxy>(param1);
        var proxy2 = TestProxyFactory.GetProxy<TestParamProxy>(param2);

        using var json1 = JsonDocument.Parse("42.0");
        using var json2 = JsonDocument.Parse("""
        {
            "{0}": [1],
            "{a}": [2]
        }
        """);

        var stage1Ok = PlayerDataTreeHelper.TryStageParamValue(param1, json1.RootElement, out var staged1, out var err1);
        Assert.True(stage1Ok);
        Assert.Null(err1);
        Assert.NotNull(staged1);
        Assert.Empty(proxy1.VolatileDataAdded);

        var stage2Ok = PlayerDataTreeHelper.TryStageParamValue(param2, json2.RootElement, out var staged2, out var err2);
        Assert.False(stage2Ok);
        Assert.NotNull(err2);
        Assert.Null(staged2);

        var stagedBatch = new List<(IGH_Param, string, IReadOnlyList<StagedItem>)>();
        var rejectedInputs = new List<PlayerInputFailure>();

        if (stage1Ok) stagedBatch.Add((param1, "Param1", staged1!));
        else rejectedInputs.Add(new PlayerInputFailure("Param1", err1!));

        if (stage2Ok) stagedBatch.Add((param2, "Param2", staged2!));
        else rejectedInputs.Add(new PlayerInputFailure("Param2", err2!));

        Assert.Single(rejectedInputs);
        Assert.Equal("Param2", rejectedInputs[0].Name);

        if (rejectedInputs.Count == 0)
        {
            InvokeCommitStagedInputs(stagedBatch);
        }

        Assert.Empty(proxy1.VolatileDataAdded);
        Assert.Empty(proxy2.VolatileDataAdded);
    }

    [Fact]
    public void N6_MultiInputStaging_WhenParam2HasNullInArray_Param1IsNotMutated()
    {
        var paramA = TestProxyFactory.CreateParam("ParamA");
        var paramB = TestProxyFactory.CreateParam("ParamB");
        var proxyA = TestProxyFactory.GetProxy<TestParamProxy>(paramA);
        var proxyB = TestProxyFactory.GetProxy<TestParamProxy>(paramB);

        using var jsonA = JsonDocument.Parse("\"ValidString\"");
        using var jsonB = JsonDocument.Parse("[10, null, 30]");

        var stageA = PlayerDataTreeHelper.TryStageParamValue(paramA, jsonA.RootElement, out var stagedA, out var errA);
        Assert.True(stageA);
        Assert.Empty(proxyA.VolatileDataAdded);

        var stageB = PlayerDataTreeHelper.TryStageParamValue(paramB, jsonB.RootElement, out var stagedB, out var errB);
        Assert.False(stageB);
        Assert.Contains("null", errB, StringComparison.OrdinalIgnoreCase);

        var rejected = new List<PlayerInputFailure>();
        if (!stageA) rejected.Add(new PlayerInputFailure("ParamA", errA!));
        if (!stageB) rejected.Add(new PlayerInputFailure("ParamB", errB!));

        Assert.Single(rejected);
        Assert.Equal("ParamB", rejected[0].Name);

        Assert.Empty(proxyA.VolatileDataAdded);
        Assert.Empty(proxyB.VolatileDataAdded);
    }

    private static (int appliedCount, List<PlayerInputFailure> rejectedInputs) InvokeCommitStagedInputs(
        IEnumerable<(IGH_Param Param, string Name, IReadOnlyList<StagedItem> Items)> staged)
    {
        var stagedList = staged.ToList();
        var method = typeof(PlayerRunner).GetMethod("CommitStagedInputs", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var result = method!.Invoke(null, new object?[] { stagedList });
        Assert.NotNull(result);

        var tupleType = result!.GetType();
        var item1Field = tupleType.GetField("Item1") ?? tupleType.GetField("appliedCount");
        var item2Field = tupleType.GetField("Item2") ?? tupleType.GetField("rejectedInputs");

        var count = (int)item1Field!.GetValue(result)!;
        var rejected = (List<PlayerInputFailure>)item2Field!.GetValue(result)!;
        return (count, rejected);
    }
}
