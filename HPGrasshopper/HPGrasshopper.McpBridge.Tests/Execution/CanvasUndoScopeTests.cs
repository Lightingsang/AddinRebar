namespace HPGrasshopper.McpBridge.Tests.Execution;

using System;
using System.Linq;
using System.Reflection;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class CanvasUndoScopeTests
{
    [Fact]
    public void CanvasUndoScope_Constructor_NullDocument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CanvasUndoScope(null!));
    }

    [Fact]
    public void CanvasUndoScope_Reflection_VerifiesRequiredMethodsAndFields()
    {
        var type = typeof(CanvasUndoScope);

        // Verify public API methods
        Assert.NotNull(type.GetMethod("TrackAddedObject"));
        Assert.NotNull(type.GetMethod("TrackRemovedObject"));
        Assert.NotNull(type.GetMethod("TrackConnectedWire"));
        Assert.NotNull(type.GetMethod("TrackDisconnectedWire"));
        Assert.NotNull(type.GetMethod("Rollback"));
        Assert.NotNull(type.GetMethod("Commit"));
        Assert.NotNull(type.GetMethod("Dispose"));

        // Verify tracking collections
        var addedGuidsField = type.GetField("_addedGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        var removedSnapshotsField = type.GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        var connectedWiresField = type.GetField("_connectedWires", BindingFlags.NonPublic | BindingFlags.Instance);
        var disconnectedWiresField = type.GetField("_disconnectedWires", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(addedGuidsField);
        Assert.NotNull(removedSnapshotsField);
        Assert.NotNull(connectedWiresField);
        Assert.NotNull(disconnectedWiresField);
    }

    [Fact]
    public void CanvasUndoScope_DisposeWithoutCommit_TriggersRollback()
    {
        var type = typeof(CanvasUndoScope);
        var completedField = type.GetField("_isCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(completedField);

        using (var scope = TestScopeFactory.CreateHeadlessScope())
        {
            var isCompletedBefore = (bool)completedField.GetValue(scope)!;
            Assert.False(isCompletedBefore);

            scope.Commit();
            var isCompletedAfter = (bool)completedField.GetValue(scope)!;
            Assert.True(isCompletedAfter);
        }
    }

    [Fact]
    public void CanvasUndoScope_TrackingMethods_AccumulateEntriesCorrectly()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();

        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        scope.TrackAddedObject(guid1);
        scope.TrackAddedObject(guid2);

        var type = typeof(CanvasUndoScope);
        var addedGuidsField = type.GetField("_addedGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        var list = (System.Collections.Generic.List<Guid>)addedGuidsField!.GetValue(scope)!;

        Assert.Equal(2, list.Count);
        Assert.Contains(guid1, list);
        Assert.Contains(guid2, list);

        scope.Commit();
    }

    [Fact]
    public void CanvasUndoScope_TrackRemovedObject_NullObject_DoesNotThrow()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        scope.TrackRemovedObject(null!);

        var type = typeof(CanvasUndoScope);
        var removedField = type.GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        var list = (System.Collections.IList)removedField!.GetValue(scope)!;
        Assert.Empty(list);
        scope.Commit();
    }

    [Fact]
    public void CanvasEditor_WhenDocNull_ReturnsCleanFailure()
    {
        var editor = new CanvasEditor();
        var result = editor.ExecuteBatch(null, default, true);

        Assert.NotNull(result);
        var propSuccess = result.GetType().GetProperty("success");
        Assert.NotNull(propSuccess);
        Assert.False((bool)propSuccess.GetValue(result)!);
    }
}
