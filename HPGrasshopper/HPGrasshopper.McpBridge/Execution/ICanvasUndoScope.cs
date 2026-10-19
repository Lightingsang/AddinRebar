namespace HPGrasshopper.McpBridge.Execution;

using System;

/// <summary>
///     Abstraction for canvas undo management coordinated with Revit transactions.
/// </summary>
public interface ICanvasUndoScope : IDisposable
{
    void Commit();

    CanvasRollbackResult Rollback();

    bool IsCommitted { get; }
}
