namespace HPGrasshopper.McpBridge.Execution;

/// <summary>
///     Encapsulates the transactional outcome of a Revit action execution.
/// </summary>
internal readonly record struct ExecutionOutcome(
    object? Value,
    string? Error,
    bool RolledBack,
    bool TimedOut,
    CanvasRollbackResult? CanvasRollback = null);
