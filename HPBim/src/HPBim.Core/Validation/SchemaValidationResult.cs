namespace HPBim.Core.Validation;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents the result of an artifact and schema validation check.
/// </summary>
public sealed record SchemaValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public string? DetectedArtifactType { get; init; }
    public string? DetectedSchemaVersion { get; init; }

    public static SchemaValidationResult Success(
        string? artifactType = null,
        string? schemaVersion = null,
        IReadOnlyList<string>? warnings = null) =>
        new()
        {
            Errors = Array.Empty<string>(),
            Warnings = warnings ?? Array.Empty<string>(),
            DetectedArtifactType = artifactType,
            DetectedSchemaVersion = schemaVersion
        };

    public static SchemaValidationResult Failure(
        IEnumerable<string> errors,
        string? artifactType = null,
        string? schemaVersion = null,
        IReadOnlyList<string>? warnings = null) =>
        new()
        {
            Errors = new List<string>(errors),
            Warnings = warnings ?? Array.Empty<string>(),
            DetectedArtifactType = artifactType,
            DetectedSchemaVersion = schemaVersion
        };

    public static SchemaValidationResult Failure(string singleError) =>
        Failure(new[] { singleError });
}
