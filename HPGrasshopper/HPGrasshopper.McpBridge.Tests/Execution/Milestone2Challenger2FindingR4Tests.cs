namespace HPGrasshopper.McpBridge.Tests.Execution;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using Xunit;

/// <summary>
///     Adversarial empirical tests for Finding R4: Canvas Rollback Ledger,
///     temporary component resurrection suppression, wire pre-mutation topology undo,
///     and truthful rollback failure reporting.
/// </summary>
public sealed class Milestone2Challenger2FindingR4Tests
{
    private class FaultyParamProxy : System.Reflection.DispatchProxy
    {
        public Guid InstanceGuid { get; set; } = Guid.NewGuid();
        public bool ThrowOnRemoveSource { get; set; }
        public bool ThrowOnAddSource { get; set; }
        public List<IGH_Param> Sources { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null) return null;
            if (targetMethod.Name == "RemoveSource" && ThrowOnRemoveSource)
                throw new InvalidOperationException("Simulated wire disconnect exception");
            if (targetMethod.Name == "AddSource" && ThrowOnAddSource)
                throw new InvalidOperationException("Simulated wire connect exception");
            if (targetMethod.Name == "get_InstanceGuid") return InstanceGuid;
            if (targetMethod.Name == "get_Sources") return Sources;
            if (targetMethod.Name == "get_Recipients") return new List<IGH_Param>();
            return null;
        }

