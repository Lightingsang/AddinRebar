namespace HPGrasshopper.McpBridge.Tests.Regression;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using Xunit;

/// <summary>
///     Empirical Challenger tests for Milestone M1 Findings N4 and N5:
///     - N4: Baseline Object Lifecycle Sequences (Remove -> Re-add -> Rollback, Remove -> Add -> Remove -> Rollback, Add Temp -> Remove Temp -> Rollback).
///     - N5: Target Identity and Path Normalization (Unsaved empty path, explicit target mismatch, definition GUID without Revit doc).
/// </summary>
public sealed class Milestone1ChallengerEmpiricalN4N5Tests
{
    // =========================================================================
    // Finding N4: Baseline Object Lifecycle Sequences
    // =========================================================================

    [Fact]
    public void N4_Sequence1_RemoveBaseline_ReAddBaseline_Rollback_PreservesBaselineAndRetainsWires()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var baselineComp = TestProxyFactory.CreateParam("baseline1", "B1");
        var sourceParam = TestProxyFactory.CreateParam("source1", "S1");

        // 1. Initial state: baseline component has wire to sourceParam
        baselineComp.AddSource(sourceParam);
        Assert.Single(baselineComp.Sources);
        Assert.Contains(sourceParam, baselineComp.Sources);

        // Populate baseline set to establish baseline ownership
        var baselineField = typeof(CanvasUndoScope).GetField("_baselineGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        var baselineSet = (HashSet<Guid>)baselineField!.GetValue(scope)!;
        baselineSet.Add(baselineComp.InstanceGuid);

        // 2. Remove baseline component and sever its wire
        scope.TrackRemovedObject(baselineComp);
        scope.TrackDisconnectedWire(baselineComp, sourceParam);
        baselineComp.RemoveSource(sourceParam);
        Assert.Empty(baselineComp.Sources);

        // Verify snapshot captured the severed wire
        var removedField = typeof(CanvasUndoScope).GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        var removedList = (IList)removedField!.GetValue(scope)!;
        Assert.Single(removedList);
        var snapshot = (RemovedComponentSnapshot)removedList[0]!;
        Assert.Equal(baselineComp.InstanceGuid, snapshot.InstanceGuid);
        Assert.Single(snapshot.SeveredWires);

        // 3. Re-add baseline component in the same scope
        scope.TrackAddedObject(baselineComp.InstanceGuid);
        var addedField = typeof(CanvasUndoScope).GetField("_addedGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        var addedList = (List<Guid>)addedField!.GetValue(scope)!;
        Assert.Contains(baselineComp.InstanceGuid, addedList);

        // 4. Rollback
        var rollbackResult = scope.Rollback();
        Assert.True(rollbackResult.Success);
        Assert.True(rollbackResult.CleanlyRolledBack);

        // 5. Verification: Baseline component is protected from deletion by SafeBaselineGuids
        Assert.Contains(baselineComp.InstanceGuid, baselineSet);
        Assert.Contains(baselineComp.InstanceGuid, addedList);

        // Verify wire is retained and restored to baseline component
        Assert.Single(baselineComp.Sources);
        Assert.Contains(sourceParam, baselineComp.Sources);
    }

    [Fact]
    public void N4_Sequence2_RemoveBaseline_Add_Remove_Rollback_RestoresBaselineSnapshot()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var baselineComp = TestProxyFactory.CreateParam("baseline2", "B2");
        var sourceParam = TestProxyFactory.CreateParam("source2", "S2");

        baselineComp.AddSource(sourceParam);

        var baselineField = typeof(CanvasUndoScope).GetField("_baselineGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        var baselineSet = (HashSet<Guid>)baselineField!.GetValue(scope)!;
        baselineSet.Add(baselineComp.InstanceGuid);

        // 1. Remove baseline component
        scope.TrackRemovedObject(baselineComp);
        scope.TrackDisconnectedWire(baselineComp, sourceParam);
        baselineComp.RemoveSource(sourceParam);

        // 2. Re-add baseline component in same transaction
        scope.TrackAddedObject(baselineComp.InstanceGuid);

        // 3. Remove baseline component again in same transaction
        scope.TrackRemovedObject(baselineComp);

        // Verify snapshot from step 1 is preserved and not dropped
        var removedField = typeof(CanvasUndoScope).GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        var removedList = (IList)removedField!.GetValue(scope)!;
        Assert.Single(removedList);

        var snapshot = (RemovedComponentSnapshot)removedList[0]!;
        Assert.Equal(baselineComp.InstanceGuid, snapshot.InstanceGuid);

        // 4. Rollback
        var rollbackResult = scope.Rollback();
        Assert.True(rollbackResult.Success);
        Assert.True(rollbackResult.CleanlyRolledBack);

        // 5. Verification: Baseline component snapshot is restored along with its wire
        Assert.Contains(baselineComp.InstanceGuid, baselineSet);
        Assert.Single(baselineComp.Sources);
        Assert.Contains(sourceParam, baselineComp.Sources);
    }

    [Fact]
    public void N4_Sequence3_AddTemp_RemoveTemp_Rollback_DoesNotResurrectTemporaryComponent()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var tempComp = TestProxyFactory.CreateParam("temporary1", "T1");

        var baselineField = typeof(CanvasUndoScope).GetField("_baselineGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        var baselineSet = (HashSet<Guid>)baselineField!.GetValue(scope)!;
        Assert.DoesNotContain(tempComp.InstanceGuid, baselineSet);

        // 1. Add temporary component
        scope.TrackAddedObject(tempComp.InstanceGuid);
        var addedField = typeof(CanvasUndoScope).GetField("_addedGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        var addedList = (List<Guid>)addedField!.GetValue(scope)!;
        Assert.Contains(tempComp.InstanceGuid, addedList);

        // 2. Remove temporary component in same transaction
        scope.TrackRemovedObject(tempComp);

        // 3. Verify temporary component removal does NOT create snapshot for resurrection
        var removedField = typeof(CanvasUndoScope).GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        var removedList = (IList)removedField!.GetValue(scope)!;
        Assert.Empty(removedList);
        Assert.DoesNotContain(tempComp.InstanceGuid, baselineSet);

        // 4. Rollback
        var rollbackResult = scope.Rollback();
        Assert.True(rollbackResult.Success);
        Assert.True(rollbackResult.CleanlyRolledBack);

        // 5. Verification: Zero snapshots exist to resurrect temporary component
        Assert.Empty(removedList);
    }

    // =========================================================================
    // Finding N5: Target Identity and Path Normalization
    // =========================================================================

    [Fact]
    public void N5_Test1_UnsavedDocument_EmptyOrWhitespacePath_DoesNotThrowPathMismatch()
    {
        // 1. Empty string TargetDocPath normalized to null
        var requestEmpty = new McpBridgeWorkRequest(
            "UnsavedEmpty",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: "Project1",
            TargetDocPath: string.Empty);

        Assert.Null(requestEmpty.TargetDocPath);
        Assert.Equal("Project1", requestEmpty.TargetDocTitle);

        // 2. Whitespace TargetDocPath normalized to null
        var requestWhitespace = new McpBridgeWorkRequest(
            "UnsavedWhitespace",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: "Project1",
            TargetDocPath: "   \t\r\n   ");

        Assert.Null(requestWhitespace.TargetDocPath);
        Assert.Equal("Project1", requestWhitespace.TargetDocTitle);

        // 3. Coordinator snapshot normalization for unsaved document
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        try
        {
            coordinator.UpdateActiveSnapshot("Project1", "   ", null, null, null);
            Assert.Equal("Project1", coordinator.ActiveDocumentTitle);
            Assert.Null(coordinator.ActiveDocumentPath);
        }
        finally
        {
            coordinator.UpdateActiveSnapshot(null, null, null);
        }
    }

    [Fact]
    public void N5_Test2_ExplicitTargetDocumentMismatch_FailsClosedWithDescriptiveError()
    {
        // 1. Explicit title mismatch when no Revit document is open
        var requestTitle = new McpBridgeWorkRequest(
            "TitleCheck",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: "ExpectedModel.rvt");

        var exTitle = Assert.Throws<InvalidOperationException>(() => requestTitle.ValidateActiveDocument(null));
        Assert.Contains("Document identity mismatch", exTitle.Message);
        Assert.Contains("expected 'ExpectedModel.rvt'", exTitle.Message);
        Assert.Contains("no document is open in Revit", exTitle.Message);

        // 2. Explicit path mismatch when no Revit document is open
        var requestPath = new McpBridgeWorkRequest(
            "PathCheck",
            (_, _) => null,
            CancellationToken.None,
            TargetDocPath: @"C:\Projects\ExpectedModel.rvt");

        var exPath = Assert.Throws<InvalidOperationException>(() => requestPath.ValidateActiveDocument(null));
        Assert.Contains("Document identity mismatch", exPath.Message);
        Assert.Contains(@"C:\Projects\ExpectedModel.rvt", exPath.Message);
        Assert.Contains("no document is open in Revit", exPath.Message);

        // 3. RequireTargetIdentity fail-closed rejection when no target specified
        var requestRequire = new McpBridgeWorkRequest(
            "MutatingOp",
            (_, _) => null,
            CancellationToken.None,
            RequireTargetIdentity: true);

        var exRequire = Assert.Throws<InvalidOperationException>(() => requestRequire.ValidateActiveDocument(null));
        Assert.Contains("Write operation 'MutatingOp' rejected", exRequire.Message);
        Assert.Contains("Target document or definition identity must be specified", exRequire.Message);
    }

    [Fact]
    public void N5_Test3_TargetDefIdSpecified_WhenRevitDocNull_ValidatesWithoutDemandingRevitDoc()
    {
        var targetDefId = Guid.NewGuid();

        // Work request specifying ONLY TargetDefId (no TargetDocTitle or TargetDocPath)
        var request = new McpBridgeWorkRequest(
            "GrasshopperOnlyOp",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: null,
            TargetDocPath: null,
            TargetDefId: targetDefId);

        // When app is null, it evaluates Grasshopper definition identity instead of throwing Revit doc error
        var ex = Assert.Throws<InvalidOperationException>(() => request.ValidateActiveDocument(null));

        // Verifies it throws GH definition mismatch, NOT Revit document mismatch
        Assert.Contains("Grasshopper definition identity mismatch", ex.Message);
        Assert.Contains(targetDefId.ToString(), ex.Message);
        Assert.DoesNotContain("no document is open in Revit", ex.Message);
    }
}
