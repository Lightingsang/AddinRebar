namespace HPBim.Client.Pipeline;

using System;
using System.Collections.Generic;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Outcome of an individual BIM pipeline stage execution.
/// </summary>
public sealed record StageResult
{
    public int StageIndex { get; init; }
    public string StageName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public long DurationMs { get; init; }
    public ChangedCounts Changed { get; init; } = new(0, 0, 0);
    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();
    public bool RolledBack { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static StageResult Failure(int index, string name, string message, long durationMs = 0) =>
        new()
        {
            StageIndex = index,
            StageName = name,
            Success = false,
            ErrorMessage = message,
            DurationMs = durationMs
        };
}