        public static IGH_Param Create(bool throwOnRemove, bool throwOnAdd)
        {
            var param = System.Reflection.DispatchProxy.Create<IGH_Param, FaultyParamProxy>();
            var proxy = (FaultyParamProxy)(object)param;
            proxy.ThrowOnRemoveSource = throwOnRemove;
            proxy.ThrowOnAddSource = throwOnAdd;
            return param;
        }
    }

    [Fact]
    public void RollbackLedger_AddThenRemoveInSameScope_DoesNotResurrectTemporaryComponent()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var baselineComp = TestProxyFactory.CreateParam("baseline1", "B1");
        var tempCompA = TestProxyFactory.CreateParam("tempA", "TA");
        var tempCompB = TestProxyFactory.CreateParam("tempB", "TB");

        scope.TrackAddedObject(tempCompA.InstanceGuid);
        scope.TrackAddedObject(tempCompB.InstanceGuid);

        scope.TrackRemovedObject(tempCompA);
        scope.TrackRemovedObject(baselineComp);

        var removedField = typeof(CanvasUndoScope).GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        var removedList = (IList?)removedField!.GetValue(scope);
        Assert.NotNull(removedList);
        Assert.Single(removedList);

        var snapshot = (RemovedComponentSnapshot)removedList[0]!;
        Assert.Equal(baselineComp.InstanceGuid, snapshot.InstanceGuid);

        var result = scope.Rollback();
        Assert.True(result.Success);
        Assert.True(result.CleanlyRolledBack);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void RollbackLedger_MultipleInScopeAddAndRemove_SuppressesAllTemporarySnapshots()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var temp1 = TestProxyFactory.CreateParam("temp1", "T1");
        var temp2 = TestProxyFactory.CreateParam("temp2", "T2");
        var temp3 = TestProxyFactory.CreateParam("temp3", "T3");

        scope.TrackAddedObject(temp1.InstanceGuid);
        scope.TrackAddedObject(temp2.InstanceGuid);
        scope.TrackAddedObject(temp3.InstanceGuid);

        scope.TrackRemovedObject(temp1);
        scope.TrackRemovedObject(temp2);
        scope.TrackRemovedObject(temp3);

        var removedField = typeof(CanvasUndoScope).GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        var removedList = (IList?)removedField!.GetValue(scope);
        Assert.NotNull(removedList);
        Assert.Empty(removedList);

        var result = scope.Rollback();
        Assert.True(result.Success);
        Assert.True(result.CleanlyRolledBack);
    }

    [Fact]
    public void RollbackWires_RestoresExactPreMutationWireTopology()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var target = TestProxyFactory.CreateParam("target", "Tgt");
        var initialSource1 = TestProxyFactory.CreateParam("source1", "S1");
        var initialSource2 = TestProxyFactory.CreateParam("source2", "S2");
        var newSource3 = TestProxyFactory.CreateParam("source3", "S3");

        target.AddSource(initialSource1);
        target.AddSource(initialSource2);
        Assert.Equal(2, target.Sources.Count);

        scope.TrackConnectedWire(target, newSource3);
        target.AddSource(newSource3);

        scope.TrackDisconnectedWire(target, initialSource1);
        target.RemoveSource(initialSource1);

        Assert.Equal(2, target.Sources.Count);
        Assert.Contains(initialSource2, target.Sources);
        Assert.Contains(newSource3, target.Sources);
        Assert.DoesNotContain(initialSource1, target.Sources);

        var result = scope.Rollback();
        Assert.True(result.Success);
        Assert.True(result.CleanlyRolledBack);

        Assert.Equal(2, target.Sources.Count);
        Assert.Contains(initialSource1, target.Sources);
        Assert.Contains(initialSource2, target.Sources);
        Assert.DoesNotContain(newSource3, target.Sources);
    }

    [Fact]
    public void RollbackWires_WhenWireMutationsFail_CollectsErrorsTruthfullyWithoutSwallowing()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var faultyDisconnectTarget = FaultyParamProxy.Create(throwOnRemove: true, throwOnAdd: false);
        var faultyConnectTarget = FaultyParamProxy.Create(throwOnRemove: false, throwOnAdd: true);
        var sourceA = TestProxyFactory.CreateParam("srcA", "SA");
        var sourceB = TestProxyFactory.CreateParam("srcB", "SB");

        scope.TrackConnectedWire(faultyDisconnectTarget, sourceA);
        scope.TrackDisconnectedWire(faultyConnectTarget, sourceB);

        var result = scope.Rollback();

        Assert.False(result.Success);
        Assert.False(result.CleanlyRolledBack);
        Assert.False(scope.RolledBackCleanly);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Disconnect wire failed", result.Errors[0]);
        Assert.Contains("Simulated wire disconnect exception", result.Errors[0]);
        Assert.Contains("Restore wire failed", result.Errors[1]);
        Assert.Contains("Simulated wire connect exception", result.Errors[1]);
    }

    [Fact]
    public void Rollback_IdempotentCallAfterFailure_PreservesFailureStatusAndErrors()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var faultyTarget = FaultyParamProxy.Create(throwOnRemove: true, throwOnAdd: false);
        var source = TestProxyFactory.CreateParam("src", "S");

        scope.TrackConnectedWire(faultyTarget, source);

        var firstResult = scope.Rollback();
        Assert.False(firstResult.Success);
        Assert.Single(firstResult.Errors);

        var secondResult = scope.Rollback();
        Assert.False(secondResult.Success);
        Assert.False(secondResult.CleanlyRolledBack);
        Assert.Single(secondResult.Errors);
        Assert.Equal(firstResult.Errors[0], secondResult.Errors[0]);
    }

    [Fact]
    public void Rollback_WhenAlreadyCommitted_ReturnsCleanWithoutPerformingRollback()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var target = TestProxyFactory.CreateParam("tgt", "T");
        var source = TestProxyFactory.CreateParam("src", "S");

        scope.TrackConnectedWire(target, source);
        target.AddSource(source);

        scope.Commit();
        Assert.True(scope.IsCommitted);

        var result = scope.Rollback();
        Assert.True(result.Success);
        Assert.True(result.CleanlyRolledBack);
        Assert.Contains(source, target.Sources);
    }

    [Fact]
    public void Dispose_WithoutCommit_TriggersRollbackAndEnforcesCleanup()
    {
        var scope = TestScopeFactory.CreateHeadlessScope();
        var target = TestProxyFactory.CreateParam("tgt", "T");
        var source = TestProxyFactory.CreateParam("src", "S");

        scope.TrackConnectedWire(target, source);
        target.AddSource(source);
        Assert.Contains(source, target.Sources);

        scope.Dispose();

        Assert.DoesNotContain(source, target.Sources);
        Assert.True(scope.RolledBackCleanly);
    }
}
