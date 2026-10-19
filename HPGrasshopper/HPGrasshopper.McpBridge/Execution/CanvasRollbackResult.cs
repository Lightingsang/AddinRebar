namespace HPGrasshopper.McpBridge.Execution;

using System;
using System.Collections.Generic;

/// <summary>
///     Result of a Grasshopper canvas rollback operation.
/// </summary>
public sealed record CanvasRollbackResult(
    bool Success,
    IReadOnlyList<string> Errors)
{
    public bool CleanlyRolledBack => Success;

    public static CanvasRollbackResult Clean() =>
        new(true, Array.Empty<string>());

    public static CanvasRollbackResult Failed(IReadOnlyList<string> errors) =>
        new(false, errors ?? Array.Empty<string>());
}
