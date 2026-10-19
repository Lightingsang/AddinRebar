namespace HPBim.Core.Execution;

using System;

/// <summary>
/// Snapshot of Revit document identity and version state used for pre-flight validation.
/// Immutable domain model residing in HPBim.Core (netstandard2.0) with zero Revit references.
/// </summary>
public sealed record DocumentIdentitySnapshot
{
    public string Title { get; init; } = string.Empty;
    public string? PathName { get; init; }
    public Guid VersionGuid { get; init; } = Guid.Empty;
    public int NumberOfSaves { get; init; }
    public Guid? CentralGuid { get; init; }
    public string? ActiveViewName { get; init; }

    /// <summary>
    /// Indicates whether this snapshot contains valid identification data.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Title) && VersionGuid == Guid.Empty;

    /// <summary>
    /// Indicates whether the document is workshared (has non-empty CentralGuid).
    /// </summary>
    public bool IsWorkshared => CentralGuid.HasValue && CentralGuid.Value != Guid.Empty;

    /// <summary>
    /// Validates whether the live document identity matches this snapshot.
    /// </summary>
    public bool Matches(string actualTitle, Guid actualVersionGuid) =>
        Matches(actualTitle, null, actualVersionGuid);

    /// <summary>
    /// Validates whether the live document identity matches this snapshot, checking Title, PathName, and VersionGuid.
    /// </summary>
    public bool Matches(string actualTitle, string? actualPathName, Guid actualVersionGuid)
    {
        if (!string.IsNullOrEmpty(Title) &&
            !string.Equals(Title, actualTitle, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(PathName) &&
            !string.Equals(PathName, actualPathName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (VersionGuid != Guid.Empty && actualVersionGuid != VersionGuid)
        {
            return false;
        }

        return true;
    }

    public override string ToString() =>
        string.IsNullOrEmpty(Title)
            ? "[Empty Document Snapshot]"
            : $"{Title} (VersionGUID: {VersionGuid:D}, Saves: {NumberOfSaves})";
}
