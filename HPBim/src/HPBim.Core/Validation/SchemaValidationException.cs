namespace HPBim.Core.Validation;

using System;
using System.Collections.Generic;

/// <summary>
/// Exception thrown when strict schema validation fails during deserialization.
/// </summary>
public sealed class SchemaValidationException : Exception
{
    public IReadOnlyList<string> ValidationErrors { get; }

    public SchemaValidationException(IReadOnlyList<string> errors)
        : base($"BIM Model schema validation failed with {errors?.Count ?? 0} error(s): {string.Join("; ", errors ?? Array.Empty<string>())}")
    {
        ValidationErrors = errors ?? Array.Empty<string>();
    }

    public SchemaValidationException(string singleError)
        : this(new[] { singleError })
    {
    }
}
