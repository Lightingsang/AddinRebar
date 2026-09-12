using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Result of a geometric or parameter validation check.
/// </summary>
public sealed record ValidationResult
{
    public bool IsSuccess { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static ValidationResult Ok() => new() { IsSuccess = true };

    public static ValidationResult Fail(string error) => new()
    {
        IsSuccess = false,
        ErrorMessage = error
    };

    public static ValidationResult WithWarnings(IReadOnlyList<string> warnings) => new()
    {
        IsSuccess = true,
        Warnings = warnings ?? Array.Empty<string>()
    };
}
