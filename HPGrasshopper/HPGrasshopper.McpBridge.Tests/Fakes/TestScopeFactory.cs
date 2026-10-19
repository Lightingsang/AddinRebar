namespace HPGrasshopper.McpBridge.Tests.Fakes;

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HPGrasshopper.McpBridge.Execution;

/// <summary>
///     Headless test factory creating CanvasUndoScope instances
///     without triggering native GH_Document initialization or rhcommon_c.dll loading.
/// </summary>
internal static class TestScopeFactory
{
    public static CanvasUndoScope CreateHeadlessScope(bool isCompleted = false)
    {
        var scope = (CanvasUndoScope)RuntimeHelpers.GetUninitializedObject(typeof(CanvasUndoScope));
        InitializeListField(scope, "_addedGuids");
        InitializeListField(scope, "_removedSnapshots");
        InitializeListField(scope, "_disconnectedWires");
        InitializeListField(scope, "_connectedWires");
        InitializeListField(scope, "_baselineGuids");

        var recordField = typeof(CanvasUndoScope).GetField("_undoRecord", BindingFlags.NonPublic | BindingFlags.Instance);
        recordField?.SetValue(scope, new Grasshopper.Kernel.Undo.GH_UndoRecord("TestUndoRecord"));

        var completedField = typeof(CanvasUndoScope).GetField("_isCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
        completedField?.SetValue(scope, isCompleted);
        return scope;
    }

    private static void InitializeListField(object target, string name)
    {
        var field = typeof(CanvasUndoScope).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            var list = Activator.CreateInstance(field.FieldType);
            field.SetValue(target, list);
        }
    }
}
