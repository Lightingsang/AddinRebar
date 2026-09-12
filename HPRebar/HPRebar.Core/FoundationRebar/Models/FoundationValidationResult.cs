using System;
using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.FoundationRebar.Models;

/// <summary>
/// Result of foundation geometry and rebar parameter pre-flight validation.
/// </summary>
public sealed record FoundationValidationResult
{
    public bool IsValid { get; init; } = true;

    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();

    /// <summary>Single aggregated error string if validation failed; null otherwise.</summary>
    public string? ErrorMessage => ErrorMessages.Count > 0 ? string.Join("; ", ErrorMessages) : null;

    public static FoundationValidationResult Success() => new()
    {
        IsValid = true,
        ErrorMessages = Array.Empty<string>()
    };

    public static FoundationValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        ErrorMessages = errors ?? Array.Empty<string>()
    };

    public static FoundationValidationResult Failure(IEnumerable<string> errors) => new()
    {
        IsValid = false,
        ErrorMessages = errors?.ToList() ?? new List<string>()
    };
}
